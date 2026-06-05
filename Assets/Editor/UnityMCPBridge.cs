using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace UnityMCP
{
    [InitializeOnLoad]
    public static class UnityMCPBridge
    {
        private static int _activePort = 8035;
        private static HttpListener _listener;
        private static Thread _listenerThread;
        private static readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
        private static readonly object _queueLock = new object();
        private static readonly object _listenerLock = new object();
        private static bool _isStopping = false;


        // Console Log cache
        private static readonly List<LogEntry> _logCache = new List<LogEntry>();
        private const int MaxLogCache = 2000;

        [System.Serializable]
        public struct LogEntry
        {
            public string condition;
            public string stackTrace;
            public string type;
        }

        static UnityMCPBridge()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.quitting += OnEditorQuitting;
            EditorApplication.update += Update;
            Application.logMessageReceivedThreaded += OnLogReceived;
            StartServer();
        }

        private static void OnBeforeAssemblyReload()
        {
            StopServer();
        }

        private static void OnEditorQuitting()
        {
            StopServer();
        }

        private static void StopServer()
        {
            lock (_listenerLock)
            {
                _isStopping = true;
                try
                {
                    if (_listener != null)
                    {
                        _listener.Abort(); // Forcefully abort connections to release the port instantly!
                        _listener.Close();
                        _listener = null;
                        Debug.Log("[UnityMCP] Server stopped forcefully for assembly reload.");
                    }

                    // Xóa file chứa cổng để tránh stale data
                    string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "unity-mcp-port.txt");
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UnityMCP] Error stopping server: {e.Message}");
                }
            }
        }

        private static void StartServer()
        {
            lock (_listenerLock)
            {
                _isStopping = false;
            }

            // Chạy khởi động Server trong một Thread nền để thực hiện cơ chế quét cổng tự động
            // tránh trùng cổng hoàn toàn khi có nhiều dự án mở cùng lúc hoặc cổng bị chiếm ngầm
            var bindThread = new Thread(() =>
            {
                int basePort = 8035;
                int maxRetries = 15;
                int delayMs = 500;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    lock (_listenerLock)
                    {
                        if (_isStopping)
                        {
                            return; // Dừng ngay lập tức nếu luồng chính đang yêu cầu dừng
                        }

                        // Quét dải cổng từ 8035 đến 8045 để tìm cổng khả dụng đầu tiên
                        for (int port = basePort; port <= basePort + 10; port++)
                        {
                            try
                            {
                                _listener = new HttpListener();
                                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                                _listener.Start();

                                _activePort = port;
                                _listenerThread = new Thread(ListenLoop)
                                {
                                    IsBackground = true
                                };
                                _listenerThread.Start();

                                // Ghi cổng đang chạy vào thư mục Temp để node.js đọc và kết nối đúng cổng
                                try
                                {
                                    string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "unity-mcp-port.txt");
                                    string dir = Path.GetDirectoryName(tempPath);
                                    if (!Directory.Exists(dir))
                                    {
                                        Directory.CreateDirectory(dir);
                                    }
                                    File.WriteAllText(tempPath, _activePort.ToString());
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogWarning($"[UnityMCP] Không thể ghi file cổng active: {ex.Message}");
                                }

                                Debug.Log($"[UnityMCP] Server started successfully on http://127.0.0.1:{_activePort}/");
                                return; // Kết nối thành công, thoát khỏi thread
                            }
                            catch (Exception)
                            {
                                if (_listener != null)
                                {
                                    _listener.Close();
                                    _listener = null;
                                }
                            }
                        }

                        if (attempt == maxRetries)
                        {
                            Debug.LogError($"[UnityMCP] Failed to start server after scanning ports 8035-8045 across {maxRetries} attempts.");
                            return;
                        }
                    }

                    // Ngủ ngoài khối lock
                    Thread.Sleep(delayMs);
                }
            });
            bindThread.IsBackground = true;
            bindThread.Start();
        }

        private static void ListenLoop()
        {
            while (_listener != null && _listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    EnqueueOnMainThread(() => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    // Listener stopped
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UnityMCP] Error in listener loop: {e.Message}");
                }
            }
        }

        private static void EnqueueOnMainThread(Action action)
        {
            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }

        private static void Update()
        {
            lock (_queueLock)
            {
                while (_mainThreadQueue.Count > 0)
                {
                    try
                    {
                        _mainThreadQueue.Dequeue()?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[UnityMCP] Error executing main thread action: {e.Message}");
                    }
                }
            }
        }

        private static void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(condition)) return;
            if (condition.Contains("missing!") || condition.Contains("BlueLightMusroom") || condition.Contains("smallMushRoom")) return;

            lock (_logCache)
            {
                _logCache.Add(new LogEntry
                {
                    condition = condition,
                    stackTrace = stackTrace,
                    type = type.ToString()
                });
                if (_logCache.Count > MaxLogCache)
                {
                    _logCache.RemoveAt(0);
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            response.ContentType = "application/json; charset=utf-8";

            if (request.HttpMethod == "OPTIONS")
            {
                SendResponse(response, HttpStatusCode.OK, "{}");
                return;
            }

            try
            {
                string path = request.Url.AbsolutePath.ToLower();
                switch (path)
                {
                    case "/hierarchy":
                        HandleHierarchy(response);
                        break;
                    case "/inspect":
                        HandleInspect(request, response);
                        break;
                    case "/update":
                        HandleUpdate(request, response);
                        break;
                    case "/logs":
                        HandleLogs(response);
                        break;
                    case "/playmode":
                        HandlePlayMode(request, response);
                        break;
                    case "/debug":
                        HandleDebug(response);
                        break;
                    case "/equip":
                        HandleEquip(request, response);
                        break;
                    case "/slice":
                        HandleSlice(request, response);
                        break;
                    case "/spawn":
                        HandleSpawn(request, response);
                        break;
                    case "/invoke":
                        HandleInvoke(request, response);
                        break;
                    case "/find":
                        HandleFind(request, response);
                        break;
                    default:
                        SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"Not Found\"}");
                        break;
                }
            }
            catch (Exception e)
            {
                SendResponse(response, HttpStatusCode.InternalServerError, "{\"error\":\"" + EscapeJson(e.Message) + "\"}");
            }
        }

        private static void HandleDebug(HttpListenerResponse response)
        {
            var manager = UnityEngine.Object.FindFirstObjectByType<LPCEquipmentManager>();
            if (manager == null)
            {
                SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"LPCEquipmentManager not found in scene\"}");
                return;
            }

            var sb = new StringBuilder();
                        sb.Append("{");
            sb.Append("\"managerActive\":" + manager.enabled.ToString().ToLower() + ",");
            sb.Append("\"slots\":[");
            if (manager.slotDefinitions != null)
            {
                for (int i = 0; i < manager.slotDefinitions.Count; i++)
                {
                    var slot = manager.slotDefinitions[i];
                    sb.Append("{");
                    sb.Append("\"slotName\":\"" + EscapeJson(slot.slotName) + "\",\"displayName\":\"" + EscapeJson(slot.displayName) + "\"");
                    sb.Append("}");
                    if (i < manager.slotDefinitions.Count - 1) sb.Append(",");
                }
            }
            sb.Append("],");
            sb.Append("\"equipped\":[");
            var equipped = manager.GetAllEquipped();
            if (equipped != null)
            {
                int count = 0;
                foreach (var kv in equipped)
                {
                    sb.Append("{");
                    sb.Append("\"slot\":\"" + EscapeJson(kv.Key) + "\",");
                    sb.Append("\"itemName\":\"" + (kv.Value != null ? EscapeJson(kv.Value.itemName) : "null") + "\",");
                    sb.Append("\"hasLibrary\":" + (kv.Value != null && kv.Value.spriteLibrary != null).ToString().ToLower());
                    sb.Append("}");
                    count++;
                    if (count < equipped.Count) sb.Append(",");
                }
            }
            sb.Append("],");

            // Check child objects and their components
            sb.Append("\"children\":[");
            var children = new List<string>();
            for (int i = 0; i < manager.transform.childCount; i++)
            {
                var child = manager.transform.GetChild(i);
                var sr = child.GetComponent<SpriteRenderer>();
                var sl = child.GetComponent<UnityEngine.U2D.Animation.SpriteLibrary>();
                var resolver = child.GetComponent<UnityEngine.U2D.Animation.SpriteResolver>();
                var sync = child.GetComponent<LPCSpriteSync>();

                var csb = new StringBuilder();
                csb.Append("{");
                csb.Append("\"name\":\"" + EscapeJson(child.name) + "\",");
                csb.Append("\"srEnabled\":" + (sr != null ? sr.enabled.ToString().ToLower() : "false") + ",");
                csb.Append("\"spriteName\":\"" + (sr != null && sr.sprite != null ? EscapeJson(sr.sprite.name) : "null") + "\",");
                csb.Append("\"hasLibraryAsset\":" + (sl != null && sl.spriteLibraryAsset != null).ToString().ToLower() + ",");
                csb.Append("\"libraryAssetName\":\"" + (sl != null && sl.spriteLibraryAsset != null ? EscapeJson(sl.spriteLibraryAsset.name) : "null") + "\",");
                csb.Append("\"resolverCategory\":\"" + (resolver != null ? EscapeJson(resolver.GetCategory()) : "null") + "\",");
                csb.Append("\"resolverLabel\":\"" + (resolver != null ? EscapeJson(resolver.GetLabel()) : "null") + "\",");
                csb.Append("\"hasSpriteSync\":" + (sync != null).ToString().ToLower());
                csb.Append("}");
                children.Add(csb.ToString());
            }
            sb.Append(string.Join(",", children));
            sb.Append("]");
            sb.Append("}");

            SendResponse(response, HttpStatusCode.OK, sb.ToString());
        }

        private static void HandleEquip(HttpListenerRequest request, HttpListenerResponse response)
        {
            string itemName = request.QueryString["item"];
            string slotName = request.QueryString["slot"];

            if (string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(slotName))
            {
                SendResponse(response, HttpStatusCode.BadRequest, "{\"error\":\"Missing 'item' or 'slot' parameters\"}");
                return;
            }

            var manager = UnityEngine.Object.FindFirstObjectByType<LPCEquipmentManager>();
            if (manager == null)
            {
                SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"LPCEquipmentManager not found in scene\"}");
                return;
            }

            string path = $"Assets/Items/{itemName}/{itemName}_ItemData.asset";
            var item = AssetDatabase.LoadAssetAtPath<LPCItemData>(path);
            if (item == null)
            {
                SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"Item asset not found at " + path + "\"}");
                return;
            }

            bool success = manager.EquipItem(slotName, item);
            manager.RebindParentAnimator();

            SendResponse(response, HttpStatusCode.OK, "{\"success\":" + success.ToString().ToLower() + "}");
        }

        private static void HandleHierarchy(HttpListenerResponse response)
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var sb = new StringBuilder();
            sb.Append("{\"rootObjects\":[");
            for (int i = 0; i < roots.Length; i++)
            {
                sb.Append(SerializeGameObject(roots[i]));
                if (i < roots.Length - 1) sb.Append(",");
            }
            sb.Append("]}");

            SendResponse(response, HttpStatusCode.OK, sb.ToString());
        }

        private static string SerializeGameObject(GameObject go)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"name\":\"" + EscapeJson(go.name) + "\",");
            sb.Append("\"id\":" + go.GetInstanceID() + ",");
            sb.Append("\"active\":" + go.activeSelf.ToString().ToLower() + ",");
            sb.Append("\"tag\":\"" + EscapeJson(go.tag) + "\",");
            sb.Append("\"layer\":" + go.layer + ",");
            
            sb.Append("\"components\":[");
            var comps = go.GetComponents<Component>();
            List<string> compNames = new List<string>();
            foreach (var comp in comps)
            {
                if (comp == null) continue;
                compNames.Add("\"" + EscapeJson(comp.GetType().Name) + "\"");
            }
            sb.Append(string.Join(",", compNames));
            sb.Append("],");

            sb.Append("\"children\":[");
            for (int i = 0; i < go.transform.childCount; i++)
            {
                sb.Append(SerializeGameObject(go.transform.GetChild(i).gameObject));
                if (i < go.transform.childCount - 1) sb.Append(",");
            }
            sb.Append("]");
            sb.Append("}");
            return sb.ToString();
        }

        private static void HandleInspect(HttpListenerRequest request, HttpListenerResponse response)
        {
            string idStr = request.QueryString["id"];
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int id))
            {
                SendResponse(response, HttpStatusCode.BadRequest, "{\"error\":\"Missing or invalid 'id' parameter\"}");
                return;
            }

            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            GameObject target = null;

            foreach (var r in roots)
            {
                target = FindGameObjectByID(r, id);
                if (target != null) break;
            }

            if (target == null)
            {
                SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"GameObject not found\"}");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"name\":\"" + EscapeJson(target.name) + "\",");
            sb.Append("\"id\":" + target.GetInstanceID() + ",");
            sb.Append("\"activeSelf\":" + target.activeSelf.ToString().ToLower() + ",");
            sb.Append("\"activeInHierarchy\":" + target.activeInHierarchy.ToString().ToLower() + ",");
            sb.Append("\"tag\":\"" + EscapeJson(target.tag) + "\",");
            sb.Append("\"layer\":\"" + LayerMask.LayerToName(target.layer) + "\",");

            sb.Append("\"transform\":{");
            sb.Append("\"position\":{\"x\":" + target.transform.position.x + ",\"y\":" + target.transform.position.y + ",\"z\":" + target.transform.position.z + "},");
            sb.Append("\"rotation\":{\"x\":" + target.transform.rotation.eulerAngles.x + ",\"y\":" + target.transform.rotation.eulerAngles.y + ",\"z\":" + target.transform.rotation.eulerAngles.z + "},");
            sb.Append("\"localScale\":{\"x\":" + target.transform.localScale.x + ",\"y\":" + target.transform.localScale.y + ",\"z\":" + target.transform.localScale.z + "}");
            sb.Append("},");

            sb.Append("\"components\":{");
            var comps = target.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                var comp = comps[i];
                if (comp == null) continue;
                string typeName = comp.GetType().Name;
                sb.Append("\"" + EscapeJson(typeName) + "\":{");
                
                // Advanced Reflection: Read public fields AND private fields marked with [SerializeField]
                var fields = comp.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                List<string> fieldEntries = new List<string>();
                foreach (var f in fields)
                {
                    try
                    {
                        bool isSerialized = f.IsPublic || Attribute.IsDefined(f, typeof(SerializeField));
                        if (!isSerialized) continue;

                        var val = f.GetValue(comp);
                        string valStr = val == null ? "null" : val.ToString();
                        fieldEntries.Add("\"" + EscapeJson(f.Name) + "\":\"" + EscapeJson(valStr) + "\"");
                    }
                    catch { }
                }
                sb.Append(string.Join(",", fieldEntries));
                sb.Append("}");
                if (i < comps.Length - 1) sb.Append(",");
            }
            sb.Append("}");
            sb.Append("}");

            SendResponse(response, HttpStatusCode.OK, sb.ToString());
        }

        private static void HandleUpdate(HttpListenerRequest request, HttpListenerResponse response)
        {
            string idStr = request.QueryString["id"];
            string compName = request.QueryString["component"];
            string fieldName = request.QueryString["field"];
            string valStr = request.QueryString["value"];

            if (string.IsNullOrEmpty(idStr) || string.IsNullOrEmpty(compName) || string.IsNullOrEmpty(fieldName) || valStr == null)
            {
                SendResponse(response, HttpStatusCode.BadRequest, "{\"error\":\"Missing parameters id, component, field, or value\"}");
                return;
            }

            if (!int.TryParse(idStr, out int id))
            {
                SendResponse(response, HttpStatusCode.BadRequest, "{\"error\":\"Invalid id parameter\"}");
                return;
            }

            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            GameObject target = null;
            foreach (var r in roots)
            {
                target = FindGameObjectByID(r, id);
                if (target != null) break;
            }

            if (target == null)
            {
                SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"GameObject not found\"}");
                return;
            }

            Component comp = target.GetComponent(compName);
            if (comp == null)
            {
                var allComps = target.GetComponentsInChildren<Component>(true);
                foreach (var c in allComps)
                {
                    if (c != null && (c.GetType().Name == compName || c.GetType().FullName == compName))
                    {
                        comp = c;
                        break;
                    }
                }
            }
            if (comp == null)
            {
                SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"Component not found on GameObject\"}");
                return;
            }

            var field = comp.GetType().GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"Field not found on component\"}");
                return;
            }

            try
            {
                object convertedValue = ConvertValue(valStr, field.FieldType);
                field.SetValue(comp, convertedValue);
                
                // Mark dirty so changes are saved in the Unity scene!
                EditorUtility.SetDirty(comp);
                if (!Application.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(target.scene);
                }

                Debug.Log("[UnityMCP] Field '" + fieldName + "' on component '" + compName + "' successfully updated to: " + convertedValue);
                SendResponse(response, HttpStatusCode.OK, "{\"success\":true,\"component\":\"" + compName + "\",\"field\":\"" + fieldName + "\",\"newValue\":\"" + EscapeJson(convertedValue.ToString()) + "\"}");
            }
            catch (Exception e)
            {
                SendResponse(response, HttpStatusCode.InternalServerError, "{\"error\":\"Failed to convert or set value: " + EscapeJson(e.Message) + "\"}");
            }
        }

        private static object ConvertValue(string value, Type type)
        {
            if (type == typeof(string)) return value;
            if (type == typeof(int)) return int.Parse(value);
            if (type == typeof(float)) return float.Parse(value);
            if (type == typeof(bool)) return bool.Parse(value);
            if (type.IsEnum) return Enum.Parse(type, value, true);
            
            return Convert.ChangeType(value, type);
        }

        private static void HandleLogs(HttpListenerResponse response)
        {
            var sb = new StringBuilder();
            sb.Append("{\"logs\":[");
            lock (_logCache)
            {
                for (int i = 0; i < _logCache.Count; i++)
                {
                    var entry = _logCache[i];
                    sb.Append("{");
                    sb.Append("\"type\":\"" + EscapeJson(entry.type) + "\",");
                    sb.Append("\"message\":\"" + EscapeJson(entry.condition) + "\",");
                    sb.Append("\"stackTrace\":\"" + EscapeJson(entry.stackTrace) + "\"");
                    sb.Append("}");
                    if (i < _logCache.Count - 1) sb.Append(",");
                }
            }
            sb.Append("]}");
            SendResponse(response, HttpStatusCode.OK, sb.ToString());
        }

        private static void HandlePlayMode(HttpListenerRequest request, HttpListenerResponse response)
        {
            string state = request.QueryString["state"];
            if (string.IsNullOrEmpty(state))
            {
                SendResponse(response, HttpStatusCode.BadRequest, "{\"error\":\"Missing 'state' parameter (play|pause|stop)\"}");
                return;
            }

            switch (state.ToLower())
            {
                case "play":
                    EditorApplication.isPlaying = true;
                    break;
                case "pause":
                    EditorApplication.isPaused = true;
                    break;
                case "stop":
                    EditorApplication.isPlaying = false;
                    break;
                default:
                    SendResponse(response, HttpStatusCode.BadRequest, "{\"error\":\"Invalid 'state' value. Use play, pause, or stop.\"}");
                    return;
            }

            SendResponse(response, HttpStatusCode.OK, "{\"success\":true,\"state\":\"" + state + "\"}");
        }

        private static GameObject FindGameObjectByID(GameObject root, int id)
        {
            if (root.GetInstanceID() == id) return root;
            for (int i = 0; i < root.transform.childCount; i++)
            {
                var found = FindGameObjectByID(root.transform.GetChild(i).gameObject, id);
                if (found != null) return found;
            }
            return null;
        }

        private static object GetGameObjectTree(GameObject go)
        {
            var children = new List<object>();
            for (int i = 0; i < go.transform.childCount; i++)
            {
                children.Add(GetGameObjectTree(go.transform.GetChild(i).gameObject));
            }
            return new { name = go.name, id = go.GetInstanceID(), children };
        }

        private static void SendResponse(HttpListenerResponse response, HttpStatusCode statusCode, string content)
        {
            try
            {
                response.KeepAlive = false; // Ngăn ngừa giữ kết nối lâu làm kẹt cổng (TIME_WAIT) khi compile
                byte[] buffer = Encoding.UTF8.GetBytes(content);
                response.StatusCode = (int)statusCode;
                response.ContentLength64 = buffer.Length;
                using (var output = response.OutputStream)
                {
                    output.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityMCP] Error sending response: {ex.Message}");
            }
            finally
            {
                try { response.Close(); } catch { }
            }
        }

        private static void HandleSlice(HttpListenerRequest request, HttpListenerResponse response)
        {
            string path = request.QueryString["path"];
            string widthStr = request.QueryString["width"];
            string heightStr = request.QueryString["height"];
            string ppuStr = request.QueryString["ppu"];
            string filterStr = request.QueryString["filter"];
            string pxStr = request.QueryString["px"];
            string pyStr = request.QueryString["py"];

            if (string.IsNullOrEmpty(path))
            {
                SendResponse(response, HttpStatusCode.BadRequest, "{\"error\":\"Missing 'path' parameter\"}");
                return;
            }

            int width = string.IsNullOrEmpty(widthStr) ? 64 : int.Parse(widthStr);
            int height = string.IsNullOrEmpty(heightStr) ? 64 : int.Parse(heightStr);
            int ppu = string.IsNullOrEmpty(ppuStr) ? 64 : int.Parse(ppuStr);
            FilterMode filter = FilterMode.Point;
            if (!string.IsNullOrEmpty(filterStr))
            {
                Enum.TryParse(filterStr, true, out filter);
            }
            float px = string.IsNullOrEmpty(pxStr) ? 0.5f : float.Parse(pxStr);
            float py = string.IsNullOrEmpty(pyStr) ? 0.05f : float.Parse(pyStr);

            List<string> texturesToSlice = new List<string>();
            if (Directory.Exists(path) || AssetDatabase.IsValidFolder(path))
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                foreach (var g in guids)
                {
                    texturesToSlice.Add(AssetDatabase.GUIDToAssetPath(g));
                }
            }
            else if (File.Exists(path) || AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
            {
                texturesToSlice.Add(path);
            }

            if (texturesToSlice.Count == 0)
            {
                SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"No textures found at path: " + EscapeJson(path) + "\"}");
                return;
            }

            int slicedCount = 0;
            foreach (var texPath in texturesToSlice)
            {
                if (SliceSingleTexture(texPath, width, height, ppu, filter, px, py))
                {
                    slicedCount++;
                }
            }

            AssetDatabase.Refresh();
            SendResponse(response, HttpStatusCode.OK, "{\"success\":true,\"slicedCount\":" + slicedCount + ",\"totalRequested\":" + texturesToSlice.Count + "}");
        }

        private static bool SliceSingleTexture(string assetPath, int spriteWidth, int spriteHeight, int ppu, FilterMode filter, float pX, float pY)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return false;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null) return false;

            importer.isReadable = true;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = filter;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;

            int tw = tex.width;
            int th = tex.height;

            int cols = Mathf.Max(1, tw / spriteWidth);
            int rows = Mathf.Max(1, th / spriteHeight);

            var rects = new List<SpriteMetaData>();
            string filename = Path.GetFileNameWithoutExtension(assetPath);

            int align = (int)SpriteAlignment.Custom;
            if (pX == 0.5f && pY == 0f) align = (int)SpriteAlignment.BottomCenter;
            else if (pX == 0.5f && pY == 0.5f) align = (int)SpriteAlignment.Center;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    rects.Add(new SpriteMetaData
                    {
                        pivot = new Vector2(pX, pY),
                        alignment = align,
                        name = $"{filename}_r{r}_c{c}",
                        rect = new Rect(
                            c * spriteWidth,
                            (rows - r - 1) * spriteHeight,
                            spriteWidth,
                            spriteHeight)
                    });
                }
            }

            importer.spritesheet = rects.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            return true;
        }

        private static void HandleSpawn(HttpListenerRequest request, HttpListenerResponse response)
        {
            string prefabPath = request.QueryString["prefab"];
            string xStr = request.QueryString["x"];
            string yStr = request.QueryString["y"];
            string name = request.QueryString["name"];

            if (string.IsNullOrEmpty(prefabPath))
            {
                SendResponse(response, HttpStatusCode.BadRequest, "{\"error\":\"Missing 'prefab' parameter\"}");
                return;
            }

            float x = string.IsNullOrEmpty(xStr) ? 0f : float.Parse(xStr);
            float y = string.IsNullOrEmpty(yStr) ? 0f : float.Parse(yStr);

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"Prefab not found at path: " + EscapeJson(prefabPath) + "\"}");
                return;
            }

            GameObject instance;
            if (Application.isPlaying)
            {
                instance = UnityEngine.Object.Instantiate(prefabAsset, new Vector3(x, y, 0f), Quaternion.identity);
            }
            else
            {
                instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                if (instance != null)
                {
                    instance.transform.position = new Vector3(x, y, 0f);
                    Undo.RegisterCreatedObjectUndo(instance, "Spawn Prefab via MCP");
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                }
            }

            if (instance == null)
            {
                SendResponse(response, HttpStatusCode.InternalServerError, "{\"error\":\"Failed to instantiate prefab\"}");
                return;
            }

            if (!string.IsNullOrEmpty(name))
            {
                instance.name = name;
            }

            SendResponse(response, HttpStatusCode.OK, "{\"success\":true,\"name\":\"" + EscapeJson(instance.name) + "\",\"id\":" + instance.GetInstanceID() + ",\"position\":{\"x\":" + instance.transform.position.x + ",\"y\":" + instance.transform.position.y + "}}");
        }

        private static void HandleInvoke(HttpListenerRequest request, HttpListenerResponse response)
        {
            string className = request.QueryString["class"];
            string methodName = request.QueryString["method"];
            string argsStr = request.QueryString["args"];

            if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(methodName))
            {
                SendResponse(response, HttpStatusCode.BadRequest, "{\"error\":\"Missing 'class' or 'method' parameters\"}");
                return;
            }

            Type type = Type.GetType(className);
            if (type == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(className);
                    if (type != null) break;
                }
            }

            if (type == null)
            {
                SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"Class type '" + EscapeJson(className) + "' not found\"}");
                return;
            }

            System.Reflection.MethodInfo method = null;
            try
            {
                method = type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            }
            catch (System.Reflection.AmbiguousMatchException)
            {
                int expectedParamCount = string.IsNullOrEmpty(argsStr) ? 0 : argsStr.Split(',').Length;
                var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                foreach (var m in methods)
                {
                    if (m.Name == methodName && m.GetParameters().Length == expectedParamCount)
                    {
                        method = m;
                        break;
                    }
                }
            }

            if (method == null)
            {
                SendResponse(response, HttpStatusCode.NotFound, "{\"error\":\"Static method '" + EscapeJson(methodName) + "' not found on class '" + EscapeJson(className) + "'\"}");
                return;
            }

            try
            {
                object[] parameters = null;
                if (!string.IsNullOrEmpty(argsStr))
                {
                    string[] rawArgs = argsStr.Split(',');
                    var methodParams = method.GetParameters();
                    parameters = new object[methodParams.Length];
                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        if (i < rawArgs.Length)
                        {
                            parameters[i] = ConvertValue(rawArgs[i].Trim(), methodParams[i].ParameterType);
                        }
                        else
                        {
                            parameters[i] = methodParams[i].HasDefaultValue ? methodParams[i].DefaultValue : null;
                        }
                    }
                }

                object result = method.Invoke(null, parameters);
                string resultStr = result == null ? "null" : result.ToString();

                SendResponse(response, HttpStatusCode.OK, "{\"success\":true,\"result\":\"" + EscapeJson(resultStr) + "\"}");
            }
            catch (Exception e)
            {
                SendResponse(response, HttpStatusCode.InternalServerError, "{\"error\":\"Invoke exception: " + EscapeJson(e.InnerException != null ? e.InnerException.Message : e.Message) + "\"}");
            }
        }

        private static void HandleFind(HttpListenerRequest request, HttpListenerResponse response)
        {
            string query = request.QueryString["query"];
            if (string.IsNullOrEmpty(query))
            {
                SendResponse(response, HttpStatusCode.BadRequest, "{\"error\":\"Missing 'query' parameter\"}");
                return;
            }

            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var sb = new StringBuilder();
            sb.Append("{\"results\":[");

            int count = 0;
            foreach (var go in allObjects)
            {
                bool matches = go.name.Contains(query, StringComparison.OrdinalIgnoreCase);
                if (!matches)
                {
                    var comps = go.GetComponents<Component>();
                    foreach (var c in comps)
                    {
                        if (c != null && c.GetType().Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            matches = true;
                            break;
                        }
                    }
                }

                if (matches)
                {
                    if (count > 0) sb.Append(",");
                    sb.Append("{");
                    sb.Append("\"name\":\"" + EscapeJson(go.name) + "\",");
                    sb.Append("\"id\":" + go.GetInstanceID() + ",");
                    sb.Append("\"active\":" + go.activeInHierarchy.ToString().ToLower() + ",");
                    sb.Append("\"position\":{\"x\":" + go.transform.position.x + ",\"y\":" + go.transform.position.y + ",\"z\":" + go.transform.position.z + "}");
                    sb.Append("}");
                    count++;
                    if (count >= 50) break;
                }
            }
            sb.Append("]}");
            SendResponse(response, HttpStatusCode.OK, sb.ToString());
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}
#endif

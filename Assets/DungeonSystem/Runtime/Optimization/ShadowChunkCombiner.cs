using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DungeonSystem.Optimization
{
    public class ShadowChunkCombiner : MonoBehaviour
    {
        public void CombineShadowProxies(Transform rootParent, int chunkSize)
        {
            ShadowProxyRenderer[] proxies = rootParent.GetComponentsInChildren<ShadowProxyRenderer>();
            if (proxies == null || proxies.Length == 0) return;

            // Phân loại proxy vào các Chunks (bằng Dictionary chia lưới 2D)
            Dictionary<Vector2Int, List<ShadowProxyRenderer>> chunkBuckets = new Dictionary<Vector2Int, List<ShadowProxyRenderer>>();

            foreach (var proxy in proxies)
            {
                Vector3 pos = proxy.transform.position;
                Vector2Int chunkCoord = new Vector2Int(
                    Mathf.FloorToInt(pos.x / chunkSize),
                    Mathf.FloorToInt(pos.z / chunkSize)
                );

                if (!chunkBuckets.ContainsKey(chunkCoord))
                {
                    chunkBuckets[chunkCoord] = new List<ShadowProxyRenderer>();
                }
                chunkBuckets[chunkCoord].Add(proxy);
            }

            // Gộp Mesh cho từng Chunk
            foreach (var chunk in chunkBuckets)
            {
                CombineSingleChunk(chunk.Key, chunk.Value, rootParent);
            }
        }

        private void CombineSingleChunk(Vector2Int coord, List<ShadowProxyRenderer> list, Transform parent)
        {
            List<CombineInstance> combineList = new List<CombineInstance>();

            foreach (var proxy in list)
            {
                Mesh meshToUse = proxy.customShadowMesh;
                if (meshToUse == null && proxy.proxyMeshFilter != null)
                {
                    meshToUse = proxy.proxyMeshFilter.sharedMesh;
                }

                if (meshToUse == null) continue;

                CombineInstance ci = new CombineInstance
                {
                    mesh = meshToUse,
                    transform = proxy.transform.localToWorldMatrix
                };
                combineList.Add(ci);

                // Disable Shadow Proxy Renderer Gameobject để tránh việc render trùng lặp
                // (Chỉ giữ lại Mesh Renderer hình ảnh chính của Wall)
                if (proxy.proxyMeshFilter != null)
                {
                    var renderer = proxy.proxyMeshFilter.GetComponent<MeshRenderer>();
                    if (renderer != null) renderer.enabled = false;
                }
            }

            if (combineList.Count == 0) return;

            Mesh combinedMesh = new Mesh();
            combinedMesh.name = $"ShadowChunkMesh_{coord.x}_{coord.y}";
            combinedMesh.CombineMeshes(combineList.ToArray(), true, true);

            // Tạo GameObject hiển thị bóng đổ Chunk
            GameObject chunkObj = new GameObject($"ShadowChunk_{coord.x}_{coord.y}");
            chunkObj.transform.SetParent(parent);
            chunkObj.transform.position = Vector3.zero;

            MeshFilter filter = chunkObj.AddComponent<MeshFilter>();
            filter.sharedMesh = combinedMesh;

            MeshRenderer rendererCombined = chunkObj.AddComponent<MeshRenderer>();
            // Sử dụng một material đơn giản hoặc không gán material vì chế độ ShadowsOnly không hiển thị bề mặt visual
            rendererCombined.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            rendererCombined.receiveShadows = false;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace DungeonSystem.Optimization
{
    [DisallowMultipleComponent]
    public class VegetationPooler : MonoBehaviour
    {
        public static VegetationPooler Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab == null) return null;

            if (!pools.ContainsKey(prefab))
            {
                pools[prefab] = new Queue<GameObject>();
            }

            GameObject obj;
            if (pools[prefab].Count > 0)
            {
                obj = pools[prefab].Dequeue();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.transform.SetParent(parent);
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(prefab, position, rotation, parent);
                var tracker = obj.AddComponent<PooledObjectTracker>();
                tracker.sourcePrefab = prefab;
            }
            return obj;
        }

        public void Recycle(GameObject obj)
        {
            if (obj == null) return;

            var tracker = obj.GetComponent<PooledObjectTracker>();
            if (tracker != null && tracker.sourcePrefab != null)
            {
                obj.SetActive(false);
                // Gán cha về Pooler để không nằm rác trong các phòng bị hủy
                obj.transform.SetParent(transform);
                pools[tracker.sourcePrefab].Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        public void ClearPools()
        {
            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject obj = pool.Dequeue();
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
            }
            pools.Clear();
        }
    }

    public class PooledObjectTracker : MonoBehaviour
    {
        public GameObject sourcePrefab;
    }
}

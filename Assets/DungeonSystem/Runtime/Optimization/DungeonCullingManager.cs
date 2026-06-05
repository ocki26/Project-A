using System.Collections.Generic;
using UnityEngine;
using DungeonSystem.Generation;

namespace DungeonSystem.Optimization
{
    [DisallowMultipleComponent]
    public class DungeonCullingManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Kéo thả Transform của Player vào đây. Nếu trống, hệ thống sẽ tự tìm kiếm Object có Tag là 'Player'")]
        public Transform playerTransform;
        
        private DungeonGenerator generator;
        private Vector2Int lastPlayerGridPos = new Vector2Int(-999, -999);
        private readonly List<GameObject> allSpawnedRooms = new List<GameObject>();
        private readonly HashSet<GameObject> activeRoomsBuffer = new HashSet<GameObject>();

        private void Awake()
        {
            generator = GetComponent<DungeonGenerator>();
        }

        private void Start()
        {
            // TỰ ĐỘNG PHÒNG NGỪA: Nếu chưa kéo thả Player, tự tìm kiếm qua Tag "Player"
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
                else
                {
                    Debug.LogWarning("[DungeonCulling] Chưa gán PlayerTransform và không tìm thấy đối tượng nào có Tag là 'Player'!");
                }
            }
        }

        public void InitializeCulling()
        {
            allSpawnedRooms.Clear();
            lastPlayerGridPos = new Vector2Int(-999, -999);

            if (generator == null || generator.SpawnedRoomsMap.Count == 0) return;

            foreach (var roomObj in generator.SpawnedRoomsMap.Values)
            {
                if (roomObj != null && !allSpawnedRooms.Contains(roomObj))
                {
                    allSpawnedRooms.Add(roomObj);
                }
            }

            // Đồng bộ hóa vị trí người chơi hiện tại ngay khi khởi tạo
            if (playerTransform != null)
            {
                int gridX = Mathf.FloorToInt(playerTransform.position.x / generator.config.cellSize);
                int gridY = Mathf.FloorToInt(playerTransform.position.y / generator.config.cellSize);
                lastPlayerGridPos = new Vector2Int(gridX, gridY);
            }

            UpdateCullingStates(true);
        }

        private void Update()
        {
            if (playerTransform == null || generator == null || generator.SpawnedRoomsMap.Count == 0) 
                return;

            // Xác định chính xác ô lưới logic của người chơi
            int gridX = Mathf.FloorToInt(playerTransform.position.x / generator.config.cellSize);
            int gridY = Mathf.FloorToInt(playerTransform.position.y / generator.config.cellSize);
            Vector2Int currentPlayerGridPos = new Vector2Int(gridX, gridY);

            if (currentPlayerGridPos != lastPlayerGridPos)
            {
                lastPlayerGridPos = currentPlayerGridPos;
                UpdateCullingStates(false);
            }
        }

        private void UpdateCullingStates(bool forceUpdate)
        {
            activeRoomsBuffer.Clear();
            Vector2Int currentPos = lastPlayerGridPos;

            Vector2Int[] targetCells = new Vector2Int[]
            {
                currentPos,                          // Ô hiện tại đứng
                currentPos + new Vector2Int(0, 1),   // Bắc
                currentPos + new Vector2Int(0, -1),  // Nam
                currentPos + new Vector2Int(1, 0),   // Đông
                currentPos + new Vector2Int(-1, 0)   // Tây
            };

            foreach (var cell in targetCells)
            {
                if (generator.SpawnedRoomsMap.TryGetValue(cell, out GameObject roomObj))
                {
                    if (roomObj != null)
                    {
                        activeRoomsBuffer.Add(roomObj);
                    }
                }
            }

            foreach (var room in allSpawnedRooms)
            {
                if (room == null) continue;

                bool shouldBeActive = activeRoomsBuffer.Contains(room);
                if (room.activeSelf != shouldBeActive || forceUpdate)
                {
                    room.SetActive(shouldBeActive);
                }
            }
        }
    }
}

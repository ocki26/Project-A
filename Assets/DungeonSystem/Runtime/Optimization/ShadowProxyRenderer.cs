using UnityEngine;

namespace DungeonSystem.Optimization
{
    public class ShadowProxyRenderer : MonoBehaviour
    {
        [Header("Mesh References")]
        public MeshFilter proxyMeshFilter;
        
        [Tooltip("Mesh thay thế siêu nhẹ (ví dụ Box/Plane) để đổ bóng")]
        public Mesh customShadowMesh;

        private void Awake()
        {
            // Tắt hoàn toàn đổ bóng của Renderer gốc để tối ưu hoá hiệu năng
            MeshRenderer originalRenderer = GetComponent<MeshRenderer>();
            if (originalRenderer != null)
            {
                originalRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}

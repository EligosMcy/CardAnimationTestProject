using UnityEngine;

namespace Test
{
    [RequireComponent(typeof(Camera))]
    public class AspectRatioController : MonoBehaviour
    {
        [Header("目标分辨率比例 (默认 16:9)")]
        public float targetAspectWidth = 16.0f;
        public float targetAspectHeight = 9.0f;

        private Camera targetCamera;
        private float lastScreenWidth = 0;
        private float lastScreenHeight = 0;

        void Start()
        {
            targetCamera = GetComponent<Camera>();
            UpdateViewport();
        }

        void Update()
        {
            // 如果你的游戏支持窗口化并允许玩家拖拽调整大小，可以在 Update 中检测变化
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                UpdateViewport();
            }
        }

        private void UpdateViewport()
        {
            float targetAspect = targetAspectWidth / targetAspectHeight;
            float windowAspect = (float)Screen.width / (float)Screen.height;
            float scaleHeight = windowAspect / targetAspect;

            // 带鱼屏情况：当前屏幕比例比目标比例更宽 (左右黑边 Pillarbox)
            if (scaleHeight > 1.0f)
            {
                Rect rect = targetCamera.rect;
                rect.width = 1.0f / scaleHeight;
                rect.height = 1.0f;
                rect.x = (1.0f - rect.width) / 2.0f; // 计算 X 偏移量，使其居中
                rect.y = 0;
                targetCamera.rect = rect;
            }
            // 传统方屏情况：当前屏幕比目标比例更窄，比如 4:3 显示器 (上下黑边 Letterbox)
            else
            {
                Rect rect = targetCamera.rect;
                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) / 2.0f; // 计算 Y 偏移量，使其居中
                targetCamera.rect = rect;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
    }
}
using UnityEngine;

namespace MapTest
{
    /// <summary>
    /// 圆环布局配置文件，保存半径、数量、部件尺寸等参数。
    /// 供 <see cref="UiRingLayout"/> 加载后生成圆环；也用于调试完成后持久化参数。
    /// </summary>
    [CreateAssetMenu(menuName = "Ring Config")]
    public class RingConfigSO : ScriptableObject
    {
        // ==================== 布局参数 ====================

        /// <summary>
        /// 圆环半径：每个部件中心到圆心的距离。
        /// </summary>
        [Header("布局参数")]
        [SerializeField] private float _radius = 200f;

        /// <summary>
        /// 圆环上的部件数量。
        /// </summary>
        [SerializeField] private int _count = 8;

        /// <summary>
        /// 每个部件的宽度（RectTransform 的 sizeDelta.x）。
        /// </summary>
        [SerializeField] private float _cellWidth = 100f;

        /// <summary>
        /// 每个部件的高度（RectTransform 的 sizeDelta.y）。
        /// </summary>
        [SerializeField] private float _cellHeight = 100f;

        /// <summary>
        /// 起始角度偏移（度），0 表示第一个部件朝 12 点方向。
        /// </summary>
        [SerializeField] private float _startAngleOffset = 0f;

        /// <summary>
        /// 是否顺时针排列，true=顺时针，false=逆时针。
        /// </summary>
        [SerializeField] private bool _isClockwise = true;

        // ==================== 属性 ====================

        /// <summary>圆环半径。</summary>
        public float Radius { get => _radius; set => _radius = value; }

        /// <summary>部件数量。</summary>
        public int Count { get => _count; set => _count = value; }

        /// <summary>部件宽度。</summary>
        public float CellWidth { get => _cellWidth; set => _cellWidth = value; }

        /// <summary>部件高度。</summary>
        public float CellHeight { get => _cellHeight; set => _cellHeight = value; }

        /// <summary>起始角度偏移。</summary>
        public float StartAngleOffset { get => _startAngleOffset; set => _startAngleOffset = value; }

        /// <summary>是否顺时针排列。</summary>
        public bool IsClockwise { get => _isClockwise; set => _isClockwise = value; }
    }
}

using UnityEngine;

namespace MapTest
{
    /// <summary>
    /// 圆环布局配置文件：保存半径、槽位上限、部件尺寸、起始角与方向等布局参数，
    /// 并持有 cell 预制体引用。供 <see cref="UiRingLayout"/> 计算几何、<see cref="RingLayer"/> 实例化 cell。
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

        // ==================== cell 预制体 ====================

        /// <summary>
        /// cell 预制体（需含 RectTransform 与 RingCell），由 RingLayer 实例化。
        /// </summary>
        [Header("cell 预制体")]
        [SerializeField] private GameObject _cellPrefab;

        // ==================== 属性 ====================

        /// <summary>圆环半径。</summary>
        public float Radius { get => _radius; set => _radius = value; }

        /// <summary>部件宽度。</summary>
        public float CellWidth { get => _cellWidth; set => _cellWidth = value; }

        /// <summary>部件高度。</summary>
        public float CellHeight { get => _cellHeight; set => _cellHeight = value; }

        /// <summary>起始角度偏移。</summary>
        public float StartAngleOffset { get => _startAngleOffset; set => _startAngleOffset = value; }

        /// <summary>cell 预制体（需含 RectTransform 与 RingCell）。</summary>
        public GameObject CellPrefab { get => _cellPrefab; set => _cellPrefab = value; }
    }
}

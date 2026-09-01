using System.Collections.Generic;
using UnityEngine;

namespace MapTest
{
    /// <summary>
    /// 环形地图单层组件：包裹一个 <see cref="UiRingLayout"/>，控制本层激活态、大小、方向与错开偏移。
    /// SetVisual 每帧由整图控制器驱动，应用随 index 递减的 baseScale 与随浏览距离衰减的 alpha。
    /// </summary>
    public class RingLayer : MonoBehaviour
    {
        // ==================== 字段 ====================

        /// <summary>
        /// 本层包裹的布局引擎。
        /// </summary>
        [SerializeField] private UiRingLayout _layout;

        /// <summary>
        /// 本层是否激活（激活层 cell 显示 Enable 贴图）。
        /// </summary>
        [SerializeField] private bool _isActive;

        /// <summary>
        /// 本层尺寸乘数（1 表示跟随全局 layerSizeCurve，可用 sizeOverride 覆盖）。
        /// </summary>
        [SerializeField] private float _layerSize = 1f;

        /// <summary>
        /// 本层方向（>0 顺时针，<0 逆时针）。
        /// </summary>
        [SerializeField] private float _layerDirection = 1f;

        /// <summary>
        /// 本层起始角错开偏移（度）。
        /// </summary>
        [SerializeField] private float _staggerOffset;

        /// <summary>
        /// 本层层 index（0 为最上层/首层）。
        /// </summary>
        [SerializeField] private int _layerIndex;

        /// <summary>
        /// 本层占据的槽位数量（= cell 数量）。
        /// </summary>
        [SerializeField] private int _baseSlotCount;

        // ==================== 运行时缓存 ====================

        /// <summary>
        /// 本层 cell 组件缓存（与布局 ActiveCells 数量对齐时视为有效）。
        /// </summary>
        private readonly List<RingCell> _cells = new List<RingCell>();

        /// <summary>
        /// 本层 CanvasGroup，用于应用 alpha 衰减。
        /// </summary>
        private CanvasGroup _canvasGroup;

        // ==================== Unity 生命周期 ====================

        /// <summary>
        /// 缓存或补挂 CanvasGroup，供 SetVisual 应用层 alpha。
        /// </summary>
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // ==================== 初始化 ====================

        /// <summary>
        /// 由生成器注入本层配置与布局引擎引用。
        /// </summary>
        public void Init(UiRingLayout layout, int layerIndex, int baseSlotCount, float layerSize, float layerDirection, float staggerOffset)
        {
            _layout = layout;
            _layerIndex = layerIndex;
            _baseSlotCount = baseSlotCount;
            _layerSize = layerSize;
            _layerDirection = layerDirection;
            _staggerOffset = staggerOffset;
        }

        // ==================== 视觉刷新 ====================

        /// <summary>
        /// 每帧刷新本层视觉：scale = layerSizeCurve.Evaluate(layerIndex) * layerSize（随 index 递减）；
        /// alpha = alphaByDistanceCurve.Evaluate(|t - layerIndex|)（焦点层最高，距离增大衰减）；
        /// 激活态绑定 activeLayerIndex（本层 index 相等时显示 Enable）。
        /// </summary>
        public void SetVisual(float t, int activeLayerIndex, AnimationCurve layerSizeCurve, AnimationCurve alphaByDistanceCurve)
        {
            ensureCellCache();
            float scale = layerSizeCurve.Evaluate(_layerIndex) * _layerSize;
            transform.localScale = new Vector3(scale, scale, 1f);
            float distance = Mathf.Abs(t - _layerIndex);
            float alpha = alphaByDistanceCurve.Evaluate(distance);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
            SetEnable(_layerIndex == activeLayerIndex);
        }

        /// <summary>
        /// 切换本层激活态：激活时遍历 cell 调 <see cref="RingCell.SetEnable"/> 切到 Enable 贴图。
        /// </summary>
        public void SetEnable(bool active)
        {
            ensureCellCache();
            if (_isActive == active)
            {
                return;
            }
            _isActive = active;
            for (int i = 0; i < _cells.Count; i++)
            {
                _cells[i].SetEnable(active);
            }
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 与布局 ActiveCells 数量不一致时重建 cell 缓存，并回填 layerIndex/cellIndex。
        /// </summary>
        private void ensureCellCache()
        {
            if (_layout == null)
            {
                return;
            }
            if (_cells.Count == _layout.ActiveCells.Count)
            {
                return;
            }
            _cells.Clear();
            for (int i = 0; i < _layout.ActiveCells.Count; i++)
            {
                if (_layout.ActiveCells[i].TryGetComponent<RingCell>(out RingCell cell))
                {
                    cell.FillIndex(_layerIndex, i);
                    _cells.Add(cell);
                }
            }
        }

        // ==================== 属性 ====================

        /// <summary>本层布局引擎。</summary>
        public UiRingLayout Layout => _layout;

        /// <summary>本层是否激活。</summary>
        public bool IsActive => _isActive;

        /// <summary>本层层 index。</summary>
        public int LayerIndex => _layerIndex;

        /// <summary>本层占据的槽位数量。</summary>
        public int BaseSlotCount => _baseSlotCount;

        /// <summary>本层尺寸乘数。</summary>
        public float LayerSize { get => _layerSize; set => _layerSize = value; }

        /// <summary>本层方向（>0 顺时针，<0 逆时针）。</summary>
        public float LayerDirection { get => _layerDirection; set => _layerDirection = value; }

        /// <summary>本层起始角错开偏移（度）。</summary>
        public float StaggerOffset { get => _staggerOffset; set => _staggerOffset = value; }
    }
}

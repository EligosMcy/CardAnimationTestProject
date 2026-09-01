using System.Collections.Generic;
using ShowX.Utils;
using UnityEngine;

namespace MapTest
{
    /// <summary>
    /// 环形地图单层组件：持有 RingConfigSO 与 UiRingLayout，负责生成/销毁本层 cell、
    /// 应用布局（位置/尺寸/旋转）、回填索引，并控制本层激活态与等比缩放。
    /// SetVisual 每帧由整图控制器驱动，按等比模型（ratio^(layerIndex - scaleOffset)）计算 scale，不透明显示。
    /// </summary>
    public class RingLayer : MonoBehaviour
    {
        // ==================== 常量 ====================

        /// <summary>等比比例有效下限（防止 ratio ≤ 0 时 Pow 产生 NaN）。</summary>
        private const float MIN_VALID_RATIO = 0.01f;

        /// <summary>满环槽位数量默认值（Init 注入前兜底）。</summary>
        private const int DEFAULT_MAX_SLOTS = 8;

        // ==================== 字段 ====================

        /// <summary>
        /// 圆环配置（唯一几何数据源），供 UiRingLayout 计算与 cell 实例化使用。
        /// </summary>
        [SerializeField] private RingConfigSO _config;

        /// <summary>
        /// 本层包裹的布局计算器（读取 _config，输出 cell 位置与旋转）。
        /// </summary>
        [SerializeField] private UiRingLayout _layout;

        /// <summary>
        /// 本层是否激活（激活层 cell 显示 Enable 贴图）。
        /// </summary>
        [SerializeField] private bool _isActive;

        /// <summary>
        /// 本层尺寸乘数（1 表示跟随全局等比缩放，可用 sizeOverride 覆盖）。
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
        /// 本层满环槽位数量（由生成器从 RingMapConfigSO 注入，用于钳制 cell 数量并下传 UiRingLayout 计算角度步进）。
        /// </summary>
        [SerializeField] private int _maxSlots = DEFAULT_MAX_SLOTS;

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
        /// 本层 cell 的 RectTransform 缓存，与 _baseSlotCount 对齐。
        /// </summary>
        private readonly List<RectTransform> _cells = new List<RectTransform>();

        /// <summary>
        /// 本层 CanvasGroup，用于强制不透明显示（alpha 恒 1）。
        /// </summary>
        private CanvasGroup _canvasGroup;

        // ==================== Unity 生命周期 ====================

        /// <summary>
        /// 缓存或补挂 CanvasGroup，并把本层配置输入给布局计算器。
        /// </summary>
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            if (_layout != null)
            {
                _layout.Config = _config;
            }
            else
            {
                XLogger.LogError("RingLayer", "Awake: _layout 为空，请检查预制体引用");
            }
        }

        /// <summary>
        /// 每帧确保 cell 数量并刷新布局；数量变化时整体重建（暂不做对象池，直接销毁重建）。
        /// </summary>
        private void Update()
        {
            if (_config == null || _layout == null)
            {
                return;
            }
            if (_cells.Count != _baseSlotCount)
            {
                rebuildCells();
                return;
            }
            layoutCells();
        }

        // ==================== 初始化 ====================

        /// <summary>
        /// 由生成器注入本层配置：层序号、cell 数量、尺寸乘数、方向与错开偏移。
        /// </summary>
        public void Init(int layerIndex, int baseSlotCount, float layerSize, float layerDirection, float staggerOffset, int maxSlots)
        {
            _layerIndex = layerIndex;
            _baseSlotCount = baseSlotCount;
            _layerSize = layerSize;
            _layerDirection = layerDirection;
            _staggerOffset = staggerOffset;
            _maxSlots = maxSlots;
            pushLayoutParams();
        }

        // ==================== 视觉刷新 ====================

        /// <summary>
        /// 每帧刷新本层视觉：scale = ratio^(layerIndex - scaleOffset) * layerSize（等比模型，offset 前进全体层等比放大一级）；
        /// alpha 恒为 1（不透明显示），激活态绑定 activeLayerIndex（本层 index 相等时显示 Enable）。
        /// </summary>
        public void SetVisual(float scaleOffset, int activeLayerIndex, float ratio)
        {
            float safeRatio = Mathf.Max(MIN_VALID_RATIO, ratio);
            float scale = Mathf.Pow(safeRatio, _layerIndex - scaleOffset) * _layerSize;
            transform.localScale = new Vector3(scale, scale, 1f);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
            SetEnable(_layerIndex == activeLayerIndex);
        }

        /// <summary>
        /// 切换本层激活态：激活时遍历 cell 调 <see cref="RingCell.SetEnable"/> 切到 Enable 贴图。
        /// </summary>
        public void SetEnable(bool active)
        {
            if (_isActive == active)
            {
                return;
            }
            _isActive = active;
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i].TryGetComponent<RingCell>(out RingCell cell))
                {
                    cell.SetEnable(active);
                }
            }
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 把本层槽位/方向参数下传给布局计算器（MaxSlots 与方向已从 RingConfigSO 迁入 RingMapConfigSO，经本层中转给 UiRingLayout）。
        /// </summary>
        private void pushLayoutParams()
        {
            if (_layout == null)
            {
                return;
            }
            _layout.MaxSlots = _maxSlots;
            _layout.IsClockwise = _layerDirection >= 0f;
        }

        /// <summary>
        /// 数量变化时整体重建 cell：销毁全部旧 cell 后按配置实例化并布局（暂不做对象池），
        /// 并同步激活态（cell 实例化时初始化为禁用态）。
        /// </summary>
        private void rebuildCells()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i] != null)
                {
                    Destroy(_cells[i].gameObject);
                }
            }
            _cells.Clear();
            int safeMaxSlots = Mathf.Max(1, _maxSlots);
            int targetCount = Mathf.Clamp(_baseSlotCount, 0, safeMaxSlots);
            if (targetCount != _baseSlotCount)
            {
                XLogger.LogWarning("RingLayer", $"rebuildCells: count={_baseSlotCount} 越界，已钳制到 [0, {safeMaxSlots}]");
                _baseSlotCount = targetCount;
            }
            for (int i = 0; i < targetCount; i++)
            {
                RectTransform cell = instantiateCell();
                if (cell == null)
                {
                    break;
                }
                _cells.Add(cell);
            }
            layoutCells();
            syncCellsEnable();
        }

        /// <summary>
        /// 把全部 cell 的激活态同步为当前层激活态（cell 新建时初始化为禁用态，
        /// 避免激活态早于 cell 生成应用后被提前返回跳过，导致贴图停留在 Disable）。
        /// </summary>
        private void syncCellsEnable()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i].TryGetComponent<RingCell>(out RingCell cell))
                {
                    cell.SetEnable(_isActive);
                }
            }
        }

        /// <summary>
        /// 实例化单个 cell（预制体来自配置），并回填层/序号索引。
        /// </summary>
        private RectTransform instantiateCell()
        {
            if (_config.CellPrefab == null)
            {
                XLogger.LogError("RingLayer", "instantiateCell: 配置未指定 cell 预制体");
                return null;
            }
            GameObject go = Instantiate(_config.CellPrefab, transform);
            if (!go.TryGetComponent<RectTransform>(out RectTransform cell))
            {
                XLogger.LogError("RingLayer", "instantiateCell: 预制体缺少 RectTransform");
                Destroy(go);
                return null;
            }
            if (cell.TryGetComponent<RingCell>(out RingCell ringCell))
            {
                ringCell.FillIndex(_layerIndex, _cells.Count);
            }
            return cell;
        }

        /// <summary>
        /// 刷新全部 cell 布局：位置/旋转来自布局计算器，尺寸来自配置。
        /// </summary>
        private void layoutCells()
        {
            int total = _cells.Count;
            for (int i = 0; i < total; i++)
            {
                RectTransform cell = _cells[i];
                CellPlacement placement = _layout.GetCellPlacement(i, _staggerOffset);
                cell.anchoredPosition = placement.Position;
                cell.localEulerAngles = new Vector3(0f, 0f, placement.AngleDeg);
                cell.sizeDelta = new Vector2(_config.CellWidth, _config.CellHeight);
            }
        }

        // ==================== 属性 ====================

        /// <summary>本层布局计算器。</summary>
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

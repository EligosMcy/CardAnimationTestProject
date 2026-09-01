using System.Collections.Generic;
using ShowX.Utils;
using UnityEditor;
using UnityEngine;

namespace MapTest
{
    /// <summary>
    /// 运行时 UI 圆环布局器。
    /// 将预制体部件按数量沿圆周排列，Update 中实时更新位置/旋转/尺寸；
    /// 仅在数量变化时实例化或回收部件，未使用部件缓存不销毁，便于后续增减。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UiRingLayout : MonoBehaviour
    {
        // ==================== 常量 ====================

        /// <summary>整圆角度（度）。</summary>
        private const float FULL_CIRCLE_ANGLE = 360f;

        /// <summary>角度方向：顺时针为 1。</summary>
        private const int CLOCKWISE_DIRECTION = 1;

        /// <summary>角度方向：逆时针为 -1。</summary>
        private const int COUNTER_CLOCKWISE_DIRECTION = -1;

        // ==================== 配置 ====================

        [Header("配置")]

        /// <summary>圆环配置文件，赋值后 OnEnable 时自动加载其参数。</summary>
        [SerializeField] private RingConfigSO _config;

        /// <summary>圆环上每个部件的预制体（需含 RectTransform）。</summary>
        [SerializeField] private GameObject _cellPrefab;

        // ==================== 运行时可调参数（从 config 拷贝，调试后可写回） ====================

        [Header("运行时参数")]

        /// <summary>圆环半径。</summary>
        [SerializeField] private float _radius = 200f;

        /// <summary>部件数量。</summary>
        [SerializeField] private int _count = 8;

        /// <summary>部件宽度。</summary>
        [SerializeField] private float _cellWidth = 100f;

        /// <summary>部件高度。</summary>
        [SerializeField] private float _cellHeight = 100f;

        /// <summary>起始角度偏移（度），0 朝 12 点。</summary>
        [SerializeField] private float _startAngleOffset = 0f;

        /// <summary>是否顺时针排列。</summary>
        [SerializeField] private bool _isClockwise = true;

        /// <summary>固定槽位数量（默认 8，angleStep = 360 / maxSlots）。</summary>
        [SerializeField] private int _maxSlots = 8;

        /// <summary>起始槽位：count 个 cell 从该槽起连续占据相邻槽位。</summary>
        [SerializeField] private int _startSlot = 0;

        // ==================== 本地对象池 ====================

        /// <summary>当前圆环上激活的部件。</summary>
        private readonly List<RectTransform> _activeCells = new List<RectTransform>();

        /// <summary>数量减少时回收的部件缓存，供后续增加时复用。</summary>
        private readonly List<RectTransform> _cachedCells = new List<RectTransform>();

        /// <summary>与 <see cref="_activeCells"/> 一一对应的 RingCell 缓存，避免 Update 中查找组件。</summary>
        private readonly List<RingCell> _cellComponents = new List<RingCell>();

        // ==================== 缓存引用 ====================

        /// <summary>本组件的 RectTransform，圆心为其 pivot。</summary>
        private RectTransform _rectTransform;

        // ==================== Unity 生命周期 ====================

        /// <summary>
        /// 缓存自身 RectTransform。
        /// </summary>
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 若有配置则加载其参数，随后同步数量并布局一次。
        /// </summary>
        private void OnEnable()
        {
            if (_config != null)
            {
                loadFromConfig();
            }
            syncCount();
            layoutAll();
        }

        /// <summary>
        /// 每帧检查数量是否变化并同步，随后重新布局所有激活部件。
        /// </summary>
        private void Update()
        {
            if (_activeCells.Count != _count)
            {
                syncCount();
            }
            layoutAll();
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 从配置文件拷贝参数到运行时字段。
        /// </summary>
        private void loadFromConfig()
        {
            _radius = _config.Radius;
            _count = _config.Count;
            _cellWidth = _config.CellWidth;
            _cellHeight = _config.CellHeight;
            _startAngleOffset = _config.StartAngleOffset;
            _isClockwise = _config.IsClockwise;
        }

        /// <summary>
        /// 同步激活部件数量：不足则实例化/取缓存，多余则回收缓存。
        /// count 越界时钳制到 [0, maxSlots] 并输出警告。
        /// </summary>
        private void syncCount()
        {
            int safeMaxSlots = Mathf.Max(1, _maxSlots);
            int targetCount = Mathf.Clamp(_count, 0, safeMaxSlots);
            if (targetCount != _count)
            {
                XLogger.LogWarning("UiRingLayout", $"syncCount: count={_count} 越界，已钳制到 [0, {safeMaxSlots}]");
                _count = targetCount;
            }
            while (_activeCells.Count < targetCount)
            {
                RectTransform cell = acquireCell();
                if (cell == null)
                {
                    break;
                }
                _activeCells.Add(cell);
                cell.TryGetComponent<RingCell>(out RingCell ringCell);
                _cellComponents.Add(ringCell);
            }
            while (_activeCells.Count > targetCount)
            {
                int lastIndex = _activeCells.Count - 1;
                RectTransform cell = _activeCells[lastIndex];
                _activeCells.RemoveAt(lastIndex);
                _cellComponents.RemoveAt(lastIndex);
                recycleCell(cell);
            }
        }

        /// <summary>
        /// 获取一个部件：优先从缓存取，否则实例化预制体。
        /// </summary>
        private RectTransform acquireCell()
        {
            if (_cellPrefab == null)
            {
                Debug.LogError("[UiRingLayout] acquireCell: _cellPrefab 为空");
                return null;
            }
            RectTransform cell;
            if (_cachedCells.Count > 0)
            {
                int lastIndex = _cachedCells.Count - 1;
                cell = _cachedCells[lastIndex];
                _cachedCells.RemoveAt(lastIndex);
            }
            else
            {
                cell = instantiateCell();
                if (cell == null)
                {
                    return null;
                }
            }
            cell.gameObject.SetActive(true);
            return cell;
        }

        /// <summary>
        /// 实例化预制体并取其 RectTransform。
        /// </summary>
        private RectTransform instantiateCell()
        {
            GameObject go = Instantiate(_cellPrefab, _rectTransform);
            if (!go.TryGetComponent<RectTransform>(out RectTransform cell))
            {
                Debug.LogError("[UiRingLayout] instantiateCell: 预制体缺少 RectTransform");
                return null;
            }
            return cell;
        }

        /// <summary>
        /// 回收部件：停用并放入缓存，不销毁。
        /// </summary>
        private void recycleCell(RectTransform cell)
        {
            if (cell == null)
            {
                return;
            }
            cell.gameObject.SetActive(false);
            _cachedCells.Add(cell);
        }

        /// <summary>
        /// 重新布局所有激活部件的位置、旋转、尺寸。
        /// 使用固定 8 槽 45° 网格（angleStep = 360 / maxSlots），count 个 cell 从起始槽连续占据相邻槽位。
        /// </summary>
        private void layoutAll()
        {
            int total = _activeCells.Count;
            if (total == 0)
            {
                return;
            }
            float angleStep = FULL_CIRCLE_ANGLE / Mathf.Max(1, _maxSlots);
            int direction = _isClockwise ? CLOCKWISE_DIRECTION : COUNTER_CLOCKWISE_DIRECTION;
            for (int i = 0; i < total; i++)
            {
                layoutCell(i, angleStep, direction);
            }
        }

        /// <summary>
        /// 计算并应用单个部件的位置、旋转、尺寸。
        /// cell 占据槽位 (startSlot + index) mod maxSlots；挂有 RingCell 时尺寸/旋转交给 RingCell 自身。
        /// </summary>
        private void layoutCell(int index, float angleStep, int direction)
        {
            RectTransform cell = _activeCells[index];
            int slot = (_startSlot + index) % Mathf.Max(1, _maxSlots);
            // 角度从 12 点起算，槽位沿 direction 方向走
            float angleDeg = _startAngleOffset + direction * slot * angleStep;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            // sin/cos 使 slot 0 落在正上方（12 点）
            float posX = _radius * Mathf.Sin(angleRad);
            float posY = _radius * Mathf.Cos(angleRad);
            cell.anchoredPosition = new Vector2(posX, posY);
            RingCell ringCell = _cellComponents[index];
            if (ringCell != null)
            {
                // RingCell 自带尺寸与方向，布局只负责定位
                ringCell.ApplySizeAndDirection();
            }
            else
            {
                // 预制体默认朝上，旋转 -angleDeg 使其顺时针贴合对应角度
                cell.localEulerAngles = new Vector3(0f, 0f, -angleDeg);
                cell.sizeDelta = new Vector2(_cellWidth, _cellHeight);
            }
        }

        // ==================== 运行时配置（供生成器注入） ====================

        /// <summary>cell 预制体。</summary>
        public GameObject CellPrefab { get => _cellPrefab; set => _cellPrefab = value; }

        /// <summary>圆环半径。</summary>
        public float Radius { get => _radius; set => _radius = value; }

        /// <summary>部件数量。</summary>
        public int Count { get => _count; set => _count = value; }

        /// <summary>部件宽度。</summary>
        public float CellWidth { get => _cellWidth; set => _cellWidth = value; }

        /// <summary>部件高度。</summary>
        public float CellHeight { get => _cellHeight; set => _cellHeight = value; }

        /// <summary>起始角度偏移（度）。</summary>
        public float StartAngleOffset { get => _startAngleOffset; set => _startAngleOffset = value; }

        /// <summary>固定槽位数量。</summary>
        public int MaxSlots { get => _maxSlots; set => _maxSlots = value; }

        /// <summary>起始槽位。</summary>
        public int StartSlot { get => _startSlot; set => _startSlot = value; }

        /// <summary>是否顺时针排列。</summary>
        public bool IsClockwise { get => _isClockwise; set => _isClockwise = value; }

        /// <summary>当前激活的 cell 列表（只读，供 RingLayer 使用）。</summary>
        public IReadOnlyList<RectTransform> ActiveCells => _activeCells;

        // ==================== 编辑器工具 ====================

#if UNITY_EDITOR
        /// <summary>
        /// 将当前运行时参数写回配置文件并保存。
        /// </summary>
        [Button("保存到配置文件")]
        private void saveToConfig()
        {
            if (_config == null)
            {
                Debug.LogError("[UiRingLayout] saveToConfig: _config 为空，无法保存");
                return;
            }
            _config.Radius = _radius;
            _config.Count = _count;
            _config.CellWidth = _cellWidth;
            _config.CellHeight = _cellHeight;
            _config.StartAngleOffset = _startAngleOffset;
            _config.IsClockwise = _isClockwise;
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            Debug.Log("[UiRingLayout] saveToConfig: 已写入配置文件");
        }
#endif
    }
}

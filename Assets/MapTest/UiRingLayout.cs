using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tools.UI.Ring
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

        // ==================== 本地对象池 ====================

        /// <summary>当前圆环上激活的部件。</summary>
        private readonly List<RectTransform> _activeCells = new List<RectTransform>();

        /// <summary>数量减少时回收的部件缓存，供后续增加时复用。</summary>
        private readonly List<RectTransform> _cachedCells = new List<RectTransform>();

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
        /// </summary>
        private void syncCount()
        {
            int targetCount = Mathf.Max(0, _count);
            while (_activeCells.Count < targetCount)
            {
                RectTransform cell = acquireCell();
                if (cell == null)
                {
                    break;
                }
                _activeCells.Add(cell);
            }
            while (_activeCells.Count > targetCount)
            {
                int lastIndex = _activeCells.Count - 1;
                RectTransform cell = _activeCells[lastIndex];
                _activeCells.RemoveAt(lastIndex);
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
        /// </summary>
        private void layoutAll()
        {
            int total = _activeCells.Count;
            if (total == 0)
            {
                return;
            }
            float angleStep = FULL_CIRCLE_ANGLE / total;
            int direction = _isClockwise ? CLOCKWISE_DIRECTION : COUNTER_CLOCKWISE_DIRECTION;
            for (int i = 0; i < total; i++)
            {
                RectTransform cell = _activeCells[i];
                layoutCell(cell, i, angleStep, direction);
            }
        }

        /// <summary>
        /// 计算并应用单个部件的位置、旋转、尺寸。
        /// </summary>
        private void layoutCell(RectTransform cell, int index, float angleStep, int direction)
        {
            // 角度从 12 点起算，i 增大沿 direction 方向走
            float angleDeg = _startAngleOffset + direction * index * angleStep;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            // sin/cos 使 i=0 落在正上方（12 点）
            float posX = _radius * Mathf.Sin(angleRad);
            float posY = _radius * Mathf.Cos(angleRad);
            cell.anchoredPosition = new Vector2(posX, posY);
            // 预制体默认朝上，旋转 -angleDeg 使其顺时针贴合对应角度
            cell.localEulerAngles = new Vector3(0f, 0f, -angleDeg);
            cell.sizeDelta = new Vector2(_cellWidth, _cellHeight);
        }

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

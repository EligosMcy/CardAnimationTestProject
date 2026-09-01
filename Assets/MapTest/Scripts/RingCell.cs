using ShowX.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MapTest
{
    /// <summary>
    /// 环形地图 cell 组件：挂载于 MapCell 预制体根节点。
    /// 需 Button（点击上报）、Image（渲染双态贴图）、cellSize 与 direction（ApplySizeAndDirection 应用）；
    /// layerIndex/cellIndex 由生成时回填，供事件上报使用。cell 只"喊"不持上游引用。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RingCell : MonoBehaviour
    {
        // ==================== 字段 ====================

        /// <summary>
        /// Button 组件，点击时上报事件总线。
        /// </summary>
        [SerializeField] private Button _button;

        /// <summary>
        /// 渲染贴图的 Image（通常为子物体，拉伸铺满根节点）。
        /// </summary>
        [SerializeField] private Image _image;

        /// <summary>
        /// 启用态贴图（所属层为激活层时显示）。
        /// </summary>
        [SerializeField] private Sprite _enableSprite;

        /// <summary>
        /// 禁用态贴图（所属层非激活层时显示）。
        /// </summary>
        [SerializeField] private Sprite _disableSprite;

        /// <summary>
        /// cell 尺寸（正方形边长，应用 sizeDelta）。
        /// </summary>
        [SerializeField] private float _cellSize = 100f;

        /// <summary>
        /// 方向（1=顺时针，-1=逆时针），由生成时注入。
        /// </summary>
        [SerializeField] private int _direction = 1;

        /// <summary>
        /// 所属层 index，生成时回填。
        /// </summary>
        [SerializeField] private int _layerIndex;

        /// <summary>
        /// 层内 cell index，生成时回填。
        /// </summary>
        [SerializeField] private int _cellIndex;

        // ==================== Unity 生命周期 ====================

        /// <summary>
        /// 缓存 Button 并挂接点击回调。
        /// </summary>
        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(onCellClicked);
            }
            else
            {
                XLogger.LogError("RingCell", "Awake: 缺少 Button 组件");
            }
        }

        // ==================== 公开方法 ====================

        /// <summary>
        /// 切换双态贴图：active 时显示 Enable 贴图，否则显示 Disable 贴图。
        /// </summary>
        public void SetEnable(bool active)
        {
            if (_image == null)
            {
                XLogger.LogError("RingCell", "SetEnable: _image 为空，请检查预制体引用");
                return;
            }
            _image.sprite = active ? _enableSprite : _disableSprite;
        }

        /// <summary>
        /// 应用尺寸与方向：将 sizeDelta 设为 (cellSize, cellSize)，localEulerAngles 保持竖直（0,0,0）。
        /// </summary>
        public void ApplySizeAndDirection()
        {
            RectTransform rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(_cellSize, _cellSize);
            rect.localEulerAngles = new Vector3(0f, 0f, 0f);
        }

        /// <summary>
        /// 回填所属层与层内序号（生成时调用）。
        /// </summary>
        public void FillIndex(int layerIndex, int cellIndex)
        {
            _layerIndex = layerIndex;
            _cellIndex = cellIndex;
        }

        // ==================== 属性 ====================

        /// <summary>cell 尺寸（正方形边长）。</summary>
        public float CellSize { get => _cellSize; set => _cellSize = value; }

        /// <summary>方向（1 顺时针 / -1 逆时针）。</summary>
        public int Direction { get => _direction; set => _direction = value; }

        /// <summary>所属层 index。</summary>
        public int LayerIndex => _layerIndex;

        /// <summary>层内 cell index。</summary>
        public int CellIndex => _cellIndex;

        // ==================== 私有方法 ====================

        /// <summary>
        /// Button 点击回调：经事件总线上报 (layerIndex, cellIndex)，不持上游引用。
        /// </summary>
        private void onCellClicked()
        {
            RingMapEvents.RaiseCellClicked(_layerIndex, _cellIndex);
        }
    }
}

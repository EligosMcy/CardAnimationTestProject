using ShowX.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MapTest
{
    /// <summary>
    /// 环形地图 cell 组件：挂载于 RingCell 预制体根节点。
    /// 仅保留层/序号身份、双态贴图切换与可点击态控制，几何（位置/尺寸/旋转）由 RingLayer 经 UiRingLayout 外部设置，
    /// cell 自身不存储任何几何数据。layerIndex/cellIndex 由生成时回填，供事件上报使用。cell 只"喊"不持上游引用。
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
        /// 所属层 index，生成时回填。
        /// </summary>
        [SerializeField] private int _layerIndex;

        /// <summary>
        /// 层内 cell index，生成时回填。
        /// </summary>
        [SerializeField] private int _cellIndex;

        // ==================== Unity 生命周期 ====================

        /// <summary>
        /// 缓存 Button、挂接点击回调，并将贴图与可点击态初始化为禁用态。
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
            SetEnable(false);
        }

        // ==================== 公开方法 ====================

        /// <summary>
        /// 切换激活态：active 时显示 Enable 贴图且 Button 可点击，否则显示 Disable 贴图且不可点击。
        /// </summary>
        public void SetEnable(bool active)
        {
            if (_button == null)
            {
                XLogger.LogError("RingCell", "SetEnable: _button 为空，请检查预制体引用");
                return;
            }
            if (_image == null)
            {
                XLogger.LogError("RingCell", "SetEnable: _image 为空，请检查预制体引用");
                return;
            }
            _button.interactable = active;
            _image.sprite = active ? _enableSprite : _disableSprite;
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

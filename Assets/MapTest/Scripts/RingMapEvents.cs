using System;

namespace MapTest
{
    /// <summary>
    /// 环形地图全局事件总线，解耦 cell 与上游控制器。
    /// cell 点击后仅上报 (layerIndex, cellIndex)，由订阅方统一处理。
    /// 外部通过 <see cref="RaiseCellClicked"/> 触发，事件本身仅在本类内读写。
    /// </summary>
    public static class RingMapEvents
    {
        /// <summary>
        /// cell 被点击事件，参数依次为 layerIndex、cellIndex。
        /// </summary>
        public static event Action<int, int> OnCellClicked;

        /// <summary>
        /// 上报 cell 点击：对订阅者判空后逐一调用（事件委托只能在声明类内读取）。
        /// </summary>
        public static void RaiseCellClicked(int layerIndex, int cellIndex)
        {
            Action<int, int> handler = OnCellClicked;
            if (handler == null)
            {
                return;
            }
            handler(layerIndex, cellIndex);
        }
    }
}

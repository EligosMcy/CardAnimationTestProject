## Why

`Assets/MapTest/` 目前只有单环布局原型（`RingConfigSO` + `UiRingLayout` + 两个 Enable/Disable 贴图），能在一个圆环上均分排列 cell，但无法构成完整地图：缺少多层配置、层间错开、纵深透视、cell 双态切换、整图控制器与推进交互。需要把它扩展为一套可配置的多层环形地图生成器，支撑"爬塔式"逐层推进玩法。

## What Changes

- 新增多层地图配置 SO：按层列出每层 cell 数量（满环为 8），并带层间错开范围、纵深/缩放曲线、`requireLongPress` 等参数。
- 改造 `UiRingLayout`：由"count 均分 360°"改为"8 槽固定 45° 网格 + 连续相邻填充"——count<8 时 cell 从起点连续占相邻槽位，保持跨层共同角度参照系。
- 新增 `RingCell` 组件（每个 cell）：为 Button 可点击，点击经全局事件总线（静态 `RingMapEvents.OnCellClicked`）上报到 `RingMapGenerator` 统一处理（先 Log 层数+序号）；可设大小与方向，持有 Enable/Disable 两张贴图，当所属层为激活层时显示 Enable。
- 新增 `RingLayer` 组件（每层）：包裹一个 `UiRingLayout`，控制本层是否激活、本层大小、本层方向、层间错开偏移。
- 新增 `RingMapGenerator` 整图控制器：从配置实例化各层、维护当前激活层索引、订阅事件总线处理 cell 点击、驱动纵深进度。
- 新增 `RingMapInteraction` 交互控制（基于 InputSystem）：长按/单击左键进入下一层、长按/单击右键退回上一层（两端无效：最上层右键无效、最底层左键无效）；`bool requireLongPress` 控制是否需长按（false 时单击直接进/退）；鼠标滚轮仅在 [激活层, 末层] 范围内浏览预览（不改激活层）；进/退与浏览时整图 scale 渐变。
- 升级 `MapCell` 预制体：挂载 `RingCell` + Button，接入两张贴图字段。
- 向下纵深：首层最大，向下层 index 递增则层尺寸递减，alpha 随到激活层距离衰减；不引入世界空间 3D。
- 层间错开允许部分重叠，只要不完全对齐以显自然；错开量在配置范围内。
- 推进边界：滚轮浏览范围为 [激活层, 末层]；左/右键可双向改变激活层（最上层不可退、最底层不可进），即允许回退。

## Capabilities

### New Capabilities
- `ring-map`: 多层环形地图的生成、布局、向下纵深表现与逐层推进交互。涵盖多层配置、8 槽固定网格布局、cell 双态、层控制、整图控制器、左/右键进退与滚轮浏览、cell 点击事件总线。

### Modified Capabilities
<!-- 无既有 spec，openspec/specs/ 不存在 -->

## Impact

- 新增代码（`Assets/MapTest/`，命名空间 `Tools.UI.Ring`）：`RingMapConfigSO`、`RingCell`、`RingLayer`、`RingMapGenerator`、`RingMapInteraction`、`RingMapEvents`（静态事件总线）。
- 修改代码：`UiRingLayout.cs`（8 槽网格与连续相邻填充逻辑）、`MapCell.prefab`（挂载 `RingCell` + Button 与贴图字段）。
- 复用：`RingConfigSO` 的配置模式（扩展为多层）、`XUtils` 的 `[Button]` 与 `XLogger`、现有对象池模式、**项目已有 InputSystem**（无新包）。
- 无破坏性变更：先前误报的 `Assets/Scripts` 节点式地图系统经核查为 CodeGraph 索引过期产物，磁盘不存在，无冲突。
- 无新依赖、无新 Unity 包。

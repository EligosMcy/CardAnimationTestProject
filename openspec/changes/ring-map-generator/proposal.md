## Why

`Assets/MapTest/` 目前只有单环布局原型（`RingConfigSO` + `UiRingLayout` + 两个 Enable/Disable 贴图），能在一个圆环上均分排列 cell，但无法构成完整地图：缺少多层配置、层间错开、纵深透视、cell 双态切换、整图控制器与推进交互。需要把它扩展为一套可配置的多层环形地图生成器，支撑"爬塔式"逐层推进玩法。

## What Changes

- 新增多层地图配置 SO：按层**仅**设置每层 cell 数量（满环为 8），其余参数（层间错开、层大小、层方向、cell 显示）自动计算或来自 `RingLayer` 预制体；并带全局错开范围、纵深/缩放曲线等参数。
- 改造 `UiRingLayout`：由"count 均分 360°"改为"8 槽固定 45° 网格 + 连续相邻填充"——count<8 时 cell 从起点连续占相邻槽位，保持跨层共同角度参照系。
- 新增 `RingLayer` 预制体与组件（每层）：预制体内置 `UiRingLayout`（cellPrefab 指向 `MapCell` 预制体）与 Enable/Disable 贴图引用，保证每层基础显示效果完全一致；组件控制本层是否激活、本层大小、本层方向、层间错开偏移（运行时覆盖）。
- 新增 `RingMapGenerator` 整图控制器：按配置**实例化 `RingLayer` 预制体**创建各层（不直接创建 cell），维护当前激活层索引、订阅事件总线处理 cell 点击、驱动纵深进度。
- 新增 `RingCell` 组件（挂在 `RingLayer` 预制体内的 cell 上）：为 Button 可点击，点击经全局事件总线（静态 `RingMapEvents.OnCellClicked`）上报到 `RingMapGenerator` 统一处理（先 Log 层数+序号）；持有 Enable/Disable 两张贴图，当所属层为激活层时显示 Enable。
- 新增 `RingMapInteraction` 交互控制（基于 **InputActionAsset**，**全部短按**）：左键短按进入下一层、右键短按退回上一层（两端无效：最上层右键无效、最底层左键无效）；鼠标滚轮前进=向深层、后退=向表层，在 [激活层, 末层] 内浏览显示层（不改激活层）；进/退与浏览时整图 scale 渐变；无长按、无进度条。
- 新增两个 TextMeshPro 指示：**方向指示**（显示当前执行的向下/向上）与**状态指示**（显示当前激活层数与当前显示层数）。
- 升级 `MapCell` 预制体：挂载 `RingCell` + Button，接入两张贴图字段，作为 `RingLayer` 预制体的 cell 子预制体。
- 向下纵深：首层最大，向下层 index 递增则层尺寸递减，alpha 随到激活层距离衰减；不引入世界空间 3D。
- 层间错开允许部分重叠，只要不完全对齐以显自然；错开量由全局范围自动计算。
- 推进边界：滚轮浏览范围为 [激活层, 末层]；左/右键可双向改变激活层（最上层不可退、最底层不可进），即允许回退。

## Capabilities

### New Capabilities
- `ring-map`: 多层环形地图的生成、布局、向下纵深表现与逐层推进交互。涵盖多层配置（每层仅数量）、8 槽固定网格布局、cell 双态、层预制体控制、整图控制器、InputActionAsset 左/右键短按与滚轮浏览、cell 点击事件总线、TextMeshPro 方向与层数指示。

### Modified Capabilities
<!-- 无既有 spec，openspec/specs/ 不存在 -->

## Impact

- 新增代码（`Assets/MapTest/`，命名空间 `Tools.UI.Ring`）：`RingMapConfigSO`、`RingCell`、`RingLayer`、`RingMapGenerator`、`RingMapInteraction`、`RingMapEvents`（静态事件总线）。
- 新增资产：`RingMapControls.inputactions`（InputActionAsset：`LeftClick`/`RightClick`/`Wheel`）、`RingLayer.prefab`（内含 `UiRingLayout` + `MapCell` 预制体实例 + 双态贴图引用）。
- 修改代码：`UiRingLayout.cs`（8 槽网格与连续相邻填充逻辑）、`MapCell.prefab`（挂载 `RingCell` + Button 与贴图字段）。
- 场景内新增：两个 TextMeshProUGUI（方向指示、激活/显示层数状态指示）。
- 移除：长按进度条机制与 `requireLongPress`/`holdDuration` 配置。
- 复用：`RingConfigSO` 的配置模式、`XUtils` 的 `[Button]` 与 `XLogger`、现有对象池模式、**项目已有 InputSystem**（无新包）。
- 无破坏性变更：先前误报的 `Assets/Scripts` 节点式地图系统经核查为 CodeGraph 索引过期产物，磁盘不存在，无冲突。
- 无新依赖、无新 Unity 包（TextMeshPro 为 Unity 内置包，需确认已导入 Essentials）。

## ADDED Requirements

### Requirement: 多层环形地图配置

系统 SHALL 提供一个 `RingMapConfigSO`（ScriptableObject），按层**仅**设置每层 cell 数量（`RingLayerConfig.count`），并包含全局层间错开范围、纵深/缩放曲线与滚轮速度参数。每层 cell 数量 MUST 在 `[0, 8]` 区间，满环为 8。层间错开、层大小、层方向与 cell 显示 MUST NOT 在此配置中逐层设置：错开与大小自动计算，方向取默认，cell 显示来自 `RingLayer` 预制体。

#### Scenario: 满层配置

- **WHEN** 配置某层 count = 8

- **THEN** 该层在 8 个固定槽位上各放一个 cell

#### Scenario: 越界数量钳制

- **WHEN** 配置某层 count > 8 或 count < 0

- **THEN** 系统 MUST 钳制到 `[0, 8]` 并通过 `XLogger` 输出警告

#### Scenario: 仅配置数量

- **WHEN** 用户为某层仅填写 count 而不填任何 cell/层视觉参数

- **THEN** 该层仍可完整生成：错开偏移按全局范围自动计算，baseScale 按 `layerSizeCurve` 自动取值，cell 显示来自 `RingLayer` 预制体

### Requirement: 8 槽固定网格与连续相邻填充

`UiRingLayout` SHALL 在一个圆环上使用固定的 8 槽 45° 网格（`angleStep = 360/8`），而非按 count 均分 360°。当 count < 8 时，cell MUST 从起始槽起连续占据 `count` 个相邻槽位，不重新均分整圈。

#### Scenario: 部分数量连续填充

- **WHEN** 某层 count = 3 且起始槽为 0（12 点）

- **THEN** cell 占据槽 0、1、2（连续相邻），其余槽位为空

#### Scenario: 跨层共同参照

- **WHEN** 两个层 count 分别为 3 与 5 且无错开偏移

- **THEN** 两层 cell 落在同一 8 槽网格上，可按角度对齐比较

### Requirement: 层间错开自动计算（允许部分重叠）

每层 SHALL 拥有一个起始角度偏移，由全局 `[minStagger, maxStagger]` 范围自动计算，不逐层配置。相邻层 MUST 不完全对齐，但 MAY 部分槽位重叠以显自然。

#### Scenario: 相邻层不完全对齐

- **WHEN** 第一层与第二层均存在且都应用了错开偏移

- **THEN** 两层的 cell 角度集合不完全相同，存在可见错开

#### Scenario: 允许部分重叠

- **WHEN** 两层 count 之和大于 8 且错开偏移较小

- **THEN** 两层 MAY 在部分槽位上重叠，系统不强制互补占位

### Requirement: 向下纵深与 Canvas 伪造深度

系统 SHALL 在 Canvas（RectTransform）内伪造向下纵深：每层 `baseScale(i)` 随层 index 单调递减（首层最大，向下越来越小），由配置 `layerSizeCurve` 自动取值；不使用世界空间 3D。每层 alpha 由连续浏览焦点 `t` 与该层 index `i` 的距离 `|t-i|` 经 `alphaByDistanceCurve` 映射：焦点层 alpha 最高，距离增大则衰减。整图根节点 SHALL 在提交/浏览时叠加 `progressScale` Tween。

#### Scenario: 首层最大向下递减

- **WHEN** 地图生成多层的 baseScale

- **THEN** layer 0 的 baseScale 最大，层 index 递增则 baseScale 单调递减

#### Scenario: 浏览焦点 alpha 最高

- **WHEN** 浏览焦点 `t` 位于层 `i`

- **THEN** 层 `i` alpha 最高

#### Scenario: 远层 alpha 衰减

- **WHEN** `|t-i|` 较大

- **THEN** 层 `i` alpha 随距离衰减

### Requirement: 激活层与浏览焦点分离

系统 SHALL 维护两个独立状态：已提交激活层 `activeLayerIndex` ∈ `[0, 末层]`，与连续浏览焦点 `t` ∈ `[activeLayerIndex, 末层]`。Enable 贴图 MUST 绑定 `activeLayerIndex`（`i == activeLayerIndex` 的层显示 Enable）。浏览焦点 `t` MUST 不改变 Enable 与 `activeLayerIndex`。

#### Scenario: 激活层显示 Enable

- **WHEN** 某 cell 所属层 index == `activeLayerIndex`

- **THEN** 该 cell 的 Image 显示 Enable 贴图

#### Scenario: 浏览不改 Enable

- **WHEN** 玩家滚动滚轮使 `t` 移动到非激活层

- **THEN** `activeLayerIndex` 与 Enable 贴图不变，被浏览层仍显示 Disable

### Requirement: 层预制体与层组件控制

`RingLayer` SHALL 为预制体：内置 `UiRingLayout`（cellPrefab 指向 `MapCell` 预制体）与 Enable/Disable 贴图引用，保证每层基础显示效果完全一致；由 `RingMapGenerator` 实例化，不直接创建 `RingCell`。`RingLayer` 组件 SHALL 控制本层是否激活、本层大小、本层方向、本层错开偏移（运行时覆盖）。

#### Scenario: 层激活态切换

- **WHEN** 整图控制器将某层 index 设为 `activeLayerIndex`

- **THEN** 该层内所有 cell 切到 Enable 贴图，其余层切到 Disable

#### Scenario: 层基础显示一致

- **WHEN** 同一 `RingLayer` 预制体被实例化为多层

- **THEN** 各层基础显示（cell 预制体、双态贴图）一致，仅 count 与运行时视觉参数不同

### Requirement: 整图控制器与事件总线订阅

`RingMapGenerator` SHALL 从 `RingMapConfigSO` 实例化各 `RingLayer` 预制体，SHALL 维护 `activeLayerIndex` 与浏览焦点 `t`，并 SHALL 在 `OnEnable` 订阅 `RingMapEvents.OnCellClicked`、`OnDisable` 取消订阅作为统一处理入口。

#### Scenario: 按配置生成多层

- **WHEN** 配置含 N 层且生成器初始化

- **THEN** 生成器实例化 N 个 `RingLayer` 预制体，每层按其 count 与自动错开偏移布局

#### Scenario: 订阅成对

- **WHEN** 生成器 OnEnable/OnDisable

- **THEN** 对 `RingMapEvents.OnCellClicked` 的订阅与取消订阅成对，无泄漏

### Requirement: 左右键短按切换激活层（可回退）

系统 SHALL 通过 InputActionAsset 的 `LeftClick`/`RightClick` 动作实现**短按**切换：短按左键使 `activeLayerIndex += 1`（进入下一层）、短按右键使 `activeLayerIndex -= 1`（退回上一层）。左键在最底层 MUST 无效，右键在最上层（index 0）MUST 无效。左键切换 MUST 忽略落在 cell Button 上的点击（命中 cell 时仅触发 cell 点击事件、不触发切换）。提交时 `t` MUST 钳到新 `[activeLayerIndex, 末层]`。

#### Scenario: 左键进入下一层

- **WHEN** 玩家短按左键且未在最底层

- **THEN** `activeLayerIndex` +1，新激活层 cell 显示 Enable

#### Scenario: 右键退回上一层

- **WHEN** 玩家短按右键且未在最上层

- **THEN** `activeLayerIndex` -1，新激活层 cell 显示 Enable

#### Scenario: 两端无效

- **WHEN** 已在最底层短按左键，或已在最上层短按右键

- **THEN** `activeLayerIndex` 不变，操作无效

#### Scenario: 左键命中 cell 不切换

- **WHEN** 短按左键且指针落在某个 cell Button 上

- **THEN** 仅触发该 cell 的点击事件，`activeLayerIndex` 不变

### Requirement: 滚轮浏览预览（不改激活层）

鼠标滚轮（InputActionAsset `Wheel`）SHALL 改变浏览焦点 `t`：前进 SHALL **向深层**（`t` 增大）、后退 SHALL **向表层**（`t` 减小）；`t` MUST 限制在 `[activeLayerIndex, 末层]`——向后不低于激活层，向前不超末层。滚轮 MUST 不改变 `activeLayerIndex` 与 Enable 贴图。浏览时整图根节点 SHALL 叠加 `progressScale` Tween。

#### Scenario: 滚轮前进向深层

- **WHEN** 玩家向前滚动滚轮

- **THEN** `t` 向深层增大但不超过末层，`activeLayerIndex` 与 Enable 不变

#### Scenario: 滚轮后退向表层止于激活层

- **WHEN** 玩家向后滚动使 `t` 趋近 `activeLayerIndex`

- **THEN** `t` 被钳制到 `activeLayerIndex`，不能浏览到激活层之前

### Requirement: HUD 状态指示（TextMeshPro）

系统 SHALL 提供两个 TextMeshProUGUI 指示，由 `RingMapInteraction` 更新：

1. **方向指示** SHALL 显示最近一次层移动方向——"向下"（进入更深层）或"向上"（退回更表层）；左/右键短按切换与滚轮浏览均更新。
2. **状态指示** SHALL 显示当前激活层数与当前显示层数（显示层取 `round(t)`）。

#### Scenario: 切换后更新方向

- **WHEN** 玩家短按左键进入更深层

- **THEN** 方向指示显示"向下"

#### Scenario: 回退后更新方向

- **WHEN** 玩家短按右键退回更表层

- **THEN** 方向指示显示"向上"

#### Scenario: 状态显示两层数

- **WHEN** 激活层为 2、显示层为 3

- **THEN** 状态指示显示"激活:2"与"显示:3"

### Requirement: Cell 点击事件总线

每个 `RingCell` SHALL 为 Button 且可点击。点击后 cell SHALL 经全局事件总线 `RingMapEvents.OnCellClicked` 上报（layerIndex, cellIndex），不持上游引用。`RingMapGenerator` SHALL 订阅该事件并 SHALL 通过 `XLogger.LogInfo` 输出层数与序号。

#### Scenario: 点击 cell 上报

- **WHEN** 玩家点击某 cell

- **THEN** cell 经 `RingMapEvents.OnCellClicked` 上报，generator 收到并 `XLogger.LogInfo` 输出 layerIndex 与 cellIndex

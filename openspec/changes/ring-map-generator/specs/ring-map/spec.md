## ADDED Requirements

### Requirement: 多层环形地图配置

系统 SHALL 提供一个 `RingMapConfigSO`（ScriptableObject），按层列出每层 cell 数量，并包含全局层间错开范围、纵深/缩放曲线与 `requireLongPress`/`holdDuration` 参数。每层 cell 数量 MUST 在 `[0, 8]` 区间，满环为 8。

#### Scenario: 满层配置

- **WHEN** 配置某层 count = 8

- **THEN** 该层在 8 个固定槽位上各放一个 cell

#### Scenario: 越界数量钳制

- **WHEN** 配置某层 count > 8 或 count < 0

- **THEN** 系统 MUST 钳制到 `[0, 8]` 并通过 `XLogger` 输出警告

### Requirement: 8 槽固定网格与连续相邻填充

`UiRingLayout` SHALL 在一个圆环上使用固定的 8 槽 45° 网格（`angleStep = 360/8`），而非按 count 均分 360°。当 count < 8 时，cell MUST 从起始槽起连续占据 `count` 个相邻槽位，不重新均分整圈。

#### Scenario: 部分数量连续填充

- **WHEN** 某层 count = 3 且起始槽为 0（12 点）

- **THEN** cell 占据槽 0、1、2（连续相邻），其余槽位为空

#### Scenario: 跨层共同参照

- **WHEN** 两个层 count 分别为 3 与 5 且无错开偏移

- **THEN** 两层 cell 落在同一 8 槽网格上，可按角度对齐比较

### Requirement: 层间错开（允许部分重叠）

每层 SHALL 拥有一个起始角度偏移，取值于配置的 `[minStagger, maxStagger]` 范围内。相邻层 MUST 不完全对齐，但 MAY 部分槽位重叠以显自然。

#### Scenario: 相邻层不完全对齐

- **WHEN** 第一层与第二层均存在且都应用了错开偏移

- **THEN** 两层的 cell 角度集合不完全相同，存在可见错开

#### Scenario: 允许部分重叠

- **WHEN** 两层 count 之和大于 8 且错开偏移较小

- **THEN** 两层 MAY 在部分槽位上重叠，系统不强制互补占位

### Requirement: 向下纵深与 Canvas 伪造深度

系统 SHALL 在 Canvas（RectTransform）内伪造向下纵深：每层 `baseScale(i)` 随层 index 单调递减（首层最大，向下越来越小），由配置 `layerSizeCurve` 决定；不使用世界空间 3D。每层 alpha 由连续浏览焦点 `t` 与该层 index `i` 的距离 `|t-i|` 经 `alphaByDistanceCurve` 映射：焦点层 alpha 最高，距离增大则衰减。整图根节点 SHALL 在提交/浏览时叠加 `progressScale` Tween。

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

### Requirement: 层组件控制

`RingLayer` 组件 SHALL 包裹一个 `UiRingLayout`，并 SHALL 控制本层是否激活、本层大小、本层方向、本层错开偏移。

#### Scenario: 层激活态切换

- **WHEN** 整图控制器将某层 index 设为 `activeLayerIndex`

- **THEN** 该层内所有 cell 切到 Enable 贴图，其余层切到 Disable

### Requirement: 整图控制器与事件总线订阅

`RingMapGenerator` SHALL 从 `RingMapConfigSO` 实例化各层，SHALL 维护 `activeLayerIndex` 与浏览焦点 `t`，并 SHALL 在 `OnEnable` 订阅 `RingMapEvents.OnCellClicked`、`OnDisable` 取消订阅作为统一处理入口。

#### Scenario: 按配置生成多层

- **WHEN** 配置含 N 层且生成器初始化

- **THEN** 生成器实例化 N 个 `RingLayer`，每层按其 count 与错开偏移布局

#### Scenario: 订阅成对

- **WHEN** 生成器 OnEnable/OnDisable

- **THEN** 对 `RingMapEvents.OnCellClicked` 的订阅与取消订阅成对，无泄漏

### Requirement: 左右键提交激活层（可回退）

系统 SHALL 通过鼠标左键使 `activeLayerIndex += 1`（进入下一层）、右键使 `activeLayerIndex -= 1`（退回上一层），输入基于 InputSystem。左键在最底层 MUST 无效，右键在最上层（index 0）MUST 无效。`requireLongPress` 为 true 时按住填进度条充满后提交 ±1 并循环，松手回退进度；为 false 时单击即时提交 ±1 且无进度条。提交时 `t` MUST 钳到新 `[activeLayerIndex, 末层]`。

#### Scenario: 左键进入下一层

- **WHEN** 玩家提交左键且未在最底层

- **THEN** `activeLayerIndex` +1，新激活层 cell 显示 Enable

#### Scenario: 右键退回上一层

- **WHEN** 玩家提交右键且未在最上层

- **THEN** `activeLayerIndex` -1，新激活层 cell 显示 Enable

#### Scenario: 两端无效

- **WHEN** 已在最底层提交左键，或已在最上层提交右键

- **THEN** `activeLayerIndex` 不变，操作无效

#### Scenario: 单击模式即时提交

- **WHEN** `requireLongPress = false` 且玩家单击左/右键

- **THEN** 立即提交 ±1，无进度条

### Requirement: 滚轮浏览预览（不改激活层）

鼠标滚轮 SHALL 改变浏览焦点 `t` 进行预览，`t` MUST 限制在 `[activeLayerIndex, 末层]`——向后不低于激活层，向前不超末层。滚轮 MUST 不改变 `activeLayerIndex` 与 Enable 贴图。浏览时整图根节点 SHALL 叠加 `progressScale` Tween。

#### Scenario: 滚轮向前浏览

- **WHEN** 玩家向前滚动滚轮

- **THEN** `t` 增大但不超过末层，`activeLayerIndex` 与 Enable 不变

#### Scenario: 滚轮向后止于激活层

- **WHEN** 玩家向后滚动使 `t` 趋近 `activeLayerIndex`

- **THEN** `t` 被钳制到 `activeLayerIndex`，不能浏览到激活层之前

### Requirement: Cell 点击事件总线

每个 `RingCell` SHALL 为 Button 且可点击。点击后 cell SHALL 经全局事件总线 `RingMapEvents.OnCellClicked` 上报（layerIndex, cellIndex），不持上游引用。`RingMapGenerator` SHALL 订阅该事件并 SHALL 通过 `XLogger.LogInfo` 输出层数与序号。

#### Scenario: 点击 cell 上报

- **WHEN** 玩家点击某 cell

- **THEN** cell 经 `RingMapEvents.OnCellClicked` 上报，generator 收到并 `XLogger.LogInfo` 输出 layerIndex 与 cellIndex


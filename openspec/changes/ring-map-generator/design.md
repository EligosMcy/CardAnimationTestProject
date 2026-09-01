## Context

`Assets/MapTest/` 已有单环布局原型：`RingConfigSO`（单环参数）、`UiRingLayout`（在一个圆环上按 `angleStep = 360/count` 均分排列 cell，含本地对象池）、`MapCell.prefab`（裸 `Image`，无脚本）与 Enable/Disable 两张贴图。命名空间为 `Tools.UI.Ring`。项目另有 `XUtils`（`[Button]` 特性、`XLogger`）、对象池/单例模式与已安装的 **InputSystem**（`Assets/InputSystem_Actions.inputactions`）可复用。

本次在此基础上扩展为完整多层环形地图。先前 CodeGraph 报告的 `Assets/Scripts` 节点式地图系统经磁盘核查为索引过期产物，实际不存在，无冲突。

## Goals / Non-Goals

**Goals:**
- 从配置生成多层环形地图：每层 cell 数量可配（满环 8），满环以外按连续相邻槽位填充。
- 8 槽固定 45° 网格作为跨层共同角度参照，层间错开（允许部分重叠，不完全对齐）显自然。
- Canvas 内伪造向下纵深（scale + alpha），不引入世界空间 3D。
- 组件分离：`RingCell`（Button + 双态贴图 + 大小/方向）、`RingLayer`（层激活/大小/方向/错开）、`RingMapGenerator`（整图 + 事件总线订阅）、`RingMapInteraction`（输入）。
- 交互：左键进入下一层、右键退回上一层（两端无效，可回退）；滚轮在 [激活层, 末层] 浏览预览不改激活层；`requireLongPress` 控制长按/单击。
- cell 点击经全局事件总线统一处理（先 Log 层数+序号）。

**Non-Goals:**
- 不做世界空间 3D / 透视相机真纵深。
- 不做 cell 之间的连接路径线（预览图中的石桥/荧光路径本期不做）。
- 不做 cell 内多种图标/内容（如预览中的笑脸/菱形节点），本期仅 Enable/Disable 两态。
- 不做地图进度存档/读档。
- 不做 cell 数量 >8 的支持（设计上限 8）。

## Decisions

### D1：复用 `UiRingLayout` 作为布局引擎，`RingLayer` 包裹它
`UiRingLayout` 已有对象池、角度计算、定位/旋转/缩放逻辑。`RingLayer` 持有一个 `UiRingLayout` 引用并叠加层状态（激活/大小/方向/错开），不重写布局。理由：避免重复造轮子，布局与层状态解耦。
**替代方案**：把布局逻辑折进 `RingLayer`——否决，会丢失现有池化与编辑器存盘能力。

### D2：8 槽固定 45° 网格 + 连续相邻填充
改造 `UiRingLayout`：`angleStep = 360 / 8 = 45°`（固定，不再随 count 变）。给定起始槽 `startSlot`，count 个 cell 占据槽 `[startSlot, startSlot+count-1]`（mod 8）。count<8 时形成连续相邻扇形，不重新均分整圈。理由：保持跨层共同角度参照，使层间错开有意义；符合用户"相邻生成"。
**替代方案**：count<8 仍均分 360°——否决，层间无法对齐错开。

### D3：层间错开 = 每层 `startAngleOffset` 在配置范围内，允许部分重叠
每层在 `[minStagger, maxStagger]`（角度）范围内取一个偏移作为本层起始角，相邻层不完全对齐即可，允许部分槽位重叠。默认偏移给到半槽量级（如 22.5°）以保证可见错开。理由：用户明确要自然、可部分重叠。
**替代方案 A**：严格互补占位——否决，过于规整不自然。**替代方案 B**：每层固定递增步进——可选作为确定性模式，留作配置开关。

### D4：向下纵深——首层最大、向下递减的 Canvas 伪造深度
地图为"向下"纵深栈：首层（index 0）最大，向下层 index 递增则层尺寸递减。三层叠加：
1. **per-layer baseScale(i)**：由配置 `layerSizeCurve` 在 index `i` 上取值，单调递减（layer 0=1.0，向下变小），实现"首层最大向下越来越小"。层最终 scale = `baseScale(i)`（不再因距离单独缩 scale，避免与 baseScale 叠加失真）。
2. **alpha 衰减**：由 `distance = |t - i|` 经 `alphaByDistanceCurve` 映射——当前焦点层 alpha 最高，距离增大则 alpha 衰减。
3. **根节点 progressScale**：整图根在左/右键提交、滚轮浏览期间按 `progressScaleCurve` 做 Tween，产生"扑面/远去"放大缩小。
其中 `t` 为连续视觉深度，取值范围 `[activeLayerIndex, lastLayerIndex]`（见 D5/D7）。理由：纯 RectTransform 即可形成向下收缩的纵深栈，贴图复用直接。
**替代方案**：转世界空间 3D Quad + 透视相机——否决，预制体与贴图需重做，改动过大。

### D5：激活层 `activeLayerIndex` 决定 Enable，与浏览焦点 `t` 分离
两个独立状态：
- `activeLayerIndex` ∈ `[0, lastLayerIndex]`——已提交激活层，**Enable 贴图绑此值**（`i == activeLayerIndex` 的层显示 Enable）。由左/右键提交改变（D7），可双向回退。
- `t` ∈ `[activeLayerIndex, lastLayerIndex]`——连续浏览焦点，由滚轮改变，驱动 alpha/根 Tween（D4），**不改变 Enable**。
即：滚轮浏览到的更深层显示 Disable（仅预览未提交），左键提交后才变 Enable。理由：用户明确"滚轮浏览 vs 左右键改激活层"，故激活态与浏览态必须分离。
**替代方案**：Enable 跟随 `round(t)`——否决，会使浏览即提交，违背"浏览不改激活层"。

### D6：多层配置 SO
新增 `RingMapConfigSO`（ScriptableObject），内含 `List<RingLayerConfig>`（每项：count + 可选 per-layer size/direction/stagger override）与全局参数（maxSlots=8、`[minStagger,maxStagger]`、`layerSizeCurve`、`alphaByDistanceCurve`、`progressScaleCurve`、`requireLongPress`、`holdDuration`、cell 默认尺寸/贴图）。`RingLayerConfig` 为 `[Serializable]` 结构。保留现有 `RingConfigSO` 供单环调试，不破坏。
**替代方案**：复用 `RingConfigSO` 加 list 字段——可行但语义混淆，故新增独立 SO。

### D7：交互——左/右键提交激活层 ±1，滚轮浏览 [active, last]，InputSystem
输入基于项目已有 InputSystem（`Mouse.current`）：
- **左键**：`activeLayerIndex += 1`，到末层无效。
- **右键**：`activeLayerIndex -= 1`，到 0 无效。**可回退**（替代原"单调不可回退"）。
- **`requireLongPress`**（bool，默认 true）：true 时按住左/右键填进度条，充满提交 ±1 并复位、持续按住则循环逐层进/退；false 时单击即时 ±1、无进度条。
- **滚轮**：改变 `t`，∈ `[activeLayerIndex, lastLayerIndex]`，向后下限 = `activeLayerIndex`（不能浏览到激活层之前），向前到末层。不改 `activeLayerIndex`。
- 提交/浏览时根节点 `progressScale` Tween。
理由：用户明确分"浏览（滚轮，只看不改激活）"与"激活层变动（左右键，提交）"两路，且允许回退。**替代方案**：滚轮直接改激活层——否决，离散跳变不连续且混淆浏览与提交。

### D8：进度条 UI（仅 hold 模式）
独立 Image 填充，按 hold 累积时长映射 0..1（`holdDuration`），充满触发提交并复位、循环；`requireLongPress=false` 时不显示/不使用。理由：用户明确要进度条与 `requireLongPress` 开关。

### D9：Cell 点击事件——全局事件总线（静态 `RingMapEvents`）
新增静态类 `RingMapEvents`，暴露 `static event Action<int,int> OnCellClicked`（参数 layerIndex, cellIndex）。`RingCell` 挂 `Button`，`onClick` 时调 `RingMapEvents.OnCellClicked?.Invoke(layerIndex, cellIndex)`——cell 只管"喊"，不持任何上游引用。`RingMapGenerator` 在 `OnEnable` 订阅、`OnDisable` 取消订阅，作为统一处理入口先 `XLogger.LogInfo($"cell clicked layer={l} index={c}")`。
理由：用户判定使用事件总线；解耦、便于后续多订阅者（音效/UI 高亮/统计）扩展。**替代方案**：直接引用链 cell→layer→generator——更可追踪但回引多、扩展差，本期不取。
**生命周期约束**：订阅必须成对 OnEnable/OnDisable，避免静态事件泄漏。

## Risks / Trade-offs

- **[Canvas 伪造深度无真遮挡]** → 层按 depth 排序 hierarchy / 用 alpha 衰减模拟，可接受。
- **[8 槽上限]** → 设计即上限 8，count 钳制到 [0,8]，>8 报错。
- **[错开偏移过小仍显对齐]** → 默认偏移半槽（22.5°），`minStagger` 下限设为可见量级。
- **[浏览焦点 `t` 与激活层 `active` 不同步]** → 明确：`t` 只动视觉且 ∈[active,last]，提交时 `active` ±1、`t` 钳到新 [active,last]。
- **[静态事件总线泄漏]** → 订阅 OnEnable/OnDisable 成对，单测覆盖。
- **[Enable=active 而浏览到更深层时该层显 Disable 但被放大]** → 属预期"预览未提交"语义；若观感不佳，后续可让浏览焦点层轻微高亮（不改 Enable）。
- **[CodeGraph 索引过期]** → 任何代码定位以磁盘 Read/列目录为准，不信任索引报告的已删文件。
- **[`UiRingLayout` 改 8 槽会破坏现有单环均分行为]** → 现有单环测试场景将表现变化；作为本期预期变更，升级场景配置。

## Open Questions

- `requireLongPress` 默认值：倾向 true（需长按），待确认。
- hold 模式下"充满提交并循环逐层"是否符合预期，还是充满只提交一次即停？倾向循环（持续按住逐层进/退）。
- 滚轮浏览 `t` 是否需要吸附（松手吸附到整数层）还是纯自由连续？倾向纯连续，提交由左右键负责。
- per-layer size/direction override 是否必填：倾向可选，缺省走全局 `layerSizeCurve`；direction 同理。
- 事件总线是否未来要升级为 ScriptableObject 通道（Inspector 可拖引用）：本期先用静态类，后续按需升级。

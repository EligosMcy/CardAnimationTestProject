## Context

`Assets/MapTest/` 已有单环布局原型：`RingConfigSO`（单环参数）、`UiRingLayout`（在一个圆环上按 `angleStep = 360/count` 均分排列 cell，含本地对象池）、`MapCell.prefab`（裸 `Image`，无脚本）与 Enable/Disable 两张贴图。命名空间为 `Tools.UI.Ring`。项目另有 `XUtils`（`[Button]` 特性、`XLogger`）、对象池/单例模式与已安装的 **InputSystem**（`Assets/InputSystem_Actions.inputactions`）可复用。

本次在此基础上扩展为完整多层环形地图。先前 CodeGraph 报告的 `Assets/Scripts` 节点式地图系统经磁盘核查为索引过期产物，实际不存在，无冲突。

## Goals / Non-Goals

**Goals:**

- 从配置生成多层环形地图：每层**仅设置 cell 数量**（满环 8），其余参数自动计算或来自 `RingLayer` 预制体。

- 8 槽固定 45° 网格作为跨层共同角度参照，层间错开（允许部分重叠，不完全对齐）显自然。

- Canvas 内伪造向下纵深（scale + alpha），不引入世界空间 3D。

- 组件分离：`RingCell`（Button + 双态贴图 + 大小/方向）、`RingLayer`（预制体，内置 `UiRingLayout` + cell 预制体 + 双态贴图，每层基础显示一致；运行时覆盖激活/大小/方向/错开）、`RingMapGenerator`（整图 + 事件总线订阅）、`RingMapInteraction`（输入 + HUD 指示）。

- 交互：输入基于 InputActionAsset，**全部短按**；左键进入下一层、右键退回上一层（两端无效，可回退）；滚轮前进=向深层、后退=向表层，在 \[激活层, 末层] 浏览显示层不改激活层。

- TextMeshPro 指示：方向指示（当前执行的向下/向上）+ 状态指示（激活层数/显示层数）。

- cell 点击经全局事件总线统一处理（先 Log 层数+序号）。

**Non-Goals:**

- 不做世界空间 3D / 透视相机真纵深。

- 不做 cell 之间的连接路径线（预览图中的石桥/荧光路径本期不做）。

- 不做 cell 内多种图标/内容（如预览中的笑脸/菱形节点），本期仅 Enable/Disable 两态。

- 不做地图进度存档/读档。

- 不做 cell 数量 >8 的支持（设计上限 8）。

- 不做长按/进度条交互（已由"全部短按"取代）。

## Decisions

### D1：`RingLayer` 预制体统一基础显示，`RingMapGenerator` 实例化预制体

`RingLayer` 制作成预制体：内含 `UiRingLayout`（其 `cellPrefab` 指向 `MapCell` 预制体）与 Enable/Disable 贴图引用，保证每层基础显示效果完全一致。`RingMapGenerator` **不直接创建** **`RingCell`**，而是按配置实例化 N 个 `RingLayer` 预制体，再把每层 count 与运行时视觉参数（size/stagger/isActive）注入覆盖差异化。理由：层基础显示一致由预制体保证，cell 相关设置（贴图、尺寸）直接填在预制体内、不进 config，避免配置冗余与显示漂移。
**替代方案**：generator 运行时拼装 cell 层级——否决，每层基础显示难以保证一致，配置面大。

### D2：8 槽固定 45° 网格 + 连续相邻填充

改造 `UiRingLayout`：`angleStep = 360 / 8 = 45°`（固定，不再随 count 变）。给定起始槽 `startSlot`，count 个 cell 占据槽 `[startSlot, startSlot+count-1]`（mod 8）。count<8 时形成连续相邻扇形，不重新均分整圈。理由：保持跨层共同角度参照，使层间错开有意义；符合用户"相邻生成"。
**替代方案**：count<8 仍均分 360°——否决，层间无法对齐错开。

### D3：层间错开自动计算（全局范围随机），允许部分重叠

每层起始角度偏移**由全局** **`[minStagger, maxStagger]`** **范围自动生成**（运行时随机取），不逐层配置；相邻层不完全对齐即可，允许部分槽位重叠。默认偏移给到半槽量级（如 22.5°）以保证可见错开。理由：用户要求"只设置数量，其余自动计算"。
**替代方案 A**：严格互补占位——否决，过于规整不自然。**替代方案 B**：每层固定递增步进——可选作为确定性模式，留作后续配置开关。

### D4：向下纵深——首层最大、向下递减的 Canvas 伪造深度

地图为"向下"纵深栈：首层（index 0）最大，向下层 index 递增则层尺寸递减。三层叠加：

1. **per-layer baseScale(i)**：由配置 `layerSizeCurve` 在 index `i` 上取值（自动计算，不逐层配置），单调递减（layer 0=1.0，向下变小），实现"首层最大向下越来越小"。层最终 scale = `baseScale(i)`（不再因距离单独缩 scale，避免与 baseScale 叠加失真）。
2. **alpha 衰减**：由 `distance = |t - i|` 经 `alphaByDistanceCurve` 映射——当前焦点层 alpha 最高，距离增大则 alpha 衰减。
3. **根节点 progressScale**：整图根在左/右键提交、滚轮浏览期间按 `progressScaleCurve` 做 Tween，产生"扑面/远去"放大缩小。
   其中 `t` 为连续视觉深度，取值范围 `[activeLayerIndex, lastLayerIndex]`（见 D5/D7）。理由：纯 RectTransform 即可形成向下收缩的纵深栈，贴图复用直接。
   **替代方案**：转世界空间 3D Quad + 透视相机——否决，预制体与贴图需重做，改动过大。

### D5：激活层 `activeLayerIndex` 决定 Enable，与浏览焦点 `t` 分离

两个独立状态：

- `activeLayerIndex` ∈ `[0, lastLayerIndex]`——已提交激活层，**Enable 贴图绑此值**（`i == activeLayerIndex` 的层显示 Enable）。由左/右键短按改变（D7），可双向回退。

- `t` ∈ `[activeLayerIndex, lastLayerIndex]`——连续浏览焦点，由滚轮改变（前进向深层、后退向表层），驱动 alpha/根 Tween（D4），**不改变 Enable**。
  即：滚轮浏览到的更深层显示 Disable（仅预览未提交），左键提交后才变 Enable。理由：用户明确"滚轮浏览 vs 左右键改激活层"，故激活态与浏览态必须分离。
  **替代方案**：Enable 跟随 `round(t)`——否决，会使浏览即提交，违背"浏览不改激活层"。

### D6：多层配置 SO——每层仅 count，cell 设置移入预制体

新增 `RingMapConfigSO`（ScriptableObject），内含 `List<RingLayerConfig> layers` 与全局参数。`RingLayerConfig` 为 `[Serializable]` 结构，**仅含** **`count`**（int，钳制 0..8）——不设 per-layer size/direction/stagger override，错开（D3）、大小（D4）、方向（默认）均由全局参数自动计算。**不含任何 cell 设置**（cell 默认尺寸、Enable/Disable 贴图）：这些配置于 `RingLayer` 预制体内。全局参数：`maxSlots=8`、`[minStagger,maxStagger]`、`layerSizeCurve`、`alphaByDistanceCurve`、`progressScaleCurve`、`scrollSpeed`。保留现有 `RingConfigSO` 供单环调试，不破坏。
**替代方案**：复用 `RingConfigSO` 加 list 字段——可行但语义混淆，故新增独立 SO。

### D7：交互——InputActionAsset，全部短按

输入基于 **InputActionAsset**（新增 `RingMapControls.inputactions`，启用 Generate C# Class），动作：

- **LeftClick**（Button，绑定 Mouse/leftButton）**短按**：`activeLayerIndex += 1`，到末层无效。实现时需**忽略落在 cell Button 上的点击**（通过 EventSystem 命中检测：命中 cell 时仅触发 cell 点击事件、不触发层切换），避免与 D9 的 cell 点击冲突。

- **RightClick**（Button，绑定 Mouse/rightButton）**短按**：`activeLayerIndex -= 1`，到 0 无效。**可回退**。

- **Wheel**（Vector2，绑定 Mouse/scroll）：前进（y>0）→ 显示层 `t` **向深层**（+）；后退（y<0）→ **向表层**（-）；`t` ∈ `[activeLayerIndex, lastLayerIndex]` 钳制，向后止于 `activeLayerIndex`，不改 `activeLayerIndex`。

全部为**短按**交互：无长按、无进度条（移除 `requireLongPress` 与 `holdDuration`）。提交/浏览时根节点 `progressScale` Tween。
理由：用户要求 InputActionAsset 实现鼠标输入、全部短按、滚轮前进/后退分别向深层/向表层、左右键切换激活层。**替代方案**：直接 `Mouse.current` 轮询——否决，不符合 InputActionAsset 要求。

### D8：HUD 状态指示（TextMeshPro）

新增两个 TextMeshProUGUI（置于场景 MapUI 下），由 `RingMapInteraction` 持有引用并更新：

1. **方向指示**：显示最近一次层移动方向——"向下"（进入更深层）或"向上"（退回更表层）；左/右键短按切换与滚轮浏览均更新。
2. **状态指示**：显示当前激活层数与当前显示层数，如 `激活:2  显示:3`（显示层取 `round(t)`）。

理由：用户要求可视化当前执行方向与激活/显示层数状态。

### D9：Cell 点击事件——全局事件总线（静态 `RingMapEvents`）

新增静态类 `RingMapEvents`，暴露 `static event Action<int,int> OnCellClicked`（参数 layerIndex, cellIndex）。`RingCell` 挂 `Button`，`onClick` 时调 `RingMapEvents.OnCellClicked?.Invoke(layerIndex, cellIndex)`——cell 只管"喊"，不持任何上游引用。`RingMapGenerator` 在 `OnEnable` 订阅、`OnDisable` 取消订阅，作为统一处理入口先 `XLogger.LogInfo($"cell clicked layer={l} index={c}")`。
理由：用户判定使用事件总线；解耦、便于后续多订阅者（音效/UI 高亮/统计）扩展。**替代方案**：直接引用链 cell→layer→generator——更可追踪但回引多、扩展差，本期不取。
**生命周期约束**：订阅必须成对 OnEnable/OnDisable，避免静态事件泄漏。
**与 D7 冲突处理**：左键短按切换须忽略命中 cell Button 的点击，cell 点击只走事件总线。

## Risks / Trade-offs

- **\[Canvas 伪造深度无真遮挡]** → 层按 depth 排序 hierarchy / 用 alpha 衰减模拟，可接受。

- **\[8 槽上限]** → 设计即上限 8，count 钳制到 \[0,8]，>8 报错。

- **\[错开偏移过小仍显对齐]** → 默认偏移半槽（22.5°），`minStagger` 下限设为可见量级。

- **\[浏览焦点** **`t`** **与激活层** **`active`** **不同步]** → 明确：`t` 只动视觉且 ∈\[active,last]，提交时 `active` ±1、`t` 钳到新 \[active,last]。

- **\[静态事件总线泄漏]** → 订阅 OnEnable/OnDisable 成对，单测覆盖。

- **\[Enable=active 而浏览到更深层时该层显 Disable 但被放大]** → 属预期"预览未提交"语义；若观感不佳，后续可让浏览焦点层轻微高亮（不改 Enable）。

- **\[左键切换与 cell Button 点击冲突]** → 切换逻辑用 EventSystem 命中检测排除 cell；若误触，可要求点击空白区才切换。

- **\[TextMeshPro 包未导入]** → 制作 HUD 前确认 TMP Essentials 已导入（Unity 内置包，无第三方依赖）。

- **\[CodeGraph 索引过期]** → 任何代码定位以磁盘 Read/列目录为准，不信任索引报告的已删文件。

- **\[`UiRingLayout`** **改 8 槽会破坏现有单环均分行为]** → 现有单环测试场景将表现变化；作为本期预期变更，升级场景配置。

- **\[`RingLayer`** **预制体内** **`UiRingLayout`** **的 cellPrefab 需预先指向** **`MapCell.prefab`]** → 制作预制体时配置一次，generator 只实例化不拼装 cell。

## Open Questions

- 滚轮浏览 `t` 是否需要吸附（松手吸附到整数层）还是纯自由连续？倾向纯连续，切换由左右键负责。

- 方向指示是否随滚轮浏览也更新（倾向：是，显示最近一次层移动方向），还是仅切换时更新？

- 层间错开"自动计算"用随机（每次生成不同）还是确定性公式（固定步进，可复现）？倾向随机 + 保留全局范围，确定性开关后续再加。

- 事件总线是否未来要升级为 ScriptableObject 通道（Inspector 可拖引用）：本期先用静态类，后续按需升级。


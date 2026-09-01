## 1. 多层配置

- [x] 1.1 新建 `RingLayerConfig`（`[Serializable]` 结构）：仅 `count`(int, 钳制 0..8)，无 per-layer override 字段（错开/大小/方向自动计算）。

- [x] 1.2 新建 `RingMapConfigSO`（ScriptableObject，`Tools.UI.Ring`）：`List<RingLayerConfig> layers`、`int maxSlots=8`、`Vector2 minMaxStagger`、`AnimationCurve layerSizeCurve`（随 index 递减）、`AnimationCurve alphaByDistanceCurve`、`AnimationCurve progressScaleCurve`、`float scrollSpeed`。不含 cell 默认尺寸/贴图（移入 `RingLayer` 预制体），不含长按相关字段。

- [x] 1.3 为 `RingMapConfigSO` 加 `[CreateAssetMenu]`，提供编辑器存盘/读盘方法（参考现有 `RingConfigSO` 模式与 `[Button]`）。

## 2. 布局引擎改造（8 槽固定网格）

- [x] 2.1 在 `UiRingLayout` 中将 `angleStep` 改为固定 `360 / maxSlots`（maxSlots 默认 8），移除按 count 均分逻辑。

- [x] 2.2 新增 `startSlot`/`startAngleOffset` 字段，count 个 cell 占据槽 `[startSlot, startSlot+count-1] mod maxSlots`（连续相邻填充）。

- [x] 2.3 count 越界钳制到 `[0, maxSlots]`，越界时 `XLogger.LogWarning`。

- [x] 2.4 保留现有本地对象池与编辑器 `saveToConfig`，确保单环测试场景可继续工作（行为变为 8 槽网格）。

## 3. Cell 组件与事件总线

- [x] 3.1 新建 `RingCell`（`Tools.UI.Ring`，挂在 `MapCell` 预制体）：需 `Button` 组件、`Image _image`、`Sprite enableSprite`/`disableSprite`、`float cellSize`/`direction`、`int layerIndex`/`cellIndex`（生成时回填）。

- [x] 3.2 实现 `SetEnable(bool active)`：切换 `_image.sprite`；暴露 `ApplySizeAndDirection()` 应用 sizeDelta 与 localEulerAngles。

- [x] 3.3 新建静态事件总线 `RingMapEvents`：`public static event Action<int,int> OnCellClicked;`（参数 layerIndex, cellIndex）。

- [x] 3.4 `RingCell` 的 `Button.onClick` → `RingMapEvents.OnCellClicked?.Invoke(layerIndex, cellIndex)`（cell 只喊，不持上游引用）。

- [x] 3.5 升级 `MapCell.prefab`（作为 `RingLayer` 预制体的 cell 子预制体）：挂 `RingCell` + `Button`，把现有 `Image` 子物体引用接入，赋值 Enable/Disable 贴图字段。

## 4. Layer 预制体与组件

- [x] 4.1 新建 `RingLayer` 组件（`Tools.UI.Ring`，挂在 `RingLayer` 预制体根）：持有 `UiRingLayout` 引用、`bool isActive`、`float layerSize`/`layerDirection`/`staggerOffset`、`int layerIndex`、`int baseSlotCount`。

- [x] 4.2 实现 `SetVisual(float t, int activeLayerIndex, AnimationCurve layerSizeCurve, AnimationCurve alphaByDistanceCurve)`：`scale = layerSizeCurve.Evaluate(layerIndex)`（随 index 递减）；`alpha = alphaByDistanceCurve.Evaluate(|t-layerIndex|)`；`SetEnable(layerIndex == activeLayerIndex)`。

- [x] 4.3 实现 `SetEnable(bool active)`：遍历 cell 调 `RingCell.SetEnable(active)`；同步 `isActive`。

- [x] 4.4 生成时把每个 cell 的 `layerIndex`/`cellIndex` 回填（供事件上报）。

- [x] 4.5 制作 `RingLayer.prefab`：内含 `UiRingLayout`（cellPrefab 指向 `MapCell.prefab`）与 Enable/Disable 贴图引用，作为 generator 实例化的模板。

## 5. 整图控制器与总线订阅

- [x] 5.1 新建 `RingMapGenerator`（`Tools.UI.Ring`，`[RequireComponent(RectTransform)]`）：持有 `RingMapConfigSO`、`RingLayer` 预制体引用、`List<RingLayer> layers`、`float browseT`（浏览焦点）、`int activeLayerIndex`。

- [x] 5.2 `Generate()`：按配置**实例化 N 个** **`RingLayer`** **预制体**（不直接创建 cell）；把每层 `count` 注入其 `UiRingLayout`；自动计算 stagger（全局 `[minStagger,maxStagger]` 随机）与 baseScale（`layerSizeCurve`）并设置到 `RingLayer`；回填 cell 的 layerIndex/cellIndex。

- [x] 5.3 `Update()`：每帧把 `browseT` 与 `activeLayerIndex` 广播给各层 `SetVisual`，刷新 Enable 双态。

- [x] 5.4 `OnEnable` 订阅 `RingMapEvents.OnCellClicked += handleCellClicked`；`OnDisable` 取消订阅（成对，防泄漏）。

- [x] 5.5 `handleCellClicked(int l, int c)`：`XLogger.LogInfo($"cell clicked layer={l} index={c}")`（统一处理入口，后续扩展路由）。

- [x] 5.6 `AdvanceLayer(int dir)`：`activeLayerIndex = Clamp(activeLayerIndex+dir, 0, layers.Count-1)`；`browseT` 钳到新 `[activeLayerIndex, 末层]`；触发根 `progressScale` Tween。

- [x] 5.7 提供编辑器 `[Button]` 重新生成与把运行时参数写回 SO（参考 `UiRingLayout.saveToConfig`）。

## 6. 交互（InputActionAsset 短按 + 滚轮）与 HUD 指示

- [x] 6.1 新建 `RingMapControls.inputactions`（InputActionAsset，启用 Generate C# Class）：`LeftClick`(Button, Mouse/leftButton)、`RightClick`(Button, Mouse/rightButton)、`Wheel`(Vector2, Mouse/scroll)；确认 TextMeshPro 包已导入（如需则导入 TMP Essentials）。

- [x] 6.2 新建 `RingMapInteraction`（`Tools.UI.Ring`）：持有 `RingMapGenerator` 引用、两个 TextMeshProUGUI（方向、状态）、`AnimationCurve progressScaleCurve`、从 config 读 `scrollSpeed`。

- [x] 6.3 左键**短按**（`LeftClick.performed`）→ `AdvanceLayer(+1)`；最底层无效；通过 EventSystem 命中检测忽略落在 cell Button 上的点击（与 cell 点击事件不冲突）。

- [x] 6.4 右键**短按**（`RightClick.performed`）→ `AdvanceLayer(-1)`；最上层无效。

- [x] 6.5 滚轮（`Wheel`）改变 `browseT`：前进（y>0）向深层(+)、后退（y<0）向表层(-)，钳制到 `[activeLayerIndex, 末层]`；不改 `activeLayerIndex`。

- [x] 6.6 提交/浏览期间按 `progressScaleCurve` 对根节点 scale 做 Tween，实现放大/缩小。

- [x] 6.7 更新 HUD：方向指示（最近一次层移动 向下/向上，切换与浏览均更新）；状态指示（激活层 + 显示层 `round(t)`）。

## 7. 场景接入

- [x] 7.1 在 `SampleScene` 的 `MapUI` 下搭建 `RingMapGenerator` 根节点，挂载配置 SO 与 `RingLayer` 预制体引用。

- [x] 7.2 添加两个 TextMeshProUGUI（方向指示、激活/显示层数状态指示）并接入 `RingMapInteraction`。

- [x] 7.3 Play 模式验证：生成多层、8 槽连续相邻填充、层间错开、向下纵深 baseScale/alpha、激活层 Enable、左右键短按进退、滚轮浏览、HUD 指示。

## 8. 验证

- [x] 8.1 验证满层(8)与部分层(3/5)布局正确，层间存在可见错开且允许部分重叠。

- [x] 8.2 验证左键短按进下一层、右键短按退上一层、两端无效；左键命中 cell 时不切换（仅 cell 点击上报）。

- [x] 8.3 验证滚轮前进向深层、后退向表层，`t` 钳制 `[activeLayerIndex, 末层]`、不改 Enable；首层最大向下递减。

- [x] 8.4 验证点击 cell 经 `RingMapEvents` 上报、generator `XLogger.LogInfo` 输出 layerIndex/cellIndex；OnEnable/OnDisable 订阅成对无泄漏。

- [x] 8.5 验证 count 越界钳制与 `XLogger` 警告输出。

- [x] 8.6 验证两个 TextMeshPro：方向指示随切换/浏览更新（向下/向上）；状态指示显示激活层与显示层数。

- [x] 8.7 运行 `openspec validate` 校验变更产物一致性。


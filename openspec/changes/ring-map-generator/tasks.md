## 1. 多层配置

- [ ] 1.1 新建 `RingLayerConfig`（`[Serializable]` 结构）：字段 `count`(int, 钳制 0..8)、可选 `sizeOverride`/`directionOverride`/`staggerOffset`。
- [ ] 1.2 新建 `RingMapConfigSO`（ScriptableObject，`Tools.UI.Ring`）：`List<RingLayerConfig> layers`、`int maxSlots=8`、`Vector2 minMaxStagger`、`AnimationCurve layerSizeCurve`（随 index 递减）、`AnimationCurve alphaByDistanceCurve`、`AnimationCurve progressScaleCurve`、`bool requireLongPress=true`、`float holdDuration`、`float scrollSpeed`、cell 默认尺寸、Enable/Disable 默认贴图。
- [ ] 1.3 为 `RingMapConfigSO` 加 `[CreateAssetMenu]`，提供编辑器存盘/读盘方法（参考现有 `RingConfigSO` 模式与 `[Button]`）。

## 2. 布局引擎改造（8 槽固定网格）

- [ ] 2.1 在 `UiRingLayout` 中将 `angleStep` 改为固定 `360 / maxSlots`（maxSlots 默认 8），移除按 count 均分逻辑。
- [ ] 2.2 新增 `startSlot`/`startAngleOffset` 字段，count 个 cell 占据槽 `[startSlot, startSlot+count-1] mod maxSlots`（连续相邻填充）。
- [ ] 2.3 count 越界钳制到 `[0, maxSlots]`，越界时 `XLogger.LogWarning`。
- [ ] 2.4 保留现有本地对象池与编辑器 `saveToConfig`，确保单环测试场景可继续工作（行为变为 8 槽网格）。

## 3. Cell 组件与事件总线

- [ ] 3.1 新建 `RingCell`（`Tools.UI.Ring`，挂在 `MapCell` 预制体）：需 `Button` 组件、`Image _image`、`Sprite enableSprite`/`disableSprite`、`float cellSize`/`direction`、`int layerIndex`/`cellIndex`（生成时回填）。
- [ ] 3.2 实现 `SetEnable(bool active)`：切换 `_image.sprite`；暴露 `ApplySizeAndDirection()` 应用 sizeDelta 与 localEulerAngles。
- [ ] 3.3 新建静态事件总线 `RingMapEvents`：`public static event Action<int,int> OnCellClicked;`（参数 layerIndex, cellIndex）。
- [ ] 3.4 `RingCell` 的 `Button.onClick` → `RingMapEvents.OnCellClicked?.Invoke(layerIndex, cellIndex)`（cell 只喊，不持上游引用）。
- [ ] 3.5 升级 `MapCell.prefab`：挂 `RingCell` + `Button`，把现有 `Image` 子物体引用接入，赋值 Enable/Disable 贴图字段。

## 4. Layer 组件

- [ ] 4.1 新建 `RingLayer`（`Tools.UI.Ring`）：持有 `UiRingLayout` 引用、`bool isActive`、`float layerSize`/`layerDirection`/`staggerOffset`、`int layerIndex`、`int baseSlotCount`。
- [ ] 4.2 实现 `SetVisual(float t, int activeLayerIndex, AnimationCurve layerSizeCurve, AnimationCurve alphaByDistanceCurve)`：`scale = layerSizeCurve.Evaluate(layerIndex)`（随 index 递减）；`alpha = alphaByDistanceCurve.Evaluate(|t-layerIndex|)`；`SetEnable(layerIndex == activeLayerIndex)`。
- [ ] 4.3 实现 `SetEnable(bool active)`：遍历 cell 调 `RingCell.SetEnable(active)`；同步 `isActive`。
- [ ] 4.4 生成时把每个 cell 的 `layerIndex`/`cellIndex` 回填（供事件上报）。

## 5. 整图控制器与总线订阅

- [ ] 5.1 新建 `RingMapGenerator`（`Tools.UI.Ring`，`[RequireComponent(RectTransform)]`）：持有 `RingMapConfigSO`、`List<RingLayer> layers`、`float browseT`（浏览焦点）、`int activeLayerIndex`。
- [ ] 5.2 `Generate()`：按配置实例化各 `RingLayer`，把每层 `count`/`staggerOffset`/size/direction 注入 `UiRingLayout` 与 `RingLayer`；回填 cell 的 layerIndex/cellIndex。
- [ ] 5.3 `Update()`：每帧把 `browseT` 与 `activeLayerIndex` 广播给各层 `SetVisual`，刷新 Enable 双态。
- [ ] 5.4 `OnEnable` 订阅 `RingMapEvents.OnCellClicked += handleCellClicked`；`OnDisable` 取消订阅（成对，防泄漏）。
- [ ] 5.5 `handleCellClicked(int l, int c)`：`XLogger.LogInfo($"cell clicked layer={l} index={c}")`（统一处理入口，后续扩展路由）。
- [ ] 5.6 `AdvanceLayer(int dir)`：`activeLayerIndex = Clamp(activeLayerIndex+dir, 0, layers.Count-1)`；`browseT` 钳到新 `[activeLayerIndex, 末层]`；触发根 `progressScale` Tween。
- [ ] 5.7 提供编辑器 `[Button]` 重新生成与把运行时参数写回 SO（参考 `UiRingLayout.saveToConfig`）。

## 6. 交互（InputSystem 左右键 + 滚轮）

- [ ] 6.1 新建 `RingMapInteraction`（`Tools.UI.Ring`）：持有 `RingMapGenerator` 引用、`Image progressBar`、`AnimationCurve progressScaleCurve`、从 config 读 `requireLongPress`/`holdDuration`/`scrollSpeed`。
- [ ] 6.2 左键提交 +1（InputSystem `Mouse.current.leftButton`）：`requireLongPress=true` 时按住累积进度，充满调 `AdvanceLayer(+1)` 并复位、循环；`false` 时按下沿（或单击）即时 `AdvanceLayer(+1)`；最底层无效。
- [ ] 6.3 右键提交 -1（`Mouse.current.rightButton`）：同 6.2 逻辑，`AdvanceLayer(-1)`；最上层无效。
- [ ] 6.4 滚轮浏览（`Mouse.current.scroll.ReadValue().y`）：改变 `browseT`，钳制到 `[activeLayerIndex, 末层]`；不改 `activeLayerIndex`。
- [ ] 6.5 提交/浏览期间按 `progressScaleCurve` 对根节点 scale 做 Tween，实现放大/缩小。
- [ ] 6.6 进度条仅 `requireLongPress=true` 时显示与填充，松手回退。

## 7. 场景接入

- [ ] 7.1 在 `SampleScene` 的 `MapUI` 下搭建 `RingMapGenerator` 根节点，挂载配置 SO。
- [ ] 7.2 接入进度条 Image 与 `RingMapInteraction`。
- [ ] 7.3 Play 模式验证：生成多层、8 槽连续相邻填充、层间错开、向下纵深 baseScale/alpha、激活层 Enable、左右键进退、滚轮浏览。

## 8. 验证

- [ ] 8.1 验证满层(8)与部分层(3/5)布局正确，层间存在可见错开且允许部分重叠。
- [ ] 8.2 验证左键进下一层、右键退上一层、两端无效；单击模式（`requireLongPress=false`）即时提交；hold 模式充满提交并循环、松手回退。
- [ ] 8.3 验证滚轮浏览 `t` 限制在 `[activeLayerIndex, 末层]`、不改 Enable；首层最大向下递减。
- [ ] 8.4 验证点击 cell 经 `RingMapEvents` 上报、generator `XLogger.LogInfo` 输出 layerIndex/cellIndex；OnEnable/OnDisable 订阅成对无泄漏。
- [ ] 8.5 验证 count 越界钳制与 `XLogger` 警告输出。
- [ ] 8.6 运行 `openspec validate` 校验变更产物一致性。

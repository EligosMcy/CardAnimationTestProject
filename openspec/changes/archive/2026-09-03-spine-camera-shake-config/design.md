# 相机晃动 + 参数收敛 ScriptableObject — 技术设计

## Context

`spine-battle-cinematic` 系列变更当前实现:打击节拍瞬时挂载放大+双定格+组/背景放大+幕布;表现窗口(当前 `_presentationDuration=0.6s`)内 FoucsAnchorGroup 向左平移 + 演出对微晃动;恢复节拍定格收缩回 Home,回位完成后才续播。实机调整后用户决定:平移冲击与演出对晃动不再需要(场景中已置 0 停用),冲击感改为**相机晃动**;并把 Director 与 BattleStage 上分散的数值参数收敛到单一 `SpineBattleSettings` ScriptableObject,默认值=当前场景数据,单文件调整个演出。

当前场景关键值(实测):节拍 `_attackStartTime=0.7/_attackFreezeFrame=0.75/_hitFrameTime=0.3`、`_presentationDuration=0.6`、`_returnDuration=0.15`、`_enableDebugKeys=1`;Stage `_focusScaleMultiplier=2/_groupScaleMultiplier=1.1`;平移/晃动参数已为 0/停用。

## Goals / Non-Goals

**Goals:**

- 以相机晃动替代组平移冲击与演出对微晃动,冲击期观感集中到相机。
- Director/Stage 全部数值/开关/名称参数收敛到单一 SO,运行期只读,单资产文件整体调参。
- 资产默认值 = 当前场景数据;演出其余机制(双定格、组/背景放大、幕布、窗口定时、回位后续播、双条件收尾)不变。

**Non-Goals:**

- 不做多套演出配置的运行时热切换(本期单资产)。
- 不做相机运镜/移动的持续动画(仅窗口期晃动并还原)。
- 不把场景对象引用(演出对/锚点/相机/幕布)搬进 SO。
- 不做编辑器自动化验收(用户人工调参)。

## Decisions

### D1 移除平移与演出对晃动,新增相机晃动(执行仍归 Stage)

删除 BattleStage 中 `_focusGroup`/平移参数字段与 `shiftGroupToLocal`/`shakePerformerCoroutine`/`applyShakeToPerformer` 等逻辑;`SnapFocusIn` 不再记录组基准、不再调用 `resetGroupToBase`(组不再被移动);恢复序列不再"组回原位"。新增:`[SerializeField] Transform _demoCamera`(场景 BattleDemo Camera);`StartCameraShake()`(打击节拍由 Director 调用)在表现窗口内以演出前相机基准为轴叠加噪声位移/旋转;`StopCameraShake()`(恢复节拍回位序列开头)停止并把相机精确还原基准。相机基准在 `SnapFocusIn`(或 shake 启动)时记录一次。备选:晃动放在 Director/独立组件——相机是变换对象,归 Stage(变换效果执行)保持分层,Director 只下指令。

### D2 单一配置源 SpineBattleSettings(ScriptableObject)

新增 `SpineBattleSettings : ScriptableObject`,字段分组:节拍参数(`AttackStartTime`/`AttackFreezeFrame`/`HitFrameTime`)、表现与回位(`PresentationDuration`/`ReturnDuration`)、缩放(`FocusScaleMultiplier`/`GroupScaleMultiplier`)、相机晃动(`CameraShakeAmplitude`/`CameraShakeSpeed`,位移/旋转幅度),以及 `AttackClipName`、`EnableDebugKeys`。字段默认值字面量 = 当前场景实测值(0.7/0.75/0.3/0.6/0.15/2/1.1/…)并加 `[CreateAssetMenu]`。Director 与 BattleStage 各保留 `[SerializeField] SpineBattleSettings _settings`,把原数值字段引用点改为 `_settings.X`;`tryValidateRefs` 增加 `_settings == null` 报错。Scene 引用(PairSetup/Stage/Canvas/Camera/Transforms)仍留在 MonoBehaviour。备选(两组件各持一份内联参数并"导出/同步"SO)会产生双份真相,否决。

### D3 相机晃动参数语义

`CameraShakeAmplitude`(位移幅度,世界单位,默认建议 ~0.03~0.06 以观感精调)、`CameraShakeSpeed`(噪声频率因子,默认 ~25)。晃动实现沿用时间噪声(Perlin),对 `_demoCamera.localPosition` 做微小偏移并在结束时还原记录基准(含旋转若启用:本期仅位移晃动,旋转幅度参数预留 `CameraShakeRotationAmplitude`,默认 0)。表现窗口结束即停晃,窗口语义与续播/回位时序不变(`fix-spine-resume-after-return` 的回位完成后续播保持)。

### D4 迁移与接线(默认值=场景数据)

1. 新增 `SpineBattleSettings.cs` 与 `SpineBattleCinematicSettings.asset`(数值按上述场景实测写死为默认,后续单文件调)。
2. `BattleStage`/`SpineBattleDirector` 移除旧数值字段(场景重存自动清理旧序列化),改挂 `_settings`;Stage 加 `_demoCamera` 接线。
3. Director 打击节拍:`_stage.StartCameraShake()`;恢复节拍回位序列:`RecoverPresentation` 开头 `StopCameraShake()` + ZoomGroupOut + ReturnHome(不再有组回位段)。
4. 编译 + 用户人工按 B 用单资产文件调参验收。

## Risks / Trade-offs

- [Screen Space-Camera 的 UI(幕布/背景)随相机晃动同抖] → 属预期冲击氛围;若需幕布不抖,后续可改 Overlay 或分离相机,本期接受。
- [SO 数值与历史场景内联值出现双真相过渡期] → 迁移期一次性写入资产并移除组件字段,场景保存后无内联残留;资产为唯一源。
- [相机晃动幅度过大导致取景偏移出画面] → 幅度默认极小,用户单文件精调;基准还原保证每轮起点一致。
- [关联变更堆叠(平移/晃动相关旧 delta 未归档)] → 本 delta 以 REMOVED 显式废除;归档顺序需在相关变更之后,否则合并报缺(见 Open Questions)。
- [单一 SO 共享给两组件后,某一字段误改影响面扩大] → 单文件调参正是诉求;字段分组+Tooltip 缓解误改。

## Migration Plan

增量:新增 SO 类与资产 → 改 Director/Stage 引用与读值 → 删除平移/晃动逻辑与字段 → 场景接线 `_settings`/`_demoCamera` 并保存清理 → 用户单文件人工调参与目视验收。回滚 git revert(资产与场景一并还原)。

## Open Questions

- 相机晃动是否仅位移(默认),还是需要可选旋转幅度(字段预留)。
- 相机晃动是否也覆盖"回位收缩"段(当前设计:窗口结束即停,回位期相机保持基准)。
- REMOVED 需求依赖先前变更归档顺序;若用户希望先归并本变更到能力主规格,需先 `/opsx-sync` 相关前置变更(规划产物,不涉及代码)。

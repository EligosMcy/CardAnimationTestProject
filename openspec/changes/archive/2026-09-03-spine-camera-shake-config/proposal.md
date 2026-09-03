# 相机晃动 + 参数收敛 ScriptableObject(spine-camera-shake-config)

## Why

演出冲击表现经实机验证后需调整:移除"FoucsAnchorGroup 整体左移的平移冲击"与"演出对自身微缩放晃动"(当前场景中相关参数已被置 0/停用),改为**表现窗口内对演示相机做晃动**来承载打击冲击感。同时,SpineBattleDirector 与 BattleStage 的数值/开关参数散落在两个组件 Inspector 上,调试整个演出需在两处反复修改;期望把两者全部可调参数收敛到**单一 ScriptableObject 资产**,默认值与当前场景数据一致,之后只改这一个文件即可整体调演出。

## What Changes

- **移除平移冲击与演出对晃动**:删除 BattleStage 的 FoucsAnchorGroup 平移(`_groupShiftDistance/_groupShiftDuration/_groupShiftCurve`、组平移协程)与演出对微晃动(`_shakeOffsetAmplitude/_shakeScaleAmplitude/_shakeSpeed/_shakeStartWithShift`、晃动协程)逻辑;`_focusGroup` 引用不再需要。
- **新增表现窗口相机晃动**:打击节拍后、表现窗口期间,由 Stage 对演示相机(BattleDemo Camera)施加以"演出前相机基准"为中心的小幅位移/旋转噪声晃动;恢复节拍(回位序列)时停止晃动并把相机精确还原到基准。参数(幅度/速度等)可调。
- **参数收敛为 ScriptableObject**:新增 `SpineBattleSettings : ScriptableObject`(命名空间 `SpineTest.Battle`),收纳 Director 与 BattleStage 当前全部**数值/布尔/名称/曲线**类可调参数(节拍时间、表现窗口与回位时长、缩放倍率、相机晃动参数、攻击 clip 名、调试开关等);Director 与 BattleStage 各自持有 `[SerializeField] SpineBattleSettings _settings`,运行期只读 SO(未赋值启动报错并中断)。Transform/演出对等**场景对象引用仍留在 MonoBehaviour**(SO 只装数据不装场景引用)。
- **默认值 = 当前场景数据**:创建资产 `Assets/SpineTest/Battle/Settings/SpineBattleCinematicSettings.asset`,数值与现场景一致(实测:节拍 0.7/0.75/0.3、表现窗口 0.6、回位 0.15、焦点倍率 2、组倍率 1.1、调试开);旧"平移/晃动"字段不迁移。
- 保留不变:双定格 + 瞬时挂载放大、SpiderGroup/background 轻微放大、Forward UI 幕布、表现窗口定时恢复、定格收缩回 Home、回 Home 完成后续播、收尾双条件。

## Capabilities

### New Capabilities

<!-- 无新能力 -->

### Modified Capabilities

- `spine-battle-cinematic`:
  - 移除"表现窗口内 FoucsAnchorGroup 冲击平移"与"演出对微缩放晃动(时机可配置)"的既有表现,改为"表现窗口内相机晃动(以演出前相机为基准,结束后精确还原)"。
  - 新增"演出参数单一配置源":全部数值/开关类演出参数由唯一 `SpineBattleSettings` 资产提供,Director/Stage 运行期读取该资产;组件内不再散落重复参数。

## Impact

- 代码:`Assets/SpineTest/Battle/Scripts/` 新增 `SpineBattleSettings.cs`(ScriptableObject + CreateAssetMenu);`BattleStage.cs` 删平移/晃动,新增相机晃动执行与相机基准还原;`SpineBattleDirector.cs` 参数字段替换为 `_settings` 引用并按需改读值点。
- 资产:新增 `Assets/SpineTest/Battle/Settings/SpineBattleCinematicSettings.asset`;场景 `SpiderBattleCinematic.unity` 接线 `_settings` 与相机引用,旧数值字段随重存清理。
- 依赖:不新增第三方;沿用 SpineTest.Battle 命名空间与 XLogger。
- 验证:用户人工在 Unity 中按 B 观察相机晃动观感与各参数调整(单文件 SO 调参);无自动化编辑器校验步骤。
- 关联变更:基于 `spine-battle-cinematic`、`fix-spine-second-round-scale`、`fix-spine-resume-after-return` 的实现状态推进。

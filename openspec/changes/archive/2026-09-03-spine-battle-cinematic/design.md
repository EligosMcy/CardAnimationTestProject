# Spine 多人战斗演出 — 技术设计

## Context

场景 `SpiderBattleCinematic.unity` 已含:`SpiderGroup`(SpiderA~D,各挂 SpineActor + 骨骼)、`HomeAnchorA~D`(HomeAnchorGroup)、`FocusAnchorA/B`(FoucsAnchorGroup,受击/攻击特写位)、`background`(BackUI 幕布下)、'--- Forward UI'(Screen Space-Camera 幕布 Canvas)。代码已实现"双定格 + 瞬时聚焦 + 缓退到退场点(ExitAnchor)+ 缩小回位"版本(含 `StartRetreat`/`OnRetreatCompleted`/Exit 锚点引用),规划产物也按该版本刚修订过。

新一轮修订:移除"缓退到 ExitAnchor"的运动;表现改为——打击节拍双定格、瞬时挂载 FocusAnchor 放大、SpiderGroup/background 轻微放大、幕布开;随后进入**表现窗口**(`_presentationDuration`,默认 1.0s,即"定格一秒扩大"):因攻击方在右冲击左侧受击方,整个 `FoucsAnchorGroup` 以缓动向左平移(距离/时长可调),期间两只蜘蛛在锚点局部叠加微缩放与晃动(晃动时机由 bool `_shakeStartWithShift` 控制:随组移动即晃 / 组移动完成后再晃);窗口结束 = 恢复节拍:解除双定格常速续播、组回原位、组/背景复原、幕布关、缩小回 Home 缓存位并还原。`ExitAnchorA/B` 与 `ExitAnchorGroup` 废弃并从场景删除。Spine 资产:Attack1≈1.33s、Hit≈0.50s、Idie=4s 循环(拼写 `Idie`);`Spider.json` 带 `AttackStart`/`AttackEnd` 事件而攻击用 `Spider_Corrupted.json` 暂无事件,主时钟维持 clip 时间轮询;场景现调 `_attackStartTime=0.7`、`_hitFrameTime=0.3`。

## Goals / Non-Goals

**Goals:**

- 四角色启动摆位与待机由代码保证。
- 演出对枚举可配置(Pair_AB / Pair_CD),换对零逻辑改动。
- 打击瞬间双定格 + 瞬时挂载放大 + 组/背景放大 + 幕布开,无退场点位移。
- 表现窗口内:定格保持 1 秒(`_presentationDuration`),FoucsAnchorGroup 向左缓动(`_groupShiftDistance`/`_groupShiftDuration`),演出对微缩放晃动(幅度/速度可调,`_shakeStartWithShift` 控制随动/滞后)。
- 窗口结束统一恢复:解除定格续播、组回原位、缩放复原、缩小回 Home、Ready 双条件。
- 观感参数全部 Inspector 可调;编辑器内人工微调与目视验收由用户完成,不设自动化编辑器校验步骤。

**Non-Goals:**

- 不做同屏 2v2 双对决(现仅一套受击/攻击特写锚点)。
- 不做 UI Canvas/UISkeletonGraphic、不做相机动画(位置/旋转/FOV 不变)、不用 `Time.timeScale`。
- 不做基于 TimeScale 的慢放、不做 Attack2 时间组(预留 SO)、特效/音效、连段、血量。
- 不修改 `DefaultSpine.cs` 与既有模块。
- 不提供自动化编辑器验收/调参脚本(用户自行在 Play 中微调)。

## Decisions

### D1 渲染层:世界空间 SkeletonAnimation(沿用)

沿用世界空间方案;Actor 封装 Spine,Director/Stage 不感知 Spine API。

### D2 层级挂载式瞬时聚焦 + 组平移驱动(取代"退场点缓退")

打击节拍不做补间拉近,演出对单帧挂载到 FocusAnchor;表现期不再移动角色到退场点,而是**整体平移 FoucsAnchorGroup** 制造冲击推镜感。执行序列(顺序有约束):

1. Stage 记录演出对两角色当前父级与本地 TRS(Home 态缓存,一轮内有效);记录 `FoucsAnchorGroup` 本地位置作为组基准位。
2. 对两只蜘蛛 `SetParent(对应 FocusAnchor, worldPositionStays: true)`、本地位置归零贴齐锚点、`localScale × _focusScaleMultiplier`(乘性,保留镜像负 X)。
3. 同帧再对 SpiderGroup/`background` 的 localScale `× _groupScaleMultiplier`(先挂载后放大组,防演出对双重放大)。
4. Director 打开 Forward UI 幕布,进入表现窗口。

表现窗口期由 Stage 执行:`FoucsAnchorGroup` 以补间曲线沿本地 X 负向平移 `_groupShiftDistance`(默认左移量,历时 `_groupShiftDuration`),结束后如需回位再反向缓动;演出对作为 FocusAnchor 子物体随组整体平移,同时叠加自身微晃动。

### D3 双定格(沿用,无退场窗口概念)

- 攻击方跳帧定格 `JumpFreezeAt(_attackFreezeFrame)`:对当前主轨道置 `entry.Time = clipTime` 且 `TimeScale = 0`,随后立即采样该帧(避免闪现上一帧)。
- 受击方 Hit 定格 `FreezeHitAt(_hitFrameTime)`(切 Hit clip、置 Time、`TimeScale=0`)。
- 恢复节拍:双方 `Resume()`(TimeScale=1),自定格帧常速续播。
- 全程不使用 `Time.timeScale`;定格期内动画与整体缩放静止,仅允许组平移与局部微晃动驱动位移/缩放噪声。

### D4 微缩放与晃动(演出对局部,时机可配置)

- 晃动作用于两只蜘蛛自身的 `localPosition`/`localScale`(在 FocusAnchor 局部叠加噪声),与"组平移"解耦:组平移改变世界位置,晃动只做高频小幅扰动。
- 实现:Stage 内部用一周期性的伪噪声(如基于 `Time.time` 的正弦/Perlin 组合)驱动两角色,幅度参数 `_shakeScaleAmplitude`(缩放脉冲)与 `_shakeOffsetAmplitude`(位置抖动),速度参数 `_shakeSpeed`。
- 时机:`_shakeStartWithShift == true` → 表现窗口开始(组开始平移)即晃;`false` → 组平移完成(`_groupShiftDuration` 结束)后再晃,持续至窗口结束。
- 晃动以缓存基准(挂载后本地位置/缩放)为中心做微小乘性/加法扰动,结束时归零,不破坏回位计算。

### D5 表现窗口与恢复节拍(定时驱动)

- 表现窗口时长 = `_presentationDuration`(Director 参数,默认 1.0s),自打击节拍起计时;到点即触发恢复节拍(由 Director 的定时协程或 Stage 上报完成,二选一实现,须单次触发守卫)。
- 组平移时长 `_groupShiftDuration` 与晃动窗口是表现窗口的子集:若 `_groupShiftDuration + (滞后晃动延时)` 超出窗口,以窗口结束为准截断。
- 恢复节拍动作序列:双方 `Resume()` → 幕布关 → Stage 执行"组回原位 + 组/背景复原 + 缩小回 Home 缓存位(还原父级与本地 TRS)"(Stage `ReturnHome` 逻辑沿用:以 FocusAnchor 局部坐标把当前点插值回 Home 目标并缩放回落,结束后还原快照)。

### D6 演出对枚举与引用(沿用)

Director 持有 `SpineBattlePair` 与两组 `PairSetup`(Defender/Attacker);当前对驱动节拍;Stage 方法一律接收 Defender/Attacker 的 Transform,不出现硬编码角色名。锚点按角色位语义复用:FocusAnchorA/B = 受击位/攻击位;Pair_CD 时 C→受击位、D→攻击位。

### D7 脚本三层拆分(沿用 3 文件)

| 脚本 | 唯一职责 | 持有 Update | 依赖 |
|---|---|---|---|
| `SpineActor`(每蜘蛛) | 播放/跳帧定格/冻结/恢复/回 Idle,隔离 Spine API | 无 | SkeletonAnimation |
| `SpineBattleDirector` | B 输入、状态机、演出对、打击节拍触发与表现窗口计时、恢复节拍调度、幕布、收尾双条件 | 有(输入 + 前摇轮询 + 窗口计时) | Actor×当前对、Stage、幕布 |
| `BattleStage` | Home 摆位、瞬时挂载、FoucsAnchorGroup 平移、微缩放晃动、组/背景缩放、缩小回位与完成回调 | 有(插值/晃动推进) | 4 角色 + 锚点/组/背景 Transform |

Director 持有节拍/时长/晃动时机参数;Stage 持有空间配置(倍率、平移距离/时长、晃动幅度/速度、锚点与组引用)。

### D8 触发与收尾

- 打击节拍:前摇轮询攻击方 `AnimationTime` 首次 `>= _attackStartTime`(单次守卫);触发后攻击方定格,不再轮询 clip。
- 恢复节拍:表现窗口(`_presentationDuration`)到时驱动(定时器/Stage 完成上报),单次守卫。
- 收尾双条件:攻击方动画 `Complete`(经 Actor C# 事件)与 Stage 回位完成回调均达成 → 当前对 `PlayIdle()`、回 Ready;两种先后都要处理。
- 若将来攻击资产补 `AttackStart` 事件,打击节拍可切事件驱动(本期维持轮询)。

### D9 参数语义(场景现调值另注;观感值由用户人工精调)

| 参数 | 默认/现调 | 语义 |
|---|---|---|
| `_attackStartTime` | 0.7(场景) | AttackStart 触发点 → 打击节拍 |
| `_attackFreezeFrame` | 0.75 | 攻击方跳转定格帧(扑击到位/命中帧) |
| `_hitFrameTime` | 0.3(场景) | 受击方 Hit 定格帧 |
| `_presentationDuration` | 1.0(新增) | 表现窗口/定格时长,到时触发恢复节拍(替代原退场时长) |
| `_returnDuration` | 0.4 | 缩小回 Home 时长 |
| `_focusScaleMultiplier` | 2 | 演出对挂载放大倍率(乘性) |
| `_groupScaleMultiplier` | 1.1 | SpiderGroup/background 轻微放大倍率(MUST < 焦点倍率) |
| `_groupShiftDistance` | 0.3(新增) | FoucsAnchorGroup 向左平移距离(正=左移),缓动曲线 |
| `_groupShiftDuration` | 0.35(新增) | 组平移历时 |
| `_shakeScaleAmplitude` | 0.03(新增) | 演出对微缩放脉冲幅度 |
| `_shakeOffsetAmplitude` | 0.02(新增) | 演出对位置抖动幅度 |
| `_shakeSpeed` | 25(新增) | 晃动频率/速度 |
| `_shakeStartWithShift` | true(新增) | true=组移动即晃;false=组移动完成后再晃 |

已废弃:`_retreatDuration`(退场语义)、ExitAnchor 相关字段/对象(场景删除)。

## Risks / Trade-offs

- [组平移与演出对晃动的坐标系叠加(晃动需以锚点局部为基准,避免随组平移被二次放大)] → 平移作用于 FoucsAnchorGroup,晃动只写演出对自身 localPosition/localScale 的小幅扰动,两者互不叠加。
- [晃动幅度过大破坏"定格"观感或与回位换算冲突] → 幅度默认极小(≈0.02~0.03),回位以缓存快照/锚点局部目标换算,晃动结束时归零。
- [`_groupShiftDuration` + 滞后晃动超过表现窗口导致截断] → 窗口结束时统一截断并进入恢复,参数语义文档化。
- [镜像负 X / 朝向在挂载/晃动/还原中被破坏] → 缩放一律对基准做乘性小扰动,结束时整组还原缓存 TRS。
- [攻击方动画结束与回位完成先后不定] → 收尾双条件化(D8)。
- [场景曾存在两份 BattleStage 组件与 Exit 对象] → 保留 Director 引用的一份;ExitAnchorGroup/ExitAnchorA/B 删除并清理引用。
- [演出对切 Pair_CD 未接线] → `tryValidateRefs` 全量校验当前对与四组 Home 映射,缺失即报错。
- [URP 17 下 Spine 材质异常] → 已核对使用 Spine URP 变体(前置任务完成)。
- [观感参数多、需要 Play 内反复试] → 全部 Inspector 可调;调参由用户人工完成,无自动化编辑器校验步骤。

## Migration Plan

代码/场景在"退场点"版基础上做增量迁移,无存量数据:删除 Stage 的退场逻辑与 Exit 字段、删除场景 ExitAnchorGroup/ExitAnchorA/B → 新增 FoucsAnchorGroup 平移/晃动字段与协程 → Director 恢复节拍改表现窗口定时并驱动新 Stage 接口 → 场景接线新字段 → Play 人工验证与调参(用户执行)。回滚:还原脚本/场景(git revert);`DefaultSpine.cs` 不动。

## Open Questions

- 同屏 2v2 双对决是否需要(本期不做)。
- 组平移方向目前固定"左移"(正值);若内容出现反向冲击(左方攻击右方),是否需要方向参数化(可改为有符号距离)。
- 晃动用正弦/Perlin 的形态与幅度初值(0.02~0.03)是否合适,由用户 Play 精调。
- `_groupShiftDuration`/晃动滞后与 `_presentationDuration` 的关系(是否在窗口内截断)待实机确认。
- 是否给攻击资产补充 AttackStart 事件以切换事件驱动(受击资产已带,可参照)。
- 是否引入 `SpineBattleTimingSO` 收纳多招式时间组(本期字段直配)。

# 任务清单:spine-battle-cinematic

## 1. 前置核对与目录

- [x] 1.1 核对 `Assets/SpineTest/Spine/Spider/` 下 Spider 与 Spider_Corrupted 的 Material 使用 Spine URP shader(URP 17 项目),异常则记录并在实施中修复导入设置
- [x] 1.2 建立 `Assets/SpineTest/Battle/Scripts` 目录,确认与既有 SpineTest 命名空间组织一致(建议 `SpineTest.Battle`)

## 2. SpineActor(角色动画驱动,沿用,现代码已具备)

- [x] 2.1 字段:`SkeletonAnimation` 引用、Idle/Hit/Attack 三个 clip 名字符串(注意资产拼写 `Idie`)
- [x] 2.2 公开接口:`PlayIdle()`、`PlayAttack(string)`、`FreezeHitAt(float)`、`Resume()`、`SetTimeScale(float)`、只读 `CurrentAnimationTime`
- [x] 2.3 `JumpFreezeAt(float clipTime)`:对当前主轨道置 `entry.Time = clipTime` 且 `TimeScale = 0`,用于攻击方跳帧定格
- [x] 2.4 冻结/跳帧后骨架立即采样该帧姿态,不闪现上一帧姿态
- [x] 2.5 内部订阅 Spine `AnimationState.Complete`(非循环轨道),上抛 `OnAnimationEnded`,Actor 不持有 Director 引用
- [x] 2.6 空引用与 clip 名校验 + XLogger(遵循项目规范,无 `?.` 链)

## 3. BattleStage(重构:移除退场点,新增组平移与演出对晃动)

- [x] 3.1 移除退场逻辑:删除 `StartRetreat`/`retreatCoroutine`/`OnRetreatCompleted`/`raiseRetreatCompleted` 及 ExitAnchor 序列化字段、快照引用与相关校验
- [x] 3.2 新增字段:`_focusGroup`(场景 FoucsAnchorGroup)Transform 引用、`_groupShiftDistance`(默认 0.3,正=左移)、`_groupShiftDuration`(默认 0.35)、平移缓动曲线;`Start`/首轮挂载时记录组基准本地位置
- [x] 3.3 实现组平移:表现窗口开始后按 `_groupShiftDuration` 将 FoucsAnchorGroup 本地 X 向左平移 `_groupShiftDistance`(缓动);提供"组回原位"方法供恢复节拍调用(按同曲线回基准位)
- [x] 3.4 实现演出对微晃动:基于时间噪声对两演出角色本地位置/缩放叠加 `_shakeOffsetAmplitude`(0.02)/`_shakeScaleAmplitude`(0.03)/`_shakeSpeed`(25) 的小幅扰动;`_shakeStartWithShift == true` 时组一开始平移即晃,false 时组平移完成后才晃;表现窗口结束(恢复节拍)时归零停止
- [x] 3.5 恢复执行序列保留并串联:组回原位 → `ZoomGroupOut()`(SpiderGroup/background 复原)→ `ReturnHome`(从当前位缩小插值回 Home 缓存目标,结束还原缓存父级与本地 TRS,上抛回位完成回调)
- [x] 3.6 保留:Home 摆位映射与 `PlaceHome()`、`SnapFocusIn()`(挂载贴齐 + 乘性放大)、`ZoomGroupIn()/Out()`、快照还原、回位完成回调

## 4. SpineBattleDirector(编排与节拍)

- [x] 4.1 状态枚举与流转:Ready / Windup / Strike(双定格 + 表现窗口)/ Recover,手写 switch
- [x] 4.2 B 键输入(InputSystem)与 Ready 守卫:仅 Ready 态接受触发
- [x] 4.3 参数字段(现具备):`_attackStartTime`、`_attackFreezeFrame`、`_hitFrameTime`、`_attackClipName`、幕布 Canvas
- [x] 4.4 演出对枚举 `SpineBattlePair`(Pair_AB / Pair_CD)与两组 `PairSetup`(Defender/Attacker),运行时按枚举取值,无硬编码角色名
- [x] 4.5 参数调整:删除 `_retreatDuration` 字段/守卫/退场相关调用;新增 `_presentationDuration`(默认 1.0s,须 > 0)与 `_returnDuration`(0.4s)校验
- [x] 4.6 打击节拍:现 JumpFreezeAt + FreezeHitAt + SnapFocusIn + ZoomGroupIn + 幕布开保持不变;将原 `StartRetreat` 调用替换为"启动 Stage 表现(组平移 + 晃动,下发表现窗口时长)",并启动窗口计时
- [x] 4.7 恢复节拍:改为**表现窗口到时**(定时/Stage 完成上报,单次守卫)驱动——`Resume()`×2、Stage 组回原位 + `ZoomGroupOut` + `ReturnHome`、幕布关;移除对"退场完成回调"的依赖
- [x] 4.8 收尾双条件:攻击方动画结束事件 + Stage 回位完成回调均达成 → 当前对 `PlayIdle()`、状态回 Ready(处理先后到达)
- [x] 4.9 引用校验(当前对 + 幕布)与全流程不使用 `Time.timeScale`

## 5. 场景接线与人工调参(SpiderBattleCinematic.unity)

- [x] 5.1 场景已含 SpiderA~D(各挂 SpineActor + 骨骼)、演示相机(固定)、HomeAnchorA~D、FocusAnchorA/B(FoucsAnchorGroup 下)、SpiderGroup、background、Forward UI 幕布;仅保留一份 BattleStage 组件(Director 引用处)
- [x] 5.2 删除场景 `ExitAnchorGroup`/`ExitAnchorA/B`,清理对 Exit 的序列化残留(Stage 组件不再引用)
- [x] 5.3 Stage 接线:`_focusGroup`=FoucsAnchorGroup、4 组 Home 摆位映射(SpiderA~D ↔ HomeAnchorA~D)、SpiderGroup/background 引用、倍率与平移/晃动参数初值(`_focusScaleMultiplier=2`、`_groupScaleMultiplier=1.1`、`_groupShiftDistance=0.3`、`_groupShiftDuration=0.35`、`_shakeScaleAmplitude=0.03`、`_shakeOffsetAmplitude=0.02`、`_shakeSpeed=25`、`_shakeStartWithShift=true`)
- [x] 5.4 Director 接线:Pair_AB=SpiderA(受击)/SpiderB(攻击)、Pair_CD=SpiderC(受击)/SpiderD(攻击)、幕布引用、`_attackStartTime=0.7`、`_attackFreezeFrame=0.75`、`_hitFrameTime=0.3`、`_presentationDuration=1.0`、`_returnDuration=0.4`

> 观感参数精调(平移距离/缓动、晃动幅度/速度/时机、定格窗口、双定格帧)与目视验收由用户在编辑器中人工完成,不设自动化编辑器校验步骤。

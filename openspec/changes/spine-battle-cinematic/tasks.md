# 任务清单:spine-battle-cinematic

## 1. 前置核对与目录

- [ ] 1.1 核对 `Assets/SpineTest/Spine/Spider/` 下 Spider 与 Spider_Corrupted 的 Material 使用 Spine URP shader(URP 17 项目),异常则记录并在实施中修复导入设置
- [ ] 1.2 建立 `Assets/SpineTest/Battle/Scripts` 目录,确认与既有 SpineTest 命名空间组织一致(建议 `SpineTest.Battle`)

## 2. SpineActor(角色动画驱动,每角色一份)

- [ ] 2.1 实现 `SpineActor` 字段:`SkeletonAnimation` 引用、Idle/Hit/Attack 三个 clip 名字符串(注意资产拼写 `Idie`)
- [ ] 2.2 实现公开接口:`PlayIdle()`、`PlayAttack(string)`、`FreezeHitAt(float)`(SetAnimation(Hit) 后置 `entry.Time` 并 `TimeScale=0`)、`Resume()`、`SetTimeScale(float)`、只读 `CurrentAnimationTime`
- [ ] 2.3 冻结首帧姿态正确:设置 `entry.Time` 后骨架立即采样该帧(必要时手动推进一次动画更新),不闪现初始帧
- [ ] 2.4 内部订阅 Spine `AnimationState.Complete`(非循环轨道),向上抛出 `event`(如 `OnAnimationEnded`),Actor 不持有 Director 引用
- [ ] 2.5 空引用与 clip 名校验:关键路径 null 判断 + XLogger 日志(遵循项目代码规范,无 `?.` 链)

## 3. BattleStage(无相机运镜)

- [ ] 3.1 实现字段:Home/Focus 两组场景锚点(Transform)、聚焦缩放倍率、补间时长与 `AnimationCurve`
- [ ] 3.2 实现 `FocusIn()` / `FocusOut()`:对 A、B 的 Transform 做位置插值 + **乘性** localScale 变化(保留镜像角色的负 X 缩放)
- [ ] 3.3 补间推进逻辑放入 Stage 自身 Update/协程,提供补间完成回调(可选),不感知任何动画时间

## 4. SpineBattleDirector(编排与节拍)

- [ ] 4.1 实现状态枚举与流转:Ready / Windup(前摇)/ Strike(打击)/ Recover(后摇),手写 switch(不引入 Patterns 状态机类)
- [ ] 4.2 B 键输入(InputSystem)与 Ready 守卫:仅 Ready 态接受触发,其余状态忽略按键
- [ ] 4.3 实现三个可调参数字段 `_attackStartTime`、`_attackEndTime`、`_hitFrameTime` 及 `_slowMoScale`(默认 0.25)、攻击 clip 名字段(Attack1)
- [ ] 4.4 打击节拍(首次 `B.AnimationTime >= _attackStartTime`):`ActorB.SetTimeScale(慢放)` + `ActorA.FreezeHitAt(_hitFrameTime)` + `Stage.FocusIn()`,一次性守卫 `_didStrike`
- [ ] 4.5 释放节拍(首次 `>= _attackEndTime`):`ActorB.SetTimeScale(1.0)` + `ActorA.Resume()` + `Stage.FocusOut()`,一次性守卫 `_didRecover`
- [ ] 4.6 收尾:订阅 ActorB 动画结束事件 → A/B `PlayIdle()`、状态回 Ready(可再次触发),不使用全局 `Time.timeScale`

## 5. 演示场景搭建与调参

- [ ] 5.1 搭场景:演示相机(固定)、BattleStageRoot、SpiderA(受击,挂 SpineActor)、SpiderB(攻击,镜像面向 A,挂 SpineActor),A/B 摆出对战间距
- [ ] 5.2 摆放 Home/Focus 锚点(4 个空物体),接线 Director/Actor/Stage 引用与动画 clip 名
- [ ] 5.3 填入估算初值(`_attackStartTime≈0.57`、`_attackEndTime≈0.80`、`_hitFrameTime≈0.12`)并 Play 验证一轮完整演出
- [ ] 5.4 提供临时取数手段(如调试键打印 B 的 AnimationTime),精调三参数至打击节奏符合预期,结束后移除或保留为可开关调试

## 6. 按规格验收

- [ ] 6.1 待机态按 B 触发演出;前摇/打击/后摇期间按 B 不打断、不重触发
- [ ] 6.2 打击节拍慢放生效且 `Time.timeScale` 恒为 1.0;A 定格在 `_hitFrameTime` 帧,释放后从该帧续播 Hit 剩余并回 Idie
- [ ] 6.3 全程相机位置/旋转不变;双人拉近放大后回原位与原缩放,镜像朝向(负 X)不被破坏
- [ ] 6.4 一轮完整结束后 A/B 均循环 Idie、状态回 Ready,可再次按 B;运行时无 Error 级日志

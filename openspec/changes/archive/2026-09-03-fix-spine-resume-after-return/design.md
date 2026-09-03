# 动画续播延迟到回 Home 完成后 — 技术设计

## Context

现行恢复节拍(`SpineBattleDirector.triggerRecoverBeat`)在表现窗口到时执行:`_roundAttacker.Resume()` + `_roundDefender.Resume()` → `Stage.RecoverPresentation(...)`(停止晃动、组回原位、SpiderGroup/background 复原、缩小回 Home)→ 幕布关闭。即"动画续播"与"缩小回位"并行发生。期望时序改为:**回位期间双方保持定格(TimeScale=0),回位完成(`BattleStage.OnReturnHomeCompleted`)后再续播**。`BattleStage` 回位序列终点会先还原缓存父级与本地 TRS、`_isPerforming=false`,再上抛 `OnReturnHomeCompleted`,续播时机挂在此事件上天然满足"还原完成后再播"。

约束:不改 `BattleStage`/`SpineActor`、不改参数;仅调整 Director 调度顺序与语义;收尾双条件保留。

## Goals / Non-Goals

**Goals:**

- 表现窗口结束 → 定格收缩回 Home → 回位完成才解除定格续播。
- 保持既有幕布/组复原/回位时长与双条件收尾语义。

**Non-Goals:**

- 不调整表现窗口/回位时长与定格帧参数。
- 不改 BattleStage 回位实现与事件(已足够)。
- 不做编辑器自动化验收(用户人工按 B 目视)。

## Decisions

### D1 恢复节拍只做"回位动作",不再先行续播

`triggerRecoverBeat` 移除两处 `Resume()`;保留:幕布关、`Stage.RecoverPresentation(...)`;`_phase = Recover` 语义更新为"定格收缩回位(回位完成后将续播)"。备选:在 RecoverPresentation 内续播——破坏"Stage 不感知 Spine 时间"分层,否决。

### D2 续播时机 = Stage 回位完成回调(事件驱动)

`handleReturnHomeCompleted` 在 `_phase == Recover` 且此前未续播(幂等守卫,如 `_didResumeAfterReturn` 或复用 `_attackerAnimationEnded` 前置判断)时,依次 `_roundAttacker.Resume()`、`_roundDefender.Resume()`,再置回位完成标志并 `tryFinishRound()`。攻击方随后续播至 Attack 播完触发 `handleActorAnimationEnded`(Recover 态)置动画结束标志并再次 `tryFinishRound()` → Ready。事件驱动保持 Director 决策、Stage 执行的分层;`SpineActor.Resume()` 幂等(将当前轨道 TimeScale 置 1)。备选:在 Stage 恢复协程尾部调用续播——反向依赖 Spine 时间,否决。

### D3 收尾双条件语义不变,处理事件先后

回位完成后先 `tryFinishRound`(此时动画未结束→不满足),攻击方动画结束后再次 `tryFinishRound` 达成;若极端路径动画已结束标志提前置位(Strike 兜底已触发恢复且回位完成晚于动画?)——定格期动画不可能结束,故 Recover 内 `_attackerAnimationEnded` 只会由续播后的 Complete 置位;仍保留双条件与 `handleActorAnimationEnded` 的 Recover 分支以防后续改动。

### D4 极端路径兜底保留

`handleActorAnimationEnded` 中 `Strike` 态"先补恢复节拍"兜底保留(双定格下攻击方不会自行播完,仅防参数异常);其触发的恢复节拍同样不再立即续播,统一走"回位完成后续播"。

## Risks / Trade-offs

- [回位 0.4s 内动画完全静止,观感依赖"收缩中定格"的表现意图] → 这正是需求;若后续想微调(如回位中允许极缓动画)需另立变更。
- [续播触发仅一次,防止双事件重复 Resume] → `handleReturnHomeCompleted` 内以相位 + 守卫标志保证单次。
- [攻击方续播后摇被拉长使总轮次略增(约 +0.4s)] → 符合"回位后播"语义;时长参数不变,观感由用户微调。

## Migration Plan

在 `fix-spine-second-round-scale` 与 `spine-battle-cinematic` 实现之上增量修改 `SpineBattleDirector.cs`:改 `triggerRecoverBeat`、改 `handleReturnHomeCompleted`、补幂等续播守卫并更新注释/阶段语义;`BattleStage`/`SpineActor` 不动;回滚 git revert。

## Open Questions

- 受击方 Hit 剩余帧较短(约 0.2s),回位后立即续播是否满足观感(需实机确认,不改参数)。
- 是否将"回位完成后续播"的守卫复用现有 `_returnHomeCompleted` 标志(事件回调天然单次,倾向不加新字段,实施时定夺)。

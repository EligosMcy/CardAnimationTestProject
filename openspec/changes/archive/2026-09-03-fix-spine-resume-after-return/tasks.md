# 任务清单:fix-spine-resume-after-return

## 1. Director 调度调整(动画续播延迟到回位完成)

- [x] 1.1 `triggerRecoverBeat` 移除对 `_roundAttacker.Resume()`/`_roundDefender.Resume()` 的调用;恢复节拍只保留幕布关闭 + `Stage.RecoverPresentation(...)`;同步更新方法注释与 `Recover` 阶段语义(定格收缩回位,回位完成后续播)
- [x] 1.2 `handleReturnHomeCompleted` 在 `_phase == Recover` 且未续播时(幂等守卫 `_didResumeAfterReturn`)依次调用 `_roundAttacker.Resume()`、`_roundDefender.Resume()`,再置回位完成标志并 `tryFinishRound()`;确认续播只发生一次
- [x] 1.3 校验收尾双条件:回位完成后攻击方续播至 Attack 播完触发动画结束事件 → `tryFinishRound` 复位 Ready;极端路径(Strike 态动画先结束的兜底)仍只走恢复节拍、不立即续播

## 2. 编译与人工验收(用户在编辑器执行)

- [x] 2.1 编译零错误,运行时无 Error 级日志
- [x] 2.2 按 B 触发演出:表现窗口结束后的缩小回位期间双方动画保持定格不推进;回 Home 完成后才从各自定格帧常速续播并正常收尾回到 Ready(可再次触发)

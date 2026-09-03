# 修复第二次演出缩放/回位异常 — 技术设计

## Context

`BattleStage.SnapFocusIn` 现行实现(变更 `spine-battle-cinematic` 落地):
1. 空引用/进行中守卫;
2. `stopGroupShiftAndShake()`(含 `resetPerformerMounts()`:把 `_defenderMountScale/_attackerMountScale` 写回角色本地缩放并清零位置);
3. `resetGroupToBase()`;
4. 记录 `_activeDefender/_activeAttacker`;
5. `snapPerformer()`:先缓存 `PerformerSnapshot`(父级 + 本地 TRS)再 `SetParent(FocusAnchor)`、位置归零、`localScale × _focusScaleMultiplier`;
6. 记录挂载基准缩放与组基准位置。

缺陷:第 2 步发生在第 5 步**采样快照之前**,且 `_defenderMountScale/_attackerMountScale` 是上一轮遗留值。第一轮运行时 `_activeDefender == null`(第 2 步内部跳过复位)因此正常;第一轮结束后 `applyReturnHomeEnd` 只把 `_isPerforming = false`,**未清空** `_activeDefender/_activeAttacker/快照/挂载缩放`;第二轮起第 2 步会把"上轮 2× 挂载缩放 + 位置(0,0,0)"写回 Home 态角色,随后第 5 步缓存到被污染的本地 TRS → 第二轮挂载放大变成 2×2=4×,且回位终点(快照)指向原点与 2× 缩放。

约束:不动演出表现/参数/编排;仅修轮次基准正确性;延续"三层分工(Stage 管变换)",不引入新文件;观感验证由用户人工进行。

## Goals / Non-Goals

**Goals:**

- 修复第二轮及以后"挂载放大尺寸漂移(倍率叠加)与回 Home 缩放/位置错误"。
- 保证每轮快照取自该轮真实 Home 基准,轮间零污染。
- 保持全部现行为参数与流程(双定格/组平移/晃动/回位时长)不变。

**Non-Goals:**

- 不改 Director 节拍/触发逻辑(根因不在此)。
- 不改场景/参数/序列化字段。
- 不做编辑器自动化验收(用户人工按 B 连续多轮目视确认)。

## Decisions

### D1 快照先于任何"写回式清理"

`SnapFocusIn` 重排为:守卫 → 记录本轮角色与组基准 → `snapPerformer`×2(采样 Home 快照并挂载放大)→ 记录挂载缩放 → 仅做"组归位兜底"(`resetGroupToBase`,不触碰角色)。`SnapFocusIn` 内**不再调用** `stopGroupShiftAndShake`(该清理只应在演出进行中的恢复路径使用)。备选:先清引用再复位——不可行,清引用后仍会用到错误基准或引入空引用,顺序重排才是根治。

### D2 复位写回仅限演出进行中

给 `resetPerformerMounts`(或 `stopGroupShiftAndShake` 内部调用点)增加 `_isPerforming` 守卫:只在演出进行中的舞台动作(恢复序列开头)才把演出对复位到挂载基准;Ready/待机态(旧引用残留期)一律跳过,杜绝把历史放大值写回 Home 态角色。备选:每次收尾清空引用后自然为空——仍保留守卫作纵深防御。

### D3 收尾清空轮次状态

`applyReturnHomeEnd` 在还原缓存父级与本地 TRS、`_isPerforming = false` 之后,将 `_activeDefender/_activeAttacker` 置空、`_defenderSnapshot/_attackerSnapshot` 置空、`_defenderMountScale/_attackerMountScale` 复位为零;并同步将 `_defenderFocusAnchor/_attackerFocusAnchor` 相关轮次记录(base 组位)归零或留待下轮覆盖均可(以代码风格取舍)。保证下轮 SnapFocusIn 从"无历史引用"出发。备选:仅重排不清空——依赖 D1 顺序即可通过,但保留脏引用对后续扩展(如中途打断/换对)不友好,故一并清空。

### D4 组/背景与平移不受影响

`resetGroupToBase`/`ZoomGroupIn/Out`/组平移协程均不涉及演出角色自身本地变换的采样,维持原实现;恢复序列(`RecoverPresentation`)中 `stopGroupShiftAndShake` 因 `_isPerforming == true` 依旧生效。

## Risks / Trade-offs

- [重排后若仍存在"上一轮协程未真正结束"导致组状态残留] → `Ready` 只能在 `applyReturnHomeEnd`(协程终点)达成,恢复路径起点已停旧协程;`SnapFocusIn` 保留 `resetGroupToBase()` 兜底组位。
- [守卫 `_isPerforming` 若在异常中断时未复位,导致恢复清理被跳过] → 现有流程中 `_isPerforming` 仅在 `applyReturnHomeEnd` 置 false;若后续加入"中断/重启"特性需单独立项,本期不在范围。
- [修改顺序/清空引用影响现有冒烟通过的流程] → 修复仅触及"引用与快照生命周期",不触碰节拍与表现序列;实施后用户按 B 连续两轮以上人工验证。

## Migration Plan

增量修复,无迁移:直接修改 `BattleStage.cs` 三处(重排 `SnapFocusIn`、`resetPerformerMounts` 守卫、`applyReturnHomeEnd` 清空),编译零错误后由用户在编辑器中连续多轮按 B 人工验收;回滚走 git revert(场景与其余脚本不变)。

## Open Questions

- 是否需要"演出中途被打断(如换对/Reset)"的统一中断清理(本期不做,`_isPerforming` 语义足够)。
- 是否值得在 Stage 增加运行时日志输出每轮 Home 快照与挂载缩放,便于用户数值比对(可选,默认不加日志)。

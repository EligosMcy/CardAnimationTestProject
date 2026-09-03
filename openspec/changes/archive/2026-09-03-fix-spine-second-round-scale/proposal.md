# 修复第二次演出时挂载放大与回 Home 缩放异常(fix-spine-second-round-scale)

## Why

Spine 战斗演出(变更 `spine-battle-cinematic`)实现后发现:**第一轮演出正常,第二轮起演出对"挂载放大的大小"与"缩放回 Home 原位"明显错误**(放大倍率翻倍、回位位置/缩放漂移)。根因在 `BattleStage.SnapFocusIn`:新一轮演出在**尚未缓存新快照前**就调用了 `stopGroupShiftAndShake() → resetPerformerMounts()`,而该复位会拿**上一轮遗留**的 `_defenderMountScale/_attackerMountScale`(上轮 2× 挂载缩放)直接写回角色本地缩放并把位置清零;第一轮因 `_activeDefender/_activeAttacker` 为空而被跳过所以正常,第二轮起缓存到的快照即被污染,导致整轮放大与回位基准错误。此缺陷影响所有"第二轮及以后"的演出,需修复。

## What Changes

- **修正 `BattleStage.SnapFocusIn` 执行顺序**:新一轮演出先缓存演出对当前(Home 态)父级与本地 TRS 快照并完成挂载,之后才做任何组归位/兜底清理;禁止在快照前用旧轮挂载缩放写回角色变换。
- **`resetPerformerMounts` 仅在演出进行中生效**:以 `_isPerforming` 或等效守卫保护,避免待机态(旧轮引用未清)时把放大值写回已归位角色;`SnapFocusIn` 不再于快照前调用它。
- **收尾清空轮次引用**:`applyReturnHomeEnd` 还原缓存父级与本地 TRS 后,将 `_activeDefender/_activeAttacker`、快照与挂载缩放字段复位/置空,保证下一轮从干净 Home 基准开始。
- 不改动演出表现参数、不新增序列化字段、不改变编排流程(双定格/组平移/晃动/回位语义不变),仅修复"每轮快照/复位基准"的正确性。

## Capabilities

### New Capabilities

<!-- 无新能力 -->

### Modified Capabilities

- `spine-battle-cinematic`: 修正"演出对挂载放大与回 Home 还原在多次轮播间保持一致"的需求——演出对在**每一轮**演出开始时 MUST 以该轮实际 Home 基准执行挂载放大与倍率,结束时 MUST 精确还原至该轮演出前父级与本地 TRS,后续轮次不得继承上一轮的放大/晃动中间值。

## Impact

- 代码:`Assets/SpineTest/Battle/Scripts/BattleStage.cs`(`SnapFocusIn` 顺序、`resetPerformerMounts` 守卫、`applyReturnHomeEnd` 引用清理;必要时调整 `stopGroupShiftAndShake`/`resetGroupToBase` 调用点)。
- 场景/资产:无改动(已有接线与参数不变)。
- 行为影响:修复后第一轮与后续轮次的放大倍率(×2)、回 Home 位置/缩放完全一致。
- 验证:在编辑器中连续触发两轮以上演出(按 B)目视/数值比对(用户人工验收);无自动化编辑器校验步骤。

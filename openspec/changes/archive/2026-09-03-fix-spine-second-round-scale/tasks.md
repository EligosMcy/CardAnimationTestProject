# 任务清单:fix-spine-second-round-scale

## 1. BattleStage 快照与复位顺序修正

- [x] 1.1 `SnapFocusIn` 重排:守卫 → 记录本轮 `_activeDefender/_activeAttacker` 与组基准本地位置 → `snapPerformer`×2(先缓存 Home 快照再挂载/放大)→ 记录 `_defenderMountScale/_attackerMountScale` → 仅保留 `resetGroupToBase()` 兜底;移除快照前的 `stopGroupShiftAndShake()` 调用
- [x] 1.2 `resetPerformerMounts`(或 `stopGroupShiftAndShake`)增加进行中守卫:仅当 `_isPerforming == true` 时才对演出对写回"挂载基准"复位,Ready/待机态跳过(杜绝旧轮放大值写回 Home 态角色)

## 2. 轮次状态收尾清理

- [x] 2.1 `applyReturnHomeEnd` 在还原缓存父级与本地 TRS、`_isPerforming = false` 后,清空 `_activeDefender/_activeAttacker`、`_defenderSnapshot/_attackerSnapshot`,并将 `_defenderMountScale/_attackerMountScale` 复位为零,保证下一轮从无历史引用状态开始
- [x] 2.2 检查 `RecoverPresentation`/`ReturnHome` 空引用与守卫路径不受清空影响(恢复序列发生在清空之前);确认 Ready 后再次触发不再命中"上一轮演出尚未回位"误报

## 3. 编译与人工验收(用户在编辑器执行)

- [x] 3.1 编译零错误后,在编辑器中连续按 B 触发两轮以上演出:第二轮及以后的挂载放大尺寸与首轮一致(约为 Home 基准 ×2),不出现倍率叠加
- [x] 3.2 每轮收尾后双方精确回到 Home 原位(父级/本地位置/旋转/缩放),第二轮及以后无回位位置/缩放漂移;晃动/平移/幕布等表现与既有流程一致

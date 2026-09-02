# Spine 双人战斗演出 — 技术设计

## Context

现有 `Assets/SpineTest/` 只有单角色的试验脚本 `DefaultSpine.cs`(自播动画 + 事件回调演示),不具备"多角色演出编排"能力。本次在 **世界空间** 用 spine-unity 4.3(URP 17 项目)制作一轮双蜘蛛(Spider / Spider_Corrupted)对战演出:按 B 触发、命中瞬间慢动作 + 受击帧定格 + 无相机移动/缩放的聚焦感。Spine 资产动画时长为实测:Attack1≈1.33s(蓄力定格 0.33~0.57、扑击 0.57~0.73、命中定格 0.73~0.80、后摇 0.80~1.33),Hit≈0.50s(受击抖动 0.10~0.33),Idie=4s 循环(注意资产拼写 `Idie`)。

## Goals / Non-Goals

**Goals:**

- 三个 Editor 可调参数(`_attackStartTime`/`_attackEndTime`/`_hitFrameTime`)驱动整轮演出,不改代码即可精调打击节奏。
- 明确的调用方向与职责分层:Actor 封装 Spine、Director 唯一决策、Stage 唯一补间执行,为将来换 UISkeletonGraphic/相机方案/事件触发留可替换边界。
- 命中慢放只作用于攻击方动画轨道;相机、全局时间、受击方以外的对象不受影响。

**Non-Goals:**

- 不做 UI Canvas/UISkeletonGraphic 方案(仅记录取舍)。
- 不做相机动画(位置/旋转不变)。
- 不做 Attack2 第二组时间参数(预留 SO 升级点)、受击特效/音效、连段队列、血量结算。
- 不修改 `DefaultSpine.cs` 与既有 ring-map 等模块。

## Decisions

### D1 渲染层:世界空间 SkeletonAnimation(用户选定)

对比过 UISkeletonGraphic(UI Canvas):UI 方案在"与卡面/血条同层、RectTransform 缩放居中"上更省事,但本演出属于场景内角色对战,且用户明确选择世界方案。分层设计(Actor 封装)保证将来两个方案只改 `SpineActor` 内部实现,Director/Stage 不感知。

### D2 无相机运镜:锚点 + 乘性缩放补间(用户选定)

不移动相机。`BattleStage` 把双人从 Home 锚点补间到 Focus 锚点,并对 `localScale` 做 **乘性** 变化(乘以倍率)以保留镜像角色的负 X 缩放。Home 位置即场景初始摆放,Focus 锚点由美术/策划在 Scene 里摆放成"屏幕中央特写"的世界坐标。替代方案(相机 dolly/推 FOV/改 orthographic size)被否:会牵连相机状态与画幅,回滚成本高。

### D3 脚本三层拆分(4 个文件)

| 脚本 | 唯一职责 | 持有 Update | 依赖 |
|---|---|---|---|
| `SpineActor`(A/B 各一) | 播放/变速/冻结/恢复/回 Idle,隔离全部 Spine API | 无(纯指令式) | SkeletonAnimation |
| `SpineBattleDirector` | B 键监听、状态机、按 B 的 clip 时间触发节拍、调用 A/B/Stage | 有(输入 + 阈值轮询) | Actor×2、Stage |
| `BattleStage` | 双人 Home↔Focus 移动+缩放补间 | 有(推进补间) | A/B 的 Transform |
| `SpineBattleTimingSO`(本期可选) | 三参数 + 慢放倍率 + 招式时间组 | — | — |

不采用单一大脚本(状态与渲染耦合、无法换渲染方案);不采用 Patterns/StateMachineMB 泛型状态机(4 态小状态机手写枚举+switch 更直白)。MVP 三参数直接放 Director 的 `[SerializeField]`;当需要 Attack1/Attack2 各自时间组或多场景复用时,再转 `SpineBattleTimingSO`(命名符合项目 SO 后缀规范),Director 接口不变。

### D4 主时钟与节拍触发:轮询 B 的 AnimationTime + 一次性守卫

以 B 当前轨道的 `TrackEntry.AnimationTime`(clip 时间,天然计入轨道 TimeScale)为主时钟。Director 的 `Update()` 做 `>= _attackStartTime` / `>= _attackEndTime` 阈值判断,`_didStrike`/`_didRecover` 布尔保证单轮只触发一次。选轮询而非 Spine event:不改资产、立即可用;将来美术加 event 标记时,D 值可平滑切换到 `AnimationState.Event` 回调(改动局限于 Director/Actor)。冻结/恢复边界与释放事件也走同一时钟,避免各脚本各记各的时间。

### D5 慢放与定格都落在动画轨道上

- 慢放:打击节拍将 B 的 `entry.TimeScale = 0.25`(字段 `_slowMoScale`),释放节拍恢复 1.0。**不用 `Time.timeScale`**(会拖慢 Stage 补间、输入与一切)。
- A 定格:打击节拍 `SetAnimation(Hit)` 后置 `entry.Time = _hitFrameTime` 且 `entry.TimeScale = 0`,使 A 恒定显示受击帧;释放节拍仅把 TimeScale 拨回 1.0,从该帧自然续播剩余 Hit(即"受击后摇"),播完回 Idie。实施时注意:设置 Time 后需让骨架立即采样该帧姿态(必要时手动推进一次动画更新),避免首帧闪现初始姿态。

### D6 收尾检测:Actor 以 C# 事件上报完成

B 的非循环攻击轨播完后摇即结束。`SpineActor` 内部订阅 Spine `AnimationState.Complete`(非循环单次),向上抛出 `C# event`(Actor 不认识 Director,事件方向仍单向);Director 收到后让 A/B 各自 `PlayIdle()` 并把状态回到 Ready。回调方案优于轮询"播完没":与 D4 的轮询各司其职(节拍=时间阈值轮询,收尾=轨道完成事件)。

### D7 三参数语义(初值来自 JSON 解析,实施时在场景精调)

| 参数 | 初值(估算) | 语义 |
|---|---|---|
| `_attackStartTime` | ≈0.57s | B clip 内前摇结束/扑击起点 → 打击节拍(慢放起 + A 定格 + 拉近) |
| `_attackEndTime` | ≈0.80s | B clip 内命中定格结束/后摇起点 → 释放节拍(恢复 + 续播 + 回位) |
| `_hitFrameTime` | ≈0.12s | A 的 Hit clip 内被定格采样的受击帧 |

调参工作流:Play Mode 用调试键随时打印 B 的 `AnimationTime`(停在该出打击/该恢复的帧读秒),回填字段即可;不依赖运行时改代码。

## Risks / Trade-offs

- [分段锚点是 JSON 反推的估算,±1~2 帧偏差] → 参数已字段化,场景内 Play + 打印读数精调;spec 以参数语义为准而非固定数值。
- [A 在打击节拍开头即定格,而 B 视觉扑击约 0.7s 才到位,观感可能"先僵后挨打"] → 属可调范围:若观感不佳,可在 Director 增加一个 `_hitFreezeDelay` 偏移(本期不做,记录为 Open Question)。
- [依赖 Complete 事件收尾,若中途打断轨道(未来加入连段)会误判] → 本期无打断路径,守卫已禁止演出中重触发;连段属于后续能力,届时改为按状态显式推进。
- [放大后贴图柔化(源图 1037×960,放大 1.5~2x)] → 演出仅约 1s、可接受;不引入额外成本。
- [URP 17 下 Spine 材质若仍为 Built-in shader 会异常] → 实施第一步核对 `Spider_Material.mat`/`Spider_Corrupted_Material.mat` 使用 Spine URP 变体,并加载 S2/S3 相关指南。

## Migration Plan

全新能力,无存量数据迁移。落地顺序:核对 Spine 材质与资产 → 编写 3 个脚本 → 搭演示场景(角色/锚点/相机/渲染管线材质)→ 场景内精调三参数 → 依据 spec 场景逐条手工验收。回滚:删除新增脚本/场景/预制体即可,不动既有模块;`DefaultSpine.cs` 保持原样,仅在其所在场景与新演示场景冲突时单独确认处置。

## Open Questions

- A 定格时机是否需要与 B 视觉命中点解耦(独立延迟参数)?本期 A 冻结与慢放同起于 `_attackStartTime`,待实机观感反馈后再定。
- 是否现在就引入 `SpineBattleTimingSO` 资产以容纳 Attack1/Attack2 两组时间?本期 MVP 先字段直配,提案已列为预留升级点。
- 演示场景命名与归属目录(建议 `Assets/SpineTest/Battle/` 与现有 SpineTest 命名空间保持一致),实施前确认即可。

# CardHouse Hover 突出显示与卡牌位置计算

## 目录

- [1. Hover 突出显示实现](#1-hover-突出显示实现)
  - [1.1 HoverDetector 事件分发](#11-hoverdetector-事件分发)
  - [1.2 Card.SetFocus 聚焦模式](#12-cardsetfocus-聚焦模式)
  - [1.3 聚焦排他性实现](#13-聚焦排他性实现)
  - [1.4 Hover + Gate 组合](#14-hover--gate-组合)
- [2. Seek 动画系统（位置计算核心）](#2-seek-动画系统位置计算核心)
  - [2.1 架构总览](#21-架构总览)
  - [2.2 Seeker 抽象基类](#22-seeker-抽象基类)
  - [2.3 BaseSeekerComponent](#23-baseseekercomponent)
  - [2.4 内置 Seeker 策略详解](#24-内置-seeker-策略详解)
- [3. 布局位置计算](#3-布局位置计算)
  - [3.1 布局策略基类 CardGroupSettings](#31-布局策略基类-cardgroupsettings)
  - [3.2 SplayLayout 扇形布局计算](#32-splaylayout-扇形布局计算)
  - [3.3 StackLayout 堆叠布局计算](#33-stacklayout-堆叠布局计算)
  - [3.4 SlotLayout 槽位布局计算](#34-slotlayout-槽位布局计算)
  - [3.5 CardGridLayout 网格布局计算](#35-cardgridlayout-网格布局计算)
- [4. 位置计算与交互的协同](#4-位置计算与交互的协同)

---

## 1. Hover 突出显示实现

### 1.1 HoverDetector 事件分发

**源码**：[HoverDetector.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Hover/HoverDetector.cs)

```csharp
public class HoverDetector : Toggleable
{
    public UnityEvent OnHover;    // UnityEvent：可在 Inspector 中绑定
    public UnityEvent OnUnHover;

    void OnMouseEnter()
    {
        if (!IsActive) return;
        OnHover.Invoke();
    }

    void OnMouseExit()
    {
        if (!IsActive) return;
        OnUnHover.Invoke();
    }
}
```

`HoverDetector` 本身是一个**纯事件分发器**，它不执行任何视觉操作。具体的 hover 响应逻辑由外部通过 `UnityEvent` 绑定，例如：

```
Inspector 绑定示例：
  OnHover → Card.SetFocus(true)
  OnHover → Homing.StartSeeking(hoverPosition)
  OnHover → Scaling.StartSeeking(1.15f)
  OnUnHover → Card.SetFocus(false)
  OnUnHover → Homing.StartSeeking(originalPosition)
  OnUnHover → Scaling.StartSeeking(1.0f)
```

**优势**：将"何时触发"与"触发后做什么"解耦。设计时可在 Inspector 中灵活配置，无需编写新代码。

### 1.2 Card.SetFocus 聚焦模式

**源码**：[Card.cs#L124-L147](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Card/Card.cs#L124-L147)

`SetFocus` 是更高级的 hover 响应——将卡牌"飞到前方、放大、正对镜头"，形成完整的预览效果：

```csharp
public void SetFocus(bool isFocused)
{
    IsFocused = isFocused;

    // 位置：Z 前移 2 单位（飞向镜头）
    FaceHoming.StartSeeking(
        isFocused
            ? Camera.main.transform.position + Vector3.forward * 2f
            : Vector3.zero,
        useLocalSpace: !isFocused  // 聚焦时用世界坐标，取消时回本地
    );

    // 旋转：正对镜头
    FaceTurning.StartSeeking(
        isFocused
            ? Camera.main.transform.rotation.eulerAngles.z
            : 0,
        useLocalSpace: !isFocused
    );

    // 缩放：动态计算大小
    FaceScaling.StartSeeking(
        isFocused
            ? 2f * Camera.main.orthographicSize / 4f
            : 1f,
        useLocalSpace: !isFocused
    );

    if (isFocused)
        OnCardFocused?.Invoke(this);  // 通知其他卡：我被聚焦了
}
```

### 聚焦效果示意

```
聚焦前（正常手牌布局）：

    ╱ 卡1 卡2 ╲
   ╱ 卡3 卡4 卡5 ╲
  ─────────────────

聚焦后（卡3 被聚焦）：

    ╱ 卡1 卡2 ╲
   ╱ 卡4 卡5 ╲
  ╱             ╲
 │    ┌───┐     │
 │    │卡3│     │  ← 飞到屏幕中央
 │    └───┘     │     放大 2x
  ╱             ╲     正对镜头
  ─────────────────
```

### 1.3 聚焦排他性实现

**源码**：[Card.cs#L48-L61](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Card/Card.cs#L48-L61)

```csharp
public static Action<Card> OnCardFocused;   // 静态事件

void Awake()
{
    ...
    OnCardFocused += HandleCardFocused;   // 每个 Card 都订阅
}

void OnDestroy()
{
    OnCardFocused -= HandleCardFocused;
}

void HandleCardFocused(Card card)
{
    // 如果我已被聚焦，但不是新聚焦的那张 → 退出聚焦
    if (IsFocused && card != this)
    {
        SetFocus(false);
    }
}
```

**工作流程**：

```
1. 用户 Hover 卡3 → OnHover → card3.SetFocus(true)
2. card3.OnCardFocused?.Invoke(card3)   ← 广播
3. 所有卡收到事件：
   - card1: IsFocused=false → 忽略
   - card2: IsFocused=false → 忽略
   - card3: IsFocused=true, card==this → 忽略（自己）
   - card4: IsFocused=true, card!=this → SetFocus(false)  ← 退出
   - card5: IsFocused=false → 忽略
4. 结果：只有 card3 处于聚焦状态
```

此外，`Card.Update()` 中还支持**点击空白处退出聚焦**：

```csharp
void Update()
{
    if (IsFocused && Input.GetMouseButtonDown(0))
    {
        SetFocus(false);  // 点击其他地方 → 退出聚焦
    }
}
```

### 1.4 Hover + Gate 组合

`HoverDetector` 继承自 `Toggleable`，因此也可以被 Gate 控制：

```
场景：某些阶段禁止 Hover 预览

Phase 配置：
  - Phase A: 所有 HoverDetector.IsActive = true  ← 允许预览
  - Phase B: 所有 HoverDetector.IsActive = false ← 禁止预览

实现：
  PhaseManager.OnPhaseChanged → 遍历所有卡
    → HoverDetector.SetIsActive(phase.allowsHover)
```

此外，`HoverDetector` 的 OnHover 事件可以绑定 Gate 检查：

```csharp
// 伪代码：只有满足条件才触发聚焦
void HandleHover()
{
    if (SomeGate.IsUnlocked())
        card.SetFocus(true);
}
```

---

## 2. Seek 动画系统（位置计算核心）

### 2.1 架构总览

CardHouse 的位置/旋转/缩放变化完全通过 `Seeker` 策略系统驱动，**与业务逻辑完全解耦**：

```
┌─────────────────────────────────────────────────────────────────┐
│                  Seeker 系统架构                                │
└─────────────────────────────────────────────────────────────────┘

  ┌───────────────────────────────────────────────────────────┐
  │  Card (MonoBehaviour)                                     │
  │                                                           │
  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────┐  │
  │  │ Homing          │  │ Turning         │  │ Scaling │  │
  │  │ (位置 Seek)     │  │ (旋转 Seek)     │  │(缩放Seek)│  │
  │  └────────┬────────┘  └────────┬────────┘  └────┬────┘  │
  │           │                    │                │       │
  │           ▼                    ▼                ▼       │
  │  ┌───────────────────────────────────────────────────┐   │
  │  │  BaseSeekerComponent<T>                           │   │
  │  │  ├── Seeker<T> MyStrategy  ← 当前运动策略        │   │
  │  │  ├── Update() → Pump() → SetNewValue()           │   │
  │  │  └── StartSeeking(dest, strategy, localSpace)     │   │
  │  └───────────────────────────────────────────────────┘   │
  └───────────────────────────────────────────────────────────┘
                              │
                              ▼
  ┌───────────────────────────────────────────────────────────┐
  │  Seeker<T> (抽象基类)                                     │
  │  ├── Start: T (起点)                                     │
  │  ├── End: T (终点)                                       │
  │  ├── Pump(currentValue, deltaTime) → newValue  ← 核心算法│
  │  ├── IsDone(currentValue) → bool  ← 到达判定            │
  │  └── MakeCopy() → Seeker<T>  ← 创建副本                  │
  │                                                           │
  │  实现：                                                   │
  │  ├── ExponentialVector3Seeker (指数衰减移动)             │
  │  ├── InstantVector3Seeker (瞬移)                        │
  │  ├── ContinuousInstantVector3Seeker (持续瞬移)          │
  │  ├── ExponentialFloatSeeker (指数衰减浮点)              │
  │  ├── ExponentialAngleFloatSeeker (指数衰减角度)          │
  │  └── ... WaypointCurve, Tweak, Randomized 等            │
  └───────────────────────────────────────────────────────────┘
```

### 2.2 Seeker 抽象基类

**源码**：[Seeker.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Seekers/Seeker.cs)

```csharp
public abstract class Seeker<T>
{
    protected T Start;
    public T End;

    public abstract Seeker<T> MakeCopy();

    public void StartSeeking(T from, T to)
    {
        Start = from;
        End = to;
    }

    public abstract T Pump(T currentValue, float TimeSinceLastFrame);
    public abstract bool IsDone(T currentValue);
}
```

**三个核心方法**：

| 方法 | 职责 |
|------|------|
| `StartSeeking(from, to)` | 记录起点和终点 |
| `Pump(currentValue, deltaTime)` | 根据当前值和帧间隔计算新值（核心算法） |
| `IsDone(currentValue)` | 判断是否到达终点 |

### 2.3 BaseSeekerComponent

**源码**：[BaseSeekerComponent.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Seekers/Components/BaseSeekerComponent.cs)

```csharp
public abstract class BaseSeekerComponent<T> : MonoBehaviour
{
    protected Seeker<T> MyStrategy;
    protected bool IsSeeking;
    protected bool UseLocalSpace;

    public SeekerScriptable<T> Strategy;  // 可在 Inspector 配置的策略

    void Awake()
    {
        MyStrategy = Strategy?.GetStrategy() ?? GetDefaultSeeker();
    }

    public void StartSeeking(T destination, Seeker<T> strategy = null, bool useLocalSpace = false)
    {
        IsSeeking = true;
        UseLocalSpace = useLocalSpace;
        // 优先级：传入参数 > Inspector 配置 > 默认策略
        MyStrategy = strategy?.MakeCopy() 
                     ?? Strategy?.GetStrategy() 
                     ?? GetDefaultSeeker();
        MyStrategy.StartSeeking(GetCurrentValue(), destination);
    }

    void Update()
    {
        if (!IsSeeking) return;
        var newValue = MyStrategy.Pump(GetCurrentValue(), Time.deltaTime);
        SetNewValue(newValue);
        if (MyStrategy.IsDone(newValue))
        {
            SetNewValue(MyStrategy.End);  // 确保精确到达
            IsSeeking = false;
        }
    }

    protected abstract Seeker<T> GetDefaultSeeker();
    protected abstract T GetCurrentValue();
    protected abstract void SetNewValue(T value);
}
```

**Homing 具体实现**：

```csharp
// Homing.cs
public class Homing : BaseSeekerComponent<Vector3>
{
    protected override Seeker<Vector3> GetDefaultSeeker() => new ExponentialVector3Seeker();
    protected override Vector3 GetCurrentValue() 
        => UseLocalSpace ? transform.localPosition : transform.position;
    protected override void SetNewValue(Vector3 value)
    {
        if (UseLocalSpace) transform.localPosition = value;
        else transform.position = value;
    }
}
```

### 2.4 内置 Seeker 策略详解

#### ExponentialVector3Seeker — 指数衰减移动（默认）

**源码**：[ExponentialVector3Seeker.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Seekers/Vector3/ExponentialVector3Seeker.cs)

```csharp
public class ExponentialVector3Seeker : Seeker<Vector3>
{
    float XYGain = 8f;    // XY 方向增益
    float ZGain = 3f;     // Z 方向增益（独立控制）
    float ArrivalDistance = 0.01f;

    public override Vector3 Pump(Vector3 currentValue, float TimeSinceLastFrame)
    {
        return currentValue
            + (Vector3.right * (End.x - currentValue.x) + Vector3.up * (End.y - currentValue.y)) * XYGain * TimeSinceLastFrame
            + Vector3.forward * (End.z - currentValue.z) * ZGain * TimeSinceLastFrame;
    }

    public override bool IsDone(Vector3 currentValue)
    {
        return (currentValue - End).magnitude <= ArrivalDistance;
    }
}
```

**运动曲线**：
```
速度
  │  ╲
  │    ╲    ← 增益大 → 快速收敛
  │      ╲
  │        ╲
  │          ────── 到达
  │
  └─────────────────→ 时间
```

**关键特性**：XY 和 Z 使用不同的增益，Z 方向收敛更快（ZGain=3 vs XYGain=8），避免卡牌"滑穿"其他卡。

#### ExponentialFloatSeeker — 指数衰减（标量）

**源码**：[ExponentialFloatSeeker.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Seekers/Float/ExponentialFloatSeeker.cs)

```csharp
public class ExponentialFloatSeeker : Seeker<float>
{
    float Gain = 8f;
    float ArrivalDistance = 0.01f;

    public override float Pump(float currentValue, float TimeSinceLastFrame)
    {
        return Mathf.Lerp(currentValue, End, Gain * TimeSinceLastFrame);
    }

    public override bool IsDone(float currentValue)
    {
        return Mathf.Abs(currentValue - End) <= ArrivalDistance;
    }
}
```

#### ExponentialAngleFloatSeeker — 角度衰减

**源码**：[ExponentialAngleFloatSeeker.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Seekers/Float/ExponentialAngleFloatSeeker.cs)

```csharp
public class ExponentialAngleFloatSeeker : ExponentialFloatSeeker
{
    public override float Pump(float currentValue, float TimeSinceLastFrame)
    {
        return Mathf.LerpAngle(currentValue, End, Gain * TimeSinceLastFrame);
        // 使用 LerpAngle 处理 360° 环绕问题
    }
}
```

#### InstantVector3Seeker — 瞬移

**源码**：[InstantVector3Seeker.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Seekers/Vector3/InstantVector3Seeker.cs)

```csharp
public class InstantVector3Seeker : Seeker<Vector3>
{
    public override Vector3 Pump(Vector3 currentValue, float TimeSinceLastFrame) => End;
    public override bool IsDone(Vector3 currentValue) => true;
}
```

#### ContinuousInstantVector3Seeker — 持续瞬移

**源码**：[ContinuousInstantVector3Seeker.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Seekers/Vector3/ContinuousInstantVector3Seeker.cs)

```csharp
public class ContinuousInstantVector3Seeker : Seeker<Vector3>
{
    public override Vector3 Pump(Vector3 currentValue, float TimeSinceLastFrame) => End;
    public override bool IsDone(Vector3 currentValue) => false;  // 永不完成
}
```

用于持续追踪目标（如拖拽时），每帧都瞬移到目标位置，永不停止。

### Seeker 策略对比表

| Seeker | 运动方式 | 完成判定 | 适用场景 |
|--------|---------|---------|---------|
| `ExponentialVector3Seeker` | 指数衰减（XY 快/Z 慢） | 距离 ≤ 0.01 | 通用移动（默认） |
| `ExponentialFloatSeeker` | 指数衰减 | 差值 ≤ 0.01 | 缩放/透明度等标量 |
| `ExponentialAngleFloatSeeker` | 角度指数衰减 | 差值 ≤ 0.01 | 旋转（LerpAngle 防环绕） |
| `InstantVector3Seeker` | 瞬移 | 立完成 | 初始化/切场景 |
| `ContinuousInstantVector3Seeker` | 持续瞬移 | 永不完成 | 拖拽实时跟随 |
| `TweakVector3Seeker` | AnimationCurve | 曲线终点 | 设计师手动调曲线 |
| `WaypointCurveVector3Seeker` | 路径点曲线 | 路径终点 | 复杂路径移动 |
| `RandomizedCurveVector3Seeker` | 带扰动曲线 | 终点 | 洗牌/散牌 |

---

## 3. 布局位置计算

### 3.1 布局策略基类 CardGroupSettings

**源码**：[CardGroupSettings.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Groups/Layouts/CardGroupSettings.cs)

```csharp
public abstract class CardGroupSettings : MonoBehaviour
{
    public int CardLimit = -1;                   // 卡牌数量上限，-1=无限制
    public float MountedCardAltitude = 0.01f;    // 基础 Z 偏移
    public CardFacing ForcedFacing;              // 强制朝向（正面/反面）
    public GroupInteractability ForcedInteractability;  // 强制交互性
    public MountingMode DragMountingMode = MountingMode.Top;  // 挂载位置策略
    public bool UseMyScale = false;             // 是否使用 Group 缩放

    public void Apply(List<Card> cards, bool instaFlip = false, SeekerSetList seekerSets = null)
    {
        // 1. 处理朝向
        for (var i = 0; i < cards.Count; i++)
        {
            if (ForcedFacing != CardFacing.None)
                cards[i].SetFacing(ForcedFacing, immediate: instaFlip);

            // 2. 处理交互性（OnlyTopActive 等）
            if (ForcedInteractability != GroupInteractability.None)
            {
                var col = card.GetComponent<Collider2D>();
                if (col)
                    col.enabled = /* 根据策略判断 */;
            }
        }

        // 3. 处理位置（调用子类实现）
        ApplySpacing(cards, seekerSets);
    }

    protected abstract void ApplySpacing(List<Card> cards, SeekerSetList seekerSets);
}
```

### 3.2 SplayLayout 扇形布局计算

**源码**：[SplayLayout.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Groups/Layouts/SplayLayout.cs)

```csharp
public class SplayLayout : CardGroupSettings
{
    public float MarginalCardOffset = 0.01f;
    public Vector2 ArcCenterOffset = new Vector2(0f, -5f);
    [Range(0f, 0.8f)]
    public float ArcMargin = 0.3f;

    protected override void ApplySpacing(List<Card> cards, SeekerSetList seekerSets = null)
    {
        var width = transform.lossyScale.x * (1f - ArcMargin);
        var spacing = width / (cards.Count + 1);

        for (var i = 0; i < cards.Count; i++)
        {
            // 位置计算
            var newPos = transform.position
                         + Vector3.back * (MountedCardAltitude + i * MarginalCardOffset)
                         + transform.right * width * -0.5f
                         + transform.right * (i + 1) * spacing;

            // 旋转计算：基于弧形圆心
            var newAngle = Mathf.Atan2(
                newPos.y - ArcCenterOffset.y,
                newPos.x - ArcCenterOffset.x
            ) * Mathf.Rad2Deg - 90;

            cards[i].Homing.StartSeeking(newPos, seekerSet?.Homing);
            cards[i].Turning.StartSeeking(newAngle, seekerSet?.Turning);
            cards[i].Scaling.StartSeeking(UseMyScale ? transform.lossyScale.y : 1, seekerSet?.Scaling);
        }
    }
}
```

**位置公式分解**：

```
newPos = GroupPosition
       + Vector3.back * (Altitude + i * MarginalOffset)   // Z 轴分层
       + Right * width * (-0.5)                           // 左边界
       + Right * (i + 1) * spacing                        // 等间距分布

newAngle = atan2(cardY - arcCenterY, cardX - arcCenterX)  // 朝向圆心
         * 180/π                                          // 转角度
         - 90                                             // 朝向正上方
```

### 3.3 StackLayout 堆叠布局计算

**源码**：[StackLayout.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Groups/Layouts/StackLayout.cs)

```csharp
public class StackLayout : CardGroupSettings
{
    public Vector3 MarginalCardOffset = new Vector3(0.01f, 0.01f, -0.01f);
    public bool Straighten = true;

    protected override void ApplySpacing(List<Card> cards, SeekerSetList seekerSets = null)
    {
        for (var i = 0; i < cards.Count; i++)
        {
            cards[i].Homing.StartSeeking(
                transform.position 
                + Vector3.back * MountedCardAltitude 
                + MarginalCardOffset * i,   // 每张卡累积偏移
                seekerSet?.Homing);

            if (Straighten)
                cards[i].Turning.StartSeeking(transform.rotation.eulerAngles.z, seekerSet?.Turning);
        }
    }
}
```

**特征**：所有卡 X/Y 偏移极小（0.01），Z 偏移为 -0.01，形成"微微错开的一叠卡"，适合 Deck/牌堆场景。

### 3.4 SlotLayout 槽位布局计算

**源码**：[SlotLayout.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Groups/Layouts/SlotLayout.cs)

```csharp
public class SlotLayout : CardGroupSettings
{
    protected override void ApplySpacing(List<Card> cards, SeekerSetList seekerSets = null)
    {
        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            card.Homing.StartSeeking(
                transform.position + Vector3.back * MountedCardAltitude,
                seekerSet?.Homing);
            card.Turning.StartSeeking(transform.rotation.eulerAngles.z, seekerSet?.Turning);
        }
    }
}
```

**特征**：所有卡精确叠加在同一位置，无任何偏移。适合"只有一张卡显示的槽位"场景（如出牌位）。

### 3.5 CardGridLayout 网格布局计算

**源码**：[CardGridLayout.cs](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Groups/Layouts/CardGridLayout.cs)

```csharp
public class CardGridLayout : CardGroupSettings
{
    public int CardsPerRow = 5;
    public float MarginalCardOffset = 0.05f;

    protected override void ApplySpacing(List<Card> cards, SeekerSetList seekerSets)
    {
        var width = transform.lossyScale.x;
        var height = transform.lossyScale.y;
        var rowCount = 1 + (cards.Count - 1) / CardsPerRow;
        var colSpacing = height / (rowCount + 1);

        for (var row = 0; row < rowCount; row++)
        {
            var cardsInThisRow = Mathf.Min(CardsPerRow, cards.Count - row * CardsPerRow);
            var rowSpacing = width / (cardsInThisRow + 1);

            for (var col = 0; col < cardsInThisRow; col++)
            {
                var newPos = transform.position
                             + transform.right * width * -0.5f
                             + transform.right * (col + 1) * rowSpacing
                             + transform.up * height * 0.5f
                             + transform.up * (row + 1) * colSpacing * -1
                             + transform.forward * (MountedCardAltitude + MarginalCardOffset * (row * CardsPerRow + col)) * -1;

                var cardIndex = row * CardsPerRow + col;
                var card = cards[cardIndex];
                card.Homing.StartSeeking(newPos, seekerSet?.Homing);
                card.Turning.StartSeeking(transform.rotation.eulerAngles.z, seekerSet?.Turning);
            }
        }
    }
}
```

**位置计算示意**：

```
CardsPerRow = 3

    ┌─────────────────────────────────────────┐
    │  ┌───┐  ┌───┐  ┌───┐                    │
    │  │C1 │  │C2 │  │C3 │  ← Row 0          │
    │  └───┘  └───┘  └───┘                    │
    │                                          │
    │  ┌───┐  ┌───┐  ┌───┐                    │
    │  │C4 │  │C5 │  │C6 │  ← Row 1          │
    │  └───┘  └───┘  └───┘                    │
    │                                          │
    │  ┌───┐  ┌───┐                           │
    │  │C7 │  │C8 │  ← Row 2 (最后一行不满)  │
    │  └───┘  └───┘                           │
    └─────────────────────────────────────────┘

每行间距：width / (cardsInThisRow + 1)
每列间距：height / (rowCount + 1)
Z 偏移：Altitude + MarginalOffset * index
```

---

## 4. 位置计算与交互的协同

### 交互→位置变更的完整链路

```
触发交互（如 Hover 或 Drag）
  │
  ▼
调用 Card.SetFocus() 或 Dragging.BeginDragging()
  │
  ▼
调用 Homing.StartSeeking(destination, strategy)
  │  内部：
  │  1. MyStrategy = strategy?.MakeCopy() ?? GetDefaultSeeker()
  │  2. MyStrategy.StartSeeking(currentPos, destination)
  │  3. IsSeeking = true
  │
  ▼
BaseSeekerComponent.Update() 每帧
  │
  ▼
MyStrategy.Pump(currentPos, deltaTime) → 计算新位置
  │
  ▼
transform.position = newPosition
  │
  ▼
MyStrategy.IsDone(newPosition) → 到达？
  │  是 → IsSeeking = false
  │  否 → 继续 Pump
  │
  ▼
最终到达精确的目标位置
```

### SeekerSetList — 每张卡独立策略

**源码**：[CardGroup.cs#L196-L208](file:///c:/Project/CardAnimationTestProject/Assets/CardHouse/CardHouseCore/Scripts/Groups/CardGroup.cs#L196-L208)

当卡牌移动到目标 Group 时，可以为**每张卡单独指定**动画策略：

```csharp
var seekerSets = new SeekerSetList();
seekerSets.Add(new SeekerSet
{
    Card = cardComponent,
    Homing = cardDragHandler.PresentationSeekers.Homing?.GetStrategy(presentationTransform.position),
    Turning = cardDragHandler.PresentationSeekers.Turning?.GetStrategy(...),
    Scaling = cardDragHandler.PresentationSeekers.Scaling?.GetStrategy(...)
});
discardGroup.Mount(cardComponent, seekerSets: seekerSets);
```

这实现了：
- 卡牌 A 用 `ExponentialVector3Seeker`（平滑缓动）
- 卡牌 B 用 `InstantVector3Seeker`（瞬移）
- 卡牌 C 用 `WaypointCurveVector3Seeker`（曲线飞行）

### 布局刷新时机

`CardGroup.Apply()` 在以下场景被调用，自动重新计算所有卡牌位置：

| 触发场景 | 调用入口 |
|---------|---------|
| Mount（挂载） | `CardGroup.Mount()` → `Strategy.Apply(MountedCards)` |
| UnMount（卸载） | `CardGroup.UnMount()` → `Strategy.Apply(MountedCards)` |
| Shuffle（洗牌） | `CardGroup.Shuffle()` → `Strategy.Apply(MountedCards)` |
| 拖拽失败回退 | `cardComponent.Group.ApplyStrategy()` |
| 拖拽成功重排 | `HandleDragDrop` 中 Mount 后自动触发 |

这确保了任何卡牌数量/顺序变化都会触发布局重算，位置始终保持正确。

# 卡牌位置计算与排序机制

## 1. 概述

UiCard 系统使用 **弧形排列算法 + Z 轴排序** 来实现美观的手牌布局和正确的卡牌层级关系。

```
┌─────────────────────────────────────────────────────────────────┐
│                    手牌排列计算流程                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. 触发 OnPileChanged 事件                                   │
│              │                                                  │
│              ▼                                                  │
│   2. UiCardHandSorter.Sort()                                    │
│      → 按顺序分配 Z 轴层级                                      │
│              │                                                  │
│              ▼                                                  │
│   3. UiCardBender.Bend()                                       │
│      → 计算每张卡的位置、角度、高度                              │
│              │                                                  │
│              ▼                                                  │
│   4. 调用 MoveTo / RotateTo 进行动画过渡                        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## 2. 弧形排列算法

### 2.1 核心类

**文件**：[UiCardBender.cs](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardHand/UiCardBender.cs)

### 2.2 参数说明

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `BentAngle` | float | 20 | 手牌总弯曲角度（度） |
| `Spacing` | float | 2 | 卡牌水平间距 |
| `Height` | float | 0.12 | 卡牌高度因子（弧度转 Y 偏移） |
| `pivot` | Transform | - | 手牌旋转中心锚点 |

### 2.3 角度计算

```csharp
// UiCardBender.cs#L48-L92
private void Bend(IUiCard[] cards)
{
    // 1. 计算总弯曲角度
    var fullAngle = -parameters.BentAngle;  // 负值表示向下弯曲
    
    // 2. 每张卡的角度增量
    var anglePerCard = fullAngle / cards.Length;
    
    // 3. 第一张卡的起始角度
    var firstAngle = CalcFirstAngle(fullAngle);
    
    // 4. 计算手牌总宽度
    var handWidth = CalcHandWidth(cards.Length);
    
    // 5. 判断手牌靠近屏幕上/下边缘
    var pivotLocationFactor = pivot.CloserEdge(Camera.main, Screen.width, Screen.height);
    
    // 6. 起始 X 位置（居中对齐）
    var offsetX = pivot.position.x - handWidth / 2;
    
    // 7. 逐卡计算位置和角度
    for (var i = 0; i < cards.Length; i++)
    {
        var card = cards[i];
        
        // Z 轴旋转角度
        var angleTwist = (firstAngle + i * anglePerCard) * pivotLocationFactor;
        
        // X 坐标
        var xPos = offsetX + CardWidth / 2;
        
        // Y 坐标（根据角度计算高度偏移）
        var yDistance = Mathf.Abs(angleTwist) * parameters.Height;
        var yPos = pivot.position.y - (yDistance * pivotLocationFactor);
        
        // 应用变换
        if (!card.IsDragging && !card.IsHovering)
        {
            var zAxisRot = pivotLocationFactor == 1 ? 0 : 180;
            var rotation = new Vector3(0, 0, angleTwist - zAxisRot);
            var position = new Vector3(xPos, yPos, card.transform.position.z);
            
            card.RotateTo(rotation, rotSpeed);
            card.MoveTo(position, parameters.MovementSpeed);
        }
        
        // 累加 X 偏移
        offsetX += CardWidth + parameters.Spacing;
    }
}
```

### 2.4 起始角度计算

```csharp
// UiCardBender.cs#L99-L103
private static float CalcFirstAngle(float fullAngle)
{
    var magicMathFactor = 0.1f;  // 经验系数
    return -(fullAngle / 2) + fullAngle * magicMathFactor;
}
```

**图示说明**：

```
                    BentAngle = 20°（向下弯）
                    ┌──────────────────────────┐
                    │                          │
        Card_0      │  Card_1  Card_2  Card_3  │  Card_4
        ╱           │    ╱       ╱      ╱       │   ╲
       ╱            │   ╱       ╱      ╱        │    ╲
      ╱    ╱        │  ╱       ╱      ╱   ╲     │     ╲
     ╱   ╱         │ ╱       ╱      ╱     ╲    │      ╲
    ╱  ╱          │╱       ╱      ╱       ╲   │       ╲
   ╱ ╱            │        ╱      ╱         ╲ │        ╲
  ╱╱              │       ╱      ╱           ╲╱         ╲
 ──────────────────┴─────────────────────────────────────────
                   pivot (旋转中心)
```

### 2.5 手牌宽度计算

```csharp
// UiCardBender.cs#L110-L115
private float CalcHandWidth(int quantityOfCards)
{
    var widthCards = quantityOfCards * CardWidth;           // 所有卡的总宽
    var widthSpacing = (quantityOfCards - 1) * parameters.Spacing;  // 间距总和
    return widthCards + widthSpacing;
}
```

### 2.6 边缘判断

```csharp
// TransformExtensions.cs#L100-L111
public static int CloserEdge(this Transform transform, Camera camera, int width, int height)
{
    // 屏幕顶部和底部的世界坐标
    var worldPointTop = camera.ScreenToWorldPoint(new Vector3(width / 2, height));
    var worldPointBot = camera.ScreenToWorldPoint(new Vector3(width / 2, 0));
    
    // 计算 pivot 到两个边缘的距离
    var deltaTop = Vector2.Distance(worldPointTop, transform.position);
    var deltaBottom = Vector2.Distance(worldPointBot, transform.position);
    
    // 返回 1（靠下）或 -1（靠上）
    return deltaBottom <= deltaTop ? 1 : -1;
}
```

**效果**：
- 下方玩家手牌：向下弯曲（`pivotLocationFactor = 1`）
- 上方玩家手牌：向上弯曲（`pivotLocationFactor = -1`）

## 3. Z 轴排序机制

### 3.1 核心类

**文件**：[UiCardHandSorter.cs](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardHand/UiCardHandSorter.cs)

### 3.2 排序逻辑

```csharp
// UiCardHandSorter.cs#L24-L37
public void Sort(IUiCard[] cards)
{
    if (cards == null)
        throw new ArgumentException("Can't sort a card list null");
    
    var layerZ = 0;
    foreach (var card in cards)
    {
        var localCardPosition = card.transform.localPosition;
        localCardPosition.z = layerZ;     // 依次递减
        card.transform.localPosition = localCardPosition;
        layerZ += OffsetZ;                // OffsetZ = -1
    }
}
```

**排序效果**：

```
┌─────────────────────────────────────────────────────────────┐
│                    Z 轴排序示意图                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  正面视图（从 Z+ 向 Z- 看）：                              │
│                                                             │
│  ┌─────┐  ┌─────┐  ┌─────┐  ┌─────┐  ┌─────┐             │
│  │Card │  │Card │  │Card │  │Card │  │Card │             │
│  │  0  │  │  1  │  │  2  │  │  3  │  │  4  │             │
│  └──┬──┘  └──┬──┘  └──┬──┘  └──┬──┘  └──┬──┘             │
│     │        │        │        │        │                  │
│     ▼        ▼        ▼        ▼        ▼                  │
│   Z=0      Z=-1     Z=-2     Z=-3     Z=-4               │
│  (最前)   (稍后)   (中间)   (稍前)   (最后)              │
│                                                             │
│  侧视图（从侧面看）：                                       │
│                                                             │
│  Z=0 ───► Card_0                                           │
│  Z=-1 ───► Card_1                                          │
│  Z=-2 ───► Card_2  ← 中间卡牌                              │
│  Z=-3 ───► Card_3                                          │
│  Z=-4 ───► Card_4                                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 4. 渲染层级机制

### 4.1 核心方法

**文件**：[UiBaseCardState.cs](file:///c:/Project/CardAnimationTestProject/Assets/Scripts/UICard/UiCardStateMachine/States/UiBaseCardState.cs)

```csharp
// UiBaseCardState.cs#L10-L11
private const int LayerToRenderNormal = 0;   // 普通层级
private const int LayerToRenderTop = 1;     // 顶层（Hover/Drag）

// 提升到顶层渲染
protected virtual void MakeRenderFirst()
{
    for (var i = 0; i < Handler.Renderers.Length; i++)
        Handler.Renderers[i].sortingOrder = LayerToRenderTop;
}

// 恢复到普通层级
protected virtual void MakeRenderNormal()
{
    for (var i = 0; i < Handler.Renderers.Length; i++)
        if (Handler.Renderers[i])
            Handler.Renderers[i].sortingOrder = LayerToRenderNormal;
}
```

### 4.2 两层排序系统

```
┌─────────────────────────────────────────────────────────────┐
│                    渲染排序的两个维度                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  维度 1：Z 轴位置（localPosition.z）                        │
│  ─────────────────────────────────                          │
│  • 影响 3D 空间中的遮挡关系                                 │
│  • Sorter 按卡牌顺序设置                                    │
│  • 值范围：0, -1, -2, -3, ...                              │
│                                                             │
│  维度 2：Sorting Order（sortingOrder）                      │
│  ─────────────────────────────────                          │
│  • 影响 2D 渲染的前后关系                                   │
│  • 普通状态：0                                             │
│  • Hover/Drag 状态：1（置顶）                              │
│  • 值范围：0 或 1                                          │
│                                                             │
│  综合效果：                                                 │
│  • Hover/Drag 卡牌显示在最前面（sortingOrder=1）            │
│  • 其余卡牌按 Z 轴顺序显示                                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 5. 位置计算数学推导

### 5.1 Y 坐标计算

```
角度 → 高度偏移：

    yDistance = |angleTwist| × Height
    
    其中：
    • angleTwist = (firstAngle + i × anglePerCard) × pivotLocationFactor
    • Height = 参数（默认 0.12）
    
    Y 坐标：
    yPos = pivot.y - (yDistance × pivotLocationFactor)
```

**示例计算**（5 张卡牌，BentAngle=20°）：

```
Card_0: angleTwist = (-8° + 0×(-4°)) × 1 = -8°
        yDistance = 8 × 0.12 = 0.96
        yPos = pivot.y - 0.96

Card_1: angleTwist = (-8° + 1×(-4°)) × 1 = -12°
        yDistance = 12 × 0.12 = 1.44
        yPos = pivot.y - 1.44

Card_2: angleTwist = (-8° + 2×(-4°)) × 1 = -16°
        yDistance = 16 × 0.12 = 1.92
        yPos = pivot.y - 1.92

Card_3: angleTwist = (-8° + 3×(-4°)) × 1 = -20°
        yDistance = 20 × 0.12 = 2.4
        yPos = pivot.y - 2.4

Card_4: angleTwist = (-8° + 4×(-4°)) × 1 = -24°
        yDistance = 24 × 0.12 = 2.88
        yPos = pivot.y - 2.88
```

### 5.2 X 坐标计算

```
X 坐标：

    offsetX = pivot.x - handWidth / 2
    
    循环：
    xPos = offsetX + CardWidth / 2
    offsetX += CardWidth + Spacing
```

**图示**：

```
    ┌────────────────────────────────────────────────────────────────┐
    │                                                                │
    │  offsetX = pivot.x - handWidth/2                              │
    │  ┌───┐  ┌───┐  ┌───┐  ┌───┐  ┌───┐                          │
    │  │ 0 │  │ 1 │  │ 2 │  │ 3 │  │ 4 │                          │
    │  └─┬─┘  └─┬─┘  └─┬─┘  └─┬─┘  └─┬─┘                          │
    │    │      │      │      │      │                              │
    │    ▼      ▼      ▼      ▼      ▼                              │
    │   x0    x1     x2     x3     x4                              │
    │                                                                │
    │  xi = offsetX + CardWidth/2 + i × (CardWidth + Spacing)       │
    │                                                                │
    └────────────────────────────────────────────────────────────────┘
```

## 6. 双玩家支持

### 6.1 位置翻转

```csharp
// UiCardBender.cs#L80-L82
var zAxisRot = pivotLocationFactor == 1 ? 0 : 180;
var rotation = new Vector3(0, 0, angleTwist - zAxisRot);
```

| 玩家位置 | pivotLocationFactor | Z 轴旋转 | 效果 |
|----------|---------------------|----------|------|
| 下方玩家 | 1 | 0° | 正常朝向 |
| 上方玩家 | -1 | 180° | 翻转 180° |

### 6.2 速度参数区分

```csharp
// UiCardBender.cs#L84
var rotSpeed = card.IsPlayer ? parameters.RotationSpeed : parameters.RotationSpeedP2;
```

| 玩家类型 | 旋转速度参数 | 默认值 |
|----------|--------------|--------|
| 己方玩家 | `RotationSpeed` | 20 |
| 对方玩家 | `RotationSpeedP2` | 500 |

## 7. 特殊状态处理

### 7.1 Hover 状态跳过

```csharp
// UiCardBender.cs#L78
if (!card.IsDragging && !card.IsHovering)
{
    // 只有非拖拽、非 Hover 状态的卡牌才会被重新定位
    card.RotateTo(rotation, rotSpeed);
    card.MoveTo(position, parameters.MovementSpeed);
}
```

**效果**：
- 当卡牌处于 Hover 状态时，保持 Hover 的位置和效果
- 手牌弯曲不会干扰 Hover 动画

### 7.2 拖拽状态跳过

拖拽中的卡牌也不会被重新定位，允许玩家自由拖动。

## 8. 配置调整指南

### 8.1 调整弯曲程度

| 目标效果 | 参数调整 |
|----------|----------|
| 更弯曲的弧形 | 增大 `BentAngle`（0~60） |
| 更平缓的弧形 | 减小 `BentAngle` |
| 卡牌更密集 | 减小 `Spacing` |
| 卡牌更分散 | 增大 `Spacing` |

### 8.2 调整高度变化

| 目标效果 | 参数调整 |
|----------|----------|
| 卡牌高度差异更大 | 增大 `Height`（0~1） |
| 卡牌更平展 | 减小 `Height` |

### 8.3 调整动画速度

| 目标效果 | 参数调整 |
|----------|----------|
| 更快的弯曲动画 | 增大 `MovementSpeed`、`RotationSpeed` |
| 更慢的优雅动画 | 减小速度参数 |

## 9. 常见问题排查

### Q1：手牌显示为直线？
**可能原因**：
- `BentAngle` 设置为 0
- `Height` 设置为 0
- `pivot` 位置不正确

### Q2：卡牌重叠严重？
**可能原因**：
- `Spacing` 值太小
- `CardWidth` 计算错误

### Q3：卡牌位置偏移？
**可能原因**：
- `pivot` 不在正确位置
- `CloserEdge` 判断错误

### Q4：对方玩家卡牌朝向错误？
**可能原因**：
- `IsPlayer` 判断逻辑异常
- `CloserEdge` 返回值不正确
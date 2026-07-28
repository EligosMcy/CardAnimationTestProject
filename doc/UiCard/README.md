# UiCard 系统技术文档

本文档集详细分析了基于 Unity 的 UiCard 卡牌交互系统，源自 GitHub 项目 [ycarowr/UiCard](https://github.com/ycarowr/UiCard)。

## 文档列表

| 文档 | 内容 |
|------|------|
| [01-CardInteractionMechanism.md](01-CardInteractionMechanism.md) | 卡牌交互实现机制详解 |
| [02-CardPositioningAndSorting.md](02-CardPositioningAndSorting.md) | 卡牌位置计算与排序机制 |
| [03-HoverAndAntiDenseSelection.md](03-HoverAndAntiDenseSelection.md) | Hover 效果与防密集选择机制 |
| [04-SystemHighlightsAndConfig.md](04-SystemHighlightsAndConfig.md) | 系统亮点与参数配置详解 |

## 快速索引

### 交互机制
- 输入捕获：`UiMouseInputProvider`
- 状态管理：`UiCardHandFsm`
- 状态类型：Idle、Hover、Drag、Disable、Draw、Discard
- 运动插值：`UiMotionMovementCard`、`UiMotionRotationCard`、`UiMotionScaleCard`

### 位置计算
- 弧形排列：`UiCardBender`
- Z 轴排序：`UiCardHandSorter`
- 边缘检测：`TransformExtensions.CloserEdge()`

### Hover 效果
- 渲染层级：`UiBaseCardState.MakeRenderFirst()`
- 位置上移：`UiCardHover.SetPosition()`
- 缩放效果：`UiCardHover.SetScale()`

### 防密集选择
- Layer 1：Z 轴排序防止遮挡
- Layer 2：Hover 卡牌渲染置顶
- Layer 3：非选中卡牌 Collider 禁用
- Layer 4：状态检查过滤输入事件

### 参数配置
- 配置类：`UiCardParameters` (ScriptableObject)
- Hover 参数：HoverHeight、HoverScale、HoverRotation、HoverSpeed
- 弯曲参数：Height、Spacing、BentAngle
- 运动参数：MovementSpeed、RotationSpeed、ScaleSpeed

## 核心类图

```
┌─────────────────────────────────────────────────────────────┐
│                        UiCard 系统架构                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  UiMouseInputProvider                                      │
│  (输入捕获)                                                  │
│         │                                                   │
│         ▼                                                   │
│  UiCardHandComponent ──────► UiCardHandFsm                 │
│  (卡牌主组件)                  (状态机)                      │
│         │                          │                        │
│         ├── Movement (UiMotionMovementCard)                  │
│         ├── Rotation (UiMotionRotationCard)                  │
│         ├── Scale (UiMotionScaleCard)                        │
│         │                                                    │
│         └── UiCardHand ──────► UiCardBender                 │
│              (手牌管理)        (位置计算)                    │
│                          └──► UiCardHandSorter               │
│                               (Z 轴排序)                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 关键文件路径

```
Assets/Scripts/UICard/
├── UiCardHandComponent/
│   └── UiCardHandComponent.cs      # 卡牌主组件
├── UiCardStateMachine/
│   ├── UiCardHandFsm.cs           # 状态机
│   └── States/
│       ├── UiCardIdle.cs          # 空闲状态
│       ├── UiCardHover.cs         # Hover 状态
│       ├── UiCardDrag.cs          # 拖拽状态
│       ├── UiCardDisable.cs       # 禁用状态
│       ├── UiCardDraw.cs          # 抽牌状态
│       └── UiCardDiscard.cs       # 弃牌状态
├── UiCardHand/
│   ├── UiCardHand.cs              # 手牌管理
│   ├── UiCardBender.cs            # 弧形排列
│   ├── UiCardHandSorter.cs        # Z 轴排序
│   └── UiCardUtils.cs             # 工具类
├── UiCardTransform/
│   ├── UiMotionBaseCard.cs        # 运动基类
│   ├── UiMotionMovementCard.cs   # 位置运动
│   ├── UiMotionRotationCard.cs   # 旋转运动
│   └── UiMotionScaleCard.cs       # 缩放运动
├── UiCardParameters/
│   └── UiCardParameters.cs        # 参数配置
└── Utils/
    └── UiHandNotify.cs            # 通知工具

Assets/Scripts/Tools/
└── Input/
    └── UiMouseInputProvider.cs    # 输入提供者

Assets/Scripts/Extensions/
└── Transform/
    └── TransformExtensions.cs      # Transform 扩展
```

## 版本信息

- 基于 [ycarowr/UiCard](https://github.com/ycarowr/UiCard) v1.2
- Unity 版本：2022.3.62f1
- 许可证：MIT
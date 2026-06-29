# Gameplay Revision Implementation Prompt

## 目标

在不改变现有两场景结构、怪物八格移动规则和直接按空格烤制流程的前提下，实现以下玩法修订：

1. 面粉点击量随机为 0.5～1 格。
2. 每次右键按下时随机水流速度倍率 1～3，本次长按期间固定，松开后下次重抽。
3. 分档烤制完成时间：
   - Softest：1.5 秒
   - Medium：2.0 秒
   - Hardest：3.0 秒
4. 完成熟成后有 1 秒完美窗口；随后进入烤焦，烤焦持续 1 秒后强制投掷。
5. 每条球道最多同时存在两只怪物。
6. 一个正确烤制的面包到达目标球道后，最多检查两只怪物；分别按比例判定，击杀所有匹配怪物。

## 保留规则

- 场景仍为 `MainMenu.unity + Game.unity`。
- 三个关卡仍在 Game 场景内由状态机切换。
- 怪物仍使用 8 个点位，每隔 1 秒移动一步。
- 不增加 E 键；玩家调完比例后直接长按空格。
- 按空格期间可切换 Hover 球道，松开时使用当前 Hover；无 Hover 时随机球道。
- 左右各 1/4 格、总宽 1/2 格的比例容错保持不变。

## 数据契约

### DoughConfig

- `FlourClickMin = 0.5`
- `FlourClickMax = 1.0`
- `WaterFillRate` 保留为基础速度
- `WaterSpeedMultiplierMin = 1.0`
- `WaterSpeedMultiplierMax = 3.0`

### BakingConfig

- `SoftestCookDuration = 1.5`
- `MediumCookDuration = 2.0`
- `HardestCookDuration = 3.0`
- `PerfectWindowDuration = 1.0`
- `BurntWindowDuration = 1.0`

烤制状态按按下空格时锁定的有效 DoughState 计算：

- `timer < CookDuration`：Undercooked
- `CookDuration <= timer < CookDuration + PerfectWindowDuration`：Cooked
- `CookDuration + PerfectWindowDuration <= timer`：Burnt
- `timer >= CookDuration + PerfectWindowDuration + BurntWindowDuration`：强制投掷

TooSoft / TooHard 不对应有效面包，使用相邻有效档位的完成时间：

- TooSoft 使用 Softest 时长
- TooHard 使用 Hardest 时长

比例命中仍以实际捕获 ratio 和目标中心 ± ToleranceHalfWidth 判断，因此 TooSoft / TooHard 通常无法命中。

### MonsterConfig

- `MaxMonstersPerLane = 2`

## 系统职责

- DoughSystem：负责随机加料；右键倍率仅在 MouseButtonDown 时抽取。
- BakingSystem：按空格时锁定 DoughState 和本次时长；发布投掷时携带 BakingState。
- MonsterSystem：拥有每道怪物列表，按实例完成查询、击败和错误反馈。
- LevelSystem：把怪物数小于容量的球道视为可生成。
- ThrowSystem：到达时读取目标道当前怪物快照，最多处理两只；Cooked 才能造成伤害。
- UISystem/BakingIndicator：进度按当前锁定 CookDuration 显示，并适配不同档位总时长。

## 命中规则

- 空道：EmptyLane。
- 非 Cooked：WrongBake，不伤害任何怪物。
- Cooked：
  - 匹配怪物全部击杀，每只各广播一次 OnMonsterDefeated。
  - 同道存在但无任何匹配：WrongRatio。
  - 有匹配也有不匹配：匹配者死亡，不匹配者保留；结果为 PartialHit。
- 每只被击败怪物的位置各生成一个爆炸特效。

## 工作流

1. 先更新 003、004、005、006、007、009、010、012 和 TaskBreakdown。
2. 更新 DecisionLog。
3. 再修改 Config、事件、系统、UI 和资产。
4. 执行 EditMode 测试与 Unity 编译检查。
5. 逐条执行 014_ReviewChecklist。
6. 更新 TASK_LOG。

## 禁止

- 不新增 Singleton、FindObjectOfType、GameObject.Find、SendMessage、DontDestroyOnLoad、Invoke 或计时协程。
- 不把 Balance 数值硬编码在系统脚本中。
- 不修改场景结构和怪物移动节奏。
- 不在未更新 Spec 前修改玩法代码。

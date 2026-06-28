# Task Breakdown

> 按依赖顺序排列。每个 Task 完成后执行 014_ReviewChecklist，然后更新 TASK_LOG.txt。
> 粒度目标：每 Task 1~3 个脚本文件，可独立测试。

---

## Phase 0 — 基础设施

### T01 · Assembly Definitions + 目录结构

**新建文件：**
- `Assets/Scripts/GWBGameJam.Runtime.asmdef`
- `Assets/Editor/GWBGameJam.Editor.asmdef`
- 在 Unity 中创建目录：`Scripts/Core/` `Scripts/Events/` `Scripts/Systems/` `Scripts/Config/` `Scripts/UI/` `Editor/` `Prefabs/Monsters/` `Prefabs/Projectile/` `ScriptableObjects/Configs/` `ScriptableObjects/MonsterData/` `Scenes/`

**验收：** Project 窗口中程序集图标正确显示；编译无报错。

---

### T02 · GameEnums + EventBus

**新建文件：**
- `Assets/Scripts/Core/GameEnums.cs` — DoughState / BakingState / ThrowResult / GameState 四个枚举
- `Assets/Scripts/Core/EventBus.cs` — 静态泛型 `EventBus<T>`，含 Subscribe / Unsubscribe / Publish

**验收：** 写一个临时测试脚本，发布并接收一个测试事件，Console 打印成功后删除测试脚本。

---

### T03 · ScriptableObject 类定义

**新建文件（均在 `Assets/Scripts/Config/`）：**
- `GameLoopConfig.cs`
- `BakingConfig.cs`（含 Awake Validate 约束）
- `MonsterConfig.cs`（含 MoveDuration 约束 Validate）
- `LevelConfig.cs` + `LevelData.cs`（嵌套数据类）
- `TableConfig.cs`（含 MaxHits ≥ 1 Validate）
- `DoughConfig.cs`
- `DoughStateBoundaryConfig.cs`（含边界严格递减 Validate）
- `LaneWaypointConfig.cs` + `LaneWaypoints.cs`（包装类）
- `MonsterData.cs`
- `ThrowConfig.cs`

**验收：** 每个 SO 类可在 Unity 中通过 `CreateAssetMenu` 创建 .asset 文件，Inspector 字段与 010_ConfigSchema 一致。

---

### T04 · Event Struct 定义

> ⚠️ **命名对齐说明：** Architecture 草稿与后续 System Spec 存在命名差异，以下以 System Spec 为准。

**新建文件（均在 `Assets/Scripts/Events/`）：**

- `GameLoopEvents.cs`
  - `OnGameStateChanged { GameState NewState, GameState PreviousState }`
  - `OnLevelStarted { int LevelIndex }`

- `MonsterEvents.cs`
  - `OnMonsterSpawned { int LaneIndex, MonsterData Data }`
  - `OnMonsterDefeated { int LaneIndex }`
  - `OnMonsterReachedTable { int LaneIndex }`

- `DoughEvents.cs`
  - `OnDoughStateChanged { DoughState NewState, DoughState PreviousState }`

- `BakingEvents.cs`
  - `OnBakingStateChanged { BakingState NewState, BakingState PreviousState }`
  - `OnThrowRequested { int LaneIndex }` ← BakingSystem 发布，ThrowSystem 订阅

- `ThrowEvents.cs`
  - `OnThrowStarted { int LaneIndex }`
  - `OnThrowCompleted { int LaneIndex, ThrowResult Result }`

- `LaneEvents.cs`
  - `OnLaneHoverChanged { int LaneIndex }` ← -1 表示无悬停

- `LevelEvents.cs`
  - `OnLevelCleared { }`

- `TableEvents.cs`
  - `OnTableDestroyed { }`

**验收：** 所有 struct 为 `readonly struct`，编译无报错。

---

### T05 · SO Asset 创建与默认值填写

**在 Unity 中操作（MCP 或 Editor 手动）：**
- 创建并配置 `ScriptableObjects/Configs/` 下所有 .asset 文件（默认值见 010_ConfigSchema）
- 创建 `ScriptableObjects/MonsterData/MonsterData_A/B/C.asset`（TargetDoughState 字段分别为 Softest/Medium/Hardest）

**验收：** Inspector 中各 SO 字段值与 010_ConfigSchema 默认值列一致。

---

## Phase 1 — GameLoop

### T06 · GameLoop.cs

**新建文件：**
- `Assets/Scripts/Core/GameLoop.cs`

**职责（见 001_GameLoop）：**
- 状态机：MAIN_MENU / PLAYING / PAUSED / LEVEL_TRANSITION / DEATH / VICTORY
- 订阅 OnTableDestroyed → DEATH；OnLevelCleared → LEVEL_TRANSITION or VICTORY
- 同帧冲突：OnTableDestroyed 优先于 OnLevelCleared
- Ctrl+Shift+P 切换 DevSpeedMultiplier（仅 PLAYING 状态有效）
- Script Execution Order 设为最低

**验收：** 运行游戏，Console 输出初始状态 `MAIN_MENU`；手动触发 OnLevelCleared，状态切换正确。

---

## Phase 2 — 场景搭建

### T07 · 场景 GameObject 层级

**在 Unity 中操作：**

Game.unity：
- 创建 `_Bootstrap`（挂 GameLoop）
- 创建 `_Systems/`（空节点，子节点：LaneSystem / MonsterSystem / DoughSystem / BakingSystem / ThrowSystem / LevelSystem / TableSystem）
- 创建 `_UI/`（各 Canvas，见 009_UISystem）
- 创建 `_World/Lanes/`（Lane_0~4，各含 Visual + Collider 子节点）
- 创建 `_World/MonsterContainer/`、`_World/ProjectileContainer/`、`_World/Table`

MainMenu.unity：
- 基础 Canvas + Camera

**验收：** 场景层级与 012_Architecture 的 GameObject 层级图完全一致，运行无报错。

---

## Phase 3 — 系统实现

### T08 · LaneSystem

**新建文件：**
- `Assets/Scripts/Systems/LaneManager.cs` — 管理 5 条球道悬停状态，提供 GetWaypoint / GetHoveredLaneIndex API
- `Assets/Scripts/Systems/LaneHoverDetector.cs` — 挂载于 Collider 子节点，检测 OnMouseEnter/Exit 并通知 LaneManager

**关键细节（见 002_LaneSystem）：**
- Awake 验证 RecordedStepCount == MonsterConfig.MoveStepCount
- 仅在 PLAYING + BakingState ≠ Idle 时响应悬停
- Scale 仅作用于 Visual 节点，不影响 Collider

**验收：** 002_LaneSystem Acceptance Criteria 全部通过（Test 1~5）。

---

### T09 · DoughSystem

**新建文件：**
- `Assets/Scripts/Systems/DoughSystem.cs`

**关键细节（见 004_DoughSystem）：**
- 左键：`ratio -= FlourClickAmount`；右键长按：`ratio += WaterFillRate * Time.deltaTime`
- 仅在 PLAYING + BakingState==Idle + DoughState!=None 时接受输入
- 同档位内 ratio 变化不重复广播 OnDoughStateChanged
- OnThrowStarted → DoughState=None；OnThrowCompleted → reset ratio

**验收：** 004_DoughSystem Acceptance Criteria 全部通过（Test 1~6）。

---

### T10 · TableSystem

**新建文件：**
- `Assets/Scripts/Systems/TableSystem.cs`

**关键细节（见 008_TableSystem）：**
- OnMonsterReachedTable → HP-=1；HP==0 → OnTableDestroyed（guard 防重复）
- OnLevelStarted → HP = MaxHits

**验收：** 008_TableSystem Acceptance Criteria 全部通过（Test 1~3）。

---

### T11 · MonsterSystem

**新建文件：**
- `Assets/Scripts/Systems/MonsterController.cs` — 单只怪物：状态机、移动计时、动画、缩放插值、闪白
- `Assets/Scripts/Systems/MonsterSystem.cs` — 管理 5 条球道的怪物数组，提供所有公开 API

**关键细节（见 003_MonsterSystem）：**
- 移动：`pos(t) = Lerp(from, to, t)`，Scale 同步插值 ScaleCurve.Evaluate(lerp(pos, nextPos, t))
- PendingMove：剩余时间 ≤ PendingMoveThreshold 时 GetTargetPosition() 返回下一点位
- 最后点位 PendingMove → GetTargetPosition() 返回当前点（不越界）
- DefeatMonster：立即停止动画销毁；TriggerWrongHitFeedback：闪白与移动并行

**验收：** 003_MonsterSystem Acceptance Criteria 全部通过（Test 1~6）。

---

### T12 · BakingSystem

**新建文件：**
- `Assets/Scripts/Systems/BakingSystem.cs`

**关键细节（见 005_BakingSystem）：**
- 空格按下 → Undercooked；三阈值推进；松开或超时 → OnThrowRequested(laneIndex)
- 无悬停时 Random.Range(0,5) 选道
- DoughState==None 时空格无效
- 计时器在 Update 中 `+= Time.deltaTime`（TimeScale=0 自动暂停）

**验收：** 005_BakingSystem Acceptance Criteria 全部通过（Test 1~6）。

---

### T13 · ThrowSystem

**新建文件：**
- `Assets/Scripts/Systems/ThrowSystem.cs`

**关键细节（见 006_ThrowSystem）：**
- OnThrowRequested → 同帧捕获 ratio + targetPos → 实例化投射物 → OnThrowStarted
- 抛物线：`pos(t) = Lerp(start, end, t) + up * PeakHeight * 4t(1-t)`
- 到达时：`|capturedRatio - doughStateCenter| <= ToleranceHalfWidth` → Hit；否则 WrongRatio / EmptyLane
- 即时判定：DefeatMonster 或 TriggerWrongHitFeedback → OnThrowCompleted

**验收：** 006_ThrowSystem Acceptance Criteria 全部通过（Test 1~6）。

---

### T14 · LevelSystem

**新建文件：**
- `Assets/Scripts/Systems/LevelSystem.cs`

**关键细节（见 007_LevelSystem）：**
- 第一只怪物在第一个 SpawnIntervalSeconds **后**生成
- 全满 → 跳过 + `_spawnTimer = 0`（完整间隔重置）
- ExitedCount（Defeated + ReachedTable）== TotalMonsters → OnLevelCleared
- `[SerializeField] MonsterData[] _availableMonsterTypes`，Awake 检查非空

**验收：** 007_LevelSystem Acceptance Criteria 全部通过（Test 1~5）。

---

## Phase 4 — UI

### T15 · UISystem（Canvas 切换）

**新建文件：**
- `Assets/Scripts/UI/UISystem.cs`

**职责：** 根据 OnGameStateChanged 控制各 Canvas 面板 SetActive；填充 LevelTransition / Death / Victory 文案。

**关键细节（见 009_UISystem）：**
- LevelTransition 文案使用 `_clearedLevelIndex`（在 OnLevelCleared 时缓存，而非 OnGameStateChanged 时读取）
- PauseMenu 的「继续」「返回主菜单」按钮通过 Button.OnClick 调用 GameLoop 公开方法

**验收：** 009_UISystem Acceptance Criteria（Test 1）通过；所有状态下正确的 Canvas 显示。

---

### T16 · RatioBar（比例条）

**新建文件：**
- `Assets/Scripts/UI/RatioBar.cs`

**关键细节（见 009_UISystem）：**
- `_displayedRatio = Mathf.Lerp(_displayedRatio, DoughSystem.GetCurrentRatio(), ElasticSpeed * Time.deltaTime)`
- 方向：左=水（高 ratio），右=面粉（低 ratio）
- 三条参考线位置由 DoughStateBoundaryConfig 计算
- DoughState==None → 指示器隐藏；OnThrowCompleted → snap（直接赋值，无弹性）

**验收：** 009_UISystem Acceptance Criteria（Test 2~3, 6）通过。

---

### T17 · BakingIndicator + TableHPBar

**新建文件：**
- `Assets/Scripts/UI/BakingIndicator.cs` — 每帧读 `BakingSystem.GetBakingTimer()`，按 BakingState 变色+填充进度
- `Assets/Scripts/UI/TableHPBar.cs` — 订阅 OnMonsterReachedTable / OnLevelStarted，更新 fillAmount

**验收：** 009_UISystem Acceptance Criteria（Test 4~5）通过。

---

## Phase 5 — DevTools

### T18 · LaneCalculator（Editor 工具）

**新建文件：**
- `Assets/Scripts/Config/LaneWaypoints.cs`（若 T03 未含则补充）
- `Assets/Editor/LaneCalculatorData.cs` — Editor-only SO，存 Y 坐标列表
- `Assets/Editor/LaneCalculatorWindow.cs` — EditorWindow，含 Auto Distribute + Bake + Preview

**关键细节（见 011_DevTools）：**
- 顶点按 X 排序识别左右边缘
- 交点公式：`t = (y - y1) / (y2 - y1)；x = x1 + t * (x2 - x1)`
- Bake 后写入 RecordedStepCount = MonsterConfig.MoveStepCount
- Scene Preview 用 `Handles.SphereHandleCap`

**验收：** 011_DevTools Acceptance Criteria 全部通过（Test 1~3）。

---

## Phase 6 — 集成

### T19 · Prefab 制作

**在 Unity 中制作：**
- `Prefabs/Monsters/Monster_A.prefab`（含 Visual 子节点 + MonsterController）
- `Prefabs/Monsters/Monster_B.prefab`
- `Prefabs/Monsters/Monster_C.prefab`
- `Prefabs/Projectile/Bread.prefab`（含 SpriteRenderer）
- Lane_0~4 Prefab（含 Visual + Collider 子节点，PolygonCollider2D 已设顶点）

**验收：** Prefab 结构与 003 / 006 / 002 Spec 的 GameObject 结构图一致。

---

### T20 · Inspector 连线（引用注入）

**在 Unity 中操作：**
- 所有系统组件的 `[SerializeField]` 字段赋值（SO 引用、Transform 引用、MonsterData 数组等）
- LaneManager：LaneWaypointConfig、MonsterConfig、HoverScaleMultiplier
- ThrowSystem：ThrowConfig、ThrowOrigin（桌子中心空节点）
- LevelSystem：LevelConfig、_availableMonsterTypes[]
- UISystem：各 Canvas 引用、DoughStateBoundaryConfig

**验收：** 运行游戏无 `NullReferenceException`，所有系统 Awake Validate 无 Error。

---

### T21 · 球道点位烘焙

**操作步骤：**
1. 打开 `GWBGameJam / Lane Waypoint Calculator`
2. 配置引用（MonsterConfig、LaneWaypointConfig、LaneCalculatorData）
3. 点击「Auto Distribute」→「Preview」验证点位位置
4. 点击「Bake」
5. 运行游戏验证 LaneSystem 不报 RecordedStepCount 过期 Error

**验收：** 40 个点位正确写入；游戏启动无过期 Error；Scene Preview 球体位置符合透视关系。

---

### T22 · 端到端集成测试

**测试路径：**
1. 主菜单点击「开始游戏」→ 进入第一关
2. 调整水粉比至 Medium → 长按空格烤制 → 瞄准有怪物的球道 → 松开 → 怪物消失
3. 让怪物碰桌 MaxHits 次 → 死亡界面
4. 完成第一关所有怪物 → 关卡过渡界面 → 进入第二关
5. 通关第三关 → 胜利界面
6. Ctrl+Shift+P 切换加速模式，验证游戏加速运行

**验收：** 所有 001_GameLoop Acceptance Criteria 通过；全流程无报错。

---

## 任务进度追踪

| Task | 描述 | 状态 |
|------|------|------|
| T01 | Assembly Definitions + 目录 | ✅ 完成 |
| T02 | GameEnums + EventBus | ✅ 完成 |
| T03 | SO 类定义（12 个） | ✅ 完成 |
| T04 | Event Structs（8 个文件）| ✅ 完成 |
| T05 | SO Assets 创建与填值 | ✅ 完成（手动）|
| T06 | GameLoop | ✅ 完成 |
| T07 | 场景层级搭建 | ✅ 完成（手动）|
| T08 | LaneSystem | ✅ 完成 |
| T09 | DoughSystem | ⬜ 未开始 |
| T10 | TableSystem | ⬜ 未开始 |
| T11 | MonsterSystem | ⬜ 未开始 |
| T12 | BakingSystem | ⬜ 未开始 |
| T13 | ThrowSystem | ⬜ 未开始 |
| T14 | LevelSystem | ⬜ 未开始 |
| T15 | UISystem（Canvas 切换）| ⬜ 未开始 |
| T16 | RatioBar | ⬜ 未开始 |
| T17 | BakingIndicator + TableHPBar | ⬜ 未开始 |
| T18 | LaneCalculator（Editor）| ⬜ 未开始 |
| T19 | Prefab 制作 | ⬜ 未开始 |
| T20 | Inspector 连线 | ⬜ 未开始 |
| T21 | 球道点位烘焙 | ⬜ 未开始 |
| T22 | 端到端集成测试 | ⬜ 未开始 |

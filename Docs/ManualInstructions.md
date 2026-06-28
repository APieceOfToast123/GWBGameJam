# 手动操作指引 — T05 & T07

需要在 Unity Editor 中完成的操作。建议按顺序执行，完成后告知 Claude 继续编码。

---

## T05 · SO Assets 创建与默认值填写

**前置：** 确认 Unity 已编译成功（Console 无红色 Error）。

### Step 1 — 创建 Configs 文件夹下的 SO

在 Project 窗口中进入 `ScriptableObjects/Configs/`，右键 → Create：

| 菜单路径 | 文件名 |
|---------|--------|
| GWBGameJam/Configs/GameLoopConfig | `GameLoopConfig` |
| GWBGameJam/Configs/BakingConfig | `BakingConfig` |
| GWBGameJam/Configs/MonsterConfig | `MonsterConfig` |
| GWBGameJam/Configs/LevelConfig | `LevelConfig` |
| GWBGameJam/Configs/TableConfig | `TableConfig` |
| GWBGameJam/Configs/DoughConfig | `DoughConfig` |
| GWBGameJam/Configs/DoughStateBoundaryConfig | `DoughStateBoundaryConfig` |
| GWBGameJam/Configs/LaneWaypointConfig | `LaneWaypointConfig` |
| GWBGameJam/Configs/ThrowConfig | `ThrowConfig` |

### Step 2 — 创建 MonsterData

进入 `ScriptableObjects/MonsterData/`，右键 → Create → GWBGameJam/MonsterData：

| 文件名 | TargetDoughState 字段值 |
|--------|------------------------|
| `MonsterData_A` | Softest |
| `MonsterData_B` | Medium |
| `MonsterData_C` | Hardest |

### Step 3 — 验证默认值

点击每个 SO，在 Inspector 中确认字段值与下表一致（脚本已设好默认值，应自动填入）：

| SO | 关键字段确认 |
|----|------------|
| GameLoopConfig | TotalLevels = 3, DevSpeedMultiplier = 3 |
| BakingConfig | Undercooked = 0.5, Cooked = 1.5, Burnt = 2.5 |
| MonsterConfig | MoveInterval = 1.0, MoveStepCount = 8, MoveDuration = 0.3 |
| LevelConfig | Levels 数组长度 = 3（Lv1: 9s/10只，Lv2: 7s/15只，Lv3: 5s/20只）|
| TableConfig | MaxHits = 5 |
| DoughConfig | FlourClick = 0.75, WaterFill = 0.5, InitialRatio = 1.0, MaxRatio = 3.0 |
| DoughStateBoundaryConfig | 1.75 / 1.25 / 0.75 / 0.25 / Tolerance = 0.25 |
| ThrowConfig | ThrowDuration = 0.4, PeakHeight = 3.0 |

### Step 4 — MonsterConfig ScaleCurve 设置

点击 MonsterConfig，找到 ScaleCurve 字段，双击打开曲线编辑器，添加以下关键帧：

| Time（X）| Value（Y）|
|----------|----------|
| 0 | 0.01 |
| 2 | 0.50 |
| 5 | 1.00 |
| 7 | 1.50 |

✅ **T05 完成标志：** Project 窗口中 ScriptableObjects/ 下可见所有 .asset 文件，Inspector 字段无异常。

---

## T07 · 场景 GameObject 层级搭建

**前置：** T05 完成。

### Game.unity

打开（或新建）`Assets/Scenes/Game.unity`。

#### 1. 删除默认 Main Camera（如有），稍后重建

#### 2. 创建顶层节点（均为空 GameObject）

```
Hierarchy 右键 → Create Empty，依次命名：
  _Bootstrap
  _Systems
  _UI
  _World
```

#### 3. _Bootstrap

选中 `_Bootstrap`，Inspector → Add Component → 搜索 `GameLoop`，挂载。

> ⚠️ Script Execution Order 设置：
> Edit → Project Settings → Script Execution Order
> 点击 "+" → 选择 GameLoop → 将数值设为 **9999**（最晚执行）

#### 4. _Systems 子节点（均为空 GameObject，挂载位置备用）

在 `_Systems` 下依次创建以下空 GameObject（先建节点，脚本后续编码后再挂）：

```
_Systems
├── LaneSystem
├── MonsterSystem
├── DoughSystem
├── BakingSystem
├── ThrowSystem
├── LevelSystem
└── TableSystem
```

#### 5. _World 子节点

```
_World
├── Lanes               ← 空节点，装5条球道
│   ├── Lane_0
│   │   ├── Visual      ← 挂 SpriteRenderer
│   │   └── Collider    ← 挂 PolygonCollider2D
│   ├── Lane_1
│   │   ├── Visual
│   │   └── Collider
│   ├── Lane_2
│   │   ├── Visual
│   │   └── Collider
│   ├── Lane_3
│   │   ├── Visual
│   │   └── Collider
│   └── Lane_4
│       ├── Visual
│       └── Collider
├── MonsterContainer    ← 空节点
├── ProjectileContainer ← 空节点
└── Table               ← 挂 SpriteRenderer（桌子图片，暂时留空）
```

> PolygonCollider2D 的顶点此时不用精确设置，等美术资源到位或用 LaneCalculator 后再调整。

#### 6. _UI 子节点（Canvas 面板）

先创建一个父节点 `_UI`（空 GameObject）。

在 `_UI` 下，用 **右键 → UI → Canvas** 依次创建以下 Canvas：

```
_UI
├── HUD_Canvas              ← Render Mode: Screen Space - Overlay
│   ├── RatioBar            ← 空节点（UI 脚本后续编码后添加子元素）
│   ├── BakingIndicator     ← 空节点
│   └── TableHPBar          ← 空节点
├── PauseMenu_Canvas
├── LevelTransition_Canvas
├── Death_Canvas
└── Victory_Canvas
```

> ⚠️ 不要在 Game.unity 里创建 MainMenu_Canvas。返回主菜单通过 SceneManager.LoadScene("MainMenu") 实现，由 GameLoop.GoToMainMenu() 触发，不需要 Canvas 切换。

> 每个 Canvas 都勾选 `EventSystem`（Unity 会自动创建一个，重复的删掉只保留一个）。

#### 7. Main Camera

在 Hierarchy 右键 → Camera，命名 `Main Camera`，保持默认设置即可。

#### 8. 保存场景

Ctrl+S，确认保存到 `Assets/Scenes/Game.unity`。

---

### MainMenu.unity

新建场景：File → New Scene → Basic（URP 或 Built-in，与 Game.unity 一致）。

创建结构：
```
MainMenu.unity
├── Main Camera
└── MainMenu_Canvas
    ├── TitleText        ← UI → Text (TMP)，内容：「揉面团」
    └── StartButton      ← UI → Button (TMP)，文字：「开始游戏」
```

保存到 `Assets/Scenes/MainMenu.unity`。

> StartButton 的 OnClick 事件暂时留空，等 T15 UISystem 完成后再连线。

✅ **T07 完成标志：** 两个场景可以正常打开，Hierarchy 层级与上方描述一致，运行 Game.unity 无红色报错（此时 GameLoop 会因缺少 Config 引用报 NullRef，下一步 T20 连线后解决）。

---

## 完成后通知 Claude

两个任务都做完后，告诉 Claude：

```
T05 和 T07 完成，继续 T08。
```

Claude 会继续写 LaneSystem 的代码。

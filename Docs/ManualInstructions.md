# 手动操作指引

需要在 Unity Editor 中完成的操作，按任务顺序执行。

---

## 已完成（参考用）

- **T05** — SO Assets 创建与默认值填写
- **T07** — 场景 GameObject 层级搭建

详细步骤保留于本文件历史版本，不再重复。

---

## T19 · Prefab 制作

**前置：** 所有脚本编译通过（Console 无红色 Error）。

### 1. Monster Prefab（共 3 个）

在 `Assets/Prefabs/Monsters/` 目录下，为每种怪物制作 Prefab。

**结构（每个 Prefab 相同）：**
```
Monster_A          ← 空 GameObject，挂 MonsterController.cs
└── Visual         ← 空 GameObject，挂 SpriteRenderer
```

**步骤：**

1. Hierarchy 右键 → Create Empty，命名 `Monster_A`
2. 在 `Monster_A` 下创建子节点 `Visual`（右键 → Create Empty）
3. 选中 `Visual` → Add Component → `Sprite Renderer`
4. 选中 `Monster_A` → Add Component → `Monster Controller`
5. 在 Inspector 中将以下字段赋值：
   - `_visual` → 拖入 `Visual` 节点（Transform）
   - `_spriteRenderer` → 拖入 `Visual` 节点（SpriteRenderer）
6. 将 `Monster_A` 拖入 Project 窗口 `Assets/Prefabs/Monsters/` 文件夹，创建 Prefab
7. 在 Hierarchy 中删除刚才的 `Monster_A` 临时对象

重复以上步骤，创建 `Monster_B.prefab` 和 `Monster_C.prefab`（结构完全相同，MonsterData 在 T20 Inspector 连线时区分）。

> MonsterData（A/B/C 对应不同 Sprite 和 TargetDoughState）在 MonsterSystem.SpawnMonster 时注入，Prefab 本身不区分外观——外观由 MonsterController.Initialize() 在运行时设置。

---

### 2. Bread Prefab（投射物）

在 `Assets/Prefabs/Projectile/` 目录下：

1. Hierarchy 右键 → Create Empty，命名 `Bread`
2. Add Component → `Sprite Renderer`（后续替换为面包图片）
3. 拖入 `Assets/Prefabs/Projectile/`，创建 Prefab，删除 Hierarchy 临时对象

---

### 3. Explosion Prefab（可选，命中特效）

如果有爆炸动画资源：

1. 创建 `Explosion` GameObject，挂需要的动画/粒子组件
2. 保存至 `Assets/Prefabs/Projectile/Explosion.prefab`

> 无此 Prefab 时 ThrowSystem 打印 Warning 但不崩溃，可先跳过。

✅ **T19 完成标志：** Project 窗口中 `Prefabs/` 目录下可见 `Monster_A/B/C.prefab` 和 `Bread.prefab`。

---

## T20 · Inspector 连线（引用注入）

**前置：** T19 完成，Game.unity 处于打开状态。

> 以下"拖入"均指将 Project 或 Hierarchy 中的对象拖到 Inspector 对应字段。

---

### ★ 先做：两处 GameLoop 集成修复

当前 GameLoop.Start() 调用 `TransitionTo(MainMenu)`，但 Game.unity 没有 MainMenu_Canvas，场景加载后屏幕为空。需要做以下两处调整：

**A. 让 Game.unity 自动开始游戏**

打开 `Assets/Scripts/Core/GameLoop.cs`，将 `Start()` 方法改为：

```csharp
private void Start()
{
    StartGame(); // Game 场景加载即开始第一关
}
```

**B. 让"返回主菜单"真正切换场景**

在 `GameLoop.cs` 文件顶部添加 `using UnityEngine.SceneManagement;`，然后修改 `GoToMainMenu()`：

```csharp
public void GoToMainMenu()
{
    Time.timeScale = 1f;
    _devSpeedActive = false;
    SceneManager.LoadScene("MainMenu");
}
```

**C. MainMenu 的 Start 按钮**

打开 `Assets/Scenes/MainMenu.unity`，选中 `StartButton`：
- Inspector → Button → On Click → 点击 "+"
- 将空节点（或任意持有脚本的对象）拖入，选择一个加载场景的方法

最简单方式：新建脚本 `Assets/Scripts/Core/SceneLoader.cs`：

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GWBGameJam
{
    public class SceneLoader : MonoBehaviour
    {
        public void LoadGame() => SceneManager.LoadScene("Game");
    }
}
```

将 `SceneLoader` 挂在 `MainMenu_Canvas`（或 `StartButton`）上，OnClick → `SceneLoader.LoadGame()`。

> 确认 Build Settings 中已添加两个场景：`MainMenu`（Index 0）和 `Game`（Index 1）。

---

### _Bootstrap

选中 `_Bootstrap` 节点：

| 组件 | 字段 | 赋值 |
|------|------|------|
| GameLoop | `_config` | 拖入 `ScriptableObjects/Configs/GameLoopConfig` |

---

### LaneSystem 节点（挂有 LaneManager）

选中 `_Systems/LaneSystem`：

| 字段 | 赋值 |
|------|------|
| `_waypointConfig` | `ScriptableObjects/Configs/LaneWaypointConfig` |
| `_monsterConfig` | `ScriptableObjects/Configs/MonsterConfig` |

选中每个 `_World/Lanes/Lane_X/Collider` 节点（共5个），确认挂有 `LaneHoverDetector`，并赋值：

| 字段 | 赋值 |
|------|------|
| `_laneManager` | 拖入 `_Systems/LaneSystem`（其上的 LaneManager 组件）|

---

### MonsterSystem 节点

选中 `_Systems/MonsterSystem`：

| 字段 | 赋值 |
|------|------|
| `_config` | `ScriptableObjects/Configs/MonsterConfig` |
| `_laneManager` | 拖入 `_Systems/LaneSystem` |
| `_monsterContainer` | 拖入 `_World/MonsterContainer` |
| `_monsterPrefab` | 拖入 `Prefabs/Monsters/Monster_A`（任意一个，因为 Prefab 结构相同）|

---

### DoughSystem 节点

选中 `_Systems/DoughSystem`：

| 字段 | 赋值 |
|------|------|
| `_config` | `ScriptableObjects/Configs/DoughConfig` |
| `_boundaryConfig` | `ScriptableObjects/Configs/DoughStateBoundaryConfig` |

---

### BakingSystem 节点

选中 `_Systems/BakingSystem`：

| 字段 | 赋值 |
|------|------|
| `_config` | `ScriptableObjects/Configs/BakingConfig` |
| `_laneManager` | 拖入 `_Systems/LaneSystem` |
| `_doughSystem` | 拖入 `_Systems/DoughSystem` |

---

### ThrowSystem 节点

选中 `_Systems/ThrowSystem`：

| 字段 | 赋值 |
|------|------|
| `_config` | `ScriptableObjects/Configs/ThrowConfig` |
| `_doughSystem` | 拖入 `_Systems/DoughSystem` |
| `_monsterSystem` | 拖入 `_Systems/MonsterSystem` |
| `_laneManager` | 拖入 `_Systems/LaneSystem` |
| `_monsterConfig` | `ScriptableObjects/Configs/MonsterConfig` |
| `_boundaryConfig` | `ScriptableObjects/Configs/DoughStateBoundaryConfig` |
| `_throwOrigin` | 拖入 `_World/Table`（或在桌子上创建一个 `ThrowOrigin` 空节点，代表投出点位）|
| `_projectilePrefab` | 拖入 `Prefabs/Projectile/Bread` |
| `_explosionPrefab` | 拖入 `Prefabs/Projectile/Explosion`（有则填，无则留空）|

---

### TableSystem 节点

选中 `_Systems/TableSystem`：

| 字段 | 赋值 |
|------|------|
| `_config` | `ScriptableObjects/Configs/TableConfig` |

---

### LevelSystem 节点

选中 `_Systems/LevelSystem`：

| 字段 | 赋值 |
|------|------|
| `_levelConfig` | `ScriptableObjects/Configs/LevelConfig` |
| `_monsterSystem` | 拖入 `_Systems/MonsterSystem` |
| `_availableMonsterTypes`（数组，Size=3）| [0] `MonsterData_A`，[1] `MonsterData_B`，[2] `MonsterData_C` |

---

### UISystem 节点

选中 `_UI`（或其上的 UISystem 组件）：

| 字段 | 赋值 |
|------|------|
| `_hudCanvas` | 拖入 `_UI/HUD_Canvas` |
| `_pauseMenuCanvas` | 拖入 `_UI/PauseMenu_Canvas` |
| `_levelTransitionCanvas` | 拖入 `_UI/LevelTransition_Canvas` |
| `_deathCanvas` | 拖入 `_UI/Death_Canvas` |
| `_victoryCanvas` | 拖入 `_UI/Victory_Canvas` |
| `_levelTransitionText` | 拖入 `LevelTransition_Canvas` 内的过关文本 TMP 组件 |
| `_levelSystem` | 拖入 `_Systems/LevelSystem` |

---

### PauseMenu 按钮连线

展开 `PauseMenu_Canvas`，找到「继续」和「返回主菜单」两个按钮：

| 按钮 | On Click | 方法 |
|------|----------|------|
| 继续按钮 | 拖入 `_Bootstrap` | `GameLoop.ResumeGame()` |
| 返回主菜单按钮 | 拖入 `_Bootstrap` | `GameLoop.GoToMainMenu()` |

LevelTransition / Death / Victory 面板中的确认按钮：

| 按钮 | On Click | 方法 |
|------|----------|------|
| 关卡过渡「继续」 | 拖入 `_Bootstrap` | `GameLoop.AdvanceFromTransition()` |
| 死亡「返回主菜单」 | 拖入 `_Bootstrap` | `GameLoop.GoToMainMenu()` |
| 胜利「返回主菜单」 | 拖入 `_Bootstrap` | `GameLoop.GoToMainMenu()` |

---

### RatioBar 节点

展开 `HUD_Canvas/RatioBar`，选中其上的 `RatioBar` 组件：

| 字段 | 赋值 |
|------|------|
| `_doughSystem` | 拖入 `_Systems/DoughSystem` |
| `_doughConfig` | `ScriptableObjects/Configs/DoughConfig` |
| `_boundaryConfig` | `ScriptableObjects/Configs/DoughStateBoundaryConfig` |
| `_indicatorRect` | 拖入指示器子节点的 RectTransform |
| `_indicatorVisual` | 拖入指示器子节点的 GameObject |
| `_softestRefLine` | 拖入最软参考线的 RectTransform |
| `_mediumRefLine` | 拖入中等参考线的 RectTransform |
| `_hardestRefLine` | 拖入最硬参考线的 RectTransform |

---

### BakingIndicator 节点

展开 `HUD_Canvas/BakingIndicator`，选中其上的 `BakingIndicator` 组件：

| 字段 | 赋值 |
|------|------|
| `_bakingSystem` | 拖入 `_Systems/BakingSystem` |
| `_bakingConfig` | `ScriptableObjects/Configs/BakingConfig` |
| `_fill` | 拖入进度条的 Fill Image 组件 |

---

### TableHPBar 节点

展开 `HUD_Canvas/TableHPBar`，选中其上的 `TableHPBar` 组件：

| 字段 | 赋值 |
|------|------|
| `_tableSystem` | 拖入 `_Systems/TableSystem` |
| `_fill` | 拖入进度条的 Fill Image 组件 |

---

### 连线完成验证

运行游戏（Play 模式），Console 窗口中：
- **无红色 [Error] 日志**（各系统 Awake Validate 通过）
- 预期看到第一关立即开始，HUD_Canvas 显示

✅ **T20 完成标志：** Play 模式下无 `[Error]` 日志，可见 HUD 界面。

---

## T21 · 球道点位烘焙

**前置：** T20 完成，Game.unity 处于打开（非 Play）模式，Lane_0~4 的 PolygonCollider2D 顶点已设置好（梯形形状，符合透视球道）。

### 步骤

1. 菜单栏 → **GWBGameJam → Lane Waypoint Calculator**（打开 Editor 窗口）

2. 赋值三个引用：
   - `MonsterConfig` → 拖入 `ScriptableObjects/Configs/MonsterConfig`
   - `LaneWaypointConfig` → 拖入 `ScriptableObjects/Configs/LaneWaypointConfig`
   - `LaneCalculatorData` → 拖入 `ScriptableObjects/Editor/LaneCalculatorData`（若无，先 Project 右键 → Create → GWBGameJam/Editor/LaneCalculatorData 创建一个）

3. 点击 **Auto Distribute** → 窗口自动根据场景中各球道 PolygonCollider2D 的 Y 范围均分出 8 个 Y 坐标

4. 勾选 **Scene Preview** → 在 Scene 视图中确认小球位置符合透视关系（远处小、近处大的球道中线上）

5. 如果点位需要调整，直接在窗口的 `[i] Y` 字段手动修改

6. 点击 **Bake** → 状态栏显示"Bake 完成"，LaneWaypointConfig 已写入 40 个点位（5 条球道 × 8 步）

7. 运行游戏：Console 中 LaneManager 若输出 `RecordedStepCount` 不匹配 Error，说明点位未生效，检查 LaneWaypointConfig 是否正确赋值给 LaneManager

✅ **T21 完成标志：** Bake 状态栏显示完成，Play 模式下 LaneManager Awake 无报错，怪物移动路径符合球道形状。

---

## T22 · 端到端集成测试

**前置：** T21 完成。按以下路径逐一测试：

### 测试 1 — 完整游戏循环

| 步骤 | 预期结果 |
|------|---------|
| 运行 MainMenu.unity，点击「开始游戏」 | 跳转到 Game.unity，HUD 显示，第一只怪物在 SpawnIntervalSeconds 后出现 |
| 不操作，等怪物走到桌子 | TableHPBar 减少一格 |
| 让怪物碰桌 MaxHits（5）次 | 出现 Death_Canvas，HUD 消失 |
| 点击「返回主菜单」 | 回到 MainMenu.unity |

### 测试 2 — 正确击杀

| 步骤 | 预期结果 |
|------|---------|
| 右键长按（加水）至 RatioBar 指示器进入 Medium 区间 | 比例条指示器弹性移动到中间 |
| 瞄准有怪物的球道（球道高亮），长按空格 | BakingIndicator 开始填充，颜色为黄色 |
| 等进入绿色（Cooked）后松开空格 | 面包沿抛物线飞出，命中怪物消失，出现爆炸特效（如有）|

### 测试 3 — 错误比例击中

| 步骤 | 预期结果 |
|------|---------|
| 将比例调至与怪物不匹配的档位，投出面包 | 怪物闪白 WrongHitFlashCount 次，不消失，继续前进 |

### 测试 4 — 关卡过渡

| 步骤 | 预期结果 |
|------|---------|
| 消灭第一关所有 10 只怪物 | 显示 LevelTransition_Canvas，文本为「第 1 关通过！」|
| 点击「继续」 | 进入第二关，SpawnInterval 缩短（7s），HUD 恢复显示 |

### 测试 5 — 通关

| 步骤 | 预期结果 |
|------|---------|
| 消灭第三关所有 20 只怪物 | 显示 Victory_Canvas（不进入 LevelTransition）|

### 测试 6 — Dev 加速

| 步骤 | 预期结果 |
|------|---------|
| 游戏运行中按 Ctrl+Shift+P | 游戏加速运行（TimeScale = DevSpeedMultiplier = 3）|
| 再次按 Ctrl+Shift+P | 恢复正常速度 |

### 测试 7 — 暂停

| 步骤 | 预期结果 |
|------|---------|
| 游戏中按 Escape | PauseMenu_Canvas 显示，游戏暂停（怪物停止移动）|
| 点击「继续」 | 恢复游戏，怪物继续从暂停位置移动 |

---

✅ **T22 完成标志：** 所有 7 个测试路径无报错，Console 无意外 Error/Warning，游戏循环完整可玩。

---

## 完成后通知 Claude

T19~T22 全部完成后，告诉 Claude：

```
T19~T22 完成，游戏可以正常运行。
```

Claude 会帮你确认是否有遗漏的 Bug 修复或打磨工作。

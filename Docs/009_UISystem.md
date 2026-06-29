# 009 UISystem Spec

> 2026-06-29 Ingredient Texture Animation Revision：RatioBar 的面粉区与水区分别使用 6 帧代码驱动 Sprite 翻页。常态 6 fps 循环；有效左键加粉时面粉区触发 0.2 秒快速翻页与 1.1 倍脉冲；有效右键按下时水区触发同样脉冲，持续按住期间以 3 倍速度翻页，松开恢复常速。动画触发复用 DoughSystem 的输入有效性门控，暂停、烤制及 DoughState=None 时不响应；所有计时使用 Time.deltaTime。

> 2026-06-29 RatioBar Visual Revision：比例条使用唯一绿色竖线作为当前比例 Indicator；绿线左侧显示面粉纹理，右侧显示水纹理，两侧区域随 Indicator 移动动态改变宽度并限制在比例条范围内。三条原 `RefLine_*` 视觉对象改作 Softest / Medium / Hardest 三种面包制作成功区间。

> 2026-06-29 Gameplay Revision：烤制进度条按本次锁定的分档 CookDuration 归一化；完成前、完美窗口、烤焦分别显示不同状态。比例进入有效容错区间时显示对应面包名称与对勾。场景和 Canvas 结构不变。

| 字段 | 内容 |
|------|------|
| Version | 1.0 |
| Status | Draft |
| Last Updated | 2026-06-27 |
| Depends On | 001_GameLoop, 004_DoughSystem, 005_BakingSystem, 008_TableSystem, 010_ConfigSchema, 012_Architecture |
| Required By | — |

---

## Goal

根据游戏状态显示/隐藏各 Canvas 面板，驱动 HUD 中所有动态 UI 元素（比例条弹性动画、烤制进度、桌子 HP 条）。

UISystem 是纯表现层：它只读数据、不改数据，所有 UI 更新由事件或 API 轮询驱动。

---

## Scope

**In Scope：**
- 根据 OnGameStateChanged 控制各 Canvas 面板的显示/隐藏
- 比例条：维护弹性插值显示值，每帧朝 DoughSystem.GetCurrentRatio() 靠拢
- 比例条参考线：在条上标注三个有效档位的容错区间（Softest / Medium / Hardest）
- 烤制指示器：根据 OnBakingStateChanged 变色 + 读取 GetBakingTimer() 驱动进度
- 桌子 HP 条：订阅 OnMonsterReachedTable / OnLevelStarted 更新显示
- 关卡过渡面板：显示关卡号（由 OnLevelStarted 携带的 levelIndex 推算当前已通关数）
- 各全屏面板文本填充（关卡号、死亡/胜利文案）

**Out of Scope：**
- 球道悬停高亮 → LaneSystem（Sprite 替换与 Scale）
- 怪物闪白动画 → MonsterSystem
- 投掷特效 Prefab 的实例化 → ThrowSystem
- 音效播放 → 游戏规模内可由各系统直接调用 AudioSource，不走 UISystem

---

## Canvas 面板一览

```
_UI/
├── HUD_Canvas          ← 游戏进行中始终可见（PLAYING / PAUSED）
│   ├── RatioBar        ← 水粉比条（弹性动画）
│   ├── BakingIndicator ← 烤制状态指示器
│   └── TableHPBar      ← 桌子 HP 条
├── MainMenu_Canvas     ← MAIN_MENU 状态
├── PauseMenu_Canvas    ← PAUSED 状态（叠加于 HUD 上层）
├── LevelTransition_Canvas ← LEVEL_TRANSITION 状态
├── Death_Canvas        ← DEATH 状态
└── Victory_Canvas      ← VICTORY 状态
```

### 各状态下的 Canvas 可见性

| 状态 | HUD | MainMenu | PauseMenu | LevelTransition | Death | Victory |
|------|-----|----------|-----------|-----------------|-------|---------|
| MAIN_MENU | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| PLAYING | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ |
| PAUSED | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ |
| LEVEL_TRANSITION | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ |
| DEATH | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ |
| VICTORY | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ |

---

## HUD 元素详细设计

### RatioBar（水粉比条）

**布局方向：左=水（软），右=面粉（硬）**

```
← 水（软）              面粉（硬）→
[TooSoft][Softest][Medium][Hardest][TooHard]
    |       |        |       |        |
   1.75    1.25     1.0    0.75     0.25    ← 比例值参考
         [  ]     [  ]    [  ]            ← 容错区间参考线（ToleranceHalfWidth）
                   ▲
              当前指示器位置（弹性跟随）
```

- 指示器 X 位置 = `Remap(_displayedRatio, MaxRatio, 0, barLeft, barRight)`（高比例→左侧）
- 三条参考线（每条宽度 = ToleranceHalfWidth × barWidth / MaxRatio × 2）
  - Softest 中心（默认 1.5）± ToleranceHalfWidth
  - Medium 中心（默认 1.0）± ToleranceHalfWidth
  - Hardest 中心（默认 0.5）± ToleranceHalfWidth
- 参考线颜色与对应怪物颜色一致（策划在 Inspector 中配置）
- DoughState = None 时：指示器隐藏（面团飞出中）

**纹理翻页动画：**
- 面粉区和水区各配置 6 张独立 Sprite，由通用 `SpriteFlipbook` 组件以 6 fps 常态循环。
- 有效左键按下：面粉区以 30 fps 快速翻页 0.2 秒，并由 1.1 倍缩放回弹至原始缩放。
- 有效右键按下：水区触发相同快速翻页与缩放脉冲；持续按住时以基础速度的 3 倍播放，松开恢复。
- `RatioBar` 仅在 `DoughSystem.IsInputActive()` 为 true 时转发输入反馈，保证表现与玩法输入门控一致。
- DoughState=None 时两个纹理区域随现有 RatioBar 逻辑隐藏，组件禁用期间停止计时。

**弹性动画：**
每帧：`_displayedRatio = Mathf.Lerp(_displayedRatio, DoughSystem.GetCurrentRatio(), ElasticSpeed * Time.deltaTime)`
- `ElasticSpeed`：`[SerializeField] float`（Inspector 配置，默认 12.0）
- 游戏暂停（TimeScale = 0）时 Lerp 自然停止，无需特殊处理

### BakingIndicator（烤制状态指示器）

- 默认：灰色（Idle，面团未开始烤制）
- Undercooked：黄色，进度条从 0 填充至 `UndercookedDuration`
- Cooked：绿色，进度条继续填充至 `CookedDuration`
- Burnt：红色，进度条继续填充至 `BurntForcedThrowDuration`（满格后强制投掷）
- 进度值：每帧读取 `BakingSystem.GetBakingTimer()`
- 投掷后（BakingState 回到 Idle）：进度条归零，颜色回灰

### TableHPBar（桌子 HP 条）

- 显示剩余 HP 格数：`fillAmount = TableSystem.GetCurrentHP() / TableSystem.GetMaxHP()`
- 颜色建议：HP > 50% 绿色，≤ 50% 黄色，≤ 25% 红色（策划 Inspector 配置阈值）
- 订阅 OnMonsterReachedTable → 立即更新（不等待 OnTableDestroyed）
- 订阅 OnLevelStarted → 重置至满格

### DoughVisual（揉面面团，菜板上）

> 2026-06-29 新增。纯视觉，不影响任何玩法判定（命中仍只看水粉比 ratio）。

- 一张面团图（不分软硬、不随烤制变），UI Image，置于菜板 Image 之上，与桌板/菜板同在 Canvas 内（统一 CanvasScaler 缩放，无世界/像素错位）。
- 显示/隐藏：`DoughSystem.GetCurrentDoughState() == None` 时隐藏（投掷飞行期间）；有面团时显示。
- 揉制长大：仅在揉制阶段（Playing + BakingState==Idle，与 DoughSystem 输入门控一致）且任意鼠标键按住时，累计长大 `_heldTime += dt`（夹到 `_growDuration`）。
  - `localScale = Lerp(1, _maxScale, _heldTime / _growDuration)`，默认 100% → 300%，按满 5 秒到上限。
  - **松开不缩回**，保持当前大小;再按从当前值继续累计。
- 重置（回 100%、_heldTime=0）：换新一坨面团时 —— `OnDoughStateChanged` 的 `None → 有面团`（投掷完成后）以及 `OnLevelStarted`。
- 组件：`Assets/Scripts/UI/DoughVisual.cs`；可调字段 `_maxScale`(默认3) / `_growDuration`(默认5)。

---

## 全屏面板文本

### LevelTransition_Canvas
```
第 {levelNumber} 关通过！
按任意键继续
```
`levelNumber = levelIndex + 1`（从 1 开始显示）
文本在收到 OnGameStateChanged(LEVEL_TRANSITION) 时刷新，levelIndex 从 `LevelSystem.GetCurrentLevelIndex()` 读取。

### Death_Canvas
```
桌子损坏！
按任意键返回主菜单
```

### Victory_Canvas
```
恭喜通关！
按任意键返回主菜单
```

### MainMenu_Canvas
```
[游戏标题]
开始游戏（Button）
```

### PauseMenu_Canvas
```
暂停
继续游戏（Button → 广播 Resume）
返回主菜单（Button → 广播 GoToMainMenu）
```

---

## Data Model

**本系统拥有（Own）：**
- `float _displayedRatio` — 比例条的弹性显示值（非真实比例，仅用于渲染）

**本系统读取（Read）：**
- `DoughSystem.GetCurrentRatio()` — 每帧读取作为弹性目标
- `DoughSystem.GetCurrentDoughState()` — 判断是否隐藏指示器（DoughState.None）
- `BakingSystem.GetBakingTimer()` — 每帧读取驱动烤制进度条
- `TableSystem.GetCurrentHP() / GetMaxHP()` — HP 条比例
- `LevelSystem.GetCurrentLevelIndex()` — 关卡过渡面板文本
- `DoughStateBoundaryConfig` — 计算参考线位置与宽度

**本系统不拥有：**
- 任何游戏逻辑状态（只读，不写）

---

## Events

### 本系统不发布任何事件

按钮的点击通过 Unity UI Button OnClick 直接调用 GameLoop 的公开方法（或广播对应事件），不经由 UISystem 中转。

### 本系统订阅（Subscribe）

| 事件名 | 来源系统 | 触发后做什么 |
|--------|---------|------------|
| OnGameStateChanged | GameLoop | 切换对应 Canvas 面板的 SetActive |
| OnBakingStateChanged | BakingSystem | 更新 BakingIndicator 颜色 |
| OnMonsterReachedTable | MonsterSystem | 更新 TableHPBar fillAmount |
| OnLevelStarted | GameLoop | 重置 TableHPBar 为满格，_displayedRatio = InitialRatio（跳帧，无弹性） |

---

## Edge Cases

**#1 弹性动画期间 DoughState 变为 None（面团被投出）**
处理方式：检测到 `GetCurrentDoughState() == None` 时，隐藏比例条指示器（不显示弹性到 0 的过程）。OnThrowCompleted 后 DoughState 恢复，指示器重新显示，_displayedRatio 直接 snap 到 InitialRatio（无弹性，避免从旧位置弹回）。

**#2 ElasticSpeed 设置过高（_displayedRatio 每帧几乎等于目标值）**
退化为即时更新，无弹性效果，但功能正确。不崩溃。

**#3 ElasticSpeed 设置过低（_displayedRatio 跟不上快速操作）**
比例条显示明显滞后，但游戏逻辑（命中判定）使用真实比例，无安全问题。策划需在 Inspector 中自行调整。

**#4 LEVEL_TRANSITION 时 LevelSystem.GetCurrentLevelIndex() 已推进至下一关**
GameLoop 在发布 OnGameStateChanged(LEVEL_TRANSITION) 前已通知 LevelSystem 加载下一关。
处理方式：在 OnGameStateChanged 回调中读取 `GetCurrentLevelIndex()` 时，数值已为「即将进入的关卡」。
因此文本应显示「刚通过的关卡号 = GetCurrentLevelIndex()」（不 +1），需确认 GameLoop 与 LevelSystem 的事件顺序，或改为在 OnLevelCleared 时缓存刚通过的关卡号。

> **[Action Required]** 确认 LevelTransition 面板的「已通过关卡号」数据来源后在 GameLoop Review 中更新。当前暂定：UISystem 在收到 OnLevelCleared 时缓存 `_clearedLevelIndex = LevelSystem.GetCurrentLevelIndex()`，LevelTransition 面板使用此缓存值。

---

## Acceptance Criteria

- [ ] Given MAIN_MENU，Then 仅 MainMenu_Canvas 可见
- [ ] Given PLAYING，Then 仅 HUD_Canvas 可见
- [ ] Given PAUSED，Then HUD_Canvas + PauseMenu_Canvas 同时可见
- [ ] Given LEVEL_TRANSITION，Then LevelTransition_Canvas 显示「第X关通过！按任意键继续」
- [ ] Given DEATH，Then Death_Canvas 显示「桌子损坏！」
- [ ] Given VICTORY，Then Victory_Canvas 显示「恭喜通关！」
- [ ] Given 右键加水，When 比例向左移动，Then 比例条指示器弹性跟随（慢于比例实际变化）
- [ ] Given 左键加粉，When 比例向右移动，Then 指示器弹性跟随
- [ ] Given DoughState = None（面团飞出），Then 比例条指示器隐藏
- [ ] Given 有效左键点击，Then 面粉纹理快速翻页 0.2 秒并完成 1.1x→1.0x 脉冲
- [ ] Given 有效右键按住，Then 水纹理先脉冲并持续 3 倍速翻页，松开后恢复 6 fps
- [ ] Given 暂停、烤制或 DoughState=None，When 点击鼠标，Then 面粉与水纹理不触发操作反馈
- [ ] Given OnThrowCompleted，Then 指示器 snap 回 InitialRatio 对应位置，无弹性
- [ ] Given BakingState = Cooked，Then BakingIndicator 显示绿色，进度条填充至 Cooked 区间
- [ ] Given OnMonsterReachedTable，Then TableHPBar fillAmount 立即减少
- [ ] Given OnLevelStarted，Then TableHPBar 立即恢复满格
- [ ] Given 三条参考线，Then 各线的宽度 = ToleranceHalfWidth 在比例条上的像素宽度 × 2

---

## Test Plan

**Test 1 — Canvas 切换**
1. 运行游戏 → ✓ MainMenu 显示
2. 点击「开始游戏」→ ✓ HUD 显示，MainMenu 隐藏
3. 按 Esc → ✓ HUD + PauseMenu 同时可见
4. 怪物碰桌至 HP=0 → ✓ Death_Canvas 显示

**Test 2 — 比例条弹性动画**
1. PLAYING 且 BakingState=Idle
2. 快速左键点击 3 次（加粉）
3. ✓ 指示器在约 0.1~0.3s 内弹性移动到新位置，而非瞬移

**Test 3 — 比例条方向**
1. 右键长按（加水，比例升高）
2. ✓ 指示器向左移动（水在左）

**Test 4 — 烤制指示器**
1. 按住空格
2. ✓ 0~0.5s：黄色进度条；0.5~1.5s：绿色；1.5s+：红色；2.5s 强制投掷后归零变灰

**Test 5 — 桌子 HP**
1. MaxHits = 5，让怪物碰桌两次
2. ✓ TableHPBar fillAmount = 3/5 = 0.6

**Test 6 — 参考线位置**
1. 在 Inspector 中观察 RatioBar
2. ✓ 三条参考线位置符合 Medium(1.0) / Softest(1.5) / Hardest(0.5) 对应的条宽百分比

---

## Future Extensions

- **怪物对应目标高亮（烤制时高亮当前悬停球道的目标怪物所需档位）**：UISystem 可订阅 OnLaneHoverChanged + MonsterSystem.GetMonsterInLane()，动态高亮对应参考线，**无需修改其他系统**。
- **本地化文本**：当前文本硬编码于 UI 组件；替换为 TextMeshProUGUI + 本地化 key 表，**UISystem 结构支持**。
- **关卡进度 UI（X/TotalMonsters）**：若策划后期决定显示，UISystem 订阅 OnMonsterDefeated / OnMonsterReachedTable 即可，**LevelSystem API 已支持**。

# ChatGPT Implementation Prompt — GWBGameJam 2D Scene Setup

> 复制以下全部内容给 ChatGPT，它将手把手指导你完成整个实现过程。

---

## Role & Context

You are a Unity developer implementing a 2D "kneading dough" mini-game (GWBGameJam entry). The project already has ALL game logic coded—35+ C# files covering 7 gameplay systems, EventBus, UI scripts, config SOs, and scene hierarchy. What remains is:

1. **Create placeholder sprites** so things are visible (zero art assets exist)
2. **HUD UI visual hierarchy** (scripts exist, UI GameObjects partially missing)
3. **3 code changes**: random flour/water amounts, E-key dough completion step, per-dough baking times
4. **Complete Inspector wiring + lane waypoint baking + MainMenu polish**

The project root is `E:\Program Files\GWBGameJam`. All C# scripts use `namespace GWBGameJam`. There are 2 assembly definitions: `GWBGameJam.Runtime` (runtime code) and `GWBGameJam.Editor` (editor tools).

---

## Before You Start — Read These Files

First, read these files to understand what exists:

1. `E:\Program Files\GWBGameJam\CLAUDE.md` — project overview and coding rules
2. `E:\Program Files\GWBGameJam\TASK_LOG.txt` — full development history (last 100 lines is enough)
3. `E:\Program Files\GWBGameJam\Docs\013_CodingRules.md` — coding conventions (read before editing any .cs file)
4. `E:\Program Files\GWBGameJam\Docs\ManualInstructions.md` — existing manual operation guide

Key architecture constraints:
- No `FindObjectOfType`, no `GameObject.Find`, no Singleton pattern
- No `Invoke`/`InvokeRepeating` — use `Update` + `Time.deltaTime`
- EventBus is static generic: `EventBus<T>.Subscribe/Unsubscribe/Publish`
- All events are `readonly struct`
- All cross-component references wired via `[SerializeField]` in Inspector
- Namespace: `GWBGameJam` only (no sub-namespaces)

---

## Step 1: Create Placeholder Sprite Generator (CODE)

**Goal**: Generate colored circle sprites for monsters, lanes, bread, and explosion so they are visible in-game.

Create a new file `Assets/Editor/SpriteGenerator.cs`:

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

namespace GWBGameJam
{
    public class SpriteGenerator : EditorWindow
    {
        [MenuItem("GWBGameJam/Generate Placeholder Sprites")]
        private static void Generate()
        {
            string outputDir = "Assets/Sprites/Generated";
            Directory.CreateDirectory(outputDir);

            // Monster sprites: 64x64 colored circles
            GenerateCircle(outputDir, "Monster_A_Idle", 64, Color.red);
            GenerateCircle(outputDir, "Monster_A_Hit", 64, new Color(1f, 0.7f, 0.7f)); // white-ish red
            GenerateCircle(outputDir, "Monster_B_Idle", 64, Color.green);
            GenerateCircle(outputDir, "Monster_B_Hit", 64, new Color(0.7f, 1f, 0.7f));
            GenerateCircle(outputDir, "Monster_C_Idle", 64, Color.blue);
            GenerateCircle(outputDir, "Monster_C_Hit", 64, new Color(0.7f, 0.7f, 1f));

            // Lane sprites: 256x128 semi-transparent rectangles
            Color[] laneColors = {
                new Color(0.8f, 0.6f, 0.4f, 0.3f), // lane 0 - tan
                new Color(0.7f, 0.5f, 0.3f, 0.3f), // lane 1
                new Color(0.6f, 0.4f, 0.2f, 0.3f), // lane 2
                new Color(0.5f, 0.3f, 0.1f, 0.3f), // lane 3
                new Color(0.4f, 0.2f, 0.0f, 0.3f), // lane 4
            };
            for (int i = 0; i < 5; i++)
            {
                GenerateRect(outDir: outputDir, fileName: $"Lane_{i}_Normal", width: 256, height: 128, color: laneColors[i]);
                GenerateRect(outDir: outputDir, fileName: $"Lane_{i}_Hovered", width: 256, height: 128, color: new Color(laneColors[i].r + 0.2f, laneColors[i].g + 0.2f, laneColors[i].b + 0.2f, 0.5f));
            }

            // Bread projectile: 32x32 brown circle
            GenerateCircle(outputDir, "Bread", 32, new Color(0.6f, 0.3f, 0.1f));

            // Explosion: 64x64 orange burst
            GenerateCircle(outputDir, "Explosion", 64, new Color(1f, 0.6f, 0f));

            AssetDatabase.Refresh();
            Debug.Log($"[SpriteGenerator] All placeholder sprites generated in {outputDir}");
        }

        private static void GenerateCircle(string outDir, string fileName, int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size);
            Color clear = Color.clear;
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = dist <= radius ? color : clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(outDir, $"{fileName}.png"), bytes);
            DestroyImmediate(tex);
        }

        private static void GenerateRect(string outDir, string fileName, int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(outDir, $"{fileName}.png"), bytes);
            DestroyImmediate(tex);
        }
    }
}
```

**After creating this file:**
1. In Unity Editor, wait for script compilation to finish
2. Click menu **GWBGameJam → Generate Placeholder Sprites**
3. You should see 16 .png files created in `Assets/Sprites/Generated/`
4. Select all sprites in Project window, in Inspector set:
   - Texture Type = **Sprite (2D and UI)**
   - Click **Apply**
5. Now assign each sprite:
   - `MonsterData_A.asset` → IdleSprite = `Monster_A_Idle`, HitSprite = `Monster_A_Hit`
   - `MonsterData_B.asset` → IdleSprite = `Monster_B_Idle`, HitSprite = `Monster_B_Hit`
   - `MonsterData_C.asset` → IdleSprite = `Monster_C_Idle`, HitSprite = `Monster_C_Hit`
   - `Bread.prefab` → SpriteRenderer.Sprite = `Bread`
   - Open `Exposion.prefab` → Add Component `SpriteRenderer` → Sprite = `Explosion`

---

## Step 2: Lane Visual Sprite Assignment (MANUAL)

In Game.unity:
1. Select `_Systems/LaneSystem` (LaneManager component)
2. Expand `_laneVisuals` array (Size=5)
3. For element [0]:
   - NormalSprite = drag in `Lane_0_Normal` from Assets/Sprites/Generated/
   - HoveredSprite = drag in `Lane_0_Hovered`
4. Repeat for elements [1] through [4] with Lane_1 through Lane_4 sprites
5. Also: select each `_World/Lanes/Lane_X/Visual_X` node → in SpriteRenderer, assign corresponding `Lane_X_Normal` sprite

---

## Step 3: Code Changes — Random Flour/Water Amounts (CODE)

Open `Assets/Scripts/Systems/DoughSystem.cs`.

**Change 1** — In `ApplyFlour()` method (~line 75), replace the single line:

```csharp
// OLD:
_currentRatio = Mathf.Clamp(_currentRatio - _config.FlourClickAmount, 0f, _config.MaxRatio);

// NEW:
float flourAmount = Random.Range(0.5f, 1.0f);
_currentRatio = Mathf.Clamp(_currentRatio - flourAmount, 0f, _config.MaxRatio);
```

**Change 2** — In `ApplyWater()` method (~line 81), replace:

```csharp
// OLD:
_currentRatio = Mathf.Clamp(_currentRatio + _config.WaterFillRate * Time.deltaTime, 0f, _config.MaxRatio);

// NEW:
float waterRate = _config.WaterFillRate * Random.Range(1f, 3f);
_currentRatio = Mathf.Clamp(_currentRatio + waterRate * Time.deltaTime, 0f, _config.MaxRatio);
```

After these changes, the DoughConfig `FlourClickAmount` field is unused. Optionally remove it from `DoughConfig.cs` (keep the asset valid—removing a field from the SO class deletes the stored value, which is fine since we no longer read it).

---

## Step 4: Code Changes — E-Key Dough Completion (CODE)

This adds a "finish dough" step between kneading and baking. Press E to lock the dough, then Space to bake.

**File: `Assets/Scripts/Systems/BakingSystem.cs`**

Add field near other private fields:
```csharp
private bool _doughReady;
private DoughState _capturedDoughState;
```

Add public API:
```csharp
public bool IsDoughReady() => _doughReady;
```

In `Update()`, before the space-key check, add E-key detection:
```csharp
// Near top of Update(), after the guard checks
if (Input.GetKeyDown(KeyCode.E) && !_doughReady && _currentDoughState != DoughState.None)
{
    _doughReady = true;
    _capturedDoughState = _currentDoughState;
    // Optional: publish event for UI feedback
}
```

Modify the space-key press condition — only start baking when `_doughReady`:
```csharp
// OLD (approximate):
if (Input.GetKeyDown(KeyCode.Space) && _currentState == BakingState.Idle && ...)

// NEW:
if (Input.GetKeyDown(KeyCode.Space) && _doughReady && _currentState == BakingState.Idle && ...)
```

At the end of `ForceThrow()` or wherever baking state resets to Idle after throw, add:
```csharp
_doughReady = false;
```

**File: `Assets/Scripts/Systems/DoughSystem.cs`**

The DoughSystem also needs to stop accepting mouse input when dough is ready (E pressed).

First, add a `[SerializeField] private BakingSystem _bakingSystem;` field at the top (alongside existing config serialized fields). Then wire it in Inspector later.

```csharp
[SerializeField] private BakingSystem _bakingSystem;
```

Modify `IsInputActive` property (~line 21) to also check dough isn't locked:
```csharp
// OLD:
private bool IsInputActive =>
    !_hasConfigError && _isPlayingState && _isBakingIdle && _currentDoughState != DoughState.None;

// NEW:
private bool IsInputActive =>
    !_hasConfigError && _isPlayingState && _isBakingIdle && _currentDoughState != DoughState.None
    && !_bakingSystem.IsDoughReady();
```

Make sure the null-ref is handled — add a null check in `Start()` or `Awake()`:
```csharp
private void Awake()
{
    ValidateConfig();
    if (_bakingSystem == null)
        Debug.LogError("[DoughSystem] BakingSystem reference not assigned in Inspector");
}
```

---

## Step 5: Code Changes — Per-Dough Baking Times (CODE)

**File: `Assets/Scripts/Config/BakingConfig.cs`**

Add new fields to the class (alongside existing duration fields):
```csharp
[Header("Per-Dough Baking Durations (Cooked time)")]
public float SoftestCookedDuration = 1.5f;
public float MediumCookedDuration = 2.0f;
public float HardestCookedDuration = 3.0f;

[Header("Proportions")]
[Range(0.1f, 0.5f)]
public float UndercookedProportion = 0.33f;
```

Add a helper method:
```csharp
public float GetCookedDuration(DoughState state)
{
    return state switch
    {
        DoughState.Softest => SoftestCookedDuration,
        DoughState.Medium  => MediumCookedDuration,
        DoughState.Hardest => HardestCookedDuration,
        _ => MediumCookedDuration // fallback for TooSoft/TooHard
    };
}
```

Update `OnValidate()` logic to handle the new fields:
```csharp
private void OnValidate()
{
    if (MediumCookedDuration <= SoftestCookedDuration)
    {
        Debug.LogError("[BakingConfig] MediumCookedDuration must be > SoftestCookedDuration, auto-fixed");
        MediumCookedDuration = SoftestCookedDuration + 0.1f;
    }
    if (HardestCookedDuration <= MediumCookedDuration)
    {
        Debug.LogError("[BakingConfig] HardestCookedDuration must be > MediumCookedDuration, auto-fixed");
        HardestCookedDuration = MediumCookedDuration + 0.1f;
    }
    // Also keep existing CookedDuration > UndercookedDuration check
}
```

**File: `Assets/Scripts/Systems/BakingSystem.cs`**

Earlier in Step 4 we already added `_capturedDoughState`. Now use it for baking times.

In the method that starts baking (where timer resets and state goes to Undercooked), capture the dough state if not already captured:
```csharp
// When starting bake (Space pressed):
_capturedDoughState = _doughSystem.GetCurrentDoughState(); // or use the one from E-key
_bakingTimer = 0f;
float cookedDuration = _config.GetCookedDuration(_capturedDoughState);
float undercookedDuration = cookedDuration * _config.UndercookedProportion;
float burntForcedDuration = cookedDuration * 1.67f;
```

In the timer advancement logic (where state transitions are checked), replace the fixed config references with these computed durations. The exact implementation depends on how the existing code checks state transitions—look for lines comparing `_bakingTimer` against `_config.UndercookedDuration`, `_config.CookedDuration`, `_config.BurntForcedThrowDuration` and replace with the per-bake computed values.

Store the computed durations as fields set at bake start:
```csharp
private float _activeUndercookedDuration;
private float _activeCookedDuration;
private float _activeBurntDuration;
```

Set them when bake begins:
```csharp
_activeUndercookedDuration = undercookedDuration;
_activeCookedDuration = cookedDuration;
_activeBurntDuration = burntForcedDuration;
```

Then in the timer check:
```csharp
if (_bakingTimer >= _activeBurntDuration)
    ForceThrow();
else if (_bakingTimer >= _activeCookedDuration)
    // transition to Burnt
else if (_bakingTimer >= _activeUndercookedDuration)
    // transition to Cooked
```

---

## Step 6: Wire DoughSystem._bakingSystem in Inspector (MANUAL)

After Step 4-5 code changes compile:
1. Select `_Systems/DoughSystem` in Game.unity Hierarchy
2. In Inspector, find the new `_bakingSystem` field
3. Drag `_Systems/BakingSystem` from Hierarchy into the field
4. Save scene

---

## Step 7: HUD UI Visual Elements Setup (MANUAL — Detailed)

**Goal:** Make RatioBar, BakingIndicator, TableHPBar actually visible in-game (scripts exist but UI GameObjects lack source images).

### 7a: RatioBar

In HUD_Canvas, expand `RatioBar`:

1. **Add a background bar:**
   - Right-click `RatioBar` → UI → Image, name it `Background`
   - Set Anchor: stretch horizontal, fixed height (~30px) at center
   - Set RectTransform: Left=10, Right=10, Height=30, PosY=0
   - Color: dark gray (R=0.3, G=0.3, B=0.3, A=0.8)
   - Source Image: leave as `UISprite` (built-in) or use any generated sprite

2. **Confirm Indicator child:**
   - There should be an `Indicator` child with RectTransform + Image
   - If missing: right-click RatioBar → Create Empty, name `Indicator`
   - Add Image component, set color = white, width=4px, height=40px (tall marker)
   - Anchor: Middle Left (so its x-position drives the bar reading)
   - This will be driven by `RatioBar._indicatorRect` (already wired)

3. **Confirm 3 reference line children:**
   - `RefLine_Softest` — red, already exists
   - `RefLine_Medium` — green, already exists
   - `RefLine_Hardest` — dark green, already exists
   - Each needs: Image component with Source Image = `UISprite` (the built-in white square), stretch width to ~8px
   - Their x-positions will be set by RatioBar script at runtime based on config values

4. **Add ratio labels (optional but recommended):**
   - Right-click each RefLine → UI → Text - TextMeshPro
   - Set text to "3:2", "1:1", "1:2" respectively
   - Font size ~14, color white, anchor below the ref line marker

**Important:** The RatioBar component's serialized fields (`_indicatorRect`, `_softestRefLine`, `_mediumRefLine`, `_hardestRefLine`) are almost certainly already wired to existing child objects in the prefab. If the children don't exist, create them with the exact names the prefab expects.

### 7b: BakingIndicator

1. **Add background:**
   - Right-click `BakingIndicator` → UI → Image, name `Background`
   - Stretch full parent, add rounded border effect if desired
   - Color: dark gray

2. **Confirm Fill child:**
   - Should have a `Fill` child with Image component
   - Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left
   - Source Image = `UISprite` (white)
   - This is already wired as `BakingIndicator._fill`

3. **Add baking label:**
   - Right-click BakingIndicator → UI → Text - TextMeshPro, name `BakingLabel`
   - Set text = "BAKING..."
   - Font size 18, bold, color white
   - Anchor: centered
   - This is decorative only (not script-driven)

### 7c: TableHPBar

1. **Add background:**
   - Right-click `TableHPBar` → UI → Image, name `Background`
   - Stretch full parent, dark gray color

2. **Confirm Fill child:**
   - Should have a `Fill` child with Image component
   - Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left
   - Source Image = `UISprite` (white)
   - Already wired as `TableHPBar._fill`

### 7d: Table Visual (workbench)

In HUD_Canvas, find the `Table` child (already exists, positioned at bottom):
1. Select `Table` Image
2. Set Source Image = `UISprite` (or any placeholder)
3. Set Color = brown (R=0.5, G=0.3, B=0.15)
4. Confirm RectTransform: anchor bottom-center, full width, ~200px height

---

## Step 8: MainMenu Scene Setup (MANUAL)

1. Open `Assets/Scenes/MainMenu.unity`
2. If no Canvas exists: right-click Hierarchy → UI → Canvas
   - Set Canvas Scaler: UI Scale Mode = Scale With Screen Size
   - Reference Resolution = 1920x1080, Screen Match Mode = Match (0.5)
3. Right-click Canvas → UI → Image, name `Background`
   - Set full screen, Color = warm orange/brown (R=0.95, G=0.85, B=0.7)
4. Right-click Canvas → UI → Text - TextMeshPro, name `Title`
   - Text = "Knead & Bread" (or "揉面团" / game name of choice)
   - Font size 72, bold, white (or dark brown)
   - Anchor: Top-Center, Y=-100
5. Right-click Canvas → UI → Button - TextMeshPro, name `StartButton`
   - If existing: confirm SceneLoader.LoadGame() is wired
   - If not: Add Component `SceneLoader` (from Assets/Scripts/Core/SceneLoader.cs)
   - Button.OnClick → (+) → drag the object with SceneLoader → select `SceneLoader.LoadGame()`
   - Button text = "开始游戏" or "Start Game"
   - Anchor: Center, bigger size
6. Right-click Canvas → UI → Text - TextMeshPro, name `ControlsHint`
   - Text = "左键: 加面粉 | 右键: 加水 | E: 完成制作 | 空格: 烤制投掷"
   - Font size 24, color gray
   - Anchor: Bottom-Center, Y=30
7. Save scene
8. Open Build Settings (File → Build Settings) and confirm:
   - Scenes[0] = MainMenu
   - Scenes[1] = Game
   - If not, drag scenes from Project window into the build list

---

## Step 9: Lane Waypoint Baking (MANUAL)

1. Open Game.unity (should already be open)
2. Menu → **GWBGameJam → Quick Waypoint Generator**
3. Fill in the fields:
   - `laneWaypointConfig` = drag in `Assets/ScriptableObjects/Configs/LaneWaypointConfig.asset`
   - `monsterConfig` = drag in `Assets/ScriptableObjects/Configs/MonsterConfig.asset`
   - Top Y = 4.5
   - Bottom Y = -2.0
   - Top Width = 3.0
   - Bottom Width = 7.0
4. Click **Generate**
5. Verify: select `LaneWaypointConfig.asset` → inspect the `Lanes` array → each lane should have 8 positions with varying Y values
6. Run the game — in Console, check: no RecordedStepCount mismatch error from LaneManager

If QuickWaypointGenerator doesn't exist or errors:
- Use the alternative: **GWBGameJam → Lane Waypoint Calculator**
- Set: `monsterConfig`, `laneWaypointConfig`, `laneCalculatorData` references
- Click **Auto Distribute**, then **Bake**
- Verify Scene Preview shows 40 spheres along the lanes

---

## Step 10: Config Values Alignment (MANUAL)

Fix the broken DisplayScale values on MonsterData assets:

For each MonsterData asset (`MonsterData_A`, `MonsterData_B`, `MonsterData_C`):
1. Select the asset in Project window → Inspector
2. Set `DisplayScale` = **0.4** (for A and B) and **0.5** (for C — slightly bigger)
3. Verify `IdleSprite` and `HitSprite` are now pointing to Step 1's generated sprites (not UISprite)

For `BakingConfig.asset` — the new SO fields from Step 5 should have auto-default values:
- SoftestCookedDuration = 1.5
- MediumCookedDuration = 2.0
- HardestCookedDuration = 3.0
- UndercookedProportion = 0.33
If they show as 0, manually set them.

---

## Step 11: AspectRatioEnforcer + Camera Setup (MANUAL)

1. In Game.unity, select `Main Camera`
2. Add Component → `AspectRatioEnforcer` (search for it)
3. Verify: TargetWidth=1920, TargetHeight=1080 (the defaults in the script)
4. Verify camera: Orthographic, Size=540, Position=(960, 540, -10)

---

## Step 12: Panel Text Polish (MANUAL + CODE)

**File: `Assets/Scripts/UI/UISystem.cs`**

The LevelTransition text is already driven by code. Verify the text formatting matches Chinese display. The existing code in `HandleGameStateChanged` sets the text for level transition:
```csharp
// If the text isn't in Chinese, modify to:
_levelTransitionText.text = $"第 {levelNumber} 关通过！";
```

Similarly, Death and Victory canvas panels may have TMP children with hardcoded text. Check each:

1. `Death_Canvas` → find TMP text child → set text = "桌子损坏！\n按任意键返回主菜单"
2. `Victory_Canvas` → find TMP text child → set text = "恭喜通关！\n按任意键返回主菜单"
3. `PauseMenu_Canvas` → add or set TMP text child = "暂停"

---

## Step 13: End-to-End Verification (PLAYTEST)

After all code compiles and scene is set, run playtests:

### Test 1: Full Game Loop
1. Run from MainMenu → click Start → Game loads
2. HUD visible: RatioBar, BakingIndicator, TableHPBar all render
3. In ~9 seconds, first monster spawns at top of a lane, starts walking down
4. Don't touch anything → monster reaches table → TableHPBar drops
5. After 5 hits → Death screen → "Back to Main Menu" returns to MainMenu

### Test 2: Dough + E-Key + Bake + Hit
1. Start game
2. Right-click hold → RatioBar indicator moves left (adding water = softer)
3. Left-click → indicator jumps right (adding flour = harder)
4. Press E → indicator locks (mouse input disabled)
5. Hold Space → BakingIndicator fills (yellow → green → red)
6. Move mouse over lanes → hovered lane highlights
7. Release Space → bread projectile flies to hovered lane
8. If monster in lane matches dough state → explosion, monster dies
9. If mismatch → monster flashes but doesn't die
10. After throw → E-dough state resets, mouse input re-enabled for next kneading

### Test 3: Per-Dough Baking Times
1. Create Softest dough (ratio ~1.5) → E → Space → baking bar reaches full at ~1.5s
2. Create Medium dough (ratio ~1.0) → E → Space → baking bar reaches full at ~2.0s
3. Create Hardest dough (ratio ~0.5) → E → Space → baking bar reaches full at ~3.0s

### Test 4: Level Progression
1. Kill all 10 monsters in level 1 → LevelTransition shows "第 1 关通过！"
2. Click Continue → Level 2 starts (7s spawn interval, 15 monsters)
3. Clear → Level 3 (5s spawn interval, 20 monsters)
4. Clear → Victory screen

### Test 5: Pause
1. Press Escape during gameplay → PauseMenu shows
2. Click "Continue" → game resumes
3. Press Escape again → PauseMenu shows again

### Test 6: Dev Speed
1. Press Ctrl+Shift+P → game speeds up (TimeScale 3x)
2. Press again → normal speed

---

## Execution Order Summary

| Order | Step | What You Do | Est. Time |
|-------|------|------------|-----------|
| 1 | Step 1 | Write + run sprite generator | 15 min |
| 2 | Step 2 | Assign lane sprites in Inspector | 5 min |
| 3 | Step 3 | Edit DoughSystem.cs (random amounts) | 5 min |
| 4 | Step 4 | Edit BakingSystem.cs + DoughSystem.cs (E-key) | 20 min |
| 5 | Step 5 | Edit BakingConfig.cs + BakingSystem.cs (per-dough times) | 15 min |
| 6 | Step 6 | Wire DoughSystem._bakingSystem in Inspector | 2 min |
| 7 | Step 7 | HUD UI setup in Scene | 30 min |
| 8 | Step 8 | MainMenu scene setup | 15 min |
| 9 | Step 9 | Lane waypoint baking | 10 min |
| 10 | Step 10 | Config values (DisplayScale fix) | 5 min |
| 11 | Step 11 | Camera AspectRatioEnforcer | 2 min |
| 12 | Step 12 | Panel text polish | 5 min |
| 13 | Step 13 | Full playtest | 30 min |

**Total: ~2.5 hours**

---

## File Reference Summary

| File Path | When to Touch |
|-----------|---------------|
| `Assets/Editor/SpriteGenerator.cs` | Create new (Step 1) |
| `Assets/Scripts/Systems/DoughSystem.cs` | Edit (Steps 3, 4) |
| `Assets/Scripts/Systems/BakingSystem.cs` | Edit (Steps 4, 5) |
| `Assets/Scripts/Config/BakingConfig.cs` | Edit (Step 5) |
| `Assets/Scripts/UI/UISystem.cs` | Optional edit (Step 12) |
| `Assets/Scenes/Game.unity` | MANUAL ops (Steps 2, 6, 7, 9, 11) |
| `Assets/Scenes/MainMenu.unity` | MANUAL ops (Step 8) |
| `Assets/ScriptableObjects/MonsterData/MonsterData_A.asset` | MANUAL edit (Step 10) |
| `Assets/ScriptableObjects/MonsterData/MonsterData_B.asset` | MANUAL edit (Step 10) |
| `Assets/ScriptableObjects/MonsterData/MonsterData_C.asset` | MANUAL edit (Step 10) |
| `Assets/ScriptableObjects/Configs/BakingConfig.asset` | Verify new fields set (Step 10) |
| `Assets/ScriptableObjects/Configs/LaneWaypointConfig.asset` | Verify after bake (Step 9) |
| `Assets/Prefabs/Projectile/Exposion.prefab` | Add SpriteRenderer (Step 1) |
| `Assets/Prefabs/Projectile/Bread.prefab` | Assign sprite (Step 1) |

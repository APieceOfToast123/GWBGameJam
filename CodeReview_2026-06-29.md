> ignore 不上传
>
> 原始指令：检查项目逻辑，代码逻辑
> 产出物：review-prompt（用于交给 AI 审查本项目代码）

---

# 代码审查 Prompt — GWBGameJam

请全面审查本项目（Unity 2D「揉面团」GWBGameJam 参赛项目）的所有 C# 代码逻辑。

## 审查范围

- `Assets/Scripts/` 下全部 33 个 .cs 文件（Core/、Systems/、UI/、Config/、Events/）
- `Assets/Editor/` 下 4 个 Editor 工具脚本
- `Assets/Tests/EditMode/` 下 1 个测试文件
- `Assets/ScriptableObjects/` 下所有 .asset 配置资产
- `Docs/` 下相关 Spec 文档（001-014）

## 审查重点

### 1. Bug 排查
- 是否存在空引用、越界、逻辑错误
- 数据流是否完整（事件发布后是否被正确处理）
- 是否有竞态条件（如同帧多个事件互相覆盖）

### 2. EventBus 配对检查
- 每个 Subscribe 是否有配对的 Unsubscribe
- 是否都在 OnEnable/OnDestroy 中配对

### 3. 禁止 API 检查
全局搜索是否存在以下任何 API 的使用：
- `FindObjectOfType` / `FindObjectsOfType`
- `GameObject.Find`
- Singleton 模式（静态 self 引用）
- `SendMessage` / `BroadcastMessage`
- `DontDestroyOnLoad`
- `PlayerPrefs`（用于存档的）
- `Invoke` / `InvokeRepeating` / 协程（`StartCoroutine`）

### 4. 计时器实现
所有计时逻辑应使用 `Update + Time.deltaTime`（不用协程），确保 `TimeScale = 0` 时自动暂停。

### 5. Config/SO 验证模式
- 每个 ScriptableObject 是否都有 `Validate()` 方法？
- Awake 中是否调用了 `ValidateConfig()`？
- 是否遵循 Debug.LogError + 自动修正、不 throw 的模式？

### 6. 与 Spec 的一致性
- 代码实现是否与 Docs/ 下的 spec 文档匹配
- 特别是 010_ConfigSchema.md 定义的字段类型、默认值、范围
- 特别关注 gameplay revision（分档烤制时长、随机加料、单道双怪）之后的变更

### 7. 数据所有权
- 每个数据集合（怪物列表、球道状态等）是否有且仅有一个系统负责增删？
- 其他系统是否只通过公开 API 只读访问？

### 8. 命名空间和程序集
- 所有代码是否使用 `namespace GWBGameJam`？
- Assets/Scripts/ 和 Assets/Editor/ 是否分别有 .asmdef 文件隔离？

## 输出要求

按以下分级输出问题：

| 标记 | 含义 |
|------|------|
| 🔴 P0 | 必须修复 — 运行时必定或极高概率出错 |
| 🟡 P1 | 建议修复 — 特定条件下出错或逻辑明显不符合预期 |
| 🟠 P2 | 值得修复 — 代码质量、健壮性、规范性问题 |
| 🔵 P3 | 轻微 — 代码冗余、注释问题、小优化 |

每个问题标注：编号 → 文件路径和行号 → 描述 → 代码片段 → 影响分析 → 修复建议。

最后给出总体评价：规范遵守情况、Spec 匹配度、架构一致性。

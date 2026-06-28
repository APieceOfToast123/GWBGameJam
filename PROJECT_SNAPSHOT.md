# PROJECT SNAPSHOT

> 生成时间：2026-06-27
> 通过 `Summarize Context` 命令自动生成并维护。

---

## 进度总览

| 维度 | 进度 | 备注 |
|------|------|------|
| Spec 文档 | 14 / 14 ✅ | 全部 Approved |
| Architecture | ✅ Frozen v1.1 | 唯一 Source of Truth |
| 代码任务 | 6 / 22 完成（8/22 含手动）| T01~T08 全部完成 |
| 手动 Unity 操作 | T05、T07 完成，T19~T21 待做 | |
| 整体里程碑 | Phase 2 结束，Phase 3 启动 | 下一步 T09 |

---

## 已完成任务

| Task | 描述 | 完成方式 |
|------|------|---------|
| T01 | Assembly Definitions + 目录结构 | 编码 |
| T02 | GameEnums + EventBus | 编码 |
| T03 | 12 个 SO 类定义 | 编码 |
| T04 | 8 个 Event Struct 文件（16 个事件）| 编码 |
| T05 | SO Assets 创建与填值 | 手动（用户完成）|
| T06 | GameLoop.cs（状态机）| 编码 |
| T07 | Game.unity 场景层级搭建 | 手动（用户完成）|
| T08 | LaneSystem（LaneManager + LaneHoverDetector）| 编码 |

---

## 当前任务

**T09 — DoughSystem**

实现水粉比计算、输入处理、DoughState 状态判断。
参考 Spec：`Docs/004_DoughSystem.md`

---

## 剩余工作（按顺序）

| Task | 描述 | 类型 |
|------|------|------|
| T09 | DoughSystem | 编码 |
| T10 | TableSystem | 编码 |
| T11 | MonsterSystem（MonsterController + MonsterSystem）| 编码 |
| T12 | BakingSystem | 编码 |
| T13 | ThrowSystem（抛物线 + 命中判定）| 编码 |
| T14 | LevelSystem（出怪节奏 + 关卡进度）| 编码 |
| T15 | UISystem（Canvas 切换）| 编码 |
| T16 | RatioBar（比例条 UI）| 编码 |
| T17 | BakingIndicator + TableHPBar | 编码 |
| T18 | LaneCalculator（Editor DevTool）| 编码 |
| T19 | Prefab 制作 | 手动 Unity |
| T20 | Inspector 连线 | 手动 Unity |
| T21 | 球道点位烘焙（需 T18 先完成）| 手动 Unity |
| T22 | 端到端集成测试 | 测试 |

---

## 关键架构决策

| 决策 | 结论 |
|------|------|
| 事件系统 | 静态泛型 EventBus<T>，双 List 防并发，异常隔离 |
| 场景架构 | 双场景（MainMenu.unity + Game.unity），SceneManager.LoadScene 切换 |
| 文件夹结构 | 按类型分（Core / Config / Events / Systems / UI），非按系统分 |
| 命名空间 | 唯一 `GWBGameJam`，含 Editor 工具 |
| 程序集 | GWBGameJam.Runtime（Scripts/）+ GWBGameJam.Editor（Editor/）|
| 计时器 | 全部 Update + Time.deltaTime，TimeScale=0 自动暂停 |
| MainMenu_Canvas | Game.unity 中不存在，返回主菜单 = SceneManager.LoadScene |
| EventBus 生命周期 | OnEnable → OnDestroy（或 OnDisable，视对象销毁逻辑）|
| Config/ 目录 | SO 类 + 配套 [Serializable] 数据类（LevelData、LaneWaypoints）|
| Source of Truth | Architecture v1.1 Frozen；修订只走 Architecture→CodingRules→TaskBreakdown→Code |

---

## 已知风险

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| T21 依赖 T18 | 球道点位无法烘焙 → LaneSystem 运行时一直报 Error | T18（DevTool）必须在 T21 前完成 |
| T11 MonsterSystem 复杂度 | 两个类 + 复杂状态机（移动/缩放/闪白）| 拆分 MonsterController（单实例）+ MonsterSystem（管理器）|
| T20 Inspector 连线 | 手动操作易遗漏引用 | ReviewChecklist 中 Awake null 检查会在运行时即时报错 |
| 无美术资源 | T19 Prefab 无真实 Sprite | 先用占位图完成功能，美术后期替换 |

---

## 项目健康度

| 指标 | 状态 |
|------|------|
| Architecture 一致性 | ★★★★★（Frozen，Remaining Drift = 0）|
| 设计文档完整性 | ★★★★★（14 份，全部 Approved）|
| 代码质量 | ★★★★★（25 个文件，ReviewChecklist 全过）|
| Unity 场景进度 | ★★★☆☆（层级完成，待 Prefab 和连线）|
| 整体健康 | **健康** — 进入核心系统编码阶段 |

# 可试玩 Demo 重构开发记录

## 2026-07-02

### 目标确认

用户明确指出：当前项目虽然有自动战斗、装备、强化、离线、转生等系统，但试玩感受仍然不像 demo，只是功能骨架。参考目标是暗黑装备放置截图，需要把开发方向切到“可试玩 Demo 重构”。

### 已决定

- 暂停零散功能堆叠。
- 进入 Demo 重构线。
- 每轮开发必须记录做什么、完成什么、验证结果。

### 本轮计划

- 建立 `docs/demo-rebuild/PLAN.md`。
- 建立 `docs/demo-rebuild/LOG.md`。
- 开始 Demo-1：主界面骨架重构。

### 本轮完成

- 已建立 Demo 重构计划和开发记录。
- 主界面开始从工程面板切到游戏 HUD：
  - 顶部改为暗黑 HUD：标题、在线状态、账号、当前层数、魂点、战力、连接入口。
  - 卡关提示从旧 header 移入战斗区。
  - 底部新增系统日志栏和“材料 / 工坊 / 转生天赋”入口按钮。
  - 沿用当前暗黑配色、按钮边框、稀有度行样式。

### 验证

- `go test ./...` 通过。
- `./scripts/verify-flow.sh` 通过。
- `git diff --check` 通过。

### 暂未完成

- Unity batchmode 编译和主场景 smoke 暂未执行：当前 Unity 编辑器已经打开该项目，Unity 不允许第二个实例同时以 batchmode 打开同一项目。

### 用户更新概念图

- 用户提供新的竖屏移动端 UI 概念图。
- 已保存为 `docs/demo-rebuild/references/mobile-dark-idle-ui-concept.png`。
- 规划调整：Demo-1 从横版三栏主界面改为竖屏移动端主界面。
- 新结构优先级：
  - 顶部资源 HUD。
  - 大战斗区。
  - 当前目标 / Boss 进度 / 最近掉落。
  - 穿戴装备 / 背包 / 强化 / 材料摘要功能卡。
  - 底部装备详情操作面板。
  - 底部导航。

### 本轮追加完成

- UI 参考分辨率改为竖屏 `945 x 1672`。
- 主界面结构改为移动端纵向布局：
  - 顶部资源 HUD：层数、战力、魂点、账号、连接入口。
  - 大战斗区：Boss 标题、Boss 进度条、战斗舞台、战斗状态。
  - 状态卡：当前目标、Boss 进度、最近掉落。
  - 功能卡：穿戴装备、背包、强化、材料摘要。
  - 底部装备详情和操作区。
  - 底部系统日志与“材料 / 工坊 / 转生天赋”入口。

### 本轮追加验证

- `git diff --check` 通过。
- `go test ./...` 通过。
- `./scripts/verify-flow.sh` 通过。

### 仍需补验

- Unity 编译和主场景 smoke 仍需在 Unity 编辑器关闭后执行。

### 本轮继续目标

- 固化“每轮开发必须记录”的执行规则。
- 补齐 Demo-1 底部导航：从临时“材料 / 工坊 / 转生天赋”入口改为概念图方向的“战斗 / 背包 / 锻造 / 天赋”。

### 本轮继续完成

- `PLAN.md` 增加执行规则：每轮必须记录目标、完成内容、验证结果、遗留问题、提交信息。
- `PLAN.md` 增加 Demo-1 checklist。
- 底部区域调整为两层：
  - 上层保留系统日志。
  - 下层改为四个移动端导航按钮：战斗、背包、锻造、天赋。

### 本轮继续验证

- `git diff --check` 通过。
- `go test ./...` 通过。
- `./scripts/verify-flow.sh` 通过。

### 本轮继续遗留

- Unity 编译和主场景 smoke 已在用户关闭 Unity 后补跑通过。

### 补充验证

- Unity `EquipmentPresenterTestRunner.Run` 通过。
- Unity `PlayModeRunner.RunMainSmoke` 通过，结果 `MAIN_SMOKE_OK`。
- Unity 日志异常关键词扫描无命中。
- `go test -race ./...` 通过。

### 准备提交

- 本次提交范围：Demo-1 竖屏主界面骨架第一版、Demo 重构计划和日志、UI 参考图资产。

### 提交信息

- Commit subject: `Start mobile demo UI rebuild`

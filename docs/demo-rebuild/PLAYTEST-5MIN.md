# 5 分钟新号试玩脚本

## 目标

验证新玩家能在 5 分钟内看懂并体验核心闭环：

自动战斗 -> 掉装 -> 对比 -> 穿戴/强化 -> 推层 -> Boss -> 奖励 -> 转生目标。

## 准备

1. 启动服务端：

```bash
./scripts/dev-server.sh
```

2. 用 Unity 打开 `client/`，进入 `Assets/Scenes/Main.unity`。
3. 使用一个新账号，例如 `demo_YYYYMMDD_HHMM`。

## 试玩步骤

### 0:00 - 0:30 连接和第一屏

- 点击 Connect。
- 确认第一屏是游戏主界面，而不是调试面板。
- 顶部能看到层数、战力、魂点、账号。
- 战斗区能看到英雄、Boss/怪物、血条、当前区域。

通过标准：

- 连接后无需额外操作，自动战斗开始。
- 战斗区有命中节奏、伤害数字、血条变化。

### 0:30 - 1:30 自动战斗和掉落

- 等待至少一次掉落。
- 查看“最近掉落”卡。
- 如果掉落是提升装备，确认舞台横幅和 toast 会提示。

通过标准：

- 掉落品质可通过颜色和文字识别。
- 可提升装备会自动选中，玩家能看到为什么值得穿。

### 1:30 - 2:30 背包、对比、穿戴

- 切到“背包”。
- 选择一件装备。
- 查看“当前选择 / 已装备 / 对比结果”三列。
- 点击穿戴。

通过标准：

- 对比结果有评分、强化、品质、词缀差异。
- 正向和负向变化容易区分。
- 穿戴后战力变化可见。

### 2:30 - 3:30 锻造

- 切到“锻造”。
- 查看强化、重铸、合成、弱装清理建议。
- 在背包里锁定一件装备，再回到锻造页。
- 如果材料足够，尝试强化或合成。
- 如果背包有弱装，点击一键分解弱装。

通过标准：

- 玩家能看懂当前材料是否足够。
- 操作结果有 toast 和评分变化反馈。
- 锁定装备不会被分解，重连后仍保持锁定状态。

### 3:30 - 4:30 Boss 和推层

- 回到“战斗”。
- 观察 Boss 进度节点。
- 推进到 Boss 关时确认中央标识和红色警示。
- 击败 Boss 后确认首通奖励横幅。

通过标准：

- 玩家知道距离 Boss 还有几层。
- Boss 通关和奖励不是静默发生。

### 4:30 - 5:00 转生目标

- 切到“天赋”。
- 查看转生条件、预计魂点、重置代价、推荐天赋。
- 若已达到条件，确认转生按钮可用。

通过标准：

- 玩家知道为什么要继续推到第 10 层。
- 玩家知道转生会重置什么、保留什么、奖励什么。

## 自动化补充验证

```bash
cd server
go test ./...
```

```bash
./scripts/verify-flow.sh
```

```bash
Unity -batchmode -projectPath client -executeMethod EquipmentPresenterTestRunner.Run -logFile equipment-presenter-test.log
Unity -batchmode -projectPath client -executeMethod PlayModeRunner.RunMainSmoke -logFile unity-main-smoke.log
Unity -batchmode -projectPath client -executeMethod PlayModeRunner.RunMainVisualCapture -logFile unity-visual-capture.log
Unity -batchmode -projectPath client -executeMethod PlayModeRunner.RunMainPopulatedVisualCapture -logFile unity-populated-capture.log
```

期望结果：

- `go test ./...` 通过。
- `verify-flow.sh` 输出 `VERIFY_OK`。
- `verify-flow.sh` 会先输出 `Balance Simulation`，确认 3 轮到第 10 层的 tick、掉落、穿戴、稀有度和转生收益指标，再启动协议闭环验证。
- 协议闭环验证会覆盖：自动战斗掉落新装、旧装强化、继承强化并自动穿戴新装、分解旧装、转生后重建穿戴。
- Presenter 测试日志包含 `[EquipmentPresenterTestRunner] OK`。
- 主场景 smoke 日志包含 `MAIN_SMOKE_OK`。
- 视觉捕获日志包含 `VISUAL_CAPTURE_OK`。
- `artifacts/ui-qa/` 生成 `battle.png`、`bag.png`、`craft.png`、`talent.png`，分辨率均为 `945 x 1672`。
- `artifacts/ui-qa/populated/` 生成同名填充状态截图，覆盖装备、词缀、材料、魂点和高等级天赋。

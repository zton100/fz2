# 装备驱动放置游戏

西方奇幻地下城题材的装备驱动放置游戏。客户端 Unity 6 (C#)，服务端 Go + WebSocket，半权威架构。

## 目录结构

- `server/` — Go 服务端
- `client/` — Unity 6 客户端
- `docs/` — 设计文档与协议

## 实现进度

| 阶段 | 内容 | 状态 |
|---|---|---|
| Stage 0 | 脚手架与协议、WebSocket 登录与全量同步 | ✅ |
| Stage 1 | 掉落核心：词缀池、装备生成器、战力计算 | ✅ |
| Stage 2 | 战斗与掉落循环：地下城 Runner、稀有度权重 | ✅ |
| Stage 3 | 背包与穿戴：8 槽位装备系统、核心闭环 | ✅ |
| Stage 4 | 分解/合成/重铸/强化：完整养成管线 | ✅ |
| Stage 5 | 离线结算：8h 封顶、tick 模拟、离线收益推送 | ✅ |
| Stage 6 | 转生系统：魂点、4 天赋、MaxFloor、转生闭环 | ✅ |
| P0 修复 | 天赋接入战斗/掉落、统一战力、魂点公式 | ✅ |
| P1 修复 | JSON 持久化存档、协议解析优化、多开踢旧 | ✅ |
| P2 打磨 | 离线弹窗、toast 通知、一键穿戴、卡点/Boss 提示 | ✅ |
| 阶段 A | Boss 奖励体验、掉落稀有度反馈、装备养成评分反馈 | ✅ |
| 可试玩 Demo | 竖屏暗黑 UI、装备对比、战斗反馈、工坊/转生面板、5 分钟试玩脚本 | ✅ |

当前状态：**可试玩暗黑装备放置 Demo**。完整消息协议见 `docs/protocol.md`，5 分钟试玩验收见 `docs/demo-rebuild/PLAYTEST-5MIN.md`。

## 本地开发联调

### 启动服务端

```bash
cd server
go run ./cmd/server/
```
服务端默认监听 `ws://127.0.0.1:8080/ws`，存档写入 `server/saves/{account}.json`。
也可以在仓库根目录运行：
```bash
./scripts/dev-server.sh
```

### 启动客户端

用 Unity Hub 打开 `client/` 项目，在编辑器中打开 `Assets/Scenes/Main.unity`，点 Play 运行。
在 Account 输入框输入账号（留空默认 hero），点 Connect，即连接并同步。
离线超过 1 次登录间隔后重新登录会弹出离线结算结果。

### 测试

服务端：
```bash
cd server
go test ./...
```

服务端端到端玩法验证（需先启动服务端）：
```bash
cd server
go run ./cmd/verifyclient
```
会自动登录新账号，验证起步装备、自动战斗掉落、推进到 10 层、转生后再次发放起步装备。
或在仓库根目录运行一键验证：
```bash
./scripts/verify-flow.sh
```

客户端自动化验证（需 Unity Editor）：
```bash
Unity -batchmode -projectPath client -executeMethod PlayModeRunner.Run
```
结果写入 `verify_result.txt`。

### 5 分钟试玩验收

见 `docs/demo-rebuild/PLAYTEST-5MIN.md`。覆盖自动战斗、掉装、对比、穿戴、锻造、Boss 奖励和转生目标。

## 技术栈说明

- **服务端**：Go 1.26 + gorilla/websocket，半权威（管存档/离线结算/掉落/强化/转生）
- **客户端**：Unity 6 LTS (C#)，零第三方依赖
  - WebSocket：.NET 内置 `System.Net.WebSockets.ClientWebSocket`
  - JSON：Unity 内置 `JsonUtility` + 轻量手写解析
  - UI：IMGUI (OnGUI)，无需 uGUI 包
- **通讯**：WebSocket 长连接，JSON 信封 `{ "t", "id", "data" }`
- **持久化**：JSON 文件存档（原子写入），关键操作后自动保存 + 5 分钟定期保存

> 注：原计划用 NativeWebSocket + Newtonsoft.Json，因当前环境无法访问 packages.unity.com 与 github.com，改为零第三方依赖方案。功能等价。

## 核心系统

- **战斗**：每 2 秒自动战斗，玩家战力 > 怪物战力即胜，推进下一层
- **装备**：8 槽位，5 稀有度，23 种词缀 × 5 Tier，强化 +0~+10
- **养成**：分解获材料 → 合成新装备 → 重铸词缀 → 强化属性
- **离线**：断线后自动结算，8h 封顶，模拟战斗+掉落+推层
- **转生**：第 10 层可转生，按 Floor/5 获魂点，4 天赋永久加成
  - damage：+5% 伤害/级（影响在线+离线战斗）
  - quality：+1 稀有度下限/级（影响在线+离线掉落）
  - drop：+3% 高稀有度权重/级（影响在线+离线掉落）
  - offline_gain：+10% 离线模拟 tick 数/级

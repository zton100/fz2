# 装备驱动放置游戏

西方奇幻地下城题材的装备驱动放置游戏。客户端 Unity 6 (C#)，服务端 Go + WebSocket，半权威架构。

## 目录结构

- `server/` — Go 服务端
- `client/` — Unity 6 客户端
- `docs/` — 设计文档与协议

## 本地开发联调

### 启动服务端

```bash
cd server
go run ./cmd/server/
```
服务端监听 `ws://localhost:8080/ws`。

### 启动客户端

用 Unity Hub 打开 `client/` 项目，在编辑器中打开 `Assets/Scenes/Main.unity`，点 Play 运行。
在 Account 输入框输入账号（留空默认 hero），点 Connect，即连接并同步。

### 测试

服务端：
```bash
cd server
go test ./...
```

## 技术栈说明

- **服务端**：Go 1.26 + gorilla/websocket，半权威（管存档/离线结算/掉落/强化/转生）
- **客户端**：Unity 6 LTS (C#)，零第三方依赖
  - WebSocket：.NET 内置 `System.Net.WebSockets.ClientWebSocket`
  - JSON：Unity 内置 `JsonUtility` + 轻量手写解析
  - UI：IMGUI (OnGUI)，无需 uGUI 包
- **通讯**：WebSocket 长连接，JSON 信封 `{ "t", "id", "data" }`

> 注：原计划用 NativeWebSocket + Newtonsoft.Json，因当前环境无法访问 packages.unity.com 与 github.com，改为零第三方依赖方案。功能等价。

## 当前阶段

阶段 0（脚手架与协议）已完成：客户端可连接服务端，登录并收到全量同步。

后续阶段见 `docs/superpowers/specs/2026-06-24-equipment-idle-game-design.md`。

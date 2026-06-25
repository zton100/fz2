# 阶段 0：脚手架与协议 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 搭建 Go 服务端 + Unity 客户端骨架，定义 WebSocket JSON 消息协议，打通"客户端连接 → 登录 → 全量同步"链路。

**Architecture:** 服务端用 Go + gorilla/websocket 起一个本地 WebSocket 服务器，维护内存中的玩家状态，处理登录与全量同步。客户端用 Unity 6 (C#) 建立 WebSocket 连接，发送登录请求，接收并显示全量同步消息。消息统一为 `{ "t": "<type>", "id": "<reqId>", "data": {...} }` 格式。

**Tech Stack:** Go 1.26、gorilla/websocket、Unity 6 LTS（C#）。

> **实现备注（2026-06-25）**：原计划客户端用 NativeWebSocket + Newtonsoft.Json 两个第三方包。实际执行时当前环境无法访问 packages.unity.com 与 github.com（SSL/TLS 握手失败），遂改为**零第三方依赖方案**：
> - WebSocket → .NET 内置 `System.Net.WebSockets.ClientWebSocket`（后台线程接收 + 主线程队列分发）
> - JSON → Unity 内置 `JsonUtility` + 轻量手写信封解析（见 `Message.cs`）
> - UI → IMGUI (`OnGUI`)，无需 uGUI 包
>
> 下文 Task 7~11 的代码块仍保留原 NativeWebSocket/Newtonsoft 版本作为设计参考；**实际落库的代码以仓库 `client/Assets/Scripts/` 为准**（已按零依赖方案实现并端到端验证通过）。

**前置条件：** Go 1.26 已安装；Unity Hub + Unity 6 LTS 编辑器已安装。

---

## 文件结构

本阶段产出文件及其职责：

**服务端（Go）**
- `server/go.mod` — Go 模块定义
- `server/cmd/server/main.go` — 服务端入口，启动 HTTP/WebSocket 服务器
- `server/internal/protocol/message.go` — 消息协议类型定义（请求/响应/推送/同步的 JSON 结构）
- `server/internal/model/player.go` — 玩家领域模型（阶段 0 最小版：账号、层数、背包占位）
- `server/internal/ws/hub.go` — 连接管理与消息路由
- `server/internal/ws/handler.go` — 消息处理（登录、全量同步）
- `server/internal/ws/handler_test.go` — 登录与同步逻辑单测
- `server/internal/save/memory.go` — 内存存档（阶段 0 用，后续替换为持久化）

**客户端（Unity / C#）**
- `client/` — Unity 项目（由 Hub 创建，2D Core 模板）
- `client/Assets/Scripts/Net/Message.cs` — 消息编解码工具
- `client/Assets/Scripts/Net/WSClient.cs` — WebSocket 客户端，收发消息
- `client/Assets/Scripts/State/GameState.cs` — 本地缓存的服务端状态（单例）
- `client/Assets/Scripts/UI/MainController.cs` — 主场景控制器
- `client/Assets/Scenes/Main.unity` — 主场景，显示连接状态与同步消息

**共享协议文档**
- `docs/protocol.md` — 消息协议人类可读文档

---

## Task 1: 初始化 Go 模块与服务端目录

**Files:**
- Create: `server/go.mod`
- Create: `server/cmd/server/main.go`（占位，下个任务充实）

- [ ] **Step 1: 创建 Go 模块**

Run:
```bash
mkdir -p server/cmd/server server/internal/protocol server/internal/model server/internal/ws server/internal/save
cd server
go mod init equipment-idle-server
```
Expected: 生成 `server/go.mod`，内容含 `module equipment-idle-server` 与 `go 1.26`。

- [ ] **Step 2: 写占位 main.go 确保可编译**

Create `server/cmd/server/main.go`:
```go
package main

import "log"

func main() {
	log.Println("equipment-idle-server starting")
}
```

- [ ] **Step 3: 验证编译**

Run:
```bash
cd server
go build ./...
```
Expected: 无输出，退出码 0。

- [ ] **Step 4: 提交**

```bash
git init
git add server
git commit -m "chore: init go server module scaffold"
```
（注：项目目录非 git 仓库，先 `git init` 初始化整个项目仓库。）

---

## Task 2: 定义消息协议（服务端）

**Files:**
- Create: `server/internal/protocol/message.go`

协议统一信封：`{ "t": "<type>", "id": "<reqId>", "data": {...} }`。请求带 `id` 供响应匹配；服务端推送/同步 `id` 可空。

- [ ] **Step 1: 写协议类型**

Create `server/internal/protocol/message.go`:
```go
package protocol

// Envelope 是所有消息的统一信封。
type Envelope struct {
	T    string          `json:"t"`          // 消息类型
	ID   string          `json:"id,omitempty"`   // 请求 ID，用于 req/resp 匹配；推送/同步可空
	Data json.RawMessage `json:"data,omitempty"` // 消息体，按 T 解析
}

// 消息类型常量
const (
	TypeLogin = "login" // 请求：登录
	TypeSync  = "sync"  // 推送：全量同步
)

// LoginRequest 登录请求体。
type LoginRequest struct {
	Account string `json:"account"` // 临时账号名
}

// SyncData 全量同步消息体。阶段 0 最小字段，后续阶段扩充。
type SyncData struct {
	Account string `json:"account"`
	Floor   int    `json:"floor"`    // 当前层数
	Souls   int    `json:"souls"`    // 魂点
	Inventory []string `json:"inventory"` // 背包物品 ID 占位
}
```

注意：`Envelope` 用 `json.RawMessage` 承载 `data`，便于按 `t` 二次解析。需补 `import "encoding/json"`。

- [ ] **Step 2: 修正 import（补 encoding/json）**

确认 `server/internal/protocol/message.go` 顶部 import 块为：
```go
package protocol

import "encoding/json"
```

- [ ] **Step 3: 验证编译**

Run:
```bash
cd server
go build ./...
```
Expected: 无输出，退出码 0。

- [ ] **Step 4: 提交**

```bash
git add server/internal/protocol/message.go
git commit -m "feat: define websocket message protocol envelope"
```

---

## Task 3: 玩家领域模型与内存存档（服务端）

**Files:**
- Create: `server/internal/model/player.go`
- Create: `server/internal/save/memory.go`
- Create: `server/internal/save/memory_test.go`

- [ ] **Step 1: 写玩家模型**

Create `server/internal/model/player.go`:
```go
package model

// Player 玩家领域模型。阶段 0 最小版，后续阶段扩充装备/穿戴/材料等。
type Player struct {
	Account   string   // 账号名
	Floor     int      // 当前层数
	Souls     int      // 魂点
	Inventory []string // 背包物品 ID 占位
}

// NewPlayer 创建新玩家，默认第 1 层、0 魂、空背包。
func NewPlayer(account string) *Player {
	return &Player{
		Account:   account,
		Floor:     1,
		Souls:     0,
		Inventory: []string{},
	}
}
```

- [ ] **Step 2: 写内存存档（失败测试先行）**

Create `server/internal/save/memory_test.go`:
```go
package save

import (
	"testing"
)

func TestMemoryStore_LoadOrCreate_NewPlayer(t *testing.T) {
	store := NewMemoryStore()
	p := store.LoadOrCreate("hero")
	if p.Account != "hero" {
		t.Fatalf("account = %q, want hero", p.Account)
	}
	if p.Floor != 1 {
		t.Fatalf("floor = %d, want 1", p.Floor)
	}
	if len(p.Inventory) != 0 {
		t.Fatalf("inventory len = %d, want 0", len(p.Inventory))
	}
}

func TestMemoryStore_LoadOrCreate_ExistingPlayer(t *testing.T) {
	store := NewMemoryStore()
	p1 := store.LoadOrCreate("hero")
	p1.Floor = 5
	p2 := store.LoadOrCreate("hero")
	if p2.Floor != 5 {
		t.Fatalf("floor = %d, want 5 (should reuse existing)", p2.Floor)
	}
	if p1 != p2 {
		t.Fatal("should return same pointer for same account")
	}
}
```

- [ ] **Step 3: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/save/...
```
Expected: 编译失败，`NewMemoryStore` / `LoadOrCreate` 未定义。

- [ ] **Step 4: 写内存存档实现**

Create `server/internal/save/memory.go`:
```go
package save

import "equipment-idle-server/internal/model"

// MemoryStore 内存存档。阶段 0 用，后续替换为持久化。
type MemoryStore struct {
	players map[string]*model.Player
}

// NewMemoryStore 创建内存存档。
func NewMemoryStore() *MemoryStore {
	return &MemoryStore{players: make(map[string]*model.Player)}
}

// LoadOrCreate 按账号读取玩家；不存在则新建。
func (s *MemoryStore) LoadOrCreate(account string) *model.Player {
	if p, ok := s.players[account]; ok {
		return p
	}
	p := model.NewPlayer(account)
	s.players[account] = p
	return p
}
```

- [ ] **Step 5: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/save/...
```
Expected: `ok equipment-idle-server/internal/save`。

- [ ] **Step 6: 提交**

```bash
git add server/internal/model/player.go server/internal/save/
git commit -m "feat: add player model and in-memory save store"
```

---

## Task 4: WebSocket 连接管理与消息路由（服务端）

**Files:**
- Create: `server/internal/ws/hub.go`
- Create: `server/internal/ws/handler.go`
- Create: `server/internal/ws/handler_test.go`

- [ ] **Step 1: 添加 gorilla/websocket 依赖**

Run:
```bash
cd server
go get github.com/gorilla/websocket
```
Expected: `go.mod` 增加依赖行。

- [ ] **Step 2: 写 Hub（连接与会话管理）**

Create `server/internal/ws/hub.go`:
```go
package ws

import (
	"log"
	"net/http"
	"sync"

	"github.com/gorilla/websocket"
	"equipment-idle-server/internal/save"
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool { return true }, // 本地开发，放开跨域
}

// Session 一个已连接的客户端会话。
type Session struct {
	Conn    *websocket.Conn
	Account string // 登录后填充
	Send    chan []byte
}

// Hub 管理所有连接与会话。
type Hub struct {
	mu       sync.Mutex
	sessions map[*Session]struct{}
	store    *save.MemoryStore
}

// NewHub 创建 Hub。
func NewHub(store *save.MemoryStore) *Hub {
	return &Hub{
		sessions: make(map[*Session]struct{}),
		store:    store,
	}
}

// ServeWS 处理 WebSocket 升级请求。
func (h *Hub) ServeWS(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Printf("upgrade error: %v", err)
		return
	}
	sess := &Session{Conn: conn, Send: make(chan []byte, 64)}
	h.register(sess)
	go h.writePump(sess)
	h.readPump(sess)
}

func (h *Hub) register(sess *Session) {
	h.mu.Lock()
	h.sessions[sess] = struct{}{}
	h.mu.Unlock()
}

func (h *Hub) unregister(sess *Session) {
	h.mu.Lock()
	delete(h.sessions, sess)
	h.mu.Unlock()
	close(sess.Send)
}

// readPump 读取客户端消息并路由到 handler。
func (h *Hub) readPump(sess *Session) {
	defer func() {
		h.unregister(sess)
		sess.Conn.Close()
	}()
	for {
		_, msg, err := sess.Conn.ReadMessage()
		if err != nil {
			break
		}
		h.handleMessage(sess, msg)
	}
}

// writePump 把 Send 通道的消息写给客户端。
func (h *Hub) writePump(sess *Session) {
	for msg := range sess.Send {
		if err := sess.Conn.WriteMessage(websocket.TextMessage, msg); err != nil {
			break
		}
	}
}
```

- [ ] **Step 3: 写 Handler（登录与同步，失败测试先行）**

Create `server/internal/ws/handler_test.go`:
```go
package ws

import (
	"encoding/json"
	"testing"

	"equipment-idle-server/internal/protocol"
	"equipment-idle-server/internal/save"
)

func TestHandleLogin_NewAccount_SendsSync(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Send: make(chan []byte, 8)}

	reqData, _ := json.Marshal(protocol.LoginRequest{Account: "hero"})
	env := protocol.Envelope{T: protocol.TypeLogin, ID: "r1", Data: reqData}
	raw, _ := json.Marshal(env)

	hub.handleMessage(sess, raw)

	select {
	case resp := <-sess.Send:
		var got protocol.Envelope
		if err := json.Unmarshal(resp, &got); err != nil {
			t.Fatalf("unmarshal: %v", err)
		}
		if got.T != protocol.TypeSync {
			t.Fatalf("type = %q, want sync", got.T)
		}
		var sd protocol.SyncData
		json.Unmarshal(got.Data, &sd)
		if sd.Account != "hero" || sd.Floor != 1 {
			t.Fatalf("sync data = %+v, want account=hero floor=1", sd)
		}
	default:
		t.Fatal("no sync message sent")
	}
}

func TestHandleLogin_ExistingAccount_RetainsFloor(t *testing.T) {
	store := save.NewMemoryStore()
	p := store.LoadOrCreate("hero")
	p.Floor = 7

	hub := NewHub(store)
	sess := &Session{Send: make(chan []byte, 8)}

	reqData, _ := json.Marshal(protocol.LoginRequest{Account: "hero"})
	env := protocol.Envelope{T: protocol.TypeLogin, ID: "r2", Data: reqData}
	raw, _ := json.Marshal(env)

	hub.handleMessage(sess, raw)

	resp := <-sess.Send
	var got protocol.Envelope
	json.Unmarshal(resp, &got)
	var sd protocol.SyncData
	json.Unmarshal(got.Data, &sd)
	if sd.Floor != 7 {
		t.Fatalf("floor = %d, want 7 (existing player retained)", sd.Floor)
	}
	if sess.Account != "hero" {
		t.Fatalf("session account = %q, want hero", sess.Account)
	}
}
```

- [ ] **Step 4: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/ws/...
```
Expected: 编译失败，`handleMessage` 未定义。

- [ ] **Step 5: 写 Handler 实现**

Create `server/internal/ws/handler.go`:
```go
package ws

import (
	"encoding/json"
	"log"

	"equipment-idle-server/internal/protocol"
	"equipment-idle-server/internal/save"
)

// handleMessage 解析信封并路由到对应处理函数。
func (h *Hub) handleMessage(sess *Session, raw []byte) {
	var env protocol.Envelope
	if err := json.Unmarshal(raw, &env); err != nil {
		log.Printf("malformed message: %v", err)
		return
	}
	switch env.T {
	case protocol.TypeLogin:
		h.handleLogin(sess, env)
	default:
		log.Printf("unknown message type: %s", env.T)
	}
}

// handleLogin 处理登录：加载/创建玩家，回发全量同步。
func (h *Hub) handleLogin(sess *Session, env protocol.Envelope) {
	var req protocol.LoginRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		log.Printf("login parse error: %v", err)
		return
	}
	player := h.store.LoadOrCreate(req.Account)
	sess.Account = req.Account

	sync := protocol.SyncData{
		Account:   player.Account,
		Floor:     player.Floor,
		Souls:     player.Souls,
		Inventory: player.Inventory,
	}
	syncData, _ := json.Marshal(sync)
	resp := protocol.Envelope{T: protocol.TypeSync, ID: env.ID, Data: syncData}
	out, _ := json.Marshal(resp)
	sess.Send <- out
}
```

- [ ] **Step 6: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/ws/...
```
Expected: `ok equipment-idle-server/internal/ws`。

- [ ] **Step 7: 提交**

```bash
git add server/internal/ws/ server/go.mod server/go.sum
git commit -m "feat: websocket hub with login and full sync"
```

---

## Task 5: 服务端入口启动 HTTP 服务器

**Files:**
- Modify: `server/cmd/server/main.go`

- [ ] **Step 1: 改写 main.go 启动服务器**

Replace `server/cmd/server/main.go` 内容为：
```go
package main

import (
	"log"
	"net/http"

	"equipment-idle-server/internal/save"
	"equipment-idle-server/internal/ws"
)

func main() {
	store := save.NewMemoryStore()
	hub := ws.NewHub(store)

	http.HandleFunc("/ws", hub.ServeWS)
	addr := ":8080"
	log.Printf("equipment-idle-server listening on ws://localhost%s/ws", addr)
	if err := http.ListenAndServe(addr, nil); err != nil {
		log.Fatalf("server error: %v", err)
	}
}
```

- [ ] **Step 2: 验证编译**

Run:
```bash
cd server
go build ./...
```
Expected: 无输出，退出码 0。

- [ ] **Step 3: 启动服务端冒烟测试**

Run（后台启动）:
```bash
cd server
go run ./cmd/server/
```
Expected: 日志输出 `equipment-idle-server listening on ws://localhost:8080/ws`，进程不退出。
（验证后用 Ctrl+C 停止。）

- [ ] **Step 4: 提交**

```bash
git add server/cmd/server/main.go
git commit -m "feat: start http server with websocket endpoint"
```

---

## Task 6: 编写协议人类可读文档

**Files:**
- Create: `docs/protocol.md`

- [ ] **Step 1: 写协议文档**

Create `docs/protocol.md`:
```markdown
# WebSocket 消息协议

## 信封格式

所有消息统一信封：

```json
{ "t": "<type>", "id": "<reqId>", "data": {...} }
```

- `t`：消息类型
- `id`：请求 ID，用于 req/resp 匹配；服务端推送/同步可空
- `data`：消息体，按 `t` 解析

## 消息类型

### login（客户端 → 服务端，请求）

登录。服务端加载或创建玩家，回发 `sync`。

```json
{ "t": "login", "id": "r1", "data": { "account": "hero" } }
```

### sync（服务端 → 客户端，推送/响应）

全量同步玩家状态。

```json
{
  "t": "sync",
  "id": "r1",
  "data": {
    "account": "hero",
    "floor": 1,
    "souls": 0,
    "inventory": []
  }
}
```

## 后续阶段待补充类型

- 实时掉落推送（阶段 2）
- 穿戴/卸下请求与响应（阶段 3）
- 分解/合成/重铸/强化请求与响应（阶段 4）
- 离线结算结果（阶段 5）
- 转生请求与响应（阶段 6）
```

- [ ] **Step 2: 提交**

```bash
git add docs/protocol.md
git commit -m "docs: websocket message protocol"
```

---

## Task 7: 初始化 Unity 客户端项目

**Files:**
- Create: `client/`（Unity 项目，由 Unity Hub 创建）
- Modify: `client/Packages/manifest.json`（由 Package Manager 自动写入）
- Create: `client/.gitignore`

**前置：** Unity Hub + Unity 6 LTS 编辑器已安装。

- [ ] **Step 1: 用 Unity Hub 创建项目**

- 打开 Unity Hub → New project → 选 **2D Core** 模板
- Project name: `EquipmentIdle`
- Location: `I:\放置2`（项目将创建为 `I:\放置2\EquipmentIdle`）
- 创建并打开，等待首次编译完成
- 若项目名非 `client`：在 Unity Hub 里关闭项目，把 `I:\放置2\EquipmentIdle` 重命名为 `I:\放置2\client`，再在 Hub 里 Add project from disk 重新打开

- [ ] **Step 2: 安装两个包（Package Manager）**

在 Unity 编辑器：Window → Package Manager

1. **Newtonsoft.Json**：左上角下拉选 `Unity Registry` → 搜索 `Newtonsoft Json` → Install（包名 `com.unity.nuget.newtonsoft-json`）
2. **NativeWebSocket**：左上角 `+` → `Add package from git URL...` → 粘贴：
   ```
   https://github.com/endel/NativeWebSocket.git#upm
   ```
   → Add，等待拉取编译，Console 无报错

（这两个包会自动写入 `client/Packages/manifest.json`。）

- [ ] **Step 3: 写 .gitignore**

Create `client/.gitignore`:
```
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
*.csproj
*.sln
*.unityproj
.vs/
```

- [ ] **Step 4: 创建脚本目录**

在 Unity 编辑器 Project 窗口，`Assets` 下新建文件夹：
- `Assets/Scripts/Net`
- `Assets/Scripts/State`
- `Assets/Scripts/UI`

（`Assets/Scenes` 默认已存在，若无需新建。）

- [ ] **Step 5: 验证**

Unity 编辑器 Console 无报错；点 Play 能进入空场景（暂黑屏正常）。

- [ ] **Step 6: 提交**

```bash
git add client/
git commit -m "chore: init unity client project with nativewebsocket"
```

---

## Task 8: 客户端消息编解码工具

**Files:**
- Create: `client/Assets/Scripts/Net/Message.cs`

- [ ] **Step 1: 写消息编解码**

Create `client/Assets/Scripts/Net/Message.cs`:
```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EquipmentIdle.Net
{
    /// <summary>
    /// WebSocket 消息信封编解码。
    /// 信封格式：{ "t": "<type>", "id": "<reqId>", "data": {...} }
    /// </summary>
    public static class Message
    {
        public const string TypeLogin = "login";
        public const string TypeSync = "sync";

        /// <summary>编码为信封 JSON 字符串。</summary>
        public static string Encode(string type, string id, object data)
        {
            var env = new { t = type, id = id, data = data };
            return JsonConvert.SerializeObject(env);
        }

        /// <summary>编码登录请求。</summary>
        public static string EncodeLogin(string id, string account)
        {
            return Encode(TypeLogin, id, new { account });
        }

        /// <summary>解析收到的信封文本。失败返回 null。</summary>
        public static ParsedMessage Parse(string text)
        {
            try
            {
                var obj = JObject.Parse(text);
                return new ParsedMessage
                {
                    t = obj.Value<string>("t") ?? "",
                    id = obj.Value<string>("id") ?? "",
                    data = obj["data"] as JObject ?? new JObject(),
                };
            }
            catch
            {
                return null;
            }
        }
    }

    public class ParsedMessage
    {
        public string t;
        public string id;
        public JObject data;
    }
}
```

- [ ] **Step 2: 验证编译**

在 Unity 编辑器切回主窗口，等待自动编译（Console 无红色错误）。

- [ ] **Step 3: 提交**

```bash
git add client/Assets/Scripts/Net/Message.cs
git commit -m "feat: client message encode/decode util"
```

---

## Task 9: 客户端 WebSocket 连接

**Files:**
- Create: `client/Assets/Scripts/Net/WSClient.cs`

- [ ] **Step 1: 写 WebSocket 客户端 MonoBehaviour**

Create `client/Assets/Scripts/Net/WSClient.cs`:
```csharp
using System;
using NativeWebSocket;
using UnityEngine;

namespace EquipmentIdle.Net
{
    /// <summary>
    /// WebSocket 客户端，负责连接、收发消息。
    /// 收到的消息通过 OnMessage 抛出，连接/断开通过 OnConnected/OnClosed。
    /// </summary>
    public class WSClient : MonoBehaviour
    {
        public event Action OnConnected;
        public event Action OnClosed;
        public event Action<ParsedMessage> OnMessage;

        private WebSocket _ws;
        private string _url = "ws://localhost:8080/ws";
        private bool _wasOpen;

        public bool IsConnected => _wasOpen;

        public async void ConnectTo(string url = null)
        {
            if (url != null) _url = url;
            _ws = new WebSocket(_url);

            _ws.OnOpen += () =>
            {
                _wasOpen = true;
                OnConnected?.Invoke();
            };
            _ws.OnClose += (code) =>
            {
                _wasOpen = false;
                OnClosed?.Invoke();
            };
            _ws.OnMessage += (bytes) =>
            {
                string text = System.Text.Encoding.UTF8.GetString(bytes);
                var msg = Message.Parse(text);
                if (msg != null) OnMessage?.Invoke(msg);
            };
            _ws.OnError += (err) => Debug.LogError($"[WS] error: {err}");

            await _ws.Connect();
        }

        public async void SendText(string text)
        {
            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                await _ws.SendText(text);
            }
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _ws?.DispatchMessageQueue();
#endif
        }

        private async void OnApplicationQuit()
        {
            if (_ws != null) await _ws.Close();
        }
    }
}
```

- [ ] **Step 2: 验证编译**

切回 Unity 主窗口等待自动编译，Console 无红色错误。

- [ ] **Step 3: 提交**

```bash
git add client/Assets/Scripts/Net/WSClient.cs
git commit -m "feat: client websocket wrapper using nativewebsocket"
```

---

## Task 10: 客户端本地状态缓存（单例）

**Files:**
- Create: `client/Assets/Scripts/State/GameState.cs`

- [ ] **Step 1: 写 GameState 单例**

Create `client/Assets/Scripts/State/GameState.cs`:
```csharp
using System.Collections.Generic;
using EquipmentIdle.Net;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EquipmentIdle.State
{
    /// <summary>
    /// 全局状态单例。缓存服务端同步的状态，供 UI 读取。
    /// 挂在场景里一个 GameObject 上（DontDestroyOnLoad），或用 [RuntimeInitializeOnLoadMethod] 创建。
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        public event System.Action<JObject> OnSyncReceived;

        public string Account { get; private set; } = "";
        public int Floor { get; private set; } = 1;
        public int Souls { get; private set; } = 0;
        public List<string> Inventory { get; private set; } = new List<string>();

        private WSClient _ws;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _ws = gameObject.AddComponent<WSClient>();
            _ws.OnConnected += HandleConnected;
            _ws.OnMessage += HandleMessage;
        }

        /// <summary>连接服务端并用指定账号登录。</summary>
        public void ConnectAndLogin(string account)
        {
            Account = account;
            _ws.ConnectTo();
        }

        private void HandleConnected()
        {
            // 连上后立即发登录
            _ws.SendText(Message.EncodeLogin("r1", Account));
        }

        private void HandleMessage(ParsedMessage msg)
        {
            if (msg.t == Message.TypeSync)
            {
                var d = msg.data;
                Account = d.Value<string>("account") ?? Account;
                Floor = d.Value<int?>("floor") ?? Floor;
                Souls = d.Value<int?>("souls") ?? Souls;
                Inventory = d["inventory"]?.ToObject<List<string>>() ?? Inventory;
                OnSyncReceived?.Invoke(d);
            }
        }

        public bool IsConnected => _ws != null && _ws.IsConnected;
    }
}
```

- [ ] **Step 2: 验证编译**

切回 Unity 主窗口等待自动编译，Console 无红色错误。

- [ ] **Step 3: 提交**

```bash
git add client/Assets/Scripts/State/GameState.cs
git commit -m "feat: client gamestate singleton with sync handling"
```

---

## Task 11: 客户端主场景与端到端验证

**Files:**
- Create: `client/Assets/Scripts/UI/MainController.cs`
- Modify: `client/Assets/Scenes/SampleScene.unity`（2D Core 模板自带，重命名为 `Main.unity`）

- [ ] **Step 1: 写主场景控制器**

Create `client/Assets/Scripts/UI/MainController.cs`:
```csharp
using EquipmentIdle.State;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EquipmentIdle.UI
{
    /// <summary>
    /// 主场景控制器：连接按钮、账号输入、状态与同步显示。
    /// </summary>
    public class MainController : MonoBehaviour
    {
        [SerializeField] private Text statusLabel;
        [SerializeField] private InputField accountEdit;
        [SerializeField] private Button connectBtn;
        [SerializeField] private Text syncLabel;

        private void Start()
        {
            // 确保 GameState 单例存在
            if (GameState.Instance == null)
            {
                var go = new GameObject("GameState");
                go.AddComponent<GameState>();
            }
            GameState.Instance.OnSyncReceived += OnSync;

            connectBtn.onClick.AddListener(OnConnect);
            statusLabel.text = "disconnected";
        }

        private void OnConnect()
        {
            string acc = accountEdit.text.Trim();
            if (string.IsNullOrEmpty(acc)) acc = "hero";
            statusLabel.text = "connecting...";
            GameState.Instance.ConnectAndLogin(acc);
        }

        private void OnSync(JObject data)
        {
            statusLabel.text = "connected";
            syncLabel.text = $"account={data.Value<string>("account")} " +
                             $"floor={data.Value<int>("floor")} " +
                             $"souls={data.Value<int>("souls")}";
        }
    }
}
```

- [ ] **Step 2: 在编辑器里搭建 Main 场景 UI**

1. Project 窗口把 `Assets/Scenes/SampleScene` 重命名为 `Main`，双击打开
2. Hierarchy 里删掉默认 Main Camera 之外的多余对象（保留 Main Camera）
3. 新建 Canvas：Hierarchy → 右键 → UI → Canvas（自动带 EventSystem）
4. Canvas 下右键 → UI → Panel（可选，便于看背景）
5. Canvas 下创建空 GameObject 命名 `VBox`，加 `Vertical Layout Group` 组件
6. `VBox` 下依次创建：
   - UI → Text（旧版 Text），命名 `StatusLabel`，文字 "disconnected"
   - UI → Input Field，命名 `AccountEdit`，Placeholder 文字 "account (default hero)"
   - UI → Button，命名 `ConnectBtn`，子 Text 改 "Connect"
   - UI → Text，命名 `SyncLabel`，文字 "(no sync yet)"
7. 选中 Canvas（或 VBox 的父对象），Add Component → `MainController`
8. 在 Inspector 把上述 4 个 UI 对象拖到 MainController 的对应字段（StatusLabel / AccountEdit / ConnectBtn / SyncLabel）
9. File → Build Settings → 把 `Main` 场景 Add Open Scenes（设为场景 0）

- [ ] **Step 3: 端到端验证（手动）**

1. 启动服务端：
```bash
cd server
go run ./cmd/server/
```
2. 在 Unity 编辑器点 ▶ Play
3. 在 Input Field 输入账号（留空默认 hero），点 Connect
Expected: StatusLabel 变为 "connected"，SyncLabel 显示 `account=hero floor=1 souls=0`。

- [ ] **Step 4: 提交**

```bash
git add client/Assets/Scripts/UI/MainController.cs client/Assets/Scenes/
git commit -m "feat: client main scene with connect and sync display"
```

---

## Task 12: 阶段 0 收尾与 README

**Files:**
- Create: `README.md`

- [ ] **Step 1: 写 README**

Create `README.md`:
```markdown
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

用 Unity Hub 打开 `client/` 项目，在编辑器中点 Play 运行 Main 场景。
点 Connect，输入账号（默认 hero），即连接并同步。

## 当前阶段

阶段 0（脚手架与协议）已完成：客户端可连接服务端，登录并收到全量同步。

后续阶段见 `docs/superpowers/specs/2026-06-24-equipment-idle-game-design.md`。
```

- [ ] **Step 2: 跑全量测试确认无回归**

Run:
```bash
cd server
go test ./...
```
Expected: 全部 `ok`。

- [ ] **Step 3: 提交**

```bash
git add README.md
git commit -m "docs: project readme and stage 0 wrap-up"
```

---

## 验收标准

阶段 0 完成后须满足：
1. `cd server && go test ./...` 全部通过
2. `cd server && go run ./cmd/server/` 启动后监听 8080
3. Unity 编辑器 Play 运行 Main 场景，连接后显示 `connected` 与同步数据
4. 关闭客户端再连接同一账号，层数等状态保留（内存存档）

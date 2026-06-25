# 阶段 2：战斗与掉落循环 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让游戏"动起来"：服务端按层数生成怪物、自动战斗结算、击杀后掉落装备、把掉落 push 给客户端，玩家推进更深层。

**Architecture:** 怪物表定义怪物强度（按层数公式生成）。`dungeon` 包管理玩家当前层与推进逻辑。`combat` 包扩展战斗结算（玩家战力 vs 怪物强度）。`loot` 包扩展掉落表（按层数和稀有度权重）。玩家模型扩展装备背包与已穿戴装备。WebSocket 在登录后启动每会话战斗循环，定时结算并 push 掉落与层数推进。

**Tech Stack:** Go 1.26、标准库 `time`、gorilla/websocket。

**前置条件：** 阶段 0（ws/protocol/save）、阶段 1（data/loot/combat/model.Equipment）已完成。

---

## 文件结构

本阶段产出/修改文件：

**静态数据**
- `server/internal/data/monster.go` — 怪物定义与按层数生成怪物强度
- `server/internal/data/monster_test.go` — 怪物强度公式单测

**战斗扩展**
- `server/internal/combat/battle.go` — 玩家 vs 怪物的单次战斗结算（胜/负、用时）

**掉落扩展**
- `server/internal/loot/drop.go` — 掉落表（按层数权重选稀有度 + 生成装备）
- `server/internal/loot/drop_test.go` — 掉落权重单测

**地下城推进**
- `server/internal/dungeon/runner.go` — 单玩家地下城推进器（当前层、击杀计数、推进条件）

**玩家模型扩展**
- `server/internal/model/player.go` — 扩展 Equipment 背包与已穿戴槽位

**协议扩展**
- `server/internal/protocol/message.go` — 新增 `loot`（掉落 push）、`floor`（层数推进 push）类型

**会话战斗循环**
- `server/internal/ws/handler.go` — 登录后启动会话战斗 goroutine，定时结算并 push
- `server/internal/ws/hub.go` — Hub 持有 loot.Generator 与战斗循环管理

---

## Task 1: 怪物强度公式

**Files:**
- Create: `server/internal/data/monster.go`
- Create: `server/internal/data/monster_test.go`

怪物强度按层数线性递增，每 5 层为 Boss（强度跳升）。

- [ ] **Step 1: 写怪物强度公式（失败测试先行）**

Create `server/internal/data/monster_test.go`:
```go
package data

import (
	"math"
	"testing"
)

func TestMonsterPower_LinearGrowth(t *testing.T) {
	p1 := MonsterPower(1)
	p2 := MonsterPower(2)
	if p2 <= p1 {
		t.Fatalf("floor2 power %.2f should > floor1 power %.2f", p2, p1)
	}
}

func TestMonsterPower_BossSpikeEvery5(t *testing.T) {
	normal := MonsterPower(4)
	boss := MonsterPower(5)
	// Boss 层强度应显著高于前一层普通层
	if boss < normal*1.5 {
		t.Fatalf("boss power %.2f should >= 1.5x normal %.2f", boss, normal)
	}
}

func TestMonsterPower_Positive(t *testing.T) {
	for f := 1; f <= 100; f++ {
		p := MonsterPower(f)
		if p <= 0 || math.IsNaN(p) {
			t.Fatalf("floor %d power %.2f invalid", f, p)
		}
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/data/...
```
Expected: 编译失败，`MonsterPower` 未定义。

- [ ] **Step 3: 写怪物强度公式**

Create `server/internal/data/monster.go`:
```go
package data

// Monster 怪物定义。
type Monster struct {
	Name  string  // 怪物名
	Power float64 // 怪物战力（强度）
	IsBoss bool   // 是否 Boss
}

// MonsterPower 按层数计算怪物强度。
// 普通层：线性增长，base + (floor-1)*step
// Boss 层（每 5 层）：强度跳升 1.8 倍
func MonsterPower(floor int) float64 {
	const base = 10.0
	const step = 8.0
	normal := base + float64(floor-1)*step
	if floor%5 == 0 {
		return normal * 1.8 // Boss 跳升
	}
	return normal
}

// MonsterAt 生成某层的怪物。
func MonsterAt(floor int) Monster {
	isBoss := floor%5 == 0
	name := "史莱姆"
	if isBoss {
		name = "守层Boss"
	}
	return Monster{
		Name:   name,
		Power:  MonsterPower(floor),
		IsBoss: isBoss,
	}
}
```

- [ ] **Step 4: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/data/...
```
Expected: `ok equipment-idle-server/internal/data`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/data/monster.go server/internal/data/monster_test.go
git commit -m "feat: monster power formula with boss spike every 5 floors"
```

---

## Task 2: 战斗结算

**Files:**
- Create: `server/internal/combat/battle.go`
- Create: `server/internal/combat/battle_test.go`

玩家战力 vs 怪物强度，判定胜负。胜则可掉落，负则停留当前层。

- [ ] **Step 1: 写战斗结算测试**

Create `server/internal/combat/battle_test.go`:
```go
package combat

import "testing"

func TestBattle_PlayerWins_WhenPowerExceeds(t *testing.T) {
	r := Battle(500, 100)
	if !r.Win {
		t.Fatal("player power 500 vs monster 100 should win")
	}
}

func TestBattle_PlayerLoses_WhenPowerLower(t *testing.T) {
	r := Battle(50, 100)
	if r.Win {
		t.Fatal("player power 50 vs monster 100 should lose")
	}
}

func TestBattle_TieCountsAsLose(t *testing.T) {
	// 严格大于才算胜，相等算负（鼓励玩家提升）
	r := Battle(100, 100)
	if r.Win {
		t.Fatal("equal power should count as lose")
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/combat/...
```
Expected: 编译失败，`Battle` 未定义。

- [ ] **Step 3: 写战斗结算**

Create `server/internal/combat/battle.go`:
```go
package combat

// BattleResult 单次战斗结算结果。
type BattleResult struct {
	Win          bool    // 是否胜利
	PlayerPower  float64 // 玩家战力
	MonsterPower float64 // 怪物战力
}

// Battle 战斗结算：玩家战力严格大于怪物战力则胜。
func Battle(playerPower, monsterPower float64) BattleResult {
	return BattleResult{
		Win:          playerPower > monsterPower,
		PlayerPower:  playerPower,
		MonsterPower: monsterPower,
	}
}
```

- [ ] **Step 4: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/combat/...
```
Expected: `ok equipment-idle-server/internal/combat`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/combat/battle.go server/internal/combat/battle_test.go
git commit -m "feat: combat battle resolution player vs monster"
```

---

## Task 3: 掉落表

**Files:**
- Create: `server/internal/loot/drop.go`
- Create: `server/internal/loot/drop_test.go`

按层数决定掉落稀有度权重，再用阶段 1 的 Generator 生成装备。

- [ ] **Step 1: 写掉落表测试**

Create `server/internal/loot/drop_test.go`:
```go
package loot

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
)

func TestRollRarity_LowFloorMostlyCommon(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(1)))
	drop := NewDropTable(g)
	// 第 1 层，100 次掉落，普通应占多数
	var common int
	for i := 0; i < 100; i++ {
		r := drop.RollRarity(1)
		if r == data.RarityCommon {
			common++
		}
	}
	if common < 50 {
		t.Fatalf("floor1 common drops = %d/100, want >= 50", common)
	}
}

func TestRollRarity_HighFloorCanDropLegendary(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(1)))
	drop := NewDropTable(g)
	// 第 50 层，200 次掉落，至少出现一次传奇或更高
	var highRarity int
	for i := 0; i < 200; i++ {
		r := drop.RollRarity(50)
		if r >= data.RarityLegendary {
			highRarity++
		}
	}
	if highRarity == 0 {
		t.Fatal("floor50 should drop legendary+ at least once in 200 rolls")
	}
}

func TestDrop_GeneratesEquipment(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(1)))
	drop := NewDropTable(g)
	eq := drop.Drop(data.SlotWeapon, 10)
	if eq == nil {
		t.Fatal("drop returned nil equipment")
	}
	if eq.Slot != data.SlotWeapon {
		t.Fatalf("slot = %d, want weapon", eq.Slot)
	}
	if eq.UID == "" {
		t.Fatal("dropped equipment has empty UID")
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/loot/...
```
Expected: 编译失败，`NewDropTable`/`RollRarity`/`Drop` 未定义。

- [ ] **Step 3: 写掉落表实现**

Create `server/internal/loot/drop.go`:
```go
package loot

import (
	"math/rand"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// DropTable 掉落表：按层数决定稀有度权重并生成装备。
type DropTable struct {
	gen *Generator
	rng *rand.Rand
}

// NewDropTable 创建掉落表，复用 Generator 的 RNG。
func NewDropTable(gen *Generator) *DropTable {
	return &DropTable{gen: gen, rng: gen.rng}
}

// rarityWeights 按层数返回各稀有度的权重（顺序对应 AllRarities）。
// 层数越高，高稀有度权重越大。
func rarityWeights(floor int) []int {
	// 基础权重随层数向高稀有度倾斜
	bonus := floor / 10 // 每 10 层 +1 高稀有度倾向
	return []int{
		max(100-bonus*10, 10), // 普通
		30,                    // 魔法
		15 + bonus*2,          // 稀有
		3 + bonus,             // 传奇
		1 + bonus/2,           // 神器
	}
}

// RollRarity 按层数权重随机选一个稀有度。
func (d *DropTable) RollRarity(floor int) data.Rarity {
	weights := rarityWeights(floor)
	rarities := data.AllRarities()
	total := 0
	for _, w := range weights {
		total += w
	}
	roll := d.rng.Intn(total)
	cum := 0
	for i, w := range weights {
		cum += w
		if roll < cum {
			return rarities[i]
		}
	}
	return rarities[len(rarities)-1]
}

// Drop 生成一件掉落装备：按层数选稀有度，按指定槽位生成。
func (d *DropTable) Drop(slot data.Slot, floor int) *model.Equipment {
	rarity := d.RollRarity(floor)
	return d.gen.Generate(slot, rarity)
}

// DropRandomSlot 随机选一个槽位掉落（用于普通击杀，掉落槽位随机）。
func (d *DropTable) DropRandomSlot(floor int) *model.Equipment {
	slots := data.AllSlots()
	slot := slots[d.rng.Intn(len(slots))]
	return d.Drop(slot, floor)
}

func max(a, b int) int {
	if a > b {
		return a
	}
	return b
}
```

- [ ] **Step 4: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/loot/...
```
Expected: `ok equipment-idle-server/internal/loot`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/loot/drop.go server/internal/loot/drop_test.go
git commit -m "feat: drop table with floor-based rarity weights"
```

---

## Task 4: 玩家模型扩展（装备背包与穿戴）

**Files:**
- Modify: `server/internal/model/player.go`

阶段 0 的 Player 只有 `Inventory []string` 占位。现在扩展为真实装备背包与已穿戴槽位。

- [ ] **Step 1: 改写 player.go**

Replace `server/internal/model/player.go` 内容为：
```go
package model

import "equipment-idle-server/internal/data"

// Player 玩家领域模型。
type Player struct {
	Account   string                 // 账号名
	Floor     int                    // 当前层数
	Souls     int                    // 魂点
	Inventory []string               // 背包物品 ID 占位（保留兼容阶段0同步）
	EquipBag  []*Equipment           // 装备背包（掉落/未穿戴的装备）
	Equipped  map[data.Slot]*Equipment // 已穿戴装备，按槽位
}

// NewPlayer 创建新玩家，默认第 1 层、0 魂、空背包。
func NewPlayer(account string) *Player {
	return &Player{
		Account:   account,
		Floor:     1,
		Souls:     0,
		Inventory: []string{},
		EquipBag:  []*Equipment{},
		Equipped:  map[data.Slot]*Equipment{},
	}
}

// AddEquipment 把掉落的装备加入背包。
func (p *Player) AddEquipment(eq *Equipment) {
	if eq == nil {
		return
	}
	p.EquipBag = append(p.EquipBag, eq)
}

// EquippedList 返回所有已穿戴装备（非空槽位），用于战力计算。
func (p *Player) EquippedList() []*Equipment {
	out := []*Equipment{}
	for _, eq := range p.Equipped {
		if eq != nil {
			out = append(out, eq)
		}
	}
	return out
}
```

- [ ] **Step 2: 验证编译**

Run:
```bash
cd server
go build ./...
```
Expected: 无输出，退出码 0。

- [ ] **Step 3: 提交**

```bash
git add server/internal/model/player.go
git commit -m "feat: player model with equipment bag and equipped slots"
```

---

## Task 5: 协议扩展（loot 与 floor push 消息）

**Files:**
- Modify: `server/internal/protocol/message.go`

新增掉落推送与层数推送消息类型。

- [ ] **Step 1: 追加协议类型**

在 `server/internal/protocol/message.go` 末尾追加：
```go
// 消息类型常量（追加）
const (
	TypeLoot  = "loot"  // 推送：掉落装备
	TypeFloor = "floor" // 推送：层数推进
)

// LootData 掉落推送消息体。
type LootData struct {
	UID     string  `json:"uid"`     // 装备唯一 ID
	BaseID  string  `json:"base_id"` // 基底 ID
	Name    string  `json:"name"`    // 装备名
	Slot    int     `json:"slot"`    // 槽位
	Rarity  int     `json:"rarity"`  // 稀有度
	Upgrade int     `json:"upgrade"` // 强化等级
	Affixes []AffixDTO `json:"affixes"` // 词缀列表
}

// AffixDTO 词缀传输对象。
type AffixDTO struct {
	Type  string  `json:"type"`
	Tier  int     `json:"tier"`
	Value float64 `json:"value"`
}

// FloorData 层数推送消息体。
type FloorData struct {
	Floor int `json:"floor"` // 新层数
}
```

注意：需把原来的 `const` 块里的 `TypeLogin`/`TypeSync` 保留，新增 `TypeLoot`/`TypeFloor` 放进同一 const 块。完整改法：把消息类型常量块合并为一个。

完整修改后的常量块应为：
```go
const (
	TypeLogin = "login"
	TypeSync  = "sync"
	TypeLoot  = "loot"
	TypeFloor = "floor"
)
```

- [ ] **Step 2: 验证编译**

Run:
```bash
cd server
go build ./...
```
Expected: 无输出，退出码 0。

- [ ] **Step 3: 提交**

```bash
git add server/internal/protocol/message.go
git commit -m "feat: protocol loot and floor push message types"
```

---

## Task 6: 地下城推进器

**Files:**
- Create: `server/internal/dungeon/runner.go`
- Create: `server/internal/dungeon/runner_test.go`

Runner 负责单玩家的地下城推进：计算玩家战力、与当前层怪物战斗、胜利则推进下一层、记录击杀。

- [ ] **Step 1: 写推进器测试**

Create `server/internal/dungeon/runner_test.go`:
```go
package dungeon

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

func makePlayer() *model.Player {
	return model.NewPlayer("test")
}

func TestRunner_PlayerStronger_AdvancesFloor(t *testing.T) {
	p := makePlayer()
	// 给玩家一件强力装备（力量 1000）
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 1000},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	r := NewRunner(p, combat.ComputePower, loot.NewDropTable(gen))
	startFloor := p.Floor
	r.Tick()
	if p.Floor <= startFloor {
		t.Fatalf("floor = %d, should advance from %d", p.Floor, startFloor)
	}
}

func TestRunner_PlayerWeaker_StaysFloor(t *testing.T) {
	p := makePlayer()
	// 玩家无装备，战力低
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	r := NewRunner(p, combat.ComputePower, loot.NewDropTable(gen))
	startFloor := p.Floor
	r.Tick()
	if p.Floor != startFloor {
		t.Fatalf("floor = %d, should stay at %d", p.Floor, startFloor)
	}
}

func TestRunner_WinDropsEquipment(t *testing.T) {
	p := makePlayer()
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 1000},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	r := NewRunner(p, combat.ComputePower, loot.NewDropTable(gen))
	r.Tick()
	if len(p.EquipBag) == 0 {
		t.Fatal("no equipment dropped after winning a battle")
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/dungeon/...
```
Expected: 编译失败，`NewRunner` 未定义。

- [ ] **Step 3: 写推进器实现**

Create `server/internal/dungeon/runner.go`:
```go
package dungeon

import (
	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

// PowerFunc 计算玩家战力的函数（可注入 combat.ComputePower）。
type PowerFunc func(combat.Stats) float64

// Runner 单玩家地下城推进器。
type Runner struct {
	player   *model.Player
	powerFn  PowerFunc
	drop     *loot.DropTable
	// LootCallback 每次掉落时回调（用于 ws 层 push 给客户端）。
	LootCallback func(eq *model.Equipment)
	// FloorCallback 每次推进层数时回调。
	FloorCallback func(newFloor int)
}

// NewRunner 创建推进器。
func NewRunner(player *model.Player, powerFn PowerFunc, drop *loot.DropTable) *Runner {
	return &Runner{player: player, powerFn: powerFn, drop: drop}
}

// Tick 执行一次战斗 tick：算战力 → 战斗 → 胜则掉落+推进 → 负则停留。
func (r *Runner) Tick() {
	stats := combat.AggregateStats(r.player.EquippedList())
	playerPower := r.powerFn(stats)
	monster := data.MonsterAt(r.player.Floor)
	result := combat.Battle(playerPower, monster.Power)
	if !result.Win {
		return
	}
	// 胜利：掉落装备
	eq := r.drop.DropRandomSlot(r.player.Floor)
	if eq != nil {
		r.player.AddEquipment(eq)
		if r.LootCallback != nil {
			r.LootCallback(eq)
		}
	}
	// 推进下一层
	r.player.Floor++
	if r.FloorCallback != nil {
		r.FloorCallback(r.player.Floor)
	}
}
```

- [ ] **Step 4: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/dungeon/...
```
Expected: `ok equipment-idle-server/internal/dungeon`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/dungeon/
git commit -m "feat: dungeon runner with battle and loot drop on win"
```

---

## Task 7: 会话战斗循环（ws 层集成）

**Files:**
- Modify: `server/internal/ws/hub.go`
- Modify: `server/internal/ws/handler.go`

登录后为会话创建 Runner，启动定时 goroutine 调用 Tick，通过回调 push 掉落与层数。

- [ ] **Step 1: 扩展 Hub 持有 Generator 与 DropTable**

修改 `server/internal/ws/hub.go`：
- Hub 新增字段 `gen *loot.Generator`
- `NewHub` 接收 store，内部创建 `gen = loot.NewGenerator(rand.New(rand.NewSource(time.Now().UnixNano())))`
- Session 新增字段 `runner *dungeon.Runner` 和 `stopCh chan struct{}`

完整改写 `hub.go`（保留原有连接管理，新增 runner 相关）：
```go
package ws

import (
	"log"
	"math/rand"
	"net/http"
	"sync"
	"time"

	"github.com/gorilla/websocket"

	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/save"
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool { return true },
}

// Session 一个已连接的客户端会话。
type Session struct {
	Conn    *websocket.Conn
	Account string
	Send    chan []byte
	runner  interface{} // *dungeon.Runner，用 interface 避免循环依赖；实际由 handler 设置
	stopCh  chan struct{}
}

// Hub 管理所有连接与会话。
type Hub struct {
	mu       sync.Mutex
	sessions map[*Session]struct{}
	store    *save.MemoryStore
	gen      *loot.Generator
}

// NewHub 创建 Hub。
func NewHub(store *save.MemoryStore) *Hub {
	return &Hub{
		sessions: make(map[*Session]struct{}),
		store:    store,
		gen:      loot.NewGenerator(rand.New(rand.NewSource(time.Now().UnixNano()))),
	}
}

// ServeWS 处理 WebSocket 升级请求。
func (h *Hub) ServeWS(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Printf("upgrade error: %v", err)
		return
	}
	sess := &Session{Conn: conn, Send: make(chan []byte, 64), stopCh: make(chan struct{})}
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
	if sess.stopCh != nil {
		select {
		case <-sess.stopCh: // 已关闭
		default:
			close(sess.stopCh)
		}
	}
}

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

func (h *Hub) writePump(sess *Session) {
	for msg := range sess.Send {
		if err := sess.Conn.WriteMessage(websocket.TextMessage, msg); err != nil {
			break
		}
	}
}
```

- [ ] **Step 2: 扩展 handler 启动战斗循环**

改写 `server/internal/ws/handler.go`：
```go
package ws

import (
	"encoding/json"
	"log"
	"time"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/protocol"
)

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

// handleLogin 处理登录：加载/创建玩家，回发全量同步，启动战斗循环。
func (h *Hub) handleLogin(sess *Session, env protocol.Envelope) {
	var req protocol.LoginRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		log.Printf("login parse error: %v", err)
		return
	}
	player := h.store.LoadOrCreate(req.Account)
	sess.Account = req.Account

	// 回发全量同步
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

	// 启动战斗循环
	drop := loot.NewDropTable(h.gen)
	runner := dungeon.NewRunner(player, combat.ComputePower, drop)
	runner.LootCallback = func(eq *model.Equipment) {
		h.pushLoot(sess, eq)
	}
	runner.FloorCallback = func(newFloor int) {
		h.pushFloor(sess, newFloor)
	}
	sess.runner = runner
	go h.battleLoop(sess, runner)
}

// battleLoop 定时战斗循环，每 2 秒一次 tick。
func (h *Hub) battleLoop(sess *Session, runner *dungeon.Runner) {
	ticker := time.NewTicker(2 * time.Second)
	defer ticker.Stop()
	for {
		select {
		case <-sess.stopCh:
			return
		case <-ticker.C:
			runner.Tick()
		}
	}
}

// pushLoot 把掉落装备推送给客户端。
func (h *Hub) pushLoot(sess *Session, eq *model.Equipment) {
	affixes := make([]protocol.AffixDTO, len(eq.Affixes))
	for i, a := range eq.Affixes {
		affixes[i] = protocol.AffixDTO{
			Type:  string(a.Type),
			Tier:  a.Tier,
			Value: a.Value,
		}
	}
	ld := protocol.LootData{
		UID:     eq.UID,
		BaseID:  eq.BaseID,
		Name:    eq.Name,
		Slot:    int(eq.Slot),
		Rarity:  int(eq.Rarity),
		Upgrade: eq.Upgrade,
		Affixes: affixes,
	}
	data, _ := json.Marshal(ld)
	env := protocol.Envelope{T: protocol.TypeLoot, Data: data}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default: // 发送缓冲满则丢弃，避免阻塞战斗循环
	}
}

// pushFloor 把层数推进推送给客户端。
func (h *Hub) pushFloor(sess *Session, newFloor int) {
	fd := protocol.FloorData{Floor: newFloor}
	data, _ := json.Marshal(fd)
	env := protocol.Envelope{T: protocol.TypeFloor, Data: data}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}
```

- [ ] **Step 3: 验证编译**

Run:
```bash
cd server
go build ./...
```
Expected: 无输出，退出码 0。

- [ ] **Step 4: 跑全量测试确认无回归**

Run:
```bash
cd server
go test ./...
```
Expected: 全部 `ok`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/ws/hub.go server/internal/ws/handler.go
git commit -m "feat: session battle loop with loot and floor push"
```

---

## Task 8: 端到端冒烟验证

**Files:**
- 无新文件，手动验证

验证完整链路：启动服务端 → 客户端连接 → 登录后自动战斗 → 收到掉落 push。

- [ ] **Step 1: 启动服务端**

Run:
```bash
cd server
go run ./cmd/server/
```
Expected: 日志 `equipment-idle-server listening on ws://localhost:8080/ws`。

- [ ] **Step 2: 用 Unity 客户端连接验证**

用 Unity Hub 打开 `client/`，Play Main 场景，连接后等待。
Expected: 约 2 秒后开始收到 loot 消息（当前客户端仅显示 sync，掉落 push 暂在服务端日志可见）。层数推进通过 sync 重连可见。

- [ ] **Step 3: 跑全量测试**

Run:
```bash
cd server
go test ./...
```
Expected: 全部 `ok`。

- [ ] **Step 4: 提交（如有改动）**

无代码改动则跳过。

---

## 验收标准

阶段 2 完成后须满足：
1. `cd server && go test ./...` 全部通过（含 data/monster、combat/battle、loot/drop、dungeon/runner）
2. 怪物强度按层数增长，每 5 层 Boss 跳升
3. 玩家战力 > 怪物强度时战斗胜利，掉落装备入背包，层数 +1
4. 登录后服务端启动每会话战斗循环，每 2 秒结算一次
5. 掉落与层数推进通过 loot/floor 消息 push 给客户端

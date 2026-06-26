# 阶段 5：离线结算 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让放置游戏名副其实——玩家关游戏后角色继续自动战斗，下次登录结算离线收益（按 DPS×时长×系数×掉落表），装备入背包，8 小时上限。

**Architecture:** `offline` 包是纯逻辑：给定玩家战力、离线时长、掉落表 → 计算战斗结果（能赢几层、掉落哪些装备）。玩家模型扩展 `LastOnline` 时间戳与离线期间推进的层数。MemoryStore 记录上次离线时间。ws handler 在登录时调用离线结算，推送结果给客户端。

**Tech Stack:** Go 1.26、标准库 `time`。

**前置条件：** 阶段 0~4 完成（combat/loot/dungeon/model 全就绪）。

---

## 设计要点

- 离线收益公式：`离线时长(秒) × 玩家DPS系数 × 0.5 × 每次掉落概率`
- 简化模型：离线期间按玩家战力 vs 当前层怪物，胜则掉落+推进（与在线 Tick 相同逻辑），按时间换算 tick 次数
- tick 间隔 = 2 秒（与在线一致），`离线 tick 数 = 离线秒数 / 2`
- 8 小时上限 = 28800 秒 = 14400 ticks
- 离线结算复用 dungeon.Runner.Tick，但用模拟 tick 计数而非真实定时器
- 离线产出的装备直接进 EquipBag（待整理背包，保留仪式感）

---

## 文件结构

**服务端**
- `server/internal/model/player.go` — 扩展 LastOnline 时间戳
- `server/internal/save/memory.go` — LoadOrCreate 时记录当前时间为 LastOnline（新玩家）/ 保留旧值（老玩家）
- `server/internal/offline/calc.go` — 离线结算纯逻辑
- `server/internal/offline/calc_test.go` — 离线结算单测
- `server/internal/protocol/message.go` — 扩展 offline_result 推送类型
- `server/internal/ws/handler.go` — 登录时调用离线结算并推送结果

---

## Task 1: 玩家模型扩展 LastOnline

**Files:**
- Modify: `server/internal/model/player.go`

- [ ] **Step 1: 追加 LastOnline 字段**

在 Player struct 追加：
```go
	LastOnline time.Time // 上次在线时间（离线结算用）
```

import 块追加 `"time"`。

NewPlayer 初始化追加：
```go
		LastOnline: time.Now(),
```

- [ ] **Step 2: 验证编译**

Run:
```bash
cd server
go build ./...
```
Expected: 无输出。

- [ ] **Step 3: 提交**

```bash
git add server/internal/model/player.go
git commit -m "feat: player LastOnline timestamp for offline calc"
```

---

## Task 2: 离线结算纯逻辑（TDD）

**Files:**
- Create: `server/internal/offline/calc.go`
- Create: `server/internal/offline/calc_test.go`

- [ ] **Step 1: 写离线结算测试**

Create `server/internal/offline/calc_test.go`:
```go
package offline

import (
	"math/rand"
	"testing"
	"time"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

func TestCalc_OfflineDurationCappedAt8Hours(t *testing.T) {
	p := model.NewPlayer("t")
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	drop := loot.NewDropTable(gen)
	// 离线 10 小时
	result := Calc(p, combat.ComputePower, drop, 10*time.Hour)
	if result.Duration > 8*time.Hour {
		t.Fatalf("duration = %v, want <= 8h", result.Duration)
	}
}

func TestCalc_StrongPlayerGainsLoot(t *testing.T) {
	p := model.NewPlayer("t")
	// 给强装备
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 1000},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	drop := loot.NewDropTable(gen)
	result := Calc(p, combat.ComputePower, drop, 1*time.Hour)
	if result.TicksSimulated == 0 {
		t.Fatal("should simulate some ticks")
	}
	if result.LootCount == 0 {
		t.Fatal("strong player should gain loot while offline")
	}
	if len(p.EquipBag) != result.LootCount {
		t.Fatalf("bag size = %d, want %d", len(p.EquipBag), result.LootCount)
	}
}

func TestCalc_WeakPlayerNoLoot(t *testing.T) {
	p := model.NewPlayer("t")
	// 无装备，战力 0，打不过第1层
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	drop := loot.NewDropTable(gen)
	result := Calc(p, combat.ComputePower, drop, 1*time.Hour)
	if result.LootCount != 0 {
		t.Fatalf("weak player loot = %d, want 0", result.LootCount)
	}
	if result.TicksSimulated == 0 {
		t.Fatal("should still simulate ticks (just lose)")
	}
}

func TestCalc_ZeroDurationNoOp(t *testing.T) {
	p := model.NewPlayer("t")
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	drop := loot.NewDropTable(gen)
	result := Calc(p, combat.ComputePower, drop, 0)
	if result.TicksSimulated != 0 {
		t.Fatal("zero duration should simulate 0 ticks")
	}
}

func TestCalc_AdvancesFloor(t *testing.T) {
	p := model.NewPlayer("t")
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 100000},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	drop := loot.NewDropTable(gen)
	startFloor := p.Floor
	result := Calc(p, combat.ComputePower, drop, 10*time.Second)
	if p.Floor <= startFloor {
		t.Fatalf("floor = %d, should advance from %d", p.Floor, startFloor)
	}
	if result.FloorsAdvanced == 0 {
		t.Fatal("should report floor advances")
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/offline/...
```
Expected: 编译失败，`Calc` 未定义。

- [ ] **Step 3: 写离线结算实现**

Create `server/internal/offline/calc.go`:
```go
package offline

import (
	"time"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

// MaxOfflineDuration 离线收益上限 8 小时。
const MaxOfflineDuration = 8 * time.Hour

// TickInterval 离线模拟的 tick 间隔，与在线一致 2 秒。
const TickInterval = 2 * time.Second

// OfflineResult 离线结算结果。
type OfflineResult struct {
	Duration        time.Duration // 实际结算的离线时长（封顶后）
	TicksSimulated  int           // 模拟的 tick 数
	LootCount       int           // 掉落装备数
	FloorsAdvanced  int           // 推进层数
}

// Calc 离线结算：按玩家战力模拟离线期间的战斗，掉落装备入背包。
// powerFn 计算战力，纯逻辑可单测。
func Calc(p *model.Player, powerFn func(combat.Stats) float64, drop *loot.DropTable, rawDuration time.Duration) OfflineResult {
	// 封顶 8 小时
	if rawDuration > MaxOfflineDuration {
		rawDuration = MaxOfflineDuration
	}
	if rawDuration <= 0 {
		return OfflineResult{}
	}
	ticks := int(rawDuration / TickInterval)
	result := OfflineResult{Duration: rawDuration, TicksSimulated: ticks}

	for i := 0; i < ticks; i++ {
		stats := combat.AggregateStats(p.EquippedList())
		playerPower := powerFn(stats)
		monster := data.MonsterAt(p.Floor)
		if playerPower <= monster.Power {
			continue // 打不过，停留
		}
		// 胜利：掉落
		eq := drop.DropRandomSlot(p.Floor)
		if eq != nil {
			p.AddEquipment(eq)
			result.LootCount++
		}
		// 推进
		p.Floor++
		result.FloorsAdvanced++
	}
	return result
}
```

- [ ] **Step 4: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/offline/...
```
Expected: `ok`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/offline/
git commit -m "feat: offline calc - simulate ticks capped at 8h"
```

---

## Task 3: 协议扩展（offline_result 推送）

**Files:**
- Modify: `server/internal/protocol/message.go`

- [ ] **Step 1: 追加协议类型**

在常量块追加：
```go
	TypeOfflineResult = "offline_result" // 推送：离线结算结果
```

在文件末尾追加：
```go
// OfflineResultData 离线结算结果推送。
type OfflineResultData struct {
	DurationSeconds int `json:"duration_seconds"` // 结算时长（秒）
	TicksSimulated  int `json:"ticks_simulated"`  // 模拟 tick 数
	LootCount       int `json:"loot_count"`       // 掉落数
	FloorsAdvanced  int `json:"floors_advanced"`  // 推进层数
}
```

- [ ] **Step 2: 验证编译**

Run:
```bash
cd server
go build ./...
```
Expected: 无输出。

- [ ] **Step 3: 提交**

```bash
git add server/internal/protocol/message.go
git commit -m "feat: protocol offline_result type"
```

---

## Task 4: ws handler 登录时离线结算

**Files:**
- Modify: `server/internal/ws/handler.go`
- Modify: `server/internal/model/player.go`（追加 SetOnline/离线时间管理）

- [ ] **Step 1: 在 handleLogin 开头插入离线结算**

在 `handleLogin` 中，`player := h.store.LoadOrCreate(req.Account)` 之后、回发 sync 之前，插入离线结算：
```go
	// 离线结算：计算上次离线时长
	now := time.Now()
	if !player.LastOnline.IsZero() {
		offlineDuration := now.Sub(player.LastOnline)
		if offlineDuration > 0 {
			drop := loot.NewDropTable(h.gen)
			result := offline.Calc(player, combat.ComputePower, drop, offlineDuration)
			if result.TicksSimulated > 0 {
				h.pushOfflineResult(sess, result)
			}
		}
	}
	player.LastOnline = now
```

在 handler.go import 块追加：
```go
	"equipment-idle-server/internal/offline"
```

- [ ] **Step 2: 追加 pushOfflineResult**

在 handler.go 追加：
```go
// pushOfflineResult 推送离线结算结果。
func (h *Hub) pushOfflineResult(sess *Session, result offline.OfflineResult) {
	od := protocol.OfflineResultData{
		DurationSeconds: int(result.Duration.Seconds()),
		TicksSimulated:  result.TicksSimulated,
		LootCount:       result.LootCount,
		FloorsAdvanced:  result.FloorsAdvanced,
	}
	dataBytes, _ := json.Marshal(od)
	env := protocol.Envelope{T: protocol.TypeOfflineResult, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}
```

- [ ] **Step 3: 在断开连接时记录离线时间**

在 hub.go 的 `unregister` 方法中，在关闭 stopCh 之后，无法直接访问 player。改为在 battleLoop 结束时更新。修改 `battleLoop`：
```go
func (h *Hub) battleLoop(sess *Session, runner *dungeon.Runner) {
	ticker := time.NewTicker(2 * time.Second)
	defer ticker.Stop()
	for {
		select {
		case <-sess.stopCh:
			// 记录离线时间
			if sess.Account != "" {
				p := h.store.LoadOrCreate(sess.Account)
				p.LastOnline = time.Now()
			}
			return
		case <-ticker.C:
			runner.Tick()
		}
	}
}
```

- [ ] **Step 4: 验证编译与测试**

Run:
```bash
cd server
go build ./... && go test ./...
```
Expected: 全部 `ok`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/ws/handler.go server/internal/ws/hub.go
git commit -m "feat: ws offline calc on login, record LastOnline on disconnect"
```

---

## Task 5: 离线结算冒烟测试 + 全量回归

**Files:**
- Create: `server/cmd/smokeoffline/main.go`

- [ ] **Step 1: 写冒烟测试**

Create `server/cmd/smokeoffline/main.go`:
```go
package main

import (
	"fmt"
	"math/rand"
	"time"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/offline"
)

func main() {
	p := model.NewPlayer("smoke")
	// 给强装备，确保能打怪
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 50000},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(11)))
	drop := loot.NewDropTable(gen)

	startFloor := p.Floor
	result := offline.Calc(p, combat.ComputePower, drop, 2*time.Hour)
	fmt.Printf("offline 2h: ticks=%d loot=%d floors=%d (floor %d->%d)\n",
		result.TicksSimulated, result.LootCount, result.FloorsAdvanced,
		startFloor, p.Floor)

	if result.LootCount == 0 {
		fmt.Println("FAIL: no offline loot"); return
	}
	if p.Floor <= startFloor {
		fmt.Println("FAIL: no floor advance"); return
	}

	// 测试 8 小时封顶
	p2 := model.NewPlayer("smoke2")
	p2.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 50000},
	}
	result2 := offline.Calc(p2, combat.ComputePower, drop, 10*time.Hour)
	if result2.Duration > 8*time.Hour {
		fmt.Println("FAIL: not capped at 8h"); return
	}
	fmt.Printf("offline 10h (capped): duration=%v ticks=%d\n", result2.Duration, result2.TicksSimulated)

	fmt.Println("SMOKE_OK: offline calc works")
}
```

- [ ] **Step 2: 运行冒烟**

Run:
```bash
cd server
go run ./cmd/smokeoffline/
```
Expected: `SMOKE_OK: offline calc works`，打印离线 2 小时的掉落数与层数推进。

- [ ] **Step 3: 全量回归**

Run:
```bash
cd server
go test ./...
```
Expected: 全部 `ok`。

- [ ] **Step 4: 提交**

```bash
git add server/cmd/smokeoffline/main.go
git commit -m "test: smoke test offline calc"
```

---

## 验收标准

阶段 5 完成后须满足：
1. `cd server && go test ./...` 全部通过（含 offline 包）
2. 离线结算按 DPS×时长×系数模拟战斗，装备入背包
3. 离线时长封顶 8 小时
4. 强玩家离线有掉落+推进，弱玩家无掉落
5. 登录时结算并推送 offline_result，断开时记录 LastOnline
6. 冒烟测试：离线 2 小时有掉落+推进，10 小时封顶 8 小时

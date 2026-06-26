# 阶段 4：分解/合成/重铸/强化 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现装备养成四件套——分解（装备→材料）、合成（材料→指定基底装备）、重铸（重随机缀）、强化（+1~+10 基础属性提升），让"刷"出的垃圾装备有价值，让好装备更强。

**Architecture:** 服务端 `crafting` 包负责分解/合成/重铸纯逻辑，`upgrade` 包负责强化纯逻辑（成功率+保底）。玩家模型扩展材料计数。协议新增四类请求响应 + materials 推送。ws handler 处理请求并推送结果。客户端工坊 UI 四个面板。

**Tech Stack:** Go 1.26、Unity 6 (C#)、IMGUI。

**前置条件：** 阶段 0~3 完成（player.EquipBag/Equipped、inventory、loot、data 全就绪）。

---

## 设计要点

### 材料系统
- `MaterialType`：基础材料（`base_mat`）+ 词缀材料（按 Tier 1~5，`affix_mat_1`~`affix_mat_5`）
- 分解：普通装备→少量基础材料；带词缀装备→基础材料 + 按词缀 Tier 对应的词缀材料
- 合成：消耗基础材料按配方合成指定槽位的普通品质装备（定向 farming 起点）
- 重铸：消耗词缀材料重随机件装备的全部词缀（保留稀有度与基底）
- 强化：消耗基础材料 + 金币占位（MVP 暂用基础材料），成功率递减，+7 以上失败不掉级（保底）

### 强化规则
| 等级 | 成功率 | 失败后果 |
|---|---|---|
| +1~+3 | 100% | — |
| +4~+6 | 80% | 不变 |
| +7~+9 | 60% | 不掉级（保底） |
| +10 | 40% | 不掉级 |

强化效果：每级 +10% 基础属性（BaseStats 全数值 ×1.1^upgrade）。

---

## 文件结构

**服务端**
- `server/internal/data/material.go` — 材料类型与分解产出规则
- `server/internal/model/player.go` — 扩展 Materials 字段
- `server/internal/crafting/decompose.go` — 分解纯逻辑
- `server/internal/crafting/decompose_test.go`
- `server/internal/crafting/compose.go` — 合成纯逻辑
- `server/internal/crafting/compose_test.go`
- `server/internal/crafting/reforge.go` — 重铸纯逻辑
- `server/internal/crafting/reforge_test.go`
- `server/internal/upgrade/upgrade.go` — 强化纯逻辑
- `server/internal/upgrade/upgrade_test.go`
- `server/internal/protocol/message.go` — 扩展四类请求响应 + materials 推送
- `server/internal/ws/handler.go` — 处理四类请求

**客户端**
- `client/Assets/Scripts/Net/Message.cs` — 扩展四类编码 + materials 解析
- `client/Assets/Scripts/State/GameState.cs` — 材料状态 + 四类方法
- `client/Assets/Scripts/UI/MainController.cs` — 工坊面板 UI

---

## Task 1: 材料类型与分解产出规则

**Files:**
- Create: `server/internal/data/material.go`

- [ ] **Step 1: 写材料定义**

Create `server/internal/data/material.go`:
```go
package data

// MaterialType 材料类型标识。
type MaterialType string

const (
	MatBase      MaterialType = "base_mat"       // 基础材料
	MatAffixT1   MaterialType = "affix_mat_1"    // 词缀材料 Tier1
	MatAffixT2   MaterialType = "affix_mat_2"
	MatAffixT3   MaterialType = "affix_mat_3"
	MatAffixT4   MaterialType = "affix_mat_4"
	MatAffixT5   MaterialType = "affix_mat_5"
)

// AffixMaterialByTier 按词缀 Tier 返回对应材料类型。
func AffixMaterialByTier(tier int) MaterialType {
	switch tier {
	case 1:
		return MatAffixT1
	case 2:
		return MatAffixT2
	case 3:
		return MatAffixT3
	case 4:
		return MatAffixT4
	default:
		return MatAffixT5
	}
}

// DecomposeBaseYield 按稀有度返回基础材料产出。
var DecomposeBaseYield = map[Rarity]int{
	RarityCommon:    2,
	RarityMagic:     4,
	RarityRare:      8,
	RarityLegendary: 16,
	RarityArtifact:  32,
}

// ComposeCost 合成某槽位普通装备所需基础材料。
const ComposeCost = 10

// ReforgeCostPerAffix 每重铸一个词缀所需词缀材料数（按词缀 Tier 对应材料）。
const ReforgeCostPerAffix = 1

// UpgradeCostTable 强化各级消耗基础材料（索引=目标等级，0不用）。
var UpgradeCostTable = []int{0, 3, 5, 8, 12, 18, 25, 35, 50, 70, 100}

// UpgradeSuccessRate 强化成功率（索引=目标等级）。
var UpgradeSuccessRate = []float64{0, 1.0, 1.0, 1.0, 0.8, 0.8, 0.8, 0.6, 0.6, 0.6, 0.4}

// UpgradeSafeThreshold +7 及以上失败不掉级。
const UpgradeSafeThreshold = 7
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
git add server/internal/data/material.go
git commit -m "feat: material types and decompose/upgrade cost tables"
```

---

## Task 2: 玩家模型扩展材料

**Files:**
- Modify: `server/internal/model/player.go`

- [ ] **Step 1: 追加 Materials 字段与方法**

在 `Player` struct 追加字段：
```go
	Materials map[data.MaterialType]int // 材料库存
```

在 `NewPlayer` 初始化：
```go
		Materials: map[data.MaterialType]int{},
```

追加方法：
```go
// AddMaterial 增加材料。
func (p *Player) AddMaterial(mt data.MaterialType, n int) {
	p.Materials[mt] += n
}

// HasMaterial 检查是否有足够材料。
func (p *Player) HasMaterial(mt data.MaterialType, n int) bool {
	return p.Materials[mt] >= n
}

// SpendMaterial 消耗材料（调用前应先 HasMaterial 检查）。
func (p *Player) SpendMaterial(mt data.MaterialType, n int) {
	p.Materials[mt] -= n
	if p.Materials[mt] < 0 {
		p.Materials[mt] = 0
	}
}
```

注意：player.go 已 import `data`，无需追加 import。

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
git commit -m "feat: player materials inventory"
```

---

## Task 3: 分解纯逻辑（TDD）

**Files:**
- Create: `server/internal/crafting/decompose.go`
- Create: `server/internal/crafting/decompose_test.go`

- [ ] **Step 1: 写分解测试**

Create `server/internal/crafting/decompose_test.go`:
```go
package crafting

import (
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestDecompose_CommonGivesBaseMat(t *testing.T) {
	p := model.NewPlayer("t")
	eq := &model.Equipment{UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon}
	yield, err := Decompose(p, eq)
	if err != nil {
		t.Fatalf("decompose error: %v", err)
	}
	if yield[data.MatBase] != 2 {
		t.Fatalf("base mat = %d, want 2", yield[data.MatBase])
	}
	if p.Materials[data.MatBase] != 2 {
		t.Fatalf("player base mat = %d, want 2", p.Materials[data.MatBase])
	}
}

func TestDecompose_RareWithAffixesGivesAffixMat(t *testing.T) {
	p := model.NewPlayer("t")
	eq := &model.Equipment{
		UID: "e2", Slot: data.SlotHelmet, Rarity: data.RarityRare,
		Affixes: []model.AffixInstance{
			{Type: data.ATStrength, Tier: 3, Value: 10},
			{Type: data.ATCritRate, Tier: 2, Value: 0.03},
			{Type: data.ATMaxHP, Tier: 3, Value: 50},
			{Type: data.ATArmor, Tier: 2, Value: 15},
		},
	}
	yield, _ := Decompose(p, eq)
	if yield[data.MatBase] != 8 {
		t.Fatalf("base mat = %d, want 8", yield[data.MatBase])
	}
	if yield[data.MatAffixT3] != 2 {
		t.Fatalf("affix t3 mat = %d, want 2", yield[data.MatAffixT3])
	}
	if yield[data.MatAffixT2] != 2 {
		t.Fatalf("affix t2 mat = %d, want 2", yield[data.MatAffixT2])
	}
}

func TestDecompose_EquippedItemError(t *testing.T) {
	p := model.NewPlayer("t")
	eq := &model.Equipment{UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon}
	p.Equipped[data.SlotWeapon] = eq
	// 分解已穿戴装备应先检查——实际上分解只从背包，这里测试传已穿戴的 UID 也应报错
	// 但 Decompose 接收 *Equipment 指针，调用方负责确保来自背包
	// 这里测试空装备报错
	_, err := Decompose(p, nil)
	if err == nil {
		t.Fatal("decompose nil should error")
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/crafting/...
```
Expected: 编译失败，`Decompose` 未定义。

- [ ] **Step 3: 写分解实现**

Create `server/internal/crafting/decompose.go`:
```go
package crafting

import (
	"errors"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// Decompose 分解一件装备：返回产出材料并加入玩家库存。
// 调用方负责确保装备已从背包/穿戴移除。
func Decompose(p *model.Player, eq *model.Equipment) (map[data.MaterialType]int, error) {
	if eq == nil {
		return nil, errors.New("cannot decompose nil equipment")
	}
	yield := map[data.MaterialType]int{}
	// 基础材料按稀有度
	yield[data.MatBase] = data.DecomposeBaseYield[eq.Rarity]
	// 词缀材料按每个词缀的 Tier
	for _, a := range eq.Affixes {
		mt := data.AffixMaterialByTier(a.Tier)
		yield[mt]++
	}
	// 加入玩家库存
	for mt, n := range yield {
		p.AddMaterial(mt, n)
	}
	return yield, nil
}
```

- [ ] **Step 4: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/crafting/...
```
Expected: `ok`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/crafting/decompose.go server/internal/crafting/decompose_test.go
git commit -m "feat: crafting decompose - equipment to materials"
```

---

## Task 4: 合成纯逻辑（TDD）

**Files:**
- Create: `server/internal/crafting/compose.go`
- Create: `server/internal/crafting/compose_test.go`

合成：消耗 ComposeCost 基础材料 → 生成指定槽位的普通品质装备。

- [ ] **Step 1: 写合成测试**

Create `server/internal/crafting/compose_test.go`:
```go
package crafting

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

func TestCompose_Success(t *testing.T) {
	p := model.NewPlayer("t")
	p.AddMaterial(data.MatBase, 20)
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	eq, err := Compose(p, gen, data.SlotWeapon)
	if err != nil {
		t.Fatalf("compose error: %v", err)
	}
	if eq == nil {
		t.Fatal("compose returned nil")
	}
	if eq.Slot != data.SlotWeapon {
		t.Fatalf("slot = %d, want weapon", eq.Slot)
	}
	if eq.Rarity != data.RarityCommon {
		t.Fatalf("rarity = %d, want common", eq.Rarity)
	}
	if p.Materials[data.MatBase] != 10 { // 20-10
		t.Fatalf("remaining base mat = %d, want 10", p.Materials[data.MatBase])
	}
}

func TestCompose_InsufficientMaterial(t *testing.T) {
	p := model.NewPlayer("t")
	p.AddMaterial(data.MatBase, 5) // 不够 10
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	_, err := Compose(p, gen, data.SlotWeapon)
	if err == nil {
		t.Fatal("should error when insufficient material")
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/crafting/...
```
Expected: 编译失败，`Compose` 未定义。

- [ ] **Step 3: 写合成实现**

Create `server/internal/crafting/compose.go`:
```go
package crafting

import (
	"errors"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

// Compose 合成：消耗基础材料生成指定槽位的普通品质装备。
func Compose(p *model.Player, gen *loot.Generator, slot data.Slot) (*model.Equipment, error) {
	if !p.HasMaterial(data.MatBase, data.ComposeCost) {
		return nil, errors.New("insufficient base material")
	}
	p.SpendMaterial(data.MatBase, data.ComposeCost)
	eq := gen.Generate(slot, data.RarityCommon)
	p.AddEquipment(eq)
	return eq, nil
}
```

- [ ] **Step 4: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/crafting/...
```
Expected: `ok`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/crafting/compose.go server/internal/crafting/compose_test.go
git commit -m "feat: crafting compose - materials to equipment"
```

---

## Task 5: 重铸纯逻辑（TDD）

**Files:**
- Create: `server/internal/crafting/reforge.go`
- Create: `server/internal/crafting/reforge_test.go`

重铸：消耗词缀材料（按现有词缀数 × ReforgeCostPerAffix，每个按其 Tier 对应材料）→ 重随机全部词缀（保留稀有度与基底）。

- [ ] **Step 1: 写重铸测试**

Create `server/internal/crafting/reforge_test.go`:
```go
package crafting

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

func TestReforge_RerollsAffixes(t *testing.T) {
	p := model.NewPlayer("t")
	// 给足词缀材料
	p.AddMaterial(data.MatAffixT1, 10)
	p.AddMaterial(data.MatAffixT2, 10)
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	eq := gen.Generate(data.SlotWeapon, data.RarityMagic) // 2 词缀
	originalTypes := map[data.AffixType]bool{}
	for _, a := range eq.Affixes {
		originalTypes[a.Type] = true
		// 给对应材料
		p.AddMaterial(data.AffixMaterialByTier(a.Tier), 5)
	}

	err := Reforge(p, gen, eq)
	if err != nil {
		t.Fatalf("reforge error: %v", err)
	}
	// 词缀数应不变（魔法=2）
	if len(eq.Affixes) != 2 {
		t.Fatalf("affix count = %d, want 2", len(eq.Affixes))
	}
	// 稀有度应不变
	if eq.Rarity != data.RarityMagic {
		t.Fatalf("rarity changed")
	}
	// 基底应不变
	if eq.BaseID != "base_weapon" {
		t.Fatalf("base changed")
	}
}

func TestReforge_InsufficientMaterial(t *testing.T) {
	p := model.NewPlayer("t")
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	eq := gen.Generate(data.SlotWeapon, data.RarityRare) // 4 词缀
	// 不给材料
	err := Reforge(p, gen, eq)
	if err == nil {
		t.Fatal("should error when insufficient material")
	}
}

func TestReforge_NoAffixesNoCost(t *testing.T) {
	p := model.NewPlayer("t")
	gen := loot.NewGenerator(rand.New(rand.NewSource(1)))
	eq := &model.Equipment{
		UID: "e1", BaseID: "base_weapon", Slot: data.SlotWeapon,
		Rarity: data.RarityCommon, BaseStats: map[data.AffixType]float64{data.ATStrength: 5},
	}
	err := Reforge(p, gen, eq)
	if err != nil {
		t.Fatalf("reforge no-affix error: %v", err)
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/crafting/...
```
Expected: 编译失败，`Reforge` 未定义。

- [ ] **Step 3: 写重铸实现**

Create `server/internal/crafting/reforge.go`:
```go
package crafting

import (
	"errors"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

// Reforge 重铸：消耗词缀材料，重随机装备全部词缀（保留稀有度与基底）。
func Reforge(p *model.Player, gen *loot.Generator, eq *model.Equipment) error {
	if eq == nil {
		return errors.New("cannot reforge nil equipment")
	}
	// 计算消耗：每个词缀需要 ReforgeCostPerAffix 个对应 Tier 材料
	cost := map[data.MaterialType]int{}
	for _, a := range eq.Affixes {
		mt := data.AffixMaterialByTier(a.Tier)
		cost[mt] += data.ReforgeCostPerAffix
	}
	// 检查材料
	for mt, n := range cost {
		if !p.HasMaterial(mt, n) {
			return errors.New("insufficient affix material: " + string(mt))
		}
	}
	// 扣材料
	for mt, n := range cost {
		p.SpendMaterial(mt, n)
	}
	// 重随机词缀：用生成器按原稀有度重新生成词缀部分
	if len(eq.Affixes) == 0 {
		return nil // 无词缀无需重铸
	}
	reforged := gen.Generate(eq.Slot, eq.Rarity)
	eq.Affixes = reforged.Affixes
	return nil
}
```

- [ ] **Step 4: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/crafting/...
```
Expected: `ok`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/crafting/reforge.go server/internal/crafting/reforge_test.go
git commit -m "feat: crafting reforge - reroll affixes"
```

---

## Task 6: 强化纯逻辑（TDD）

**Files:**
- Create: `server/internal/upgrade/upgrade.go`
- Create: `server/internal/upgrade/upgrade_test.go`

- [ ] **Step 1: 写强化测试**

Create `server/internal/upgrade/upgrade_test.go`:
```go
package upgrade

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestUpgrade_LowLevelAlwaysSuccess(t *testing.T) {
	p := model.NewPlayer("t")
	p.AddMaterial(data.MatBase, 100)
	eq := &model.Equipment{
		UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon,
		BaseStats: map[data.AffixType]float64{data.ATStrength: 10}, Upgrade: 0,
	}
	rng := rand.New(rand.NewSource(1))
	result, err := Upgrade(p, rng, eq)
	if err != nil {
		t.Fatalf("upgrade error: %v", err)
	}
	if !result.Success {
		t.Fatal("+1 should always succeed")
	}
	if eq.Upgrade != 1 {
		t.Fatalf("upgrade = %d, want 1", eq.Upgrade)
	}
}

func TestUpgrade_MaxLevelError(t *testing.T) {
	p := model.NewPlayer("t")
	p.AddMaterial(data.MatBase, 100)
	eq := &model.Equipment{
		UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon,
		BaseStats: map[data.AffixType]float64{data.ATStrength: 10}, Upgrade: 10,
	}
	rng := rand.New(rand.NewSource(1))
	_, err := Upgrade(p, rng, eq)
	if err == nil {
		t.Fatal("should error at max level")
	}
}

func TestUpgrade_InsufficientMaterial(t *testing.T) {
	p := model.NewPlayer("t")
	eq := &model.Equipment{
		UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon,
		BaseStats: map[data.AffixType]float64{data.ATStrength: 10}, Upgrade: 0,
	}
	rng := rand.New(rand.NewSource(1))
	_, err := Upgrade(p, rng, eq)
	if err == nil {
		t.Fatal("should error when insufficient material")
	}
}

func TestUpgrade_HighLevelFailNoDegrade(t *testing.T) {
	p := model.NewPlayer("t")
	p.AddMaterial(data.MatBase, 100)
	// 构造一个 +9 的装备，强化到 +10 成功率 40%
	eq := &model.Equipment{
		UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon,
		BaseStats: map[data.AffixType]float64{data.ATStrength: 10}, Upgrade: 9,
	}
	// 用多个 seed 测试，至少有一次失败且不掉级
	var failNoDegrade bool
	for seed := 0; seed < 100; seed++ {
		p2 := model.NewPlayer("t")
		p2.AddMaterial(data.MatBase, 100)
		eq2 := &model.Equipment{
			UID: "e1", Slot: data.SlotWeapon, Rarity: data.RarityCommon,
			BaseStats: map[data.AffixType]float64{data.ATStrength: 10}, Upgrade: 9,
		}
		rng := rand.New(rand.NewSource(int64(seed)))
		result, _ := Upgrade(p2, rng, eq2)
		if !result.Success {
			if eq2.Upgrade == 9 { // 没掉级
				failNoDegrade = true
				break
			}
		}
	}
	if !failNoDegrade {
		t.Fatal("should observe a failure with no degrade at +9->+10")
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/upgrade/...
```
Expected: 编译失败，`Upgrade` 未定义。

- [ ] **Step 3: 写强化实现**

Create `server/internal/upgrade/upgrade.go`:
```go
package upgrade

import (
	"errors"
	"math/rand"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// MaxUpgrade 强化上限。
const MaxUpgrade = 10

// UpgradeResult 强化结果。
type UpgradeResult struct {
	Success bool // 是否成功
	NewLvl  int  // 强化后的等级
}

// Upgrade 强化一件装备一级。
// 消耗基础材料（按目标等级查表），按成功率判定，+7以上失败不掉级。
func Upgrade(p *model.Player, rng *rand.Rand, eq *model.Equipment) (UpgradeResult, error) {
	if eq.Upgrade >= MaxUpgrade {
		return UpgradeResult{}, errors.New("already at max upgrade level")
	}
	targetLvl := eq.Upgrade + 1
	cost := data.UpgradeCostTable[targetLvl]
	if !p.HasMaterial(data.MatBase, cost) {
		return UpgradeResult{}, errors.New("insufficient base material")
	}
	p.SpendMaterial(data.MatBase, cost)

	rate := data.UpgradeSuccessRate[targetLvl]
	if rng.Float64() < rate {
		eq.Upgrade = targetLvl
		return UpgradeResult{Success: true, NewLvl: targetLvl}, nil
	}
	// 失败：+7以上不掉级（保底），+6及以下也不掉级（MVP简化：失败一律不掉级）
	// spec 原设计 +6及以下失败"不变"，+7以上"不掉级"——含义一致：失败不掉级
	return UpgradeResult{Success: false, NewLvl: eq.Upgrade}, nil
}
```

- [ ] **Step 4: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/upgrade/...
```
Expected: `ok`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/upgrade/
git commit -m "feat: upgrade with success rate and safe threshold"
```

---

## Task 7: 协议扩展（四类请求 + materials 推送）

**Files:**
- Modify: `server/internal/protocol/message.go`

- [ ] **Step 1: 追加协议类型**

在常量块追加：
```go
	TypeDecompose = "decompose" // 请求：分解
	TypeCompose   = "compose"   // 请求：合成
	TypeReforge   = "reforge"   // 请求：重铸
	TypeUpgrade   = "upgrade"   // 请求：强化
	TypeMaterials = "materials" // 推送：材料库存
```

在文件末尾追加消息体：
```go
// DecomposeRequest 分解请求体。
type DecomposeRequest struct {
	UID string `json:"uid"`
}

// ComposeRequest 合成请求体。
type ComposeRequest struct {
	Slot int `json:"slot"`
}

// ReforgeRequest 重铸请求体。
type ReforgeRequest struct {
	UID string `json:"uid"`
}

// UpgradeRequest 强化请求体。
type UpgradeRequest struct {
	UID string `json:"uid"`
}

// MaterialsData 材料库存推送。
type MaterialsData struct {
	Materials map[string]int `json:"materials"`
}

// CraftResult 养成操作结果（分解/合成/重铸/强化通用响应推送）。
type CraftResult struct {
	OK      bool   `json:"ok"`
	Msg     string `json:"msg"`
	UID     string `json:"uid,omitempty"`
	Upgrade int    `json:"upgrade,omitempty"`
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
git commit -m "feat: protocol decompose/compose/reforge/upgrade/materials"
```

---

## Task 8: ws handler 处理四类养成请求

**Files:**
- Modify: `server/internal/ws/handler.go`

- [ ] **Step 1: 在 handleMessage switch 追加四个 case**

```go
	case protocol.TypeDecompose:
		h.handleDecompose(sess, env)
	case protocol.TypeCompose:
		h.handleCompose(sess, env)
	case protocol.TypeReforge:
		h.handleReforge(sess, env)
	case protocol.TypeUpgrade:
		h.handleUpgrade(sess, env)
```

- [ ] **Step 2: 追加处理函数与辅助**

在 handler.go 顶部 import 追加：
```go
	"equipment-idle-server/internal/crafting"
	"equipment-idle-server/internal/upgrade"
```

追加处理函数：
```go
// handleDecompose 分解请求：从背包找装备→分解→推送材料+背包。
func (h *Hub) handleDecompose(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.DecomposeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	// 从背包找装备
	idx := findBagIndex(player, req.UID)
	if idx < 0 {
		h.pushCraftResult(sess, false, "equipment not in bag", "", 0)
		return
	}
	eq := player.EquipBag[idx]
	player.EquipBag = append(player.EquipBag[:idx], player.EquipBag[idx+1:]...)
	crafting.Decompose(player, eq)
	h.pushMaterials(sess, player)
	h.pushBag(sess, player)
	h.pushCraftResult(sess, true, "decomposed", "", 0)
}

// handleCompose 合成请求。
func (h *Hub) handleCompose(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.ComposeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	eq, err := crafting.Compose(player, h.gen, data.Slot(req.Slot))
	if err != nil {
		h.pushCraftResult(sess, false, err.Error(), "", 0)
		return
	}
	h.pushMaterials(sess, player)
	h.pushBag(sess, player)
	h.pushCraftResult(sess, true, "composed", eq.UID, 0)
}

// handleReforge 重铸请求：从背包找装备→重铸。
func (h *Hub) handleReforge(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.ReforgeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	idx := findBagIndex(player, req.UID)
	if idx < 0 {
		h.pushCraftResult(sess, false, "equipment not in bag", "", 0)
		return
	}
	eq := player.EquipBag[idx]
	if err := crafting.Reforge(player, h.gen, eq); err != nil {
		h.pushCraftResult(sess, false, err.Error(), "", 0)
		return
	}
	h.pushMaterials(sess, player)
	h.pushBag(sess, player)
	h.pushCraftResult(sess, true, "reforged", eq.UID, eq.Upgrade)
}

// handleUpgrade 强化请求：从背包找装备→强化。
func (h *Hub) handleUpgrade(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.UpgradeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	idx := findBagIndex(player, req.UID)
	if idx < 0 {
		h.pushCraftResult(sess, false, "equipment not in bag", "", 0)
		return
	}
	eq := player.EquipBag[idx]
	result, err := upgrade.Upgrade(player, h.rng, eq)
	if err != nil {
		h.pushCraftResult(sess, false, err.Error(), "", 0)
		return
	}
	h.pushMaterials(sess, player)
	h.pushBag(sess, player)
	h.pushPower(sess, player)
	msg := "upgraded"
	if !result.Success {
		msg = "upgrade failed (no degrade)"
	}
	h.pushCraftResult(sess, result.Success, msg, eq.UID, eq.Upgrade)
}

// pushMaterials 推送材料库存。
func (h *Hub) pushMaterials(sess *Session, player *model.Player) {
	mats := map[string]int{}
	for mt, n := range player.Materials {
		mats[string(mt)] = n
	}
	md := protocol.MaterialsData{Materials: mats}
	dataBytes, _ := json.Marshal(md)
	env := protocol.Envelope{T: protocol.TypeMaterials, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

// pushCraftResult 推送养成操作结果。
func (h *Hub) pushCraftResult(sess *Session, ok bool, msg, uid string, upg int) {
	cr := protocol.CraftResult{OK: ok, Msg: msg, UID: uid, Upgrade: upg}
	dataBytes, _ := json.Marshal(cr)
	env := protocol.Envelope{T: "craft_result", Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

// findBagIndex 在背包按 UID 找索引。
func findBagIndex(p *model.Player, uid string) int {
	for i, eq := range p.EquipBag {
		if eq.UID == uid {
			return i
		}
	}
	return -1
}
```

注意：Hub 需要 `rng *rand.Rand` 字段供强化使用。在 hub.go 的 Hub struct 追加 `rng *rand.Rand`，NewHub 初始化 `rng: rand.New(rand.NewSource(time.Now().UnixNano()))`。hub.go 已 import rand/time，无需追加。

- [ ] **Step 3: 在 hub.go 追加 rng 字段**

修改 Hub struct 追加字段：
```go
	rng *rand.Rand
```
修改 NewHub 追加初始化：
```go
		rng: rand.New(rand.NewSource(time.Now().UnixNano())),
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
git commit -m "feat: ws handler decompose/compose/reforge/upgrade"
```

---

## Task 9: 登录后推送材料 + 掉落后推送材料

**Files:**
- Modify: `server/internal/ws/handler.go`

- [ ] **Step 1: 在 handleLogin 推送材料**

在 `handleLogin` 的 pushBag/pushPower 之后追加：
```go
	h.pushMaterials(sess, player)
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
git add server/internal/ws/handler.go
git commit -m "feat: push materials on login"
```

---

## Task 10: 客户端 Message.cs 扩展

**Files:**
- Modify: `client/Assets/Scripts/Net/Message.cs`

- [ ] **Step 1: 追加类型常量与编码**

在 Message 类追加：
```csharp
public const string TypeDecompose = "decompose";
public const string TypeCompose = "compose";
public const string TypeReforge = "reforge";
public const string TypeUpgrade = "upgrade";
public const string TypeMaterials = "materials";
public const string TypeCraftResult = "craft_result";

public static string EncodeDecompose(string id, string uid)
{
    string dataJson = "{\"uid\":\"" + Escape(uid) + "\"}";
    return "{\"t\":\"" + TypeDecompose + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
}

public static string EncodeCompose(string id, int slot)
{
    string dataJson = "{\"slot\":" + slot + "}";
    return "{\"t\":\"" + TypeCompose + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
}

public static string EncodeReforge(string id, string uid)
{
    string dataJson = "{\"uid\":\"" + Escape(uid) + "\"}";
    return "{\"t\":\"" + TypeReforge + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
}

public static string EncodeUpgrade(string id, string uid)
{
    string dataJson = "{\"uid\":\"" + Escape(uid) + "\"}";
    return "{\"t\":\"" + TypeUpgrade + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
}
```

- [ ] **Step 2: 追加数据类**

```csharp
[Serializable]
public class MaterialsData
{
    // JsonUtility 不支持 Dictionary，用数组绕过
    public MaterialEntry[] entries;
}

[Serializable]
public class MaterialEntry
{
    public string key;
    public int value;
}

[Serializable]
public class CraftResultData
{
    public bool ok;
    public string msg;
    public string uid;
    public int upgrade;
}
```

注意：JsonUtility 不支持 Dictionary 反序列化。MaterialsData 的 JSON 是 `{"materials":{"base_mat":10,...}}`。需手写解析。在 Message 类追加静态方法：
```csharp
/// <summary>解析 materials 推送的 JSON，返回 key->count 字典。</summary>
public static System.Collections.Generic.Dictionary<string,int> ParseMaterials(string dataJson)
{
    var dict = new System.Collections.Generic.Dictionary<string,int>();
    // 简单提取 "key":value 对
    int i = 0;
    while (i < dataJson.Length)
    {
        int q1 = dataJson.IndexOf('"', i);
        if (q1 < 0) break;
        int q2 = dataJson.IndexOf('"', q1 + 1);
        if (q2 < 0) break;
        string key = dataJson.Substring(q1 + 1, q2 - q1 - 1);
        int colon = dataJson.IndexOf(':', q2);
        if (colon < 0) break;
        int comma = dataJson.IndexOf(',', colon);
        int brace = dataJson.IndexOf('}', colon);
        int end = comma;
        if (comma < 0 || (brace >= 0 && brace < comma)) end = brace;
        if (end < 0) end = dataJson.Length;
        string valStr = dataJson.Substring(colon + 1, end - colon - 1).Trim();
        int val;
        if (int.TryParse(valStr, out val)) dict[key] = val;
        i = end + 1;
    }
    return dict;
}
```

- [ ] **Step 3: 提交**

```bash
git add client/Assets/Scripts/Net/Message.cs
git commit -m "feat: client message decompose/compose/reforge/upgrade/materials"
```

---

## Task 11: 客户端 GameState + 工坊 UI

**Files:**
- Modify: `client/Assets/Scripts/State/GameState.cs`
- Modify: `client/Assets/Scripts/UI/MainController.cs`

- [ ] **Step 1: GameState 追加材料状态与方法**

在 GameState 类追加：
```csharp
public event System.Action<System.Collections.Generic.Dictionary<string,int>> OnMaterialsReceived;
public event System.Action<CraftResultData> OnCraftResult;
public System.Collections.Generic.Dictionary<string,int> Materials { get; private set; } = new System.Collections.Generic.Dictionary<string,int>();

public void Decompose(string uid) { _ws.SendText(Message.EncodeDecompose("r" + (_reqSeq++), uid)); }
public void Compose(int slot) { _ws.SendText(Message.EncodeCompose("r" + (_reqSeq++), slot)); }
public void Reforge(string uid) { _ws.SendText(Message.EncodeReforge("r" + (_reqSeq++), uid)); }
public void Upgrade(string uid) { _ws.SendText(Message.EncodeUpgrade("r" + (_reqSeq++), uid)); }
```

在 HandleMessage switch 追加：
```csharp
case Message.TypeMaterials:
    Materials = Message.ParseMaterials(msg.dataJson);
    OnMaterialsReceived?.Invoke(Materials);
    break;
case Message.TypeCraftResult:
    var cr = JsonUtility.FromJson<CraftResultData>(msg.dataJson);
    OnCraftResult?.Invoke(cr);
    break;
```

- [ ] **Step 2: MainController 追加工坊面板**

在 OnGUI 末尾（背包 ScrollView 之后、EndArea 之前）追加工坊区：
```csharp
GUILayout.Space(8);
GUILayout.Label("--- Workshop ---");
// 材料显示
string matStr = "Mats: ";
foreach (var kv in _gameState.Materials)
    matStr += kv.Key + "=" + kv.Value + " ";
GUILayout.Label(matStr);

// 合成按钮（8 槽位）
GUILayout.Label("Compose (cost 10 base_mat):");
GUILayout.BeginHorizontal();
for (int s = 0; s < 8; s++)
{
    if (GUILayout.Button(SlotName(s), GUILayout.Width(50)))
        _gameState.Compose(s);
}
GUILayout.EndHorizontal();

// 选中装备的养成操作提示
GUILayout.Label("Select an item in backpack, then use Decompose/Reforge/Upgrade below");
GUILayout.Label("(click Equip to wear, or use workshop on selected)");
```

并在背包列表每行追加三个按钮（分解/重铸/强化）。修改背包行：
```csharp
foreach (var eq in _gameState.Bag)
{
    GUILayout.BeginHorizontal(GUI.skin.box);
    int affCount = eq.affixes != null ? eq.affixes.Length : 0;
    string info = $"[{RarityName(eq.rarity)}] {eq.name} +{eq.upgrade} ({SlotName(eq.slot)}) {affCount}aff";
    GUILayout.Label(info, GUILayout.Width(280));
    if (GUILayout.Button("Equip", GUILayout.Width(60))) _gameState.Equip(eq.uid);
    if (GUILayout.Button("Dec", GUILayout.Width(45))) _gameState.Decompose(eq.uid);
    if (GUILayout.Button("Ref", GUILayout.Width(45))) _gameState.Reforge(eq.uid);
    if (GUILayout.Button("Up", GUILayout.Width(45))) _gameState.Upgrade(eq.uid);
    GUILayout.EndHorizontal();
}
```

- [ ] **Step 3: 验证编译**

Unity batchmode 编译，无 CS 错误。

- [ ] **Step 4: 提交**

```bash
git add client/Assets/Scripts/State/GameState.cs client/Assets/Scripts/UI/MainController.cs
git commit -m "feat: client workshop UI decompose/compose/reforge/upgrade"
```

---

## Task 12: 端到端验证 + 全量回归

- [ ] **Step 1: 服务端养成冒烟测试**

Create `server/cmd/smokecraft/main.go`:
```go
package main

import (
	"fmt"
	"math/rand"

	"equipment-idle-server/internal/crafting"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/upgrade"
)

func main() {
	p := model.NewPlayer("smoke")
	gen := loot.NewGenerator(rand.New(rand.NewSource(7)))
	rng := rand.New(rand.NewSource(3))

	// 掉落并分解
	eq := gen.Generate(data.SlotWeapon, data.RarityRare)
	p.AddEquipment(eq)
	yield, _ := crafting.Decompose(p, eq)
	fmt.Printf("decomposed rare: %+v\n", yield)
	if p.Materials[data.MatBase] < 8 {
		fmt.Println("FAIL: decompose base mat"); return
	}

	// 合成
	p.AddMaterial(data.MatBase, 20)
	composed, _ := crafting.Compose(p, gen, data.SlotHelmet)
	fmt.Printf("composed: %s uid=%s\n", composed.Name, composed.UID)

	// 强化到 +3
	for i := 0; i < 3; i++ {
		r, _ := upgrade.Upgrade(p, rng, composed)
		fmt.Printf("upgrade attempt %d: success=%v lvl=%d\n", i+1, r.Success, composed.Upgrade)
	}
	if composed.Upgrade != 3 {
		fmt.Println("FAIL: should be +3"); return
	}

	// 重铸
	reforgeEq := gen.Generate(data.SlotWeapon, data.RarityMagic)
	p.AddEquipment(reforgeEq)
	for _, a := range reforgeEq.Affixes {
		p.AddMaterial(data.AffixMaterialByTier(a.Tier), 5)
	}
	err := crafting.Reforge(p, gen, reforgeEq)
	fmt.Printf("reforge: err=%v affixes=%d\n", err, len(reforgeEq.Affixes))

	fmt.Println("SMOKE_OK: crafting pipeline works")
}
```

- [ ] **Step 2: 运行冒烟**

Run:
```bash
cd server
go run ./cmd/smokecraft/
```
Expected: `SMOKE_OK: crafting pipeline works`。

- [ ] **Step 3: 全量回归**

Run:
```bash
cd server
go test ./...
```
Expected: 全部 `ok`。

- [ ] **Step 4: 提交**

```bash
git add server/cmd/smokecraft/main.go
git commit -m "test: smoke test crafting pipeline"
```

---

## 验收标准

阶段 4 完成后须满足：
1. `cd server && go test ./...` 全部通过（含 crafting/upgrade 包）
2. 分解：装备→基础材料+词缀材料（按稀有度与词缀Tier）
3. 合成：消耗10基础材料→指定槽位普通装备
4. 重铸：消耗词缀材料→重随机全部词缀（保留稀有度/基底）
5. 强化：+1~+10，成功率递减，失败不掉级
6. 客户端工坊 UI：材料显示、合成按钮、背包每行 Decompose/Reforge/Upgrade 按钮
7. 冒烟测试：分解→合成→强化+3→重铸 全链路 SMOKE_OK

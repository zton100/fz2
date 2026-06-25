# 阶段 1：装备生成核心 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现服务端装备生成核心：115 词缀条目数据、装备基底、稀有度框架、`loot` 包（RNG + 词缀抽取 + 数值生成）、`combat` 包（战力公式），全部 TDD 覆盖。

**Architecture:** 静态数据（词缀池/装备基底/稀有度）用 Go 原生数据结构定义在 `internal/data` 包，便于阶段 7 热调。`loot` 包负责按稀有度抽取词缀并生成数值，`combat` 包负责从装备词缀聚合属性并计算战力。所有逻辑纯函数化、可单测，不依赖 WebSocket。

**Tech Stack:** Go 1.26、标准库 `math/rand`。

**前置条件：** 阶段 0 服务端已就绪（`server/` 目录、`go.mod`）。

---

## 文件结构

本阶段产出文件及其职责：

**静态数据**
- `server/internal/data/rarity.go` — 稀有度枚举与词缀数量规则
- `server/internal/data/slot.go` — 装备槽位枚举（8 槽）
- `server/internal/data/affix_pool.go` — 115 词缀条目定义（23 类型 × 5 Tier）
- `server/internal/data/item_base.go` — 装备基底定义（武器/头盔/护甲等 8 类基底）

**领域模型**
- `server/internal/model/equipment.go` — 装备实例模型（基底+稀有度+词缀实例+强化等级）

**生成系统**
- `server/internal/loot/generator.go` — 装备生成（RNG + 稀有度抽取 + 词缀抽取 + 数值生成）
- `server/internal/loot/generator_test.go` — 生成逻辑单测

**战斗系统**
- `server/internal/combat/stats.go` — 从装备词缀聚合属性为 Stats 结构
- `server/internal/combat/power.go` — 战力公式
- `server/internal/combat/combat_test.go` — 聚合与战力单测

---

## Task 1: 稀有度与槽位数据

**Files:**
- Create: `server/internal/data/rarity.go`
- Create: `server/internal/data/slot.go`

- [ ] **Step 1: 写稀有度枚举与词缀数量规则**

Create `server/internal/data/rarity.go`:
```go
package data

// Rarity 稀有度枚举。
type Rarity int

const (
	RarityCommon  Rarity = iota // 普通 白
	RarityMagic                 // 魔法 蓝
	RarityRare                  // 稀有 黄
	RarityLegendary             // 传奇 橙
	RarityArtifact              // 神器 红
)

// RarityName 稀有度中文名（用于展示/日志）。
var RarityName = map[Rarity]string{
	RarityCommon:   "普通",
	RarityMagic:    "魔法",
	RarityRare:     "稀有",
	RarityLegendary: "传奇",
	RarityArtifact: "神器",
}

// AffixCount 一个稀有度的前缀/后缀数量规则。
type AffixCount struct {
	Prefix int // 前缀数
	Suffix int // 后缀数
}

// RarityAffixCount 各稀有度的词缀数量。
var RarityAffixCount = map[Rarity]AffixCount{
	RarityCommon:   {Prefix: 0, Suffix: 0},
	RarityMagic:    {Prefix: 1, Suffix: 1},
	RarityRare:     {Prefix: 2, Suffix: 2},
	RarityLegendary: {Prefix: 1, Suffix: 2}, // 1 固定 + 2 随机后缀
	RarityArtifact: {Prefix: 2, Suffix: 2},
}

// AllRarities 按从低到高返回所有稀有度，用于掉落权重。
func AllRarities() []Rarity {
	return []Rarity{RarityCommon, RarityMagic, RarityRare, RarityLegendary, RarityArtifact}
}
```

- [ ] **Step 2: 写槽位枚举**

Create `server/internal/data/slot.go`:
```go
package data

// Slot 装备槽位枚举。
type Slot int

const (
	SlotWeapon    Slot = iota // 武器
	SlotHelmet                // 头盔
	SlotArmor                 // 护甲
	SlotGloves                // 手套
	SlotBoots                 // 靴子
	SlotRing1                 // 戒指1
	SlotRing2                 // 戒指2
	SlotAmulet                // 项链
)

// SlotName 槽位中文名。
var SlotName = map[Slot]string{
	SlotWeapon: "武器",
	SlotHelmet: "头盔",
	SlotArmor:  "护甲",
	SlotGloves: "手套",
	SlotBoots:  "靴子",
	SlotRing1:  "戒指1",
	SlotRing2:  "戒指2",
	SlotAmulet: "项链",
}

// AllSlots 返回全部 8 个槽位。
func AllSlots() []Slot {
	return []Slot{SlotWeapon, SlotHelmet, SlotArmor, SlotGloves, SlotBoots, SlotRing1, SlotRing2, SlotAmulet}
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
git add server/internal/data/rarity.go server/internal/data/slot.go
git commit -m "feat: rarity and slot data definitions"
```

---

## Task 2: 词缀池数据（115 条目）

**Files:**
- Create: `server/internal/data/affix_pool.go`
- Create: `server/internal/data/affix_pool_test.go`

词缀结构：每个词缀有类型、Tier、数值范围。23 类型 × 5 Tier = 115 条目。

- [ ] **Step 1: 写词缀定义结构**

Create `server/internal/data/affix_pool.go`:
```go
package data

// AffixCategory 词缀大类。
type AffixCategory int

const (
	AffixBasic AffixCategory = iota // 基础属性
	AffixDerived                    // 衍生属性
	AffixSpecial                    // 经济/特殊
)

// AffixPosition 词缀位置：前缀或后缀。
type AffixPosition int

const (
	PosPrefix AffixPosition = iota
	PosSuffix
)

// AffixType 词缀类型标识（如"力量""暴击率"）。
type AffixType string

const (
	// 基础属性（6）
	ATStrength AffixType = "strength"
	ATAgility  AffixType = "agility"
	ATIntellect AffixType = "intellect"
	ATVitality AffixType = "vitality"
	ATMaxHP    AffixType = "max_hp"
	ATArmor    AffixType = "armor"
	// 衍生属性（9）
	ATCritRate    AffixType = "crit_rate"
	ATCritDamage AffixType = "crit_damage"
	ATAttackSpeed AffixType = "attack_speed"
	ATLifesteal   AffixType = "lifesteal"
	ATFireDmg     AffixType = "fire_dmg"
	ATColdDmg     AffixType = "cold_dmg"
	ATLightningDmg AffixType = "lightning_dmg"
	ATAccuracy    AffixType = "accuracy"
	ATEvasion     AffixType = "evasion"
	// 经济/特殊（8）
	ATDropRate    AffixType = "drop_rate"
	ATExpBonus    AffixType = "exp_bonus"
	ATKillHeal    AffixType = "kill_heal"
	ATMoveSpeed   AffixType = "move_speed"
	ATCooldownRed AffixType = "cooldown_red"
	ATReflect     AffixType = "reflect"
	ATShield      AffixType = "shield"
	ATResourceGain AffixType = "resource_gain"
)

// AffixDef 单个词缀条目定义（一个类型在某个 Tier 的具体档位）。
type AffixDef struct {
	Type     AffixType
	Category AffixCategory
	Position AffixPosition
	Tier     int       // 1~5
	Min      float64   // 数值下限
	Max      float64   // 数值上限
}

// tierRanges 按 AffixType 给出 5 档 (Min,Max)。
// 数值为相对量；百分比类词缀用 0~1 区间，固定值类用整数区间。
var tierRanges = map[AffixType][5][2]float64{
	ATStrength:    {{1, 3}, {4, 7}, {8, 12}, {13, 20}, {21, 35}},
	ATAgility:     {{1, 3}, {4, 7}, {8, 12}, {13, 20}, {21, 35}},
	ATIntellect:   {{1, 3}, {4, 7}, {8, 12}, {13, 20}, {21, 35}},
	ATVitality:    {{1, 3}, {4, 7}, {8, 12}, {13, 20}, {21, 35}},
	ATMaxHP:       {{5, 15}, {16, 35}, {36, 60}, {61, 100}, {101, 180}},
	ATArmor:       {{2, 6}, {7, 15}, {16, 28}, {29, 50}, {51, 90}},
	ATCritRate:    {{0.01, 0.02}, {0.02, 0.04}, {0.04, 0.07}, {0.07, 0.12}, {0.12, 0.20}},
	ATCritDamage:  {{0.05, 0.10}, {0.10, 0.20}, {0.20, 0.35}, {0.35, 0.55}, {0.55, 0.90}},
	ATAttackSpeed: {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATLifesteal:   {{0.005, 0.01}, {0.01, 0.02}, {0.02, 0.03}, {0.03, 0.05}, {0.05, 0.08}},
	ATFireDmg:     {{2, 6}, {7, 15}, {16, 28}, {29, 50}, {51, 90}},
	ATColdDmg:     {{2, 6}, {7, 15}, {16, 28}, {29, 50}, {51, 90}},
	ATLightningDmg:{{2, 6}, {7, 15}, {16, 28}, {29, 50}, {51, 90}},
	ATAccuracy:    {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATEvasion:     {{0.01, 0.02}, {0.02, 0.04}, {0.04, 0.07}, {0.07, 0.12}, {0.12, 0.20}},
	ATDropRate:    {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATExpBonus:    {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATKillHeal:    {{1, 3}, {4, 7}, {8, 12}, {13, 20}, {21, 35}},
	ATMoveSpeed:   {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATCooldownRed: {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATReflect:     {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATShield:      {{5, 15}, {16, 35}, {36, 60}, {61, 100}, {101, 180}},
	ATResourceGain:{{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
}

// affixMeta 词缀元信息：大类与默认位置。
var affixMeta = map[AffixType]struct {
	Cat AffixCategory
	Pos AffixPosition
}{
	ATStrength:     {AffixBasic, PosPrefix},
	ATAgility:      {AffixBasic, PosPrefix},
	ATIntellect:    {AffixBasic, PosPrefix},
	ATVitality:     {AffixBasic, PosPrefix},
	ATMaxHP:        {AffixBasic, PosSuffix},
	ATArmor:        {AffixBasic, PosSuffix},
	ATCritRate:     {AffixDerived, PosSuffix},
	ATCritDamage:   {AffixDerived, PosSuffix},
	ATAttackSpeed:  {AffixDerived, PosPrefix},
	ATLifesteal:    {AffixDerived, PosSuffix},
	ATFireDmg:      {AffixDerived, PosPrefix},
	ATColdDmg:      {AffixDerived, PosPrefix},
	ATLightningDmg: {AffixDerived, PosPrefix},
	ATAccuracy:     {AffixDerived, PosSuffix},
	ATEvasion:      {AffixDerived, PosSuffix},
	ATDropRate:     {AffixSpecial, PosSuffix},
	ATExpBonus:     {AffixSpecial, PosSuffix},
	ATKillHeal:     {AffixSpecial, PosSuffix},
	ATMoveSpeed:    {AffixSpecial, PosPrefix},
	ATCooldownRed:  {AffixSpecial, PosSuffix},
	ATReflect:      {AffixSpecial, PosSuffix},
	ATShield:       {AffixSpecial, PosSuffix},
	ATResourceGain: {AffixSpecial, PosSuffix},
}

// AllAffixTypes 返回全部 23 个词缀类型。
func AllAffixTypes() []AffixType {
	return []AffixType{
		ATStrength, ATAgility, ATIntellect, ATVitality, ATMaxHP, ATArmor,
		ATCritRate, ATCritDamage, ATAttackSpeed, ATLifesteal, ATFireDmg, ATColdDmg, ATLightningDmg, ATAccuracy, ATEvasion,
		ATDropRate, ATExpBonus, ATKillHeal, ATMoveSpeed, ATCooldownRed, ATReflect, ATShield, ATResourceGain,
	}
}

// BuildAffixPool 构建完整词缀池：23 类型 × 5 Tier = 115 条目。
func BuildAffixPool() []AffixDef {
	types := AllAffixTypes()
	pool := make([]AffixDef, 0, len(types)*5)
	for _, t := range types {
		meta := affixMeta[t]
		ranges := tierRanges[t]
		for tier := 1; tier <= 5; tier++ {
			pool = append(pool, AffixDef{
				Type:     t,
				Category: meta.Cat,
				Position: meta.Pos,
				Tier:     tier,
				Min:      ranges[tier-1][0],
				Max:      ranges[tier-1][1],
			})
		}
	}
	return pool
}

// AffixesByPosition 从词缀池筛选指定位置的词缀。
func AffixesByPosition(pool []AffixDef, pos AffixPosition) []AffixDef {
	var out []AffixDef
	for _, a := range pool {
		if a.Position == pos {
			out = append(out, a)
		}
	}
	return out
}
```

- [ ] **Step 2: 写词缀池测试**

Create `server/internal/data/affix_pool_test.go`:
```go
package data

import "testing"

func TestBuildAffixPool_Count115(t *testing.T) {
	pool := BuildAffixPool()
	if len(pool) != 115 {
		t.Fatalf("pool size = %d, want 115", len(pool))
	}
}

func TestBuildAffixPool_TierRangeValid(t *testing.T) {
	pool := BuildAffixPool()
	for _, a := range pool {
		if a.Tier < 1 || a.Tier > 5 {
			t.Fatalf("affix %s tier = %d, want 1..5", a.Type, a.Tier)
		}
		if a.Min > a.Max {
			t.Fatalf("affix %s tier %d min %.4f > max %.4f", a.Type, a.Tier, a.Min, a.Max)
		}
	}
}

func TestAffixesByPosition(t *testing.T) {
	pool := BuildAffixPool()
	prefixes := AffixesByPosition(pool, PosPrefix)
	suffixes := AffixesByPosition(pool, PosSuffix)
	if len(prefixes)+len(suffixes) != 115 {
		t.Fatalf("prefix %d + suffix %d != 115", len(prefixes), len(suffixes))
	}
	for _, a := range prefixes {
		if a.Position != PosPrefix {
			t.Fatal("prefix filter returned non-prefix affix")
		}
	}
}
```

- [ ] **Step 3: 运行测试**

Run:
```bash
cd server
go test ./internal/data/...
```
Expected: `ok equipment-idle-server/internal/data`。

- [ ] **Step 4: 提交**

```bash
git add server/internal/data/affix_pool.go server/internal/data/affix_pool_test.go
git commit -m "feat: affix pool with 115 entries (23 types x 5 tiers)"
```

---

## Task 3: 装备基底数据

**Files:**
- Create: `server/internal/data/item_base.go`
- Create: `server/internal/data/item_base_test.go`

每类槽位对应一个基底，基底定义基础属性（无词缀时的白板属性）。

- [ ] **Step 1: 写装备基底定义**

Create `server/internal/data/item_base.go`:
```go
package data

// ItemBase 装备基底：某槽位的白板装备定义。
type ItemBase struct {
	ID       string        // 基底 ID
	Name     string        // 中文名
	Slot     Slot          // 槽位
	BaseStats map[AffixType]float64 // 白板基础属性（无词缀时）
}

// allBases 8 个槽位对应的基底。
var allBases = []ItemBase{
	{ID: "base_weapon", Name: "铁剑", Slot: SlotWeapon, BaseStats: map[AffixType]float64{
		ATStrength: 5, ATAttackSpeed: 0.1}},
	{ID: "base_helmet", Name: "皮盔", Slot: SlotHelmet, BaseStats: map[AffixType]float64{
		ATArmor: 8, ATVitality: 3}},
	{ID: "base_armor", Name: "锁甲", Slot: SlotArmor, BaseStats: map[AffixType]float64{
		ATArmor: 15, ATMaxHP: 20}},
	{ID: "base_gloves", Name: "皮手套", Slot: SlotGloves, BaseStats: map[AffixType]float64{
		ATAgility: 4, ATAttackSpeed: 0.05}},
	{ID: "base_boots", Name: "皮靴", Slot: SlotBoots, BaseStats: map[AffixType]float64{
		ATMoveSpeed: 0.1, ATEvasion: 0.03}},
	{ID: "base_ring", Name: "铜戒", Slot: SlotRing1, BaseStats: map[AffixType]float64{
		ATIntellect: 5}},
	{ID: "base_ring2", Name: "铜戒", Slot: SlotRing2, BaseStats: map[AffixType]float64{
		ATIntellect: 5}},
	{ID: "base_amulet", Name: "护身符", Slot: SlotAmulet, BaseStats: map[AffixType]float64{
		ATVitality: 5, ATMaxHP: 10}},
}

// AllItemBases 返回全部 8 个基底。
func AllItemBases() []ItemBase {
	return allBases
}

// BaseBySlot 按槽位取基底（戒指1/2 共用 base_ring 定义，ring2 用 base_ring2）。
func BaseBySlot(slot Slot) ItemBase {
	for _, b := range allBases {
		if b.Slot == slot {
			return b
		}
	}
	return allBases[0]
}
```

- [ ] **Step 2: 写基底测试**

Create `server/internal/data/item_base_test.go`:
```go
package data

import "testing"

func TestAllItemBases_Count8(t *testing.T) {
	bases := AllItemBases()
	if len(bases) != 8 {
		t.Fatalf("bases count = %d, want 8", len(bases))
	}
	seen := map[Slot]bool{}
	for _, b := range bases {
		if seen[b.Slot] {
			t.Fatalf("duplicate slot %d", b.Slot)
		}
		seen[b.Slot] = true
		if len(b.BaseStats) == 0 {
			t.Fatalf("base %s has empty BaseStats", b.ID)
		}
	}
}

func TestBaseBySlot(t *testing.T) {
	b := BaseBySlot(SlotWeapon)
	if b.Slot != SlotWeapon {
		t.Fatalf("got slot %d, want weapon", b.Slot)
	}
	if b.ID != "base_weapon" {
		t.Fatalf("got id %s, want base_weapon", b.ID)
	}
}
```

- [ ] **Step 3: 运行测试**

Run:
```bash
cd server
go test ./internal/data/...
```
Expected: `ok equipment-idle-server/internal/data`。

- [ ] **Step 4: 提交**

```bash
git add server/internal/data/item_base.go server/internal/data/item_base_test.go
git commit -m "feat: item base definitions for 8 slots"
```

---

## Task 4: 装备实例模型

**Files:**
- Create: `server/internal/model/equipment.go`

- [ ] **Step 1: 写装备实例模型**

Create `server/internal/model/equipment.go`:
```go
package model

import "equipment-idle-server/internal/data"

// AffixInstance 词缀实例：词缀定义 + 随机生成的具体数值。
type AffixInstance struct {
	Type  data.AffixType
	Tier  int
	Value float64
}

// Equipment 装备实例：基底 + 稀有度 + 词缀实例列表 + 强化等级。
type Equipment struct {
	UID       string               // 全局唯一 ID（生成时分配）
	BaseID    string               // 基底 ID
	Name      string               // 显示名（基底名，稀有度前缀在展示层加）
	Slot      data.Slot            // 槽位
	Rarity    data.Rarity          // 稀有度
	Affixes   []AffixInstance      // 生成的词缀实例
	BaseStats map[data.AffixType]float64 // 白板基础属性（复制自基底）
	Upgrade   int                  // 强化等级 0~10
}

// AllStats 聚合白板基础属性 + 词缀数值，返回每个 AffixType 的总值。
func (e *Equipment) AllStats() map[data.AffixType]float64 {
	out := make(map[data.AffixType]float64)
	for k, v := range e.BaseStats {
		out[k] += v
	}
	for _, a := range e.Affixes {
		out[a.Type] += a.Value
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
git add server/internal/model/equipment.go
git commit -m "feat: equipment instance model with stat aggregation"
```

---

## Task 5: 装备生成器（loot 包）

**Files:**
- Create: `server/internal/loot/generator.go`
- Create: `server/internal/loot/generator_test.go`

生成器职责：给定槽位、稀有度、随机源 → 生成 Equipment 实例（含词缀抽取与数值生成）。

- [ ] **Step 1: 写生成器（失败测试先行）**

Create `server/internal/loot/generator_test.go`:
```go
package loot

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/data"
)

func TestGenerate_NormalRarity_NoAffixes(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(1)))
	eq := g.Generate(data.SlotWeapon, data.RarityCommon)
	if eq.Slot != data.SlotWeapon {
		t.Fatalf("slot = %d, want weapon", eq.Slot)
	}
	if eq.Rarity != data.RarityCommon {
		t.Fatalf("rarity = %d, want common", eq.Rarity)
	}
	if len(eq.Affixes) != 0 {
		t.Fatalf("common affix count = %d, want 0", len(eq.Affixes))
	}
	if eq.Upgrade != 0 {
		t.Fatalf("upgrade = %d, want 0", eq.Upgrade)
	}
	if eq.UID == "" {
		t.Fatal("UID should not be empty")
	}
}

func TestGenerate_MagicRarity_TwoAffixes(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(42)))
	eq := g.Generate(data.SlotHelmet, data.RarityMagic)
	// 魔法：1 前缀 + 1 后缀 = 2
	if len(eq.Affixes) != 2 {
		t.Fatalf("magic affix count = %d, want 2", len(eq.Affixes))
	}
	// 检查前缀后缀各一
	var prefixes, suffixes int
	for _, a := range eq.Affixes {
		pool := data.BuildAffixPool()
		for _, def := range pool {
			if def.Type == a.Type && def.Tier == a.Tier {
				if def.Position == data.PosPrefix {
					prefixes++
				} else {
					suffixes++
				}
				if a.Value < def.Min || a.Value > def.Max {
					t.Fatalf("affix %s value %.4f out of [%.4f,%.4f]", a.Type, a.Value, def.Min, def.Max)
				}
			}
		}
	}
	if prefixes != 1 || suffixes != 1 {
		t.Fatalf("prefixes=%d suffixes=%d, want 1/1", prefixes, suffixes)
	}
}

func TestGenerate_RareRarity_FourAffixes(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(7)))
	eq := g.Generate(data.SlotArmor, data.RarityRare)
	// 稀有：2 前缀 + 2 后缀 = 4
	if len(eq.Affixes) != 4 {
		t.Fatalf("rare affix count = %d, want 4", len(eq.Affixes))
	}
}

func TestGenerate_AffixValueWithinRange(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(100)))
	pool := data.BuildAffixPool()
	for i := 0; i < 50; i++ {
		eq := g.Generate(data.SlotWeapon, data.RarityRare)
		for _, a := range eq.Affixes {
			var def *data.AffixDef
			for j := range pool {
				if pool[j].Type == a.Type && pool[j].Tier == a.Tier {
					def = &pool[j]
					break
				}
			}
			if def == nil {
				t.Fatalf("affix %s tier %d not found in pool", a.Type, a.Tier)
			}
			if a.Value < def.Min || a.Value > def.Max {
				t.Fatalf("affix %s value %.4f out of [%.4f,%.4f]", a.Type, a.Value, def.Min, def.Max)
			}
		}
	}
}

func TestGenerate_UIDUnique(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(1)))
	seen := map[string]bool{}
	for i := 0; i < 100; i++ {
		eq := g.Generate(data.SlotWeapon, data.RarityCommon)
		if seen[eq.UID] {
			t.Fatal("duplicate UID generated")
		}
		seen[eq.UID] = true
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/loot/...
```
Expected: 编译失败，`NewGenerator`/`Generate` 未定义。

- [ ] **Step 3: 写生成器实现**

Create `server/internal/loot/generator.go`:
```go
package loot

import (
	"fmt"
	"math/rand"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// Generator 装备生成器。注入 *rand.Rand 便于测试确定性。
type Generator struct {
	rng    *rand.Rand
	pool   []data.AffixDef
	uidSeq int
}

// NewGenerator 创建生成器。
func NewGenerator(rng *rand.Rand) *Generator {
	return &Generator{
		rng:  rng,
		pool: data.BuildAffixPool(),
	}
}

// Generate 按槽位与稀有度生成一件装备实例。
func (g *Generator) Generate(slot data.Slot, rarity data.Rarity) *model.Equipment {
	base := data.BaseBySlot(slot)
	eq := &model.Equipment{
		UID:       g.nextUID(),
		BaseID:    base.ID,
		Name:      base.Name,
		Slot:      slot,
		Rarity:    rarity,
		BaseStats: copyStats(base.BaseStats),
		Upgrade:   0,
	}
	rule := data.RarityAffixCount[rarity]
	prefixes := data.AffixesByPosition(g.pool, data.PosPrefix)
	suffixes := data.AffixesByPosition(g.pool, data.PosSuffix)

	// 抽取前缀（不重复类型）
	chosenTypes := map[data.AffixType]bool{}
	for i := 0; i < rule.Prefix && len(prefixes) > 0; i++ {
		a := g.pickAffix(prefixes, chosenTypes)
		if a != nil {
			eq.Affixes = append(eq.Affixes, g.rollValue(a))
			chosenTypes[a.Type] = true
		}
	}
	for i := 0; i < rule.Suffix && len(suffixes) > 0; i++ {
		a := g.pickAffix(suffixes, chosenTypes)
		if a != nil {
			eq.Affixes = append(eq.Affixes, g.rollValue(a))
			chosenTypes[a.Type] = true
		}
	}
	return eq
}

// pickAffix 从候选词缀中随机选一个（避开已选类型），返回 AffixDef 指针。
func (g *Generator) pickAffix(candidates []data.AffixDef, chosen map[data.AffixType]bool) *data.AffixDef {
	// 过滤已选类型
	var avail []data.AffixDef
	for _, c := range candidates {
		if !chosen[c.Type] {
			avail = append(avail, c)
		}
	}
	if len(avail) == 0 {
		return nil
	}
	idx := g.rng.Intn(len(avail))
	availCopy := avail[idx]
	return &availCopy
}

// rollValue 在词缀 Min~Max 间随机生成数值。
func (g *Generator) rollValue(def *data.AffixDef) model.AffixInstance {
	val := def.Min + g.rng.Float64()*(def.Max-def.Min)
	return model.AffixInstance{
		Type:  def.Type,
		Tier:  def.Tier,
		Value: val,
	}
}

// nextUID 生成全局唯一 ID。
func (g *Generator) nextUID() string {
	g.uidSeq++
	return fmt.Sprintf("eq_%d", g.uidSeq)
}

// copyStats 复制基础属性 map，避免共享底层数据。
func copyStats(src map[data.AffixType]float64) map[data.AffixType]float64 {
	dst := make(map[data.AffixType]float64, len(src))
	for k, v := range src {
		dst[k] = v
	}
	return dst
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
git add server/internal/loot/
git commit -m "feat: loot generator with rarity-based affix rolling"
```

---

## Task 6: 战斗属性聚合（combat 包）

**Files:**
- Create: `server/internal/combat/stats.go`
- Create: `server/internal/combat/combat_test.go`

从多件装备的 AllStats() 聚合为玩家总 Stats，用于战力计算。

- [ ] **Step 1: 写 Stats 结构与聚合函数（失败测试先行）**

Create `server/internal/combat/combat_test.go`:
```go
package combat

import (
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestAggregateStats_SumsAcrossEquipment(t *testing.T) {
	eqs := []*model.Equipment{
		{BaseStats: map[data.AffixType]float64{data.ATStrength: 5}, Affixes: []model.AffixInstance{
			{Type: data.ATStrength, Value: 10}}},
		{BaseStats: map[data.AffixType]float64{data.ATStrength: 3}, Affixes: []model.AffixInstance{
			{Type: data.ATCritRate, Value: 0.05}}},
	}
	stats := AggregateStats(eqs)
	if stats[data.ATStrength] != 18 { // 5+10+3
		t.Fatalf("strength = %.2f, want 18", stats[data.ATStrength])
	}
	if stats[data.ATCritRate] != 0.05 {
		t.Fatalf("crit_rate = %.4f, want 0.05", stats[data.ATCritRate])
	}
}

func TestAggregateStats_Empty(t *testing.T) {
	stats := AggregateStats(nil)
	if len(stats) != 0 {
		t.Fatalf("empty stats len = %d, want 0", len(stats))
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/combat/...
```
Expected: 编译失败，`AggregateStats` 未定义。

- [ ] **Step 3: 写聚合实现**

Create `server/internal/combat/stats.go`:
```go
package combat

import (
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// Stats 玩家总属性：AffixType -> 总值。
type Stats map[data.AffixType]float64

// AggregateStats 把多件装备的 AllStats 聚合为玩家总属性。
func AggregateStats(eqs []*model.Equipment) Stats {
	out := Stats{}
	for _, eq := range eqs {
		if eq == nil {
			continue
		}
		for k, v := range eq.AllStats() {
			out[k] += v
		}
	}
	return out
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
git add server/internal/combat/
git commit -m "feat: combat stat aggregation from equipment"
```

---

## Task 7: 战力公式

**Files:**
- Modify: `server/internal/combat/power.go`（新建）
- Modify: `server/internal/combat/combat_test.go`（追加测试）

战力公式：`战力 = 攻击 × 攻速 × (1 + 暴击期望) × 生存系数`
- 攻击 = 力量 + 火伤 + 冰伤 + 雷伤（基础+元素攻击）
- 攻速 = 1 + attack_speed
- 暴击期望 = crit_rate × crit_damage
- 生存系数 = (1 + 生命/100) × (1 + 护甲/100)

- [ ] **Step 1: 追加战力测试**

在 `server/internal/combat/combat_test.go` 末尾追加：
```go
func TestComputePower_BasicCase(t *testing.T) {
	stats := Stats{
		data.ATStrength:    20, // 攻击=20
		data.ATAttackSpeed: 0.0, // 攻速=1.0
		data.ATCritRate:    0.0,
		data.ATCritDamage:  0.0,
		data.ATMaxHP:       0,
		data.ATArmor:       0,
	}
	p := ComputePower(stats)
	// 攻击20 × 攻速1 × (1+0) × (1+0)×(1+0) = 20
	if p != 20 {
		t.Fatalf("power = %.2f, want 20", p)
	}
}

func TestComputePower_WithCritAndSpeed(t *testing.T) {
	stats := Stats{
		data.ATStrength:    10, // 攻击=10
		data.ATAttackSpeed: 0.5, // 攻速=1.5
		data.ATCritRate:    0.5,
		data.ATCritDamage:  1.0, // 暴击期望=0.5
		data.ATMaxHP:       100, // 生存系数=(1+1)=2
		data.ATArmor:       0,
	}
	p := ComputePower(stats)
	// 10 × 1.5 × (1+0.5) × 2 × (1+0) = 45
	if p != 45 {
		t.Fatalf("power = %.2f, want 45", p)
	}
}

func TestComputePower_ElementalDamageAddsAttack(t *testing.T) {
	stats := Stats{
		data.ATStrength:    10,
		data.ATFireDmg:     5,
		data.ATColdDmg:     5,
		data.ATLightningDmg: 5, // 攻击=10+5+5+5=25
		data.ATAttackSpeed: 0.0,
		data.ATCritRate:    0.0,
		data.ATCritDamage:  0.0,
		data.ATMaxHP:       0,
		data.ATArmor:       0,
	}
	p := ComputePower(stats)
	if p != 25 {
		t.Fatalf("power = %.2f, want 25", p)
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/combat/...
```
Expected: 编译失败，`ComputePower` 未定义。

- [ ] **Step 3: 写战力公式实现**

Create `server/internal/combat/power.go`:
```go
package combat

import "equipment-idle-server/internal/data"

// ComputePower 战力公式：
//   战力 = 攻击 × 攻速 × (1 + 暴击期望) × 生存系数
//   攻击 = 力量 + 火伤 + 冰伤 + 雷伤
//   攻速 = 1 + attack_speed
//   暴击期望 = crit_rate × crit_damage
//   生存系数 = (1 + 生命/100) × (1 + 护甲/100)
func ComputePower(s Stats) float64 {
	attack := s[data.ATStrength] + s[data.ATFireDmg] + s[data.ATColdDmg] + s[data.ATLightningDmg]
	attackSpeed := 1.0 + s[data.ATAttackSpeed]
	critExpectation := s[data.ATCritRate] * s[data.ATCritDamage]
	survival := (1.0 + s[data.ATMaxHP]/100.0) * (1.0 + s[data.ATArmor]/100.0)
	return attack * attackSpeed * (1.0 + critExpectation) * survival
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
git add server/internal/combat/
git commit -m "feat: combat power formula with attack/crit/survival"
```

---

## Task 8: 集成冒烟测试与全量回归

**Files:**
- Create: `server/internal/loot/integration_test.go`

验证"生成装备 → 聚合属性 → 计算战力"完整链路。

- [ ] **Step 1: 写集成测试**

Create `server/internal/loot/integration_test.go`:
```go
package loot

import (
	"math/rand"
	"testing"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestIntegration_GenerateAggregatePower(t *testing.T) {
	g := NewGenerator(rand.New(rand.NewSource(999)))
	eqs := []*model.Equipment{}
	for _, slot := range data.AllSlots() {
		eq := g.Generate(slot, data.RarityRare)
		eqs = append(eqs, eq)
	}
	stats := combat.AggregateStats(eqs)
	power := combat.ComputePower(stats)

	if power <= 0 {
		t.Fatalf("power = %.2f, want > 0", power)
	}
	// 稀有装备 8 件，战力应有合理下限（白板攻击就 > 0）
	t.Logf("8-slot rare power = %.2f", power)
}
```

- [ ] **Step 2: 运行集成测试**

Run:
```bash
cd server
go test ./internal/loot/ -run TestIntegration -v
```
Expected: `--- PASS: TestIntegration_GenerateAggregatePower`，日志打印 power 值。

- [ ] **Step 3: 跑全量测试确认无回归**

Run:
```bash
cd server
go test ./...
```
Expected: 全部 `ok`（data/loot/combat/save/ws 都通过）。

- [ ] **Step 4: 提交**

```bash
git add server/internal/loot/integration_test.go
git commit -m "test: integration test for generate-aggregate-power pipeline"
```

---

## 验收标准

阶段 1 完成后须满足：
1. `cd server && go test ./...` 全部通过
2. `data.BuildAffixPool()` 返回 115 条目
3. `loot.Generator.Generate` 能按稀有度生成正确词缀数量（普通0/魔法2/稀有4/传奇3/神器4）
4. `combat.AggregateStats` + `combat.ComputePower` 能从装备算出战力
5. 集成测试验证完整链路：生成8槽装备 → 聚合 → 战力 > 0

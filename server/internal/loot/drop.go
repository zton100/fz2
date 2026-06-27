package loot

import (
	"math/rand"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// DropModifier 天赋加成参数，影响掉落稀有度。
type DropModifier struct {
	DropBonus    float64    // 高稀有度权重加成
	QualityFloor data.Rarity // 最低稀有度
}

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

// RollRarityWithBonus 按层数+天赋加成选稀有度。
// dropBonus: 提高高稀有度权重（直接给稀有/传奇/神器加权）。
// qualityFloor: 稀有度下限，低于下限的权重置零。
func (d *DropTable) RollRarityWithBonus(floor int, dropBonus float64, qualityFloor data.Rarity) data.Rarity {
	weights := rarityWeights(floor)
	rarities := data.AllRarities()
	// dropBonus 直接提升稀有/传奇/神器权重
	weights[2] += int(dropBonus * 100) // Rare
	weights[3] += int(dropBonus * 40)  // Legendary
	weights[4] += int(dropBonus * 20)  // Artifact
	total := 0
	for i := range weights {
		if rarities[i] < qualityFloor {
			weights[i] = 0
		}
		total += weights[i]
	}
	if total == 0 {
		return qualityFloor
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

// DropRandomSlotWithBonus 带天赋加成的随机槽位掉落（离线结算专用）。
func (d *DropTable) DropRandomSlotWithBonus(floor int, dropBonus float64, qualityFloor data.Rarity) *model.Equipment {
	slots := data.AllSlots()
	slot := slots[d.rng.Intn(len(slots))]
	rarity := d.RollRarityWithBonus(floor, dropBonus, qualityFloor)
	return d.gen.Generate(slot, rarity)
}

// DropRandomSlotModified 带 DropModifier 的随机槽位掉落（在线 Runner 使用）。
// drop/quality 天赋通过 DropModifier 影响掉落权重与稀有度下限。
func (d *DropTable) DropRandomSlotModified(floor int, mod DropModifier) *model.Equipment {
	slots := data.AllSlots()
	slot := slots[d.rng.Intn(len(slots))]
	rarity := d.RollRarityWithBonus(floor, mod.DropBonus, mod.QualityFloor)
	return d.gen.Generate(slot, rarity)
}

func max(a, b int) int {
	if a > b {
		return a
	}
	return b
}

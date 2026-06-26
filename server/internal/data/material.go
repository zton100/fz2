package data

// MaterialType 材料类型标识。
type MaterialType string

const (
	MatBase    MaterialType = "base_mat"    // 基础材料
	MatAffixT1 MaterialType = "affix_mat_1" // 词缀材料 Tier1
	MatAffixT2 MaterialType = "affix_mat_2"
	MatAffixT3 MaterialType = "affix_mat_3"
	MatAffixT4 MaterialType = "affix_mat_4"
	MatAffixT5 MaterialType = "affix_mat_5"
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

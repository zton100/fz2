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
	RarityCommon:    "普通",
	RarityMagic:     "魔法",
	RarityRare:      "稀有",
	RarityLegendary: "传奇",
	RarityArtifact:  "神器",
}

// AffixCount 一个稀有度的前缀/后缀数量规则。
type AffixCount struct {
	Prefix int // 前缀数
	Suffix int // 后缀数
}

// RarityAffixCount 各稀有度的词缀数量。
var RarityAffixCount = map[Rarity]AffixCount{
	RarityCommon:    {Prefix: 0, Suffix: 0},
	RarityMagic:     {Prefix: 1, Suffix: 1},
	RarityRare:      {Prefix: 2, Suffix: 2},
	RarityLegendary: {Prefix: 1, Suffix: 2}, // 1 固定 + 2 随机后缀
	RarityArtifact:  {Prefix: 2, Suffix: 2},
}

// AllRarities 按从低到高返回所有稀有度，用于掉落权重。
func AllRarities() []Rarity {
	return []Rarity{RarityCommon, RarityMagic, RarityRare, RarityLegendary, RarityArtifact}
}

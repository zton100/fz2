package data

import "equipment-idle-server/internal/locale"

// Rarity 稀有度枚举。
type Rarity int

const (
	RarityCommon   Rarity = iota // 普通 白
	RarityMagic                  // 魔法 蓝
	RarityRare                   // 稀有 黄
	RarityLegendary              // 传奇 橙
	RarityArtifact               // 神器 红
)

// RarityName returns the localized display name for a rarity.
func RarityName(r Rarity) string {
	l := locale.Current()
	switch r {
	case RarityCommon:
		return l.RarityCommon
	case RarityMagic:
		return l.RarityMagic
	case RarityRare:
		return l.RarityRare
	case RarityLegendary:
		return l.RarityLegendary
	case RarityArtifact:
		return l.RarityArtifact
	}
	return "?"
}

// AffixCount 一个稀有度的前缀/后缀数量规则。
type AffixCount struct {
	Prefix int
	Suffix int
}

// RarityAffixCount 各稀有度的词缀数量。
var RarityAffixCount = map[Rarity]AffixCount{
	RarityCommon:    {Prefix: 0, Suffix: 0},
	RarityMagic:     {Prefix: 1, Suffix: 1},
	RarityRare:      {Prefix: 2, Suffix: 2},
	RarityLegendary: {Prefix: 1, Suffix: 2},
	RarityArtifact:  {Prefix: 2, Suffix: 2},
}

// AllRarities 按从低到高返回所有稀有度，用于掉落权重。
func AllRarities() []Rarity {
	return []Rarity{RarityCommon, RarityMagic, RarityRare, RarityLegendary, RarityArtifact}
}

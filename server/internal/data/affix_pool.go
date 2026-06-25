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
	ATStrength     AffixType = "strength"
	ATAgility      AffixType = "agility"
	ATIntellect    AffixType = "intellect"
	ATVitality     AffixType = "vitality"
	ATMaxHP        AffixType = "max_hp"
	ATArmor        AffixType = "armor"
	// 衍生属性（9）
	ATCritRate     AffixType = "crit_rate"
	ATCritDamage   AffixType = "crit_damage"
	ATAttackSpeed  AffixType = "attack_speed"
	ATLifesteal    AffixType = "lifesteal"
	ATFireDmg      AffixType = "fire_dmg"
	ATColdDmg      AffixType = "cold_dmg"
	ATLightningDmg AffixType = "lightning_dmg"
	ATAccuracy     AffixType = "accuracy"
	ATEvasion      AffixType = "evasion"
	// 经济/特殊（8）
	ATDropRate     AffixType = "drop_rate"
	ATExpBonus     AffixType = "exp_bonus"
	ATKillHeal     AffixType = "kill_heal"
	ATMoveSpeed    AffixType = "move_speed"
	ATCooldownRed  AffixType = "cooldown_red"
	ATReflect      AffixType = "reflect"
	ATShield       AffixType = "shield"
	ATResourceGain AffixType = "resource_gain"
)

// AffixDef 单个词缀条目定义（一个类型在某个 Tier 的具体档位）。
type AffixDef struct {
	Type     AffixType
	Category AffixCategory
	Position AffixPosition
	Tier     int // 1~5
	Min      float64
	Max      float64
}

// tierRanges 按 AffixType 给出 5 档 (Min,Max)。
var tierRanges = map[AffixType][5][2]float64{
	ATStrength:      {{1, 3}, {4, 7}, {8, 12}, {13, 20}, {21, 35}},
	ATAgility:       {{1, 3}, {4, 7}, {8, 12}, {13, 20}, {21, 35}},
	ATIntellect:     {{1, 3}, {4, 7}, {8, 12}, {13, 20}, {21, 35}},
	ATVitality:      {{1, 3}, {4, 7}, {8, 12}, {13, 20}, {21, 35}},
	ATMaxHP:         {{5, 15}, {16, 35}, {36, 60}, {61, 100}, {101, 180}},
	ATArmor:         {{2, 6}, {7, 15}, {16, 28}, {29, 50}, {51, 90}},
	ATCritRate:      {{0.01, 0.02}, {0.02, 0.04}, {0.04, 0.07}, {0.07, 0.12}, {0.12, 0.20}},
	ATCritDamage:    {{0.05, 0.10}, {0.10, 0.20}, {0.20, 0.35}, {0.35, 0.55}, {0.55, 0.90}},
	ATAttackSpeed:   {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATLifesteal:     {{0.005, 0.01}, {0.01, 0.02}, {0.02, 0.03}, {0.03, 0.05}, {0.05, 0.08}},
	ATFireDmg:       {{2, 6}, {7, 15}, {16, 28}, {29, 50}, {51, 90}},
	ATColdDmg:       {{2, 6}, {7, 15}, {16, 28}, {29, 50}, {51, 90}},
	ATLightningDmg:  {{2, 6}, {7, 15}, {16, 28}, {29, 50}, {51, 90}},
	ATAccuracy:      {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATEvasion:       {{0.01, 0.02}, {0.02, 0.04}, {0.04, 0.07}, {0.07, 0.12}, {0.12, 0.20}},
	ATDropRate:      {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATExpBonus:      {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATKillHeal:      {{1, 3}, {4, 7}, {8, 12}, {13, 20}, {21, 35}},
	ATMoveSpeed:     {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATCooldownRed:   {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATReflect:       {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
	ATShield:        {{5, 15}, {16, 35}, {36, 60}, {61, 100}, {101, 180}},
	ATResourceGain:  {{0.02, 0.04}, {0.04, 0.07}, {0.07, 0.10}, {0.10, 0.15}, {0.15, 0.25}},
}

// affixMeta 词缀元信息：大类与默认位置。
var affixMeta = map[AffixType]struct {
	Cat AffixCategory
	Pos AffixPosition
}{
	ATStrength:      {AffixBasic, PosPrefix},
	ATAgility:       {AffixBasic, PosPrefix},
	ATIntellect:     {AffixBasic, PosPrefix},
	ATVitality:      {AffixBasic, PosPrefix},
	ATMaxHP:         {AffixBasic, PosSuffix},
	ATArmor:         {AffixBasic, PosSuffix},
	ATCritRate:      {AffixDerived, PosSuffix},
	ATCritDamage:    {AffixDerived, PosSuffix},
	ATAttackSpeed:   {AffixDerived, PosPrefix},
	ATLifesteal:     {AffixDerived, PosSuffix},
	ATFireDmg:       {AffixDerived, PosPrefix},
	ATColdDmg:       {AffixDerived, PosPrefix},
	ATLightningDmg:  {AffixDerived, PosPrefix},
	ATAccuracy:      {AffixDerived, PosSuffix},
	ATEvasion:       {AffixDerived, PosSuffix},
	ATDropRate:      {AffixSpecial, PosSuffix},
	ATExpBonus:      {AffixSpecial, PosSuffix},
	ATKillHeal:      {AffixSpecial, PosSuffix},
	ATMoveSpeed:     {AffixSpecial, PosPrefix},
	ATCooldownRed:   {AffixSpecial, PosSuffix},
	ATReflect:       {AffixSpecial, PosSuffix},
	ATShield:        {AffixSpecial, PosSuffix},
	ATResourceGain:  {AffixSpecial, PosSuffix},
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

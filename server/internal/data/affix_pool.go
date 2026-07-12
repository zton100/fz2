package data

// AffixCategory 词缀大类。
type AffixCategory int

const (
	AffixBasic   AffixCategory = iota // 基础属性
	AffixDerived                      // 衍生属性
	AffixSpecial                      // 经济/特殊
)

// Floor unlock thresholds for each affix category.
// Creates "discovery" moments as players push deeper floors.
const (
	FloorUnlockBasic   = 1  // Basic affixes available from floor 1
	FloorUnlockDerived = 6  // Derived affixes (crit, speed, elemental) unlock at floor 6
	FloorUnlockSpecial = 16 // Special affixes (drop, exp, etc.) unlock at floor 16
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
	ATStrength  AffixType = "strength"
	ATAgility   AffixType = "agility"
	ATIntellect AffixType = "intellect"
	ATVitality  AffixType = "vitality"
	ATMaxHP     AffixType = "max_hp"
	ATArmor     AffixType = "armor"
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
	FloorMin int // 最低解锁楼层
}

// tierMultiplier 各 Tier 相对于 T1 的倍率。
var tierMultiplier = [5]float64{1.0, 2.0, 4.0, 8.0, 16.0}

// tier1Base 各词缀类型的 T1 [min, max] 基准区间。
var tier1Base = map[AffixType][2]float64{
	ATStrength:     {1, 3},
	ATAgility:      {1, 3},
	ATIntellect:    {1, 3},
	ATVitality:     {1, 3},
	ATMaxHP:        {5, 15},
	ATArmor:        {2, 6},
	ATCritRate:     {0.01, 0.02},
	ATCritDamage:   {0.05, 0.10},
	ATAttackSpeed:  {0.02, 0.04},
	ATLifesteal:    {0.005, 0.01},
	ATFireDmg:      {2, 6},
	ATColdDmg:      {2, 6},
	ATLightningDmg: {2, 6},
	ATAccuracy:     {0.02, 0.04},
	ATEvasion:      {0.01, 0.02},
	ATDropRate:     {0.02, 0.04},
	ATExpBonus:     {0.02, 0.04},
	ATKillHeal:     {1, 3},
	ATMoveSpeed:    {0.02, 0.04},
	ATCooldownRed:  {0.02, 0.04},
	ATReflect:      {0.02, 0.04},
	ATShield:       {5, 15},
	ATResourceGain: {0.02, 0.04},
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

// categoryFloorMin maps affix category to its minimum unlock floor.
func categoryFloorMin(cat AffixCategory) int {
	switch cat {
	case AffixBasic:
		return FloorUnlockBasic
	case AffixDerived:
		return FloorUnlockDerived
	case AffixSpecial:
		return FloorUnlockSpecial
	default:
		return 1
	}
}

// AffixCategoryOf returns the category of a given affix type.
func AffixCategoryOf(t AffixType) AffixCategory {
	return affixMeta[t].Cat
}

// AllAffixTypes 返回全部 23 个词缀类型。
func AllAffixTypes() []AffixType {
	return []AffixType{
		ATStrength, ATAgility, ATIntellect, ATVitality, ATMaxHP, ATArmor,
		ATCritRate, ATCritDamage, ATAttackSpeed, ATLifesteal, ATFireDmg, ATColdDmg, ATLightningDmg, ATAccuracy, ATEvasion,
		ATDropRate, ATExpBonus, ATKillHeal, ATMoveSpeed, ATCooldownRed, ATReflect, ATShield, ATResourceGain,
	}
}

// ActiveAffixTypes returns affixes with implemented combat or economy effects.
// Unsupported types stay defined so existing saves remain readable.
func ActiveAffixTypes() []AffixType {
	unsupported := map[AffixType]bool{
		ATExpBonus: true, ATMoveSpeed: true, ATCooldownRed: true,
	}
	active := make([]AffixType, 0, len(AllAffixTypes())-len(unsupported))
	for _, affix := range AllAffixTypes() {
		if !unsupported[affix] {
			active = append(active, affix)
		}
	}
	return active
}

// BuildAffixPool 构建当前生效词缀池：20 类型 × 5 Tier = 100 条目。
// 类型常量仍保留 23 种，尚未实现对应系统的词缀不会生成新掉落。
// 各 Tier 的数值区间由 tier1Base × tierMultiplier[tier-1] 统一计算。
// FloorMin 由词缀大类决定：Basic=1, Derived=6, Special=16。
func BuildAffixPool() []AffixDef {
	types := ActiveAffixTypes()
	pool := make([]AffixDef, 0, len(types)*5)
	for _, t := range types {
		meta := affixMeta[t]
		base := tier1Base[t]
		floorMin := categoryFloorMin(meta.Cat)
		for tier := 1; tier <= 5; tier++ {
			mul := tierMultiplier[tier-1]
			pool = append(pool, AffixDef{
				Type:     t,
				Category: meta.Cat,
				Position: meta.Pos,
				Tier:     tier,
				Min:      base[0] * mul,
				Max:      base[1] * mul,
				FloorMin: floorMin,
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

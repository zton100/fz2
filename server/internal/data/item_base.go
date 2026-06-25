package data

// ItemBase 装备基底：某槽位的白板装备定义。
type ItemBase struct {
	ID        string                 // 基底 ID
	Name      string                 // 中文名
	Slot      Slot                   // 槽位
	BaseStats map[AffixType]float64  // 白板基础属性（无词缀时）
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

// BaseBySlot 按槽位取基底。
func BaseBySlot(slot Slot) ItemBase {
	for _, b := range allBases {
		if b.Slot == slot {
			return b
		}
	}
	return allBases[0]
}

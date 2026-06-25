package data

// Slot 装备槽位枚举。
type Slot int

const (
	SlotWeapon Slot = iota // 武器
	SlotHelmet             // 头盔
	SlotArmor              // 护甲
	SlotGloves             // 手套
	SlotBoots              // 靴子
	SlotRing1              // 戒指1
	SlotRing2              // 戒指2
	SlotAmulet             // 项链
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

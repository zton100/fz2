package data

import "equipment-idle-server/internal/locale"

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
	SlotNeck               // 项链
)

// SlotName returns the localized display name for a slot.
func SlotName(s Slot) string {
	l := locale.Current()
	switch s {
	case SlotWeapon:
		return l.SlotWeapon
	case SlotHelmet:
		return l.SlotHelmet
	case SlotArmor:
		return l.SlotArmor
	case SlotGloves:
		return l.SlotGloves
	case SlotBoots:
		return l.SlotBoots
	case SlotRing1:
		return l.SlotRing1
	case SlotRing2:
		return l.SlotRing2
	case SlotNeck:
		return l.SlotNeck
	}
	return "?"
}

// AllSlots 返回全部 8 个槽位。
func AllSlots() []Slot {
	return []Slot{SlotWeapon, SlotHelmet, SlotArmor, SlotGloves, SlotBoots, SlotRing1, SlotRing2, SlotNeck}
}

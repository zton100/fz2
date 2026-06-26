package inventory

import (
	"errors"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// Equip 把背包中指定 UID 的装备穿戴到对应槽位。
// 若该槽位已有装备，旧装备回背包。
func Equip(p *model.Player, uid string) error {
	idx := findInBag(p, uid)
	if idx < 0 {
		return errors.New("equipment not found in bag: " + uid)
	}
	eq := p.EquipBag[idx]
	// 从背包移除
	p.EquipBag = append(p.EquipBag[:idx], p.EquipBag[idx+1:]...)
	// 旧装备回背包
	if old := p.Equipped[eq.Slot]; old != nil {
		p.EquipBag = append(p.EquipBag, old)
	}
	p.Equipped[eq.Slot] = eq
	return nil
}

// Unequip 卸下指定槽位的装备，放回背包。
func Unequip(p *model.Player, slot data.Slot) error {
	eq := p.Equipped[slot]
	if eq == nil {
		return errors.New("no equipment in slot")
	}
	p.EquipBag = append(p.EquipBag, eq)
	delete(p.Equipped, slot)
	return nil
}

// findInBag 返回 UID 在背包的索引，找不到 -1。
func findInBag(p *model.Player, uid string) int {
	for i, eq := range p.EquipBag {
		if eq.UID == uid {
			return i
		}
	}
	return -1
}

package main

import (
	"fmt"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/reincarnation"
)

func main() {
	p := model.NewPlayer("smoke")
	p.Floor = 12
	p.MaxFloor = 12
	p.Equipped[data.SlotWeapon] = &model.Equipment{UID: "e1"}
	p.AddEquipment(&model.Equipment{UID: "e2"})
	p.AddMaterial(data.MatBase, 30)

	fmt.Printf("before: floor=%d souls=%d bag=%d equipped=%d mats=%d\n",
		p.Floor, p.Souls, len(p.EquipBag), len(p.Equipped), p.Materials[data.MatBase])

	err := reincarnation.Reincarnate(p)
	if err != nil {
		fmt.Println("FAIL reincarnate:", err)
		return
	}
	fmt.Printf("after: floor=%d souls=%d bag=%d equipped=%d mats=%d maxfloor=%d\n",
		p.Floor, p.Souls, len(p.EquipBag), len(p.Equipped), p.Materials[data.MatBase], p.MaxFloor)

	if p.Souls != 2 { // floor(12/5)=2
		fmt.Println("FAIL: souls should be 2")
		return
	}

	// 升级天赋
	reincarnation.UpgradeTalent(p, "damage")
	reincarnation.UpgradeTalent(p, "damage")
	fmt.Printf("talents: %+v souls=%d\n", p.Talents, p.Souls)
	if p.Talents["damage"] != 2 || p.Souls != 0 {
		fmt.Println("FAIL: talent upgrade")
		return
	}
	fmt.Printf("damage bonus: %.0f%%\n", reincarnation.DamageBonus(p)*100)

	fmt.Println("SMOKE_OK: reincarnation works")
}

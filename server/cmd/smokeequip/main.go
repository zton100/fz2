package main

import (
	"fmt"
	"math/rand"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/inventory"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

func main() {
	p := model.NewPlayer("smoke")
	gen := loot.NewGenerator(rand.New(rand.NewSource(5)))
	drop := loot.NewDropTable(gen)

	// 掉落 5 件装备进背包
	for i := 0; i < 5; i++ {
		p.AddEquipment(drop.Drop(data.SlotWeapon, 10))
	}
	fmt.Printf("dropped %d items into bag\n", len(p.EquipBag))

	// 穿前战力
	before := combat.ComputePower(combat.AggregateStats(p.EquippedList()))
	fmt.Printf("power before equip: %.2f\n", before)

	// 穿第一件武器
	uid := p.EquipBag[0].UID
	if err := inventory.Equip(p, uid); err != nil {
		fmt.Println("FAIL equip:", err)
		return
	}
	after := combat.ComputePower(combat.AggregateStats(p.EquippedList()))
	fmt.Printf("power after equip weapon %s: %.2f\n", uid, after)

	if after <= before {
		fmt.Println("FAIL: power did not increase after equip")
		return
	}
	fmt.Println("SMOKE_OK: equip increases power")
}

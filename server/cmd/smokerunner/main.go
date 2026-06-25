package main

import (
	"fmt"
	"math/rand"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

// 直接验证：构造带强装备的玩家跑 Runner，确认 loot/floor 回调触发。
func main() {
	p := model.NewPlayer("smoke")
	// 给一把强力武器（力量 1000，战力 1000 > 第1层怪物强度10）
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 1000},
	}

	gen := loot.NewGenerator(rand.New(rand.NewSource(99)))
	drop := loot.NewDropTable(gen)
	runner := dungeon.NewRunner(p, combat.ComputePower, drop)

	lootCount := 0
	floorAdvances := 0
	runner.LootCallback = func(eq *model.Equipment) {
		lootCount++
		if lootCount <= 3 {
			fmt.Printf("[loot #%d] %s slot=%d rarity=%d uid=%s affixes=%d\n",
				lootCount, eq.Name, int(eq.Slot), int(eq.Rarity), eq.UID, len(eq.Affixes))
		}
	}
	runner.FloorCallback = func(newFloor int) {
		floorAdvances++
		if floorAdvances <= 3 {
			fmt.Printf("[floor] advanced to %d\n", newFloor)
		}
	}

	// 跑 10 个 tick（每次都该胜利，因为武器太强）
	for i := 0; i < 10; i++ {
		runner.Tick()
	}

	fmt.Printf("--- summary after 10 ticks ---\n")
	fmt.Printf("floor=%d loot_dropped=%d floor_advances=%d bag_size=%d\n",
		p.Floor, lootCount, floorAdvances, len(p.EquipBag))

	if lootCount == 0 {
		fmt.Println("FAIL: no loot dropped")
		return
	}
	if floorAdvances == 0 {
		fmt.Println("FAIL: no floor advance")
		return
	}
	fmt.Println("SMOKE_OK: loot and floor push callbacks fire correctly")
}

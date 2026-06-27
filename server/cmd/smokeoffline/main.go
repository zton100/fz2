package main

import (
	"fmt"
	"math/rand"
	"time"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/offline"
)

func main() {
	p := model.NewPlayer("smoke")
	p.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 50000},
	}
	gen := loot.NewGenerator(rand.New(rand.NewSource(11)))
	drop := loot.NewDropTable(gen)

	startFloor := p.Floor
	result := offline.Calc(p, nil, drop, 2*time.Hour, 0, 0, 0)
	fmt.Printf("offline 2h: ticks=%d loot=%d floors=%d (floor %d->%d)\n",
		result.TicksSimulated, result.LootCount, result.FloorsAdvanced,
		startFloor, p.Floor)

	if result.LootCount == 0 {
		fmt.Println("FAIL: no offline loot"); return
	}
	if p.Floor <= startFloor {
		fmt.Println("FAIL: no floor advance"); return
	}

	// 测试 8 小时封顶
	p2 := model.NewPlayer("smoke2")
	p2.Equipped[data.SlotWeapon] = &model.Equipment{
		BaseStats: map[data.AffixType]float64{data.ATStrength: 50000},
	}
	result2 := offline.Calc(p2, nil, drop, 10*time.Hour, 0, 0, 0)
	if result2.Duration > 8*time.Hour {
		fmt.Println("FAIL: not capped at 8h"); return
	}
	fmt.Printf("offline 10h (capped): duration=%v ticks=%d\n", result2.Duration, result2.TicksSimulated)

	fmt.Println("SMOKE_OK: offline calc works")
}

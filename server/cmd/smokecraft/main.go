package main

import (
	"fmt"
	"math/rand"

	"equipment-idle-server/internal/crafting"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/upgrade"
)

func main() {
	p := model.NewPlayer("smoke")
	gen := loot.NewGenerator(rand.New(rand.NewSource(7)))
	rng := rand.New(rand.NewSource(3))

	// 掉落并分解
	eq := gen.Generate(data.SlotWeapon, data.RarityRare, 10)
	p.AddEquipment(eq)
	yield, _ := crafting.Decompose(p, eq)
	fmt.Printf("decomposed rare: %+v\n", yield)
	if p.Materials[data.MatBase] < 8 {
		fmt.Println("FAIL: decompose base mat"); return
	}

	// 合成
	p.AddMaterial(data.MatBase, 20)
	composed, _ := crafting.Compose(p, gen, data.SlotHelmet)
	fmt.Printf("composed: %s uid=%s\n", composed.Name, composed.UID)

	// 强化到 +3
	for i := 0; i < 3; i++ {
		r, _ := upgrade.Upgrade(p, rng, composed)
		fmt.Printf("upgrade attempt %d: success=%v lvl=%d\n", i+1, r.Success, composed.Upgrade)
	}
	if composed.Upgrade != 3 {
		fmt.Println("FAIL: should be +3"); return
	}

	// 重铸
	reforgeEq := gen.Generate(data.SlotWeapon, data.RarityMagic, 10)
	p.AddEquipment(reforgeEq)
	for _, a := range reforgeEq.Affixes {
		p.AddMaterial(data.AffixMaterialByTier(a.Tier), 5)
	}
	err := crafting.Reforge(p, gen, reforgeEq)
	fmt.Printf("reforge: err=%v affixes=%d\n", err, len(reforgeEq.Affixes))

	fmt.Println("SMOKE_OK: crafting pipeline works")
}

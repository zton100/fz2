package data

import (
	_ "embed"
	"encoding/json"
	"fmt"
)

// ItemBase is a versioned equipment base definition loaded from embedded JSON.
type ItemBase struct {
	ID        string                `json:"id"`
	Name      string                `json:"name"`
	Slot      Slot                  `json:"slot"`
	BaseStats map[AffixType]float64 `json:"base_stats"`
}

type equipmentBaseFile struct {
	Version int        `json:"version"`
	Bases   []ItemBase `json:"bases"`
}

//go:embed equipment_bases.json
var equipmentBasesJSON []byte

var equipmentBases equipmentBaseFile

func init() {
	if err := json.Unmarshal(equipmentBasesJSON, &equipmentBases); err != nil {
		panic(fmt.Errorf("decode equipment bases: %w", err))
	}
	if equipmentBases.Version <= 0 {
		panic("equipment data version must be positive")
	}
	ids := map[string]bool{}
	counts := map[Slot]int{}
	for _, base := range equipmentBases.Bases {
		if base.ID == "" || base.Name == "" {
			panic("equipment base id and name are required")
		}
		if ids[base.ID] {
			panic("duplicate equipment base id: " + base.ID)
		}
		ids[base.ID] = true
		counts[base.Slot]++
	}
	for _, slot := range AllSlots() {
		if counts[slot] == 0 {
			panic(fmt.Sprintf("equipment slot %d has no bases", slot))
		}
	}
}

func EquipmentDataVersion() int {
	return equipmentBases.Version
}

func AllBases() []ItemBase {
	out := make([]ItemBase, len(equipmentBases.Bases))
	copy(out, equipmentBases.Bases)
	return out
}

func BasesBySlot(slot Slot) []ItemBase {
	var out []ItemBase
	for _, base := range equipmentBases.Bases {
		if base.Slot == slot {
			out = append(out, base)
		}
	}
	return out
}

func BaseByID(id string) (ItemBase, bool) {
	for _, base := range equipmentBases.Bases {
		if base.ID == id {
			return base, true
		}
	}
	return ItemBase{}, false
}

func BaseBySlot(slot Slot) ItemBase {
	bases := BasesBySlot(slot)
	if len(bases) > 0 {
		return bases[0]
	}
	return equipmentBases.Bases[0]
}

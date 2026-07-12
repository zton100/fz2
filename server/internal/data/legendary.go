package data

import (
	_ "embed"
	"encoding/json"
	"fmt"
)

type LegendaryDefinition struct {
	ID                   string                `json:"id"`
	Name                 string                `json:"name"`
	BaseID               string                `json:"base_id"`
	Slot                 Slot                  `json:"slot"`
	Description          string                `json:"description"`
	BonusStats           map[AffixType]float64 `json:"bonus_stats"`
	PowerMultiplier      float64               `json:"power_multiplier"`
	BossRewardMultiplier float64               `json:"boss_reward_multiplier"`
}

type legendaryFile struct {
	Version     int                   `json:"version"`
	Legendaries []LegendaryDefinition `json:"legendaries"`
}

//go:embed legendary_equipment.json
var legendaryJSON []byte

var legendaryData legendaryFile

func init() {
	if err := json.Unmarshal(legendaryJSON, &legendaryData); err != nil {
		panic(fmt.Errorf("decode legendary equipment: %w", err))
	}
	if legendaryData.Version <= 0 {
		panic("legendary data version must be positive")
	}
	seen := map[string]bool{}
	for _, def := range legendaryData.Legendaries {
		if def.ID == "" || def.Name == "" || def.Description == "" {
			panic("legendary id, name and description are required")
		}
		if seen[def.ID] {
			panic("duplicate legendary id: " + def.ID)
		}
		seen[def.ID] = true
		base, ok := BaseByID(def.BaseID)
		if !ok || base.Slot != def.Slot {
			panic(fmt.Sprintf("legendary %s references invalid base %s", def.ID, def.BaseID))
		}
	}
}

func LegendaryDataVersion() int {
	return legendaryData.Version
}

func AllLegendaries() []LegendaryDefinition {
	out := make([]LegendaryDefinition, len(legendaryData.Legendaries))
	copy(out, legendaryData.Legendaries)
	return out
}

func LegendariesBySlot(slot Slot) []LegendaryDefinition {
	var out []LegendaryDefinition
	for _, def := range legendaryData.Legendaries {
		if def.Slot == slot {
			out = append(out, def)
		}
	}
	return out
}

func LegendaryByID(id string) (LegendaryDefinition, bool) {
	for _, def := range legendaryData.Legendaries {
		if def.ID == id {
			return def, true
		}
	}
	return LegendaryDefinition{}, false
}

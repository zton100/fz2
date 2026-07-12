package data

import (
	_ "embed"
	"encoding/json"
	"fmt"
)

const (
	ArtifactEchoStrike    = "echo_strike"
	ArtifactOpeningShield = "opening_shield"
	ArtifactExecute       = "execute"
	ArtifactHitHeal       = "hit_heal"
)

type ArtifactDefinition struct {
	ID          string                `json:"id"`
	Name        string                `json:"name"`
	BaseID      string                `json:"base_id"`
	Slot        Slot                  `json:"slot"`
	Description string                `json:"description"`
	Trigger     string                `json:"trigger"`
	BonusStats  map[AffixType]float64 `json:"bonus_stats"`
	Value       float64               `json:"value"`
}

type artifactFile struct {
	Version   int                  `json:"version"`
	Artifacts []ArtifactDefinition `json:"artifacts"`
}

//go:embed artifact_equipment.json
var artifactJSON []byte

var artifactData artifactFile

func init() {
	if err := json.Unmarshal(artifactJSON, &artifactData); err != nil {
		panic(fmt.Errorf("decode artifact equipment: %w", err))
	}
	if artifactData.Version <= 0 {
		panic("artifact data version must be positive")
	}
	seen := map[string]bool{}
	triggers := map[string]bool{
		ArtifactEchoStrike:    true,
		ArtifactOpeningShield: true,
		ArtifactExecute:       true,
		ArtifactHitHeal:       true,
	}
	for _, def := range artifactData.Artifacts {
		if def.ID == "" || def.Name == "" || def.Description == "" || def.Trigger == "" {
			panic("artifact id, name, description and trigger are required")
		}
		if seen[def.ID] {
			panic("duplicate artifact id: " + def.ID)
		}
		seen[def.ID] = true
		if !triggers[def.Trigger] {
			panic("unsupported artifact trigger: " + def.Trigger)
		}
	}
}

func ArtifactDataVersion() int {
	return artifactData.Version
}

func AllArtifacts() []ArtifactDefinition {
	out := make([]ArtifactDefinition, len(artifactData.Artifacts))
	copy(out, artifactData.Artifacts)
	return out
}

func ArtifactsBySlot(slot Slot) []ArtifactDefinition {
	var out []ArtifactDefinition
	for _, def := range artifactData.Artifacts {
		if def.Slot == slot {
			out = append(out, def)
		}
	}
	return out
}

func ArtifactByID(id string) (ArtifactDefinition, bool) {
	for _, def := range artifactData.Artifacts {
		if def.ID == id {
			return def, true
		}
	}
	return ArtifactDefinition{}, false
}

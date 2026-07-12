package loot

import (
	"crypto/rand"
	"encoding/hex"
	mathrand "math/rand"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// Generator 装备生成器。注入 *mathrand.Rand 便于测试确定性。
type Generator struct {
	rng             *mathrand.Rand
	pool            []data.AffixDef
	baseCursor      map[data.Slot]int
	legendaryCursor map[data.Slot]int
	artifactCursor  map[data.Slot]int
}

// NewGenerator 创建生成器。
func NewGenerator(rng *mathrand.Rand) *Generator {
	return &Generator{
		rng:             rng,
		pool:            data.BuildAffixPool(),
		baseCursor:      map[data.Slot]int{},
		legendaryCursor: map[data.Slot]int{},
		artifactCursor:  map[data.Slot]int{},
	}
}

// Generate 按槽位、稀有度和楼层生成一件装备实例。
func (g *Generator) Generate(slot data.Slot, rarity data.Rarity, floor int) *model.Equipment {
	bases := data.BasesBySlot(slot)
	base := data.BaseBySlot(slot)
	legendaryID := ""
	artifactID := ""
	name := ""
	if rarity == data.RarityLegendary {
		defs := data.LegendariesBySlot(slot)
		if len(defs) > 0 {
			def := defs[g.legendaryCursor[slot]%len(defs)]
			g.legendaryCursor[slot]++
			legendaryID = def.ID
			name = def.Name
			if selected, ok := data.BaseByID(def.BaseID); ok {
				base = selected
			}
		}
	} else if rarity == data.RarityArtifact {
		defs := data.ArtifactsBySlot(slot)
		if len(defs) > 0 {
			def := defs[g.artifactCursor[slot]%len(defs)]
			g.artifactCursor[slot]++
			artifactID = def.ID
			name = def.Name
			if selected, ok := data.BaseByID(def.BaseID); ok {
				base = selected
			}
		}
	} else if len(bases) > 0 {
		index := g.baseCursor[slot] % len(bases)
		base = bases[index]
		g.baseCursor[slot]++
	}
	if name == "" {
		name = base.Name
	}
	eq := &model.Equipment{
		UID:         nextUID(),
		BaseID:      base.ID,
		LegendaryID: legendaryID,
		ArtifactID:  artifactID,
		Name:        name,
		Slot:        slot,
		Rarity:      rarity,
		BaseStats:   copyStats(base.BaseStats),
		Upgrade:     0,
	}
	rule := data.RarityAffixCount[rarity]
	prefixes := g.filteredPool(data.PosPrefix, floor)
	suffixes := g.filteredPool(data.PosSuffix, floor)

	chosenTypes := map[data.AffixType]bool{}
	for i := 0; i < rule.Prefix && len(prefixes) > 0; i++ {
		a := g.pickAffix(prefixes, chosenTypes)
		if a != nil {
			eq.Affixes = append(eq.Affixes, g.rollValue(a))
			chosenTypes[a.Type] = true
		}
	}
	for i := 0; i < rule.Suffix && len(suffixes) > 0; i++ {
		a := g.pickAffix(suffixes, chosenTypes)
		if a != nil {
			eq.Affixes = append(eq.Affixes, g.rollValue(a))
			chosenTypes[a.Type] = true
		}
	}
	return eq
}

func (g *Generator) filteredPool(pos data.AffixPosition, floor int) []data.AffixDef {
	var out []data.AffixDef
	for _, a := range g.pool {
		if a.Position == pos && a.FloorMin <= floor {
			out = append(out, a)
		}
	}
	return out
}

func (g *Generator) pickAffix(candidates []data.AffixDef, chosen map[data.AffixType]bool) *data.AffixDef {
	var avail []data.AffixDef
	for _, c := range candidates {
		if !chosen[c.Type] {
			avail = append(avail, c)
		}
	}
	if len(avail) == 0 {
		return nil
	}
	idx := g.rng.Intn(len(avail))
	picked := avail[idx]
	return &picked
}

func (g *Generator) rollValue(def *data.AffixDef) model.AffixInstance {
	val := def.Min + g.rng.Float64()*(def.Max-def.Min)
	return model.AffixInstance{Type: def.Type, Tier: def.Tier, Value: val}
}

// nextUID generates a globally unique equipment ID using crypto/rand.
func nextUID() string {
	var b [16]byte
	if _, err := rand.Read(b[:]); err != nil {
		return "eq_fallback"
	}
	return "eq_" + hex.EncodeToString(b[:])
}

func copyStats(src map[data.AffixType]float64) map[data.AffixType]float64 {
	dst := make(map[data.AffixType]float64, len(src))
	for k, v := range src {
		dst[k] = v
	}
	return dst
}

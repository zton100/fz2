package crafting

import (
	"errors"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

// Compose 合成：消耗基础材料生成指定槽位的普通品质装备。
func Compose(p *model.Player, gen *loot.Generator, slot data.Slot) (*model.Equipment, error) {
	if !p.HasMaterial(data.MatBase, data.ComposeCost) {
		return nil, errors.New("insufficient base material")
	}
	p.SpendMaterial(data.MatBase, data.ComposeCost)
	eq := gen.Generate(slot, data.RarityCommon)
	p.AddEquipment(eq)
	return eq, nil
}

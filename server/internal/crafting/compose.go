package crafting

import (
	"errors"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

// Compose 合成一件指定槽位的普通装备。
func Compose(p *model.Player, gen *loot.Generator, slot data.Slot) (*model.Equipment, error) {
	if !p.HasMaterial(data.MatBase, data.ComposeCost) {
		return nil, errors.New("基础材料不足")
	}
	p.SpendMaterial(data.MatBase, data.ComposeCost)
	eq := gen.Generate(slot, data.RarityCommon, p.Floor)
	p.EquipBag = append(p.EquipBag, eq)
	return eq, nil
}

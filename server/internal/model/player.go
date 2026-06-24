package model

// Player 玩家领域模型。阶段 0 最小版，后续阶段扩充装备/穿戴/材料等。
type Player struct {
	Account   string   // 账号名
	Floor     int      // 当前层数
	Souls     int      // 魂点
	Inventory []string // 背包物品 ID 占位
}

// NewPlayer 创建新玩家，默认第 1 层、0 魂、空背包。
func NewPlayer(account string) *Player {
	return &Player{
		Account:   account,
		Floor:     1,
		Souls:     0,
		Inventory: []string{},
	}
}

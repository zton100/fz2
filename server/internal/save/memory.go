package save

import "equipment-idle-server/internal/model"

// MemoryStore 内存存档。阶段 0 用，后续替换为持久化。
type MemoryStore struct {
	players map[string]*model.Player
}

// NewMemoryStore 创建内存存档。
func NewMemoryStore() *MemoryStore {
	return &MemoryStore{players: make(map[string]*model.Player)}
}

// LoadOrCreate 按账号读取玩家；不存在则新建。
func (s *MemoryStore) LoadOrCreate(account string) *model.Player {
	if p, ok := s.players[account]; ok {
		return p
	}
	p := model.NewPlayer(account)
	s.players[account] = p
	return p
}

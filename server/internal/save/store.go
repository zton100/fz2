package save

import (
	"encoding/json"
	"os"
	"path/filepath"

	"equipment-idle-server/internal/model"
)

// Store 持久化存档：内存缓存 + JSON 文件持久化。
// dir 为空时退化为纯内存存档（测试用）。
type Store struct {
	players map[string]*model.Player
	dir     string
}

// NewStore 创建带文件持久化的存档。
// dir 为保存目录，如 "saves"；传空字符串则纯内存。
func NewStore(dir string) *Store {
	if dir != "" {
		os.MkdirAll(dir, 0755)
	}
	return &Store{
		players: make(map[string]*model.Player),
		dir:     dir,
	}
}

// LoadOrCreate 按账号读取玩家；内存有则返回，否则尝试从文件加载，文件无则新建。
func (s *Store) LoadOrCreate(account string) *model.Player {
	if p, ok := s.players[account]; ok {
		return p
	}
	if p := s.loadFromFile(account); p != nil {
		s.players[account] = p
		return p
	}
	p := model.NewPlayer(account)
	s.players[account] = p
	return p
}

// Save 将玩家数据写入 JSON 文件（原子写入：tmp → rename）。
func (s *Store) Save(account string) error {
	if s.dir == "" {
		return nil
	}
	p, ok := s.players[account]
	if !ok {
		return nil
	}
	data, err := json.MarshalIndent(p, "", "  ")
	if err != nil {
		return err
	}
	path := filepath.Join(s.dir, account+".json")
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, data, 0644); err != nil {
		return err
	}
	return os.Rename(tmp, path)
}

func (s *Store) loadFromFile(account string) *model.Player {
	if s.dir == "" {
		return nil
	}
	path := filepath.Join(s.dir, account+".json")
	data, err := os.ReadFile(path)
	if err != nil {
		return nil
	}
	var p model.Player
	if err := json.Unmarshal(data, &p); err != nil {
		return nil
	}
	return &p
}

package save

import (
	"encoding/json"
	"log"
	"os"
	"path/filepath"
	"sync"
	"time"

	"equipment-idle-server/internal/model"
)

// Store handles persistence with in-memory cache + JSON file storage.
// dir is the save directory; empty string means pure in-memory (no disk).
type Store struct {
	mu      sync.RWMutex
	players map[string]*model.Player
	dir     string
	dirty   map[string]bool // accounts with unsaved changes since last flush
}

// NewStore creates a Store. Pass empty dir for in-memory only (tests).
func NewStore(dir string) *Store {
	if dir != "" {
		os.MkdirAll(dir, 0755)
	}
	return &Store{
		players: make(map[string]*model.Player),
		dir:     dir,
		dirty:   make(map[string]bool),
	}
}

// LoadOrCreate returns the player for account, loading from file or creating new.
func (s *Store) LoadOrCreate(account string) *model.Player {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.loadOrCreateLocked(account)
}

func (s *Store) loadOrCreateLocked(account string) *model.Player {
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

// Save writes player data to JSON file (atomic: tmp → rename).
func (s *Store) Save(account string) error {
	if s.dir == "" {
		return nil
	}
	s.mu.RLock()
	p, ok := s.players[account]
	s.mu.RUnlock()
	if !ok {
		return nil
	}
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.saveLocked(account, p)
}

func (s *Store) saveLocked(account string, p *model.Player) error {
	if s.dir == "" {
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
	if err := os.Rename(tmp, path); err != nil {
		return err
	}
	delete(s.dirty, account)
	return nil
}

// MarkDirty flags an account as having unsaved changes.
func (s *Store) MarkDirty(account string) {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.dirty[account] = true
}

// SaveDirty saves all dirty accounts. Returns the first error encountered.
func (s *Store) SaveAllDirty() {
	s.mu.Lock()
	accounts := make([]string, 0, len(s.dirty))
	for a := range s.dirty {
		accounts = append(accounts, a)
	}
	s.mu.Unlock()

	for _, a := range accounts {
		s.mu.RLock()
		p, ok := s.players[a]
		s.mu.RUnlock()
		if !ok {
			continue
		}
		s.mu.Lock()
		if err := s.saveLocked(a, p); err != nil {
			log.Printf("save dirty %s: %v", a, err)
		}
		s.mu.Unlock()
	}
}

// StartPeriodicFlush starts a goroutine that saves dirty accounts every interval.
// Returns a stop function.
func (s *Store) StartPeriodicFlush(interval time.Duration) (stop func()) {
	ticker := time.NewTicker(interval)
	done := make(chan struct{})
	go func() {
		for {
			select {
			case <-ticker.C:
				s.SaveAllDirty()
			case <-done:
				ticker.Stop()
				return
			}
		}
	}()
	return func() { close(done) }
}

// WithPlayer executes fn while holding the write lock for the player's account.
// This serializes all mutations to a single player (battle loop + user actions).
func (s *Store) WithPlayer(account string, fn func(p *model.Player)) {
	s.mu.Lock()
	defer s.mu.Unlock()
	p := s.loadOrCreateLocked(account)
	fn(p)
}

// WithPlayerSave executes fn while holding the write lock, then persists the
// player before releasing the lock. This keeps mutation and immediate saves
// serialized for websocket request handlers.
func (s *Store) WithPlayerSave(account string, fn func(p *model.Player)) error {
	s.mu.Lock()
	defer s.mu.Unlock()
	p := s.loadOrCreateLocked(account)
	fn(p)
	return s.saveLocked(account, p)
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

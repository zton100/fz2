package save

// MemoryStore 纯内存存档（等价于 dir 为空的 Store），供测试用。
type MemoryStore = Store

// NewMemoryStore 创建纯内存存档。
func NewMemoryStore() *MemoryStore {
	return NewStore("")
}

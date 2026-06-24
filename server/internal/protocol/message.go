package protocol

import "encoding/json"

// Envelope 是所有消息的统一信封。
type Envelope struct {
	T    string          `json:"t"`              // 消息类型
	ID   string          `json:"id,omitempty"`   // 请求 ID，用于 req/resp 匹配；推送/同步可空
	Data json.RawMessage `json:"data,omitempty"` // 消息体，按 T 解析
}

// 消息类型常量
const (
	TypeLogin = "login" // 请求：登录
	TypeSync  = "sync"  // 推送：全量同步
)

// LoginRequest 登录请求体。
type LoginRequest struct {
	Account string `json:"account"` // 临时账号名
}

// SyncData 全量同步消息体。阶段 0 最小字段，后续阶段扩充。
type SyncData struct {
	Account   string   `json:"account"`   // 账号
	Floor     int      `json:"floor"`     // 当前层数
	Souls     int      `json:"souls"`     // 魂点
	Inventory []string `json:"inventory"` // 背包物品 ID 占位
}

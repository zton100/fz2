package main

import (
	"encoding/json"
	"fmt"
	"log"
	"os"
	"time"

	"github.com/gorilla/websocket"
	"equipment-idle-server/internal/protocol"
)

// 端到端验证客户端：连接服务端，登录，等待 8 秒收集 loot/floor push。
func main() {
	conn, _, err := websocket.DefaultDialer.Dial("ws://localhost:8080/ws", nil)
	if err != nil {
		log.Fatalf("dial: %v", err)
	}
	defer conn.Close()

	// 发登录
	login := protocol.Envelope{
		T:    protocol.TypeLogin,
		ID:   "v1",
		Data: mustMarshal(protocol.LoginRequest{Account: "verifyhero"}),
	}
	if err := conn.WriteJSON(login); err != nil {
		log.Fatalf("write login: %v", err)
	}

	// 给玩家一点初始战力——通过发多次登录不行，这里依赖服务端默认玩家无装备。
	// 无装备玩家战力=0，打不过第1层（强度10），不会掉落也不会推进。
	// 所以这个验证主要确认：连接、登录、sync 回包正常；loot/floor 在玩家强时才出现。
	// 为验证 push，我们连上后收 8 秒消息并打印类型统计。

	counts := map[string]int{}
	deadline := time.Now().Add(8 * time.Second)
	for time.Now().Before(deadline) {
		conn.SetReadDeadline(deadline)
		_, msg, err := conn.ReadMessage()
		if err != nil {
			break
		}
		var env protocol.Envelope
		json.Unmarshal(msg, &env)
		counts[env.T]++
		if env.T == protocol.TypeSync {
			var sd protocol.SyncData
			json.Unmarshal(env.Data, &sd)
			fmt.Printf("[sync] account=%s floor=%d souls=%d\n", sd.Account, sd.Floor, sd.Souls)
		}
	}

	fmt.Println("--- message type counts ---")
	for t, c := range counts {
		fmt.Printf("%s: %d\n", t, c)
	}

	if counts[protocol.TypeSync] == 0 {
		fmt.Println("FAIL: no sync received")
		os.Exit(1)
	}
	fmt.Println("VERIFY_OK: connection and sync working")
}

func mustMarshal(v interface{}) json.RawMessage {
	b, _ := json.Marshal(v)
	return b
}

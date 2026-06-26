# 阶段 3：背包与穿戴 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让玩家能穿戴掉落的装备、对比属性、卸下装备，形成"掉落→穿戴→战力提升→推进更深"的核心循环闭环。

**Architecture:** 服务端新增 `inventory` 包，提供按 UID 穿戴/卸下装备的纯逻辑（从背包移到 Equipped 槽位，旧装备回背包），可单测。协议新增 `equip`/`unequip` 请求响应与 `bag` 全量背包推送。ws handler 处理穿戴请求并在穿戴后回发新战力。客户端 GameState 扩展背包与穿戴状态，新增背包 UI 与一键穿戴。

**Tech Stack:** Go 1.26、Unity 6 (C#)、IMGUI。

**前置条件：** 阶段 0~2 完成（player.EquipBag/Equipped、loot、combat、ws handler 已就绪）。

---

## 文件结构

**服务端**
- `server/internal/inventory/manager.go` — 背包穿戴纯逻辑（穿戴/卸下/查找）
- `server/internal/inventory/manager_test.go` — 穿戴逻辑单测
- `server/internal/protocol/message.go` — 扩展 equip/unequip 请求响应 + bag 推送 + power 推送
- `server/internal/ws/handler.go` — 处理 equip/unequip 请求，穿戴后 push bag + power

**客户端**
- `client/Assets/Scripts/Net/Message.cs` — 扩展 equip/unequip 编码 + bag/power 解析
- `client/Assets/Scripts/State/GameState.cs` — 扩展背包列表与已穿戴字典
- `client/Assets/Scripts/UI/MainController.cs` — 扩展 IMGUI 显示背包列表 + 一键穿戴按钮

---

## Task 1: 背包穿戴纯逻辑（inventory 包，TDD）

**Files:**
- Create: `server/internal/inventory/manager.go`
- Create: `server/internal/inventory/manager_test.go`

穿戴规则：从 EquipBag 按 UID 找装备 → 移到对应 Slot 的 Equipped → 该槽位旧装备（若有）回 EquipBag。

- [ ] **Step 1: 写穿戴逻辑测试**

Create `server/internal/inventory/manager_test.go`:
```go
package inventory

import (
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func makePlayerWithBag() *model.Player {
	p := model.NewPlayer("test")
	p.AddEquipment(&model.Equipment{UID: "e1", BaseID: "base_helmet", Name: "皮盔", Slot: data.SlotHelmet})
	p.AddEquipment(&model.Equipment{UID: "e2", BaseID: "base_weapon", Name: "铁剑", Slot: data.SlotWeapon})
	return p
}

func TestEquip_FromBagToSlot(t *testing.T) {
	p := makePlayerWithBag()
	err := Equip(p, "e1")
	if err != nil {
		t.Fatalf("equip error: %v", err)
	}
	if p.Equipped[data.SlotHelmet] == nil || p.Equipped[data.SlotHelmet].UID != "e1" {
		t.Fatal("helmet slot should have e1")
	}
	// 背包里不应再有 e1
	if findInBag(p, "e1") != -1 {
		t.Fatal("e1 should be removed from bag after equip")
	}
}

func TestEquip_ReplacesOldEquipment(t *testing.T) {
	p := makePlayerWithBag()
	// 先穿 e1
	Equip(p, "e1")
	// 再放一把头盔 e3 进背包
	p.AddEquipment(&model.Equipment{UID: "e3", BaseID: "base_helmet", Name: "皮盔2", Slot: data.SlotHelmet})
	// 穿 e3，e1 应回背包
	err := Equip(p, "e3")
	if err != nil {
		t.Fatalf("equip e3 error: %v", err)
	}
	if p.Equipped[data.SlotHelmet].UID != "e3" {
		t.Fatal("helmet slot should have e3 now")
	}
	if findInBag(p, "e1") == -1 {
		t.Fatal("e1 should be back in bag after replaced")
	}
}

func TestEquip_UIDNotFound(t *testing.T) {
	p := makePlayerWithBag()
	err := Equip(p, "nope")
	if err == nil {
		t.Fatal("should error when UID not found")
	}
}

func TestUnequip_MoveBackToBag(t *testing.T) {
	p := makePlayerWithBag()
	Equip(p, "e1")
	err := Unequip(p, data.SlotHelmet)
	if err != nil {
		t.Fatalf("unequip error: %v", err)
	}
	if p.Equipped[data.SlotHelmet] != nil {
		t.Fatal("helmet slot should be empty after unequip")
	}
	if findInBag(p, "e1") == -1 {
		t.Fatal("e1 should be back in bag after unequip")
	}
}

func TestUnequip_EmptySlot(t *testing.T) {
	p := makePlayerWithBag()
	err := Unequip(p, data.SlotBoots)
	if err == nil {
		t.Fatal("should error when unequip empty slot")
	}
}

// findInBag 辅助：返回 UID 在背包的索引，找不到 -1。
func findInBag(p *model.Player, uid string) int {
	for i, eq := range p.EquipBag {
		if eq.UID == uid {
			return i
		}
	}
	return -1
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/inventory/...
```
Expected: 编译失败，`Equip`/`Unequip` 未定义。

- [ ] **Step 3: 写穿戴逻辑实现**

Create `server/internal/inventory/manager.go`:
```go
package inventory

import (
	"errors"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// Equip 把背包中指定 UID 的装备穿戴到对应槽位。
// 若该槽位已有装备，旧装备回背包。
func Equip(p *model.Player, uid string) error {
	idx := findInBag(p, uid)
	if idx < 0 {
		return errors.New("equipment not found in bag: " + uid)
	}
	eq := p.EquipBag[idx]
	// 从背包移除
	p.EquipBag = append(p.EquipBag[:idx], p.EquipBag[idx+1:]...)
	// 旧装备回背包
	if old := p.Equipped[eq.Slot]; old != nil {
		p.EquipBag = append(p.EquipBag, old)
	}
	p.Equipped[eq.Slot] = eq
	return nil
}

// Unequip 卸下指定槽位的装备，放回背包。
func Unequip(p *model.Player, slot data.Slot) error {
	eq := p.Equipped[slot]
	if eq == nil {
		return errors.New("no equipment in slot")
	}
	p.EquipBag = append(p.EquipBag, eq)
	delete(p.Equipped, slot)
	return nil
}
```

- [ ] **Step 4: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/inventory/...
```
Expected: `ok equipment-idle-server/internal/inventory`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/inventory/
git commit -m "feat: inventory equip/unequip pure logic"
```

---

## Task 2: 协议扩展（equip/unequip/bag/power）

**Files:**
- Modify: `server/internal/protocol/message.go`

- [ ] **Step 1: 追加协议类型**

在 `server/internal/protocol/message.go` 的常量块追加 equip/unequip/bag/power：
```go
const (
	TypeLogin   = "login"
	TypeSync    = "sync"
	TypeLoot    = "loot"
	TypeFloor   = "floor"
	TypeEquip   = "equip"   // 请求：穿戴
	TypeUnequip = "unequip" // 请求：卸下
	TypeBag     = "bag"     // 推送：背包全量
	TypePower   = "power"   // 推送：当前战力
)
```

在文件末尾追加消息体：
```go
// EquipRequest 穿戴请求体。
type EquipRequest struct {
	UID string `json:"uid"`
}

// UnequipRequest 卸下请求体。
type UnequipRequest struct {
	Slot int `json:"slot"`
}

// BagData 背包全量推送。
type BagData struct {
	Items []EquipmentDTO `json:"items"`
}

// EquipmentDTO 装备传输对象（背包与已穿戴通用）。
type EquipmentDTO struct {
	UID     string     `json:"uid"`
	BaseID  string     `json:"base_id"`
	Name    string     `json:"name"`
	Slot    int        `json:"slot"`
	Rarity  int        `json:"rarity"`
	Upgrade int        `json:"upgrade"`
	Affixes []AffixDTO `json:"affixes"`
}

// PowerData 战力推送。
type PowerData struct {
	Power float64 `json:"power"`
}
```

- [ ] **Step 2: 验证编译**

Run:
```bash
cd server
go build ./...
```
Expected: 无输出，退出码 0。

- [ ] **Step 3: 提交**

```bash
git add server/internal/protocol/message.go
git commit -m "feat: protocol equip/unequip/bag/power types"
```

---

## Task 3: ws handler 处理穿戴请求

**Files:**
- Modify: `server/internal/ws/handler.go`

处理 equip/unequip 请求：调用 inventory 包穿戴 → 回发 bag + power push。

- [ ] **Step 1: 在 handler.go 追加 equip/unequip 处理**

在 `handleMessage` 的 switch 追加两个 case。完整改写 `handleMessage`：
```go
func (h *Hub) handleMessage(sess *Session, raw []byte) {
	var env protocol.Envelope
	if err := json.Unmarshal(raw, &env); err != nil {
		log.Printf("malformed message: %v", err)
		return
	}
	switch env.T {
	case protocol.TypeLogin:
		h.handleLogin(sess, env)
	case protocol.TypeEquip:
		h.handleEquip(sess, env)
	case protocol.TypeUnequip:
		h.handleUnequip(sess, env)
	default:
		log.Printf("unknown message type: %s", env.T)
	}
}
```

在 handler.go 末尾追加处理函数与辅助函数：
```go
// handleEquip 处理穿戴请求。
func (h *Hub) handleEquip(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.EquipRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		log.Printf("equip parse error: %v", err)
		return
	}
	if err := inventory.Equip(player, req.UID); err != nil {
		log.Printf("equip error: %v", err)
		return
	}
	h.pushBag(sess, player)
	h.pushPower(sess, player)
}

// handleUnequip 处理卸下请求。
func (h *Hub) handleUnequip(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.UnequipRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		log.Printf("unequip parse error: %v", err)
		return
	}
	if err := inventory.Unequip(player, data.Slot(req.Slot)); err != nil {
		log.Printf("unequip error: %v", err)
		return
	}
	h.pushBag(sess, player)
	h.pushPower(sess, player)
}

// pushBag 推送背包全量。
func (h *Hub) pushBag(sess *Session, player *model.Player) {
	items := make([]protocol.EquipmentDTO, len(player.EquipBag))
	for i, eq := range player.EquipBag {
		items[i] = toEquipmentDTO(eq)
	}
	bd := protocol.BagData{Items: items}
	dataBytes, _ := json.Marshal(bd)
	env := protocol.Envelope{T: protocol.TypeBag, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

// pushPower 推送当前战力。
func (h *Hub) pushPower(sess *Session, player *model.Player) {
	stats := combat.AggregateStats(player.EquippedList())
	power := combat.ComputePower(stats)
	pd := protocol.PowerData{Power: power}
	dataBytes, _ := json.Marshal(pd)
	env := protocol.Envelope{T: protocol.TypePower, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

// toEquipmentDTO 把装备实例转为传输对象。
func toEquipmentDTO(eq *model.Equipment) protocol.EquipmentDTO {
	affixes := make([]protocol.AffixDTO, len(eq.Affixes))
	for i, a := range eq.Affixes {
		affixes[i] = protocol.AffixDTO{
			Type:  string(a.Type),
			Tier:  a.Tier,
			Value: a.Value,
		}
	}
	return protocol.EquipmentDTO{
		UID:     eq.UID,
		BaseID:  eq.BaseID,
		Name:    eq.Name,
		Slot:    int(eq.Slot),
		Rarity:  int(eq.Rarity),
		Upgrade: eq.Upgrade,
		Affixes: affixes,
	}
}
```

并在 handler.go 顶部 import 块追加：
```go
"equipment-idle-server/internal/data"
"equipment-idle-server/internal/inventory"
```

- [ ] **Step 2: 验证编译与测试**

Run:
```bash
cd server
go build ./... && go test ./...
```
Expected: 全部 `ok`。

- [ ] **Step 3: 提交**

```bash
git add server/internal/ws/handler.go
git commit -m "feat: ws handler equip/unequip with bag and power push"
```

---

## Task 4: 登录后推送初始背包与战力

**Files:**
- Modify: `server/internal/ws/handler.go`

在 `handleLogin` 回发 sync 后，也推送当前 bag 与 power，让客户端登录即看到背包和战力。

- [ ] **Step 1: 在 handleLogin 末尾追加 push**

在 `handleLogin` 函数中，启动战斗循环之前，追加：
```go
	// 推送当前背包与战力
	h.pushBag(sess, player)
	h.pushPower(sess, player)
```

- [ ] **Step 2: 验证编译与测试**

Run:
```bash
cd server
go build ./... && go test ./...
```
Expected: 全部 `ok`。

- [ ] **Step 3: 提交**

```bash
git add server/internal/ws/handler.go
git commit -m "feat: push bag and power on login"
```

---

## Task 5: 客户端 Message.cs 扩展

**Files:**
- Modify: `client/Assets/Scripts/Net/Message.cs`

新增 equip/unequip 编码 + bag/power/equipment 解析。

- [ ] **Step 1: 在 Message.cs 追加类型常量与编码**

在 `Message` 静态类内追加：
```csharp
public const string TypeEquip = "equip";
public const string TypeUnequip = "unequip";
public const string TypeBag = "bag";
public const string TypePower = "power";

/// <summary>编码穿戴请求。</summary>
public static string EncodeEquip(string id, string uid)
{
    string dataJson = "{\"uid\":\"" + Escape(uid) + "\"}";
    return "{\"t\":\"" + TypeEquip + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
}

/// <summary>编码卸下请求。</summary>
public static string EncodeUnequip(string id, int slot)
{
    string dataJson = "{\"slot\":" + slot + "}";
    return "{\"t\":\"" + TypeUnequip + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
}
```

- [ ] **Step 2: 追加数据类**

在命名空间内追加：
```csharp
[Serializable]
public class EquipmentDTO
{
    public string uid;
    public string base_id;
    public string name;
    public int slot;
    public int rarity;
    public int upgrade;
    public AffixData[] affixes;
}

[Serializable]
public class AffixData
{
    public string type;
    public int tier;
    public float value;
}

[Serializable]
public class BagData
{
    public EquipmentDTO[] items;
}

[Serializable]
public class PowerData
{
    public float power;
}
```

- [ ] **Step 3: 验证编译**

在 Unity 编辑器重新编译（batchmode 或打开编辑器），Console 无红色错误。

- [ ] **Step 4: 提交**

```bash
git add client/Assets/Scripts/Net/Message.cs
git commit -m "feat: client message equip/unequip/bag/power"
```

---

## Task 6: 客户端 GameState 扩展

**Files:**
- Modify: `client/Assets/Scripts/State/GameState.cs`

扩展背包列表、已穿戴字典、战力，处理 bag/power 消息，提供 Equip/Unequip 方法。

- [ ] **Step 1: 改写 GameState.cs**

完整替换 `client/Assets/Scripts/State/GameState.cs`：
```csharp
using System.Collections.Generic;
using EquipmentIdle.Net;
using UnityEngine;

namespace EquipmentIdle.State
{
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        public event System.Action<SyncData> OnSyncReceived;
        public event System.Action<List<EquipmentDTO>> OnBagReceived;
        public event System.Action<float> OnPowerReceived;

        public string Account { get; private set; } = "";
        public int Floor { get; private set; } = 1;
        public int Souls { get; private set; } = 0;
        public List<string> Inventory { get; private set; } = new List<string>();
        public List<EquipmentDTO> Bag { get; private set; } = new List<EquipmentDTO>();
        public float Power { get; private set; } = 0;

        private WSClient _ws;
        private int _reqSeq = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _ws = gameObject.AddComponent<WSClient>();
            _ws.OnConnected += HandleConnected;
            _ws.OnMessage += HandleMessage;
        }

        public void ConnectAndLogin(string account)
        {
            Account = account;
            _ws.ConnectTo();
        }

        public void Equip(string uid)
        {
            _ws.SendText(Message.EncodeEquip("r" + (_reqSeq++), uid));
        }

        public void Unequip(int slot)
        {
            _ws.SendText(Message.EncodeUnequip("r" + (_reqSeq++), slot));
        }

        private void HandleConnected()
        {
            _ws.SendText(Message.EncodeLogin("r" + (_reqSeq++), Account));
        }

        private void HandleMessage(ParsedMessage msg)
        {
            switch (msg.t)
            {
                case Message.TypeSync:
                    var sd = JsonUtility.FromJson<SyncData>(msg.dataJson);
                    if (sd != null)
                    {
                        Account = sd.account ?? Account;
                        Floor = sd.floor != 0 ? sd.floor : Floor;
                        Souls = sd.souls;
                        OnSyncReceived?.Invoke(sd);
                    }
                    break;
                case Message.TypeBag:
                    var bag = JsonUtility.FromJson<BagData>(msg.dataJson);
                    if (bag != null && bag.items != null)
                    {
                        Bag = new List<EquipmentDTO>(bag.items);
                        OnBagReceived?.Invoke(Bag);
                    }
                    break;
                case Message.TypePower:
                    var pd = JsonUtility.FromJson<PowerData>(msg.dataJson);
                    if (pd != null)
                    {
                        Power = pd.power;
                        OnPowerReceived?.Invoke(Power);
                    }
                    break;
            }
        }

        public bool IsConnected => _ws != null && _ws.IsConnected;
    }
}
```

- [ ] **Step 2: 验证编译**

Unity 重新编译，Console 无红色错误。

- [ ] **Step 3: 提交**

```bash
git add client/Assets/Scripts/State/GameState.cs
git commit -m "feat: client gamestate with bag and power handling"
```

---

## Task 7: 客户端背包 UI 与一键穿戴

**Files:**
- Modify: `client/Assets/Scripts/UI/MainController.cs`

在 IMGUI 界面显示背包列表，每件装备一个"装备"按钮，点击穿戴。显示当前战力。

- [ ] **Step 1: 改写 MainController.cs**

完整替换 `client/Assets/Scripts/UI/MainController.cs`：
```csharp
using EquipmentIdle.Net;
using EquipmentIdle.State;
using System.Collections.Generic;
using UnityEngine;

namespace EquipmentIdle.UI
{
    public class MainController : MonoBehaviour
    {
        private string _accountInput = "";
        private string _statusText = "disconnected";
        private string _syncText = "(no sync yet)";
        private string _powerText = "power: 0";
        private GameState _gameState;
        private Vector2 _bagScroll;

        private void Start()
        {
            if (GameState.Instance == null)
            {
                var go = new GameObject("GameState");
                go.AddComponent<GameState>();
            }
            _gameState = GameState.Instance;
            _gameState.OnSyncReceived += OnSync;
            _gameState.OnBagReceived += OnBag;
            _gameState.OnPowerReceived += OnPower;
        }

        private void OnSync(SyncData data)
        {
            _statusText = "connected";
            _syncText = $"account={data.account} floor={data.floor} souls={data.souls}";
        }

        private void OnBag(List<EquipmentDTO> bag)
        {
            // UI 在 OnGUI 里直接读 GameState.Instance.Bag，无需缓存
        }

        private void OnPower(float power)
        {
            _powerText = $"power: {power:F1}";
        }

        private void OnGUI()
        {
            float w = 480f, h = 420f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.box);

            GUILayout.Label("EquipmentIdle - Stage 3");
            GUILayout.Space(4);
            GUILayout.Label("Status: " + _statusText);
            GUILayout.Label(_syncText);
            GUILayout.Label(_powerText);
            GUILayout.Space(6);

            GUILayout.Label("Account:");
            _accountInput = GUILayout.TextField(_accountInput);
            if (GUILayout.Button("Connect", GUILayout.Height(24)))
            {
                string acc = _accountInput.Trim();
                if (string.IsNullOrEmpty(acc)) acc = "hero";
                _statusText = "connecting...";
                _gameState.ConnectAndLogin(acc);
            }

            GUILayout.Space(8);
            GUILayout.Label("Backpack (" + _gameState.Bag.Count + " items):");

            _bagScroll = GUILayout.BeginScrollView(_bagScroll, GUILayout.Height(180));
            foreach (var eq in _gameState.Bag)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                string info = $"[{RarityName(eq.rarity)}] {eq.name} +{eq.Upgrade} ({SlotName(eq.slot)}) {eq.affixes?.Length ?? 0}aff";
                GUILayout.Label(info, GUILayout.Width(300));
                if (GUILayout.Button("Equip", GUILayout.Width(80)))
                {
                    _gameState.Equip(eq.uid);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private static string RarityName(int r)
        {
            switch (r)
            {
                case 0: return "普通";
                case 1: return "魔法";
                case 2: return "稀有";
                case 3: return "传奇";
                case 4: return "神器";
                default: return "?";
            }
        }

        private static string SlotName(int s)
        {
            string[] names = { "武器", "头盔", "护甲", "手套", "靴子", "戒指1", "戒指2", "项链" };
            if (s >= 0 && s < names.Length) return names[s];
            return "?";
        }
    }
}
```

- [ ] **Step 2: 验证编译**

Unity 重新编译，Console 无红色错误。

- [ ] **Step 3: 提交**

```bash
git add client/Assets/Scripts/UI/MainController.cs
git commit -m "feat: client backpack UI with one-click equip"
```

---

## Task 8: 端到端验证

**Files:**
- 无新文件

验证：服务端穿戴逻辑 + 客户端背包 UI 协同。由于客户端 UI 需手动操作，用服务端冒烟测试验证穿戴后战力变化。

- [ ] **Step 1: 写服务端穿戴冒烟测试**

Create `server/cmd/smokeequip/main.go`:
```go
package main

import (
	"fmt"
	"math/rand"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/inventory"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

func main() {
	p := model.NewPlayer("smoke")
	gen := loot.NewGenerator(rand.New(rand.NewSource(5)))
	drop := loot.NewDropTable(gen)

	// 掉落 5 件装备进背包
	for i := 0; i < 5; i++ {
		p.AddEquipment(drop.Drop(data.SlotWeapon, 10))
	}
	fmt.Printf("dropped %d items into bag\n", len(p.EquipBag))

	// 穿前战力
	before := combat.ComputePower(combat.AggregateStats(p.EquippedList()))
	fmt.Printf("power before equip: %.2f\n", before)

	// 穿第一件武器
	uid := p.EquipBag[0].UID
	if err := inventory.Equip(p, uid); err != nil {
		fmt.Println("FAIL equip:", err)
		return
	}
	after := combat.ComputePower(combat.AggregateStats(p.EquippedList()))
	fmt.Printf("power after equip weapon %s: %.2f\n", uid, after)

	if after <= before {
		fmt.Println("FAIL: power did not increase after equip")
		return
	}
	fmt.Println("SMOKE_OK: equip increases power")
}
```

- [ ] **Step 2: 运行冒烟测试**

Run:
```bash
cd server
go run ./cmd/smokeequip/
```
Expected: 输出 `SMOKE_OK: equip increases power`，战力从 0 提升到正值。

- [ ] **Step 3: 全量回归**

Run:
```bash
cd server
go test ./...
```
Expected: 全部 `ok`。

- [ ] **Step 4: 提交**

```bash
git add server/cmd/smokeequip/main.go
git commit -m "test: smoke test equip increases power"
```

---

## 验收标准

阶段 3 完成后须满足：
1. `cd server && go test ./...` 全部通过（含 inventory 包）
2. `inventory.Equip` 从背包穿戴到槽位，旧装备回背包；`Unequip` 反向
3. 客户端登录后收到 bag + power 推送
4. 客户端背包 UI 显示装备列表，点 Equip 发送穿戴请求
5. 服务端穿戴后回发新 bag + power，战力数值变化
6. 冒烟测试：穿戴后战力从 0 提升

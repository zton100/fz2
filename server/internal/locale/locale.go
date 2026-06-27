package locale

import (
	"encoding/json"
	"log"
	"os"
	"sync"
)

// L holds all user-facing text strings in the game.
// Zero values are filled from the built-in Chinese defaults at init time.
type L struct {
	// Rarity names (index = Rarity enum 0..4)
	RarityCommon    string `json:"rarity_common"`
	RarityMagic     string `json:"rarity_magic"`
	RarityRare      string `json:"rarity_rare"`
	RarityLegendary string `json:"rarity_legendary"`
	RarityArtifact  string `json:"rarity_artifact"`

	// Slot names (index = Slot enum 0..7)
	SlotWeapon string `json:"slot_weapon"`
	SlotHelmet string `json:"slot_helmet"`
	SlotArmor  string `json:"slot_armor"`
	SlotGloves string `json:"slot_gloves"`
	SlotBoots  string `json:"slot_boots"`
	SlotRing1  string `json:"slot_ring1"`
	SlotRing2  string `json:"slot_ring2"`
	SlotNeck   string `json:"slot_neck"`

	// Item base names (key = base_id)
	ItemIronSword    string `json:"item_iron_sword"`
	ItemLeatherHelm  string `json:"item_leather_helm"`
	ItemChainArmor   string `json:"item_chain_armor"`
	ItemLeatherGlove string `json:"item_leather_glove"`
	ItemLeatherBoot  string `json:"item_leather_boot"`
	ItemIronRing     string `json:"item_iron_ring"`
	ItemAmulet       string `json:"item_amulet"`

	// Monster names
	MonsterNormal string `json:"monster_normal"`
	MonsterBoss   string `json:"monster_boss"`

	// Craft / operation result messages
	MsgDecomposed     string `json:"msg_decomposed"`
	MsgComposed       string `json:"msg_composed"`
	MsgReforged       string `json:"msg_reforged"`
	MsgUpgraded       string `json:"msg_upgraded"`
	MsgUpgradeFailed  string `json:"msg_upgrade_failed"`
	MsgReincarnated   string `json:"msg_reincarnated"`
	MsgTalentUpgraded string `json:"msg_talent_upgraded"`
	MsgNotInBag       string `json:"msg_not_in_bag"`
	MsgBootstrap      string `json:"msg_bootstrap"`
}

// Built-in Chinese defaults.
var defaultZH = L{
	RarityCommon:    "普通",
	RarityMagic:     "魔法",
	RarityRare:      "稀有",
	RarityLegendary: "传奇",
	RarityArtifact:  "神器",

	SlotWeapon: "武器",
	SlotHelmet: "头盔",
	SlotArmor:  "护甲",
	SlotGloves: "手套",
	SlotBoots:  "靴子",
	SlotRing1:  "戒指1",
	SlotRing2:  "戒指2",
	SlotNeck:   "项链",

	ItemIronSword:    "铁剑",
	ItemLeatherHelm:  "皮盔",
	ItemChainArmor:   "锁甲",
	ItemLeatherGlove: "皮手套",
	ItemLeatherBoot:  "皮靴",
	ItemIronRing:     "铁戒",
	ItemAmulet:       "护符",

	MonsterNormal: "史莱姆",
	MonsterBoss:   "守层Boss",

	MsgDecomposed:     "decomposed",
	MsgComposed:       "composed",
	MsgReforged:       "reforged",
	MsgUpgraded:       "upgraded",
	MsgUpgradeFailed:  "upgrade failed (no degrade)",
	MsgReincarnated:   "reincarnated",
	MsgTalentUpgraded: "talent upgraded: %s",
	MsgNotInBag:       "equipment not in bag",
		MsgBootstrap:      "granted 10 base mats to new player",
}

var (
	current L
	mu      sync.RWMutex
)

func init() {
	current = defaultZH
}

// Current returns the active locale (thread-safe).
func Current() L {
	mu.RLock()
	defer mu.RUnlock()
	return current
}

// Set replaces the active locale atomically.
func Set(l L) {
	mu.Lock()
	defer mu.Unlock()
	current = l
}

// LoadJSON reads a locale JSON file and merges non-zero values into the
// active locale (Chinese defaults remain for any keys not in the file).
func LoadJSON(path string) error {
	data, err := os.ReadFile(path)
	if err != nil {
		return err
	}
	var override L
	if err := json.Unmarshal(data, &override); err != nil {
		return err
	}
	merged := defaultZH
	merge(&merged, &override)
	Set(merged)
	log.Printf("locale: loaded %s (%d bytes)", path, len(data))
	return nil
}

// merge copies non-zero string values from src into dst.
func merge(dst, src *L) {
	if src.RarityCommon != "" {
		dst.RarityCommon = src.RarityCommon
	}
	if src.RarityMagic != "" {
		dst.RarityMagic = src.RarityMagic
	}
	if src.RarityRare != "" {
		dst.RarityRare = src.RarityRare
	}
	if src.RarityLegendary != "" {
		dst.RarityLegendary = src.RarityLegendary
	}
	if src.RarityArtifact != "" {
		dst.RarityArtifact = src.RarityArtifact
	}
	if src.SlotWeapon != "" {
		dst.SlotWeapon = src.SlotWeapon
	}
	if src.SlotHelmet != "" {
		dst.SlotHelmet = src.SlotHelmet
	}
	if src.SlotArmor != "" {
		dst.SlotArmor = src.SlotArmor
	}
	if src.SlotGloves != "" {
		dst.SlotGloves = src.SlotGloves
	}
	if src.SlotBoots != "" {
		dst.SlotBoots = src.SlotBoots
	}
	if src.SlotRing1 != "" {
		dst.SlotRing1 = src.SlotRing1
	}
	if src.SlotRing2 != "" {
		dst.SlotRing2 = src.SlotRing2
	}
	if src.SlotNeck != "" {
		dst.SlotNeck = src.SlotNeck
	}
	if src.ItemIronSword != "" {
		dst.ItemIronSword = src.ItemIronSword
	}
	if src.ItemLeatherHelm != "" {
		dst.ItemLeatherHelm = src.ItemLeatherHelm
	}
	if src.ItemChainArmor != "" {
		dst.ItemChainArmor = src.ItemChainArmor
	}
	if src.ItemLeatherGlove != "" {
		dst.ItemLeatherGlove = src.ItemLeatherGlove
	}
	if src.ItemLeatherBoot != "" {
		dst.ItemLeatherBoot = src.ItemLeatherBoot
	}
	if src.ItemIronRing != "" {
		dst.ItemIronRing = src.ItemIronRing
	}
	if src.ItemAmulet != "" {
		dst.ItemAmulet = src.ItemAmulet
	}
	if src.MonsterNormal != "" {
		dst.MonsterNormal = src.MonsterNormal
	}
	if src.MonsterBoss != "" {
		dst.MonsterBoss = src.MonsterBoss
	}
	if src.MsgDecomposed != "" {
		dst.MsgDecomposed = src.MsgDecomposed
	}
	if src.MsgComposed != "" {
		dst.MsgComposed = src.MsgComposed
	}
	if src.MsgReforged != "" {
		dst.MsgReforged = src.MsgReforged
	}
	if src.MsgUpgraded != "" {
		dst.MsgUpgraded = src.MsgUpgraded
	}
	if src.MsgUpgradeFailed != "" {
		dst.MsgUpgradeFailed = src.MsgUpgradeFailed
	}
	if src.MsgReincarnated != "" {
		dst.MsgReincarnated = src.MsgReincarnated
	}
	if src.MsgTalentUpgraded != "" {
		dst.MsgTalentUpgraded = src.MsgTalentUpgraded
	}
	if src.MsgNotInBag != "" {
		dst.MsgNotInBag = src.MsgNotInBag
	}
}



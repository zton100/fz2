using System.Collections.Generic;
using EquipmentIdle.Net;
using UnityEngine;

namespace EquipmentIdle.State
{
    /// <summary>
    /// 全局状态单例。缓存服务端同步的状态，供 UI 读取。
    /// 挂在场景里一个 GameObject 上（DontDestroyOnLoad）。
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        public event System.Action<SyncData> OnSyncReceived;
        public event System.Action<List<EquipmentDTO>, List<EquipmentDTO>> OnBagReceived;
        public event System.Action<float> OnPowerReceived;
        public event System.Action<CombatData> OnCombatReceived;
        public event System.Action<EquipmentDTO> OnLootReceived;
        public event System.Action<int> OnFloorReceived;
        public event System.Action<Dictionary<string, int>> OnMaterialsReceived;
        public event System.Action<CraftResultData> OnCraftResult;
        public event System.Action<int, int, bool, Dictionary<string, int>> OnTalentsReceived;
        public event System.Action<OfflineResultData> OnOfflineResultReceived;

        public string Account { get; private set; } = "";
        public int Floor { get; private set; } = 1;
        public int FloorKills { get; private set; } = 0;
        public int MinionsTotal { get; private set; } = 3;
        public int EquipmentDataVersion { get; private set; } = 0;
        public int LegendaryDataVersion { get; private set; } = 0;
        public int ArtifactDataVersion { get; private set; } = 0;
        public int Souls { get; private set; } = 0;
        public int MaxFloor { get; private set; } = 1;
        public bool CanReincarn { get; private set; } = false;
        public List<string> Inventory { get; private set; } = new List<string>();
        public List<EquipmentDTO> Bag { get; private set; } = new List<EquipmentDTO>();
        public List<EquipmentDTO> Equipped { get; private set; } = new List<EquipmentDTO>();
        public float Power { get; private set; } = 0;
        public Dictionary<string, int> Materials { get; private set; } = new Dictionary<string, int>();
        public Dictionary<string, int> Talents { get; private set; } = new Dictionary<string, int>();
        public OfflineResultData LastOfflineResult { get; private set; }
        public CombatData LastCombat { get; private set; }

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

        public void Decompose(string uid)
        {
            _ws.SendText(Message.EncodeDecompose("r" + (_reqSeq++), uid));
        }

        public void Compose(int slot)
        {
            _ws.SendText(Message.EncodeCompose("r" + (_reqSeq++), slot));
        }

        public void Reforge(string uid)
        {
            _ws.SendText(Message.EncodeReforge("r" + (_reqSeq++), uid));
        }

        public void Upgrade(string uid)
        {
            _ws.SendText(Message.EncodeUpgrade("r" + (_reqSeq++), uid));
        }

        public void TransferUpgrade(string sourceUid, string targetUid)
        {
            _ws.SendText(Message.EncodeTransferUpgrade("r" + (_reqSeq++), sourceUid, targetUid));
        }

        public void LockEquipment(string uid, bool locked)
        {
            _ws.SendText(Message.EncodeLockEquipment("r" + (_reqSeq++), uid, locked));
        }

        public void Reincarn()
        {
            _ws.SendText(Message.EncodeReincarn("r" + (_reqSeq++)));
        }

        public void TalentUp(string name)
        {
            _ws.SendText(Message.EncodeTalentUp("r" + (_reqSeq++), name));
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
                        FloorKills = sd.floor_kills;
                        MinionsTotal = sd.minions_total > 0 ? sd.minions_total : MinionsTotal;
                        EquipmentDataVersion = sd.equipment_data_version;
                        LegendaryDataVersion = sd.legendary_data_version;
                        ArtifactDataVersion = sd.artifact_data_version;
                        Souls = sd.souls;
                        OnSyncReceived?.Invoke(sd);
                    }
                    break;
                case Message.TypeCombat:
                    var combat = JsonUtility.FromJson<CombatData>(msg.dataJson);
                    if (combat != null)
                    {
                        LastCombat = combat;
                        FloorKills = combat.floor_advanced ? 0 : combat.minions_killed;
                        if (combat.minions_total > 0) MinionsTotal = combat.minions_total;
                        OnCombatReceived?.Invoke(combat);
                    }
                    break;
                case Message.TypeBag:
                    var bag = JsonUtility.FromJson<BagData>(msg.dataJson);
                    if (bag != null)
                    {
                        Bag = bag.items != null ? new List<EquipmentDTO>(bag.items) : new List<EquipmentDTO>();
                        Equipped = bag.equipped != null ? new List<EquipmentDTO>(bag.equipped) : new List<EquipmentDTO>();
                        OnBagReceived?.Invoke(Bag, Equipped);
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
                case Message.TypeLoot:
                    var ld = JsonUtility.FromJson<LootData>(msg.dataJson);
                    if (ld != null)
                    {
                        var dto = new EquipmentDTO
                        {
                            uid = ld.uid,
                            base_id = ld.base_id,
                            legendary_id = ld.legendary_id,
                            artifact_id = ld.artifact_id,
                            legendary_description = ld.legendary_description,
                            artifact_description = ld.artifact_description,
                            legendary_bonuses = ld.legendary_bonuses,
                            artifact_bonuses = ld.artifact_bonuses,
                            legendary_power_bonus = ld.legendary_power_bonus,
                            boss_reward_bonus = ld.boss_reward_bonus,
                            artifact_trigger = ld.artifact_trigger,
                            artifact_value = ld.artifact_value,
                            name = ld.name,
                            slot = ld.slot,
                            rarity = ld.rarity,
                            upgrade = ld.upgrade,
                            affixes = ld.affixes,
                        };
                        OnLootReceived?.Invoke(dto);
                    }
                    break;
                case Message.TypeFloor:
                    var fd = JsonUtility.FromJson<FloorData>(msg.dataJson);
                    if (fd != null)
                    {
                        Floor = fd.floor;
                        OnFloorReceived?.Invoke(fd.floor);
                    }
                    break;
                case Message.TypeMaterials:
                    var md = JsonUtility.FromJson<MaterialsData>(msg.dataJson);
                    if (md != null) Materials = Message.MaterialsToDict(md.materials);
                    OnMaterialsReceived?.Invoke(Materials);
                    break;
                case Message.TypeCraftResult:
                    var cr = JsonUtility.FromJson<CraftResultData>(msg.dataJson);
                    if (cr != null) OnCraftResult?.Invoke(cr);
                    break;
                case Message.TypeTalents:
                    var td = JsonUtility.FromJson<TalentsData>(msg.dataJson);
                    if (td != null)
                    {
                        Souls = td.souls;
                        MaxFloor = td.max_floor;
                        CanReincarn = td.can_reincarn;
                        Talents = Message.TalentsToDict(td.talents);
                        OnTalentsReceived?.Invoke(Souls, MaxFloor, CanReincarn, Talents);
                    }
                    break;
                case Message.TypeOfflineResult:
                    var ord = JsonUtility.FromJson<OfflineResultData>(msg.dataJson);
                    if (ord != null)
                    {
                        LastOfflineResult = ord;
                        OnOfflineResultReceived?.Invoke(ord);
                    }
                    break;
            }
        }

        public bool IsConnected => _ws != null && _ws.IsConnected;
    }
}

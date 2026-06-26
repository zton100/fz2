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
        public event System.Action<List<EquipmentDTO>> OnBagReceived;
        public event System.Action<float> OnPowerReceived;
        public event System.Action<EquipmentDTO> OnLootReceived;
        public event System.Action<int> OnFloorReceived;
        public event System.Action<Dictionary<string, int>> OnMaterialsReceived;
        public event System.Action<CraftResultData> OnCraftResult;

        public string Account { get; private set; } = "";
        public int Floor { get; private set; } = 1;
        public int Souls { get; private set; } = 0;
        public List<string> Inventory { get; private set; } = new List<string>();
        public List<EquipmentDTO> Bag { get; private set; } = new List<EquipmentDTO>();
        public float Power { get; private set; } = 0;
        public Dictionary<string, int> Materials { get; private set; } = new Dictionary<string, int>();

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
                case Message.TypeLoot:
                    var ld = JsonUtility.FromJson<LootData>(msg.dataJson);
                    if (ld != null)
                    {
                        var dto = new EquipmentDTO
                        {
                            uid = ld.uid,
                            base_id = ld.base_id,
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
                    Materials = Message.ParseMaterials(msg.dataJson);
                    OnMaterialsReceived?.Invoke(Materials);
                    break;
                case Message.TypeCraftResult:
                    var cr = JsonUtility.FromJson<CraftResultData>(msg.dataJson);
                    if (cr != null) OnCraftResult?.Invoke(cr);
                    break;
            }
        }

        public bool IsConnected => _ws != null && _ws.IsConnected;
    }
}

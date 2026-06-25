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

        public string Account { get; private set; } = "";
        public int Floor { get; private set; } = 1;
        public int Souls { get; private set; } = 0;
        public List<string> Inventory { get; private set; } = new List<string>();

        private WSClient _ws;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _ws = gameObject.AddComponent<WSClient>();
            _ws.OnConnected += HandleConnected;
            _ws.OnMessage += HandleMessage;
        }

        /// <summary>连接服务端并用指定账号登录。</summary>
        public void ConnectAndLogin(string account)
        {
            Account = account;
            _ws.ConnectTo();
        }

        private void HandleConnected()
        {
            // 连上后立即发登录
            _ws.SendText(Message.EncodeLogin("r1", Account));
        }

        private void HandleMessage(ParsedMessage msg)
        {
            if (msg.t == Message.TypeSync)
            {
                var sd = JsonUtility.FromJson<SyncData>(msg.dataJson);
                if (sd == null) return;
                Account = sd.account ?? Account;
                Floor = sd.floor != 0 ? sd.floor : Floor;
                Souls = sd.souls;
                Inventory = sd.inventory != null ? new List<string>(sd.inventory) : Inventory;
                OnSyncReceived?.Invoke(sd);
            }
        }

        public bool IsConnected => _ws != null && _ws.IsConnected;
    }
}

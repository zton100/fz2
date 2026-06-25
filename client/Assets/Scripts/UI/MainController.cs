using EquipmentIdle.Net;
using EquipmentIdle.State;
using UnityEngine;

namespace EquipmentIdle.UI
{
    /// <summary>
    /// 主场景控制器：用 IMGUI(OnGUI) 画界面，零 UI 包依赖。
    /// 连接按钮、账号输入、状态与同步显示。
    /// </summary>
    public class MainController : MonoBehaviour
    {
        private string _accountInput = "";
        private string _statusText = "disconnected";
        private string _syncText = "(no sync yet)";
        private GameState _gameState;

        private void Start()
        {
            // 确保 GameState 单例存在
            if (GameState.Instance == null)
            {
                var go = new GameObject("GameState");
                go.AddComponent<GameState>();
            }
            _gameState = GameState.Instance;
            _gameState.OnSyncReceived += OnSync;
        }

        private void OnSync(SyncData data)
        {
            _statusText = "connected";
            _syncText = $"account={data.account} floor={data.floor} souls={data.souls}";
        }

        private void OnGUI()
        {
            // 居中面板
            float w = 360f, h = 200f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.box);

            GUILayout.Label("EquipmentIdle - Stage 0");
            GUILayout.Space(8);

            GUILayout.Label("Status: " + _statusText);
            GUILayout.Space(4);

            GUILayout.Label("Account:");
            _accountInput = GUILayout.TextField(_accountInput);
            GUILayout.Space(4);

            if (GUILayout.Button("Connect", GUILayout.Height(30)))
            {
                OnConnect();
            }

            GUILayout.Space(8);
            GUILayout.Label("Sync: " + _syncText);

            GUILayout.EndArea();
        }

        private void OnConnect()
        {
            string acc = _accountInput.Trim();
            if (string.IsNullOrEmpty(acc)) acc = "hero";
            _statusText = "connecting...";
            _gameState.ConnectAndLogin(acc);
        }
    }
}

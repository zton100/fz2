using System.IO;
using EquipmentIdle.Net;
using EquipmentIdle.State;
using UnityEngine;

namespace EquipmentIdle.UI
{
    /// <summary>
    /// 自动验证脚本：batchmode 下自动连接服务端并记录结果到文件。
    /// 仅用于自动化测试，正常游戏不挂载。
    /// 挂到场景后会自动连接默认账号，收到 sync 后写入 verify_result.txt 并退出。
    /// </summary>
    public class AutoVerifier : MonoBehaviour
    {
        public string account = "autohero";
        private float _timer;
        private bool _done;

        private void Start()
        {
            if (GameState.Instance == null)
            {
                var go = new GameObject("GameState");
                go.AddComponent<GameState>();
            }
            GameState.Instance.OnSyncReceived += OnSync;
            // 延迟 1 秒后连接，确保 WSClient 就绪
            Invoke(nameof(DoConnect), 1f);
        }

        private void DoConnect()
        {
            GameState.Instance.ConnectAndLogin(account);
        }

        private void OnSync(SyncData data)
        {
            if (_done) return;
            _done = true;
            string result = $"SYNC_OK account={data.account} floor={data.floor} souls={data.souls} inventory_len={(data.inventory != null ? data.inventory.Length : 0)}";
            File.WriteAllText("verify_result.txt", result);
            Debug.Log("[AutoVerifier] " + result);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            // 15 秒超时
            if (!_done && _timer > 15f)
            {
                string result = $"SYNC_FAIL timeout after 15s connected={GameState.Instance.IsConnected}";
                File.WriteAllText("verify_result.txt", result);
                Debug.Log("[AutoVerifier] " + result);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EquipmentIdle.Net
{
    /// <summary>
    /// WebSocket 客户端，使用 .NET 内置 ClientWebSocket，零第三方依赖。
    /// 接收在后台线程进行，收到的消息放入队列，在 Unity 主线程 Update 里分发。
    /// </summary>
    public class WSClient : MonoBehaviour
    {
        public event Action OnConnected;
        public event Action OnClosed;
        public event Action<ParsedMessage> OnMessage;

        private ClientWebSocket _ws;
        private string _url = "ws://localhost:8080/ws";
        private volatile bool _isOpen;
        private readonly ConcurrentQueue<ParsedMessage> _inbox = new ConcurrentQueue<ParsedMessage>();
        private readonly ConcurrentQueue<bool> _stateChanges = new ConcurrentQueue<bool>(); // true=opened,false=closed
        private CancellationTokenSource _cts;

        public bool IsConnected => _isOpen;

        public async void ConnectTo(string url = null)
        {
            if (url != null) _url = url;
            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();
            try
            {
                await _ws.ConnectAsync(new Uri(_url), _cts.Token);
                _isOpen = true;
                _stateChanges.Enqueue(true);
                _ = ReceiveLoop(_cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WS] connect error: {e.Message}");
                _isOpen = false;
                _stateChanges.Enqueue(false);
            }
        }

        public async void SendText(string text)
        {
            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(text);
                    await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, _cts.Token);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WS] send error: {e.Message}");
                }
            }
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buffer = new byte[8192];
            try
            {
                while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
                {
                    var sb = new StringBuilder();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(buffer, ct);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _isOpen = false;
                            _stateChanges.Enqueue(false);
                            return;
                        }
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    } while (!result.EndOfMessage);

                    var msg = Message.Parse(sb.ToString());
                    if (msg != null) _inbox.Enqueue(msg);
                }
            }
            catch (Exception e)
            {
                if (!ct.IsCancellationRequested)
                    Debug.LogWarning($"[WS] receive loop ended: {e.Message}");
            }
            finally
            {
                _isOpen = false;
                _stateChanges.Enqueue(false);
            }
        }

        private void Update()
        {
            // 主线程分发状态变更
            while (_stateChanges.TryDequeue(out bool opened))
            {
                if (opened) OnConnected?.Invoke();
                else OnClosed?.Invoke();
            }
            // 主线程分发消息
            while (_inbox.TryDequeue(out var msg))
            {
                OnMessage?.Invoke(msg);
            }
        }

        private async void OnApplicationQuit()
        {
            try
            {
                _cts?.Cancel();
                if (_ws != null && _ws.State == WebSocketState.Open)
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "quit", CancellationToken.None);
            }
            catch { }
        }
    }
}

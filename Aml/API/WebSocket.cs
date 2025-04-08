using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AbyssCLI.Aml.API
{
    public class WebSocket : IDisposable
    {
        private readonly ClientWebSocket _webSocket = new ClientWebSocket();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// An event triggered when a text message is received.
        /// </summary>
        public event Action<string>? MessageReceived;

        /// <summary>
        /// An event triggered when the WebSocket is closed or encounters an error.
        /// </summary>
        public event Action<Exception?>? Disconnected;

        /// <summary>
        /// Initiates a connection to the WebSocket server.
        /// </summary>
        public async Task ConnectAsync(string uri)
        {
            try
            {
                // If the socket is already running, do not reconnect
                if (_webSocket.State == WebSocketState.Open)
                    return;

                // Reset the internal state so you can re-use the object if needed
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();

                await _webSocket.ConnectAsync(new Uri(uri), _cancellationTokenSource.Token);

                // Start a background receive loop
                _ = Task.Run(ReceiveLoopAsync);
            }
            catch (Exception ex)
            {
                // Signal that we are disconnected if there's an error
                OnDisconnected(ex);
            }
        }

        /// <summary>
        /// Sends a text message to the WebSocket server.
        /// </summary>
        public async Task SendMessageAsync(string message)
        {
            if (_webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("The WebSocket is not connected.");

            var messageBuffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(messageBuffer),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Gracefully closes the connection.
        /// </summary>
        public async Task CloseAsync()
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Normal closure",
                        _cancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                OnDisconnected(ex);
            }
            finally
            {
                OnDisconnected(null);
            }
        }

        /// <summary>
        /// The main receive loop that continuously listens for new messages.
        /// </summary>
        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Server closed connection",
                            CancellationToken.None);
                        OnDisconnected(null);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        MessageReceived?.Invoke(message);
                    }
                }
            }
            catch (Exception ex)
            {
                // If we fail for any reason, close and report the error
                OnDisconnected(ex);
            }
        }

        private void OnDisconnected(Exception? ex)
        {
            try
            {
                if (_webSocket.State != WebSocketState.Closed &&
                    _webSocket.State != WebSocketState.Aborted)
                {
                    _webSocket.Abort();
                }
            }
            catch
            {
                // No-throw policy on teardown
            }

            Disconnected?.Invoke(ex);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _webSocket.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace PythonCommunication
{
    /// <summary>
    /// Enum representing different types of server messages that can be sent
    /// Corresponds with the python messages declaration
    /// </summary>
    public enum ServerMessages
    {
        SEND_MUSICXML = 1
    }


    public class PythonConnector : MonoBehaviour
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private ServerConfig _serverConfig;

        public void Start()
        {
            _serverConfig = ServerConfig.LoadConfig();
        }

        /// <summary>
        /// Sends a request to the Python generator server with optional parameters
        /// </summary>
        /// <param name="message">Type of server message to send</param>
        /// <param name="parameters">Optional dictionary of parameters for the request</param>
        /// <returns>Server response from the generator</returns>
        [ItemCanBeNull]
        public async Task<ServerResponse> SendRequestToGenerator(ServerMessages message, Dictionary<string, object> parameters = null)
        {
            if (!IsConnectedAndFunctional())
            {
                throw new InvalidOperationException("Not connected to the generator.");
            }
            var messageStr = message.ToString();
            var request = JsonConvert.SerializeObject(new ServerRequest
            {
                requestType = messageStr,
                parameters = parameters ?? new Dictionary<string, object>()
            });
            try
            {
                await SendDataAsync(request, 2_000); // 2-second timeout
                Debug.Log("Message sent to server.");

                // 5 minutes (CPU generation can be slow)
                var responseData = await ReceiveDataAsync(60 * 1000 * 5);
                Debug.Log($"Received response: status={responseData.statusCode}");
                return responseData;
            }
            catch (TimeoutException e)
            {
                Debug.LogError($"Timeout: {e.Message} {e.StackTrace}");
                CloseConnection();
                throw;
            }
            catch (SocketException e)
            {
                Debug.LogError($"Socket exception: {e.Message} {e.StackTrace}");
                CloseConnection();
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error: {e.Message} {e.StackTrace}");
                CloseConnection();
                throw;
            }
        }
    
        /// <summary>
        /// Establishes a connection to the Python server with a configurable timeout
        /// </summary>
        /// <param name="timeoutMs">Connection timeout in milliseconds</param>
        public async Task ConnectWithTimeout(int timeoutMs = 5_000)
        {
            if (_serverConfig == null)
                throw new NullReferenceException("Server config is invalid.");
            
            if (IsConnectedAndFunctional())
                return;

            CloseConnection();
            
            try
            {
                _client = new TcpClient();
                using (_client.CreateTimeoutScope(TimeSpan.FromMilliseconds(timeoutMs)))
                {
                    await _client.ConnectAsync(_serverConfig.host, _serverConfig.port);
                    _stream = _client.GetStream();
                }
            } catch (ObjectDisposedException)
            {
                CloseConnection();
                throw new TimeoutException("Timeout waiting for client to connect");
            }
            catch (Exception ex)
            {
                CloseConnection();
                throw new Exception($"Connection failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sends data to the server asynchronously with a specified timeout
        /// </summary>
        /// <param name="message">Message to be sent</param>
        /// <param name="timeoutMs">Maximum time allowed for sending data</param>
        private async Task SendDataAsync(string message, int timeoutMs)
        {
            if (_stream == null || !_stream.CanWrite)
                throw new InvalidOperationException("Network stream is not writable.");

            var data = Encoding.UTF8.GetBytes(message);

            using (var cts = new CancellationTokenSource(timeoutMs))
            {
                try
                {
                    await _stream.WriteAsync(data, 0, data.Length, cts.Token);
                    await _stream.FlushAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException($"Send operation timed out after {timeoutMs}ms");
                }
            }
        }
        
        /// <summary>
        /// Receives data from the server asynchronously with a specified timeout
        /// </summary>
        /// <param name="timeoutMs">Maximum time allowed for receiving data</param>
        /// <returns>Deserialized server response</returns>
        private async Task<ServerResponse> ReceiveDataAsync(int timeoutMs)
        {
            if (_stream == null || !_stream.CanRead)
                throw new InvalidOperationException("Network stream is not readable.");

            byte[] dataBuffer;
            try
            {
                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    cts.Token.Register(() => CloseConnection());
                    var lengthBytes = new byte[4];
                    await ReadWithTimeoutAsync(lengthBytes, 4, cts.Token);
                    var messageLength = BitConverter.ToInt32(lengthBytes, 0);
                    var contentLength = System.Net.IPAddress.NetworkToHostOrder(messageLength);
                    
                    if (contentLength <= 0)
                        throw new Exception($"Invalid message length: {contentLength}");
                    
                    dataBuffer = new byte[contentLength];
                    await ReadWithTimeoutAsync(dataBuffer, contentLength, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Receive operation timed out after {timeoutMs}ms");
            }

            string response = null;
            try
            {
                response = Encoding.UTF8.GetString(dataBuffer);
                return JsonConvert.DeserializeObject<ServerResponse>(response);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to parse JSON: {response}");
                throw new FormatException($"Invalid JSON response: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Read data of given length into provided buffer
        /// </summary>
        /// <param name="buffer">Buffer to store the data</param>
        /// <param name="bytesToRead">Length of the data to read</param>
        /// <param name="token">Cancellation token</param>
        private async Task ReadWithTimeoutAsync(byte[] buffer, int bytesToRead, CancellationToken token)
        {
            try
            {
                var totalRead = 0;
                while (totalRead < bytesToRead)
                {
                    token.ThrowIfCancellationRequested();
                    var bytesRead = await _stream.ReadAsync(buffer, totalRead, bytesToRead - totalRead, token);
                    if (bytesRead == 0)
                    {
                        CloseConnection();
                        throw new Exception("Connection closed unexpectedly.");
                    }
                    totalRead += bytesRead;
                }
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Receive operation timed out");
            }
        }
        
        /// <summary>
        /// Checks if the current network connection is active and functional
        /// </summary>
        /// <returns>True if connection is established and working, false otherwise</returns>
        private bool IsConnectedAndFunctional()
        {
            if (_client is not { Connected: true })
                return false;

            try
            {
                // Check if client is still connected
                if (_client.Client.Poll(0, SelectMode.SelectRead))
                {
                    var buffer = new byte[1];
                    if (_client.Client.Receive(buffer, SocketFlags.Peek) == 0)
                    {
                        // Client disconnected
                        CloseConnection();
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                CloseConnection();
                return false;
            }
        }
    
        /// <summary>
        /// Safely closes and disposes of the current network connection
        /// </summary>
        private void CloseConnection()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
                _client?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error closing connection: {e.Message}");
            }
            finally
            {
                _stream = null;
                _client = null;
            }
        }
        
        private void OnApplicationQuit()
        {
            CloseConnection();
            Debug.Log("Connection closed on application quit.");
        }

        private void OnDisable()
        {
            CloseConnection();
        }
    
        private void OnDestroy()
        {
            CloseConnection();
        }

    }
}
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

namespace GuanYao.Tool.Network
{
    public class UdpNetworkClient : MonoBehaviour
    {
        [Header("Network Settings")] public string serverIP = "127.0.0.1";
        public int serverPort = 8888;
        public int localPort = 8889;

        private UdpClient udpClient;
        private IPEndPoint serverEndPoint;
        private Thread receiveThread;
        public bool isRunning = false;

        // 消息队列（线程安全）
        private Queue<byte[]> receivedMessages = new Queue<byte[]>();
        private object queueLock = new object();

        // 事件回调
        public System.Action<NetworkTouchData> OnMessageReceived;
        public System.Action<string> OnStatusChanged;
        public System.Action<byte[]> OnRawDataReceived;

        // 连接状态
        public bool IsConnected => isRunning && udpClient != null;

        void Start()
        {
            // InitializeClient();
        }

        public void InitializeClient()
        {
            try
            {
                serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);

                // 创建UDP客户端并绑定到本地端口
                udpClient = new UdpClient(localPort);
                udpClient.Connect(serverEndPoint);

                UnityEngine.Debug.Log($"客户端启动，连接至服务器: {serverIP}:{serverPort}");
                OnStatusChanged?.Invoke($"已连接至服务器 - {serverIP}:{serverPort}");

                // 启动接收线程
                isRunning = true;
                receiveThread = new Thread(new ThreadStart(ReceiveData));
                receiveThread.IsBackground = true;
                receiveThread.Start();

            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"客户端初始化失败: {e.Message}");
                OnStatusChanged?.Invoke($"连接失败: {e.Message}");
            }
        }

        private void ReceiveData()
        {
            while (isRunning && udpClient != null)
            {
                try
                {
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = udpClient.Receive(ref endPoint);

                    // 验证消息来源是否为服务器
                    if (endPoint.Address.Equals(serverEndPoint.Address) && endPoint.Port == serverEndPoint.Port)
                    {
                        Debug.Log($"收到来自服务器的消息，数据大小: {receivedBytes.Length} 字节");

                        if (receivedBytes != null && receivedBytes.Length > 0)
                        {
                            lock (queueLock)
                            {
                                receivedMessages.Enqueue(receivedBytes);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"收到未知来源的消息: {endPoint.Address}:{endPoint.Port}");
                    }
                }
                catch (SocketException e)
                {
                    if (isRunning)
                    {
                        UnityEngine.Debug.LogWarning($"接收数据时发生Socket异常: {e.SocketErrorCode}");
                    }
                }
                catch (Exception e)
                {
                    if (isRunning)
                    {
                        UnityEngine.Debug.LogError($"接收数据时发生异常: {e.Message}");
                    }
                }
            }
        }

        void Update()
        {
            // 在主线程中处理接收到的消息
            ProcessReceivedMessages();
        }

        private void ProcessReceivedMessages()
        {
            lock (queueLock)
            {
                while (receivedMessages.Count > 0)
                {
                    byte[] messageData = receivedMessages.Dequeue();
                    HandleMessage(messageData);
                }
            }
        }

        private void HandleMessage(byte[] data)
        {
            try
            {
                // 触发原始数据接收事件
                OnRawDataReceived?.Invoke(data);

                NetworkTouchData message = ProtoBufSerializer.Deserialize<NetworkTouchData>(data);
                if (message != null)
                {
                    // 确保在主线程中执行回调
                    MainThreadDispatcher.Execute(() =>
                    {
                        OnMessageReceived?.Invoke(message);
                        // Debug.Log($"收到触摸数据 - 索引: {message.index}," +
                        //           $" 方向: {message.screenOrientation}," +
                        //           $" 触摸点数量: {message.touches?.Count ?? 0}"+
                        //           $" 消息内容: {message.keyboardData}");
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"处理消息失败: {e.Message}");
            }
        }

        // 发送触摸数据
        public void SendTouchData(NetworkTouchData touchData)
        {
            if (udpClient == null || !isRunning)
            {
                Debug.LogWarning("客户端未连接，无法发送消息");
                return;
            }

            try
            {
                byte[] serializedData = ProtoBufSerializer.Serialize(touchData);
                if (serializedData != null)
                {
                    udpClient.Send(serializedData, serializedData.Length);
                    Debug.Log($"发送触摸数据成功，数据大小: {serializedData.Length} 字节");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"发送消息失败: {e.Message}");
            }
        }

        // 发送触摸数据
        public void SendTouchData(MathematicalData touchData)
        {
            if (udpClient == null || !isRunning)
            {
                Debug.LogWarning("客户端未连接，无法发送消息");
                return;
            }

            try
            {
                byte[] serializedData = ProtoBufSerializer.Serialize(touchData);
                if (serializedData != null)
                {
                    udpClient.Send(serializedData, serializedData.Length);
                    Debug.Log($"发送触摸数据成功，数据大小: {serializedData.Length} 字节");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"发送消息失败: {e.Message}");
            }
        }

        // 重新连接服务器
        public void Reconnect(string newServerIP = null, int newServerPort = 0)
        {
            if (!string.IsNullOrEmpty(newServerIP))
                serverIP = newServerIP;
            if (newServerPort > 0)
                serverPort = newServerPort;

            CloseClient();
            InitializeClient();
        }

        void OnDestroy()
        {
            CloseClient();
        }

        void OnApplicationQuit()
        {
            CloseClient();
        }

        private void CloseClient()
        {
            isRunning = false;

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Abort();
                receiveThread = null;
            }

            if (udpClient != null)
            {
                udpClient.Close();
                udpClient = null;
            }

            UnityEngine.Debug.Log("客户端已关闭");
        }
    }
}
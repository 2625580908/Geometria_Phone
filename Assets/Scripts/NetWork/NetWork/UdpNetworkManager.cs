using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;
using GuanYao.Tool.Network;

public class UdpNetworkManager : MonoBehaviour
{
    [Header("Network Settings")]
    public string remoteIP = "127.0.0.1";
    public int remotePort = 8888;
    public int localPort = 8889;
    public bool isServer = false;

    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private Thread receiveThread;
    public bool isRunning = false;
    
    // 消息队列（线程安全）
    private Queue<byte[]> receivedMessages = new Queue<byte[]>();
    private object queueLock = new object();

    // 事件回调
    public System.Action<NetworkTouchData> OnMessageReceived;
    public System.Action<string> OnStatusChanged;

    // 在UdpNetworkManager类中添加这些内容
    public System.Action<byte[], NetworkTouchData> OnRawDataSent;
    public System.Action<byte[]> OnRawDataReceived;
    
    void Start()
    {
        InitializeNetwork();
    }

    void InitializeNetwork()
    {
        try
        {
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
            
            if (isServer)
            {
                udpClient = new UdpClient(localPort);
                UnityEngine.Debug.Log($"服务器启动，监听端口: {localPort}");
                OnStatusChanged?.Invoke($"服务器启动 - 端口: {localPort}");
            }
            else
            {
                udpClient = new UdpClient();
                UnityEngine.Debug.Log($"客户端启动，连接至: {remoteIP}:{remotePort}");
                OnStatusChanged?.Invoke($"客户端连接 - {remoteIP}:{remotePort}");
            }

            // 启动接收线程
            isRunning = true;
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();

        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"网络初始化失败: {e.Message}");
            OnStatusChanged?.Invoke($"连接失败: {e.Message}");
        }
    }

    private void ReceiveData()
    {
        while (isRunning && udpClient != null)
        {
            try
            {
                // 正确的写法：端口设为0，让Receive方法自动填充
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = udpClient.Receive(ref endPoint);
            
                Debug.Log($"收到来自 {endPoint.Address}:{endPoint.Port} 的消息");
                Debug.Log($"数据大小: {receivedBytes.Length} 字节");
                
                if (receivedBytes != null && receivedBytes.Length > 0)
                {
                    lock (queueLock)
                    {
                        receivedMessages.Enqueue(receivedBytes);
                    }
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

    private int index = 0;
    void Update()
    {
        // 在主线程中处理接收到的消息
        ProcessReceivedMessages();
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NetworkTouchData message = new NetworkTouchData();
            message.index = index;
            index++;
            message.screenOrientation = (int)Screen.orientation;
            message.keyboardData = "ABC";
            // message.touches = new List<TouchData>();
            // foreach (Touch touch in Input.touches)
            // {
            //     TouchData touchData = new TouchData();
            //     touchData.fingerId = touch.fingerId;
            //     touchData.touchPos = touch.position;
            //     touchData.deltaPosition = touch.deltaPosition;
            //     touchData.touchPhase = touch.phase;
            //     touchData.isTap = touch.tapCount > 0;
            //     touchData.pressed = touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved;
            // }
            SendMessage(message);
        }
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

    // 修改HandleMessage方法
    private void HandleMessage(byte[] data)
    {
        try
        {
            // 触发原始数据接收事件
            OnRawDataReceived?.Invoke(data);
        
            NetworkTouchData message = ProtoBufSerializer.Deserialize<NetworkTouchData>(data);
            if (message != null)
            {
                MainThreadDispatcher.Execute(() => {
                    OnMessageReceived?.Invoke(message);
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"处理消息失败: {e.Message}");
        }
    }

    // 修改SendMessage方法
    public void SendMessage(NetworkTouchData message)
    {
        if (udpClient == null || !isRunning) return;

        try
        {
            byte[] serializedData = ProtoBufSerializer.Serialize(message);
            if (serializedData != null)
            {
                udpClient.Send(serializedData, serializedData.Length, remoteEndPoint);
                Debug.Log("AAA:" + serializedData.Length);
                // 触发原始数据发送事件
                OnRawDataSent?.Invoke(serializedData, message);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"发送消息失败: {e.Message}");
        }
    }

    void OnDestroy()
    {
        CloseNetwork();
    }

    void OnApplicationQuit()
    {
        CloseNetwork();
    }

    private void CloseNetwork()
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

        UnityEngine.Debug.Log("网络连接已关闭");
    }
    
}
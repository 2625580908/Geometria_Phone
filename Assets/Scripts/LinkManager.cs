using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using GuanYao.Tool.Network;
using GuanYao.Tool.Singleton;
using UnityEngine;
using UnityEngine.UI;
public class LinkManager : SingletonMono<LinkManager>
{
    public InputField Ip;
    public Button Link;
    public Button SendTest;
    
    public List<Sprite> LinkSpritesStatus;
    private UdpNetworkClient udpClient;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        udpClient = transform.GetComponent<UdpNetworkClient>();
        Ip.text = GetWirelessIPAddress();
#if UNITY_EDITOR
       
#endif
       
        Link.onClick.AddListener(() =>
        {
            if (!udpClient.isRunning)
            {
                udpClient.serverIP = Ip.text;
                udpClient.serverPort = 8889;
                udpClient.InitializeClient();
            }

            if (udpClient.isRunning && LinkSpritesStatus != null)
            {
                Link.GetComponent<Image>().sprite = LinkSpritesStatus[1];
                SendRokidDevicesInit();
            }
              
        });
        
        SendTest.onClick.AddListener(() =>
        {
            SendRokidDevices();
        });
    }
    
    public string GetWirelessIPAddress()
    {
        string ipAddress = "";
        
        // 获取所有网络接口
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        
        foreach (NetworkInterface ni in interfaces)
        {
            // 检查是否为无线网络适配器且状态为启用
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && 
                ni.OperationalStatus == OperationalStatus.Up)
            {
                // 获取IP属性
                IPInterfaceProperties ipProps = ni.GetIPProperties();
                
                // 遍历所有单播地址
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                {
                    // 只获取IPv4地址
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress = addr.Address.ToString();
                        Debug.Log("无线网络IP地址: " + ipAddress);
                        return ipAddress;
                    }
                }
            }
        }
        
        Debug.LogWarning(ipAddress);
        return ipAddress;
    }
    
    
    // Update is called once per frame
    void Update()
    {
    }

    private int index = 0;
    public MathematicalData message;
    
    /// <summary>
    /// 发生数据函数
    /// </summary>
    public void SendRokidDevices()
    {
        if (udpClient != null && udpClient.isRunning)
        {
            MathematicalData message = new MathematicalData();
            message.index = index++;
            message.nowTime = DateTime.Now.ToString();
            message.screenOrientation = (int)Screen.orientation;
            message.mathematicalFunctionList = MainManager.Instance.mathematicalFunctions.ToArray();
            udpClient.SendTouchData(message);
        }
    }
    
    /// <summary>
    /// 发生数据函数
    /// </summary>
    public void SendRokidDevicesInit()
    {
        MathematicalData message = new MathematicalData();
        message.index = index++;
        message.nowTime = DateTime.Now.ToString();
        message.screenOrientation = (int)Screen.orientation;
        message.mathematicalFunctionList = new MathematicalFunction[]{};
        udpClient.SendTouchData(message);
    }
}

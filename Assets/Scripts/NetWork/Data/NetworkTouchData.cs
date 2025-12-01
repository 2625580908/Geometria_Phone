// NetworkMessage.cs
using ProtoBuf;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[ProtoContract]
public class NetworkTouchData
{
    [ProtoMember(1)]
    public long index;
    
    [ProtoMember(2)]
    public string nowTime;
    
    [ProtoMember(3)]
    public int screenOrientation;
    
    [ProtoMember(4)]
    public string keyboardData;
    
    [ProtoMember(5)]
    public TouchData[] touches;
}

[Serializable]
[ProtoContract]
public class TouchData
{
    [ProtoMember(1)]
    public int fingerId;
    
    [ProtoMember(2)]
    public Vector2Serializable touchPos;
    
    [ProtoMember(3)]
    public Vector2Serializable deltaPosition;
    
    [ProtoMember(4)]
    public int touchPhase;  // 使用int而不是TouchPhase
    
    // 便捷属性，用于转换TouchPhase
    [ProtoIgnore]
    public TouchPhase TouchPhase
    {
        get => (TouchPhase)touchPhase;
        set => touchPhase = (int)value;
    }
}

// 可序列化的Vector2包装类
[Serializable]
[ProtoContract]
public class Vector2Serializable
{
    [ProtoMember(1)]
    public float x;
    
    [ProtoMember(2)]
    public float y;
    
    public Vector2Serializable() { }
    
    public Vector2Serializable(Vector2 vector)
    {
        x = vector.x;
        y = vector.y;
    }
    
    public Vector2Serializable(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    
    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
    
    public static implicit operator Vector2(Vector2Serializable v) => v.ToVector2();
    public static implicit operator Vector2Serializable(Vector2 v) => new Vector2Serializable(v);
}

[Serializable]
[ProtoContract]
public class NetworkKeyboardData
{
    [ProtoMember(1)]  // 需要添加ProtoMember
    public string key;
}
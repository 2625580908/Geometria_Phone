using UnityEngine;
using ProtoBuf;
using System;

namespace GuanYao.Tool.Network
{
    [Serializable]
    [ProtoContract]
    public class MathematicalData
    {
        [ProtoMember(1)] public int index;

        [ProtoMember(2)] public string nowTime;

        [ProtoMember(3)] public int screenOrientation;

        [ProtoMember(4)] public MathematicalFunction[] mathematicalFunctionList;
    }

    [Serializable]
    [ProtoContract]
    public class MathematicalFunction
    {
        [ProtoMember(1)] public int index;

        [ProtoMember(2)] public bool activate;

        [ProtoMember(3)] public RGBA color;

        [ProtoMember(4)] public string formula;
    }

    [Serializable]
    [ProtoContract]
    public class RGBA
    {
        [ProtoMember(1)] public float r;

        [ProtoMember(2)] public float g;

        [ProtoMember(3)] public float b;

        [ProtoMember(4)] public float a;
    }
}
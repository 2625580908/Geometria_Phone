using System.IO;
using ProtoBuf;

namespace GuanYao.Tool.Network
{
    public static class ProtoBufSerializer
    {
        public static byte[] Serialize<T>(T obj)
        {
            if (obj == null) return null;

            try
            {
                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(stream, obj);
                    return stream.ToArray();
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"ProtoBuf序列化失败: {e.Message}");
                return null;
            }
        }

        public static T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0) return default(T);

            try
            {
                using (var stream = new MemoryStream(data))
                {
                    return Serializer.Deserialize<T>(stream);
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"ProtoBuf反序列化失败: {e.Message}");
                return default(T);
            }
        }
    }
}
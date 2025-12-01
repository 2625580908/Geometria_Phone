using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuanYao.Tool.Network
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher instance;
        private static readonly Queue<Action> executionQueue = new Queue<Action>();

        public static MainThreadDispatcher Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("MainThreadDispatcher");
                    instance = obj.AddComponent<MainThreadDispatcher>();
                    DontDestroyOnLoad(obj);
                }

                return instance;
            }
        }

        public static void Execute(Action action)
        {
            if (action == null) return;

            lock (executionQueue)
            {
                executionQueue.Enqueue(action);
            }
        }

        void Update()
        {
            lock (executionQueue)
            {
                while (executionQueue.Count > 0)
                {
                    executionQueue.Dequeue()?.Invoke();
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            // 确保在游戏启动时创建实例
            var _ = Instance;
        }
    }
}
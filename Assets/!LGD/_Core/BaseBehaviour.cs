using LGD.Core.Events;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LGD.Core
{
    public class BaseBehaviour : SerializedMonoBehaviour
    {
        protected virtual void OnEnable()
        {
            ServiceBus.RegisterInstance(this);
            RequestBus.RegisterInstance(this);
        }

        protected virtual void OnDisable()
        {
            ServiceBus.UnregisterInstance(this);
            RequestBus.UnregisterInstance(this);
        }

        public void Publish(string topic, params object[] args)
        {
            DebugManager.Log("[Core] Publishing: " + topic);
            ServiceBus.Publish(topic, this, args);
        }

        public T Request<T>(string requestType, params object[] args)
        {
            return RequestBus.Request<T>(requestType, this, args);
        }

        public T[] RequestAll<T>(string requestType, params object[] args)
        {
            return RequestBus.RequestAll<T>(requestType, this, args).ToArray();
        }
    }
}
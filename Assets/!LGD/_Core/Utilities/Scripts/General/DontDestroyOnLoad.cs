using System.Collections.Generic;
using UnityEngine;

namespace LGD.Utilities.General
{
    public class DontDestroyOnLoad : MonoBehaviour
    {
        [SerializeField] private bool _destroyDuplicates = true;
        [SerializeField] private string _instanceID = "Default";

        private static Dictionary<string, DontDestroyOnLoad> _instances = new Dictionary<string, DontDestroyOnLoad>();

        private void Awake()
        {
            if (_destroyDuplicates)
            {
                if (TryRegisterInstance())
                {
                    DontDestroyOnLoad(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private bool TryRegisterInstance()
        {
            if (_instances.ContainsKey(_instanceID))
            {
                DebugManager.Warning($"[Core] [DontDestroyOnLoad] Duplicate instance of '{_instanceID}' detected. Destroying new instance.");
                return false;
            }

            _instances[_instanceID] = this;
            return true;
        }

        private void OnDestroy()
        {
            if (_destroyDuplicates && _instances.ContainsKey(_instanceID) && _instances[_instanceID] == this)
            {
                _instances.Remove(_instanceID);
            }
        }
    }
}
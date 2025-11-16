using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using LGD.Core.Singleton;

#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
#endif

namespace PoolSystem
{
    public class PoolingManager : MonoSingleton<PoolingManager>
    {
        [SerializeField, FoldoutGroup("Pool Settings")]
        private List<Pool> _pools = new List<Pool>();

        private Dictionary<string, Pool> _poolDictionary;

        private void Start()
        {
            InitializePools();
        }

        private void InitializePools()
        {
            _poolDictionary = new Dictionary<string, Pool>();

            foreach (Pool pool in _pools)
            {
                if (_poolDictionary.ContainsKey(pool.GetKey()))
                {
                    DebugManager.Error($"[Polling] [PoolingManager] Duplicate pool key detected: {pool.GetKey()}");
                    continue;
                }

                pool.Initialize(transform);
                _poolDictionary.Add(pool.GetKey(), pool);
            }
        }

        /// <summary>
        /// Get a GameObject from the pool with optional component retrieval
        /// </summary>
        public static T Get<T>(string key, Transform newParent = null) where T : Component
        {
            GameObject obj = Get(key, newParent);
            if (obj == null)
                return null;

            T component = obj.GetComponent<T>();
            if (component == null)
            {
                DebugManager.Error($"[Polling] [PoolingManager] GameObject from pool '{key}' does not have component of type {typeof(T).Name}");
                return null;
            }

            return component;
        }

        /// <summary>
        /// Get a GameObject from the pool
        /// </summary>
        public static GameObject Get(string key, Transform newParent = null)
        {
            if (Instance == null)
            {
                DebugManager.Error("[Polling] [PoolingManager] Instance is null. Make sure PoolingManager exists in the scene.");
                return null;
            }

            if (!Instance._poolDictionary.ContainsKey(key))
            {
                DebugManager.Error($"[Polling] [PoolingManager] Pool with key '{key}' does not exist.");
                return null;
            }

            Pool pool = Instance._poolDictionary[key];
            return pool.GetObject(newParent);
        }

        /// <summary>
        /// Return a GameObject to the pool
        /// </summary>
        public static void Return(GameObject obj, string key, bool deactivate = true)
        {
            if (Instance == null)
            {
                DebugManager.Error("[Polling] [PoolingManager] Instance is null. Make sure PoolingManager exists in the scene.");
                return;
            }

            if (!Instance._poolDictionary.ContainsKey(key))
            {
                DebugManager.Error($"[Polling] [PoolingManager] Pool with key '{key}' does not exist.");
                return;
            }

            Pool pool = Instance._poolDictionary[key];
            pool.ReturnObject(obj, deactivate);
        }

#if UNITY_EDITOR
        [Button("Generate Pool Keys"), FoldoutGroup("Pool Settings")]
        private void GeneratePoolKeys()
        {
            if (_pools == null || _pools.Count == 0)
            {
                DebugManager.Warning("[Polling] [PoolingManager] No pools defined. Cannot generate PoolKeys.");
                return;
            }

            // Build the content for the generated file
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// Auto-generated file. Do not modify manually.");
            sb.AppendLine("namespace PoolSystem");
            sb.AppendLine("{");
            sb.AppendLine("    public static class PoolKeys");
            sb.AppendLine("    {");

            HashSet<string> usedKeys = new HashSet<string>();
            foreach (Pool pool in _pools)
            {
                string key = pool.GetKey();

                if (string.IsNullOrEmpty(key))
                {
                    DebugManager.Warning("[Polling] [PoolingManager] Found pool with empty key. Skipping.");
                    continue;
                }

                if (usedKeys.Contains(key))
                {
                    DebugManager.Warning($"[Polling] [PoolingManager] Duplicate key '{key}' found. Skipping duplicate.");
                    continue;
                }

                usedKeys.Add(key);

                // Convert key to POOLEDITEM_SCREAMING_SNAKE format
                string constName = "POOLEDITEM_" + MakeScreamingSnakeCase(key);
                // Convert key value to kebab-case
                string keyValue = MakeKebabCase(key);
                sb.AppendLine($"        public const string {constName} = \"{keyValue}\";");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // Define the output directory and file path
            string targetDir = "Assets/!LGD/Systems/PoolSystem";
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
            string filePath = Path.Combine(targetDir, "PoolKeys.cs");

            File.WriteAllText(filePath, sb.ToString());
            DebugManager.Log($"[Polling] [PoolingManager] PoolKeys generated at: {filePath}");

            // Refresh the AssetDatabase to show the new file in Unity
            AssetDatabase.Refresh();
        }

        private string MakeScreamingSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "INVALID_KEY";

            StringBuilder result = new StringBuilder();
            bool lastWasLetterOrDigit = false;

            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c))
                {
                    result.Append(char.ToUpper(c));
                    lastWasLetterOrDigit = true;
                }
                else if (lastWasLetterOrDigit)
                {
                    result.Append('_');
                    lastWasLetterOrDigit = false;
                }
            }

            // Remove trailing underscore if exists
            string final = result.ToString().TrimEnd('_');

            // Ensure it starts with a letter
            if (final.Length > 0 && char.IsDigit(final[0]))
            {
                final = "_" + final;
            }

            return final.Length > 0 ? final : "INVALID_KEY";
        }

        private string MakeKebabCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "invalid-key";

            StringBuilder result = new StringBuilder();
            bool lastWasLetterOrDigit = false;

            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c))
                {
                    result.Append(char.ToLower(c));
                    lastWasLetterOrDigit = true;
                }
                else if (lastWasLetterOrDigit)
                {
                    result.Append('-');
                    lastWasLetterOrDigit = false;
                }
            }

            // Remove trailing dash if exists
            return result.ToString().TrimEnd('-');
        }
#endif
    }
}
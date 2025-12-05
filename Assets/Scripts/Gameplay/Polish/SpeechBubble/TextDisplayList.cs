using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;

namespace Gameplay.Polish
{
    public class TextDisplayList : MonoBehaviour
    {
        [FoldoutGroup("References")]
        [SerializeField] private TextMeshProUGUI _textComponent;

        [FoldoutGroup("Settings")]
        [SerializeField] private string[] _textMessages;

        [Button]
        public void DisplayRandomText()
        {
            if (_textMessages == null || _textMessages.Length == 0)
            {
                return;
            }

            if (_textComponent == null)
            {
                return;
            }

            int randomIndex = Random.Range(0, _textMessages.Length);
            _textComponent.text = _textMessages[randomIndex];
        }

        public string GetRandomText()
        {
            if (_textMessages == null || _textMessages.Length == 0)
            {
                return string.Empty;
            }

            int randomIndex = Random.Range(0, _textMessages.Length);
            return _textMessages[randomIndex];
        }

        public void ClearText()
        {
            if (_textComponent != null)
            {
                _textComponent.text = string.Empty;
            }
        }
    }
}
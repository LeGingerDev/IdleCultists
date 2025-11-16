using UnityEngine;

namespace LGD.SceneManagement.Data
{
    [CreateAssetMenu(fileName = "ColourTemplate_[NAME]", menuName = "OTBG/Customisation/Colour/Create Colour Template")]
    public class ColorTemplate : ScriptableObject
    {
        public Sprite backgroundSprite;
        public Color highlightColour = Color.white;
    }
}
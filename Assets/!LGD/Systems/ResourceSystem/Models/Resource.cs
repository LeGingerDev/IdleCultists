using Sirenix.OdinInspector;
using UnityEngine;
namespace LGD.ResourceSystem.Models
{
    [CreateAssetMenu(fileName = "Resource_[NAME]", menuName = "BagOfDucks/Resources/Create Resource")]
    public class Resource : ScriptableObject
    {
        public string id;
        public string displayName;
        public string description;
        [PreviewField] public Sprite icon;
        public Color colorAssociation = Color.white;
        public PhysicalResource physicalResourceAssociation;
        public string resourceSpriteName;

        public string GetResourceSpriteText()
        {
            return $"<sprite name={resourceSpriteName}>";
        }
    }
}
using System;
using UnityEngine;
namespace LGD.Utilities.Attributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class SpriteSortingLayerAttribute : PropertyAttribute
    {
    }
}
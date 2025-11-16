using UnityEngine;
namespace LGD.InteractionSystem
{
    public static class InteractionEventIds
    {
        public const string ON_INTERACTION_HOVER_ENTERED = "Interaction.HoverEntered";
        public const string ON_INTERACTION_HOVER_EXITED = "Interaction.HoverExited";
        public const string ON_INTERACTION_HOVER_CLEARED = "Interaction.HoverCleared";

        public const string ON_INTERACTABLE_CLICKED = "Interaction.Clicked";
        public const string ON_WORLD_CLICKED_EMPTY = "Interaction.ClickedEmpty";

        public const string ON_MOUSE_DOWN = "Interaction.MouseDown";
        public const string ON_MOUSE_DOWN_EMPTY = "Interaction.MouseDownEmpty";
        public const string ON_MOUSE_UP = "Interaction.MouseUp";
        public const string ON_MOUSE_UP_EMPTY = "Interaction.MouseUpEmpty";

        public const string ON_INTERACTION_DRAGGING = "Interaction.Dragging";
    }
}
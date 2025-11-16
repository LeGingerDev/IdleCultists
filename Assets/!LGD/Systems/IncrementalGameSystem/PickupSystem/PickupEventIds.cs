namespace LGD.PickupSystem
{
    public static class PickupEventIds
    {
        public const string ON_ENTITY_PICKED_UP = "entity-picked-up";
        public const string ON_ENTITY_DROPPED = "entity-dropped";
        public const string ON_ENTITY_INVALID_DROP = "entity-invalid-drop";
        public const string ON_ENTITY_ASSIGNED_TO_ZONE = "entity-assigned-to-zone";
        public const string ON_ENTITY_REMOVED_FROM_ZONE = "entity-removed-from-zone";
        public const string ON_ENTITY_RECONNECTED_TO_ZONE = "entity-reconnected-to-zone"; // For save/load restoration - visual/audio only
        public const string ON_ENTITY_RETURNED_TO_ZONE = "entity-returned-to-zone"; // For invalid drop return - no animations
    }
}
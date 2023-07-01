namespace DesertImage.ECS
{
    public static class ComponentTools
    {
        private static uint _typesIdCounter;

        public static uint GetComponentId<T>()
        {
            var id = ComponentTypes<T>.TypeId;

            if (id >= 0) return id;

            id = ++_typesIdCounter;

            ComponentTypes<T>.TypeId = id;

            return id;
        }
    }
}
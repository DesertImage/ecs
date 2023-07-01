namespace DesertImage.ECS
{
    public static class Worlds
    {
        public static World Current;

        private static uint _idCounter;

        public static World Create()
        {
            var world = new World(++_idCounter);
            Current = world;
            return world;
        }
    }
}
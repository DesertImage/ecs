namespace DesertImage.ECS
{
    public class ComponentsStorageBase
    {
    }

    public class ComponentsStorage<T> : ComponentsStorageBase where T : unmanaged
    {
        public readonly NativeSparseSet<T> Data;

        public ComponentsStorage(int denseCapacity, int sparseCapacity)
        {
            Data = new NativeSparseSet<T>(denseCapacity, sparseCapacity);
        }
    }
}
using UnityEngine;

namespace DesertImage.ECS
{
    public static class ObjectsReferenceRegistry
    {
        private static ObjectReferenceStorage _data;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Initialize() => _data = default;

        public static ObjectReferenceStorage GetStorage()
        {
            if (!_data.IsNotNull)
            {
                _data = new ObjectReferenceStorage(20);
            }

            return _data;
        }
    }
}
using DesertImage.ECS;
using DesertImage.Pools;
using UnityEngine.SceneManagement;

namespace DesertImage
{
    public static class ComponentsTool
    {
        private static ComplexPool Pool => _pool ??= Core.Instance?.Get<ComplexPool>();

        private static ComplexPool _pool;

        static ComponentsTool()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //reset instance when going to another scene
            _pool = null;
        }

        /// <summary>
        /// Get new component instance from pool
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public static TComponent GetInstanceFromPool<TComponent>() where TComponent : new()
        {
            return Pool != null ? Pool.GetInstance<TComponent>() : new TComponent();
        }

        public static void ReturnToPool<TComponent>(TComponent instance) where TComponent : class
        {
            Pool?.ReturnInstance(instance);
        }

        public static TComponent AddComponentFromPool<TComponent>(this IComponentHolder componentHolder)
            where TComponent : class, new()
        {
            var instance = Pool?.GetInstance<TComponent>() /*new TComponent()*/;

            if (instance == null) return new TComponent();

            componentHolder?.Add((IComponent)instance);

            return instance;
        }
    }
}
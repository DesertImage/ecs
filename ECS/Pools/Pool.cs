using System;
using System.Collections.Generic;

namespace DesertImage.Pools
{
    public class Pool<T> where T : IPoolable, new()
    {
        protected readonly Stack<T> Stack = new Stack<T>();
        protected readonly Func<T> Factory;

        public Pool(Func<T> factory = null) => Factory = factory;

        public void Register(uint count)
        {
            for (var i = 0; i < count; i++)
            {
                ReturnInstance(CreateInstance());
            }
        }

        public virtual T GetInstance()
        {
            var instance = Stack.Count > 0 ? Stack.Pop() : CreateInstance();

            instance.OnCreate();

            return instance;
        }

        public virtual void ReturnInstance(T instance) => Stack.Push(instance);

        protected virtual T CreateInstance() => Factory != null ? Factory.Invoke() : new T();
    }
}
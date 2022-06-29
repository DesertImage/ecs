﻿using System.Collections.Generic;using DesertImage;using UnityEngine;namespace DesertImage.Pools{    public abstract class PoolMonoBehaviour<T> where T : Object    {        private readonly Dictionary<int, Stack<T>> _cachedObjects = new Dictionary<int, Stack<T>>();        private readonly Dictionary<int, int> _cachedIds = new Dictionary<int, int>();        protected readonly Transform Parent;        protected PoolMonoBehaviour(Transform parent)        {            Parent = parent;        }        public T GetInstance(T objInstance)        {            T obj;            if (_cachedObjects.Count == 0)            {                obj = CreateInstance(objInstance);            }            else            {                if (!_cachedObjects.TryGetValue(GetId(objInstance), out var objStack))                {                    obj = CreateInstance(objInstance);                }                else                {                    obj = objStack.Count > 0 ? objStack.Pop() : CreateInstance(objInstance);                }            }            if (objInstance is IPoolable poolable)            {                poolable.OnCreate();            }            GetStuff(obj);            return obj;        }        public void Register(T objInstance, int count = 1)        {            var hash = GetId(objInstance);            if (!_cachedObjects.TryGetValue(hash, out var objStack))            {                objStack = new Stack<T>();                _cachedObjects.Add(hash, objStack);            }            for (var i = 0; i < count; i++)            {                objStack.Push(CreateInstance(objInstance));                _cachedObjects[hash] = objStack;            }        }        public void ReturnInstance(T objInstance)        {            var instanceId = GetId(objInstance);            if (!_cachedIds.TryGetValue(instanceId, out var key))            {                key = instanceId;            }            if (_cachedObjects.TryGetValue(key, out var objStack))            {                if (objStack.Contains(objInstance)) return;                objStack.Push(objInstance);            }            ReturnStuff(objInstance);        }        private T CreateInstance(T instance)        {            var newObj = Object.Instantiate(instance, Parent);            _cachedIds.Add(GetId(newObj), GetId(instance));            ReturnStuff(newObj);            return newObj;        }        protected abstract void GetStuff(T instance);        protected virtual int GetId(T instance)        {            return HashCodeTypeTool.GetCachedHashCode<T>();        }        protected abstract void ReturnStuff(T instance);    }}
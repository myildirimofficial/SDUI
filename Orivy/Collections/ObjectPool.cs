using System;
using System.Collections.Concurrent;

namespace Orivy.Collections;

public class ObjectPool<T>
{
    private readonly Func<T> _objectGenerator;
    private readonly ConcurrentBag<T> _objects;

    public ObjectPool(Func<T> objectGenerator)
    {
        _objects = new ConcurrentBag<T>();
        _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
    }

    public T Get()
    {
        return _objects.TryTake(out var item) ? item : _objectGenerator();
    }

    public void Return(T item)
    {
        if (item != null)
            _objects.Add(item);
    }
}
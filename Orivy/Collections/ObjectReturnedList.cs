using System.Collections.Generic;

namespace Orivy.Collections;

public class ObjectReturnedList<T> : List<T>
{
    public new T Add(T item)
    {
        base.Add(item);
        return item;
    }
}
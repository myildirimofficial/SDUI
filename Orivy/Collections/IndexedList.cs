using System.Collections.Generic;

namespace Orivy.Collections;

public class IndexedList<T> : List<T>
{
    public new int Add(T item)
    {
        base.Add(item);
        return Count - 1;
    }
}
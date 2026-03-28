using System;
using System.Collections;
using System.Collections.Generic;
using Orivy.Controls;

namespace Orivy.Collections;

public class ArrangedElementCollection : IList
{
    internal static ArrangedElementCollection Empty = new(0);

    internal ArrangedElementCollection()
        : this(4)
    {
    }

    internal ArrangedElementCollection(List<IElement> innerList)
    {
        InnerList = innerList;
    }

    private ArrangedElementCollection(int size)
    {
        InnerList = new List<IElement>(size);
    }

    private protected List<IElement> InnerList { get; }

    internal virtual IElement this[int index] => InnerList[index];

    void IList.Clear()
    {
        ((IList)InnerList).Clear();
    }

    bool IList.IsFixedSize => ((IList)InnerList).IsFixedSize;

    bool IList.Contains(object? value)
    {
        return ((IList)InnerList).Contains(value);
    }

    public virtual bool IsReadOnly => ((IList)InnerList).IsReadOnly;

    void IList.RemoveAt(int index)
    {
        ((IList)InnerList).RemoveAt(index);
    }

    void IList.Remove(object? value)
    {
        ((IList)InnerList).Remove(value);
    }

    int IList.Add(object? value)
    {
        return ((IList)InnerList).Add(value);
    }

    int IList.IndexOf(object? value)
    {
        return ((IList)InnerList).IndexOf(value);
    }

    void IList.Insert(int index, object? value)
    {
        throw new NotSupportedException();
    }

    object? IList.this[int index]
    {
        get => InnerList[index];
        set => throw new NotSupportedException();
    }

    public virtual int Count => InnerList.Count;

    object ICollection.SyncRoot => ((ICollection)InnerList).SyncRoot;

    public void CopyTo(Array array, int index)
    {
        ((ICollection)InnerList).CopyTo(array, index);
    }

    bool ICollection.IsSynchronized => ((ICollection)InnerList).IsSynchronized;

    public virtual IEnumerator GetEnumerator()
    {
        return InnerList.GetEnumerator();
    }

    public override bool Equals(object? obj)
    {
        if (!(obj is ArrangedElementCollection other) || Count != other.Count) return false;

        for (var i = 0; i < Count; i++)
            if (InnerList[i] != other.InnerList[i])
                return false;

        return true;
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (object o in InnerList) hash.Add(o);

        return hash.ToHashCode();
    }

    /// <summary>
    ///     Repositions a element in this list.
    /// </summary>
    private protected void MoveElement(IElement element, int fromIndex, int toIndex)
    {
        var delta = toIndex - fromIndex;

        switch (delta)
        {
            case -1:
            case 1:
                // Simple swap
                InnerList[fromIndex] = InnerList[toIndex];
                break;

            default:
                int start;
                int dest;

                // Which direction are we moving?
                if (delta > 0)
                {
                    // Shift down by the delta to open the new spot
                    start = fromIndex + 1;
                    dest = fromIndex;
                }
                else
                {
                    // Shift up by the delta to open the new spot
                    start = toIndex;
                    dest = toIndex + 1;

                    // Make it positive
                    delta = -delta;
                }

                Copy(this, start, this, dest, delta);
                break;
        }

        InnerList[toIndex] = element;
    }

    private static void Copy(ArrangedElementCollection sourceList, int sourceIndex,
        ArrangedElementCollection destinationList, int destinationIndex, int length)
    {
        if (sourceIndex < destinationIndex)
        {
            // We need to copy from the back forward to prevent overwrite if source and
            // destination lists are the same, so we need to flip the source/dest indices
            // to point at the end of the spans to be copied.
            sourceIndex += length;
            destinationIndex += length;

            for (; length > 0; length--)
                destinationList.InnerList[--destinationIndex] = sourceList.InnerList[--sourceIndex];
        }
        else
        {
            for (; length > 0; length--)
                destinationList.InnerList[destinationIndex++] = sourceList.InnerList[sourceIndex++];
        }
    }
}
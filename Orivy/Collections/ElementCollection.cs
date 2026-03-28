using Orivy.Controls;
using Orivy.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Orivy.Collections;

public class ElementCollection : ArrangedElementCollection, IList, ICloneable
{
    /// A caching mechanism for key accessor
    /// We use an index here rather than control so that we don't have lifetime
    /// issues by holding on to extra references.
    /// Note this is not Thread Safe - but WinForms has to be run in a STA anyways.
    private int _lastAccessedIndex = -1;

    private int _maxZOrder;

    public ElementCollection(ElementBase owner)
    {
        Owner = owner;
    }

    /// <summary>
    ///     Who owns this control collection.
    /// </summary>
    public ElementBase Owner { get; }

    /// <summary>
    ///     Retrieves the child control with the specified index.
    /// </summary>
    public new virtual ElementBase this[int index]
    {
        get
        {
            //do some bounds checking here...
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(
                    nameof(index));

            var control = (ElementBase)InnerList[index]!;
            Debug.Assert(control is not null, "Why are we returning null controls from a valid index?");
            return control;
        }
    }

    /// <summary>
    ///     Retrieves the child control with the specified key.
    /// </summary>
    public virtual ElementBase? this[string? key]
    {
        get
        {
            // We do not support null and empty string as valid keys.
            if (string.IsNullOrEmpty(key)) return null;

            // Search for the key in our collection
            var index = IndexOfKey(key);
            if (IsValidIndex(index)) return this[index];

            return null;
        }
    }

    object ICloneable.Clone()
    {
        // Use CreateControlInstance so we get the same type of ControlCollection, but whack the
        // owner so adding controls to this new collection does not affect the control we cloned from.
        var ccOther = Owner.Controls.MemberwiseClone() as ElementCollection;

        // We add using InnerList to prevent unnecessary parent cycle checks, etc.
        ccOther.InnerList.AddRange(InnerList);
        return ccOther;
    }

    int IList.Add(object? control)
    {
        if (control is ElementBase c)
        {
            Add(c);
            return IndexOf(c);
        }

        return -1;
    }

    public override IEnumerator GetEnumerator()
    {
        return new ControlCollectionEnumerator(this);
    }

    void IList.Remove(object? control)
    {
        if (control is ElementBase c) Remove(c);
    }

    public void RemoveAt(int index)
    {
        Remove(this[index]);
    }

    public virtual void Clear()
    {
        Owner.SuspendLayout();

        try
        {
            while (Count != 0) RemoveAt(Count - 1);
        }
        finally
        {
            Owner.ResumeLayout();
        }
    }

    /// <summary>
    ///     Returns true if the collection contains an item with the specified key, false otherwise.
    /// </summary>
    public virtual bool ContainsKey(string? key)
    {
        return IsValidIndex(IndexOfKey(key));
    }

    /// <summary>
    ///     Adds a child control to this control. The control becomes the last control in
    ///     the child control list. If the control is already a child of another control it
    ///     is first removed from that control.
    /// </summary>
    public virtual void Add(ElementBase value)
    {
        if (value is null) return;

        if (value.Parent == Owner)
        {
            value.BringToFront();
            return;
        }

        // Remove the new control from its old parent (if any)
        value.Parent?.Controls.Remove(value);

        Owner.SuspendLayout();

        try
        {
            InnerList.Add(value);

            value.Parent = Owner;
            Owner.OnControlAdded(new ElementEventArgs(value));

            if (value.TabIndex == -1)
            {
                var nextTabIndex = 0;
                for (var c = 0; c < Count - 1; c++)
                {
                    var t = this[c].TabIndex;
                    if (nextTabIndex <= t) nextTabIndex = t + 1;
                }

                value.TabIndex = nextTabIndex;
            }

            _maxZOrder++;
            value.ZOrder = _maxZOrder;

            if (Owner.FocusedElement == null && value.TabStop) Owner.FocusedElement = value;
        }
        finally
        {
            Owner.ResumeLayout(true);
        }

        
    }

    public virtual void AddRange(ElementBase[] controls)
    {
        ArgumentNullException.ThrowIfNull(controls);

        if (controls.Length > 0)
        {
            Owner.SuspendLayout();
            try
            {
                for (var i = 0; i < controls.Length; ++i) Add(controls[i]);
            }
            finally
            {
                Owner.ResumeLayout(true);
            }
        }
    }

    public bool Contains(ElementBase? control)
    {
        return ((IList)InnerList).Contains(control);
    }

    /// <summary>
    ///     Searches for Controls by their Name property, builds up an array
    ///     of all the controls that match.
    /// </summary>
    public ElementBase[] Find(string key, bool searchAllChildren)
    {
        List<ElementBase> foundControls = new();
        FindInternal(key, searchAllChildren, this, foundControls);
        return foundControls.ToArray();
    }

    /// <summary>
    ///     Searches for Controls by their Name property, builds up a list
    ///     of all the controls that match.
    /// </summary>
    private static void FindInternal(string key, bool searchAllChildren, ElementCollection controlsToLookIn,
        List<ElementBase> foundControls)
    {
        try
        {
            // Perform breadth first search - as it's likely people will want controls belonging
            // to the same parent close to each other.
            for (var i = 0; i < controlsToLookIn.Count; i++)
            {
                if (controlsToLookIn[i] is null) continue;

                if (controlsToLookIn[i].Name.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                    foundControls.Add(controlsToLookIn[i]);
            }

            // Optional recursive search for controls in child collections.
            if (searchAllChildren)
                for (var i = 0; i < controlsToLookIn.Count; i++)
                {
                    if (controlsToLookIn[i] is null) continue;

                    if (controlsToLookIn[i].Controls.Count > 0)
                        // If it has a valid child collection, append those results to our collection.
                        FindInternal(key, true, controlsToLookIn[i].Controls, foundControls);
                }
        }
        catch (Exception e)
        {
        }
    }

    public int IndexOf(IElement control)
    {
        return ((IList)InnerList).IndexOf(control);
    }

    /// <summary>
    ///     The zero-based index of the first occurrence of value within the entire CollectionBase, if found; otherwise, -1.
    /// </summary>
    public virtual int IndexOfKey(string? key)
    {
        // Step 0 - Arg validation
        if (string.IsNullOrEmpty(key)) return -1; // we don't support empty or null keys.

        // step 1 - check the last cached item
        if (IsValidIndex(_lastAccessedIndex))
            if (this[_lastAccessedIndex].Name.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                return _lastAccessedIndex;

        // step 2 - search for the item
        for (var i = 0; i < Count; i++)
            if (this[i].Name.Equals(key, StringComparison.CurrentCultureIgnoreCase))
            {
                _lastAccessedIndex = i;
                return i;
            }

        // step 3 - we didn't find it.  Invalidate the last accessed index and return -1.
        _lastAccessedIndex = -1;
        return -1;
    }

    /// <summary>
    ///     Determines if the index is valid for the collection.
    /// </summary>
    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < Count;
    }

    /// <summary>
    ///     Removes control from this control. Inheriting controls should call
    ///     base.remove to ensure that the control is removed.
    /// </summary>
    public virtual void Remove(IElement value)
    {
        // Sanity check parameter
        if (value is null) return; // Don't do anything

        if (value.Parent != Owner) return;

        InnerList.Remove(value);

        if (Owner.FocusedElement == value) Owner.FocusedElement = null;

        value.Parent = null;

        if (Owner is ElementBase control) control.OnControlRemoved(new ElementEventArgs(value));

        Owner.PerformLayout();
        Owner.Invalidate();
    }

    /// <summary>
    ///     Removes the child control with the specified key.
    /// </summary>
    public virtual void RemoveByKey(string? key)
    {
        var index = IndexOfKey(key);
        if (IsValidIndex(index)) RemoveAt(index);
    }

    /// <summary>
    ///     Retrieves the index of the specified
    ///     child control in this array.  An ArgumentException
    ///     is thrown if child is not parented to this
    ///     UIElementBase.
    /// </summary>
    public int GetChildIndex(IElement child)
    {
        return GetChildIndex(child, true);
    }

    /// <summary>
    ///     Retrieves the index of the specified
    ///     child control in this array.  An ArgumentException
    ///     is thrown if child is not parented to this
    ///     UIElementBase.
    /// </summary>
    public virtual int GetChildIndex(IElement child, bool throwException)
    {
        return IndexOf(child);
    }

    /// <summary>
    ///     This is internal virtual method so that "Readonly Collections" can override this and throw as they should not allow
    ///     changing
    ///     the child control indices.
    /// </summary>
    internal virtual void SetChildIndexInternal(IElement child, int newIndex)
    {
        // Sanity check parameters
        ArgumentNullException.ThrowIfNull(child);

        var currentIndex = GetChildIndex(child);

        if (currentIndex == newIndex) return;

        if (newIndex >= Count || newIndex == -1) newIndex = Count - 1;

        MoveElement(child, currentIndex, newIndex);
        child.UpdateZOrder();

        //LayoutTransaction.DoLayout(Owner, child, PropertyNames.ChildIndex);
    }

    /// <summary>
    ///     Sets the index of the specified
    ///     child control in this array.  An ArgumentException
    ///     is thrown if child is not parented to this
    ///     UIElementBase.
    /// </summary>
    public virtual void SetChildIndex(ElementBase child, int newIndex)
    {
        SetChildIndexInternal(child, newIndex);
    }

    internal void Insert(int index, ElementBase value)
    {
        Add(value);
        SetChildIndexInternal(value, index);
    }

    private class ControlCollectionEnumerator : IEnumerator
    {
        private readonly ElementCollection list;
        private ElementBase currentElement;
        private int index;

        internal ControlCollectionEnumerator(ElementCollection list)
        {
            this.list = list;
            index = -1;
        }

        public ElementBase Current
        {
            get
            {
                if (index == -1)
                    throw new InvalidOperationException();
                if (index >= list.Count)
                    throw new InvalidOperationException();
                return currentElement;
            }
        }

        public bool MoveNext()
        {
            if (index < list.Count - 1)
            {
                index++;
                currentElement = list[index];
                return true;
            }

            index = list.Count;
            return false;
        }

        object IEnumerator.Current => Current;

        public void Reset()
        {
            currentElement = null;
            index = -1;
        }
    }
}

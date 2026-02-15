using System.Collections.Generic;
using System.Linq;

namespace System.Windows.Forms;

public static class ListViewExtensions
{
    /// <summary>
    /// Adds multiple items to the ListView with minimal flickering.
    /// Uses BeginUpdate/EndUpdate to batch the operation.
    /// </summary>
    /// <param name="sender">The ListView</param>
    /// <param name="items">Items to add</param>
    public static void AddRange(this ListView sender, IEnumerable<ListViewItem> items)
    {
        if (items == null)
            return;

        var itemArray = items.ToArray();
        if (itemArray.Length == 0)
            return;

        sender.BeginUpdate();
        try
        {
            sender.Items.AddRange(itemArray);
        }
        finally
        {
            sender.EndUpdate();
        }
    }

    /// <summary>
    /// Clears all items from the ListView with minimal flickering.
    /// </summary>
    /// <param name="sender">The ListView</param>
    public static void ClearItems(this ListView sender)
    {
        if (sender.Items.Count == 0)
            return;

        sender.BeginUpdate();
        try
        {
            sender.Items.Clear();
        }
        finally
        {
            sender.EndUpdate();
        }
    }

    /// <summary>
    /// Replaces all items in the ListView with new items, with minimal flickering.
    /// </summary>
    /// <param name="sender">The ListView</param>
    /// <param name="items">New items to set</param>
    public static void ReplaceItems(this ListView sender, IEnumerable<ListViewItem> items)
    {
        sender.BeginUpdate();
        try
        {
            sender.Items.Clear();
            if (items != null)
            {
                var itemArray = items.ToArray();
                if (itemArray.Length > 0)
                    sender.Items.AddRange(itemArray);
            }
        }
        finally
        {
            sender.EndUpdate();
        }
    }

    /// <summary>
    /// Move the selected items by <seealso cref="MoveDirection"/>
    /// </summary>
    /// <param name="sender">The ListView</param>
    /// <param name="direction">The move direction</param>
    public static void MoveSelectedItems(this ListView sender, MoveDirection direction)
    {
        var valid =
            sender.SelectedItems.Count > 0
            && (
                (
                    direction == MoveDirection.Down
                    && (sender.SelectedItems[sender.SelectedItems.Count - 1].Index < sender.Items.Count - 1)
                ) || (direction == MoveDirection.Up && (sender.SelectedItems[0].Index > 0))
            );

        if (valid)
        {
            var firstIndex = sender.SelectedItems[0].Index;
            var selectedItems = sender.SelectedItems.Cast<ListViewItem>().ToList();

            sender.BeginUpdate();
            try
            {
                foreach (ListViewItem item in sender.SelectedItems)
                    item.Remove();

                if (direction == MoveDirection.Up)
                {
                    var insertTo = firstIndex - 1;
                    foreach (var item in selectedItems)
                    {
                        sender.Items.Insert(insertTo, item);
                        insertTo++;
                    }
                }
                else
                {
                    var insertTo = firstIndex + 1;
                    foreach (var item in selectedItems)
                    {
                        sender.Items.Insert(insertTo, item);
                        insertTo++;
                    }
                }
            }
            finally
            {
                sender.EndUpdate();
            }
        }
    }
}

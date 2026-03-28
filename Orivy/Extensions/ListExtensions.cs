using System.Collections;

namespace Orivy.Extensions;

public static class ListExtensions
{
    public static void AddRange(this IList list, IEnumerable items)
    {
        foreach (var item in items) list.Add(item);
    }
}
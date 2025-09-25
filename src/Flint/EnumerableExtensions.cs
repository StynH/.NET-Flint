namespace Flint;

internal static class EnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
    {
        var i = 0;
        foreach (var value in enumerable)
        {
            action(value, i);
            ++i;
        }
    }
}

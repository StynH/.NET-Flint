namespace Flint;

internal static class Assertions
{
    public static void ThrowIfArgumentIsNull<T>(T argument, string paramName)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}

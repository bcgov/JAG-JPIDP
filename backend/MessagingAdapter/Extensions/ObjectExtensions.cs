namespace MessagingAdapter.Extensions;
public static class ObjectExtensions
{
    /// <summary>
    /// Throws an ArgumentNullException if the given data item is null.
    /// </summary>
    /// <param name="data">The item to check for nullity.</param>
    /// <param name="name">The name to use when throwing an exception</param>
    public static void ThrowIfNull<T>(this T data, string name) where T : class
    {
        if (data == null)
        {
            throw new ArgumentNullException(name);
        }
    }
}


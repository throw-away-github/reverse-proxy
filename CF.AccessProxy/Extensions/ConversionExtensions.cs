namespace CF.AccessProxy.Extensions;

public static class ConversionExtensions
{
    /// <summary>
    /// Will return the source as a <see cref="List{TSource}"/> only converting if necessary.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{TSource}"/> to convert to a list.</param>
    /// <typeparam name="TSource">The type of elements in the source.</typeparam>
    /// <returns>A <see cref="List{TSource}"/> containing the elements of the source.</returns>
    public static List<TSource> AsList<TSource>(this IEnumerable<TSource> source)
    {
        return source as List<TSource> ?? source.ToList();
    }

    /// <summary>
    /// Wraps a single <see cref="KeyValuePair{TKey,TValue}"/> into a <see cref="Dictionary{TKey,TValue}"/>
    /// allowing for lambda expressions to be used to transform the key and value.
    /// </summary>
    /// <remarks>
    /// Completely overkill but I wanted to learn how to write LINQ like extension methods where
    /// the selector parameters determine the return type.
    /// </remarks>
    public static Dictionary<TNewKey, TNewValue> ToDictionary<TKey, TValue, TNewKey, TNewValue>(
        this KeyValuePair<TKey, TValue> source, 
        Func<KeyValuePair<TKey, TValue>, TNewKey> keySelector,
        Func<KeyValuePair<TKey, TValue>, TNewValue> valueSelector) 
        where TNewKey : notnull
    {
        return new Dictionary<TNewKey, TNewValue> { { keySelector(source), valueSelector(source) } };
    }
}
# ![Icon](icon.png) .NET - Flint

Flint is a lightweight C# library that provides fast text matching utilities using the [Aho-Corasick algorithm](https://en.wikipedia.org/wiki/Aho%E2%80%93Corasick_algorithm)..

## Usage Example

```csharp
using Flint;

var matcher = new TextMatcher(new[] { "cat", "dog" });
var results = matcher.Find("The dog chased the cat.");

foreach (var match in results)
{
    Console.WriteLine($"Found '{match.Value}' at index {match.StartIndex}.");
}
```

Output:

```
Found 'dog' at index 4.
Found 'cat' at index 19.
```

## Support

* .NET Standard 2.0 and .NET 8.0

## License

Flint is licensed under the [MIT License](LICENSE).

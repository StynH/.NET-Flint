# ![Icon](logo.png) 
![NuGet Version](https://img.shields.io/nuget/v/Flint)

Flint is a lightweight, zero-dependency C# library that provides fast text matching utilities using the [Aho-Corasick algorithm](https://en.wikipedia.org/wiki/Aho%E2%80%93Corasick_algorithm).

## Usage Examples

### Basic

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

An optional ```StringComparison``` can be passed to ```TextMatcher```, influencing it's behavior when performing searches/replacement.
If none is given, it will default to ```StringComparison.CurrentCulture```.

```csharp
using Flint;

var matcher = new TextMatcher(new[] { "cat", "dog" }, StringComparison.OrdinalIgnoreCase);
var results = matcher.Find("The DOG chased the CaT.");

foreach (var match in results)
{
    Console.WriteLine($"Found '{match.Value}' at index {match.StartIndex}.");
}
```

Output:

```
Found 'DOG' at index 4.
Found 'CaT' at index 19.
```

### Find with a callback

`TextMatcher` provides an overload of `Find` that accepts a callback invoked for each match. This can be convenient to process matches as they are found without allocating a results collection.

```csharp
using Flint;

var matcher = new TextMatcher(new[] { "cat", "dog" });
matcher.Find("The dog chased the cat.", match =>
{
    Console.WriteLine($"Found '{match.Value}' at index {match.StartIndex}.");
});
```

Output:

```
Found 'dog' at index 4.
Found 'cat' at index 19.
```

### Scan multiple texts with `FindAll`

```csharp
using Flint;

var patterns = new[] { "cat", "dog", "bird" };
var matcher = new TextMatcher(patterns);

var texts = new[]
{
    "The dog chased the cat.",
    "A bird watched them from above.",
    "No animals here."
};

var all = matcher.FindAll(texts);

foreach (var kv in all)
{
    Console.WriteLine(kv.Key);
    foreach (var m in kv.Value)
    {
        Console.WriteLine($"Found '{m.Value}' at index {m.StartIndex}.");
    }
}
```

Example output:

```
The dog chased the cat.
Found 'cat' at index 19.
Found 'dog' at index 4.
A bird watched them from above.
Found 'bird' at index 2.
No animals here.
```

If you need to scan many input strings and handle matches as they are discovered, use the `FindAll` overload that accepts an `Action<string, Match>` callback. The callback receives the original text and the match.

```csharp
using Flint;

var patterns = new[] { "cat", "dog" };
var matcher = new TextMatcher(patterns);

var texts = new[]
{
    "The dog chased the cat.",
    "A bird watched from above.",
};

matcher.FindAll(texts, (text, match) =>
{
    Console.WriteLine(text);
    Console.WriteLine($"Found '{match.Value}' at index {match.StartIndex}.");
});
```

Example output:

```
The dog chased the cat.
Found 'dog' at index 4.
Found 'cat' at index 19.
A bird watched from above.
```

### Exact (whole-word) matching with `MatchMode`

The `MatchMode` enum lets you require matches to be whole words. This prevents short patterns from matching inside longer words (for example, preventing "he" from matching inside "she").

```csharp
using Flint;

var patterns = new[] { "he", "she" };
var matcher = new TextMatcher(patterns, StringComparison.CurrentCulture, MatchMode.ExactMatch);

var results = matcher.Find("she sells seashells.");

foreach (var m in results)
{
    Console.WriteLine($"Found '{m.Value}' at index {m.StartIndex}.");
}
```

Output (only the whole-word `she` is matched):

```
Found 'she' at index 0.
```

### Overlapping and nested matches

```csharp
using Flint;

var matcher = new TextMatcher(new[] { "he", "she", "his", "hers" });
var results = matcher.Find("she is his hero");

foreach (var m in results)
{
    Console.WriteLine($"Found '{m.Value}' at index {m.StartIndex}.");
}
```

Example output:

```
Found 'she' at index 0.
Found 'he' at index 1.
Found 'his' at index 7.
Found 'he' at index 10.
```

### Replace all matches with a single value

```csharp
using Flint;

var matcher = new TextMatcher(new[] { "cat", "dog" });
var replaced = matcher.Replace("The dog chased the cat.", "animal");
Console.WriteLine(replaced);
```

Output:

```
The animal chased the animal.
```

### Replace matches using a function

```csharp
using Flint;

var matcher = new TextMatcher(new[] { "cat", "dog" });
var replaced = matcher.Replace(
    "The dog chased the cat.",
    match => match.Value.ToUpper()
);
Console.WriteLine(replaced);
```

Output:

```
The DOG chased the CAT.
```

### Replace each pattern with a different value

```csharp
using Flint;

var matcher = new TextMatcher(new[] { "cat", "dog" });
var replaced = matcher.Replace(
    "The dog chased the cat.",
    new[] { "feline", "canine" }
);
Console.WriteLine(replaced);
```

Output:

```
The canine chased the feline.
```

### Load patterns from a file

```csharp
using Flint;

var patterns = File.ReadAllLines("patterns.txt")
                   .Where(l => !string.IsNullOrWhiteSpace(l));

var matcher = new TextMatcher(patterns);

var text = File.ReadAllText("large_input.txt");
var hits = matcher.Find(text).ToArray();

Console.WriteLine($"Found {hits.Length} matches.");
```

## Support

* .NET Standard 2.0 and .NET 8.0

## License

Flint is licensed under the [MIT License](LICENSE).

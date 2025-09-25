using System.Text;
using System.Linq;

using Flint.TextProcessing;

namespace Flint;

public sealed class TextMatcher
{
    private readonly TrieNode _root;
    private readonly IEnumerable<string> _patterns;

    public TextMatcher(IEnumerable<string> patterns)
    {
        Assertions.ThrowIfArgumentIsNull(patterns, nameof(patterns));

        _patterns = patterns;
        _root = AhoCorasick.BuildTrie(_patterns);
        AhoCorasick.BuildFailureLinks(_root);
    }

    public IEnumerable<Match> Find(string text)
    {
        Assertions.ThrowIfArgumentIsNull(text, nameof(text));

        return AhoCorasick.PerformSearch(_root, text, _patterns);
    }

    public IDictionary<string, IEnumerable<Match>> FindAll(IEnumerable<string> texts)
    {
        Assertions.ThrowIfArgumentIsNull(texts, nameof(texts));

        var results = new Dictionary<string, IEnumerable<Match>>();

        foreach (var text in texts)
        {
            results[text] = Find(text);
        }

        return results;
    }

    public string Replace(string text, string replacement)
    {
        Assertions.ThrowIfArgumentIsNull(text, nameof(text));
        Assertions.ThrowIfArgumentIsNull(replacement, nameof(replacement));

        return Replace(text, _ => replacement);
    }

    public string Replace(string text, Func<Match, string> replacementProvider)
    {
        Assertions.ThrowIfArgumentIsNull(text, nameof(text));
        Assertions.ThrowIfArgumentIsNull(replacementProvider, nameof(replacementProvider));

        var matches = Find(text).OrderBy(m => m.StartIndex).ToList();
        if (matches.Count == 0)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length);
        var lastIndex = 0;

        foreach (var match in matches)
        {
            if (match.StartIndex < lastIndex)
            {
                continue;
            }

            if (match.StartIndex > lastIndex)
            {
                sb.Append(text, lastIndex, match.StartIndex - lastIndex);
            }

            var replacement = replacementProvider(match) ?? string.Empty;
            sb.Append(replacement);

            lastIndex = match.EndIndex + 1;
        }

        if (lastIndex < text.Length)
        {
            sb.Append(text, lastIndex, text.Length - lastIndex);
        }

        return sb.ToString();
    }

    public string Replace(string text, IEnumerable<string> replacements)
    {
        Assertions.ThrowIfArgumentIsNull(text, nameof(text));
        Assertions.ThrowIfArgumentIsNull(replacements, nameof(replacements));

        var patternsList = _patterns.ToList();
        var replacementsList = replacements.ToList();

        if (patternsList.Count != replacementsList.Count)
        {
            throw new ArgumentException("Replacements must have the same number of elements as patterns", nameof(replacements));
        }

        for (var i = 0; i < replacementsList.Count; ++i)
        {
            if (replacementsList[i] is null)
            {
                throw new ArgumentNullException($"replacements[{i}]");
            }
        }

        if (patternsList.Count == 0)
        {
            return text;
        }

        var idMatches = new List<(int Start, int End, int Id)>();
        AhoCorasick.PerformSearchWithIds(_root, text, (endIndex, id) =>
        {
            var patternLength = patternsList[id].Length;
            idMatches.Add((endIndex - patternLength + 1, endIndex, id));
        });

        if (idMatches.Count == 0)
        {
            return text;
        }

        var stringBuilder = new StringBuilder(text.Length);
        var lastIndex = 0;

        foreach (var (Start, End, Id) in idMatches.OrderBy(x => x.Start))
        {
            if (Start < lastIndex)
            {
                continue;
            }

            if (Start > lastIndex)
            {
                stringBuilder.Append(text, lastIndex, Start - lastIndex);
            }

            var replacement = replacementsList[Id] ?? string.Empty;
            stringBuilder.Append(replacement);

            lastIndex = End + 1;
        }

        if (lastIndex < text.Length)
        {
            stringBuilder.Append(text, lastIndex, text.Length - lastIndex);
        }

        return stringBuilder.ToString();
    }
}

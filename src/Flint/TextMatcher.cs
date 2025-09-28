using Flint.TextProcessing;

namespace Flint;

public sealed class TextMatcher
{
    private readonly TrieNode _root;
    private readonly IEnumerable<string> _patterns;
    private readonly StringComparison _comparison;
    private readonly MatchMode _mode;

    public TextMatcher(IEnumerable<string> patterns, StringComparison comparison, MatchMode mode)
    {
        Assertions.ThrowIfArgumentIsNull(patterns, nameof(patterns));

        _patterns = patterns;
        _comparison = comparison;
        _mode = mode;
        _root = AhoCorasick.BuildTrie(_patterns, _comparison);
        AhoCorasick.BuildFailureLinks(_root);
    }

    public TextMatcher(IEnumerable<string> patterns, StringComparison comparison) : this(patterns, comparison, MatchMode.Fuzzy)
    {
    }

    public TextMatcher(IEnumerable<string> patterns, MatchMode mode) : this(patterns, StringComparison.CurrentCulture, mode)
    {
    }

    public TextMatcher(IEnumerable<string> patterns) : this(patterns, StringComparison.CurrentCulture, MatchMode.Fuzzy)
    {
    }

    public IEnumerable<Match> Find(string text)
    {
        Assertions.ThrowIfArgumentIsNull(text, nameof(text));

        return AhoCorasick.PerformSearch(_root, text, _patterns, _comparison, _mode);
    }

    public void Find(string text, Action<Match> onMatchCallback)
    {
        Assertions.ThrowIfArgumentIsNull(text, nameof(text));
        Assertions.ThrowIfArgumentIsNull(onMatchCallback, nameof(onMatchCallback));

        AhoCorasick.PerformSearchWithCallback(_root, text, _patterns, onMatchCallback, _comparison, _mode);
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

    public void FindAll(IEnumerable<string> texts, Action<string, Match> onMatchCallback)
    {
        Assertions.ThrowIfArgumentIsNull(texts, nameof(texts));

        foreach (var text in texts)
        {
            Find(text, (match) =>
            {
                onMatchCallback(text, match);
            });
        }
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

        var mapped = matches.Select(m => (m.StartIndex, m.EndIndex, Replacement: replacementProvider(m) ?? string.Empty)).ToList();
        return BuildReplacedString(text, mapped);
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
        }, _comparison);

        if (idMatches.Count == 0)
        {
            return text;
        }

        var mapped = idMatches.OrderBy(x => x.Start)
            .Select(m => (m.Start, m.End, Replacement: replacementsList[m.Id]))
            .ToList();

        return BuildReplacedString(text, mapped);
    }

    private static string BuildReplacedString(string text, List<(int Start, int End, string Replacement)> matches)
    {
        var finalLength = 0;
        var lastIndex = 0;

        foreach (var (start, end, replacement) in matches)
        {
            if (start < lastIndex)
            {
                continue;
            }

            finalLength += start - lastIndex;
            finalLength += replacement.Length;
            lastIndex = end + 1;
        }

        finalLength += text.Length - lastIndex;

        var buffer = new char[finalLength];
        var bufferIndex = 0;
        lastIndex = 0;

        foreach (var (Start, End, Replacement) in matches)
        {
            if (Start < lastIndex)
            {
                continue;
            }

            var count = Start - lastIndex;
            text.CopyTo(lastIndex, buffer, bufferIndex, count);
            bufferIndex += count;

            Replacement.CopyTo(0, buffer, bufferIndex, Replacement.Length);
            bufferIndex += Replacement.Length;

            lastIndex = End + 1;
        }

        if (lastIndex < text.Length)
        {
            var count = text.Length - lastIndex;
            text.CopyTo(lastIndex, buffer, bufferIndex, count);
            bufferIndex += count;
        }

        return new string(buffer, 0, bufferIndex);
    }
}

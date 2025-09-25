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
}

using System.ComponentModel;
using System.Globalization;
using Flint;

namespace Flint.TextProcessing;

[EditorBrowsable(EditorBrowsableState.Never)]
public class AhoCorasick
{
    internal static TrieNode BuildTrie(IEnumerable<string> patterns, StringComparison comparison)
    {
        Assertions.ThrowIfArgumentIsNull(patterns, nameof(patterns));

        var root = new TrieNode();
        root.Fail = root;

        var normalizedPatterns = patterns.Select(p => Normalize(p, comparison));
        normalizedPatterns.ForEach((value, index) =>
        {
            var node = root;
            foreach (var character in value)
            {
                if (!node.Children.TryGetValue(character, out var childNode))
                {
                    childNode = new TrieNode();
                    node.Children[character] = childNode;
                }
                node = childNode;
            }
            node.Outputs.Add(index);
        });

        return root;
    }

    internal static void BuildFailureLinks(TrieNode root)
    {
        Assertions.ThrowIfArgumentIsNull(root, nameof(root));

        var queue = new Queue<TrieNode>();
        foreach (var kvp in root.Children)
        {
            var node = kvp.Value;
            node.Fail = root;
            queue.Enqueue(node);
        }

        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();

            foreach (var kvp in currentNode.Children)
            {
                var node = kvp.Value;
                var character = kvp.Key;

                var failNode = currentNode.Fail;

                queue.Enqueue(node);

                while (failNode != root && !failNode.Children.TryGetValue(character, out var _))
                {
                    failNode = failNode.Fail;
                }

                node.Fail = failNode.Children.TryGetValue(character, out var child) ? child : root;
                foreach (var output in node.Fail.Outputs)
                {
                    node.Outputs.Add(output);
                }
            }
        }
    }

    internal static void PerformSearchWithIds(TrieNode root, string text, Action<int, int> onMatch, StringComparison comparison)
    {
        Assertions.ThrowIfArgumentIsNull(root, nameof(root));
        Assertions.ThrowIfArgumentIsNull(text, nameof(text));
        Assertions.ThrowIfArgumentIsNull(onMatch, nameof(onMatch));

        var node = root;
        var normalizedText = Normalize(text, comparison);

        for (var i = 0; i < normalizedText.Length; ++i)
        {
            var character = normalizedText[i];
            while (node != root && !node.Children.TryGetValue(character, out var _))
            {
                node = node.Fail;
            }

            node = node.Children.TryGetValue(character, out var child) ? child : root;
            foreach (var id in node.Outputs)
            {
                onMatch(i, id);
            }
        }
    }

    internal static IEnumerable<Match> PerformSearch(TrieNode root, string text, IEnumerable<string> patterns, StringComparison comparison, MatchMode mode)
    {
        Assertions.ThrowIfArgumentIsNull(root, nameof(root));
        Assertions.ThrowIfArgumentIsNull(text, nameof(text));
        Assertions.ThrowIfArgumentIsNull(patterns, nameof(patterns));

        if (!patterns.Any())
        {
            return Array.Empty<Match>();
        }

        var results = new List<Match>();
        var normalizedPatterns = patterns.Select(p => Normalize(p, comparison)).ToList();
        var originalPatterns = patterns.ToList();
        var normalizedText = Normalize(text, comparison);

        PerformSearchWithIds(root, normalizedText, (endIndex, id) =>
        {
            var patternLength = normalizedPatterns.ElementAt(id).Length;
            var start = endIndex - patternLength + 1;
            var value = text.Substring(start, patternLength);

            if (mode == MatchMode.ExactMatch)
            {
                if (!IsWholeWord(text, start, endIndex))
                {
                    return;
                }

                var originalPattern = originalPatterns.ElementAt(id);
                if (!string.Equals(value, originalPattern, comparison))
                {
                    return;
                }
            }

            results.Add(new(start, endIndex, value));
        }, comparison);

        return results;
    }

    internal static void PerformSearchWithCallback(TrieNode root, string text, IEnumerable<string> patterns, Action<Match> onMatch, StringComparison comparison, MatchMode mode)
    {
        Assertions.ThrowIfArgumentIsNull(root, nameof(root));
        Assertions.ThrowIfArgumentIsNull(text, nameof(text));
        Assertions.ThrowIfArgumentIsNull(patterns, nameof(patterns));
        Assertions.ThrowIfArgumentIsNull(onMatch, nameof(onMatch));

        if (!patterns.Any())
        {
            return;
        }

        var normalizedPatterns = patterns.Select(p => Normalize(p, comparison)).ToList();
        var originalPatterns = patterns.ToList();
        var normalizedText = Normalize(text, comparison);

        PerformSearchWithIds(root, normalizedText, (endIndex, id) =>
        {
            var patternLength = normalizedPatterns.ElementAt(id).Length;
            var start = endIndex - patternLength + 1;
            var value = text.Substring(start, patternLength);

            if (mode == MatchMode.ExactMatch)
            {
                if (!IsWholeWord(text, start, endIndex))
                {
                    return;
                }

                var originalPattern = originalPatterns.ElementAt(id);
                if (!string.Equals(value, originalPattern, comparison))
                {
                    return;
                }
            }

            onMatch(new Match(start, endIndex, value));
        }, comparison);
    }

    private static string Normalize(string input, StringComparison comparison)
    {
        return comparison switch
        {
            StringComparison.OrdinalIgnoreCase => input.ToUpperInvariant(),
            StringComparison.CurrentCultureIgnoreCase => input.ToUpper(CultureInfo.CurrentCulture),
            StringComparison.InvariantCultureIgnoreCase => input.ToUpper(CultureInfo.InvariantCulture),
            StringComparison.CurrentCulture => input,
            StringComparison.InvariantCulture => input,
            StringComparison.Ordinal => input,
            _ => input
        };
    }

    private static bool IsWholeWord(string text, int start, int end)
    {
        static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

        if (start > 0 && IsWordChar(text[start - 1]))
        {
            return false;
        }

        if (end + 1 < text.Length && IsWordChar(text[end + 1]))
        {
            return false;
        }

        return true;
    }
}

using System.ComponentModel;

namespace Flint.TextProcessing;

[EditorBrowsable(EditorBrowsableState.Never)]
public class AhoCorasick
{
    internal static TrieNode BuildTrie(IEnumerable<string> patterns)
    {
        Assertions.ThrowIfArgumentIsNull(patterns, nameof(patterns));

        var root = new TrieNode();
        root.Fail = root;

        patterns.ForEach((value, index) =>
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

    internal static IEnumerable<Match> PerformSearch(TrieNode root, string text, IEnumerable<string> patterns)
    {
        Assertions.ThrowIfArgumentIsNull(root, nameof(root));
        Assertions.ThrowIfArgumentIsNull(text, nameof(text));
        Assertions.ThrowIfArgumentIsNull(patterns, nameof(patterns));

        if (!patterns.Any())
        {
            return [];
        }

        var node = root;
        var results = new List<Match>();

        for (var i = 0; i < text.Length; ++i)
        {
            var character = text[i];

            while (node != root && !node.Children.TryGetValue(character, out var _))
            {
                node = node.Fail;
            }

            node = node.Children.TryGetValue(character, out var child) ? child : root;
            foreach (var id in node.Outputs)
            {
                var match = patterns.ElementAt(id);
                var patternLength = match.Length;
                results.Add(new(i - patternLength + 1, i, match));
            }
        }

        return results;
    }
}

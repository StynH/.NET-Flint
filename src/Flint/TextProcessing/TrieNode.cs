using System.ComponentModel;

namespace Flint.TextProcessing;

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class TrieNode : IComparable<TrieNode>
{
    public Dictionary<char, TrieNode> Children { get; } = [];

    public TrieNode Fail { get; set; } = null!;

    public IList<int> Outputs { get; } = [];

    public int CompareTo(TrieNode? other)
    {
        return ReferenceEquals(this, other) ? 0 : other is null ? 1 : GetHashCode().CompareTo(other.GetHashCode());
    }
}

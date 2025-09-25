using System.ComponentModel;

namespace Flint.TextProcessing;

[EditorBrowsable(EditorBrowsableState.Never)]
public record Match(int StartIndex, int EndIndex, string Value)
{
}

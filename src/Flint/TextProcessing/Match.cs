using System.ComponentModel;

namespace Flint.TextProcessing;

[EditorBrowsable(EditorBrowsableState.Never)]
public record Match(int startIndex, int endIndex, string match)
{
}

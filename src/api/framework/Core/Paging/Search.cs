namespace FSH.Framework.Core.Paging;

public class Search
{
    public IReadOnlyList<string> Fields { get; set; } = Array.Empty<string>();
    public string? Keyword { get; set; }
}

namespace MoogleEngine;

/// <summary>
/// Public entry point for the Moogle search engine.
///
/// This class acts as a thin façade between the Blazor server (MoogleServer)
/// and the internal SearchEngine implementation.
///
/// The SearchEngine is built lazily on the first query and then cached
/// for the entire application lifetime — all subsequent queries reuse
/// the pre-built index without re-reading any files.
/// </summary>
public static class Moogle
{
    // Relative path from the MoogleServer working directory to the Content folder.
    private const string ContentPath = "../Content";

    // Thread-safe lazy initialisation — the SearchEngine is built exactly once.
    private static readonly Lazy<SearchEngine> _engine =
        new(() => new SearchEngine(ResolveContentPath()));

    /// <summary>
    /// Executes a search query and returns a SearchResult.
    /// Exposed to the Blazor UI via the MoogleServer project.
    /// </summary>
    public static SearchResult Query(string query)
    {
        try
        {
            return _engine.Value.Search(query);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Moogle] Query error: {ex.Message}");
            return new SearchResult();
        }
    }

    private static string ResolveContentPath()
    {
        string relative = Path.GetFullPath(ContentPath);
        if (Directory.Exists(relative))
            return relative;

        string assemblyDir = AppDomain.CurrentDomain.BaseDirectory;
        string sibling = Path.Combine(assemblyDir, "Content");
        if (Directory.Exists(sibling))
            return sibling;

        return relative;
    }
}

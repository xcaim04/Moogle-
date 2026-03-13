namespace MoogleEngine.Models;

/// <summary>
/// Represents a fully processed document ready for TF-IDF scoring.
/// Stores the document's tokenized content, term frequencies, and word positions
/// so that searches do not require re-reading from disk.
/// </summary>
public class Document
{
    /// <summary>Internal numeric ID assigned during index building.</summary>
    public int Id { get; set; }

    /// <summary>Human-readable title (filename without extension).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Absolute path to the original .txt file.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Raw text content, preserved for snippet extraction.</summary>
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// Term Frequency (TF) for each unique term in the document.
    /// TF(t,d) = count(t in d) / totalTerms(d).
    /// </summary>
    public Dictionary<string, float> TermFrequency { get; set; } = new();

    /// <summary>
    /// Ordered list of character positions where each term appears.
    /// Used by the snippet extractor and proximity operator (~).
    /// </summary>
    public Dictionary<string, List<int>> WordPositions { get; set; } = new();

    /// <summary>Total number of tokens in the document (denominator for TF).</summary>
    public int TotalTokens { get; set; }
}

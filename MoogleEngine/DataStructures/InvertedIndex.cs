namespace MoogleEngine.DataStructures;

/// <summary>
/// Posting entry: a document that contains a term together with
/// all positions in that document where the term appears.
/// Positions are character offsets (not word indices) to support
/// accurate proximity scoring and snippet extraction.
/// </summary>
public readonly record struct Posting(int DocumentId, IReadOnlyList<int> Positions);

/// <summary>
/// Classic inverted index: maps every term to the list of documents
/// that contain it ("postings list").
///
/// Why an inverted index?
///   Without it, a query would require scanning every document for
///   every query term — O(D × L) per query, where D is the number of
///   documents and L is the average document length.
///   With an inverted index, we retrieve only the relevant documents
///   in O(1) per term lookup plus O(k) to iterate the postings list.
///
/// The index is built once at startup and held in memory; lookups
/// during queries are therefore extremely fast.
/// </summary>
public sealed class InvertedIndex
{
    // term → postings list
    private readonly Dictionary<string, List<Posting>> _index = new(StringComparer.Ordinal);

    /// <summary>Total number of indexed documents (N in IDF formula).</summary>
    public int DocumentCount { get; private set; }

    // ── Building ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds all terms of a document to the index.
    /// </summary>
    public void AddDocument(int documentId, Dictionary<string, List<int>> wordPositions)
    {
        DocumentCount++;
        foreach (var (term, positions) in wordPositions)
        {
            if (!_index.TryGetValue(term, out List<Posting>? postings))
            {
                postings = new List<Posting>();
                _index[term] = postings;
            }
            postings.Add(new Posting(documentId, positions));
        }
    }

    // ── Querying ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the postings list for a term (empty if not found).
    /// Time complexity: O(1) average.
    /// </summary>
    public IReadOnlyList<Posting> GetPostings(string term)
    {
        return _index.TryGetValue(term, out List<Posting>? postings)
            ? postings
            : Array.Empty<Posting>();
    }

    /// <summary>
    /// Returns the number of documents that contain the term.
    /// This is the "df" used in the IDF formula.
    /// </summary>
    public int GetDocumentFrequency(string term)
    {
        return _index.TryGetValue(term, out List<Posting>? postings)
            ? postings.Count
            : 0;
    }

    /// <summary>Returns all indexed terms.</summary>
    public IEnumerable<string> AllTerms() => _index.Keys;

    /// <summary>Returns true if the index has any postings for the term.</summary>
    public bool Contains(string term) => _index.ContainsKey(term);
}

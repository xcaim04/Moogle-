namespace MoogleEngine;

using MoogleEngine.Algorithms;
using MoogleEngine.DataStructures;
using MoogleEngine.Models;

/// <summary>
/// The core search engine. Holds all pre-built data structures in memory
/// and executes queries against them.
///
/// Lifecycle
/// ─────────
/// 1. Constructor builds the index from documents in the Content directory.
///    This is O(D × L) — done once at startup.
/// 2. Each call to <see cref="Search"/> executes in near-realtime:
///    - Parse query with operators.
///    - Retrieve candidate documents from the inverted index.
///    - Score each candidate with TF-IDF cosine similarity.
///    - Apply operator filters (exclusion, requirement, proximity).
///    - Sort and return top results.
///    - Build spelling suggestion if results are sparse.
/// </summary>
public sealed class SearchEngine
{
    // ── Data structures ──────────────────────────────────────────────────

    private readonly List<Document> _documents;
    private readonly InvertedIndex  _index;
    private readonly Trie           _trie;
    private readonly Dictionary<string, float> _idfs;
    private readonly Dictionary<int, float>    _docMagnitudes;

    // Maximum results returned per query
    private const int MaxResults = 10;

    // ── Constructor (index building) ─────────────────────────────────────

    public SearchEngine(string contentDirectory)
    {
        Console.WriteLine("[SearchEngine] Building index...");

        // 1. Load and tokenise every document
        _documents = DocumentProcessor.LoadDocuments(contentDirectory);

        // 2. Build inverted index and Trie
        _index = new InvertedIndex();
        _trie  = new Trie();

        foreach (Document doc in _documents)
        {
            _index.AddDocument(doc.Id, doc.WordPositions);
        }

        // 3. Populate Trie with vocabulary (for suggestions)
        foreach (string term in _index.AllTerms())
        {
            _trie.Insert(term, _index.GetDocumentFrequency(term));
        }

        // 4. Pre-compute IDF for all terms
        _idfs = TFIDFCalculator.ComputeIDFs(_index);

        // 5. Pre-compute document magnitudes for cosine similarity
        _docMagnitudes = TFIDFCalculator.ComputeDocumentMagnitudes(_documents, _idfs);

        Console.WriteLine($"[SearchEngine] Index ready. " +
                          $"{_documents.Count} documents, {_idfs.Count} unique terms.");
    }

    // ── Public query API ──────────────────────────────────────────────────

    /// <summary>
    /// Executes a search query and returns a ranked list of <see cref="SearchItem"/>s.
    /// </summary>
    public SearchResult Search(string rawQuery)
    {
        if (string.IsNullOrWhiteSpace(rawQuery) || _documents.Count == 0)
            return new SearchResult();

        // 1. Parse operators
        ProcessedQuery query = QueryParser.Parse(rawQuery);
        if (query.AllScoredTerms.Count == 0)
            return new SearchResult();

        // 2. Build TF-IDF query vector
        var scoredTerms = QueryParser.GetScoredTerms(query).ToList();
        var (queryVector, queryMagnitude) = TFIDFCalculator.BuildQueryVector(scoredTerms, _idfs);

        // 3. Retrieve candidate documents (union of postings lists)
        var candidateIds = GatherCandidates(query);

        // 4. Score each candidate
        var scored = new List<(Document doc, float score)>();

        foreach (int docId in candidateIds)
        {
            Document doc = _documents[docId];

            // ── Exclusion filter (! operator) ─────────────────────────
            if (query.ExcludedTerms.Any(t => doc.TermFrequency.ContainsKey(t)))
                continue;

            // ── Requirement filter (^ operator) ───────────────────────
            if (query.RequiredTerms.Any(t => !doc.TermFrequency.ContainsKey(t)))
                continue;

            // ── Base TF-IDF cosine score ──────────────────────────────
            float score = TFIDFCalculator.CosineSimilarity(
                queryVector, doc, _idfs,
                _docMagnitudes[docId], queryMagnitude);

            // ── Proximity bonus (~ operator) ──────────────────────────
            if (query.ProximityTerms.Count >= 2)
                score += ComputeProximityBonus(doc, query.ProximityTerms);

            if (score > 0)
                scored.Add((doc, score));
        }

        // 5. Sort descending by score
        scored.Sort((a, b) => b.score.CompareTo(a.score));

        // 6. Build SearchItem array
        SearchItem[] items = scored
            .Take(MaxResults)
            .Select(x => new SearchItem(
                x.doc.Title,
                SnippetExtractor.Extract(x.doc, query.AllScoredTerms),
                x.score))
            .ToArray();

        // 7. Compute spelling suggestion if results are sparse
        string suggestion = string.Empty;
        if (items.Length < 3)
        {
            suggestion = BuildSuggestion(query) ?? string.Empty;
        }

        return new SearchResult(items, suggestion);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Returns the set of document IDs that appear in at least one
    /// postings list for any scored query term.
    /// </summary>
    private HashSet<int> GatherCandidates(ProcessedQuery query)
    {
        var ids = new HashSet<int>();
        foreach (string term in query.AllScoredTerms)
        {
            foreach (Posting p in _index.GetPostings(term))
                ids.Add(p.DocumentId);
        }
        return ids;
    }

    /// <summary>
    /// Computes a proximity bonus for the ~ operator.
    /// Finds the minimum distance (in characters) between any pair of
    /// proximity terms and converts it to a small additive bonus.
    ///
    /// The closer the terms, the higher the bonus. We cap the bonus so
    /// that it never overwhelms the base TF-IDF score.
    /// </summary>
    private static float ComputeProximityBonus(
        Document doc,
        HashSet<string> proximityTerms)
    {
        var termLists = proximityTerms
            .Where(t => doc.WordPositions.ContainsKey(t))
            .Select(t => doc.WordPositions[t])
            .ToList();

        if (termLists.Count < 2) return 0f;

        // Find minimum gap between any two different term occurrence lists
        int minGap = int.MaxValue;
        for (int a = 0; a < termLists.Count - 1; a++)
        {
            for (int b = a + 1; b < termLists.Count; b++)
            {
                int gap = MinPositionGap(termLists[a], termLists[b]);
                if (gap < minGap) minGap = gap;
            }
        }

        // Convert gap to a bonus in [0, 0.3].
        // Gap of 0–10 chars → 0.3; gap of 500+ chars → ~0
        const float maxBonus = 0.3f;
        return minGap < 1 ? maxBonus : maxBonus / (1f + minGap / 50f);
    }

    /// <summary>
    /// Returns the minimum absolute difference between any position in
    /// listA and any position in listB.  Both lists are sorted.
    /// Uses a two-pointer technique: O(m + n) instead of O(m × n).
    /// </summary>
    private static int MinPositionGap(List<int> a, List<int> b)
    {
        int i = 0, j = 0, minGap = int.MaxValue;
        while (i < a.Count && j < b.Count)
        {
            int gap = Math.Abs(a[i] - b[j]);
            if (gap < minGap) minGap = gap;
            if (a[i] < b[j]) i++;
            else              j++;
        }
        return minGap;
    }

    /// <summary>
    /// Uses Levenshtein distance on the Trie vocabulary to suggest a
    /// corrected version of the query.
    /// </summary>
    private string? BuildSuggestion(ProcessedQuery query)
    {
        // Only apply spell-check to plain terms, not operator terms
        if (query.PlainTerms.Count == 0) return null;

        return LevenshteinDistance.SuggestQuery(
            query.PlainTerms.ToArray(),
            term => _trie.Contains(term),
            _trie.AllWords(),
            _trie.GetDocumentFrequency);
    }
}

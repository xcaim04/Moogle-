namespace MoogleEngine.Algorithms;

using MoogleEngine.DataStructures;
using MoogleEngine.Models;

/// <summary>
/// Implements TF-IDF (Term Frequency – Inverse Document Frequency) scoring.
///
/// ─── Why TF-IDF? ──────────────────────────────────────────────────────────
/// A naive "count how many query words appear" approach treats all words
/// equally. TF-IDF solves two problems:
///   1. A word that appears many times in a document is more characteristic
///      of that document → Term Frequency (TF).
///   2. A word that appears in almost every document (e.g. "the", "and")
///      carries little information → Inverse Document Frequency (IDF).
///
/// ─── Formulas ─────────────────────────────────────────────────────────────
/// TF(t, d)  = count(t in d) / totalTerms(d)
///              (normalised so long documents don't dominate)
///
/// IDF(t)    = log( (N + 1) / (df(t) + 1) ) + 1
///              (+1 smoothing avoids log(0); the outer +1 keeps IDF ≥ 1)
///
/// TF-IDF(t,d) = TF(t,d) × IDF(t)
///
/// ─── Cosine Similarity ────────────────────────────────────────────────────
/// We model both query and document as TF-IDF vectors in a high-dimensional
/// term space. The cosine of the angle between them measures relevance
/// independently of vector length (document length normalisation).
///
/// cos(q, d) = (q · d) / (|q| × |d|)
///
/// We pre-compute document magnitudes |d| at index time so that the
/// division at query time is O(1) per document rather than O(V).
/// </summary>
public static class TFIDFCalculator
{
    // ── Pre-computation (called once at startup) ──────────────────────────

    /// <summary>
    /// Computes IDF for every term in the inverted index.
    /// Returns a dictionary term → IDF value.
    /// </summary>
    public static Dictionary<string, float> ComputeIDFs(InvertedIndex index)
    {
        int n = index.DocumentCount;
        var idfs = new Dictionary<string, float>(StringComparer.Ordinal);

        foreach (string term in index.AllTerms())
        {
            int df = index.GetDocumentFrequency(term);
            // Smoothed IDF: log((N+1)/(df+1)) + 1
            idfs[term] = MathF.Log((n + 1f) / (df + 1f)) + 1f;
        }

        return idfs;
    }

    /// <summary>
    /// Computes the TF-IDF score for a single term in a single document.
    /// </summary>
    public static float ComputeScore(float tf, float idf) => tf * idf;

    /// <summary>
    /// Pre-computes the Euclidean magnitude of the TF-IDF vector for
    /// every document. Used as the denominator in cosine similarity.
    ///
    /// |d| = sqrt( Σ (tf-idf(t,d))² )
    /// </summary>
    public static Dictionary<int, float> ComputeDocumentMagnitudes(
        IEnumerable<Document> documents,
        Dictionary<string, float> idfs)
    {
        var magnitudes = new Dictionary<int, float>();

        foreach (Document doc in documents)
        {
            float sumSq = 0f;
            foreach (var (term, tf) in doc.TermFrequency)
            {
                if (idfs.TryGetValue(term, out float idf))
                {
                    float score = tf * idf;
                    sumSq += score * score;
                }
            }
            magnitudes[doc.Id] = sumSq > 0 ? MathF.Sqrt(sumSq) : 1f;
        }

        return magnitudes;
    }

    /// <summary>
    /// Computes the cosine similarity between a query vector and a document.
    ///
    /// queryVector: term → tf-idf weight for that query term.
    /// document: the target document.
    /// idfs: pre-computed IDF table.
    /// docMagnitude: pre-computed |d|.
    /// queryMagnitude: pre-computed |q| (passed in so it is computed once per query).
    ///
    /// Returns a value in [0, 1] — higher means more relevant.
    /// </summary>
    public static float CosineSimilarity(
        Dictionary<string, float> queryVector,
        Document document,
        Dictionary<string, float> idfs,
        float docMagnitude,
        float queryMagnitude)
    {
        if (queryMagnitude == 0 || docMagnitude == 0) return 0f;

        float dotProduct = 0f;
        foreach (var (term, qWeight) in queryVector)
        {
            if (document.TermFrequency.TryGetValue(term, out float tf)
                && idfs.TryGetValue(term, out float idf))
            {
                dotProduct += qWeight * (tf * idf);
            }
        }

        return dotProduct / (queryMagnitude * docMagnitude);
    }

    /// <summary>
    /// Builds the TF-IDF query vector for a list of (term, boost) pairs.
    /// Query TF is simply 1/|unique query terms|, then multiplied by IDF and boost.
    /// </summary>
    public static (Dictionary<string, float> vector, float magnitude) BuildQueryVector(
        IEnumerable<(string term, float boost)> terms,
        Dictionary<string, float> idfs)
    {
        var vector = new Dictionary<string, float>(StringComparer.Ordinal);
        float queryTf = 1f; // uniform weight per query term

        foreach (var (term, boost) in terms)
        {
            if (!idfs.TryGetValue(term, out float idf)) continue;
            float weight = queryTf * idf * boost;
            if (vector.TryGetValue(term, out float existing))
                vector[term] = Math.Max(existing, weight); // de-duplicate
            else
                vector[term] = weight;
        }

        float mag = 0f;
        foreach (float w in vector.Values)
            mag += w * w;
        mag = MathF.Sqrt(mag);

        return (vector, mag);
    }
}

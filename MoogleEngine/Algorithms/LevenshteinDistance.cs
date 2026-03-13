namespace MoogleEngine.Algorithms;

/// <summary>
/// Computes the Levenshtein edit distance between two strings and uses
/// it to suggest corrections for misspelled query terms.
///
/// What is Levenshtein distance?
///   The minimum number of single-character edits (insertions, deletions,
///   or substitutions) required to transform one string into another.
///   Example: "reculsibidá" → "recursividad" has distance 3.
///
/// Why Levenshtein for suggestions?
///   It handles typos, transpositions, and partial matches in a principled
///   way without needing a hand-crafted dictionary. We combine it with
///   TF-IDF document frequency so that rare, very specific words are
///   preferred over common stop words.
///
/// Algorithm complexity: O(m × n) time, O(min(m,n)) space using the
/// two-row optimisation (we only keep the previous and current rows).
/// </summary>
public static class LevenshteinDistance
{
    /// <summary>
    /// Computes the edit distance between two strings.
    /// Uses the two-row space-optimised dynamic-programming approach.
    /// </summary>
    public static int Compute(string source, string target)
    {
        if (source.Length == 0) return target.Length;
        if (target.Length == 0) return source.Length;
        if (source == target) return 0;

        int m = source.Length;
        int n = target.Length;

        // Keep only two rows of the DP matrix to save memory
        int[] prev = new int[n + 1];
        int[] curr = new int[n + 1];

        // Initialise first row (distance from empty string to each prefix of target)
        for (int j = 0; j <= n; j++)
            prev[j] = j;

        for (int i = 1; i <= m; i++)
        {
            curr[0] = i; // distance from each prefix of source to empty target
            for (int j = 1; j <= n; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1,   // insertion
                             prev[j] + 1),        // deletion
                    prev[j - 1] + cost);           // substitution
            }
            // Swap rows: current becomes previous for next iteration
            (prev, curr) = (curr, prev);
        }

        return prev[n];
    }

    /// <summary>
    /// Given a misspelled word and a collection of candidate words,
    /// returns the best spelling suggestion weighted by:
    ///   1. Edit distance (lower is better).
    ///   2. Document frequency from the index (higher df → word is common → prefer it for suggestions).
    ///
    /// Returns null if no good candidate is found within maxDistance.
    /// </summary>
    /// <param name="misspelled">The word to correct.</param>
    /// <param name="candidates">All words in the vocabulary (from Trie).</param>
    /// <param name="getDocFreq">Callback to retrieve document frequency for a word.</param>
    /// <param name="maxDistance">Maximum edit distance to consider.</param>
    public static string? FindBestSuggestion(
        string misspelled,
        IEnumerable<string> candidates,
        Func<string, int> getDocFreq,
        int maxDistance = 2)
    {
        string? bestWord = null;
        int bestDistance = maxDistance + 1;
        int bestDocFreq = -1;

        foreach (string candidate in candidates)
        {
            // Early-exit optimisation: skip words whose length difference
            // already exceeds maxDistance (they cannot possibly be within it)
            if (Math.Abs(candidate.Length - misspelled.Length) > maxDistance)
                continue;

            int dist = Compute(misspelled, candidate);
            if (dist > maxDistance) continue;

            int df = getDocFreq(candidate);

            // Prefer lower distance; break ties by higher document frequency
            if (dist < bestDistance || (dist == bestDistance && df > bestDocFreq))
            {
                bestDistance = dist;
                bestDocFreq = df;
                bestWord = candidate;
            }
        }

        return bestWord;
    }

    /// <summary>
    /// Builds a corrected suggestion for an entire query by attempting
    /// to fix each term that is NOT in the vocabulary.
    /// Returns null if every term is already valid.
    /// </summary>
    public static string? SuggestQuery(
        string[] queryTerms,
        Func<string, bool> inVocabulary,
        IEnumerable<string> allWords,
        Func<string, int> getDocFreq)
    {
        // Materialise candidates once for performance
        string[] wordList = allWords.ToArray();

        bool anyCorrected = false;
        string[] corrected = new string[queryTerms.Length];

        for (int i = 0; i < queryTerms.Length; i++)
        {
            string term = queryTerms[i];
            if (inVocabulary(term))
            {
                corrected[i] = term;
            }
            else
            {
                string? suggestion = FindBestSuggestion(term, wordList, getDocFreq);
                if (suggestion != null)
                {
                    corrected[i] = suggestion;
                    anyCorrected = true;
                }
                else
                {
                    corrected[i] = term; // leave unchanged if no suggestion found
                }
            }
        }

        return anyCorrected ? string.Join(" ", corrected) : null;
    }
}

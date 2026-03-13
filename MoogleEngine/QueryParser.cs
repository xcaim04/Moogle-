namespace MoogleEngine;

using MoogleEngine.Models;

/// <summary>
/// Parses a raw query string into a <see cref="ProcessedQuery"/> by
/// identifying and stripping operator prefixes from each token.
///
/// Supported operators
/// ───────────────────
/// !word   → Exclusion: no returned document may contain this word.
/// ^word   → Requirement: every returned document must contain this word.
/// ~word   → Proximity: proximity of this word to others boosts the score.
/// *word   → Boost ×2 per leading *.  "**word" boosts ×3, etc.
/// (none)  → Plain search term.
///
/// Operator detection is done on a per-token basis (split on whitespace)
/// so that multi-word queries work naturally.
/// </summary>
public static class QueryParser
{
    /// <summary>
    /// Parses the raw query and returns a <see cref="ProcessedQuery"/>.
    /// </summary>
    public static ProcessedQuery Parse(string rawQuery)
    {
        var result = new ProcessedQuery { RawQuery = rawQuery };

        if (string.IsNullOrWhiteSpace(rawQuery))
            return result;

        string[] tokens = rawQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            string stripped = token.TrimStart();

            // ── Determine leading operators ───────────────────────────────
            bool exclude    = false;
            bool require    = false;
            bool proximity  = false;
            float boost     = 1.0f;

            int opEnd = 0;
            while (opEnd < stripped.Length)
            {
                char c = stripped[opEnd];
                if (c == '!')       { exclude = true;   opEnd++; }
                else if (c == '^')  { require = true;   opEnd++; }
                else if (c == '~')  { proximity = true; opEnd++; }
                else if (c == '*')  { boost += 1.0f;    opEnd++; } // each * adds 1×
                else break;
            }

            string word = DocumentProcessor.NormaliseWord(stripped[opEnd..]);
            if (word.Length == 0) continue;

            // ── Assign to the appropriate sets ───────────────────────────
            if (exclude)
            {
                result.ExcludedTerms.Add(word);
                // Excluded words are never scored — skip to next token
                continue;
            }

            if (require)
                result.RequiredTerms.Add(word);

            if (proximity)
                result.ProximityTerms.Add(word);

            if (boost > 1.0f)
                result.BoostedTerms[word] = boost;

            // All non-excluded terms go into the plain list unless they
            // already have a special role (they are still searched for)
            if (!require && !proximity && boost <= 1.0f)
                result.PlainTerms.Add(word);

            // Every scored term (non-excluded) goes into AllScoredTerms
            result.AllScoredTerms.Add(word);
        }

        return result;
    }

    /// <summary>
    /// Returns a flat list of (term, boost) pairs for building the
    /// TF-IDF query vector.  Required and proximity terms have their
    /// regular boost; boosted terms multiply by their * count.
    /// </summary>
    public static IEnumerable<(string term, float boost)> GetScoredTerms(ProcessedQuery query)
    {
        foreach (string term in query.AllScoredTerms)
        {
            float boost = query.BoostedTerms.TryGetValue(term, out float b) ? b : 1.0f;
            yield return (term, boost);
        }
    }
}

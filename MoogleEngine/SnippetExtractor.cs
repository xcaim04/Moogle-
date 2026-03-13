namespace MoogleEngine;

using MoogleEngine.Models;

/// <summary>
/// Extracts the most relevant snippet from a document given the query terms.
///
/// Strategy
/// ────────
/// 1. Collect all character positions where any query term appears.
/// 2. Find the window of fixed character width (≈ 300 chars) that covers
///    the highest density of query-term positions.
/// 3. Expand the window to the nearest sentence/word boundary to avoid
///    cutting words mid-word.
/// 4. Highlight query terms with surrounding markers (passed to Blazor UI).
///
/// Why a sliding window?
///   A document may contain query terms scattered throughout its text.
///   The window approach finds the *most informative* passage — the region
///   where the most query terms cluster together — rather than just the
///   first occurrence. This is similar to how search engines like Google
///   choose what to show in their result cards.
/// </summary>
public static class SnippetExtractor
{
    private const int SnippetLength = 320;   // target snippet character width
    private const int WindowStep   = 40;     // step size for the sliding window

    /// <summary>
    /// Returns a snippet (plain text, no markers) for the given document
    /// that covers the densest cluster of query terms.
    /// </summary>
    public static string Extract(Document doc, IEnumerable<string> queryTerms)
    {
        string text = doc.RawContent;
        if (text.Length <= SnippetLength)
            return CleanSnippet(text);

        // Collect all hit positions across all query terms
        var hitPositions = new List<int>();
        foreach (string term in queryTerms)
        {
            if (doc.WordPositions.TryGetValue(term, out List<int>? positions))
                hitPositions.AddRange(positions);
        }

        if (hitPositions.Count == 0)
        {
            // No query term found → return beginning of document
            return CleanSnippet(text[..Math.Min(SnippetLength, text.Length)]);
        }

        hitPositions.Sort();

        // Sliding window: find start position that maximises hits inside window
        int bestStart = hitPositions[0];
        int bestCount = 0;

        int left = 0;
        for (int right = 0; right < hitPositions.Count; right++)
        {
            // Shrink window from left while window is too wide
            while (hitPositions[right] - hitPositions[left] > SnippetLength)
                left++;

            int count = right - left + 1;
            if (count > bestCount)
            {
                bestCount = count;
                // Centre the window around the cluster
                int clusterMid = (hitPositions[left] + hitPositions[right]) / 2;
                bestStart = Math.Max(0, clusterMid - SnippetLength / 2);
            }
        }

        // Clamp to document length
        bestStart = Math.Min(bestStart, text.Length - 1);
        int end = Math.Min(bestStart + SnippetLength, text.Length);

        // Expand start backwards to the nearest word boundary
        while (bestStart > 0 && char.IsLetterOrDigit(text[bestStart]))
            bestStart--;

        // Shrink end to the nearest word boundary
        while (end < text.Length && char.IsLetterOrDigit(text[end]))
            end++;

        string snippet = text[bestStart..end].Trim();
        return CleanSnippet(snippet);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    /// <summary>Collapses excess whitespace and newlines in the snippet.</summary>
    private static string CleanSnippet(string text)
    {
        // Replace newlines / tabs with a space and collapse runs
        return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
    }
}

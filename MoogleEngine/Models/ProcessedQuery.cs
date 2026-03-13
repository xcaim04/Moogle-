namespace MoogleEngine.Models;

/// <summary>
/// Represents a user query after parsing all operators.
/// Operators supported:
///   !word  → word must NOT appear in results
///   ^word  → word MUST appear in results
///   ~word  → proximity: prefer docs where this word is close to others
///   *word  → boost: word counts double (accumulates per extra *)
/// </summary>
public class ProcessedQuery
{
    /// <summary>Terms that must appear in every returned document (^ operator).</summary>
    public HashSet<string> RequiredTerms { get; set; } = new();

    /// <summary>Terms that must NOT appear in any returned document (! operator).</summary>
    public HashSet<string> ExcludedTerms { get; set; } = new();

    /// <summary>Terms whose proximity boosts the document score (~ operator).</summary>
    public HashSet<string> ProximityTerms { get; set; } = new();

    /// <summary>
    /// Boosted terms and their multipliers (* operator).
    /// Two leading * symbols → multiplier = 3, three → 4, etc.
    /// </summary>
    public Dictionary<string, float> BoostedTerms { get; set; } = new();

    /// <summary>
    /// All remaining plain terms (no special operator).
    /// </summary>
    public List<string> PlainTerms { get; set; } = new();

    /// <summary>
    /// Union of all terms (plain + required + boosted + proximity)
    /// that should actually contribute to the score.
    /// Excluded terms are NOT in this set.
    /// </summary>
    public HashSet<string> AllScoredTerms { get; set; } = new();

    /// <summary>The original raw query string before parsing.</summary>
    public string RawQuery { get; set; } = string.Empty;
}

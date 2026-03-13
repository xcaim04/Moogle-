namespace MoogleEngine.DataStructures;

/// <summary>
/// A single node in the Trie.
/// </summary>
internal sealed class TrieNode
{
    /// <summary>Child nodes keyed by character.</summary>
    public readonly Dictionary<char, TrieNode> Children = new();

    /// <summary>
    /// If this node marks the end of a word, stores the word itself
    /// along with how many documents contain it (for IDF-based pruning).
    /// </summary>
    public string? Word { get; set; }

    /// <summary>Number of documents that contain this word (used for IDF weighting).</summary>
    public int DocumentFrequency { get; set; }
}

/// <summary>
/// Trie (prefix tree) data structure optimised for:
///   1. O(L) word lookup, where L is word length.
///   2. Prefix-based enumeration for autocomplete.
///   3. Collecting all stored words for Levenshtein-based spell suggestion.
///
/// Why a Trie?
///   A hash-set gives O(1) lookup but cannot enumerate words by prefix.
///   A sorted list gives O(log n) lookup.
///   A Trie gives O(L) lookup AND efficient prefix enumeration, which is
///   critical for building the suggestion feature.
/// </summary>
public sealed class Trie
{
    private readonly TrieNode _root = new();

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Inserts a word into the Trie and records how many documents it appeared in.
    /// Time complexity: O(L).
    /// </summary>
    public void Insert(string word, int documentFrequency = 1)
    {
        TrieNode node = _root;
        foreach (char c in word)
        {
            if (!node.Children.TryGetValue(c, out TrieNode? child))
            {
                child = new TrieNode();
                node.Children[c] = child;
            }
            node = child;
        }
        node.Word = word;
        node.DocumentFrequency = documentFrequency;
    }

    /// <summary>
    /// Returns true if the Trie contains the exact word.
    /// Time complexity: O(L).
    /// </summary>
    public bool Contains(string word)
    {
        TrieNode? node = FindNode(word);
        return node?.Word != null;
    }

    /// <summary>
    /// Retrieves all words that share a given prefix.
    /// Useful for autocomplete / prefix search.
    /// Time complexity: O(L + k) where k is the number of results.
    /// </summary>
    public IEnumerable<string> StartsWith(string prefix)
    {
        TrieNode? node = FindNode(prefix);
        if (node == null) yield break;

        foreach (string word in CollectWords(node))
            yield return word;
    }

    /// <summary>
    /// Returns all words stored in the Trie (used for Levenshtein suggestions).
    /// </summary>
    public IEnumerable<string> AllWords() => CollectWords(_root);

    /// <summary>
    /// Returns document frequency of the word (0 if not found).
    /// </summary>
    public int GetDocumentFrequency(string word)
    {
        TrieNode? node = FindNode(word);
        return node?.DocumentFrequency ?? 0;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private TrieNode? FindNode(string prefix)
    {
        TrieNode node = _root;
        foreach (char c in prefix)
        {
            if (!node.Children.TryGetValue(c, out TrieNode? child))
                return null;
            node = child;
        }
        return node;
    }

    private static IEnumerable<string> CollectWords(TrieNode node)
    {
        if (node.Word != null)
            yield return node.Word;

        foreach (TrieNode child in node.Children.Values)
            foreach (string word in CollectWords(child))
                yield return word;
    }
}

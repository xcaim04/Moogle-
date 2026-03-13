namespace MoogleEngine;

using MoogleEngine.Models;

/// <summary>
/// Responsible for reading .txt files from the Content directory,
/// tokenising their text, and building Document objects ready for indexing.
///
/// Tokenisation pipeline:
///   1. Read raw text from disk.
///   2. Normalise: lower-case, strip punctuation.
///   3. Build word list (token stream).
///   4. Record each token's position (character offset) for proximity and snippet use.
///   5. Compute Term Frequency: TF(t,d) = count(t,d) / |d|.
/// </summary>
public static class DocumentProcessor
{
    // Characters that are considered part of a word
    private static readonly char[] WordSeparators =
        Enumerable.Range(0, 256)
                  .Select(i => (char)i)
                  .Where(c => !char.IsLetterOrDigit(c))
                  .ToArray();

    /// <summary>
    /// Loads all .txt files from the given directory and returns a list of
    /// fully processed Document objects.
    /// </summary>
    public static List<Document> LoadDocuments(string contentDirectory)
    {
        var documents = new List<Document>();

        if (!Directory.Exists(contentDirectory))
        {
            Console.WriteLine($"[DocumentProcessor] Content directory not found: {contentDirectory}");
            return documents;
        }

        string[] files = Directory.GetFiles(contentDirectory, "*.txt");

        for (int i = 0; i < files.Length; i++)
        {
            Document? doc = ProcessFile(files[i], i);
            if (doc != null)
                documents.Add(doc);
        }

        Console.WriteLine($"[DocumentProcessor] Loaded {documents.Count} documents.");
        return documents;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static Document? ProcessFile(string filePath, int id)
    {
        try
        {
            string raw = File.ReadAllText(filePath);
            string title = Path.GetFileNameWithoutExtension(filePath);

            var (wordPositions, totalTokens) = Tokenise(raw);

            // Compute normalised TF for each unique term
            var tf = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var (term, positions) in wordPositions)
            {
                tf[term] = totalTokens > 0
                    ? (float)positions.Count / totalTokens
                    : 0f;
            }

            return new Document
            {
                Id = id,
                Title = FormatTitle(title),
                FilePath = filePath,
                RawContent = raw,
                TermFrequency = tf,
                WordPositions = wordPositions,
                TotalTokens = totalTokens
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentProcessor] Error reading {filePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Converts raw text into a map of term → (sorted list of character offsets).
    /// Also returns the total number of tokens (for TF normalisation).
    ///
    /// Using character offsets (rather than word indices) lets the snippet
    /// extractor take a clean substring of the original text.
    /// </summary>
    public static (Dictionary<string, List<int>> positions, int totalTokens) Tokenise(string text)
    {
        var positions = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        int totalTokens = 0;

        int i = 0;
        while (i < text.Length)
        {
            // Skip non-word characters
            while (i < text.Length && !char.IsLetterOrDigit(text[i]))
                i++;

            if (i >= text.Length) break;

            int start = i;

            // Advance through the word
            while (i < text.Length && char.IsLetterOrDigit(text[i]))
                i++;

            string token = text[start..i].ToLowerInvariant();
            if (token.Length == 0) continue;

            totalTokens++;

            if (!positions.TryGetValue(token, out List<int>? posList))
            {
                posList = new List<int>();
                positions[token] = posList;
            }
            posList.Add(start);
        }

        return (positions, totalTokens);
    }

    /// <summary>
    /// Normalises a single word (lower-case, alphanumeric only).
    /// Used when processing individual query tokens.
    /// </summary>
    public static string NormaliseWord(string word)
        => new string(word.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    /// <summary>Converts snake_case or kebab-case filenames to readable titles.</summary>
    private static string FormatTitle(string filename)
        => filename.Replace('_', ' ').Replace('-', ' ');
}

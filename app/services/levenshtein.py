from app.data_structures.trie import Trie


class LevenshteinDistance:
    @staticmethod
    def distance(s1: str, s2: str) -> int:
        if len(s1) < len(s2):
            return LevenshteinDistance.distance(s2, s1)

        if len(s2) == 0:
            return len(s1)

        prev_row = list(range(len(s2) + 1))
        for i, c1 in enumerate(s1):
            curr_row = [i + 1]
            for j, c2 in enumerate(s2):
                insertions = prev_row[j + 1] + 1
                deletions = curr_row[j] + 1
                substitutions = prev_row[j] + (c1 != c2)
                curr_row.append(min(insertions, deletions, substitutions))
            prev_row = curr_row

        return prev_row[-1]

    @staticmethod
    def find_best_suggestion(
        word: str, trie: Trie, max_distance: int = 2
    ) -> str | None:
        best_word: str | None = None
        best_distance = max_distance + 1
        best_doc_freq = 0

        for vocab_word in trie.all_words():
            dist = LevenshteinDistance.distance(word, vocab_word)
            if dist <= max_distance:
                doc_freq = trie.get_document_frequency(vocab_word)
                if (
                    dist < best_distance
                    or (dist == best_distance and doc_freq > best_doc_freq)
                ):
                    best_distance = dist
                    best_word = vocab_word
                    best_doc_freq = doc_freq

        return best_word

    @staticmethod
    def suggest_query(
        query_terms: list[str], trie: Trie, max_distance: int = 2
    ) -> str:
        corrected = []
        changed = False

        for term in query_terms:
            if trie.contains(term):
                corrected.append(term)
            else:
                suggestion = LevenshteinDistance.find_best_suggestion(
                    term, trie, max_distance
                )
                if suggestion:
                    corrected.append(suggestion)
                    changed = True
                else:
                    corrected.append(term)

        return " ".join(corrected) if changed else ""

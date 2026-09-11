from app.data_structures.trie import Trie
from app.services.levenshtein import LevenshteinDistance


class TestLevenshteinDistance:
    def test_identical_strings(self):
        assert LevenshteinDistance.distance("hello", "hello") == 0

    def test_single_insertion(self):
        assert LevenshteinDistance.distance("hello", "helloo") == 1

    def test_single_deletion(self):
        assert LevenshteinDistance.distance("hello", "hell") == 1

    def test_single_substitution(self):
        assert LevenshteinDistance.distance("hello", "hallo") == 1

    def test_completely_different(self):
        assert LevenshteinDistance.distance("abc", "xyz") == 3

    def test_empty_strings(self):
        assert LevenshteinDistance.distance("", "") == 0
        assert LevenshteinDistance.distance("hello", "") == 5
        assert LevenshteinDistance.distance("", "hello") == 5

    def test_symmetric(self):
        assert LevenshteinDistance.distance("abc", "def") == LevenshteinDistance.distance(
            "def", "abc"
        )


class TestFindBestSuggestion:
    def setup_method(self):
        self.trie = Trie()
        self.trie.insert("hello", 10)
        self.trie.insert("world", 5)
        self.trie.insert("python", 8)

    def test_exact_match_not_needed(self):
        result = LevenshteinDistance.find_best_suggestion("hello", self.trie)
        assert result == "hello"

    def test_close_match(self):
        result = LevenshteinDistance.find_best_suggestion("hell", self.trie, max_distance=2)
        assert result == "hello"

    def test_no_match_too_far(self):
        result = LevenshteinDistance.find_best_suggestion("xyz", self.trie, max_distance=1)
        assert result is None

    def test_prefer_higher_doc_freq(self):
        self.trie.insert("hellp", 20)
        result = LevenshteinDistance.find_best_suggestion("hell", self.trie, max_distance=2)
        assert result == "hellp"


class TestSuggestQuery:
    def setup_method(self):
        self.trie = Trie()
        self.trie.insert("hello", 10)
        self.trie.insert("world", 5)

    def test_no_corrections_needed(self):
        result = LevenshteinDistance.suggest_query(["hello", "world"], self.trie)
        assert result == ""

    def test_correction_applied(self):
        result = LevenshteinDistance.suggest_query(["hell", "world"], self.trie)
        assert result == "hello world"

    def test_partial_correction(self):
        result = LevenshteinDistance.suggest_query(["hell", "xyz"], self.trie)
        assert "hello" in result

from app.data_structures.trie import Trie


class TestTrie:
    def setup_method(self):
        self.trie = Trie()

    def test_insert_and_contains(self):
        self.trie.insert("hello", 5)
        assert self.trie.contains("hello") is True
        assert self.trie.contains("hell") is False

    def test_insert_multiple_words(self):
        self.trie.insert("hello", 5)
        self.trie.insert("world", 3)
        assert self.trie.contains("hello") is True
        assert self.trie.contains("world") is True
        assert self.trie.contains("foo") is False

    def test_starts_with(self):
        self.trie.insert("hello", 5)
        self.trie.insert("help", 3)
        self.trie.insert("world", 1)
        results = self.trie.starts_with("hel")
        assert "hello" in results
        assert "help" in results
        assert "world" not in results

    def test_starts_with_no_match(self):
        self.trie.insert("hello", 5)
        results = self.trie.starts_with("xyz")
        assert results == []

    def test_all_words(self):
        self.trie.insert("apple", 2)
        self.trie.insert("banana", 3)
        self.trie.insert("cherry", 1)
        words = self.trie.all_words()
        assert set(words) == {"apple", "banana", "cherry"}

    def test_get_document_frequency(self):
        self.trie.insert("hello", 5)
        assert self.trie.get_document_frequency("hello") == 5
        assert self.trie.get_document_frequency("nonexistent") == 0

    def test_empty_trie(self):
        assert self.trie.contains("anything") is False
        assert self.trie.all_words() == []
        assert self.trie.starts_with("a") == []

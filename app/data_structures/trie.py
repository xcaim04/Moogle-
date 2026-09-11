from __future__ import annotations


class TrieNode:
    __slots__ = ("children", "word", "document_frequency")

    def __init__(self) -> None:
        self.children: dict[str, TrieNode] = {}
        self.word: str | None = None
        self.document_frequency: int = 0


class Trie:
    def __init__(self) -> None:
        self._root = TrieNode()

    def insert(self, word: str, doc_freq: int = 0) -> None:
        node = self._root
        for char in word:
            if char not in node.children:
                node.children[char] = TrieNode()
            node = node.children[char]
        node.word = word
        node.document_frequency = doc_freq

    def contains(self, word: str) -> bool:
        node = self._root
        for char in word:
            if char not in node.children:
                return False
            node = node.children[char]
        return node.word is not None

    def starts_with(self, prefix: str) -> list[str]:
        node = self._root
        for char in prefix:
            if char not in node.children:
                return []
            node = node.children[char]
        words: list[str] = []
        self._collect(node, words)
        return words

    def all_words(self) -> list[str]:
        words: list[str] = []
        self._collect(self._root, words)
        return words

    def get_document_frequency(self, word: str) -> int:
        node = self._root
        for char in word:
            if char not in node.children:
                return 0
            node = node.children[char]
        return node.document_frequency if node.word else 0

    def _collect(self, node: TrieNode, words: list[str]) -> None:
        if node.word is not None:
            words.append(node.word)
        for child in node.children.values():
            self._collect(child, words)

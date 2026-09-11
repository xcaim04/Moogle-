from app.models.posting import Posting


class InvertedIndex:
    def __init__(self) -> None:
        self._index: dict[str, list[Posting]] = {}

    def add_document(self, doc_id: int, word_positions: dict[str, list[int]]) -> None:
        for word, positions in word_positions.items():
            posting = Posting(document_id=doc_id, positions=tuple(positions))
            self._index.setdefault(word, []).append(posting)

    def get_postings(self, term: str) -> list[Posting]:
        return self._index.get(term, [])

    def get_document_frequency(self, term: str) -> int:
        return len(self._index.get(term, []))

    def all_terms(self) -> list[str]:
        return list(self._index.keys())

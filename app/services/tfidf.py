import math

from app.data_structures.inverted_index import InvertedIndex
from app.models.document import Document
from app.models.query import ProcessedQuery


class TFIDFCalculator:
    def __init__(self, inverted_index: InvertedIndex, total_documents: int) -> None:
        self._index = inverted_index
        self._total_documents = total_documents
        self._idf_cache: dict[str, float] = {}
        self._document_magnitudes: dict[int, float] = {}

    def compute_idfs(self, terms: list[str]) -> None:
        for term in terms:
            df = self._index.get_document_frequency(term)
            self._idf_cache[term] = math.log((self._total_documents + 1) / (df + 1)) + 1

    def compute_document_magnitudes(self, documents: dict[int, Document]) -> None:
        for doc_id, doc in documents.items():
            magnitude = 0.0
            for term, tf in doc.term_frequency.items():
                idf = self._idf_cache.get(term, 0.0)
                tfidf = tf * idf
                magnitude += tfidf * tfidf
            self._document_magnitudes[doc_id] = math.sqrt(magnitude)

    def get_idf(self, term: str) -> float:
        return self._idf_cache.get(term, 0.0)

    def compute_score(self, tf: float, idf: float) -> float:
        return tf * idf

    def cosine_similarity(
        self,
        query_vector: dict[str, float],
        doc_tfidf: dict[str, float],
        doc_magnitude: float,
    ) -> float:
        if doc_magnitude == 0:
            return 0.0

        dot_product = 0.0
        for term, q_weight in query_vector.items():
            if term in doc_tfidf:
                dot_product += q_weight * doc_tfidf[term]

        query_magnitude = math.sqrt(sum(w * w for w in query_vector.values()))
        if query_magnitude == 0:
            return 0.0

        return dot_product / (query_magnitude * doc_magnitude)

    def build_query_vector(self, query: ProcessedQuery) -> dict[str, float]:
        vector: dict[str, float] = {}
        all_terms = (
            query.plain_terms
            | query.required_terms
            | query.proximity_terms
            | set(query.boosted_terms.keys())
        )

        for term in all_terms:
            idf = self._idf_cache.get(term, 0.0)
            boost = query.boosted_terms.get(term, 1.0)
            vector[term] = idf * boost

        return vector

    def build_doc_tfidf(self, doc: Document) -> dict[str, float]:
        return {
            term: tf * self._idf_cache.get(term, 0.0)
            for term, tf in doc.term_frequency.items()
        }

    def get_document_magnitude(self, doc_id: int) -> float:
        return self._document_magnitudes.get(doc_id, 0.0)

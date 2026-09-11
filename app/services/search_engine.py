import logging

from app.config import Settings
from app.data_structures.inverted_index import InvertedIndex
from app.data_structures.trie import Trie
from app.models.document import Document
from app.models.result import SearchItem, SearchResult
from app.repositories.document_repository import DocumentRepository
from app.services.levenshtein import LevenshteinDistance
from app.services.query_parser import QueryParser
from app.services.snippet_extractor import SnippetExtractor
from app.services.tfidf import TFIDFCalculator

logger = logging.getLogger(__name__)


class SearchEngine:
    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._documents: dict[int, Document] = {}
        self._inverted_index = InvertedIndex()
        self._trie = Trie()
        self._tfidf: TFIDFCalculator | None = None
        self._snippet_extractor = SnippetExtractor(
            snippet_length=settings.snippet_length,
            window_step=settings.window_step,
        )

    def build_index(self) -> None:
        repo = DocumentRepository(self._settings.content_dir)
        doc_list = repo.load_documents()

        if not doc_list:
            logger.warning("No documents found in %s", self._settings.content_dir)
            return

        self._documents = {doc.id: doc for doc in doc_list}

        for doc in doc_list:
            self._inverted_index.add_document(doc.id, doc.word_positions)

        all_terms = self._inverted_index.all_terms()
        for term in all_terms:
            df = self._inverted_index.get_document_frequency(term)
            self._trie.insert(term, df)

        self._tfidf = TFIDFCalculator(self._inverted_index, len(doc_list))
        self._tfidf.compute_idfs(all_terms)
        self._tfidf.compute_document_magnitudes(self._documents)

        logger.info("Indexed %d documents with %d unique terms", len(doc_list), len(all_terms))

    def search(self, query: str) -> SearchResult:
        if not self._tfidf or not self._documents:
            return SearchResult()

        try:
            return self._execute_search(query)
        except Exception:
            logger.exception("Error executing search for query: %s", query)
            return SearchResult()

    def _execute_search(self, query: str) -> SearchResult:
        assert self._tfidf is not None

        parsed = QueryParser.parse(query)
        query_vector = self._tfidf.build_query_vector(parsed)

        candidate_ids = self._gather_candidates(parsed)
        scored_results: list[tuple[int, float]] = []

        for doc_id in candidate_ids:
            doc = self._documents[doc_id]

            if parsed.excluded_terms & set(doc.term_frequency.keys()):
                continue

            if parsed.required_terms and not parsed.required_terms.issubset(
                set(doc.term_frequency.keys())
            ):
                continue

            doc_tfidf = self._tfidf.build_doc_tfidf(doc)
            doc_magnitude = self._tfidf.get_document_magnitude(doc_id)
            score = self._tfidf.cosine_similarity(
                query_vector, doc_tfidf, doc_magnitude
            )

            if parsed.proximity_terms:
                score += self._compute_proximity_bonus(doc, parsed.proximity_terms)

            if score > 0:
                scored_results.append((doc_id, score))

        scored_results.sort(key=lambda x: x[1], reverse=True)
        top_results = scored_results[: self._settings.max_results]

        items: list[SearchItem] = []
        for doc_id, score in top_results:
            doc = self._documents[doc_id]
            term_positions = {
                term: doc.word_positions.get(term, [])
                for term in parsed.all_scored_terms
                if term in doc.word_positions
            }
            snippet = self._snippet_extractor.extract(doc.raw_content, term_positions)
            items.append(SearchItem(title=doc.title, snippet=snippet, score=score))

        suggestion = ""
        if len(items) < 3 and parsed.plain_terms:
            suggestion = LevenshteinDistance.suggest_query(
                parsed.plain_terms, self._trie, self._settings.max_levenshtein_distance
            )

        return SearchResult(items=items, suggestion=suggestion)

    def _gather_candidates(self, parsed) -> set[int]:
        candidate_ids: set[int] = set()

        for term in parsed.all_scored_terms:
            for posting in self._inverted_index.get_postings(term):
                candidate_ids.add(posting.document_id)

        return candidate_ids

    def _compute_proximity_bonus(self, doc: Document, proximity_terms: set[str]) -> float:
        all_positions = sorted(
            pos
            for term in proximity_terms
            for pos in doc.word_positions.get(term, [])
        )

        if len(all_positions) < 2:
            return 0.0

        min_gap = all_positions[1] - all_positions[0]
        for i in range(1, len(all_positions)):
            gap = all_positions[i] - all_positions[i - 1]
            if gap < min_gap:
                min_gap = gap

        max_gap = self._settings.snippet_length
        return max(0.0, 1.0 - (min_gap / max_gap))

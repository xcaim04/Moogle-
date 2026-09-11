from app.data_structures.inverted_index import InvertedIndex


class TestInvertedIndex:
    def setup_method(self):
        self.index = InvertedIndex()

    def test_add_document(self):
        self.index.add_document(0, {"hello": [0, 10], "world": [5]})
        assert self.index.get_document_frequency("hello") == 1
        assert self.index.get_document_frequency("world") == 1
        assert self.index.get_document_frequency("missing") == 0

    def test_get_postings(self):
        self.index.add_document(0, {"hello": [0, 10]})
        self.index.add_document(1, {"hello": [5], "world": [0]})
        postings = self.index.get_postings("hello")
        assert len(postings) == 2
        doc_ids = {p.document_id for p in postings}
        assert doc_ids == {0, 1}

    def test_all_terms(self):
        self.index.add_document(0, {"hello": [0], "world": [5]})
        self.index.add_document(1, {"foo": [0]})
        terms = self.index.all_terms()
        assert set(terms) == {"hello", "world", "foo"}

    def test_empty_index(self):
        assert self.index.get_postings("anything") == []
        assert self.index.get_document_frequency("anything") == 0
        assert self.index.all_terms() == []

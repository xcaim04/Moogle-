from app.services.query_parser import QueryParser


class TestQueryParser:
    def test_simple_query(self):
        result = QueryParser.parse("hello world")
        assert result.plain_terms == ["hello", "world"]
        assert result.excluded_terms == set()
        assert result.required_terms == set()

    def test_exclusion_operator(self):
        result = QueryParser.parse("hello !world")
        assert result.plain_terms == ["hello"]
        assert result.excluded_terms == {"world"}

    def test_require_operator(self):
        result = QueryParser.parse("hello ^world")
        assert result.plain_terms == ["hello"]
        assert result.required_terms == {"world"}

    def test_proximity_operator(self):
        result = QueryParser.parse("hello ~world")
        assert result.plain_terms == ["hello"]
        assert result.proximity_terms == {"world"}

    def test_boost_operator(self):
        result = QueryParser.parse("hello *world")
        assert result.plain_terms == ["hello"]
        assert "world" in result.boosted_terms
        assert result.boosted_terms["world"] == 1.5

    def test_multiple_boost_operators(self):
        result = QueryParser.parse("**hello")
        assert "hello" in result.boosted_terms
        assert result.boosted_terms["hello"] == 2.0

    def test_all_scored_terms(self):
        result = QueryParser.parse("plain ^required !excluded ~proximity *boosted")
        assert "plain" in result.all_scored_terms
        assert "required" in result.all_scored_terms
        assert "proximity" in result.all_scored_terms
        assert "boosted" in result.all_scored_terms
        assert "excluded" not in result.all_scored_terms

    def test_case_insensitive(self):
        result = QueryParser.parse("Hello WORLD")
        assert result.plain_terms == ["hello", "world"]

    def test_raw_query_preserved(self):
        result = QueryParser.parse("Hello World")
        assert result.raw_query == "Hello World"

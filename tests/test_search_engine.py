import pytest

from app.config import Settings
from app.services.search_engine import SearchEngine


@pytest.fixture
def content_dir(tmp_path):
    docs = {
        "csharp.txt": (
            "C# is a programming language developed by Microsoft. "
            "It is used for building software applications. "
            "C# runs on the .NET platform. Programming in C# is popular."
        ),
        "python.txt": (
            "Python is a programming language that emphasizes readability. "
            "Programming with Python is fun. Python is used for data science. "
            "The Python programming language supports many paradigms."
        ),
        "readme.txt": (
            "A readme file describes a software project. "
            "It contains documentation for developers. "
            "Good documentation helps software teams."
        ),
    }
    for name, content in docs.items():
        (tmp_path / name).write_text(content, encoding="utf-8")
    return tmp_path


@pytest.fixture
def engine(content_dir):
    settings = Settings(content_path=str(content_dir))
    engine = SearchEngine(settings)
    engine.build_index()
    return engine


class TestSearchEngine:
    def test_build_index(self, engine):
        assert len(engine._documents) == 3

    def test_simple_search(self, engine):
        result = engine.search("programming")
        assert result.count > 0
        assert all(item.score >= 0 for item in result.items)

    def test_exclusion_operator(self, engine):
        result = engine.search("programming !python")
        titles = [item.title for item in result.items]
        assert all("python" not in title for title in titles)

    def test_required_operator(self, engine):
        result = engine.search("^python programming")
        titles = [item.title for item in result.items]
        assert any("python" in title for title in titles)

    def test_ranked_results(self, engine):
        result = engine.search("programming")
        scores = [item.score for item in result.items]
        assert scores == sorted(scores, reverse=True)

    def test_max_results(self, engine):
        result = engine.search("programming")
        assert result.count <= 10

    def test_no_results(self, engine):
        result = engine.search("xyzzy nonexistent")
        assert result.count == 0

    def test_snippet_generation(self, engine):
        result = engine.search("programming")
        assert all(len(item.snippet) > 0 for item in result.items)

    def test_suggestion(self, engine):
        result = engine.search("progtamming")
        assert result.suggestion != ""

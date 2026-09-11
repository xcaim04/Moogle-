import pytest
from fastapi.testclient import TestClient

from app.config import Settings
from app.main import app
from app.services.search_engine import SearchEngine


@pytest.fixture
def content_dir(tmp_path):
    docs = {
        "csharp.txt": (
            "C# is a programming language developed by Microsoft. "
            "It is used for building software applications."
        ),
        "python.txt": (
            "Python is a programming language that emphasizes readability. "
            "Programming with Python is fun."
        ),
    }
    for name, content in docs.items():
        (tmp_path / name).write_text(content, encoding="utf-8")
    return tmp_path


@pytest.fixture
def client(content_dir):
    settings = Settings(content_path=str(content_dir))
    engine = SearchEngine(settings)
    engine.build_index()
    with TestClient(app) as test_client:
        app.state.search_engine = engine
        yield test_client


class TestSearchAPI:
    def test_health(self, client):
        response = client.get("/api/health")
        assert response.status_code == 200
        assert response.json() == {"status": "ok"}

    def test_search_success(self, client):
        response = client.post("/api/search", json={"query": "programming"})
        assert response.status_code == 200
        data = response.json()
        assert data["count"] > 0
        assert "items" in data
        assert isinstance(data["suggestion"], str)

    def test_search_item_structure(self, client):
        response = client.post("/api/search", json={"query": "programming"})
        data = response.json()
        item = data["items"][0]
        assert set(item.keys()) == {"title", "snippet", "score"}

    def test_search_empty_query(self, client):
        response = client.post("/api/search", json={"query": ""})
        assert response.status_code == 422

    def test_search_invalid_body(self, client):
        response = client.post("/api/search", json={})
        assert response.status_code == 422

    def test_root_serves_html(self, client):
        response = client.get("/")
        assert response.status_code == 200
        assert "text/html" in response.headers["content-type"]

    def test_static_css(self, client):
        response = client.get("/static/css/style.css")
        assert response.status_code == 200
        assert "text/css" in response.headers["content-type"]

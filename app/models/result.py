from dataclasses import dataclass, field


@dataclass
class SearchItem:
    title: str
    snippet: str
    score: float


@dataclass
class SearchResult:
    items: list[SearchItem] = field(default_factory=list)
    suggestion: str = ""

    @property
    def count(self) -> int:
        return len(self.items)

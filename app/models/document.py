from dataclasses import dataclass, field


@dataclass
class Document:
    id: int
    title: str
    file_path: str
    raw_content: str
    term_frequency: dict[str, float] = field(default_factory=dict)
    word_positions: dict[str, list[int]] = field(default_factory=dict)
    total_tokens: int = 0

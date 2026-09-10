from dataclasses import dataclass


@dataclass(frozen=True)
class Posting:
    document_id: int
    positions: tuple[int, ...]

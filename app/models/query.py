from dataclasses import dataclass, field


@dataclass
class ProcessedQuery:
    raw_query: str
    plain_terms: list[str] = field(default_factory=list)
    required_terms: set[str] = field(default_factory=set)
    excluded_terms: set[str] = field(default_factory=set)
    proximity_terms: set[str] = field(default_factory=set)
    boosted_terms: dict[str, float] = field(default_factory=dict)
    all_scored_terms: set[str] = field(default_factory=set)

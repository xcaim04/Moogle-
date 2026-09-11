import re

from app.models.query import ProcessedQuery


class QueryParser:
    @staticmethod
    def parse(query: str) -> ProcessedQuery:
        processed = ProcessedQuery(raw_query=query)
        tokens = query.split()

        for token in tokens:
            QueryParser._process_token(token, processed)

        processed.all_scored_terms = (
            set(processed.plain_terms)
            | processed.required_terms
            | processed.proximity_terms
            | set(processed.boosted_terms.keys())
        ) - processed.excluded_terms

        return processed

    @staticmethod
    def _process_token(token: str, processed: ProcessedQuery) -> None:
        word = token
        boost_count = 0.0

        while word and word[0] in "!^~*":
            op = word[0]
            word = word[1:]
            if op == "!":
                processed.excluded_terms.add(QueryParser._normalize(word))
                return
            elif op == "^":
                processed.required_terms.add(QueryParser._normalize(word))
                return
            elif op == "~":
                processed.proximity_terms.add(QueryParser._normalize(word))
                return
            elif op == "*":
                boost_count += 1.0

        normalized = QueryParser._normalize(word)
        if not normalized:
            return

        if boost_count > 0:
            processed.boosted_terms[normalized] = 1.0 + (boost_count * 0.5)
        else:
            processed.plain_terms.append(normalized)

    @staticmethod
    def _normalize(text: str) -> str:
        text = text.lower()
        text = re.sub(r"[^a-z0-9\s]", " ", text)
        text = re.sub(r"\s+", " ", text)
        return text.strip()

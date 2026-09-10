import re


class SnippetExtractor:
    def __init__(self, snippet_length: int = 320, window_step: int = 40) -> None:
        self.snippet_length = snippet_length
        self.window_step = window_step

    def extract(self, raw_content: str, term_positions: dict[str, list[int]]) -> str:
        all_positions = sorted(
            pos for positions in term_positions.values() for pos in positions
        )

        if not all_positions:
            return raw_content[: self.snippet_length].strip()

        best_start = 0
        best_count = 0

        for window_start in range(
            0, len(raw_content), self.window_step
        ):
            window_end = window_start + self.snippet_length
            count = sum(
                1 for pos in all_positions if window_start <= pos < window_end
            )
            if count > best_count:
                best_count = count
                best_start = window_start

        snippet = raw_content[best_start : best_start + self.snippet_length]
        snippet = self._expand_to_word_boundaries(snippet, raw_content, best_start)
        snippet = re.sub(r"\s+", " ", snippet).strip()

        return snippet

    def _expand_to_word_boundaries(
        self, snippet: str, full_text: str, start: int
    ) -> str:
        expanded_start = start
        while expanded_start > 0 and full_text[expanded_start - 1] not in " \n\t":
            expanded_start -= 1

        expanded_end = start + self.snippet_length
        while expanded_end < len(full_text) and full_text[expanded_end] not in " \n\t":
            expanded_end += 1

        return full_text[expanded_start:expanded_end]

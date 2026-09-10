import re
from pathlib import Path

from app.models.document import Document


class DocumentRepository:
    def __init__(self, content_dir: Path) -> None:
        self.content_dir = content_dir

    def load_documents(self) -> list[Document]:
        documents = []
        txt_files = sorted(self.content_dir.glob("*.txt"))

        for doc_id, file_path in enumerate(txt_files):
            raw_content = file_path.read_text(encoding="utf-8", errors="replace")
            title = self._build_title(file_path.stem)
            term_frequency, word_positions, total_tokens = self._tokenize(raw_content)

            documents.append(
                Document(
                    id=doc_id,
                    title=title,
                    file_path=str(file_path),
                    raw_content=raw_content,
                    term_frequency=term_frequency,
                    word_positions=word_positions,
                    total_tokens=total_tokens,
                )
            )

        return documents

    def _build_title(self, stem: str) -> str:
        return stem.replace("_", " ").replace("-", " ")

    def _tokenize(
        self, text: str
    ) -> tuple[dict[str, float], dict[str, list[int]], int]:
        term_freq: dict[str, float] = {}
        word_positions: dict[str, list[int]] = {}
        total_tokens = 0

        normalized = self._normalize(text)
        for match in re.finditer(r"\S+", normalized):
            word = match.group()
            start_pos = match.start()
            total_tokens += 1

            term_freq[word] = term_freq.get(word, 0) + 1
            word_positions.setdefault(word, []).append(start_pos)

        for word in term_freq:
            term_freq[word] /= total_tokens if total_tokens > 0 else 1

        return term_freq, word_positions, total_tokens

    @staticmethod
    def _normalize(text: str) -> str:
        text = text.lower()
        text = re.sub(r"[^a-z0-9\s]", " ", text)
        text = re.sub(r"\s+", " ", text)
        return text.strip()

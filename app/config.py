from pathlib import Path

from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    content_path: str = "./Content"
    max_results: int = 10
    snippet_length: int = 320
    window_step: int = 40
    max_levenshtein_distance: int = 2
    debug: bool = False

    model_config = {"env_prefix": "MOOGLE_"}

    @property
    def content_dir(self) -> Path:
        path = Path(self.content_path)
        if not path.is_absolute():
            path = Path(__file__).parent.parent / path
        return path.resolve()


settings = Settings()

from pydantic import BaseModel, Field


class SearchRequest(BaseModel):
    query: str = Field(..., min_length=1, max_length=500, description="Texto de búsqueda")


class SearchItemResponse(BaseModel):
    title: str
    snippet: str
    score: float = Field(..., ge=0.0)


class SearchResponse(BaseModel):
    items: list[SearchItemResponse]
    suggestion: str = ""
    count: int

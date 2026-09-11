from fastapi import APIRouter, Request

from app.schemas.search import SearchItemResponse, SearchRequest, SearchResponse

router = APIRouter(prefix="/api", tags=["search"])


@router.post("/search", response_model=SearchResponse)
async def search(request: Request, body: SearchRequest) -> SearchResponse:
    engine = request.app.state.search_engine
    result = engine.search(body.query)

    items = [
        SearchItemResponse(
            title=item.title,
            snippet=item.snippet,
            score=round(item.score, 4),
        )
        for item in result.items
    ]

    return SearchResponse(
        items=items,
        suggestion=result.suggestion,
        count=result.count,
    )


@router.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}

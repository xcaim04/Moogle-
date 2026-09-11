from contextlib import asynccontextmanager
from pathlib import Path

from fastapi import FastAPI
from fastapi.staticfiles import StaticFiles

from app.config import settings
from app.middleware import setup_middleware
from app.routers import search
from app.services.search_engine import SearchEngine


@asynccontextmanager
async def lifespan(app: FastAPI):
    engine = SearchEngine(settings)
    engine.build_index()
    app.state.search_engine = engine
    yield


app = FastAPI(
    title="Moogle",
    description="Motor de búsqueda de documentos",
    version="2.0.0",
    lifespan=lifespan,
)

setup_middleware(app)
app.include_router(search.router)
app.mount("/static", StaticFiles(directory=Path(__file__).parent / "static"), name="static")


@app.get("/", include_in_schema=False)
async def root():
    from fastapi.responses import FileResponse

    return FileResponse(Path(__file__).parent / "static" / "index.html")

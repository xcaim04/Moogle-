<div align="center">

```
◈ moogle
```

**An intelligent full-text search engine built with Python and FastAPI.**  
*TF-IDF · Levenshtein Distance · Trie · Inverted Index · Vector Space Model*

[![Python](https://img.shields.io/badge/Python-3.11+-3776AB?style=flat-square&logo=python&logoColor=white)](https://www.python.org/)
[![FastAPI](https://img.shields.io/badge/FastAPI-0.115-009688?style=flat-square&logo=fastapi&logoColor=white)](https://fastapi.tiangolo.com/)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?style=flat-square&logo=docker&logoColor=white)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

[English](#english) · [Español](#español)

</div>

---

## English

### Overview

Moogle is a document search engine that ranks results using the **Vector Space Model** with **TF-IDF** weighting, implements **spell correction** via the Levenshtein edit-distance algorithm, and uses a **Trie** plus an **Inverted Index** for sub-linear lookup performance.

It was originally built as a portfolio project at the Faculty of Mathematics and Computer Science, University of Havana. This version is a full migration from **C# / Blazor Server** to **Python / FastAPI**, following a clean architecture.

---

### Architecture

The project follows a clean architecture with clear separation of concerns:

```
moogle/
├── Content/                   ← .txt files to search (add your own!)
├── app/                       ← Application package
│   ├── main.py                ← FastAPI app entry point (lifespan: index build)
│   ├── config.py              ← Settings via pydantic-settings (MOOGLE_* env vars)
│   ├── middleware.py          ← CORS setup
│   ├── models/                ← Domain models (dataclasses)
│   │   ├── document.py        ← Processed document with TF map + positions
│   │   ├── query.py           ← Parsed query with operator metadata
│   │   ├── result.py          ← SearchItem + SearchResult containers
│   │   └── posting.py         ← Frozen index entry (doc ID + positions)
│   ├── schemas/               ← Pydantic schemas (API validation)
│   │   └── search.py          ← SearchRequest / SearchResponse
│   ├── repositories/          ← Data access layer
│   │   └── document_repository.py ← Reads + tokenizes .txt files
│   ├── data_structures/       ← Core data structures
│   │   ├── trie.py            ← Prefix tree for O(L) lookup & suggestions
│   │   └── inverted_index.py  ← term → postings list (doc IDs + positions)
│   ├── services/              ← Business logic (use cases)
│   │   ├── search_engine.py   ← Main orchestrator (index build + query exec)
│   │   ├── query_parser.py    ← Operator parser (!, ^, ~, *)
│   │   ├── tfidf.py           ← TF-IDF, IDF pre-computation, cosine similarity
│   │   ├── snippet_extractor.py ← Sliding-window snippet finder
│   │   └── levenshtein.py     ← Edit-distance + query spell correction
│   ├── routers/               ← HTTP layer
│   │   └── search.py          ← POST /api/search, GET /api/health
│   └── static/                ← Frontend (HTML, CSS, JS)
├── tests/                     ← pytest unit + integration tests
├── Dockerfile                 ← Production image (non-root user)
├── docker-compose.yml         ← Content volume + env configuration
├── requirements.txt           ← Runtime dependencies
└── pyproject.toml             ← Project metadata, ruff, pytest config
```

**Data flow:**

```
Startup → DocumentRepository reads .txt files
        → InvertedIndex + Trie built from vocabulary
        → IDF values pre-computed for all terms
        → Document magnitude vectors pre-computed

Query   → QueryParser detects operators (!, ^, ~, *)
        → InvertedIndex returns candidate doc IDs
        → TF-IDF cosine similarity scored per document
        → Operator filters applied (exclusion, requirement, proximity)
        → Results sorted by score → Snippets extracted
        → If sparse: LevenshteinDistance suggests correction
```

---

### API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Serves the web UI |
| `/api/search` | POST | `{"query": "..."}` → ranked results with snippets |
| `/api/health` | GET | Health check |
| `/docs` | GET | Interactive OpenAPI documentation (Swagger UI) |

---

### Algorithms & Data Structures Explained

#### 1. Inverted Index

The inverted index maps every unique word to the list of documents that contain it — called a *postings list*. Each entry also stores the character positions of the word inside the document.

**Why?** Without an inverted index, a search would require reading every document for every query term: O(D × L) per query. With the index, candidate documents are retrieved in O(1) per term.

#### 2. Trie (Prefix Tree)

A Trie stores the entire vocabulary with O(L) insert and lookup, where L is the word length. Each leaf node holds the word and its document frequency.

**Why?** Used for two purposes:
- Fast `Contains(word)` checks during spell correction (O(L) vs O(n) for a list).
- Enumerating all stored words to feed the Levenshtein corrector without a separate array.

#### 3. TF-IDF + Cosine Similarity

TF-IDF is a classical Information Retrieval weighting scheme:

```
TF(t, d)    = count(t in d) / totalTokens(d)          (normalised frequency)
IDF(t)      = log((N + 1) / (df(t) + 1)) + 1          (rarity bonus)
TF-IDF(t,d) = TF(t,d) × IDF(t)
```

Both query and documents are modelled as TF-IDF vectors. The angle between them (cosine similarity) measures relevance regardless of document length:

```
cos(q, d) = (q · d) / (|q| × |d|)
```

Document magnitudes `|d|` are pre-computed at index time, so the cosine division at query time is O(1) per document.

#### 4. Levenshtein Edit Distance

Levenshtein distance is the minimum number of single-character edits (insert, delete, substitute) to transform one string into another. We use the standard 2-row DP approach for O(m×n) time and O(min(m,n)) space.

**Why?** When a query returns few results (< 3), we check each query term against the entire Trie vocabulary. The closest word (by edit distance, tie-broken by document frequency) is suggested as a correction.

#### 5. Sliding-Window Snippet Extractor

Instead of returning the first occurrence of a query term, the snippet extractor finds the dense *cluster* of query terms in the document using a sliding window.

**Why?** The snippet shown in the result card should be the most informative passage — the region where the most query words co-occur.

---

### Query Operators

| Operator | Example | Effect |
|----------|---------|--------|
| `!word`  | `algorithms !sorting` | Excludes any document containing *sorting* |
| `^word`  | `^recursion algorithms` | Only documents containing *recursion* |
| `~word`  | `binary ~search tree` | Boosts documents where *search* is close to other terms |
| `*word`  | `**sorting algorithms` | Multiplies *sorting*'s weight (one × per `*`) |

Operators can be combined: `^recursion *algorithms !sorting`

---

### Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| Python | **3.11+** | `python --version` to check |
| Docker | 20+ (optional) | Recommended for containerized deployment |

---

### Installation & Running

#### With Docker (recommended)

```bash
# 1. Add your .txt documents to the Content folder
cp your_documents/*.txt Content/

# 2. Build and run
docker compose up --build
```

Open your browser at **http://localhost:8000**.

#### Without Docker

```bash
# 1. Create and activate a virtual environment
python -m venv .venv
source .venv/bin/activate

# 2. Install dependencies
pip install -r requirements.txt

# 3. Add your documents and run
cp your_documents/*.txt Content/
uvicorn app.main:app --reload
```

Open your browser at **http://localhost:8000**.

---

### Adding Your Own Documents

Simply drop `.txt` files into the `Content/` folder and restart the application. Moogle will index them automatically at startup. The more documents you add, the better TF-IDF discrimination becomes.

---

### Configuration

All settings are read from environment variables prefixed with `MOOGLE_` (see `.env.example`):

| Variable | Default | Description |
|----------|---------|-------------|
| `MOOGLE_CONTENT_PATH` | `./Content` | Directory containing `.txt` documents |
| `MOOGLE_MAX_RESULTS` | `10` | Maximum results per search |
| `MOOGLE_SNIPPET_LENGTH` | `320` | Snippet length in characters |
| `MOOGLE_DEBUG` | `false` | Enable debug logging |

---

### Running Tests

```bash
pip install -r requirements.txt
pip install pytest==8.3.4 httpx==0.28.1
pytest tests/
```

---

### License

MIT — see [LICENSE](LICENSE).

---
---

## Español

### Descripción General

Moogle es un motor de búsqueda de documentos que clasifica los resultados usando el **Modelo de Espacio Vectorial** con pesos **TF-IDF**, implementa **corrección ortográfica** mediante el algoritmo de distancia de edición de Levenshtein, y utiliza un **Trie** más un **Índice Invertido** para búsquedas en tiempo sub-lineal.

Esta versión es una migración completa desde **C# / Blazor Server** a **Python / FastAPI**, siguiendo una arquitectura limpia.

---

### Arquitectura

El proyecto sigue una arquitectura limpia con separación clara de responsabilidades:

```
moogle/
├── Content/                     ← Archivos .txt a buscar (¡añade los tuyos!)
├── app/                         ← Paquete de la aplicación
│   ├── main.py                  ← Punto de entrada FastAPI (lifespan: construcción del índice)
│   ├── config.py                ← Configuración con pydantic-settings (variables MOOGLE_*)
│   ├── middleware.py            ← Setup de CORS
│   ├── models/                  ← Modelos de dominio (dataclasses)
│   │   ├── document.py          ← Documento procesado con mapa TF + posiciones
│   │   ├── query.py             ← Query parseada con metadatos de operadores
│   │   ├── result.py            ← Contenedores SearchItem + SearchResult
│   │   └── posting.py           ← Entrada de índice congelada (ID + posiciones)
│   ├── schemas/                 ← Schemas Pydantic (validación de API)
│   │   └── search.py            ← SearchRequest / SearchResponse
│   ├── repositories/            ← Capa de acceso a datos
│   │   └── document_repository.py ← Lee y tokeniza archivos .txt
│   ├── data_structures/         ← Estructuras de datos núcleo
│   │   ├── trie.py              ← Árbol de prefijos para búsqueda O(L)
│   │   └── inverted_index.py    ← término → lista de postings
│   ├── services/                ← Lógica de negocio (casos de uso)
│   │   ├── search_engine.py     ← Orquestador principal (índice + ejecución)
│   │   ├── query_parser.py      ← Parser de operadores (!, ^, ~, *)
│   │   ├── tfidf.py             ← TF-IDF, precálculo de IDF, similitud coseno
│   │   ├── snippet_extractor.py ← Extractor de fragmentos por ventana deslizante
│   │   └── levenshtein.py       ← Distancia de edición + corrección de queries
│   ├── routers/                 ← Capa HTTP
│   │   └── search.py            ← POST /api/search, GET /api/health
│   └── static/                  ← Frontend (HTML, CSS, JS)
├── tests/                       ← Tests unitarios y de integración (pytest)
├── Dockerfile                   ← Imagen de producción (usuario no-root)
├── docker-compose.yml           ← Volumen de Content + configuración
├── requirements.txt             ← Dependencias de runtime
└── pyproject.toml               ← Metadatos, ruff, configuración de pytest
```

**Flujo de datos:**

```
Arranque → DocumentRepository lee los .txt
         → Se construyen InvertedIndex + Trie
         → Se pre-calculan los valores IDF
         → Se pre-calculan las magnitudes de los vectores de documentos

Query    → QueryParser detecta operadores (!, ^, ~, *)
         → InvertedIndex devuelve los IDs de documentos candidatos
         → Similitud coseno TF-IDF puntuada por documento
         → Se aplican filtros de operadores (exclusión, requerimiento, proximidad)
         → Resultados ordenados por score → Snippets extraídos
         → Si hay pocos resultados: LevenshteinDistance sugiere corrección
```

---

### API

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/` | GET | Sirve la interfaz web |
| `/api/search` | POST | `{"query": "..."}` → resultados rankeados con snippets |
| `/api/health` | GET | Health check |
| `/docs` | GET | Documentación interactiva OpenAPI (Swagger UI) |

---

### Operadores de Query

| Operador | Ejemplo | Efecto |
|----------|---------|--------|
| `!palabra`  | `algoritmos !ordenacion` | Excluye documentos que contengan *ordenacion* |
| `^palabra`  | `^recursion algoritmos` | Solo documentos que contengan *recursion* |
| `~palabra`  | `busqueda ~binaria arbol` | Favorece documentos donde *binaria* está cerca de otros términos |
| `*palabra`  | `**ordenacion algoritmos` | Multiplica el peso de *ordenacion* (un × por `*`) |

Los operadores se pueden combinar: `^recursion *algoritmos !ordenacion`

---

### Requisitos Previos

| Herramienta | Versión | Notas |
|-------------|---------|-------|
| Python | **3.11+** | `python --version` para verificar |
| Docker | 20+ (opcional) | Recomendado para despliegue contenerizado |

---

### Instalación y Ejecución

#### Con Docker (recomendado)

```bash
# 1. Añadir tus documentos .txt a la carpeta Content
cp tus_documentos/*.txt Content/

# 2. Construir y ejecutar
docker compose up --build
```

Abre tu navegador en **http://localhost:8000**.

#### Sin Docker

```bash
# 1. Crear y activar el entorno virtual
python -m venv .venv
source .venv/bin/activate

# 2. Instalar dependencias
pip install -r requirements.txt

# 3. Añadir documentos y ejecutar
cp tus_documentos/*.txt Content/
uvicorn app.main:app --reload
```

Abre tu navegador en **http://localhost:8000**.

---

### Añadir Tus Propios Documentos

Simplemente coloca archivos `.txt` en la carpeta `Content/` y reinicia la aplicación. Moogle los indexará automáticamente al arrancar. Cuantos más documentos añadas, mejor será la discriminación TF-IDF.

---

### Configuración

Toda la configuración se lee de variables de entorno con prefijo `MOOGLE_` (ver `.env.example`):

| Variable | Valor por defecto | Descripción |
|----------|-------------------|-------------|
| `MOOGLE_CONTENT_PATH` | `./Content` | Directorio con los documentos `.txt` |
| `MOOGLE_MAX_RESULTS` | `10` | Máximo de resultados por búsqueda |
| `MOOGLE_SNIPPET_LENGTH` | `320` | Longitud del snippet en caracteres |
| `MOOGLE_DEBUG` | `false` | Activa logging de depuración |

---

### Ejecutar Tests

```bash
pip install -r requirements.txt
pip install pytest==8.3.4 httpx==0.28.1
pytest tests/
```

---

### Licencia

MIT — ver [LICENSE](LICENSE).
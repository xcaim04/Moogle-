<div align="center">

```
◈ moogle
```

**An intelligent full-text search engine built with C# and Blazor.**  
*TF-IDF · Levenshtein Distance · Trie · Inverted Index · Vector Space Model*

[![.NET](https://img.shields.io/badge/.NET-6.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-10.0-239120?style=flat-square)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?style=flat-square)](https://blazor.net/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

[English](#english) · [Español](#español)

</div>

---

## English

### Overview

Moogle is a document search engine that ranks results using the **Vector Space Model** with **TF-IDF** weighting, implements **spell correction** via the Levenshtein edit-distance algorithm, and uses a **Trie** plus an **Inverted Index** for sub-linear lookup performance.

It was built as a portfolio project at the Faculty of Mathematics and Computer Science, University of Havana. The codebase is intentionally clean, heavily documented, and designed to demonstrate real algorithms and data structures.

---

### Architecture

```
moogle/
├── Content/                   ← .txt files to search (add your own!)
├── MoogleEngine/              ← Core library (pure C#, no web deps)
│   ├── Models/
│   │   ├── Document.cs        ← Processed document with TF map + positions
│   │   └── ProcessedQuery.cs  ← Parsed query with operator metadata
│   ├── DataStructures/
│   │   ├── Trie.cs            ← Prefix tree for O(L) lookup & suggestions
│   │   └── InvertedIndex.cs   ← term → postings list (doc IDs + positions)
│   ├── Algorithms/
│   │   ├── TFIDFCalculator.cs ← TF-IDF, IDF pre-computation, cosine similarity
│   │   └── LevenshteinDistance.cs ← Edit-distance + query spell correction
│   ├── DocumentProcessor.cs   ← File reader + tokeniser
│   ├── QueryParser.cs         ← Operator parser (!, ^, ~, *)
│   ├── SearchEngine.cs        ← Main orchestrator (index build + query exec)
│   ├── SnippetExtractor.cs    ← Sliding-window snippet finder
│   ├── Moogle.cs              ← Public facade (lazy singleton engine)
│   ├── SearchResult.cs        ← Query result container
│   └── SearchItem.cs          ← Single result (title, snippet, score)
└── MoogleServer/              ← Blazor Server web app (UI)
    ├── Pages/Index.razor      ← Main search page (all UI logic)
    └── wwwroot/css/site.css   ← Full design system (dark editorial theme)
```

**Data flow:**

```
Startup → DocumentProcessor reads .txt files
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

**Why?** Documents with rare, specific terms score higher for rare queries. Common stop-words (appearing in most documents) get a near-zero IDF automatically — no hand-coded stop-word list needed.

Document magnitudes `|d|` are pre-computed at index time, so the cosine division at query time is O(1) per document.

#### 4. Levenshtein Edit Distance

Levenshtein distance is the minimum number of single-character edits (insert, delete, substitute) to transform one string into another. We use the standard 2-row DP approach for O(m×n) time and O(min(m,n)) space.

```
"algoritmo" → "algorithm" = 2 edits (substitute o→h, substitute o→m... wait, insert)
```

**Why?** When a query returns few results (< 3), we check each query term against the entire Trie vocabulary. The closest word (by edit distance, tie-broken by document frequency) is suggested as a correction.

#### 5. Sliding-Window Snippet Extractor

Instead of returning the first occurrence of a query term, the snippet extractor finds the dense *cluster* of query terms in the document using a two-pointer sliding window.

**Why?** The snippet shown in the result card should be the most informative passage — the region where the most query words co-occur — just like Google's result cards.

---

### Query Operators

| Operator | Example | Effect |
|----------|---------|--------|
| `!word`  | `algorithms !sorting` | Excludes any document containing *sorting* |
| `^word`  | `^recursion algorithms` | Only documents containing *recursion* |
| `~word`  | `binary ~search tree` | Boosts documents where *search* is close to other terms |
| `*word`  | `**sorting algorithms` | Multiplies *sorting*'s weight by 3 (one × per `*`) |

Operators can be combined: `^recursion *algorithms !sorting`

---

### Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| [.NET SDK](https://dotnet.microsoft.com/download) | **6.0 or later** | `dotnet --version` to check |
| Any terminal | — | Linux, macOS, WSL2 on Windows |
| make (optional) | — | Only needed for `make dev` shortcut |

No database, no Docker, no Node.js required.

---

### Installation & Running

```bash
# 1. Clone (or unzip) the project
git clone https://github.com/youruser/moogle.git
cd moogle

# 2. Add your .txt documents to the Content folder
cp your_documents/*.txt Content/

# 3. Run the application
make dev
# or directly:
dotnet watch run --project MoogleServer
```

Open your browser at **http://localhost:5000** (or whichever port is shown in the terminal).

The index is built automatically on first query. Subsequent queries are instant.

---

### Adding Your Own Documents

Simply drop `.txt` files into the `Content/` folder and restart the application. Moogle will index them automatically. The more documents you add, the better TF-IDF discrimination becomes.

---

### Project Structure Deep-Dive

| File | Responsibility |
|------|---------------|
| `DocumentProcessor.cs` | Reads .txt files, tokenises text (lower-case, alphanumeric only), computes TF maps and character-level word positions |
| `QueryParser.cs` | Strips operator characters from each token, classifies each word as plain / required / excluded / proximity / boosted |
| `SearchEngine.cs` | Orchestrates index build and query execution; applies all operator filters; delegates scoring, snippets, and suggestions to specialised classes |
| `TFIDFCalculator.cs` | Computes IDF table (once), document magnitude vectors (once), query vectors (per query), and cosine similarity (per candidate document) |
| `LevenshteinDistance.cs` | Two-row DP edit distance; `FindBestSuggestion` scans vocabulary; `SuggestQuery` corrects all plain query terms |
| `Trie.cs` | Prefix-tree insert/lookup/enumerate; stores document frequency per word for IDF-guided suggestion ranking |
| `InvertedIndex.cs` | Maps term → `List<Posting>` (docId + positions); `GetDocumentFrequency` is O(1) for IDF computation |
| `SnippetExtractor.cs` | Sliding two-pointer window over sorted hit positions; centres snippet on densest term cluster |
| `Moogle.cs` | Thread-safe lazy singleton; resolves Content directory path; catches all engine exceptions before they reach the UI |

---

### Running Tests (manual)

Since .NET is the only dependency, you can add an `xUnit` test project:

```bash
dotnet new xunit -n MoogleTests
dotnet add MoogleTests/MoogleTests.csproj reference MoogleEngine/MoogleEngine.csproj
dotnet test
```

Example unit test ideas:
- `LevenshteinDistance.Compute("kitten", "sitting") == 3`
- `Trie.Contains("algorithm") == true` after insert
- `TFIDFCalculator.ComputeScore(0.1f, 5f) == 0.5f`

---

### License

MIT — see [LICENSE](LICENSE).

---
---

## Español

### Descripción General

Moogle es un motor de búsqueda de documentos que clasifica los resultados usando el **Modelo de Espacio Vectorial** con pesos **TF-IDF**, implementa **corrección ortográfica** mediante el algoritmo de distancia de edición de Levenshtein, y utiliza un **Trie** más un **Índice Invertido** para búsquedas en tiempo sub-lineal.

---

### Arquitectura

```
moogle/
├── Content/                     ← Archivos .txt a buscar (¡añade los tuyos!)
├── MoogleEngine/                ← Biblioteca principal (C# puro, sin dependencias web)
│   ├── Models/
│   │   ├── Document.cs          ← Documento procesado con mapa TF + posiciones
│   │   └── ProcessedQuery.cs    ← Query parseada con metadatos de operadores
│   ├── DataStructures/
│   │   ├── Trie.cs              ← Árbol de prefijos para búsqueda O(L)
│   │   └── InvertedIndex.cs     ← término → lista de postings
│   ├── Algorithms/
│   │   ├── TFIDFCalculator.cs   ← TF-IDF, precálculo de IDF, similitud coseno
│   │   └── LevenshteinDistance.cs ← Distancia de edición + corrección de queries
│   ├── DocumentProcessor.cs     ← Lector de archivos + tokenizador
│   ├── QueryParser.cs           ← Parser de operadores (!, ^, ~, *)
│   ├── SearchEngine.cs          ← Orquestador principal
│   ├── SnippetExtractor.cs      ← Extractor de fragmentos por ventana deslizante
│   ├── Moogle.cs                ← Fachada pública (singleton lazy)
│   ├── SearchResult.cs          ← Contenedor de resultados
│   └── SearchItem.cs            ← Resultado individual (título, snippet, score)
└── MoogleServer/                ← Aplicación web Blazor Server
    ├── Pages/Index.razor        ← Página de búsqueda principal
    └── wwwroot/css/site.css     ← Sistema de diseño completo (tema oscuro editorial)
```

**Flujo de datos:**

```
Arranque → DocumentProcessor lee los .txt
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

### Algoritmos y Estructuras de Datos Explicados

#### 1. Índice Invertido

El índice invertido mapea cada palabra única a la lista de documentos que la contienen — llamada *lista de postings*. Cada entrada también guarda las posiciones de carácter de la palabra dentro del documento.

**¿Por qué?** Sin índice invertido, una búsqueda requeriría leer cada documento para cada término de la query: O(D × L) por búsqueda. Con el índice, los documentos candidatos se recuperan en O(1) por término.

#### 2. Trie (Árbol de Prefijos)

Un Trie almacena todo el vocabulario con insert y lookup en O(L), donde L es la longitud de la palabra. Cada nodo hoja guarda la palabra y su frecuencia de documento.

**¿Por qué?** Se usa para dos propósitos:
- Verificaciones rápidas `Contains(palabra)` durante la corrección ortográfica (O(L) frente a O(n) de una lista).
- Enumerar todas las palabras almacenadas para alimentar el corrector Levenshtein sin un arreglo separado.

#### 3. TF-IDF + Similitud Coseno

TF-IDF es un esquema clásico de ponderación en Recuperación de Información:

```
TF(t, d)    = count(t en d) / totalTokens(d)        (frecuencia normalizada)
IDF(t)      = log((N + 1) / (df(t) + 1)) + 1        (bonus de rareza)
TF-IDF(t,d) = TF(t,d) × IDF(t)
```

Tanto la query como los documentos se modelan como vectores TF-IDF. El ángulo entre ellos (similitud coseno) mide la relevancia independientemente de la longitud del documento:

```
cos(q, d) = (q · d) / (|q| × |d|)
```

**¿Por qué?** Los documentos con términos raros y específicos obtienen mayor puntuación para queries raras. Las palabras vacías comunes (que aparecen en la mayoría de los documentos) obtienen un IDF cercano a cero automáticamente — sin necesidad de listas de stop-words codificadas a mano.

Las magnitudes de documento `|d|` se pre-calculan al indexar, de modo que la división coseno en tiempo de query es O(1) por documento.

#### 4. Distancia de Edición de Levenshtein

La distancia de Levenshtein es el número mínimo de ediciones de un solo carácter (inserción, eliminación, sustitución) para transformar una cadena en otra. Usamos el enfoque estándar de DP de 2 filas para tiempo O(m×n) y espacio O(min(m,n)).

**¿Por qué?** Cuando una query devuelve pocos resultados (< 3), comparamos cada término de la query con todo el vocabulario del Trie. La palabra más cercana (por distancia de edición, con desempate por frecuencia de documento) se sugiere como corrección.

#### 5. Extractor de Snippets por Ventana Deslizante

En lugar de devolver la primera ocurrencia de un término de la query, el extractor de snippets encuentra el *clúster denso* de términos de query en el documento usando una ventana deslizante de dos punteros.

**¿Por qué?** El snippet mostrado en la tarjeta de resultado debe ser el pasaje más informativo — la región donde co-ocurren la mayor cantidad de palabras de la query — tal como hacen los motores de búsqueda comerciales.

---

### Operadores de Query

| Operador | Ejemplo | Efecto |
|----------|---------|--------|
| `!palabra`  | `algoritmos !ordenacion` | Excluye documentos que contengan *ordenacion* |
| `^palabra`  | `^recursion algoritmos` | Solo documentos que contengan *recursion* |
| `~palabra`  | `busqueda ~binaria arbol` | Favorece documentos donde *binaria* está cerca de otros términos |
| `*palabra`  | `**ordenacion algoritmos` | Multiplica el peso de *ordenacion* por 3 (un × por `*`) |

Los operadores se pueden combinar: `^recursion *algoritmos !ordenacion`

---

### Requisitos Previos

| Herramienta | Versión | Notas |
|-------------|---------|-------|
| [SDK de .NET](https://dotnet.microsoft.com/download) | **6.0 o superior** | `dotnet --version` para verificar |
| Cualquier terminal | — | Linux, macOS, WSL2 en Windows |
| make (opcional) | — | Solo necesario para el atajo `make dev` |

No se requiere base de datos, Docker ni Node.js.

---

### Instalación y Ejecución

```bash
# 1. Clonar (o descomprimir) el proyecto
git clone https://github.com/tuusuario/moogle.git
cd moogle

# 2. Añadir tus documentos .txt a la carpeta Content
cp tus_documentos/*.txt Content/

# 3. Ejecutar la aplicación
make dev
# o directamente:
dotnet watch run --project MoogleServer
```

Abre tu navegador en **http://localhost:5000** (o el puerto que se muestre en la terminal).

El índice se construye automáticamente en la primera búsqueda. Las búsquedas posteriores son instantáneas.

---

### Añadir Tus Propios Documentos

Simplemente coloca archivos `.txt` en la carpeta `Content/` y reinicia la aplicación. Moogle los indexará automáticamente. Cuantos más documentos añadas, mejor será la discriminación TF-IDF.

---

### Tabla Resumen de Archivos

| Archivo | Responsabilidad |
|---------|----------------|
| `DocumentProcessor.cs` | Lee archivos .txt, tokeniza el texto (minúsculas, solo alfanumérico), calcula mapas TF y posiciones a nivel de carácter |
| `QueryParser.cs` | Elimina caracteres de operador de cada token, clasifica cada palabra como simple / requerida / excluida / proximidad / potenciada |
| `SearchEngine.cs` | Orquesta la construcción del índice y la ejecución de queries; aplica todos los filtros de operadores |
| `TFIDFCalculator.cs` | Calcula la tabla IDF (una vez), vectores de magnitud de documentos (una vez), vectores de query (por query), similitud coseno (por documento candidato) |
| `LevenshteinDistance.cs` | DP de 2 filas para distancia de edición; `FindBestSuggestion` escanea el vocabulario; `SuggestQuery` corrige todos los términos simples |
| `Trie.cs` | Árbol de prefijos insert/lookup/enumerar; almacena frecuencia de documento por palabra |
| `InvertedIndex.cs` | Mapea término → `List<Posting>` (docId + posiciones) |
| `SnippetExtractor.cs` | Ventana deslizante de dos punteros sobre posiciones de hits ordenadas |
| `Moogle.cs` | Singleton lazy thread-safe; resuelve la ruta del directorio Content |

---

### Proceso de Desarrollo: Por Qué Elegí Cada Tecnología

#### ¿Por qué C# y .NET 6?
El proyecto lo especificaba. C# tiene un sistema de tipos expresivo, soporte nativo para `IEnumerable`, `Dictionary`, y colecciones genéricas que hacen que los algoritmos sean naturales de implementar. .NET 6 es LTS y tiene excelente rendimiento.

#### ¿Por qué Blazor Server?
Permite escribir UI interactiva en C# sin JavaScript. La comunicación entre el motor de búsqueda (biblioteca de clases) y la interfaz es una llamada directa a método — sin REST API, sin serialización JSON, sin latencia de red adicional.

#### ¿Por qué un Índice Invertido y no un simple bucle?
Con 1000 documentos de 10,000 palabras cada uno, un bucle doble haría 10 millones de comparaciones por búsqueda. El índice invertido reduce esto a recuperar solo los documentos relevantes — típicamente < 1% del corpus.

#### ¿Por qué TF-IDF y no contar ocurrencias?
Contar ocurrencias da el mismo peso a "el", "la", "de" y a "recursión", "algoritmo". TF-IDF penaliza automáticamente las palabras que aparecen en todos los documentos — sin listas de stop-words codificadas — y premia los términos específicos y raros.

#### ¿Por qué Trie y no HashSet para el vocabulario?
Un HashSet tiene O(1) lookup pero no puede enumerar palabras por prefijo ni listar todas las palabras eficientemente para Levenshtein. El Trie da O(L) lookup Y enumeración eficiente del vocabulario completo.

#### ¿Por qué Levenshtein y no solo "¿quisiste decir"?
La corrección ortográfica basada en distancia de edición es el estándar de la industria. Maneja errores tipográficos, transposiciones y omisiones de una manera matemáticamente principiada sin necesitar un diccionario externo.

---

### Licencia

MIT — ver [LICENSE](LICENSE).


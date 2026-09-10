const API_URL = '/api/search';

const searchInput = document.getElementById('search-input');
const searchBtn = document.getElementById('search-btn');
const heroSection = document.getElementById('hero');
const resultsSection = document.getElementById('results-section');
const resultsInfo = document.getElementById('results-info');
const resultsList = document.getElementById('results-list');
const suggestionBanner = document.getElementById('suggestion-banner');
const suggestionBtn = document.getElementById('suggestion-btn');
const noResults = document.getElementById('no-results');
const loading = document.getElementById('loading');

let lastQuery = '';

function showHero() {
    heroSection.style.display = 'block';
    resultsSection.style.display = 'none';
    noResults.style.display = 'none';
    suggestionBanner.style.display = 'none';
    loading.style.display = 'none';
}

function showLoading() {
    heroSection.style.display = 'none';
    resultsSection.style.display = 'none';
    noResults.style.display = 'none';
    suggestionBanner.style.display = 'none';
    loading.style.display = 'block';
}

function showResults(data) {
    loading.style.display = 'none';

    if (data.count === 0) {
        noResults.style.display = 'block';
        resultsSection.style.display = 'none';
        return;
    }

    noResults.style.display = 'none';
    resultsSection.style.display = 'block';

    resultsInfo.textContent = `${data.count} resultado${data.count !== 1 ? 's' : ''} encontrado${data.count !== 1 ? 's' : ''}`;

    resultsList.innerHTML = '';
    data.items.forEach((item, index) => {
        const card = document.createElement('div');
        card.className = 'result-card';
        card.style.animationDelay = `${index * 0.05}s`;

        const scorePercent = Math.min(item.score * 100, 100);

        card.innerHTML = `
            <div class="result-header">
                <span class="result-title">${escapeHtml(item.title)}</span>
                <span class="result-score">${item.score.toFixed(4)}</span>
            </div>
            <p class="result-snippet">${escapeHtml(item.snippet)}</p>
            <div class="score-bar">
                <div class="score-fill" style="width: ${scorePercent}%"></div>
            </div>
        `;

        resultsList.appendChild(card);
    });

    if (data.suggestion) {
        suggestionBanner.style.display = 'block';
        suggestionBtn.textContent = data.suggestion;
    } else {
        suggestionBanner.style.display = 'none';
    }
}

async function runSearch(query) {
    if (!query.trim()) return;

    lastQuery = query.trim();
    showLoading();

    try {
        const response = await fetch(API_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ query: lastQuery }),
        });

        if (!response.ok) throw new Error(`HTTP ${response.status}`);

        const data = await response.json();
        showResults(data);
    } catch (err) {
        console.error('Search failed:', err);
        loading.style.display = 'none';
        noResults.style.display = 'block';
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

searchBtn.addEventListener('click', () => runSearch(searchInput.value));

searchInput.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') runSearch(searchInput.value);
});

suggestionBtn.addEventListener('click', () => {
    const suggested = suggestionBtn.textContent;
    searchInput.value = suggested;
    runSearch(suggested);
});

showHero();

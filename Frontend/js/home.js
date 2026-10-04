/* Kitabi — Home page logic.
   Depends on js/api.js (provides API_BASE and the API helper). */

(function () {
    "use strict";

    const FEATURED_COUNT = 8;      // books shown before "View all"
    const SEARCH_DEBOUNCE_MS = 250;

    // ---------- DOM ----------
    const $ = (id) => document.getElementById(id);
    const grid = $("bookGrid");
    const statusBox = $("status");
    const chipsBox = $("chips");
    const chipsNext = $("chipsNext");
    const searchForm = $("searchForm");
    const searchInput = $("searchInput");
    const viewAllBtn = $("viewAllBtn");
    const booksTitle = $("booksTitle");
    const sellLink = $("sellLink");
    const loginLink = $("loginLink");
    const logoutBtn = $("logoutBtn");
    const profileLink = $("profileLink");

    // ---------- State ----------
    const state = {
        books: [],
        category: "All",
        query: "",
        showAll: false,
    };

    // ---------- Helpers ----------
    function el(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    function formatPrice(value) {
        const n = Number(value) || 0;
        return "$" + (Number.isInteger(n) ? n : n.toFixed(2));
    }

    function imageUrl(path) {
        if (!path) return "";
        return /^https?:\/\//i.test(path) ? path : API_BASE + path;
    }

    function hash(str) {
        let h = 0;
        for (let i = 0; i < str.length; i++) h = (h * 31 + str.charCodeAt(i)) | 0;
        return Math.abs(h);
    }

    function badgeClass(condition) {
        const c = (condition || "").toLowerCase();
        if (c === "good") return "badge good";
        if (c === "fair") return "badge fair";
        return "badge"; // Like New, Very Good, Excellent...
    }

    function getUser() {
        try {
            const u = JSON.parse(localStorage.getItem("user") || "null");
            return u && Object.keys(u).length ? u : null;
        } catch {
            return null;
        }
    }

    // Turns any API error into a friendly { title, message } (same status codes the login page handles)
    function describeError(err) {
        if (!err.status) {
            return { title: "Can't reach the server", message: "Check that the Kitabi backend is running, then try again." };
        }
        if (err.status === 429) {
            return { title: "Too many requests", message: "Please wait a minute and try again." };
        }
        if (err.status === 404) {
            return { title: "Not found", message: err.message || "We couldn't find what you asked for." };
        }
        if (err.status >= 500) {
            return { title: "Server error", message: "Something went wrong on our side. Please try again shortly." };
        }
        return { title: "Something went wrong", message: err.message || "We couldn't complete that request." };
    }

    // Small message bar under the header for non-blocking problems (e.g. logout failed)
    let toastTimer;
    function showToast(message) {
        let toast = document.getElementById("toast");
        if (!toast) {
            toast = el("div", "toast");
            toast.id = "toast";
            toast.setAttribute("role", "alert");
            document.body.appendChild(toast);
        }
        toast.textContent = message;
        toast.classList.add("show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => toast.classList.remove("show"), 4000);
    }

    // ---------- Header / auth state ----------
    function renderAuth() {
        const user = getUser();
        loginLink.hidden = !!user;
        logoutBtn.hidden = !user;

        if (user) {
            profileLink.title = user.name || "Account";
            profileLink.setAttribute("aria-label", "Account: " + (user.name || ""));
        }
    }

    async function logout() {
        logoutBtn.disabled = true;
        try {
            // The backend reads the refresh token from the cookie; the body is only a fallback.
            await API.post("/api/auth/logout", { refreshToken: "" });
        } catch (err) {
            // Still sign the user out locally, but tell them if the server couldn't confirm it.
            console.error("Logout failed:", err);
            showToast(describeError(err).message);
        } finally {
            localStorage.removeItem("user");
            logoutBtn.disabled = false;
            renderAuth();
        }
    }

    // ---------- Rendering ----------
    function showStatus(type, title, message, actionLabel, action) {
        grid.replaceChildren();
        statusBox.hidden = false;
        statusBox.className = "status" + (type === "error" ? " error" : "");
        statusBox.replaceChildren(el("strong", "", title));
        if (message) statusBox.appendChild(el("span", "", message));

        if (actionLabel) {
            const btn = el("button", "btn btn-outline", actionLabel);
            btn.type = "button";
            btn.addEventListener("click", action);
            statusBox.appendChild(btn);
        }
    }

    function hideStatus() {
        statusBox.hidden = true;
        statusBox.replaceChildren();
    }

    function renderSkeletons(count = FEATURED_COUNT) {
        hideStatus();
        grid.replaceChildren();
        for (let i = 0; i < count; i++) {
            const card = el("div", "book-card skeleton");
            card.append(el("div", "cover"), el("div", "line"), el("div", "line w70"), el("div", "line w40"));
            grid.appendChild(card);
        }
    }

    function buildCover(book) {
        const cover = el("div", "cover");

        function fallback() {
            cover.replaceChildren();
            const f = el("div", "cover-fallback c" + (hash(book.title || "") % 4));
            f.append(el("b", "", book.title), el("small", "", book.author || ""));
            cover.appendChild(f);
        }

        const first = Array.isArray(book.imageUrls) ? book.imageUrls[0] : null;
        if (first) {
            const img = el("img");
            img.src = imageUrl(first);
            img.alt = "Cover of " + book.title;
            img.loading = "lazy";
            img.addEventListener("error", fallback, { once: true });
            cover.appendChild(img);
        } else {
            fallback();
        }
        return cover;
    }

    function buildCard(book) {
        const card = el("a", "book-card");
        card.href = "book.html?id=" + encodeURIComponent(book.listingId);
        card.setAttribute("aria-label", book.title + ", " + formatPrice(book.price));

        const meta = [book.category, book.sellerUniversity].filter(Boolean).join(" • ");
        const author = book.author && book.author !== "Unknown" ? book.author : "";

        card.append(
            buildCover(book),
            el("h3", "book-title", book.title),
            el("p", "book-author", author),
            el("p", "book-meta", meta),
            el("span", badgeClass(book.condition), book.condition),
            el("p", "book-price", formatPrice(book.price))
        );
        return card;
    }

    function getFiltered() {
        const q = state.query.trim().toLowerCase();
        const cat = state.category.toLowerCase();

        return state.books.filter((b) => {
            if (cat !== "all" && (b.category || "").toLowerCase() !== cat) return false;
            if (!q) return true;
            return [b.title, b.author, b.isbn].some((v) => (v || "").toLowerCase().includes(q));
        });
    }

    function renderBooks() {
        const filtered = getFiltered();
        const isFiltering = state.category !== "All" || state.query.trim() !== "";

        // Heading
        if (state.query.trim()) {
            booksTitle.textContent = `${filtered.length} result${filtered.length === 1 ? "" : "s"} for “${state.query.trim()}”`;
        } else if (state.category !== "All") {
            booksTitle.textContent = state.category;
        } else {
            booksTitle.textContent = "Featured Books";
        }

        // "View all" only matters for the unfiltered home view
        const canToggle = !isFiltering && filtered.length > FEATURED_COUNT;
        viewAllBtn.hidden = !canToggle;
        viewAllBtn.firstChild.textContent = state.showAll ? "Show less " : "View all ";

        if (!filtered.length) {
            if (isFiltering) {
                showStatus("empty", "No books found", "Try a different title, author, ISBN or category.", "Clear filters", clearFilters);
            } else {
                showStatus("empty", "No books listed yet", "Be the first to sell a book on Kitabi.", "Sell a Book", () => sellLink.click());
            }
            return;
        }

        hideStatus();
        const visible = isFiltering || state.showAll ? filtered : filtered.slice(0, FEATURED_COUNT);
        grid.replaceChildren(...visible.map(buildCard));
    }

    function renderChips() {
        const counts = new Map();
        state.books.forEach((b) => {
            if (b.category) counts.set(b.category, (counts.get(b.category) || 0) + 1);
        });

        const categories = ["All", ...[...counts.keys()].sort((a, b) => a.localeCompare(b))];

        chipsBox.replaceChildren(
            ...categories.map((name) => {
                const chip = el("button", "chip" + (name === state.category ? " active" : ""), name);
                chip.type = "button";
                chip.dataset.category = name;
                chip.setAttribute("aria-pressed", String(name === state.category));
                return chip;
            })
        );
    }

    function updateChipsActive() {
        chipsBox.querySelectorAll(".chip").forEach((chip) => {
            const active = chip.dataset.category === state.category;
            chip.classList.toggle("active", active);
            chip.setAttribute("aria-pressed", String(active));
        });
    }

    function clearFilters() {
        state.category = "All";
        state.query = "";
        state.showAll = false;
        searchInput.value = "";
        updateChipsActive();
        renderBooks();
    }

    // ---------- Data ----------
    async function loadBooks() {
        renderSkeletons();
        try {
            const data = await API.get("/api/books");

            if (!Array.isArray(data)) {
                throw new Error("The server sent an unexpected response.");
            }

            // Newest listings first (higher id = newer)
            state.books = data.slice().sort((a, b) => b.listingId - a.listingId);

            renderChips();
            renderBooks();
        } catch (err) {
            console.error("Failed to load books:", err);
            state.books = [];
            renderChips();
            const { title, message } = describeError(err);
            showStatus("error", title, message, "Try again", loadBooks);
        }
    }

    // ---------- Events ----------
    chipsBox.addEventListener("click", (e) => {
        const chip = e.target.closest(".chip");
        if (!chip) return;
        state.category = chip.dataset.category;
        state.showAll = false;
        updateChipsActive();
        renderBooks();
    });

    chipsNext.addEventListener("click", () => {
        chipsBox.scrollBy({ left: 220, behavior: "smooth" });
    });

    let debounceTimer;
    searchInput.addEventListener("input", () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            state.query = searchInput.value;
            renderBooks();
        }, SEARCH_DEBOUNCE_MS);
    });

    searchForm.addEventListener("submit", (e) => {
        e.preventDefault();
        clearTimeout(debounceTimer);
        state.query = searchInput.value;
        renderBooks();
        document.getElementById("books").scrollIntoView({ behavior: "smooth" });
    });

    viewAllBtn.addEventListener("click", () => {
        state.showAll = !state.showAll;
        renderBooks();
    });

    // Selling needs an account — send guests to login first.
    sellLink.addEventListener("click", (e) => {
        if (!getUser()) {
            e.preventDefault();
            window.location.href = "login.html";
        }
    });

    logoutBtn.addEventListener("click", logout);

    // ---------- Init ----------
    $("year").textContent = new Date().getFullYear();
    renderAuth();
    loadBooks();
})();
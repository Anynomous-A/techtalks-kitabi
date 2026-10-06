/* Kitabi — listing details page.
   Depends on js/api.js and its API / API_BASE helpers. */
(function () {
    "use strict";

    const $ = (id) => document.getElementById(id);
    const detail = $("bookDetail");
    const pageStatus = $("pageStatus");
    const mainImage = $("mainImage");
    const imageThumbs = $("imageThumbs");
    const contactBtn = $("contactBtn");
    const contactHint = $("contactHint");
    const loginLink = $("loginLink");
    const logoutBtn = $("logoutBtn");
    const profileLink = $("profileLink");
    const sellLink = $("sellLink");

    let currentBook = null;
    let toastTimer = null;

    function getUser() {
        try {
            const user = JSON.parse(localStorage.getItem("user") || "null");
            return user && Object.keys(user).length ? user : null;
        } catch {
            return null;
        }
    }

    function formatPrice(value) {
        const amount = Number(value);
        if (!Number.isFinite(amount)) return "Price not available";
        return "$" + (Number.isInteger(amount) ? amount : amount.toFixed(2));
    }

    function imageUrl(path) {
        if (!path) return "";
        return /^https?:\/\//i.test(path) ? path : API_BASE + path;
    }

    function showToast(message) {
        const toast = $("toast");
        toast.textContent = message;
        toast.classList.add("show");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => toast.classList.remove("show"), 4000);
    }

    function showStatus(type, title, message, actionLabel, action) {
        detail.hidden = true;
        pageStatus.hidden = false;
        pageStatus.className = "page-status" + (type === "error" ? " error" : "");
        pageStatus.replaceChildren();

        if (type === "loading") {
            const spinner = document.createElement("span");
            spinner.className = "loading-spinner";
            spinner.setAttribute("aria-hidden", "true");
            pageStatus.appendChild(spinner);
        }

        const heading = document.createElement("strong");
        heading.textContent = title;
        pageStatus.appendChild(heading);

        if (message) {
            const paragraph = document.createElement("p");
            paragraph.textContent = message;
            pageStatus.appendChild(paragraph);
        }

        if (actionLabel && action) {
            const button = document.createElement("a");
            button.className = "btn btn-outline";
            button.href = "index.html#books";
            button.textContent = actionLabel;
            pageStatus.appendChild(button);
        }
    }

    function initials(name) {
        return (name || "Kitabi seller")
            .trim()
            .split(/\s+/)
            .slice(0, 2)
            .map((part) => part[0] || "")
            .join("")
            .toUpperCase();
    }

    function fallbackCover() {
        const cover = document.createElement("div");
        const hash = Array.from(currentBook?.title || "").reduce((sum, char) => sum + char.charCodeAt(0), 0);
        cover.className = "cover-fallback tone-" + (hash % 4);

        const title = document.createElement("b");
        title.textContent = currentBook?.title || "Book cover";
        cover.appendChild(title);

        if (currentBook?.author && currentBook.author !== "Unknown") {
            const author = document.createElement("small");
            author.textContent = currentBook.author;
            cover.appendChild(author);
        }
        return cover;
    }

    function showMainImage(path, alt) {
        mainImage.replaceChildren();
        if (!path) {
            mainImage.appendChild(fallbackCover());
            return;
        }

        const img = document.createElement("img");
        img.src = imageUrl(path);
        img.alt = alt;
        img.addEventListener("error", () => {
            if (img.parentNode === mainImage) mainImage.replaceChildren(fallbackCover());
        }, { once: true });
        mainImage.appendChild(img);
    }

    function renderGallery(book) {
        const images = Array.isArray(book.imageUrls) ? book.imageUrls.filter(Boolean) : [];
        imageThumbs.replaceChildren();
        imageThumbs.hidden = images.length < 2;

        showMainImage(images[0], "Cover of " + book.title);
        if (images.length < 2) return;

        images.forEach((path, index) => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "thumb" + (index === 0 ? " active" : "");
            button.setAttribute("aria-label", `Show photo ${index + 1} of ${images.length}`);
            button.setAttribute("aria-pressed", String(index === 0));

            const img = document.createElement("img");
            img.src = imageUrl(path);
            img.alt = "";
            img.loading = "lazy";
            img.addEventListener("error", () => button.remove(), { once: true });
            button.appendChild(img);

            button.addEventListener("click", () => {
                imageThumbs.querySelectorAll(".thumb").forEach((thumb) => {
                    thumb.classList.remove("active");
                    thumb.setAttribute("aria-pressed", "false");
                });
                button.classList.add("active");
                button.setAttribute("aria-pressed", "true");
                showMainImage(path, `Photo ${index + 1} of ${book.title}`);
            });
            imageThumbs.appendChild(button);
        });
    }

    function renderAuth() {
        const user = getUser();
        loginLink.hidden = !!user;
        logoutBtn.hidden = !user;
        if (user) {
            profileLink.title = user.name || "Account";
            profileLink.setAttribute("aria-label", "Account: " + (user.name || ""));
        }
    }

    function setConditionBadge(condition) {
        const badge = $("bookCondition");
        const value = condition || "Condition not specified";
        badge.textContent = value;
        badge.className = "badge";
        const normalized = value.toLowerCase();
        if (normalized === "good" || normalized === "new") badge.classList.add("condition-good");
        if (normalized === "fair" || normalized === "poor") badge.classList.add("condition-" + normalized);
    }

    function renderBook(book) {
        currentBook = book;
        const title = book.title || "Book details";
        const category = book.category || "Uncategorized";
        const author = book.author && book.author !== "Unknown" ? book.author : "Author not listed";
        const city = book.city || "Location not listed";
        const seller = book.sellerName || "Kitabi seller";
        const status = book.status || "Available";

        document.title = title + " — Kitabi";
        $("crumbTitle").textContent = title;
        $("bookCategory").textContent = category;
        $("bookTitle").textContent = title;
        $("bookAuthor").textContent = "by " + author;
        $("bookPrice").textContent = formatPrice(book.price);
        $("bookCity").textContent = city;
        $("factCategory").textContent = category;
        $("bookDescription").textContent = book.description || "The seller has not added a description.";
        $("sellerName").textContent = seller;
        $("sellerUniversity").textContent = book.sellerUniversity || "";
        $("sellerAvatar").textContent = initials(seller);

        setConditionBadge(book.condition);

        const statusBadge = $("bookStatus");
        const normalizedStatus = status.trim().toLowerCase();
        const available = normalizedStatus === "available" || normalizedStatus === "active";
        statusBadge.textContent = available ? "Available" : status;
        statusBadge.className = "badge status-badge" + (available ? "" : " unavailable");

        const isbn = book.isbn && book.isbn.toLowerCase() !== "unknown" ? book.isbn : "";
        $("bookIsbn").textContent = isbn || "Not provided";
        $("isbnFact").hidden = !isbn;

        contactBtn.disabled = !available;
        contactBtn.textContent = available
            ? (getUser() ? "Message seller on WhatsApp" : "Log in to contact seller")
            : "Listing unavailable";
        contactHint.textContent = available
            ? "Arrange payment and pickup directly with the seller."
            : "This listing is no longer available.";

        renderGallery(book);
        pageStatus.hidden = true;
        detail.hidden = false;
    }

    function errorMessage(error) {
        if (error.status === 404) return "This listing may have been removed or the link may be incorrect.";
        if (error.status === 429) return "Please wait a moment, then try loading the listing again.";
        if (!error.status) return "Check that the Kitabi backend is running, then try again.";
        return error.message || "We couldn't load this listing right now.";
    }

    async function loadBook(id) {
        showStatus("loading", "Loading book details…");
        try {
            const book = await API.get("/api/books/" + encodeURIComponent(id));
            if (!book || typeof book !== "object" || !book.listingId) {
                throw new Error("The server returned an unexpected listing.");
            }
            renderBook(book);
        } catch (error) {
            console.error("Failed to load listing:", error);
            showStatus("error", error.status === 404 ? "Listing not found" : "Couldn’t load this book", errorMessage(error), "Browse books", true);
        }
    }

    async function contactSeller() {
        if (!currentBook || !contactBtn || contactBtn.disabled) return;

        if (!getUser()) {
            window.location.href = "login.html";
            return;
        }

        const originalLabel = contactBtn.textContent;
        contactBtn.disabled = true;
        contactBtn.textContent = "Opening WhatsApp…";

        try {
            const message = `Hello, I'm interested in "${currentBook.title}". Is it still available?`;
            const result = await API.get(
                `/api/books/${encodeURIComponent(currentBook.listingId)}/contact/whatsapp/link?message=${encodeURIComponent(message)}`
            );
            if (!result?.whatsappUrl) throw new Error("The seller contact link was not available.");
            window.location.assign(result.whatsappUrl);
        } catch (error) {
            console.error("Could not open seller contact:", error);
            showToast(error.message || "Couldn't open WhatsApp. Please try again.");
            contactBtn.disabled = false;
            contactBtn.textContent = originalLabel;
        }
    }

    async function logout() {
        logoutBtn.disabled = true;
        try {
            await API.post("/api/auth/logout", { refreshToken: "" });
        } catch (error) {
            console.error("Logout failed:", error);
            showToast("You were signed out here, but the server couldn't confirm it.");
        } finally {
            localStorage.removeItem("user");
            logoutBtn.disabled = false;
            renderAuth();
            if (currentBook) {
                contactBtn.textContent = "Log in to contact seller";
                contactHint.textContent = "Log in to message the seller.";
            }
        }
    }

    sellLink.addEventListener("click", (event) => {
        if (!getUser()) {
            event.preventDefault();
            window.location.href = "login.html";
        }
    });
    logoutBtn.addEventListener("click", logout);
    contactBtn.addEventListener("click", contactSeller);
    $("year").textContent = new Date().getFullYear();
    renderAuth();

    const listingId = new URLSearchParams(window.location.search).get("id");
    if (!listingId || !/^\d+$/.test(listingId)) {
        showStatus("error", "Listing not found", "Open this page from a book card on the Kitabi homepage.", "Browse books", true);
    } else {
        loadBook(listingId);
    }
})();

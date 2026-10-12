/* Kitabi — Profile page logic.
   Depends on js/api.js (provides API_BASE and the API helper).

   Backend routes used:
     GET    /api/users/me                       -> my profile
     PUT    /api/users/me                       -> edit email / phone / university
     GET    /api/users/me/listings              -> my listings
     PUT    /api/books/{id}                     -> edit a listing (multipart form)
     DELETE /api/books/{id}                     -> delete a listing
     POST   /api/books/{id}/images              -> add photos
     DELETE /api/books/{id}/images/{imageId}    -> remove a photo
     POST   /api/auth/logout                    -> log out
*/

(function () {
    "use strict";

    // ---------- Constants (mirror the backend validation) ----------
    const CONDITIONS = ["New", "Good", "Fair", "Poor"];
    const MAX_FILES = 10;
    const MAX_FILE_BYTES = 5 * 1024 * 1024;
    const ALLOWED_TYPES = ["image/jpeg", "image/png", "image/gif"];
    const PHONE_RE = /^(70|71|76|78|79|81|03|30|31|36|38|39)\d{6}$/;
    const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const ISBN_RE = /^\d{10}(\d{3})?$/;

    // ---------- DOM ----------
    const $ = (id) => document.getElementById(id);

    const logoutBtn = $("logoutBtn");

    const avatar = $("avatar");
    const pName = $("pName");
    const pUniversityTag = $("pUniversityTag");
    const pEmail = $("pEmail");
    const pPhone = $("pPhone");
    const pUniversity = $("pUniversity");
    const statTotal = $("statTotal");
    const statAvailable = $("statAvailable");
    const statSold = $("statSold");

    const profileView = $("profileView");
    const profileForm = $("profileForm");
    const profileMsg = $("profileMsg");
    const editProfileBtn = $("editProfileBtn");
    const cancelProfileBtn = $("cancelProfileBtn");
    const saveProfileBtn = $("saveProfileBtn");

    const filterChips = $("filterChips");
    const listingGrid = $("listingGrid");
    const statusBox = $("status");

    const editDialog = $("editDialog");
    const editForm = $("editForm");
    const editMsg = $("editMsg");
    const editSave = $("editSave");
    const photoGrid = $("photoGrid");
    const imagesInput = $("eImages");
    const dropZone = document.querySelector("#editDialog .drop");

    const deleteDialog = $("deleteDialog");
    const deleteName = $("deleteName");
    const deleteConfirm = $("deleteConfirm");

    // ---------- State ----------
    const state = {
        profile: null,
        listings: [],
        filter: "all",
        editing: null,        // the listing open in the edit dialog
        keptImages: [],       // existing photos that stay
        removedImageIds: [],  // existing photos to delete on save
        newFiles: [],         // File objects to upload on save
        deleting: null,       // the listing in the delete dialog
    };
    let previewUrls = [];

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
        if (c === "poor") return "badge poor";
        return "badge"; // New, Like New, Excellent...
    }

    const isSold = (l) => (l.status || "").toLowerCase() === "sold";
    const cleanAuthor = (a) => (a && a !== "Unknown" ? a : "");
    const cleanIsbn = (i) => (i && i !== "Unknown" ? i : "");

    function getUser() {
        try {
            const u = JSON.parse(localStorage.getItem("user") || "null");
            return u && Object.keys(u).length ? u : null;
        } catch {
            return null;
        }
    }

    function saveUser(patch) {
        try {
            localStorage.setItem("user", JSON.stringify({ ...(getUser() || {}), ...patch }));
        } catch { /* storage unavailable — not critical */ }
    }

    function describeError(err) {
        if (!err.status) {
            return { title: "Can't reach the server", message: "Check that the Kitabi backend is running, then try again." };
        }
        if (err.status === 429) return { title: "Too many requests", message: "Please wait a minute and try again." };
        if (err.status === 404) return { title: "Not found", message: err.message || "We couldn't find what you asked for." };
        if (err.status >= 500) return { title: "Server error", message: "Something went wrong on our side. Please try again shortly." };
        return { title: "Something went wrong", message: err.message || "We couldn't complete that request." };
    }

    let toastTimer;
    function showToast(message, type = "error") {
        let toast = $("toast");
        if (!toast) {
            toast = el("div", "toast");
            toast.id = "toast";
            toast.setAttribute("role", "alert");
            document.body.appendChild(toast);
        }
        toast.textContent = message;
        toast.className = "toast show" + (type === "success" ? " success" : "");
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => toast.classList.remove("show"), 4000);
    }

    // Form helpers (scoped to a container so profile + listing forms don't clash)
    function setAlert(box, message, type = "error") {
        box.textContent = message;
        box.className = `alert ${type}`;
    }
    function clearAlert(box) {
        box.textContent = "";
        box.className = "alert";
    }
    function setFieldError(root, field, message) {
        const errorEl = root.querySelector(`[data-error-for="${field}"]`);
        if (!errorEl) return false;
        errorEl.textContent = message || "";
        const wrap = errorEl.closest(".field");
        if (wrap) wrap.classList.toggle("invalid", !!message);
        return true;
    }
    function clearFieldErrors(root) {
        root.querySelectorAll(".field-error").forEach((e) => (e.textContent = ""));
        root.querySelectorAll(".field").forEach((f) => f.classList.remove("invalid"));
    }
    function resolveFieldName(root, serverKey) {
        const last = String(serverKey).split(".").pop().toLowerCase();
        const names = [...root.querySelectorAll("[data-error-for]")].map((e) => e.getAttribute("data-error-for"));
        return names.find((n) => n.toLowerCase() === last) || null;
    }
    // Shows a 400 response (validation dictionary or single error) on a form
    function showServerErrors(root, box, err, fallback) {
        if (err.status === 400 && err.body?.errors) {
            const unmatched = [];
            for (const [key, messages] of Object.entries(err.body.errors)) {
                const msg = Array.isArray(messages) ? messages.join(" ") : String(messages);
                const field = resolveFieldName(root, key);
                if (field) setFieldError(root, field, msg);
                else unmatched.push(msg);
            }
            setAlert(box, unmatched.length ? unmatched.join(" ") : "Please fix the errors below.");
        } else if (err.status === 429) {
            setAlert(box, "Too many attempts. Try again in a minute.");
        } else if (!err.status) {
            setAlert(box, "Can't reach the server. Is the API running on localhost:5042?");
        } else {
            setAlert(box, err.body?.error || err.message || fallback);
        }
    }

    // ---------- Header / auth ----------
    async function logout() {
        logoutBtn.disabled = true;
        try {
            await API.post("/api/auth/logout", { refreshToken: "" });
        } catch (err) {
            console.error("Logout failed:", err);
        } finally {
            localStorage.removeItem("user");
            window.location.href = "index.html";
        }
    }

    // ---------- Profile ----------
    function initials(name) {
        const parts = String(name || "").trim().split(/\s+/).filter(Boolean);
        if (!parts.length) return "?";
        return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : "")).toUpperCase();
    }

    function renderProfile() {
        const p = state.profile || {};
        avatar.textContent = initials(p.name);
        pName.textContent = p.name || "Your account";
        pUniversityTag.textContent = p.university || "";
        pEmail.textContent = p.email || "–";
        pPhone.textContent = p.phone || "–";
        pUniversity.textContent = p.university || "–";
    }

    function renderStats() {
        const sold = state.listings.filter(isSold).length;
        const total = state.listings.length;
        statTotal.textContent = total;
        statSold.textContent = sold;
        statAvailable.textContent = total - sold;
        $("countAll").textContent = total;
        $("countAvailable").textContent = total - sold;
        $("countSold").textContent = sold;
    }

    function setProfileEditing(on) {
        profileView.hidden = on;
        profileForm.hidden = !on;
        clearAlert(profileMsg);
        clearFieldErrors(profileForm);
        if (on) {
            const p = state.profile || {};
            $("fName").value = p.name || "";
            $("fEmail").value = p.email || "";
            $("fPhone").value = p.phone || "";
            $("fUniversity").value = p.university || "";
            $("fEmail").focus();
        } else {
            editProfileBtn.focus();
        }
    }

    function validateProfile(data) {
        clearFieldErrors(profileForm);
        let ok = true;
        if (!data.email || !EMAIL_RE.test(data.email) || data.email.length > 100) {
            setFieldError(profileForm, "Email", "Enter a valid email address.");
            ok = false;
        }
        if (!PHONE_RE.test(data.phone)) {
            setFieldError(profileForm, "Phone", "Enter a valid Lebanese number (e.g. 70123456, 81234567).");
            ok = false;
        }
        if (!data.university) {
            setFieldError(profileForm, "University", "University is required.");
            ok = false;
        } else if (data.university.length > 100) {
            setFieldError(profileForm, "University", "University name must not exceed 100 characters.");
            ok = false;
        }
        return ok;
    }

    async function saveProfile(e) {
        e.preventDefault();
        clearAlert(profileMsg);

        const data = {
            email: $("fEmail").value.trim(),
            phone: $("fPhone").value.trim(),
            university: $("fUniversity").value.trim(),
        };
        if (!validateProfile(data)) {
            setAlert(profileMsg, "Please fix the errors below.");
            return;
        }

        saveProfileBtn.disabled = true;
        saveProfileBtn.textContent = "Saving...";
        try {
            const updated = await API.put("/api/users/me", data);
            state.profile = updated;
            saveUser(updated);
            renderProfile();
            setProfileEditing(false);
            showToast("Profile updated.", "success");
        } catch (err) {
            console.error("Update profile failed:", err);
            // The backend reports "Email already registered" as { error }
            if (err.status === 400 && err.body?.error && /email/i.test(err.body.error)) {
                setFieldError(profileForm, "Email", err.body.error);
                setAlert(profileMsg, "Please fix the errors below.");
            } else {
                showServerErrors(profileForm, profileMsg, err, "Could not update your profile.");
            }
        } finally {
            saveProfileBtn.disabled = false;
            saveProfileBtn.textContent = "Save changes";
        }
    }

    async function loadProfile() {
        // Show what we already know straight away, then refresh from the server.
        state.profile = getUser();
        renderProfile();
        try {
            state.profile = await API.get("/api/users/me");
            saveUser(state.profile);
            renderProfile();
        } catch (err) {
            console.error("Failed to load profile:", err);
            if (!state.profile) showToast(describeError(err).message);
        }
    }

    // ---------- Listings ----------
    function showStatus(type, title, message, actionLabel, action, href) {
        listingGrid.replaceChildren();
        statusBox.hidden = false;
        statusBox.className = "status" + (type === "error" ? " error" : "");
        statusBox.replaceChildren(el("strong", "", title));
        if (message) statusBox.appendChild(el("span", "", message));

        if (actionLabel) {
            const node = href ? el("a", "btn btn-primary", actionLabel) : el("button", "btn btn-outline", actionLabel);
            if (href) node.href = href;
            else {
                node.type = "button";
                node.addEventListener("click", action);
            }
            statusBox.appendChild(node);
        }
    }

    function hideStatus() {
        statusBox.hidden = true;
        statusBox.replaceChildren();
    }

    function renderSkeletons(count = 3) {
        hideStatus();
        listingGrid.replaceChildren();
        for (let i = 0; i < count; i++) {
            const card = el("div", "book-card skeleton");
            card.append(el("div", "cover"), el("div", "line"), el("div", "line w70"), el("div", "line w40"));
            listingGrid.appendChild(card);
        }
    }

    function buildCover(listing) {
        const cover = el("div", "cover");

        function fallback() {
            cover.querySelectorAll("img, .cover-fallback").forEach((n) => n.remove());
            const f = el("div", "cover-fallback c" + (hash(listing.title || "") % 4));
            f.append(el("b", "", listing.title), el("small", "", cleanAuthor(listing.author)));
            cover.appendChild(f);
        }

        const first = listing.images && listing.images[0];
        if (first) {
            const img = el("img");
            img.src = imageUrl(first.imageUrl);
            img.alt = "Cover of " + listing.title;
            img.loading = "lazy";
            img.addEventListener("error", fallback, { once: true });
            cover.appendChild(img);
        } else {
            fallback();
        }

        if (isSold(listing)) cover.prepend(el("span", "status-tag", "Sold"));
        return cover;
    }

    function iconButton(className, label, svgPath, text) {
        const btn = el("button", "act " + className);
        btn.type = "button";
        btn.setAttribute("aria-label", label);
        btn.title = label;
        btn.innerHTML =
            `<svg viewBox="0 0 24 24" width="15" height="15" aria-hidden="true"><path d="${svgPath}" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"/></svg>`;
        if (text) btn.appendChild(el("span", "", text));
        return btn;
    }

    function buildCard(listing) {
        const sold = isSold(listing);
        const card = el("article", "listing-card" + (sold ? " is-sold" : ""));
        card.dataset.id = listing.listingId;

        const title = el("h3", "book-title");
        const link = el("a", "", listing.title);
        link.href = "book.html?id=" + encodeURIComponent(listing.listingId);
        title.appendChild(link);

        const meta = [listing.category, listing.city].filter(Boolean).join(" • ");

        const row = el("div", "card-row");
        row.append(el("span", badgeClass(listing.condition), listing.condition), el("p", "book-price", formatPrice(listing.price)));

        const actions = el("div", "card-actions");
        const editBtn = iconButton("edit", "Edit " + listing.title, "M4 20h4L19 9l-4-4L4 16v4zM13.5 6.5l4 4", "Edit");
        const soldBtn = iconButton(
            "toggle",
            sold ? "Mark as available" : "Mark as sold",
            sold ? "M3 12a9 9 0 1 0 3-6.7M3 4v5h5" : "M5 12.5l4.5 4.5L19 7.5",
            sold ? "Relist" : "Sold"
        );
        const delBtn = iconButton("danger", "Delete " + listing.title, "M4 7h16M10 11v6M14 11v6M6 7l1 12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1l1-12M9 7V4h6v3");

        editBtn.addEventListener("click", () => openEdit(listing));
        soldBtn.addEventListener("click", () => toggleSold(listing, soldBtn));
        delBtn.addEventListener("click", () => openDelete(listing));
        actions.append(editBtn, soldBtn, delBtn);

        card.append(
            buildCover(listing),
            title,
            el("p", "book-author", cleanAuthor(listing.author)),
            el("p", "book-meta", meta),
            row,
            actions
        );
        return card;
    }

    function getFiltered() {
        if (state.filter === "sold") return state.listings.filter(isSold);
        if (state.filter === "available") return state.listings.filter((l) => !isSold(l));
        return state.listings;
    }

    function renderListings() {
        renderStats();

        filterChips.querySelectorAll(".chip").forEach((chip) => {
            const active = chip.dataset.filter === state.filter;
            chip.classList.toggle("active", active);
            chip.setAttribute("aria-pressed", String(active));
        });

        const visible = getFiltered();
        if (!visible.length) {
            if (!state.listings.length) {
                showStatus("empty", "You haven't listed any books yet", "Post your first book and start earning from your old textbooks.", "Sell a Book", null, "post-listing.html");
            } else {
                showStatus("empty", state.filter === "sold" ? "No sold books yet" : "No available books", "Switch filters to see your other listings.", "Show all", () => {
                    state.filter = "all";
                    renderListings();
                });
            }
            return;
        }

        hideStatus();
        listingGrid.replaceChildren(...visible.map(buildCard));
    }

    async function loadListings() {
        renderSkeletons();
        try {
            const data = await API.get("/api/users/me/listings");
            if (!Array.isArray(data)) throw new Error("The server sent an unexpected response.");
            state.listings = data;
            renderListings();
        } catch (err) {
            console.error("Failed to load listings:", err);
            state.listings = [];
            renderStats();
            const { title, message } = describeError(err);
            showStatus("error", title, message, "Try again", loadListings);
        }
    }

    // Builds the multipart body the PUT /api/books/{id} endpoint expects
    function listingFormData(listing, overrides = {}) {
        const v = {
            Title: listing.title,
            Author: cleanAuthor(listing.author),
            Category: listing.category,
            Isbn: cleanIsbn(listing.isbn),
            Description: listing.description,
            Price: listing.price,
            Condition: listing.condition,
            City: listing.city,
            Status: isSold(listing) ? "Sold" : "Available",
            ...overrides,
        };
        const fd = new FormData();
        for (const [k, val] of Object.entries(v)) fd.append(k, val ?? "");
        return fd;
    }

    async function toggleSold(listing, button) {
        const nextStatus = isSold(listing) ? "Available" : "Sold";
        button.disabled = true;
        try {
            await API.put(`/api/books/${listing.listingId}`, listingFormData(listing, { Status: nextStatus }));
            listing.status = nextStatus;
            renderListings();
            showToast(nextStatus === "Sold" ? "Marked as sold." : "Listing is available again.", "success");
        } catch (err) {
            console.error("Status change failed:", err);
            button.disabled = false;
            if (err.status === 400) {
                // Old data (e.g. an unsupported condition) fails validation — let the user fix it.
                showToast("Please review this listing's details first.");
                openEdit(listing, nextStatus);
            } else {
                showToast(describeError(err).message);
            }
        }
    }

    // ---------- Delete ----------
    function openDelete(listing) {
        state.deleting = listing;
        deleteName.textContent = listing.title;
        deleteDialog.showModal();
    }

    async function confirmDelete() {
        const listing = state.deleting;
        if (!listing) return;
        deleteConfirm.disabled = true;
        deleteConfirm.textContent = "Deleting...";
        try {
            await API.delete(`/api/books/${listing.listingId}`);
            state.listings = state.listings.filter((l) => l.listingId !== listing.listingId);
            deleteDialog.close();
            renderListings();
            showToast("Listing deleted.", "success");
        } catch (err) {
            console.error("Delete failed:", err);
            deleteDialog.close();
            showToast(describeError(err).message);
        } finally {
            deleteConfirm.disabled = false;
            deleteConfirm.textContent = "Delete";
            state.deleting = null;
        }
    }

    // ---------- Edit listing ----------
    function renderPhotos() {
        previewUrls.forEach((u) => URL.revokeObjectURL(u));
        previewUrls = [];
        photoGrid.replaceChildren();

        function removeBtn(label, onClick) {
            const b = el("button", "remove");
            b.type = "button";
            b.setAttribute("aria-label", label);
            b.innerHTML = '<svg viewBox="0 0 24 24" width="12" height="12" aria-hidden="true"><path d="M6 6l12 12M18 6 6 18" stroke="currentColor" stroke-width="2.4" stroke-linecap="round"/></svg>';
            b.addEventListener("click", onClick);
            return b;
        }

        state.keptImages.forEach((img) => {
            const tile = el("div", "photo");
            const im = el("img");
            im.src = imageUrl(img.imageUrl);
            im.alt = "Listing photo";
            tile.append(im, removeBtn("Remove photo", () => {
                state.removedImageIds.push(img.imageId);
                state.keptImages = state.keptImages.filter((k) => k.imageId !== img.imageId);
                setFieldError(editForm, "Images", "");
                renderPhotos();
            }));
            photoGrid.appendChild(tile);
        });

        state.newFiles.forEach((file, index) => {
            const url = URL.createObjectURL(file);
            previewUrls.push(url);
            const tile = el("div", "photo new");
            const im = el("img");
            im.src = url;
            im.alt = file.name;
            tile.append(im, el("span", "new-tag", "New"), removeBtn("Remove " + file.name, () => {
                state.newFiles.splice(index, 1);
                setFieldError(editForm, "Images", "");
                renderPhotos();
            }));
            photoGrid.appendChild(tile);
        });
    }

    function addFiles(fileList) {
        const errors = [];
        for (const file of fileList) {
            if (!ALLOWED_TYPES.includes(file.type)) {
                errors.push(`"${file.name}" is not a JPG, PNG, or GIF.`);
            } else if (file.size > MAX_FILE_BYTES) {
                errors.push(`"${file.name}" is larger than 5 MB.`);
            } else if (state.keptImages.length + state.newFiles.length >= MAX_FILES) {
                errors.push(`You can have at most ${MAX_FILES} photos per listing.`);
                break;
            } else {
                state.newFiles.push(file);
            }
        }
        setFieldError(editForm, "Images", errors.join(" "));
        renderPhotos();
    }

    function updateDescCount() {
        $("descCount").textContent = $("eDescription").value.length + "/1000";
    }

    function openEdit(listing, presetStatus) {
        state.editing = listing;
        state.keptImages = (listing.images || []).slice();
        state.removedImageIds = [];
        state.newFiles = [];

        clearFieldErrors(editForm);
        clearAlert(editMsg);

        $("eTitle").value = listing.title || "";
        $("eAuthor").value = cleanAuthor(listing.author);
        $("eCategory").value = listing.category || "";
        $("eIsbn").value = cleanIsbn(listing.isbn);
        $("ePrice").value = listing.price;
        $("eCity").value = listing.city || "";
        $("eDescription").value = listing.description || "";
        $("eStatus").value = presetStatus || (isSold(listing) ? "Sold" : "Available");
        imagesInput.value = "";

        // Older listings may use a condition the form no longer offers (e.g. "Like New")
        const cond = $("eCondition");
        cond.value = CONDITIONS.includes(listing.condition) ? listing.condition : "";

        updateDescCount();
        renderPhotos();
        editDialog.showModal();
        $("eTitle").focus();
    }

    function validateListing(fd) {
        clearFieldErrors(editForm);
        let ok = true;
        const val = (k) => (fd.get(k) || "").toString().trim();
        const need = (k, label) => {
            if (!val(k)) {
                setFieldError(editForm, k, `${label} is required.`);
                ok = false;
            }
        };

        need("Title", "Title");
        need("Category", "Category");
        need("Description", "Description");
        need("City", "City");
        need("Condition", "Condition");
        need("Price", "Price");

        if (val("Title") && (val("Title").length < 2 || val("Title").length > 100)) {
            setFieldError(editForm, "Title", "Title must be between 2 and 100 characters.");
            ok = false;
        }
        const price = parseFloat(val("Price"));
        if (val("Price") && (isNaN(price) || price <= 0)) {
            setFieldError(editForm, "Price", "Price must be greater than 0.");
            ok = false;
        }
        if (val("Isbn") && !ISBN_RE.test(val("Isbn"))) {
            setFieldError(editForm, "Isbn", "ISBN must be 10 or 13 digits.");
            ok = false;
        }
        if (val("Condition") && !CONDITIONS.includes(val("Condition"))) {
            setFieldError(editForm, "Condition", "Condition must be New, Good, Fair, or Poor.");
            ok = false;
        }
        return ok;
    }

    async function saveListing(e) {
        e.preventDefault();
        const listing = state.editing;
        if (!listing) return;
        clearAlert(editMsg);

        const fd = new FormData(editForm);
        if (!validateListing(fd)) {
            setAlert(editMsg, "Please fix the errors below.");
            return;
        }

        const body = new FormData();
        ["Title", "Author", "Category", "Isbn", "Description", "Price", "Condition", "City", "Status"].forEach((k) =>
            body.append(k, (fd.get(k) || "").toString().trim())
        );

        editSave.disabled = true;
        editSave.textContent = "Saving...";
        try {
            await API.put(`/api/books/${listing.listingId}`, body);

            // Photos: remove first so the "max 10" limit on the server is respected, then add.
            const photoErrors = [];
            for (const imageId of state.removedImageIds) {
                try {
                    await API.delete(`/api/books/${listing.listingId}/images/${imageId}`);
                } catch (err) {
                    photoErrors.push(err.message || "A photo could not be removed.");
                }
            }
            if (state.newFiles.length) {
                const upload = new FormData();
                state.newFiles.forEach((f) => upload.append("images", f, f.name));
                try {
                    await API.post(`/api/books/${listing.listingId}/images`, upload);
                } catch (err) {
                    photoErrors.push(err.status === 413 ? "The photos are too large." : err.message || "Photos could not be uploaded.");
                }
            }

            editDialog.close();
            await loadListings();
            if (photoErrors.length) showToast("Saved, but: " + photoErrors.join(" "));
            else showToast("Listing updated.", "success");
        } catch (err) {
            console.error("Update listing failed:", err, err.body);
            showServerErrors(editForm, editMsg, err, "Could not update the listing.");
        } finally {
            editSave.disabled = false;
            editSave.textContent = "Save changes";
        }
    }

    // ---------- Events ----------
    logoutBtn.addEventListener("click", logout);

    editProfileBtn.addEventListener("click", () => setProfileEditing(true));
    cancelProfileBtn.addEventListener("click", () => setProfileEditing(false));
    profileForm.addEventListener("submit", saveProfile);

    filterChips.addEventListener("click", (e) => {
        const chip = e.target.closest(".chip");
        if (!chip) return;
        state.filter = chip.dataset.filter;
        renderListings();
    });

    editForm.addEventListener("submit", saveListing);
    $("editClose").addEventListener("click", () => editDialog.close());
    $("editCancel").addEventListener("click", () => editDialog.close());
    $("eDescription").addEventListener("input", updateDescCount);

    imagesInput.addEventListener("change", () => {
        addFiles(imagesInput.files);
        imagesInput.value = "";
    });
    ["dragenter", "dragover"].forEach((ev) =>
        dropZone.addEventListener(ev, (e) => { e.preventDefault(); dropZone.classList.add("over"); })
    );
    ["dragleave", "drop"].forEach((ev) =>
        dropZone.addEventListener(ev, (e) => { e.preventDefault(); dropZone.classList.remove("over"); })
    );
    dropZone.addEventListener("drop", (e) => {
        if (e.dataTransfer && e.dataTransfer.files.length) addFiles(e.dataTransfer.files);
    });

    $("deleteCancel").addEventListener("click", () => deleteDialog.close());
    deleteConfirm.addEventListener("click", confirmDelete);

    // Click on the dimmed backdrop closes a dialog; lock page scroll while one is open.
    [editDialog, deleteDialog].forEach((dlg) => {
        dlg.addEventListener("click", (e) => { if (e.target === dlg) dlg.close(); });
        dlg.addEventListener("close", () => {
            document.body.classList.remove("modal-open");
            if (dlg === editDialog) {
                previewUrls.forEach((u) => URL.revokeObjectURL(u));
                previewUrls = [];
            }
        });
    });
    const lockScroll = () => document.body.classList.add("modal-open");
    const origShow = HTMLDialogElement.prototype.showModal;
    [editDialog, deleteDialog].forEach((dlg) => {
        dlg.showModal = function () { lockScroll(); origShow.call(this); };
    });

    // ---------- Init ----------
    $("year").textContent = new Date().getFullYear();

    // The profile is only for signed-in users.
    if (!getUser()) {
        window.location.replace("login.html");
    } else {
        loadProfile();
        loadListings();
    }
})();
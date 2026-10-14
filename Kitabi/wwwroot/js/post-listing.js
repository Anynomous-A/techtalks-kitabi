const form        = document.getElementById("postForm");
const btn         = document.getElementById("submitBtn");
const alertBox    = document.getElementById("formMsg");
const imagesInput = document.getElementById("Images");
const previewBox  = document.getElementById("imagePreview");

const MAX_FILES      = 10;
const MAX_FILE_BYTES = 5 * 1024 * 1024;  
const ALLOWED_TYPES  = ["image/jpeg", "image/png", "image/gif"];
const REDIRECT_URL   = "index.html";

let redirecting = false;


function showAlert(message, type = "error") {
    alertBox.textContent = message;
    alertBox.className = `alert ${type}`;
}

function clearAlert() {
    alertBox.textContent = "";
    alertBox.className = "alert";
}

function setFieldError(field, message) {
    const errorEl = document.querySelector(`[data-error-for="${field}"]`);
    if (!errorEl) return false;

    errorEl.textContent = message || "";

    const input = document.getElementById(field);
    if (input && input.closest(".field")) {
        input.closest(".field").classList.toggle("invalid", !!message);
    }
    return true;
}

function clearFieldErrors() {
    document.querySelectorAll(".field-error").forEach(el => el.textContent = "");
    document.querySelectorAll(".field").forEach(f => f.classList.remove("invalid"));
}

function resolveFieldName(serverKey) {
    const last = String(serverKey).split(".").pop();
    const fields = [...document.querySelectorAll("[data-error-for]")]
        .map(el => el.getAttribute("data-error-for"));
    return fields.find(f => f.toLowerCase() === last.toLowerCase()) || null;
}


function renderPreviews(files) {
    previewBox.innerHTML = "";

    for (const file of files) {
        if (!file.type.startsWith("image/")) continue;

        const url = URL.createObjectURL(file);

        const thumb = document.createElement("div");
        thumb.className = "thumb";

        const img = document.createElement("img");
        img.src = url;
        img.alt = file.name;
        img.onload = () => URL.revokeObjectURL(url);

        thumb.appendChild(img);
        previewBox.appendChild(thumb);
    }
}

imagesInput.addEventListener("change", () => {
    setFieldError("Images", "");
    renderPreviews(imagesInput.files);
});


function validateFiles(files) {
    const errors = [];

    if (files.length > MAX_FILES) {
        errors.push(`You can upload at most ${MAX_FILES} images.`);
    }

    for (const file of files) {
        if (!ALLOWED_TYPES.includes(file.type)) {
            errors.push(`"${file.name}" is not a JPG, PNG, or GIF.`);
        }
        if (file.size > MAX_FILE_BYTES) {
            errors.push(`"${file.name}" is larger than 5 MB.`);
        }
    }

    return errors;
}


form.addEventListener("submit", async (e) => {
    e.preventDefault();
    if (redirecting) return;

    clearAlert();
    clearFieldErrors();

    const formData = new FormData(form);

  
   const requiredFields = ["Title", "Category", "Description", "Price", "Condition", "City"];
    let ok = true;

    for (const field of requiredFields) {
        const value = (formData.get(field) || "").toString().trim();
        if (!value) {
            setFieldError(field, `${field} is required.`);
            ok = false;
        }
    }

    const price = parseFloat(formData.get("Price"));
    if (formData.get("Price") && (isNaN(price) || price <= 0)) {
        setFieldError("Price", "Price must be greater than 0.");
        ok = false;
    }

    const files = imagesInput.files;
    const fileErrors = validateFiles(files);
    if (fileErrors.length > 0) {
        setFieldError("Images", fileErrors.join(" "));
        ok = false;
    }

    if (!ok) {
        showAlert("Please fix the errors below.", "error");
        return;
    }

    
    const clean = new FormData();
    clean.append("Title",       formData.get("Title").toString().trim());
    clean.append("Author",      (formData.get("Author") || "").toString().trim());
    clean.append("Category",    formData.get("Category").toString().trim());
    clean.append("Isbn",        (formData.get("Isbn") || "").toString().trim());
    clean.append("Description", formData.get("Description").toString().trim());
    clean.append("Price",       formData.get("Price"));
    clean.append("Condition",   formData.get("Condition"));
    clean.append("City",        formData.get("City").toString().trim());

    for (const file of files) {
        clean.append("Images", file, file.name);
    }

    btn.disabled = true;
    btn.textContent = "Posting...";

    try {
    const res = await API.post("/api/books", clean);

    // If API.post returns a fetch Response, parse JSON:
    const data = typeof res.json === "function" ? await res.json() : res;
    const { listingId } = data;

    redirecting = true;
    try { sessionStorage.setItem("flash", "Book posted successfully!"); } catch {}

    // Redirect to the new listing page
    window.location.replace(`book.html?id=${listingId}`);
    return;

} catch (err) {
    console.error("Post book failed:", err, err.body);

    if (err.status === 401) {
        showAlert("Please log in to post a book.", "error");
        return;
    }

    if (err.status === 400 && err.body?.errors) {
        const unmatched = [];
        for (const [key, messages] of Object.entries(err.body.errors)) {
            const msg = Array.isArray(messages) ? messages.join(" ") : String(messages);
            const field = resolveFieldName(key);
            if (field) {
                setFieldError(field, msg);
            } else {
                unmatched.push(msg);
            }
        }
        showAlert(unmatched.length ? unmatched.join(" ") : "Please fix the errors below.", "error");
        return;
    }

    if (err.status === 400) {
        showAlert(err.body?.error || err.body?.title || "Invalid input.", "error");
        return;
    }

    if (err.status === 413) {
        showAlert("Files are too large. Try fewer or smaller images.", "error");
        return;
    }

    if (err instanceof TypeError) {
        showAlert("Can't reach the server. Is the API running on localhost:5042?", "error");
        return;
    }

    showAlert(err.message || "Something went wrong.", "error");

} finally {
    if (!redirecting) {
        btn.disabled = false;
        btn.textContent = "Post Book";
    }
}

});
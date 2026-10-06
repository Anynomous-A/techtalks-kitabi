const form = document.getElementById("loginForm");
const btn = document.getElementById("submitBtn");
const alertBox = document.getElementById("alert");

function showAlert(message, type = "error") {
  alertBox.textContent = message;
  alertBox.className = `alert ${type}`;
}

function clearAlert() {
  alertBox.textContent = "";
  alertBox.className = "alert";
}

function setFieldError(field, message) {
  const el = document.querySelector(`[data-error-for="${field}"]`);
  if (el) el.textContent = message || "";
}

function clearFieldErrors() {
  document
    .querySelectorAll(".field-error")
    .forEach((el) => (el.textContent = ""));
}

function validate(data) {
  let ok = true;
  clearFieldErrors();

  if (data.name.length < 3 || data.name.length > 50) {
    setFieldError("name", "Name must be 3–50 characters.");
    ok = false;
  } else if (!/^[a-zA-Z0-9_ ]+$/.test(data.name)) {
    setFieldError("name", "Only letters, numbers, underscores, and spaces.");
    ok = false;
  }

  if (!data.password || data.password.length < 6) {
    setFieldError("password", "Password must be at least 6 characters.");
    ok = false;
  }

  return ok;
}

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  clearAlert();

  const formData = new FormData(form);
  const data = {
    name: (formData.get("name") || "").trim(),
    password: formData.get("password") || "",
  };

  if (!validate(data)) {
    showAlert("Please fix the errors below.", "error");
    return;
  }

  btn.disabled = true;
  btn.textContent = "Logging in...";

  try {
    const body = await API.post("/api/auth/login", data);

    const user = body?.data?.user;

    localStorage.setItem("user", JSON.stringify(user || {}));

    form.reset();
    clearFieldErrors();

    const firstName = user?.name || data.name;
    showAlert(`Welcome to Kitabi, ${firstName}!`, "success");
    /*
// ---------- TEMPORARY: AUTH FLOW VERIFICATION ----------
console.log("===== AUTH FLOW TEST =====");
console.log("Cookie (JS can see):", document.cookie);
console.log("AccessToken hidden from JS:", !document.cookie.includes("accessToken"));
console.log("RefreshToken hidden from JS:", !document.cookie.includes("refreshToken"));

// 1) Authenticated request WITHOUT a token — should be 401
try {
    const res = await fetch("http://localhost:5042/api/books", { method: "POST" });
    console.log("1. POST /api/books WITHOUT cookies → status:", res.status, "(expected 401)");
} catch (e) {
    console.log("1. Failed:", e.message);
}

// 2) Authenticated request WITH the cookie — should succeed
try {
    const form = new FormData();
    form.append("Title", "Test Book " + Date.now());
    form.append("Author", "Test Author");
    form.append("Category", "Test");
    form.append("Isbn", "1234567890");
    form.append("Description", "Testing cookie auth");
    form.append("Price", "9.99");
    form.append("Condition", "Good");
    form.append("City", "Beirut");

    const created = await API.post("/api/books", form);
    console.log("2. POST /api/books WITH cookie → success, listingId:", created?.listingId);
} catch (e) {
    console.log("2. POST /api/books FAILED:", e.status, e.message);
}

// 3) Refresh the session
try {
    const refreshResult = await API.post("/api/auth/refresh", { refreshToken: "" });
    console.log("3. POST /api/auth/refresh →", refreshResult?.message);
} catch (e) {
    console.log("3. Refresh FAILED:", e.status, e.message);
}

// 4) Authenticated request again AFTER refresh
try {
    const form = new FormData();
    form.append("Title", "Test Book After Refresh " + Date.now());
    form.append("Author", "Test Author");
    form.append("Category", "Test");
    form.append("Isbn", "1234567890");
    form.append("Description", "Testing new cookie");
    form.append("Price", "19.99");
    form.append("Condition", "Good");
    form.append("City", "Beirut");

    const created = await API.post("/api/books", form);
    console.log("4. POST /api/books AFTER refresh → success, listingId:", created?.listingId);
} catch (e) {
    console.log("4. POST after refresh FAILED:", e.status, e.message);
}

console.log("After all calls — document.cookie:", document.cookie);
console.log("AccessToken still hidden:", !document.cookie.includes("accessToken"));
console.log("===== END AUTH FLOW TEST =====");
// ---------- END TEMPORARY ----------
*/
  } catch (err) {
    if (err.status === 400 && err.body?.errors) {
      for (const [field, messages] of Object.entries(err.body.errors)) {
        const fieldName = field.toLowerCase();
        const msg = Array.isArray(messages) ? messages.join(" ") : messages;
        setFieldError(fieldName, msg);
      }
      showAlert("Please fix the errors below.", "error");
    } else if (err.status === 400) {
      showAlert(err.body?.error || "Invalid name or password.", "error");
    } else if (err.status === 429) {
      showAlert("Too many attempts. Try again in a minute.", "error");
    } else {
      showAlert(err.message || "Something went wrong.", "error");
    }
  } finally {
    btn.disabled = false;
    btn.textContent = "Login";
  }
});

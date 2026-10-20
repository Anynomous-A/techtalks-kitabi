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

     setTimeout(() => {
      window.location.href = "index.html";
    }, 1000);
   

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
document.querySelectorAll(".toggle-password").forEach((toggle) => {
  toggle.addEventListener("click", () => {
    const input = document.getElementById(toggle.dataset.target);
    if (!input) return;

    const isHidden = input.type === "password";

    // Show the password when the eye is open.
    input.type = isHidden ? "text" : "password";

    // Open eye = visible password.
    // Crossed-out eye = hidden password.
    toggle.classList.toggle("is-visible", isHidden);
    toggle.setAttribute("aria-pressed", String(isHidden));
    toggle.setAttribute(
      "aria-label",
      isHidden ? "Hide password" : "Show password"
    );
  });
});
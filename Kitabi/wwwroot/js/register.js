const form     = document.getElementById("registerForm");
const btn      = document.getElementById("submitBtn");
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
    document.querySelectorAll(".field-error").forEach(el => el.textContent = "");
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

    if (!data.email || !/^\S+@\S+\.\S+$/.test(data.email)) {
        setFieldError("email", "Enter a valid email.");
        ok = false;
    }

    if (!/^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,}$/.test(data.password)) {
        setFieldError("password", "6+ chars with upper, lower, and a digit.");
        ok = false;
    }

    if (!/^(70|71|76|78|79|81|03|30|31|36|38|39)\d{6}$/.test(data.phone)) {
        setFieldError("phone", "Use a valid Lebanese number, e.g. 70123456.");
        ok = false;
    }

    if (!data.university) {
        setFieldError("university", "University is required.");
        ok = false;
    }

    return ok;
}

form.addEventListener("submit", async (e) => {
    e.preventDefault();
    clearAlert();

    const formData = new FormData(form);
    const data = {
        name:       (formData.get("name") || "").trim(),
        email:      (formData.get("email") || "").trim(),
        password:   formData.get("password") || "",
        phone:      (formData.get("phone") || "").trim(),
        university: (formData.get("university") || "").trim(),
    };

    if (!validate(data)) {
        showAlert("Please fix the errors below.", "error");
        return;
    }

    btn.disabled = true;
    btn.textContent = "Creating account...";

    try {
        const body = await API.post("/api/auth/register", data);

       
        showAlert(body?.message || "Account created! Redirecting...", "success");

        form.reset();
        clearFieldErrors();

        setTimeout(() => {
            window.location.href = "login.html";
        }, 1200);

    } catch (err) {
        if (err.status === 400 && err.body?.errors) {
            for (const [field, messages] of Object.entries(err.body.errors)) {
                const fieldName = field.toLowerCase();
                const msg = Array.isArray(messages) ? messages.join(" ") : messages;
                setFieldError(fieldName, msg);
            }
            showAlert("Please fix the errors below.", "error");
        } else if (err.status === 400) {
            showAlert(err.body?.error || "Invalid input.", "error");
        } else if (err.status === 429) {
            showAlert("Too many attempts. Try again in a minute.", "error");
        } else {
            showAlert(err.message || "Something went wrong.", "error");
        }
    } finally {
        btn.disabled = false;
        btn.textContent = "Register";
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
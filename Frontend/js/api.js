// Responsibilities:
//   - Always sends cookies (credentials: "include")
//   - Sets Content-Type: application/json for non-FormData bodies
//   - Parses the response body (JSON if possible, text otherwise)
//   - Throws structured errors with status + body
//   - On 401 (from non-auth endpoints), tries to refresh the session once,
//     then retries the original request
//   - Single-flight refresh: concurrent 401s share one refresh call

const API_BASE = "http://localhost:5042";

let refreshPromise = null;

async function tryRefresh() {
    if (refreshPromise) return refreshPromise;

    refreshPromise = (async () => {
        try {
            const res = await fetch(API_BASE + "/api/auth/refresh", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ refreshToken: "" }),
                credentials: "include",
            });
            return res.ok;
        } catch {
            return false;
        } finally {
            refreshPromise = null;
        }
    })();

    return refreshPromise;
}

async function apiFetch(path, options = {}, allowRetry = true) {
    const headers = { ...(options.headers || {}) };
    const isFormData = options.body instanceof FormData;

    if (options.body && !isFormData && !headers["Content-Type"]) {
        headers["Content-Type"] = "application/json";
    }

    const response = await fetch(API_BASE + path, {
        ...options,
        headers,
        credentials: "include",
    });


    if (response.status === 401 && allowRetry && !path.startsWith("/api/auth/")) {
        const refreshed = await tryRefresh();

        if (refreshed) {
          
            return apiFetch(path, options, false);
        }

    
        localStorage.removeItem("user");
        window.location.href = "/Frontend/login.html";
        throw new Error("Session expired");
    }

    const text = await response.text();
    let body = null;
    try {
        body = text ? JSON.parse(text) : null;
    } catch {
        body = text;
    }

    if (!response.ok) {
        const err = new Error(body?.error || body?.message || response.statusText);
        err.status = response.status;
        err.body = body;
        throw err;
    }

    return body;
}

const API = {
    get:    (path)         => apiFetch(path),
    post:   (path, body)   => apiFetch(path, { method: "POST",   body: body instanceof FormData ? body : JSON.stringify(body) }),
    put:    (path, body)   => apiFetch(path, { method: "PUT",    body: body instanceof FormData ? body : JSON.stringify(body) }),
    delete: (path)         => apiFetch(path, { method: "DELETE" }),
};
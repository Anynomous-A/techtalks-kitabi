/* ============================================================
   Kitabi — Navbar behaviour
   - Hamburger toggle (mobile)
   - Active link highlighting
   - Auth-aware login / logout / profile
   - Footer year
   Safe to include on every page (null-checks everything).

   Auth source of truth:
     - Session lives in an HTTP-only cookie (server-side).
     - The logged-in user object is mirrored to localStorage("user")
       by the login flow. api.js removes it on 401.
     - So: localStorage.getItem("user") !== null  => logged in.
   ============================================================ */

(function () {
    'use strict';

    /* ============================================================
       1) AUTH CHECK
       ============================================================ */
    function getStoredUser() {
        const raw = localStorage.getItem("user");
        if (!raw) return null;
        try {
            return JSON.parse(raw);
        } catch {
            // corrupted value — treat as logged out
            localStorage.removeItem("user");
            return null;
        }
    }

    function isLoggedIn() {
        return getStoredUser() !== null;
    }

    /* ============================================================
       2) HAMBURGER TOGGLE
       ============================================================ */
    const toggleBtn = document.querySelector('.nav-toggle');
    const nav = document.querySelector('.main-nav');

    if (toggleBtn && nav) {
        toggleBtn.addEventListener('click', () => {
            const expanded = toggleBtn.getAttribute('aria-expanded') === 'true';
            toggleBtn.setAttribute('aria-expanded', String(!expanded));
            nav.classList.toggle('open', !expanded);
        });

        // Close menu when a nav link is tapped (mobile UX)
        nav.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', () => {
                if (window.matchMedia('(max-width: 720px)').matches) {
                    nav.classList.remove('open');
                    toggleBtn.setAttribute('aria-expanded', 'false');
                }
            });
        });

        // Close menu if user resizes back to desktop
        window.addEventListener('resize', () => {
            if (window.matchMedia('(min-width: 721px)').matches) {
                nav.classList.remove('open');
                toggleBtn.setAttribute('aria-expanded', 'false');
            }
        });
    }

    /* ============================================================
       3) ACTIVE LINK HIGHLIGHTING
       ============================================================ */
    const currentPage = (window.location.pathname.split('/').pop() || 'index.html').toLowerCase();
    document.querySelectorAll('.main-nav a').forEach(link => {
        const href = (link.getAttribute('href') || '').toLowerCase();
        if (href && href === currentPage) {
            link.classList.add('active');
        }
    });

    /* ============================================================
       4) AUTH-AWARE HEADER
       ============================================================ */
    const loginLink   = document.getElementById('loginLink');
    const logoutBtn   = document.getElementById('logoutBtn');
    const profileLink = document.getElementById('profileLink');

    function renderAuthState() {
        if (!loginLink || !logoutBtn || !profileLink) return;

        if (isLoggedIn()) {
            loginLink.hidden   = true;
            logoutBtn.hidden   = false;
            profileLink.hidden = false;
        } else {
            loginLink.hidden   = false;
            logoutBtn.hidden   = true;
            profileLink.hidden = true;
        }
    }

    renderAuthState();

    /* ============================================================
       5) LOGOUT
       Calls the server to clear the HTTP-only cookie, then clears
       the local mirror and redirects home.
       ============================================================ */
    if (logoutBtn) {
        logoutBtn.addEventListener('click', async () => {
            try {
                // Use API helper if present (it sends credentials: "include")
                if (typeof API !== 'undefined' && API.post) {
                    await API.post("/api/auth/logout", {});
                } else {
                    // Fallback: raw fetch with credentials
                    await fetch("http://localhost:5042/api/auth/logout", {
                        method: "POST",
                        credentials: "include",
                        headers: { "Content-Type": "application/json" },
                        body: "{}",
                    });
                }
            } catch (err) {
                // Even if the server call fails, clear local state so the
                // UI is not stuck in a logged-in look.
                console.warn("[navbar] logout request failed:", err);
            } finally {
                localStorage.removeItem("user");
                window.location.href = "index.html";
            }
        });
    }

    /* ============================================================
       6) FOOTER YEAR
       ============================================================ */
    const yearEl = document.getElementById('year');
    if (yearEl) {
        yearEl.textContent = new Date().getFullYear();
    }
})();
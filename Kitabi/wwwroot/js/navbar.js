// navbar.js
const toggleBtn = document.querySelector('.nav-toggle');
const nav = document.querySelector('.main-nav');

// Toggle menu open/close
toggleBtn.addEventListener('click', () => {
    const expanded = toggleBtn.getAttribute('aria-expanded') === 'true';
    toggleBtn.setAttribute('aria-expanded', !expanded);
    nav.style.display = expanded ? 'none' : 'flex';
});

// Active link highlighting
const currentPage = window.location.pathname.split("/").pop();
document.querySelectorAll('.main-nav a').forEach(link => {
    if (link.getAttribute('href') === currentPage) {
        link.classList.add('active');
    }
});

// Auth-aware navbar (example logic)
const loginLink = document.getElementById('loginLink');
const logoutBtn = document.getElementById('logoutBtn');
const profileLink = document.getElementById('profileLink');

// Simulated check (replace with real auth state)
const isLoggedIn = false; // set dynamically

if (isLoggedIn) {
    loginLink.style.display = 'none';
    logoutBtn.hidden = false;
    profileLink.style.display = 'inline-block';
} else {
    loginLink.style.display = 'inline-block';
    logoutBtn.hidden = true;
    profileLink.style.display = 'none';
}

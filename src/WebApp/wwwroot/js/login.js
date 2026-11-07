(() => {
    const toggle = document.querySelector('[data-password-toggle]');
    if (!toggle) return;
    const password = document.getElementById(toggle.getAttribute('aria-controls'));
    if (!password) return;
    toggle.addEventListener('click', () => {
        const visible = password.type === 'password';
        password.type = visible ? 'text' : 'password';
        toggle.setAttribute('aria-pressed', String(visible));
        toggle.setAttribute('aria-label', visible ? 'Sembunyikan password' : 'Tampilkan password');
    });
})();

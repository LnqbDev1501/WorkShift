(() => {
    const button = document.querySelector('[data-payroll-version-toggle]');
    if (!button) return;
    const panel = document.getElementById(button.getAttribute('aria-controls'));
    if (!panel) return;
    button.addEventListener('click', () => {
        panel.hidden = !panel.hidden;
        button.setAttribute('aria-expanded', String(!panel.hidden));
        button.textContent = panel.hidden ? 'View Versions' : 'Hide Versions';
        if (!panel.hidden) panel.scrollIntoView({ block: 'start', behavior: 'smooth' });
    });
})();

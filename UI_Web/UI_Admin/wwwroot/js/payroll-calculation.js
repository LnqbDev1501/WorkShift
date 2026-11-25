document.querySelectorAll('[data-calculation-form]').forEach(form => {
    form.addEventListener('submit', () => {
        if (!form.checkValidity()) return;
        form.querySelectorAll('button[type="submit"]').forEach(button => {
            button.disabled = true;
            button.textContent = 'Calculating…';
        });
    });
});
window.addEventListener('pageshow', event => {
    // A cached page must re-check server readiness before another calculation.
    if (event.persisted) window.location.reload();
});

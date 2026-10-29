document.querySelectorAll('[data-feedback-deadline]').forEach(container => {
    const deadline = Date.parse(container.dataset.feedbackDeadline);
    if (!Number.isFinite(deadline)) return;
    const check = () => {
        if (Date.now() < deadline) return;
        container.querySelectorAll('[data-feedback-submit]').forEach(button => button.disabled = true);
        container.querySelectorAll('[data-feedback-link]').forEach(link => { link.removeAttribute('href'); link.setAttribute('aria-disabled', 'true'); link.textContent = 'Feedback deadline expired'; });
        if (!container.querySelector('[data-expiry-message]')) {
            const message = document.createElement('p'); message.dataset.expiryMessage = ''; message.setAttribute('role', 'status'); message.textContent = 'Feedback submission window has closed.'; container.appendChild(message);
        }
    };
    let timer;
    const start = () => { check(); clearInterval(timer); timer = setInterval(check, 1000); };
    start();
    window.addEventListener('pageshow', start);
    window.addEventListener('pagehide', () => clearInterval(timer));
});

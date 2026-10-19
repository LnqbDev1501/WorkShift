(() => {
    document.querySelectorAll('[data-salary-form]').forEach(form => {
        form.querySelectorAll('[data-monday]').forEach(input => {
            const validate = () => {
                const date = input.value ? new Date(`${input.value}T00:00:00Z`) : null;
                input.setCustomValidity(date && date.getUTCDay() !== 1 ? 'Please select a Monday.' : '');
            };
            input.addEventListener('input', validate);
            validate();
        });
        form.addEventListener('submit', event => {
            if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
            if (!form.reportValidity()) { event.preventDefault(); return; }
            form.dataset.submitting = 'true';
            form.querySelectorAll('button[type="submit"]').forEach(button => {
                button.disabled = true;
                button.textContent = 'Processing...';
            });
        });
    });
    window.addEventListener('pageshow', event => { if (event.persisted) window.location.reload(); });
})();

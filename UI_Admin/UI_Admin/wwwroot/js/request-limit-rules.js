(() => {
    const form = document.getElementById('rule-editor');
    const preview = document.querySelector('.rl-preview');
    if (!form || !preview) return;
    const update = () => {
        const type = form.querySelector('select[name="RequestType"]');
        if (type) preview.querySelector('[data-preview-type]').textContent = type.value ? type.selectedOptions[0].textContent : 'Choose a request type';
        ['limit', 'notice'].forEach((name, i) => {
            const field = form.querySelector(`[name="${i ? 'MinimumAdvanceDays' : 'MaxRequestsPerMonth'}"]`);
            preview.querySelector(`[data-preview-${name}]`).textContent = field.value || '—';
        });
        const date = form.querySelector('[name="EffectiveFrom"]').value;
        const parsed = date ? new Date(`${date}T00:00:00Z`) : null;
        preview.querySelector('[data-preview-date]').textContent = parsed && !isNaN(parsed) ? new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(parsed) : 'Not set';
        const weekday = form.querySelector('[name="SubmissionDeadlineDayOfWeek"]');
        preview.querySelector('[data-preview-weekday]').textContent = weekday.selectedOptions[0]?.textContent || 'No weekday cutoff';
    };
    form.addEventListener('input', update);
    form.addEventListener('change', update);
    update();
})();

(() => {
    const dialog = document.getElementById('publish-rule-dialog');
    const opener = document.getElementById('open-publish-dialog');
    if (!dialog || !opener) return;
    let previousOverflow = '';
    opener.addEventListener('click', () => {
        previousOverflow = document.body.style.overflow;
        document.body.style.overflow = 'hidden';
        dialog.showModal();
    });
    dialog.querySelectorAll('[data-close-publish]').forEach(button => button.addEventListener('click', () => dialog.close()));
    dialog.addEventListener('click', event => {
        if (event.target !== dialog) return;
        const rect = dialog.getBoundingClientRect();
        if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) dialog.close();
    });
    dialog.addEventListener('close', () => {
        document.body.style.overflow = previousOverflow;
        opener.focus();
    });
    document.getElementById('publish-rule-form')?.addEventListener('submit', event => {
        const button = event.currentTarget.querySelector('button[type="submit"]');
        button.disabled = true;
        button.textContent = 'Publishing…';
    });
})();

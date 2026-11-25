(() => {
    const dialog = document.getElementById('status-dialog');
    const submit = dialog?.querySelector('button[type="submit"]');
    const submitLabel = submit?.textContent;
    document.getElementById('status-open')?.addEventListener('click', () => dialog.showModal());
    document.getElementById('status-cancel')?.addEventListener('click', () => dialog.close());
    dialog?.addEventListener('close', () => document.getElementById('status-open')?.focus());
    dialog?.querySelector('form')?.addEventListener('submit', event => {
        if (submit.disabled) { event.preventDefault(); return; }
        submit.disabled = true;
        submit.textContent = 'Saving…';
        event.currentTarget.setAttribute('aria-busy', 'true');
    });
    window.addEventListener('pageshow', () => {
        if (submit) { submit.disabled = false; submit.textContent = submitLabel; }
        dialog?.querySelector('form')?.removeAttribute('aria-busy');
    });
    document.getElementById('print-template')?.addEventListener('click', () => window.print());
})();

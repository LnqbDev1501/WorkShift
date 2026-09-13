(() => {
    const dialog = document.getElementById('list-status-dialog');
    if (!dialog) return;
    const confirm = document.getElementById('list-status-confirm');
    let opener;
    document.querySelectorAll('.template-status-trigger').forEach(button => {
        button.addEventListener('click', () => {
            opener = button;
            const restore = button.dataset.restore === 'true';
            document.getElementById('list-status-id').value = button.dataset.id;
            document.getElementById('list-status-restore').value = button.dataset.restore;
            document.getElementById('list-status-title').textContent = restore ? 'Restore this shift template?' : 'Deactivate this shift template?';
            document.getElementById('list-status-description').textContent = `“${button.dataset.name}” will be marked as ${restore ? 'active' : 'inactive'}. Existing scheduled shifts will remain unchanged. You can change its status again later.`;
            confirm.textContent = restore ? 'Restore Template' : 'Deactivate Template';
            confirm.className = restore ? 'template-confirm-restore' : 'template-confirm-deactivate';
            confirm.disabled = false;
            dialog.showModal();
        });
    });
    document.getElementById('list-status-cancel').addEventListener('click', () => dialog.close());
    dialog.addEventListener('close', () => opener?.focus());
    dialog.querySelector('form').addEventListener('submit', () => {
        confirm.disabled = true;
        confirm.textContent = 'Saving…';
    });
    window.addEventListener('pageshow', () => { confirm.disabled = false; });
})();

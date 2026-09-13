(() => {
    const dialog = document.getElementById('template-success-dialog');
    if (!dialog) return;

    dialog.addEventListener('close', () => {
        document.querySelector('#status-open, .shift-heading a, .detail-back')?.focus();
    });
    dialog.showModal();
})();

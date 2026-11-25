(() => {
    const dialog = document.getElementById('working-time-action-dialog');
    if (!dialog) return;

    const title = document.getElementById('working-time-action-title');
    const description = document.getElementById('working-time-action-description');
    const confirmButton = document.getElementById('working-time-action-confirm');
    const dismissButton = document.getElementById('working-time-action-dismiss');
    const actions = {
        delete: { title: 'Delete this draft?', text: 'will be permanently deleted. This action cannot be undone.', label: 'Delete draft', danger: true },
        publish: { title: 'Publish this version?', text: 'will be scheduled for its effective date. Only one version can be Publish at a time.', label: 'Publish version', danger: false },
        cancel: { title: 'Cancel publication?', text: 'will be marked as Cancelled and will no longer activate on its effective date.', label: 'Cancel publication', danger: true }
    };
    let pendingForm = null;
    let trigger = null;

    document.querySelectorAll('form[data-rule-action]').forEach(form => {
        form.addEventListener('submit', event => {
            event.preventDefault();
            const action = actions[form.dataset.ruleAction];
            if (!action || dialog.open) return;
            pendingForm = form;
            trigger = event.submitter;
            title.textContent = action.title;
            description.textContent = `“${form.dataset.ruleName}” ${action.text}`;
            confirmButton.textContent = action.label;
            confirmButton.disabled = false;
            dialog.classList.toggle('is-danger', action.danger);
            dialog.showModal();
            dismissButton.focus();
        });
    });

    dismissButton.addEventListener('click', () => dialog.close());
    dialog.addEventListener('close', () => {
        pendingForm = null;
        trigger?.focus();
        trigger = null;
    });
    confirmButton.addEventListener('click', () => {
        if (!pendingForm || confirmButton.disabled) return;
        confirmButton.disabled = true;
        confirmButton.textContent = 'Processing…';
        // Submit the original form so its route and anti-forgery token are preserved.
        HTMLFormElement.prototype.submit.call(pendingForm);
    });
})();

(() => {
    'use strict';
    let loading = false;
    const feedback = document.querySelector('[data-employee-type-feedback]');
    const loadError = 'Unable to load employee type details. Please try again.';
    document.querySelector('[data-refresh-employee-types]')?.addEventListener('click', () => window.location.reload());
    document.querySelectorAll('[data-employee-type-action]').forEach(button => {
        button.addEventListener('click', async () => {
            if (loading) return;
            const dialog = document.getElementById(`${button.dataset.employeeTypeAction}-employee-type-dialog`);
            if (!dialog) return;
            loading = true;
            button.disabled = true;
            button.setAttribute('aria-busy', 'true');
            if (feedback) feedback.hidden = true;
            try {
                const response = await fetch(button.dataset.confirmationUrl, {
                    headers: { Accept: 'application/json' }, credentials: 'same-origin', cache: 'no-store'
                });
                if (response.redirected) throw new Error('Your session has expired. Please sign in again.');
                const detail = await response.json();
                if (!response.ok) throw new Error(detail.message || loadError);
                if (String(detail.employeeTypeId) !== button.dataset.id || typeof detail.typeName !== 'string' ||
                    !detail.typeName.trim() || !['ACTIVE', 'INACTIVE'].includes(detail.status))
                    throw new Error(loadError);
                const deactivate = button.dataset.employeeTypeAction === 'deactivate';
                if (detail.status === (deactivate ? 'INACTIVE' : 'ACTIVE'))
                    throw new Error(deactivate ? 'The employee type is already inactive.' : 'The employee type is already active.');

                dialog.querySelector('[data-target-name]').textContent = detail.typeName;
                dialog.querySelector('[data-target-status]').textContent = detail.status === 'ACTIVE' ? 'Active' : 'Inactive';
                dialog.querySelector('input[name="id"]').value = String(detail.employeeTypeId);
                dialog.querySelector('button[type="submit"]').disabled = false;
                dialog.dataset.loadedId = String(detail.employeeTypeId);
                dialog.showModal();
            } catch (error) {
                if (feedback) {
                    feedback.querySelector('[data-error-text]').textContent = error instanceof Error && !(error instanceof SyntaxError) && !(error instanceof TypeError)
                        ? error.message : loadError;
                    feedback.hidden = false;
                    feedback.focus();
                }
            } finally {
                loading = false;
                button.disabled = false;
                button.removeAttribute('aria-busy');
            }
        });
    });
    document.querySelectorAll('dialog[id$="-employee-type-dialog"]').forEach(dialog => {
        dialog.querySelectorAll('[data-close-dialog]').forEach(button => {
            button.addEventListener('click', () => dialog.close());
        });
        dialog.addEventListener('click', event => {
            const bounds = dialog.getBoundingClientRect();
            if (event.target === dialog && (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom)) {
                dialog.close();
            }
        });
        dialog.addEventListener('close', () => {
            dialog.dataset.loadedId = '';
            dialog.querySelector('input[name="id"]').value = '';
        });
        dialog.querySelector('form').addEventListener('submit', event => {
            if (!dialog.open || !dialog.dataset.loadedId || dialog.querySelector('input[name="id"]').value !== dialog.dataset.loadedId) {
                event.preventDefault();
                return;
            }
            dialog.querySelector('button[type="submit"]').disabled = true;
        });
    });
})();

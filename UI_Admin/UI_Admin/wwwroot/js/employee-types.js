(() => {
    document.querySelectorAll('[data-employee-type-action]').forEach(button => {
        button.addEventListener('click', () => {
            const dialog = document.getElementById(`${button.dataset.employeeTypeAction}-employee-type-dialog`);
            if (!dialog) return;
            dialog.querySelector('[data-target-name]').textContent = button.dataset.name;
            dialog.querySelector('input[name="id"]').value = button.dataset.id;
            dialog.showModal();
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
        dialog.querySelector('form').addEventListener('submit', () => {
            dialog.querySelector('button[type="submit"]').disabled = true;
        });
    });
})();

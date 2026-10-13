(() => {
    const form = document.getElementById('shift-form');
    if (!form) return;
    const rows = document.getElementById('requirement-rows');
    const template = document.getElementById('requirement-template');
    const reindex = () => {
        [...rows.children].forEach((row, i) => {
            row.querySelectorAll('[data-field]').forEach(field => {
                field.name = `EmployeeRequirements[${i}].${field.dataset.field}`;
                field.id = `EmployeeRequirements_${i}__${field.dataset.field}`;
                field.closest('div').querySelector('label').htmlFor = field.id;
            });
            row.querySelector('.sh-remove').disabled = rows.children.length <= 1;
        });
    };
    document.getElementById('add-requirement').addEventListener('click', () => {
        rows.append(template.content.cloneNode(true));
        reindex(); rows.lastElementChild.querySelector('select').focus();
    });
    rows.addEventListener('click', event => {
        const remove = event.target.closest('.sh-remove');
        if (!remove || rows.children.length <= 1) return;
        const row = remove.closest('.sh-requirement-row');
        const focusRow = row.nextElementSibling || row.previousElementSibling;
        row.remove(); reindex(); focusRow?.querySelector('select').focus();
    });
    form.addEventListener('submit', reindex);
    reindex();
})();

(() => {
    const form = document.getElementById('shift-form');
    const preview = document.getElementById('shift-preview');
    if (!form || !preview) return;
    const update = () => {
        const template = form.querySelector('[name="ShiftTemplateId"]');
        preview.querySelector('[data-shift-preview]').textContent = template.value ? template.selectedOptions[0].textContent : 'Select a template';
        const dateValue = form.querySelector('[name="WorkDate"]').value;
        const date = dateValue ? new Date(`${dateValue}T00:00:00Z`) : null;
        preview.querySelector('[data-work-date-preview]').textContent = date && !isNaN(date) ? new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(date) : 'Not set';
        const counts = [...form.querySelectorAll('[data-field="RequiredCount"]')].map(field => Number(field.value));
        preview.querySelector('[data-slots-preview]').textContent = counts.every(value => Number.isInteger(value) && value > 0) ? counts.reduce((total, value) => total + value, 0).toLocaleString('en-US') : '—';
    };
    form.addEventListener('input', update); form.addEventListener('change', update);
    new MutationObserver(update).observe(document.getElementById('requirement-rows'), { childList: true });
    update();
})();

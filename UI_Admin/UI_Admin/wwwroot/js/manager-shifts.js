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

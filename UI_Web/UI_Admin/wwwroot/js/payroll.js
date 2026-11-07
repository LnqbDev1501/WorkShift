(() => {
    const table = document.getElementById('payroll-table');
    document.getElementById('payroll-region')?.addEventListener('change', () => {
        document.getElementById('payroll-branch').value = '';
        document.getElementById('payroll-filter-form').requestSubmit();
    });
    document.getElementById('payroll-density')?.addEventListener('click', event => {
        const compact = table.classList.toggle('compact');
        event.currentTarget.setAttribute('aria-pressed', String(compact));
        event.currentTarget.textContent = compact ? '☷ Comfortable rows' : '☷ Compact rows';
    });
    document.getElementById('export-payroll')?.addEventListener('click', () => {
        const quote = text => {
            let value = text.trim().replace(/\s*\n\s*/g, ' · ');
            if (/^[=+@-]/.test(value)) value = "'" + value;
            return '"' + value.replaceAll('"', '""') + '"';
        };
        const rows = Array.from(table.rows).map(row => Array.from(row.cells)
            .filter(cell => !cell.hasAttribute('data-no-export')).map(cell => quote(cell.innerText)).join(','));
        const url = URL.createObjectURL(new Blob(['\uFEFF' + rows.join('\r\n')], { type: 'text/csv;charset=utf-8;' }));
        const link = document.createElement('a');
        link.href = url;
        link.download = 'payroll-periods-page.csv';
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    });
})();

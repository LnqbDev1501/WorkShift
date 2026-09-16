document.addEventListener('DOMContentLoaded', function () {
    var container = document.getElementById('holiday-rows');
    var addButton = document.getElementById('add-holiday-row');
    if (!container || !addButton) return;

    function reindex() {
        var rows = container.querySelectorAll('[data-row]');
        var rowCount = document.getElementById('batch-row-count');
        var total = 0;
        rows.forEach(function (row) {
            var from = row.querySelector('[name$=".FromDate"]');
            var to = row.querySelector('[name$=".ToDate"]');
            if (from && to) {
                to.min = from.value;
                to.setCustomValidity(to.value && from.value && to.value < from.value ? 'To date must be on or after From date.' : '');
                if (from.value && to.value && to.value >= from.value)
                    total += Math.round((Date.parse(to.value) - Date.parse(from.value)) / 86400000) + 1;
            }
        });
        if (rowCount) rowCount.textContent = String(total);
        addButton.disabled = rows.length >= 366;
        rows.forEach(function (row, index) {
            row.querySelector('.row-number').textContent = String(index + 1);
            row.querySelectorAll('[name]').forEach(function (field) {
                field.name = field.name.replace(/Holidays\[\d+\]/, 'Holidays[' + index + ']');
                field.id = field.id.replace(/Holidays_\d+__/, 'Holidays_' + index + '__');
            });
            row.querySelectorAll('[data-valmsg-for]').forEach(function (message) {
                var key = 'Holidays[' + index + '].' + (message.dataset.valmsgFor.split('.').pop());
                message.dataset.valmsgFor = key;
            });
        });
        container.querySelectorAll('.remove-holiday-row').forEach(function (button) {
            button.disabled = rows.length === 1;
            button.style.opacity = rows.length === 1 ? '.35' : '1';
        });
    }

    function wireRemove(button) {
        button.addEventListener('click', function () {
            if (container.querySelectorAll('[data-row]').length <= 1) return;
            button.closest('[data-row]').remove();
            reindex();
        });
    }

    container.querySelectorAll('.remove-holiday-row').forEach(wireRemove);
    addButton.addEventListener('click', function () {
        var index = container.querySelectorAll('[data-row]').length;
        if (index >= 366) return;
        var row = document.createElement('div');
        row.className = 'holiday-form-row holiday-range-row';
        row.setAttribute('data-row', '');
        row.innerHTML = '<div class="row-number">' + (index + 1) + '</div>' +
            '<label><span>Holiday name *</span><input name="Holidays[' + index + '].HolidayName" id="Holidays_' + index + '__HolidayName" maxlength="150" placeholder="e.g., National Day" required /><span data-valmsg-for="Holidays[' + index + '].HolidayName"></span></label>' +
            '<label><span>From date *</span><input name="Holidays[' + index + '].FromDate" id="Holidays_' + index + '__FromDate" type="date" required /><span data-valmsg-for="Holidays[' + index + '].FromDate"></span></label>' +
            '<label><span>To date *</span><input name="Holidays[' + index + '].ToDate" id="Holidays_' + index + '__ToDate" type="date" required /><span data-valmsg-for="Holidays[' + index + '].ToDate"></span></label>' +
            '<label><span>Pay multiplier *</span><div class="multiplier-input"><input name="Holidays[' + index + '].PayMultiplier" id="Holidays_' + index + '__PayMultiplier" type="number" min="0.01" max="999.99" step="0.01" value="3.00" required /><b>×</b></div><span data-valmsg-for="Holidays[' + index + '].PayMultiplier"></span></label>' +
            '<button type="button" class="remove-holiday-row" title="Remove row"><span class="material-symbols-outlined">delete</span></button>';
        container.appendChild(row);
        wireRemove(row.querySelector('.remove-holiday-row'));
        reindex();
        row.querySelector('input').focus();
    });
    container.addEventListener('input', reindex);
    reindex();
});

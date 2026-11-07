(function () {
    function openModal(modalId, nameId, inputId, id, name) {
        var modal = document.getElementById(modalId);
        if (!modal) return;
        var nameEl = document.getElementById(nameId);
        if (nameEl) nameEl.textContent = name || 'this staff';
        var inputEl = document.getElementById(inputId);
        if (inputEl) inputEl.value = id || '';
        modal.classList.remove('hidden');
    }

    function closeModal(modalId) {
        var modal = document.getElementById(modalId);
        if (modal) modal.classList.add('hidden');
    }

    // Delegated click handling: works no matter when the buttons are rendered.
    document.addEventListener('click', function (e) {
        var openDeactivate = e.target.closest ? e.target.closest('.open-deactivate-btn') : null;
        if (openDeactivate) {
            openModal('deactivate-staff-modal', 'deactivate-target-name', 'deactivate-staff-id',
                openDeactivate.getAttribute('data-id'), openDeactivate.getAttribute('data-name'));
            return;
        }

        var openRestore = e.target.closest ? e.target.closest('.open-restore-btn') : null;
        if (openRestore) {
            openModal('restore-staff-modal', 'restore-target-name', 'restore-staff-id',
                openRestore.getAttribute('data-id'), openRestore.getAttribute('data-name'));
            return;
        }

        if (e.target.closest && e.target.closest('#close-deactivate-modal')) {
            closeModal('deactivate-staff-modal');
            return;
        }
        if (e.target.closest && e.target.closest('#cancel-deactivate-modal')) {
            closeModal('deactivate-staff-modal');
            return;
        }
        if (e.target.closest && e.target.closest('#close-restore-modal')) {
            closeModal('restore-staff-modal');
            return;
        }
        if (e.target.closest && e.target.closest('#cancel-restore-modal')) {
            closeModal('restore-staff-modal');
            return;
        }

        if (e.target && e.target.id === 'deactivate-staff-modal') {
            closeModal('deactivate-staff-modal');
            return;
        }
        if (e.target && e.target.id === 'restore-staff-modal') {
            closeModal('restore-staff-modal');
            return;
        }
    });

    // Client-side quick filter fallback
    var searchInput = document.getElementById('staff-search');
    var tableBody = document.getElementById('staff-table-body');

    function applyClientFilters() {
        if (!tableBody) return;
        var query = (searchInput ? searchInput.value : '').toLowerCase().trim();
        var rows = tableBody.querySelectorAll('tr');
        for (var i = 0; i < rows.length; i++) {
            var text = rows[i].innerText.toLowerCase();
            rows[i].style.display = !query || text.indexOf(query) !== -1 ? '' : 'none';
        }
    }

    if (searchInput) {
        var debounceTimer;
        searchInput.addEventListener('input', function () {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(applyClientFilters, 250);
        });
    }
})();

(function () {
    // Deactivate Modal Controls
    const deactivateModal = document.getElementById('deactivate-category-modal');
    const closeDeactivateBtn = document.getElementById('close-deactivate-modal');
    const cancelDeactivateBtn = document.getElementById('cancel-deactivate-modal');
    const deactivateTargetName = document.getElementById('deactivate-target-name');
    const deactivateCategoryId = document.getElementById('deactivate-category-id');

    const toggleDeactivateModal = (show, id = 0, name = '') => {
        if (!deactivateModal) return;
        if (show) {
            if (deactivateTargetName) deactivateTargetName.textContent = name || 'this category';
            if (deactivateCategoryId) deactivateCategoryId.value = id;
            deactivateModal.classList.remove('hidden');
        } else {
            deactivateModal.classList.add('hidden');
        }
    };

    document.querySelectorAll('.open-deactivate-btn').forEach((btn) => {
        btn.addEventListener('click', function () {
            const id = this.getAttribute('data-id');
            const name = this.getAttribute('data-name');
            toggleDeactivateModal(true, id, name);
        });
    });

    if (closeDeactivateBtn) closeDeactivateBtn.addEventListener('click', () => toggleDeactivateModal(false));
    if (cancelDeactivateBtn) cancelDeactivateBtn.addEventListener('click', () => toggleDeactivateModal(false));

    if (deactivateModal) {
        deactivateModal.addEventListener('click', (e) => {
            if (e.target === deactivateModal) toggleDeactivateModal(false);
        });
    }

    // Restore Modal Controls
    const restoreModal = document.getElementById('restore-category-modal');
    const closeRestoreBtn = document.getElementById('close-restore-modal');
    const cancelRestoreBtn = document.getElementById('cancel-restore-modal');
    const restoreTargetName = document.getElementById('restore-target-name');
    const restoreCategoryId = document.getElementById('restore-category-id');

    const toggleRestoreModal = (show, id = 0, name = '') => {
        if (!restoreModal) return;
        if (show) {
            if (restoreTargetName) restoreTargetName.textContent = name || 'this category';
            if (restoreCategoryId) restoreCategoryId.value = id;
            restoreModal.classList.remove('hidden');
        } else {
            restoreModal.classList.add('hidden');
        }
    };

    document.querySelectorAll('.open-restore-btn').forEach((btn) => {
        btn.addEventListener('click', function () {
            const id = this.getAttribute('data-id');
            const name = this.getAttribute('data-name');
            toggleRestoreModal(true, id, name);
        });
    });

    if (closeRestoreBtn) closeRestoreBtn.addEventListener('click', () => toggleRestoreModal(false));
    if (cancelRestoreBtn) cancelRestoreBtn.addEventListener('click', () => toggleRestoreModal(false));

    if (restoreModal) {
        restoreModal.addEventListener('click', (e) => {
            if (e.target === restoreModal) toggleRestoreModal(false);
        });
    }

    // Client-side quick filter fallback
    const searchInput = document.getElementById('category-search');
    const statusFilter = document.getElementById('status-filter');
    const tableBody = document.getElementById('categories-table-body');

    const applyClientFilters = () => {
        if (!tableBody) return;
        const query = (searchInput ? searchInput.value : '').toLowerCase().trim();
        const statusVal = statusFilter ? statusFilter.value : 'all';
        const rows = tableBody.querySelectorAll('tr');

        rows.forEach((row) => {
            const text = row.innerText.toLowerCase();
            const matchesQuery = !query || text.includes(query);
            let matchesStatus = true;

            if (statusVal === 'active') {
                matchesStatus = text.includes('active') && !text.includes('inactive');
            } else if (statusVal === 'inactive') {
                matchesStatus = text.includes('inactive');
            }

            row.style.display = matchesQuery && matchesStatus ? '' : 'none';
        });
    };

    // Keyup search filtering
    if (searchInput) {
        let debounceTimer;
        searchInput.addEventListener('input', () => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(applyClientFilters, 250);
        });
    }
})();

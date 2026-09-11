(function () {
    // Sidebar Active State Sync
    const navLinks = document.querySelectorAll('aside nav a[asp-controller="Regions"], aside nav a[data-path="regions"]');
    navLinks.forEach((link) => {
        link.classList.add('bg-primary-container', 'text-white', 'font-title-md');
        link.classList.remove('text-[#9ca3af]');
    });

    // Create Modal Controls
    const openCreateBtn = document.getElementById('open-create-modal');
    const closeCreateBtn = document.getElementById('close-create-modal');
    const cancelCreateBtn = document.getElementById('cancel-create-modal');
    const createModal = document.getElementById('create-region-modal');

    const toggleCreateModal = (show) => {
        if (!createModal) return;
        if (show) {
            createModal.classList.remove('hidden');
        } else {
            createModal.classList.add('hidden');
        }
    };

    if (openCreateBtn) openCreateBtn.addEventListener('click', () => toggleCreateModal(true));
    if (closeCreateBtn) closeCreateBtn.addEventListener('click', () => toggleCreateModal(false));
    if (cancelCreateBtn) cancelCreateBtn.addEventListener('click', () => toggleCreateModal(false));

    if (createModal) {
        createModal.addEventListener('click', (e) => {
            if (e.target === createModal) toggleCreateModal(false);
        });
    }

    // Deactivate Modal Controls
    const deactivateModal = document.getElementById('deactivate-region-modal');
    const closeDeactivateBtn = document.getElementById('close-deactivate-modal');
    const cancelDeactivateBtn = document.getElementById('cancel-deactivate-modal');
    const deactivateTargetName = document.getElementById('deactivate-target-name');
    const deactivateRegionId = document.getElementById('deactivate-region-id');

    const toggleDeactivateModal = (show, id = 0, name = '') => {
        if (!deactivateModal) return;
        if (show) {
            if (deactivateTargetName) deactivateTargetName.textContent = name || 'this region';
            if (deactivateRegionId) deactivateRegionId.value = id;
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

    // Client-side quick filter fallback if required
    const searchInput = document.getElementById('region-search');
    const statusFilter = document.getElementById('status-filter');
    const tableBody = document.getElementById('regions-table-body');

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

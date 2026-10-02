(() => {
    const form = document.getElementById('contract-form');
    const start = document.getElementById('StartDate');
    const end = document.getElementById('EndDate');
    const position = document.getElementById('EmployeePosition');
    const staffSelect = document.getElementById('StaffUserId');
    const replacesContractInput = document.getElementById('ReplacesContractId');
    const staffSelectLabel = document.getElementById('staff-select-label');
    const staffHelpText = document.getElementById('staff-help-text');
    const noStaffWarning = document.getElementById('no-staff-warning');
    const button = document.getElementById('save-contract');
    const radioNew = document.getElementById('radio-mode-new');
    const radioRenewal = document.getElementById('radio-mode-renewal');
    const labelNew = document.getElementById('label-mode-new');
    const labelRenewal = document.getElementById('label-mode-renewal');

    const cache = {};

    function updateModeStyles() {
        const isRenewal = radioRenewal && radioRenewal.checked;
        if (labelNew && labelRenewal) {
            if (isRenewal) {
                labelRenewal.classList.add('border-indigo-600', 'bg-indigo-50/40', 'ring-2', 'ring-indigo-600/20');
                labelRenewal.classList.remove('border-slate-200');
                labelNew.classList.remove('border-indigo-600', 'bg-indigo-50/40', 'ring-2', 'ring-indigo-600/20');
                labelNew.classList.add('border-slate-200');
            } else {
                labelNew.classList.add('border-indigo-600', 'bg-indigo-50/40', 'ring-2', 'ring-indigo-600/20');
                labelNew.classList.remove('border-slate-200');
                labelRenewal.classList.remove('border-indigo-600', 'bg-indigo-50/40', 'ring-2', 'ring-indigo-600/20');
                labelRenewal.classList.add('border-slate-200');
            }
        }

        if (staffSelectLabel) {
            staffSelectLabel.textContent = isRenewal ? 'Staff for Renewal' : 'New Staff';
        }
        if (staffHelpText) {
            staffHelpText.textContent = isRenewal
                ? 'Only displaying staff belonging to your branch who previously had an employment contract.'
                : 'Only displaying staff who have never had an employment contract. Branch and minimum wage will follow your managed branch.';
        }
        if (button) {
            button.textContent = isRenewal ? 'Save Renewal Draft' : 'Save Draft';
        }
    }

    async function loadStaffOptions(mode) {
        if (cache[mode]) {
            renderOptions(cache[mode], mode);
            return;
        }

        if (staffSelect) {
            staffSelect.disabled = true;
            staffSelect.innerHTML = '<option value="">Loading staff list...</option>';
        }

        try {
            const resp = await fetch(`/ManagerContracts/StaffOptions?mode=${encodeURIComponent(mode)}`);
            if (resp.ok) {
                const data = await resp.json();
                cache[mode] = data;
                renderOptions(data, mode);
            } else {
                renderError();
            }
        } catch {
            renderError();
        } finally {
            if (staffSelect) staffSelect.disabled = false;
        }
    }

    function renderOptions(options, mode) {
        if (!staffSelect) return;
        const isRenewal = mode === 'RENEWAL';
        staffSelect.innerHTML = '<option value="">-- Select Staff Member --</option>';

        if (options && options.length > 0) {
            options.forEach(opt => {
                const el = document.createElement('option');
                el.value = opt.staffUserId;
                el.dataset.contractId = opt.currentContractId || '';
                el.dataset.contractNumber = opt.currentContractNumber || '';
                el.dataset.position = opt.currentPosition || '';
                el.textContent = isRenewal && opt.currentContractNumber
                    ? `${opt.fullName} (Contract: ${opt.currentContractNumber}${opt.currentPosition ? ` - ${opt.currentPosition}` : ''})`
                    : opt.fullName;
                staffSelect.appendChild(el);
            });
            if (noStaffWarning) noStaffWarning.classList.add('hidden');
            if (button) button.disabled = false;
        } else {
            if (noStaffWarning) noStaffWarning.classList.remove('hidden');
            if (button) button.disabled = true;
        }
        onStaffSelected();
    }

    function renderError() {
        if (!staffSelect) return;
        staffSelect.innerHTML = '<option value="">Failed to load staff list</option>';
        if (noStaffWarning) noStaffWarning.classList.remove('hidden');
        if (button) button.disabled = true;
    }

    function onStaffSelected() {
        if (!staffSelect) return;
        const opt = staffSelect.options[staffSelect.selectedIndex];
        const isRenewal = radioRenewal && radioRenewal.checked;
        if (opt && opt.value) {
            if (isRenewal && replacesContractInput) {
                replacesContractInput.value = opt.dataset.contractId || '';
                if (position && !position.value && opt.dataset.position) {
                    position.value = opt.dataset.position;
                }
            } else if (replacesContractInput) {
                replacesContractInput.value = '';
            }
        } else {
            if (replacesContractInput) replacesContractInput.value = '';
        }
    }

    if (staffSelect) {
        staffSelect.addEventListener('change', onStaffSelected);
    }

    if (radioNew) {
        radioNew.addEventListener('change', () => {
            if (radioNew.checked) {
                updateModeStyles();
                loadStaffOptions('NEW');
            }
        });
    }

    if (radioRenewal) {
        radioRenewal.addEventListener('change', () => {
            if (radioRenewal.checked) {
                updateModeStyles();
                loadStaffOptions('RENEWAL');
            }
        });
    }

    function validate() {
        if (end && start) {
            end.setCustomValidity(end.value && start.value && end.value <= start.value ? 'End date must be later than start date.' : '');
        }
        if (position) {
            position.setCustomValidity(position.value.trim() ? '' : 'Please enter employee position.');
        }
    }

    [start, end, position].filter(Boolean).forEach(input => input.addEventListener('input', validate));

    let submitting = false;
    if (form) {
        form.addEventListener('submit', event => {
            validate();
            onStaffSelected();
            if (submitting || !form.reportValidity()) {
                event.preventDefault();
                return;
            }
            submitting = true;
            if (button) {
                button.disabled = true;
                button.textContent = 'Saving…';
            }
        });
    }

    window.addEventListener('pageshow', () => {
        submitting = false;
        updateModeStyles();
        onStaffSelected();
    });

    // Initialize on load
    updateModeStyles();
    onStaffSelected();
})();

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
    const iconBoxNew = document.getElementById('icon-box-new');
    const iconBoxRenewal = document.getElementById('icon-box-renewal');
    const indicatorNew = document.getElementById('indicator-new');
    const indicatorRenewal = document.getElementById('indicator-renewal');

    const hourlyRateInput = document.getElementById('HourlyRate');
    const contractTermsInput = document.getElementById('ContractTerms');
    const wageSourceBadge = document.getElementById('wage-source-badge');
    const hourlyRateGuidanceNote = document.getElementById('hourly-rate-guidance-note');
    const regionalWageInput = document.getElementById('regional-minimum-wage-data');

    // Smart Guidance Elements
    const renewalGuidanceCard = document.getElementById('renewal-guidance-card');
    const guidanceIconBox = document.getElementById('guidance-icon-box');
    const guidanceIcon = document.getElementById('guidance-icon');
    const guidanceStatusBadge = document.getElementById('guidance-status-badge');
    const btnApplySuggestedDate = document.getElementById('btn-apply-suggested-date');
    const btnApplySuggestedDateText = document.getElementById('btn-apply-suggested-date-text');
    const snapshotContractNumber = document.getElementById('snapshot-contract-number');
    const snapshotPosition = document.getElementById('snapshot-position');
    const snapshotHourlyRate = document.getElementById('snapshot-hourly-rate');
    const snapshotTerm = document.getElementById('snapshot-term');
    const guidanceMessageBox = document.getElementById('guidance-message-box');
    const guidanceMessageText = document.getElementById('guidance-message-text');
    const startDateTip = document.getElementById('start-date-tip');
    const startDateGuidanceNote = document.getElementById('start-date-guidance-note');

    const cache = {};

    let regionalWage = 0;
    if (regionalWageInput && regionalWageInput.value) {
        regionalWage = parseFloat(regionalWageInput.value) || 0;
    }

    let isUserCustomRate = false;

    async function ensureRegionalWage() {
        if (regionalWage > 0) return regionalWage;
        try {
            const resp = await fetch('/ManagerContracts/BranchMinimumWage');
            if (resp.ok) {
                const data = await resp.json();
                if (data && data.minimumWage) {
                    regionalWage = parseFloat(data.minimumWage) || 0;
                    if (regionalWageInput) regionalWageInput.value = regionalWage;
                    const isRenewal = Boolean(radioRenewal?.checked);
                    if (!isRenewal && (!hourlyRateInput.value || !isUserCustomRate)) {
                        applyNewContractWage();
                    }
                }
            }
        } catch {
            // ignore network errors
        }
        return regionalWage;
    }

    const activeCardClasses = ['border-indigo-600', 'bg-gradient-to-br', 'from-indigo-50/70', 'via-white', 'to-indigo-50/20', 'shadow-md', 'shadow-indigo-100/50', 'ring-4', 'ring-indigo-600/10'];
    const inactiveCardClasses = ['border-slate-200', 'bg-white'];

    const activeIconClasses = ['bg-indigo-600', 'text-white', 'shadow-sm', 'shadow-indigo-300', 'scale-105'];
    const inactiveIconClasses = ['bg-slate-100', 'text-slate-500'];

    const activeIndicatorClasses = ['border-indigo-600', 'bg-indigo-600', 'text-white', 'shadow-sm'];
    const inactiveIndicatorClasses = ['border-slate-300', 'bg-white', 'text-transparent'];

    // Local today string in YYYY-MM-DD
    function getTodayString() {
        const now = new Date();
        const year = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, '0');
        const day = String(now.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    const todayStr = getTodayString();
    if (start) {
        start.min = todayStr;
    }

    function addDays(dateStr, days) {
        if (!dateStr) return '';
        const [y, m, d] = dateStr.split('-').map(Number);
        const dt = new Date(Date.UTC(y, m - 1, d));
        dt.setUTCDate(dt.getUTCDate() + days);
        return dt.toISOString().slice(0, 10);
    }

    function applyClasses(el, toAdd, toRemove) {
        if (!el) return;
        toRemove.forEach(c => el.classList.remove(c));
        toAdd.forEach(c => el.classList.add(c));
    }

    function updateModeStyles() {
        const isRenewal = Boolean(radioRenewal?.checked);

        if (isRenewal) {
            applyClasses(labelRenewal, activeCardClasses, inactiveCardClasses);
            applyClasses(labelNew, inactiveCardClasses, activeCardClasses);

            applyClasses(iconBoxRenewal, activeIconClasses, inactiveIconClasses);
            applyClasses(iconBoxNew, inactiveIconClasses, activeIconClasses);

            applyClasses(indicatorRenewal, activeIndicatorClasses, inactiveIndicatorClasses);
            applyClasses(indicatorNew, inactiveIndicatorClasses, activeIndicatorClasses);
        } else {
            applyClasses(labelNew, activeCardClasses, inactiveCardClasses);
            applyClasses(labelRenewal, inactiveCardClasses, activeCardClasses);

            applyClasses(iconBoxNew, activeIconClasses, inactiveIconClasses);
            applyClasses(iconBoxRenewal, inactiveIconClasses, activeIconClasses);

            applyClasses(indicatorNew, activeIndicatorClasses, inactiveIndicatorClasses);
            applyClasses(indicatorRenewal, inactiveIndicatorClasses, activeIndicatorClasses);
        }

        if (staffSelectLabel) {
            staffSelectLabel.textContent = isRenewal ? 'Staff for Renewal / Replacement' : 'New Staff';
        }
        if (staffHelpText) {
            staffHelpText.textContent = isRenewal
                ? 'Only displaying staff belonging to your branch who previously had an employment contract.'
                : 'Only displaying staff without an active contract (including new staff or staff whose previous contract was cancelled/expired). Branch and minimum wage will follow your managed branch.';
        }
        if (button) {
            button.textContent = isRenewal ? 'Save Renewal Draft' : 'Save Draft';
        }

        if (!isRenewal) {
            if (renewalGuidanceCard) {
                renewalGuidanceCard.classList.add('hidden');
            }
        }
    }

    function applyNewContractWage() {
        if (!hourlyRateInput) return;
        if (!isUserCustomRate || !hourlyRateInput.value) {
            if (regionalWage > 0) {
                hourlyRateInput.value = regionalWage;
            }
        }
        updateWageDisplayAndValidation();
    }

    function applyRenewalContractWage(opt) {
        if (!hourlyRateInput) return;
        if (opt && opt.dataset.hourlyRateRaw) {
            if (!isUserCustomRate || !hourlyRateInput.value) {
                hourlyRateInput.value = opt.dataset.hourlyRateRaw;
            }
        } else if (!opt || !opt.value) {
            if (!isUserCustomRate) {
                hourlyRateInput.value = '';
            }
        } else {
            if (!isUserCustomRate || !hourlyRateInput.value) {
                if (regionalWage > 0) {
                    hourlyRateInput.value = regionalWage;
                }
            }
        }
        updateWageDisplayAndValidation();
    }

    function updateWageDisplayAndValidation() {
        if (!hourlyRateInput) return;
        const isRenewal = Boolean(radioRenewal?.checked);
        const rate = parseFloat(hourlyRateInput.value);
        const opt = staffSelect && staffSelect.selectedIndex >= 0 ? staffSelect.options[staffSelect.selectedIndex] : null;

        if (isRenewal) {
            const historyRate = opt && opt.dataset.hourlyRateRaw ? parseFloat(opt.dataset.hourlyRateRaw) : 0;
            if (!opt || !opt.value) {
                if (wageSourceBadge) {
                    wageSourceBadge.textContent = 'Salary history';
                    wageSourceBadge.className = 'text-[11px] text-slate-400';
                }
                if (hourlyRateGuidanceNote) {
                    hourlyRateGuidanceNote.textContent = 'Select a staff member to auto-load salary history.';
                    hourlyRateGuidanceNote.className = 'text-[11px] text-slate-500 mt-1';
                }
            } else if (historyRate > 0 && rate === historyRate && !isUserCustomRate) {
                if (wageSourceBadge) {
                    wageSourceBadge.textContent = 'Salary history';
                    wageSourceBadge.className = 'text-[11px] text-emerald-600 font-medium';
                }
                if (hourlyRateGuidanceNote) {
                    hourlyRateGuidanceNote.textContent = `Auto-filled from staff salary history (${Number(historyRate).toLocaleString('en-US')} VND/h, can be adjusted).`;
                    hourlyRateGuidanceNote.className = 'text-[11px] text-slate-500 mt-1';
                }
                hourlyRateInput.setCustomValidity('');
            } else if (regionalWage > 0 && rate === regionalWage && historyRate === 0 && !isUserCustomRate) {
                if (wageSourceBadge) {
                    wageSourceBadge.textContent = 'Regional min wage';
                    wageSourceBadge.className = 'text-[11px] text-indigo-600 font-medium';
                }
                if (hourlyRateGuidanceNote) {
                    hourlyRateGuidanceNote.textContent = `Auto-filled with regional minimum wage (${Number(regionalWage).toLocaleString('en-US')} VND/h, can be adjusted).`;
                    hourlyRateGuidanceNote.className = 'text-[11px] text-slate-500 mt-1';
                }
                hourlyRateInput.setCustomValidity('');
            } else {
                if (wageSourceBadge) {
                    wageSourceBadge.textContent = 'Custom rate';
                    wageSourceBadge.className = 'text-[11px] text-slate-600 font-medium';
                }
                if (regionalWage > 0 && rate < regionalWage) {
                    if (hourlyRateGuidanceNote) {
                        hourlyRateGuidanceNote.textContent = `⚠️ Rate cannot be lower than regional minimum wage (${Number(regionalWage).toLocaleString('en-US')} VND/h).`;
                        hourlyRateGuidanceNote.className = 'text-[11px] text-rose-600 font-medium mt-1';
                    }
                    hourlyRateInput.setCustomValidity(`Hourly rate cannot be lower than the regional minimum wage (${Number(regionalWage).toLocaleString('en-US')} VND/h).`);
                } else {
                    if (hourlyRateGuidanceNote) {
                        hourlyRateGuidanceNote.textContent = 'Manually adjusted rate (must be at or above regional minimum wage).';
                        hourlyRateGuidanceNote.className = 'text-[11px] text-slate-500 mt-1';
                    }
                    hourlyRateInput.setCustomValidity('');
                }
            }
        } else {
            if (regionalWage > 0 && rate === regionalWage && !isUserCustomRate) {
                if (wageSourceBadge) {
                    wageSourceBadge.textContent = 'Regional min wage';
                    wageSourceBadge.className = 'text-[11px] text-indigo-600 font-medium';
                }
                if (hourlyRateGuidanceNote) {
                    hourlyRateGuidanceNote.textContent = `Auto-filled with regional minimum wage (${Number(regionalWage).toLocaleString('en-US')} VND/h, can be adjusted).`;
                    hourlyRateGuidanceNote.className = 'text-[11px] text-slate-500 mt-1';
                }
                hourlyRateInput.setCustomValidity('');
            } else {
                if (wageSourceBadge) {
                    wageSourceBadge.textContent = 'Custom rate';
                    wageSourceBadge.className = 'text-[11px] text-slate-600 font-medium';
                }
                if (regionalWage > 0 && rate < regionalWage) {
                    if (hourlyRateGuidanceNote) {
                        hourlyRateGuidanceNote.textContent = `⚠️ Rate cannot be lower than regional minimum wage (${Number(regionalWage).toLocaleString('en-US')} VND/h).`;
                        hourlyRateGuidanceNote.className = 'text-[11px] text-rose-600 font-medium mt-1';
                    }
                    hourlyRateInput.setCustomValidity(`Hourly rate cannot be lower than the regional minimum wage (${Number(regionalWage).toLocaleString('en-US')} VND/h).`);
                } else {
                    if (hourlyRateGuidanceNote) {
                        hourlyRateGuidanceNote.textContent = 'Manually adjusted rate (must be at or above regional minimum wage).';
                        hourlyRateGuidanceNote.className = 'text-[11px] text-slate-500 mt-1';
                    }
                    hourlyRateInput.setCustomValidity('');
                }
            }
        }
    }

    if (hourlyRateInput) {
        hourlyRateInput.addEventListener('input', () => {
            isUserCustomRate = true;
            updateWageDisplayAndValidation();
        });
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
                el.dataset.startDate = opt.currentStartDate || '';
                el.dataset.endDate = opt.currentEndDate || '';
                el.dataset.hourlyRate = opt.currentHourlyRate != null ? Number(opt.currentHourlyRate).toLocaleString('en-US') : '';
                el.dataset.hourlyRateRaw = opt.currentHourlyRate != null ? opt.currentHourlyRate : '';
                el.dataset.contractTerms = opt.currentContractTerms || '';
                el.dataset.daysLeft = opt.daysUntilExpiration != null ? opt.daysUntilExpiration : '';
                el.dataset.category = opt.expirationCategory || '';

                if (isRenewal && opt.currentContractNumber) {
                    let badge = '';
                    if (opt.expirationCategory === 'EXPIRED') {
                        badge = `🔴 Expired (${Math.abs(opt.daysUntilExpiration || 0)}d ago)`;
                    } else if (opt.expirationCategory === 'URGENT') {
                        badge = `🔴 Expires in ${opt.daysUntilExpiration}d`;
                    } else if (opt.expirationCategory === 'UPCOMING') {
                        badge = `🟡 Expires in ${opt.daysUntilExpiration}d`;
                    } else if (opt.expirationCategory === 'LONG_TERM') {
                        badge = `🟢 Active (${opt.daysUntilExpiration}d left)`;
                    } else if (opt.expirationCategory === 'INDEFINITE') {
                        badge = '🟢 Active (Indefinite)';
                    }
                    const badgePrefix = badge ? badge + ' • ' : '';
                    const posSuffix = opt.currentPosition ? ' - ' + opt.currentPosition : '';
                    el.textContent = `${badgePrefix}${opt.fullName} (${opt.currentContractNumber}${posSuffix})`;
                } else {
                    el.textContent = opt.fullName;
                }

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

    let currentSuggestedStartDate = '';

    function onStaffSelected() {
        if (!staffSelect) return;
        const opt = staffSelect.options[staffSelect.selectedIndex];
        const isRenewal = Boolean(radioRenewal && radioRenewal.checked);

        if (opt && opt.value) {
            if (isRenewal && replacesContractInput) {
                replacesContractInput.value = opt.dataset.contractId || '';
                if (position && !position.value && opt.dataset.position) {
                    position.value = opt.dataset.position;
                }
                updateGuidanceCard(opt);
                applyRenewalContractWage(opt);
            } else {
                if (replacesContractInput) replacesContractInput.value = '';
                if (renewalGuidanceCard) renewalGuidanceCard.classList.add('hidden');
                clearStartDateGuidance();
                applyNewContractWage();
            }
        } else {
            if (replacesContractInput) replacesContractInput.value = '';
            if (renewalGuidanceCard) renewalGuidanceCard.classList.add('hidden');
            clearStartDateGuidance();
            if (isRenewal) {
                applyRenewalContractWage(null);
            } else {
                applyNewContractWage();
            }
        }
    }

    function updateGuidanceCard(opt) {
        if (!renewalGuidanceCard || !opt || !opt.dataset.contractId) {
            if (renewalGuidanceCard) renewalGuidanceCard.classList.add('hidden');
            return;
        }

        renewalGuidanceCard.classList.remove('hidden');

        const contractNum = opt.dataset.contractNumber || 'N/A';
        const pos = opt.dataset.position || 'N/A';
        const wage = opt.dataset.hourlyRate ? `${opt.dataset.hourlyRate} VND/h` : 'N/A';
        const startDate = opt.dataset.startDate || '';
        const endDate = opt.dataset.endDate || '';
        const daysLeft = opt.dataset.daysLeft !== '' ? Number.parseInt(opt.dataset.daysLeft, 10) : null;
        const category = opt.dataset.category || 'LONG_TERM';

        if (snapshotContractNumber) snapshotContractNumber.textContent = contractNum;
        if (snapshotPosition) snapshotPosition.textContent = pos;
        if (pos && position && position.tagName === 'SELECT') {
            let matched = false;
            for (let i = 0; i < position.options.length; i++) {
                if (position.options[i].value.toLowerCase() === pos.toLowerCase()) {
                    position.selectedIndex = i;
                    matched = true;
                    break;
                }
            }
            if (!matched && pos) {
                const opt = new Option(`${pos} (Current)`, pos, true, true);
                position.add(opt);
            }
        }
        if (snapshotHourlyRate) snapshotHourlyRate.textContent = wage;

        // Auto-fill Contract Terms from previous contract
        if (contractTermsInput && opt.dataset.contractTerms) {
            contractTermsInput.value = opt.dataset.contractTerms;
        }

        let termText = '';
        if (startDate && endDate) {
            termText = `${startDate} → ${endDate}`;
        } else if (startDate) {
            termText = `Since ${startDate} (Indefinite)`;
        } else {
            termText = 'Active Contract';
        }
        if (snapshotTerm) snapshotTerm.textContent = termText;

        // Calculate suggested start date
        let suggested = todayStr;
        if (endDate) {
            const nextDay = addDays(endDate, 1);
            if (category !== 'EXPIRED' && nextDay >= todayStr) {
                suggested = nextDay;
            }
        }

        currentSuggestedStartDate = suggested;

        if (btnApplySuggestedDateText) {
            btnApplySuggestedDateText.textContent = `Apply Start Date: ${suggested}`;
        }

        // Auto-fill StartDate if empty
        if (start && !start.value) {
            start.value = suggested;
        }

        // Configure Guidance Card theme and messages
        renewalGuidanceCard.className = 'rounded-2xl border transition-all duration-200 overflow-hidden';
        if (category === 'EXPIRED') {
            renewalGuidanceCard.classList.add('border-rose-300', 'bg-rose-50/50');
            setGuidanceBadge('bg-rose-100', 'text-rose-800', 'border-rose-200', '🔴 Expired', `Expired ${Math.abs(daysLeft || 0)} days ago`);
            setGuidanceIcon('error', 'bg-rose-600', 'text-white');
            setGuidanceMessage('bg-rose-100/70', 'text-rose-900',
                `The previous contract has already expired. Create an immediate renewal starting today (${todayStr}) or an upcoming date to restore active contract status.`);
        } else if (category === 'URGENT') {
            renewalGuidanceCard.classList.add('border-rose-300', 'bg-rose-50/40');
            setGuidanceBadge('bg-rose-100', 'text-rose-800', 'border-rose-200', '🔴 Urgent Renewal', `Expires in ${daysLeft} days`);
            setGuidanceIcon('warning', 'bg-rose-600', 'text-white');
            setGuidanceMessage('bg-rose-100/70', 'text-rose-900',
                `Urgent renewal needed! The current contract expires on ${endDate}. To prevent service disruption, the new contract is recommended to start seamlessly on ${suggested} (the day immediately following expiration).`);
        } else if (category === 'UPCOMING') {
            renewalGuidanceCard.classList.add('border-amber-300', 'bg-amber-50/40');
            setGuidanceBadge('bg-amber-100', 'text-amber-900', 'border-amber-200', '🟡 Renewal Window', `Expires in ${daysLeft} days`);
            setGuidanceIcon('schedule', 'bg-amber-500', 'text-white');
            setGuidanceMessage('bg-amber-100/70', 'text-amber-900',
                `Ideal 15–30 day renewal window. Preparing this renewal now provides ample time for staff review and ensures seamless continuation on ${suggested}.`);
        } else {
            // LONG_TERM or INDEFINITE
            renewalGuidanceCard.classList.add('border-emerald-200', 'bg-emerald-50/30');
            const statusLabel = category === 'INDEFINITE' ? 'Indefinite Term' : `${daysLeft} days remaining`;
            setGuidanceBadge('bg-emerald-100', 'text-emerald-900', 'border-emerald-200', '🟢 Replacement / Promotion', statusLabel);
            setGuidanceIcon('upgrade', 'bg-emerald-600', 'text-white');
            setGuidanceMessage('bg-emerald-100/70', 'text-emerald-900',
                `The current contract is still active. Creating a replacement contract allows for early role promotion, position adjustment, or wage revision. The new terms will take effect on your chosen Start Date and supersede the current contract.`);
        }

        updateStartDateGuidance();
    }

    function setGuidanceBadge(bg, text, border, iconText, detail) {
        if (!guidanceStatusBadge) return;
        guidanceStatusBadge.className = `inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold ${bg} ${text} border ${border}`;
        guidanceStatusBadge.textContent = `${iconText} • ${detail}`;
    }

    function setGuidanceIcon(iconName, bg, text) {
        if (!guidanceIcon || !guidanceIconBox) return;
        guidanceIcon.textContent = iconName;
        guidanceIconBox.className = `w-9 h-9 rounded-xl flex items-center justify-center shrink-0 shadow-sm ${bg} ${text}`;
    }

    function setGuidanceMessage(bg, text, message) {
        if (!guidanceMessageBox || !guidanceMessageText) return;
        guidanceMessageBox.className = `p-3.5 rounded-xl text-xs leading-relaxed flex items-start gap-2.5 ${bg} ${text}`;
        guidanceMessageText.textContent = message;
    }

    function clearStartDateGuidance() {
        if (startDateTip) {
            startDateTip.textContent = '';
            startDateTip.classList.add('hidden');
        }
        if (startDateGuidanceNote) {
            startDateGuidanceNote.textContent = '';
        }
    }

    function updateStartDateGuidance() {
        if (!start || !radioRenewal || !radioRenewal.checked || !staffSelect) {
            clearStartDateGuidance();
            return;
        }

        const opt = staffSelect.options[staffSelect.selectedIndex];
        if (!opt || !opt.dataset.contractId) {
            clearStartDateGuidance();
            return;
        }

        const val = start.value;
        const oldEnd = opt.dataset.endDate;

        if (!val) {
            clearStartDateGuidance();
            return;
        }

        if (startDateTip) startDateTip.classList.remove('hidden');

        if (oldEnd) {
            const nextDay = addDays(oldEnd, 1);
            if (val === nextDay) {
                if (startDateTip) {
                    startDateTip.className = 'text-[11px] font-semibold text-emerald-600 inline-flex items-center gap-1';
                    startDateTip.textContent = '✨ Seamless Continuation';
                }
                if (startDateGuidanceNote) {
                    startDateGuidanceNote.textContent = `Continuous renewal: Starts the very next day after old contract ends (${oldEnd}).`;
                }
            } else if (val <= oldEnd) {
                if (startDateTip) {
                    startDateTip.className = 'text-[11px] font-semibold text-indigo-600 inline-flex items-center gap-1';
                    startDateTip.textContent = '⚡ Mid-Term Replacement';
                }
                if (startDateGuidanceNote) {
                    startDateGuidanceNote.textContent = `Replacement mode: Will take effect on ${val} and supersede the current contract.`;
                }
            } else {
                if (startDateTip) {
                    startDateTip.className = 'text-[11px] font-semibold text-amber-600 inline-flex items-center gap-1';
                    startDateTip.textContent = '⚠️ Gap Notice';
                }
                if (startDateGuidanceNote) {
                    startDateGuidanceNote.textContent = `Notice: There is a gap between the old contract end date (${oldEnd}) and new start date (${val}).`;
                }
            }
        } else {
            if (startDateTip) {
                startDateTip.className = 'text-[11px] font-semibold text-indigo-600 inline-flex items-center gap-1';
                startDateTip.textContent = '⚡ Replacement Mode';
            }
            if (startDateGuidanceNote) {
                startDateGuidanceNote.textContent = `Will supersede the existing indefinite contract starting on ${val}.`;
            }
        }
    }

    if (btnApplySuggestedDate) {
        btnApplySuggestedDate.addEventListener('click', () => {
            if (start && currentSuggestedStartDate) {
                start.value = currentSuggestedStartDate;
                start.dispatchEvent(new Event('input', { bubbles: true }));
                start.focus();
                start.classList.add('ring-2', 'ring-emerald-500');
                setTimeout(() => start.classList.remove('ring-2', 'ring-emerald-500'), 800);
            }
        });
    }

    if (staffSelect) {
        staffSelect.addEventListener('change', onStaffSelected);
    }

    if (radioNew) {
        radioNew.addEventListener('change', () => {
            if (radioNew.checked) {
                isUserCustomRate = false;
                updateModeStyles();
                applyNewContractWage();
                void loadStaffOptions('NEW');
            }
        });
    }

    if (radioRenewal) {
        radioRenewal.addEventListener('change', () => {
            if (radioRenewal.checked) {
                isUserCustomRate = false;
                updateModeStyles();
                applyRenewalContractWage(null);
                void loadStaffOptions('RENEWAL');
            }
        });
    }

    function validate() {
        if (start && start.value && start.value < todayStr) {
            start.setCustomValidity('The contract start date cannot be in the past. It must be today or a future date.');
        } else if (start) {
            start.setCustomValidity('');
        }

        if (end && start) {
            end.setCustomValidity(end.value && start.value && end.value <= start.value ? 'End date must be later than start date.' : '');
        }
        if (position) {
            position.setCustomValidity(position.value.trim() ? '' : 'Please enter employee position.');
        }

        if (hourlyRateInput && regionalWage > 0) {
            const rate = parseFloat(hourlyRateInput.value);
            if (!rate || isNaN(rate)) {
                hourlyRateInput.setCustomValidity('Please enter an hourly rate.');
            } else if (rate < regionalWage) {
                hourlyRateInput.setCustomValidity(`Hourly rate cannot be lower than the regional minimum wage (${Number(regionalWage).toLocaleString('en-US')} VND/h).`);
            } else {
                hourlyRateInput.setCustomValidity('');
            }
        }
    }

    [start, end, position].filter(Boolean).forEach(input => input.addEventListener('input', () => {
        validate();
        updateStartDateGuidance();
    }));

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
    void ensureRegionalWage().then(() => {
        updateModeStyles();
        onStaffSelected();
    });
})();

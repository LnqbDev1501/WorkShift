(() => {
    'use strict';
    const get = id => document.getElementById(id);
    const form = get('template-form');
    if (!form) return;
    const name = get('ShiftName'), description = get('Description');
    const start = get('StartTime'), end = get('EndTime'), rest = get('BreakMinutes');
    const submit = get('create-submit');
    const submitLabel = submit.textContent;
    const parseTime = value => {
        if (!/^([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$/.test(value)) return null;
        const [h, m, s] = value.split(':').map(Number);
        return h * 3600 + m * 60 + s;
    };
    const duration = seconds => `${Math.floor(seconds / 3600)}h ${String(Math.floor(seconds % 3600 / 60)).padStart(2, '0')}m${seconds % 60 ? ` ${seconds % 60}s` : ''}`;
    const minutes = seconds => `${Number((seconds / 60).toFixed(2))} mins`;
    function rule(id, valid, label) {
        get(id).textContent = `${valid ? '✓' : '✕'} ${label}`;
        get(id).className = valid ? 'valid' : 'invalid';
    }
    function update() {
        get('name-count').textContent = `${name.value.length} / 150`;
        get('description-count').textContent = `${description.value.length} / 500`;
        name.setCustomValidity(name.value.trim() ? '' : 'Shift name is required.');
        const a = parseTime(start.value), b = parseTime(end.value);
        const validTimes = a !== null && b !== null;
        const sameDay = validTimes && b > a;
        const breakValue = Number(rest.value);
        const validBreak = /^[0-9]+$/.test(rest.value) && Number.isSafeInteger(breakValue) && breakValue <= 2147483647;
        const fits = sameDay && validBreak && breakValue * 60 < b - a;
        start.setCustomValidity(a === null ? 'Use 24-hour HH:mm:ss format.' : '');
        end.setCustomValidity(b === null ? 'Use 24-hour HH:mm:ss format.' : validTimes && !sameDay ? 'End time must be later than start time on the same day.' : '');
        rest.setCustomValidity(!validBreak ? 'Enter a non-negative whole number.' : sameDay && !fits ? 'Break duration must be shorter than the shift.' : '');
        const valid = sameDay && fits;
        const total = sameDay ? b - a : 0;
        const net = valid ? total - breakValue * 60 : 0;
        const ratio = valid ? net / total * 100 : 0;
        get('total-duration').textContent = sameDay ? duration(total) : '—';
        get('total-minutes').textContent = sameDay ? `${minutes(total)} total` : 'Invalid interval';
        get('break-duration').textContent = validBreak ? `${breakValue}m` : '—';
        get('net-duration').textContent = valid ? duration(net) : '—';
        get('net-minutes').textContent = valid ? `${minutes(net)} working` : 'Calculation paused';
        get('ratio').textContent = valid ? `${ratio.toFixed(1)}% Work / ${(100 - ratio).toFixed(1)}% Rest` : 'Invalid configuration';
        get('density').textContent = valid ? `${ratio.toFixed(1)}%` : '—';
        get('density-ring').style.setProperty('--density', `${ratio}%`);
        get('work-bar').style.width = `${ratio}%`;
        get('break-bar').style.width = valid ? `${100 - ratio}%` : '0%';
        get('preview-status').textContent = valid ? '● Valid Timing' : '● Check Timing';
        get('preview-status').className = `shift-status ${valid ? 'is-active' : 'is-inactive'}`;
        rule('rule-time', validTimes, 'Valid 24-hour times (HH:mm:ss)');
        rule('rule-day', sameDay, 'End time later than start; no overnight shifts');
        rule('rule-break', fits, 'Whole-number break shorter than the shift');
        get('timing-error').hidden = valid;
        get('timing-error').textContent = !validTimes ? 'Enter start and end times in HH:mm:ss format.' : !sameDay ? 'End time must be later than start time on the same day. Overnight shifts are not supported.' : !validBreak ? 'Break minutes must be a non-negative whole number.' : !fits ? 'Break duration must be shorter than the shift window.' : '';
    }
    form.addEventListener('input', update);
    form.addEventListener('submit', event => {
        update();
        if (!form.reportValidity() || (window.jQuery && !window.jQuery(form).valid())) {
            event.preventDefault();
            return;
        }
        submit.disabled = true;
        submit.textContent = form.dataset.edit === 'true' ? 'Saving Changes…' : 'Creating Template…';
        form.setAttribute('aria-busy', 'true');
    });
    window.addEventListener('pageshow', () => {
        submit.disabled = false;
        submit.textContent = submitLabel;
        form.removeAttribute('aria-busy');
        update();
    });
    update();
})();

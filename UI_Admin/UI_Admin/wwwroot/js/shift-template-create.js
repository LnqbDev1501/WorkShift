(() => {
    'use strict';
    const get = id => document.getElementById(id);
    const form = get('template-form');
    if (!form) return;
    const name = get('ShiftName'), description = get('Description');
    const start = get('StartTime'), end = get('EndTime');
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
        start.setCustomValidity(a === null ? 'Use 24-hour HH:mm:ss format.' : '');
        end.setCustomValidity(b === null ? 'Use 24-hour HH:mm:ss format.' : validTimes && !sameDay ? 'End time must be later than start time on the same day.' : '');
        const valid = sameDay;
        const total = sameDay ? b - a : 0;
        get('total-duration').textContent = sameDay ? duration(total) : '—';
        get('total-minutes').textContent = sameDay ? `${minutes(total)} total` : 'Invalid interval';
        get('ratio').textContent = valid ? duration(total) : 'Invalid configuration';
        get('work-bar').style.width = valid ? '100%' : '0%';
        get('preview-status').textContent = valid ? '● Valid Timing' : '● Check Timing';
        get('preview-status').className = `shift-status ${valid ? 'is-active' : 'is-inactive'}`;
        rule('rule-time', validTimes, 'Valid 24-hour times (HH:mm:ss)');
        rule('rule-day', sameDay, 'End time later than start; no overnight shifts');
        get('timing-error').hidden = valid;
        get('timing-error').textContent = !validTimes ? 'Enter start and end times in HH:mm:ss format.' : !sameDay ? 'End time must be later than start time on the same day. Overnight shifts are not supported.' : '';
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

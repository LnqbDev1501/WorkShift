(() => {
    'use strict';
    const inputs = ['start-time', 'end-time', 'StartTime', 'EndTime'].map(id => document.getElementById(id)).filter(Boolean);
    if (!inputs.length) return;
    const dialog = document.createElement('dialog');
    dialog.className = 'shift-clock-dialog';
    dialog.id = 'shift-clock-dialog';
    dialog.setAttribute('aria-labelledby', 'shift-clock-title');
    dialog.innerHTML = `<h2 id="shift-clock-title">Choose time · 24h</h2>
        <p class="shift-clock-context"></p>
        <div class="shift-clock-values"><label>Hour<select aria-label="Hour (00–23)"></select></label><span>:</span><label>Minute<select aria-label="Minute (00–59)"></select></label></div>
        <p class="shift-clock-hint" aria-live="polite"></p>
        <div class="shift-clock-face" role="group" aria-label="Clock dial"></div>
        <div class="shift-clock-actions"><button type="button" data-action="clear">Clear</button><button type="button" data-action="cancel">Cancel</button><button type="button" data-action="apply" class="shift-primary">Set time</button></div>`;
    document.body.append(dialog);
    const [hours, minutes] = dialog.querySelectorAll('select');
    const face = dialog.querySelector('.shift-clock-face');
    const pad = number => String(number).padStart(2, '0');
    [hours, minutes].forEach((select, index) => {
        for (let n = 0; n < (index === 0 ? 24 : 60); n++) select.add(new Option(pad(n), pad(n)));
    });
    let target, opener, seconds = '00', mode = 'hours';
    function render() {
        face.replaceChildren();
        dialog.querySelector('.shift-clock-hint').textContent = mode === 'hours' ? 'Select an hour · outer ring 00–11, inner ring 12–23' : 'Select minutes · use the dropdown for any minute';
        face.setAttribute('aria-label', mode === 'hours' ? '24-hour clock dial' : 'Minute clock dial');
        const value = Number(mode === 'hours' ? hours.value : minutes.value);
        const angle = (mode === 'hours' ? value % 12 : value / 5) * 30;
        const hand = document.createElement('span');
        hand.className = 'shift-clock-hand';
        hand.setAttribute('aria-hidden', 'true');
        hand.style.height = mode === 'hours' && value >= 12 ? '65px' : '104px';
        hand.style.transform = `rotate(${angle}deg)`;
        face.append(hand);
        const count = mode === 'hours' ? 24 : 12;
        for (let n = 0; n < count; n++) {
            const number = mode === 'hours' ? n : n * 5;
            const radians = (n % 12) * Math.PI / 6;
            const radius = mode === 'hours' && n >= 12 ? 65 : 104;
            const button = document.createElement('button');
            button.type = 'button';
            button.textContent = pad(number);
            button.style.left = `calc(50% + ${Math.sin(radians) * radius}px)`;
            button.style.top = `calc(50% - ${Math.cos(radians) * radius}px)`;
            button.setAttribute('aria-label', `${pad(number)} ${mode === 'hours' ? 'hours' : 'minutes'}`);
            button.setAttribute('aria-pressed', String(number === value));
            button.addEventListener('click', () => {
                if (mode === 'hours') { hours.value = pad(number); mode = 'minutes'; render(); minutes.focus(); }
                else { minutes.value = pad(number); render(); dialog.querySelector('[data-action="apply"]').focus(); }
            });
            face.append(button);
        }
    }
    hours.addEventListener('focus', () => { mode = 'hours'; render(); });
    minutes.addEventListener('focus', () => { mode = 'minutes'; render(); });
    hours.addEventListener('change', render);
    minutes.addEventListener('change', render);
    inputs.forEach(input => {
        const isStart = input.id === 'start-time' || input.id === 'StartTime';
        const isCreate = input.id === 'StartTime' || input.id === 'EndTime';
        const wrapper = document.createElement('div');
        wrapper.className = 'shift-clock-input';
        input.before(wrapper);
        wrapper.append(input);
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'shift-clock-trigger';
        button.setAttribute('aria-label', `Choose ${isStart ? 'start' : 'end'} time (24h)`);
        button.setAttribute('aria-haspopup', 'dialog');
        button.setAttribute('aria-controls', dialog.id);
        button.innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="M12 6v6l4 2"/></svg>';
        wrapper.append(button);
        button.addEventListener('click', () => {
            target = input; opener = button;
            const match = /^([01]\d|2[0-3]):([0-5]\d)(?::([0-5]\d))?$/.exec(input.value);
            hours.value = match ? match[1] : '00';
            minutes.value = match ? match[2] : '00';
            seconds = match?.[3] ?? '00';
            mode = 'hours';
            dialog.querySelector('.shift-clock-context').textContent = isCreate
                ? `${isStart ? 'Start' : 'End'} Time · HH:mm:ss`
                : isStart ? 'Starts at or after' : 'Ends at or before';
            render(); dialog.showModal(); hours.focus();
        });
    });
    dialog.querySelectorAll('[data-action]').forEach(button => button.addEventListener('click', () => {
        if (button.dataset.action !== 'cancel') {
            const suffix = target.id === 'StartTime' || target.id === 'EndTime' ? `:${seconds}` : '';
            target.value = button.dataset.action === 'clear' ? '' : `${hours.value}:${minutes.value}${suffix}`;
            target.dispatchEvent(new Event('input', { bubbles: true }));
            target.dispatchEvent(new Event('change', { bubbles: true }));
        }
        dialog.close();
    }));
    dialog.addEventListener('close', () => opener?.focus());
    dialog.addEventListener('click', event => {
        if (event.target !== dialog) return;
        const box = dialog.getBoundingClientRect();
        if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) dialog.close();
    });
})();

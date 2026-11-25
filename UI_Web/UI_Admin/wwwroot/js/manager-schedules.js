(() => {
    const form = document.getElementById('schedule-editor');
    if (!form) return;
    const dateLabel = date => new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(date);
    const update = () => {
        const startValue = form.querySelector('[name="WeekStartDate"]').value;
        const start = startValue ? new Date(`${startValue}T00:00:00Z`) : null;
        const target = document.querySelector('[data-week-preview]');
        if (start && !isNaN(start)) { const end = new Date(start); end.setUTCDate(end.getUTCDate() + 6); target.textContent = `${dateLabel(start)} – ${dateLabel(end)}`; }
        else target.textContent = 'Choose your week';
        const deadlineValue = form.querySelector('[name="RegistrationDeadline"]').value;
        const deadline = deadlineValue ? new Date(`${deadlineValue}Z`) : null;
        document.querySelector('[data-deadline-preview]').textContent = deadline && !isNaN(deadline) ? `${dateLabel(deadline)}, ${deadline.toISOString().slice(11, 16)}` : 'Not set';
    };
    form.addEventListener('input', update); form.addEventListener('change', update); update();
})();

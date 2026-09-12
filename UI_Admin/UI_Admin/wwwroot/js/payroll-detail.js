(() => {
    document.querySelectorAll('[data-inspect]').forEach(button => {
        button.addEventListener('click', () => {
            const panel = document.getElementById(button.dataset.inspect);
            panel.hidden = !panel.hidden;
            button.setAttribute('aria-expanded', String(!panel.hidden));
        });
    });
    document.querySelectorAll('[data-reject]').forEach(button => {
        button.addEventListener('click', () => {
            const form = document.getElementById(button.dataset.reject);
            form.hidden = !form.hidden;
            button.setAttribute('aria-expanded', String(!form.hidden));
            if (!form.hidden) form.querySelector('textarea').focus();
        });
    });
    document.querySelectorAll('[data-cancel]').forEach(button => {
        button.addEventListener('click', () => {
            document.getElementById(button.dataset.cancel).hidden = true;
            const toggle = document.querySelector(`[data-reject="${button.dataset.cancel}"]`);
            toggle.setAttribute('aria-expanded', 'false');
            toggle.focus();
        });
    });
    document.querySelectorAll('form[method="post"]').forEach(form => {
        form.addEventListener('submit', event => {
            const reason = form.querySelector('textarea[name="Reason"]');
            if (reason && !reason.value.trim()) {
                event.preventDefault();
                reason.setCustomValidity('Vui lòng nhập lý do yêu cầu chỉnh sửa.');
                reason.reportValidity();
                reason.addEventListener('input', () => reason.setCustomValidity(''), { once: true });
                return;
            }
            if (form.dataset.submitting) { event.preventDefault(); return; }
            form.dataset.submitting = 'true';
            form.querySelectorAll('button[type="submit"]').forEach(button => {
                button.disabled = true;
                button.textContent = 'Đang xử lý…';
            });
        });
    });
})();

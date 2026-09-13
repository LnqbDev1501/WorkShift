document.addEventListener('DOMContentLoaded', function () {
    var deleteModal = document.getElementById('holiday-delete-modal');
    var successModal = document.getElementById('holiday-success-modal');
    var lastTrigger = null;

    function setModalOpen(modal, isOpen) {
        if (!modal) return;
        modal.hidden = !isOpen;
        document.body.classList.toggle('holiday-modal-open', isOpen);
        if (!isOpen && lastTrigger) lastTrigger.focus();
    }

    document.querySelectorAll('[data-delete-open]').forEach(function (button) {
        button.addEventListener('click', function () {
            lastTrigger = button;
            var id = button.dataset.id;
            var name = button.dataset.name || 'this public holiday';
            var idInput = document.getElementById('delete-holiday-id');
            var title = document.getElementById('delete-modal-title');
            var description = document.getElementById('delete-modal-description');
            if (idInput) idInput.value = id;
            if (title) title.textContent = 'Deactivate ' + name + '?';
            if (description) description.textContent = 'Are you sure you want to deactivate "' + name + '"? Only this holiday will become inactive. Its date remains reserved.';
            setModalOpen(deleteModal, true);
            deleteModal.querySelector('[data-delete-close]')?.focus();
        });
    });

    document.querySelectorAll('[data-delete-close]').forEach(function (button) {
        button.addEventListener('click', function () { setModalOpen(deleteModal, false); });
    });
    document.querySelectorAll('[data-success-close]').forEach(function (button) {
        button.addEventListener('click', function () { setModalOpen(successModal, false); });
    });
    [deleteModal, successModal].forEach(function (modal) {
        if (!modal) return;
        modal.addEventListener('click', function (event) {
            if (event.target === modal) setModalOpen(modal, false);
        });
    });
    document.addEventListener('keydown', function (event) {
        if (event.key !== 'Escape') return;
        if (deleteModal && !deleteModal.hidden) setModalOpen(deleteModal, false);
        else if (successModal && !successModal.hidden) setModalOpen(successModal, false);
    });
    if (successModal) document.body.classList.add('holiday-modal-open');
});

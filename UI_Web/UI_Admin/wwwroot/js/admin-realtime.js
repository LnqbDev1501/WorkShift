(function () {
    'use strict';
    const configElement = document.getElementById('admin-realtime-config');
    if (!configElement || !window.signalR || window.workshiftRealtime) return;

    const config = JSON.parse(configElement.textContent);
    const dirtyFields = new Set();
    let stopped = false;
    let pending = false;
    let submitting = false;
    let timer;
    let retryTimer;
    let banner;
    let starting;
    let connectedBefore = false;
    const connection = new window.signalR.HubConnectionBuilder()
        .withUrl(config.apiBaseUrl + '/hubs/admin-realtime', {
            transport: window.signalR.HttpTransportType.WebSockets | window.signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .build();
    window.workshiftRealtime = connection;

    function hasUnsavedInput() {
        return [...dirtyFields].some(field => field.isConnected);
    }

    function hasOpenDialog() {
        return [...document.querySelectorAll('dialog[open], [role="dialog"], .modal.show, [id$="Modal"], [id$="modal"]')]
            .some(element => element.getClientRects().length && getComputedStyle(element).visibility !== 'hidden');
    }

    function showUpdate() {
        if (banner) return;
        banner = document.createElement('div');
        banner.className = 'ws-realtime-update';
        banner.setAttribute('role', 'status');
        const message = document.createElement('span');
        message.textContent = 'New data is available. Your current input has been kept.';
        const refreshButton = document.createElement('button');
        refreshButton.type = 'button';
        refreshButton.textContent = 'Refresh';
        refreshButton.addEventListener('click', () => {
            if (hasUnsavedInput() && !window.confirm('Refresh and discard your unsaved changes?')) return;
            // A POST response must never be reloaded: it could repeat the mutation.
            window.location.assign(config.refreshUrl || window.location.pathname);
        });
        banner.append(message, refreshButton);
        document.body.append(banner);
    }

    function refresh() {
        clearTimeout(timer);
        timer = setTimeout(() => {
            if (stopped || !pending) return;
            if (document.hidden || submitting) {
                refresh();
                return;
            }
            if (!config.canReload || hasUnsavedInput() || hasOpenDialog()) {
                showUpdate();
                refresh();
                return;
            }
            pending = false;
            // MVC re-renders metrics, charts and tables together, preserving the URL and filters.
            window.location.reload();
        }, 700);
    }

    function invalidate() {
        pending = true;
        refresh();
    }

    function trackInput(event) {
        if (event.target.matches('input:not([type="hidden"]), select, textarea, [contenteditable="true"]')) {
            dirtyFields.add(event.target);
        }
    }
    document.addEventListener('input', trackInput);
    document.addEventListener('change', trackInput);
    document.addEventListener('reset', event => {
        for (const field of dirtyFields) {
            if (field.form === event.target) dirtyFields.delete(field);
        }
        if (pending) refresh();
    });
    document.addEventListener('submit', event => {
        submitting = true;
        // Native form navigation stays protected until the next document loads.
        // Let AJAX handlers and forms targeting another tab continue to receive updates.
        queueMicrotask(() => {
            submitting = !event.defaultPrevented && (!event.target.target || event.target.target === '_self');
            if (pending) refresh();
        });
    });
    connection.on('AdminDataChanged', (data) => {
        const customEvent = new CustomEvent('workshift:admin-data-changed', {
            detail: data,
            cancelable: true
        });
        const defaultPrevented = !window.dispatchEvent(customEvent);
        if (defaultPrevented) return;
        invalidate();
    });
    connection.onreconnected(invalidate);

    function scheduleRetry() {
        clearTimeout(retryTimer);
        if (!stopped) retryTimer = setTimeout(start, 5000);
    }

    async function start() {
        if (stopped || starting || connection.state !== window.signalR.HubConnectionState.Disconnected) return;
        starting = (async () => {
            try {
                await connection.start();
                // Resynchronize changes missed while the connection was offline.
                if (connectedBefore) invalidate();
                connectedBefore = true;
            } catch (error) {
                console.warn('[WorkShift realtime] Connection failed; retrying.', error);
                scheduleRetry();
            } finally {
                starting = null;
            }
        })();
        await starting;
    }
    connection.onclose(scheduleRetry);
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden && pending) refresh();
    });
    let stopping = Promise.resolve();
    window.addEventListener('pagehide', () => {
        stopped = true;
        clearTimeout(timer);
        clearTimeout(retryTimer);
        stopping = connection.stop();
    });
    window.addEventListener('pageshow', async event => {
        if (!event.persisted) return;
        await stopping;
        stopped = false;
        submitting = false;
        await start();
    });
    start();
})();

(function () {
    'use strict';
    const configElement = document.getElementById('admin-realtime-config');
    if (!configElement || !window.signalR) return;
    const signalR = window.signalR;
    const config = JSON.parse(configElement.textContent);
    let stopped = false;
    let pending = false;
    let timer;
    let retryTimer;
    let submitting = false;
    const dirtyForms = new Set();

    // Detail pages contain action forms; do not interrupt a reason being entered.
    document.addEventListener('input', function (event) {
        const form = event.target.closest('form');
        if (form && form.method.toLowerCase() !== 'get') dirtyForms.add(form);
    });
    document.addEventListener('reset', event => {
        dirtyForms.delete(event.target);
    });
    document.addEventListener('submit', () => {
        submitting = true;
        setTimeout(() => { submitting = false; if (pending) refresh(); }, 3000);
    });

    function refresh() {
        clearTimeout(timer);
        timer = setTimeout(function () {
            if (stopped || !pending) return;
            if (submitting || document.hidden || [...dirtyForms].some(form => form.isConnected && form.getClientRects().length)) {
                timer = setTimeout(refresh, 1000);
                return;
            }
            pending = false;
            // Reload through MVC to preserve filters, pagination, authorization and
            // initialize page scripts against the new data (including deleted records).
            window.location.reload();
        }, 400);
    }

    function invalidate() { pending = true; refresh(); }
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(config.apiBaseUrl + '/hubs/admin-realtime', {
            transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .build();

    connection.on('AdminDataChanged', invalidate);

    function scheduleRetry() {
        clearTimeout(retryTimer);
        if (!stopped) retryTimer = setTimeout(start, 5000);
    }

    async function start() {
        if (stopped) return;
        try {
            await connection.start();
        } catch (error) {
            console.warn('[Admin realtime] Connection failed; retrying.', error);
            await connection.stop();
            scheduleRetry();
        }
    }
    connection.onreconnected(invalidate);
    connection.onclose(scheduleRetry);
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden && pending) refresh();
    });
    window.addEventListener('pagehide', () => {
        stopped = true;
        clearTimeout(timer);
        clearTimeout(retryTimer);
        connection.stop();
    });
    start();
})();

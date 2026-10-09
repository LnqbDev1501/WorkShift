(() => {
    const chart = document.querySelector('.ad-salary-chart');
    if (!chart) return;
    const svg = chart.querySelector('svg');
    const currentMonth = Number(chart.dataset.currentMonth);
    const rows = [...chart.querySelectorAll('[data-salary-month]')];
    const tabs = [...chart.querySelectorAll('[data-metric]')];
    const money = value => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value);
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const labels = { GrossSalary: 'Gross Salary', TotalDeduction: 'Deductions', NetSalary: 'Net Salary' };
    const keys = { GrossSalary: 'grossSalary', TotalDeduction: 'totalDeduction', NetSalary: 'netSalary' };
    const colors = { GrossSalary: '#4f46e5', TotalDeduction: '#b77912', NetSalary: '#0f8a70' };
    const tints = { GrossSalary: '#efedff', TotalDeduction: '#fff5df', NetSalary: '#e7f6ef' };
    const element = (name, attributes, text) => {
        const node = document.createElementNS('http://www.w3.org/2000/svg', name);
        Object.entries(attributes).forEach(([key, value]) => node.setAttribute(key, value));
        if (text !== undefined) node.textContent = text;
        return node;
    };
    function render(metric) {
        const values = rows.map(row => Number(row.dataset[keys[metric]]));
        const color = colors[metric];
        chart.style.setProperty('--chart-color', color);
        chart.style.setProperty('--chart-tint', tints[metric]);
        const low = Math.min(0, ...values);
        const high = Math.max(1, ...values);
        const minimum = low < 0 ? low - (high - low) * .1 : 0;
        const maximum = high * 1.15;
        const x = i => 88 + i * 76;
        const y = value => 245 - (value - minimum) / (maximum - minimum) * 215;
        svg.replaceChildren();
        svg.append(element('title', {}, `${labels[metric]} across 12 months. Current month: ${currentMonth}.`));
        const defs = element('defs', {});
        const gradient = element('linearGradient', { id: 'ad-salary-gradient', x1: '0', y1: '0', x2: '0', y2: '1' });
        gradient.append(element('stop', { offset: '0%', 'stop-color': color, 'stop-opacity': '.18' }), element('stop', { offset: '100%', 'stop-color': color, 'stop-opacity': '.01' }));
        defs.append(gradient); svg.append(defs);
        const currentX = x(currentMonth - 1);
        svg.append(element('rect', { x: currentX - 23, y: 22, width: 46, height: 230, rx: 8, fill: color, opacity: '.06' }));
        for (let i = 0; i <= 4; i++) {
            const value = minimum + (maximum - minimum) * i / 4;
            const height = y(value);
            svg.append(element('line', { x1: 88, x2: 924, y1: height, y2: height, stroke: '#e7e8f0' }));
            svg.append(element('text', { x: 74, y: height + 4, fill: '#687086', 'text-anchor': 'end', 'font-size': 11 }, new Intl.NumberFormat('en-US', { notation: 'compact', maximumFractionDigits: 1 }).format(value)));
        }
        const points = values.map((value, i) => `${x(i)},${y(value)}`);
        svg.append(element('path', { d: `M ${x(0)},${y(0)} L ${points.join(' L ')} L ${x(11)},${y(0)} Z`, fill: 'url(#ad-salary-gradient)' }));
        svg.append(element('polyline', { points: points.join(' '), fill: 'none', stroke: color, 'stroke-width': 2.5, 'stroke-linejoin': 'round' }));
        values.forEach((value, i) => {
            const month = i + 1;
            svg.append(element('text', { x: x(i), y: 280, fill: month === currentMonth ? color : '#687086', 'text-anchor': 'middle', 'font-size': 12, 'font-weight': month === currentMonth ? 700 : 400 }, months[i]));
            if (month === currentMonth) svg.append(element('circle', { cx: x(i), cy: y(value), r: 12, fill: color, opacity: '.12', 'pointer-events': 'none' }));
            const point = element('circle', { cx: x(i), cy: y(value), r: month === currentMonth ? 6 : 4, fill: month === currentMonth ? color : '#fff', stroke: color, 'stroke-width': 2, tabindex: 0, role: 'img', 'aria-label': `Month ${month}: ${money(value)}, ${rows[i].dataset.count} payrolls${month === currentMonth ? ', current month' : ''}` });
            point.append(element('title', {}, `Month ${month} · ${labels[metric]}: ${money(value)} · ${rows[i].dataset.count} payrolls`));
            point.addEventListener('focus', () => show(month, value));
            point.addEventListener('mouseenter', () => show(month, value));
            point.addEventListener('blur', () => show(currentMonth, values[currentMonth - 1]));
            point.addEventListener('mouseleave', () => show(currentMonth, values[currentMonth - 1]));
            svg.append(point);
        });
        function show(month, value) {
            chart.querySelector('[data-chart-total]').textContent = money(value);
            chart.querySelector('[data-chart-caption]').textContent = `${labels[metric]} · Month ${month}${month === currentMonth ? ' (current)' : ''} · ${rows[month - 1].dataset.count} payrolls`;
        }
        show(currentMonth, values[currentMonth - 1]);
        svg.setAttribute('aria-label', `${labels[metric]} by month. Current month ${currentMonth}: ${money(values[currentMonth - 1])}. Detailed data is available in the table below.`);
        tabs.forEach(tab => tab.setAttribute('aria-pressed', String(tab.dataset.metric === metric)));
    }
    tabs.forEach(tab => tab.addEventListener('click', () => render(tab.dataset.metric)));
    render('NetSalary');
})();

export function money(value) {
    const amount = Number(value || 0);
    return new Intl.NumberFormat("it-IT", { style: "currency", currency: "EUR" }).format(amount);
}

export function statusClass(value) {
    return `status-${String(value || "").toLowerCase()}`;
}

export function safeText(value) {
    return value ?? "-";
}

export function relativeTime(value) {
    if (!value) {
        return "-";
    }

    return new Date(value).toLocaleString("it-IT");
}

export function percentage(value, total) {
    if (!total) {
        return 0;
    }

    return Math.max(0, Math.min(100, Math.round((value / total) * 100)));
}

export function createOptions(items, selectedValue, valueSelector, labelSelector) {
    return items.map(item => {
        const value = valueSelector(item);
        const selected = value === selectedValue ? "selected" : "";
        return `<option value="${value}" ${selected}>${labelSelector(item)}</option>`;
    }).join("");
}

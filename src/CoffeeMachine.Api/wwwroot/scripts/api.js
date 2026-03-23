const headers = {
    "Content-Type": "application/json"
};

async function request(path, options = {}) {
    const response = await fetch(path, {
        headers,
        ...options
    });

    const contentType = response.headers.get("content-type") || "";
    const payload = contentType.includes("application/json")
        ? await response.json()
        : await response.text();

    if (!response.ok) {
        const message = typeof payload === "string"
            ? payload
            : payload?.error || "Request failed";
        throw new Error(message);
    }

    return payload;
}

export const api = {
    getMachineStatus: () => request("/api/machine/status"),
    getMachineDiagnostics: () => request("/api/machine/diagnostics"),
    getMachineComponents: () => request("/api/machine/components"),
    powerOn: () => request("/api/machine/power/on", { method: "POST" }),
    powerOff: () => request("/api/machine/power/off", { method: "POST" }),
    resetMachine: () => request("/api/machine/reset", { method: "POST" }),
    resetMaintenance: () => request("/api/machine/maintenance/reset", { method: "POST" }),
    getCredit: () => request("/api/credit"),
    addCredit: body => request("/api/credit/add", { method: "POST", body: JSON.stringify(body) }),
    resetCredit: () => request("/api/credit/reset", { method: "POST" }),
    getTransactions: () => request("/api/transactions"),
    getProducts: () => request("/api/products"),
    getProduct: id => request(`/api/products/${id}`),
    createProduct: body => request("/api/products", { method: "POST", body: JSON.stringify(body) }),
    updateProduct: (id, body) => request(`/api/products/${id}`, { method: "PUT", body: JSON.stringify(body) }),
    deleteProduct: id => request(`/api/products/${id}`, { method: "DELETE" }),
    getRecipes: () => request("/api/recipes"),
    getRecipe: id => request(`/api/recipes/${id}`),
    createRecipe: body => request("/api/recipes", { method: "POST", body: JSON.stringify(body) }),
    updateRecipe: (id, body) => request(`/api/recipes/${id}`, { method: "PUT", body: JSON.stringify(body) }),
    deleteRecipe: id => request(`/api/recipes/${id}`, { method: "DELETE" }),
    getIngredients: () => request("/api/ingredients"),
    getIngredient: id => request(`/api/ingredients/${id}`),
    updateIngredient: (id, body) => request(`/api/ingredients/${id}`, { method: "PUT", body: JSON.stringify(body) }),
    refillIngredient: (id, quantity) => request(`/api/ingredients/${id}/refill`, { method: "POST", body: JSON.stringify({ quantity }) }),
    getTankStatus: () => request("/api/tanks/status"),
    refillWater: quantity => request("/api/water/refill", { method: "POST", body: JSON.stringify({ quantity }) }),
    getOrders: () => request("/api/orders"),
    createOrder: body => request("/api/orders", { method: "POST", body: JSON.stringify(body) }),
    cancelOrder: id => request(`/api/orders/${id}/cancel`, { method: "POST" }),
    getConfig: () => request("/api/config"),
    updateConfig: body => request("/api/config", { method: "PUT", body: JSON.stringify(body) }),
    getSimulationConfig: () => request("/api/config/simulation"),
    updateSimulationConfig: body => request("/api/config/simulation", { method: "PUT", body: JSON.stringify(body) }),
    getRecentLogs: lines => request(`/api/logs/recent?lines=${lines}`),
    getMaintenanceStatus: () => request("/api/maintenance/status"),
    getMetrics: () => request("/api/metrics"),
    injectFault: body => request("/api/faults/inject", { method: "POST", body: JSON.stringify(body) }),
    saveState: () => request("/api/state/save", { method: "POST" }),
    reloadState: () => request("/api/state/reload", { method: "POST" }),
    exportState: () => request("/api/state/export")
};

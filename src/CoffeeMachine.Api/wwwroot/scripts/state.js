const listeners = new Set();

export const state = {
    machineStatus: null,
    machineComponents: null,
    diagnostics: null,
    credit: null,
    transactions: [],
    products: [],
    recipes: [],
    ingredients: [],
    tank: null,
    orders: [],
    config: null,
    simulationConfig: null,
    maintenance: null,
    metrics: null,
    recentLogs: [],
    connectionStatus: "connecting",
    eventFeed: [],
    dispensedItem: null,
    dashboardKeypadInput: ""
};

export function subscribe(listener) {
    listeners.add(listener);
    return () => listeners.delete(listener);
}

export function setState(patch) {
    Object.assign(state, patch);
    listeners.forEach(listener => listener(state, patch));
}

export function pushEvent(event) {
    state.eventFeed.unshift({
        id: crypto.randomUUID(),
        at: new Date().toISOString(),
        ...event
    });
    state.eventFeed = state.eventFeed.slice(0, 100);
    listeners.forEach(listener => listener(state, { eventFeed: state.eventFeed }));
}

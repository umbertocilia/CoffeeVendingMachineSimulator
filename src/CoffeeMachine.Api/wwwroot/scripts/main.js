import { api } from "./api.js";
import { renderDashboard } from "./dashboard.js";
import { connectRealtime } from "./realtime.js";
import { setState as applyState, state, subscribe, pushEvent } from "./state.js";
import { bindNavigation, bindSidebarToggle, closeSidebar, showToast } from "./ui.js";
import {
    renderConfig,
    renderDiagnostics,
    renderIngredients,
    renderMachine,
    renderMaintenance,
    renderOrders,
    renderProducts,
    renderPurchase,
    renderRecipes
} from "./views.js";

const roots = {
    dashboard: document.getElementById("view-dashboard"),
    purchase: document.getElementById("view-purchase"),
    machine: document.getElementById("view-machine"),
    ingredients: document.getElementById("view-ingredients"),
    recipes: document.getElementById("view-recipes"),
    products: document.getElementById("view-products"),
    orders: document.getElementById("view-orders"),
    diagnostics: document.getElementById("view-diagnostics"),
    maintenance: document.getElementById("view-maintenance"),
    config: document.getElementById("view-config")
};

const machineStatusChip = document.getElementById("machine-status-chip");
const machineName = document.getElementById("machine-name");
const headerCredit = document.getElementById("header-credit");
const headerTemperature = document.getElementById("header-temperature");
const connectionStatus = document.getElementById("connection-status");
const terminalOrderStatuses = ["Completed", "Failed", "Cancelled"];
let activeSection = "dashboard";

subscribe(renderApp);
bindNavigation(section => {
    activeSection = section;
    renderCurrentView(state);
    closeSidebar();
});
bindSidebarToggle();
document.body.addEventListener("click", handleAction);
document.body.addEventListener("submit", handleSubmit);

await bootstrap();

async function bootstrap() {
    try {
        await refreshAll();
        await connectRealtime(handleRealtimeEvent);
        window.setInterval(refreshPassiveData, 15000);
        window.setInterval(refreshLiveStateIfNeeded, 2000);
    } catch (error) {
        showToast(error.message, "error");
    }
}

async function refreshAll() {
    const [
        machineStatus,
        machineComponents,
        diagnostics,
        credit,
        transactions,
        products,
        recipes,
        ingredients,
        tank,
        orders,
        config,
        simulationConfig,
        maintenance,
        metrics,
        recentLogs
    ] = await Promise.all([
        api.getMachineStatus(),
        api.getMachineComponents(),
        api.getMachineDiagnostics(),
        api.getCredit(),
        api.getTransactions(),
        api.getProducts(),
        api.getRecipes(),
        api.getIngredients(),
        api.getTankStatus(),
        api.getOrders(),
        api.getConfig(),
        api.getSimulationConfig(),
        api.getMaintenanceStatus(),
        api.getMetrics(),
        api.getRecentLogs(50)
    ]);

    setState({
        machineStatus,
        machineComponents,
        diagnostics,
        credit,
        transactions,
        products,
        recipes,
        ingredients,
        tank,
        orders,
        config,
        simulationConfig,
        maintenance,
        metrics,
        recentLogs
    });
}

async function refreshPassiveData() {
    try {
        const [diagnostics, orders, metrics, recentLogs, maintenance] = await Promise.all([
            api.getMachineDiagnostics(),
            api.getOrders(),
            api.getMetrics(),
            api.getRecentLogs(50),
            api.getMaintenanceStatus()
        ]);

        setState({ diagnostics, orders, metrics, recentLogs, maintenance });
    } catch (error) {
        showToast(error.message, "error");
    }
}

async function refreshLiveStateIfNeeded() {
    const hasActiveOrder = state.orders.some(order => !["Completed", "Failed", "Cancelled"].includes(order.status));
    const shouldPoll = hasActiveOrder || state.connectionStatus !== "connected";

    if (!shouldPoll) {
        return;
    }

    try {
        const [machineStatus, machineComponents, diagnostics, orders, credit] = await Promise.all([
            api.getMachineStatus(),
            api.getMachineComponents(),
            api.getMachineDiagnostics(),
            api.getOrders(),
            api.getCredit()
        ]);

        setState({ machineStatus, machineComponents, diagnostics, orders, credit });
    } catch (error) {
        pushEvent({ type: "PollingError", message: error.message });
    }
}

function renderApp(appState, patch = {}) {
    machineName.textContent = appState.machineStatus?.machineId ?? "CoffeeMachine";
    machineStatusChip.textContent = appState.machineStatus?.status ?? "Unknown";
    machineStatusChip.className = `status-chip ${String(appState.machineStatus?.status || "").toLowerCase() ? `status-${String(appState.machineStatus?.status || "").toLowerCase()}` : ""}`;
    headerCredit.textContent = new Intl.NumberFormat("it-IT", { style: "currency", currency: "EUR" }).format(appState.credit?.currentCredit ?? 0);
    headerTemperature.textContent = `${appState.machineStatus?.currentTemperature?.toFixed?.(1) ?? "0"}°C`;
    connectionStatus.textContent = `Realtime: ${appState.connectionStatus}`;

    if (shouldRenderSection(activeSection, patch)) {
        renderCurrentView(appState, patch);
    }
}

function renderCurrentView(appState, patch = {}) {
    switch (activeSection) {
        case "purchase":
            renderPurchase(roots.purchase, appState);
            break;
        case "machine":
            renderMachine(roots.machine, appState);
            break;
        case "ingredients":
            renderIngredients(roots.ingredients, appState);
            break;
        case "recipes":
            renderRecipes(roots.recipes, appState);
            break;
        case "products":
            renderProducts(roots.products, appState);
            break;
        case "orders":
            renderOrders(roots.orders, appState);
            break;
        case "diagnostics":
            renderDiagnostics(roots.diagnostics, appState);
            break;
        case "maintenance":
            renderMaintenance(roots.maintenance, appState);
            break;
        case "config":
            renderConfig(roots.config, appState);
            break;
        case "dashboard":
        default:
            renderDashboard(roots.dashboard, appState, patch);
            break;
    }
}

function shouldRenderSection(section, patch) {
    const patchKeys = Object.keys(patch || {});
    if (!patchKeys.length) {
        return true;
    }

    const dependencies = {
        dashboard: ["machineStatus", "machineComponents", "ingredients", "orders", "products", "recipes", "diagnostics", "eventFeed", "dispensedItem", "credit", "tank", "connectionStatus", "dashboardKeypadInput"],
        purchase: ["machineStatus", "orders", "products", "credit", "dispensedItem", "connectionStatus"],
        machine: ["machineStatus", "machineComponents", "maintenance"],
        ingredients: ["ingredients", "tank"],
        recipes: ["recipes"],
        products: ["products", "recipes"],
        orders: ["orders"],
        diagnostics: ["diagnostics", "metrics", "recentLogs", "connectionStatus", "eventFeed"],
        maintenance: ["maintenance"],
        config: ["simulationConfig"]
    };

    return patchKeys.some(key => (dependencies[section] || dependencies.dashboard).includes(key));
}

async function handleAction(event) {
    const trigger = event.target.closest("[data-action]");
    if (!trigger) {
        return;
    }

    try {
        switch (trigger.dataset.action) {
            case "add-credit-preset":
                await api.addCredit({ amount: Number(trigger.dataset.amount), description: "Dashboard quick add" });
                break;
            case "keypad-digit":
                setState({
                    dashboardKeypadInput: `${state.dashboardKeypadInput || ""}${trigger.dataset.digit}`.slice(0, 2)
                });
                return;
            case "keypad-clear":
                setState({ dashboardKeypadInput: "" });
                return;
            case "keypad-ok": {
                const product = findProductByDashboardCode(state.products, state.dashboardKeypadInput);
                if (!product) {
                    showToast("Codice prodotto non valido", "error");
                    return;
                }

                setState({ dispensedItem: null });
                await api.createOrder({ productId: product.id });
                setState({ dashboardKeypadInput: "" });
                break;
            }
            case "reset-credit":
                await api.resetCredit();
                break;
            case "order-product":
                setState({ dispensedItem: null });
                await api.createOrder({ productId: trigger.dataset.productId });
                break;
            case "pickup-product":
                setState({ dispensedItem: null });
                showToast("Prodotto ritirato", "success");
                return;
            case "power-on":
                await api.powerOn();
                break;
            case "power-off":
                await api.powerOff();
                break;
            case "reset-machine":
                await api.resetMachine();
                break;
            case "refill-ingredient":
                await api.refillIngredient(trigger.dataset.id, 250);
                break;
            case "refill-water":
                await api.refillWater(Number(trigger.dataset.amount));
                break;
            case "cancel-order":
                await api.cancelOrder(trigger.dataset.id);
                break;
            case "delete-product":
                await api.deleteProduct(trigger.dataset.id);
                break;
            case "delete-recipe":
                await api.deleteRecipe(trigger.dataset.id);
                break;
            case "edit-product":
                fillProductForm(trigger.dataset.id);
                return;
            case "new-product":
                resetForm("product-form");
                return;
            case "edit-recipe":
                fillRecipeForm(trigger.dataset.id);
                return;
            case "new-recipe":
                resetForm("recipe-form");
                return;
            case "reset-maintenance":
                await api.resetMaintenance();
                break;
            case "save-state":
                await api.saveState();
                showToast("Snapshot salvato", "success");
                break;
            case "reload-state":
                await api.reloadState();
                showToast("Snapshot ricaricato", "success");
                break;
            case "export-state":
                await downloadState();
                return;
            default:
                return;
        }

        await refreshAll();
    } catch (error) {
        showToast(error.message, "error");
    }
}

async function handleSubmit(event) {
    if (event.target.id === "product-form") {
        event.preventDefault();
        const data = new FormData(event.target);
        const body = {
            name: data.get("name"),
            price: Number(data.get("price")),
            recipeId: data.get("recipeId"),
            enabled: data.get("enabled") === "true"
        };

        try {
            const id = data.get("id");
            id ? await api.updateProduct(id, body) : await api.createProduct(body);
            resetForm("product-form");
            await refreshAll();
            showToast("Prodotto salvato", "success");
        } catch (error) {
            showToast(error.message, "error");
        }
    }

    if (event.target.id === "recipe-form") {
        event.preventDefault();
        const data = new FormData(event.target);

        try {
            const body = {
                name: data.get("name"),
                targetTemperature: Number(data.get("targetTemperature")),
                steps: JSON.parse(data.get("steps"))
            };

            const id = data.get("id");
            id ? await api.updateRecipe(id, body) : await api.createRecipe(body);
            resetForm("recipe-form");
            await refreshAll();
            showToast("Ricetta salvata", "success");
        } catch (error) {
            showToast(error.message || "JSON step non valido", "error");
        }
    }

    if (event.target.id === "simulation-form") {
        event.preventDefault();
        const data = new FormData(event.target);
        const body = {
            tickIntervalMs: Number(data.get("tickIntervalMs")),
            heatingRatePerTick: Number(data.get("heatingRatePerTick")),
            coolingRatePerTick: Number(data.get("coolingRatePerTick")),
            heatingTimeoutSeconds: Number(data.get("heatingTimeoutSeconds")),
            processFailureProbability: Number(data.get("processFailureProbability")),
            maximumBoilerTemperature: Number(data.get("maximumBoilerTemperature")),
            autoSaveEnabled: data.get("autoSaveEnabled") === "true",
            autoSaveIntervalSeconds: Number(data.get("autoSaveIntervalSeconds"))
        };

        try {
            await api.updateSimulationConfig(body);
            await refreshAll();
            showToast("Configurazione simulazione aggiornata", "success");
        } catch (error) {
            showToast(error.message, "error");
        }
    }

    if (event.target.id === "fault-form") {
        event.preventDefault();
        const data = new FormData(event.target);

        try {
            await api.injectFault({
                faultType: data.get("faultType"),
                message: data.get("message")
            });
            await refreshAll();
            showToast("Fault iniettato", "success");
        } catch (error) {
            showToast(error.message, "error");
        }
    }
}

async function handleRealtimeEvent(eventName) {
    try {
        switch (eventName) {
            case "MachineStateChanged":
            case "TemperatureChanged":
            case "MaintenanceStatusChanged":
                setState({
                    machineStatus: await api.getMachineStatus(),
                    machineComponents: await api.getMachineComponents(),
                    maintenance: await api.getMaintenanceStatus()
                });
                break;
            case "IngredientLevelChanged":
                setState({
                    ingredients: await api.getIngredients(),
                    tank: await api.getTankStatus(),
                    machineStatus: await api.getMachineStatus()
                });
                break;
            case "CreditChanged":
                setState({
                    credit: await api.getCredit(),
                    transactions: await api.getTransactions(),
                    machineStatus: await api.getMachineStatus()
                });
                break;
            case "OrderCreated":
            case "OrderStatusChanged":
            case "DispensingProgressChanged":
                setState({
                    orders: await api.getOrders(),
                    machineStatus: await api.getMachineStatus(),
                    diagnostics: await api.getMachineDiagnostics()
                });
                break;
            default:
                await refreshAll();
                break;
        }
    } catch (error) {
        pushEvent({ type: "FrontendError", message: error.message });
    }
}

function fillProductForm(id) {
    const product = state.products.find(item => item.id === id);
    const form = document.getElementById("product-form");
    if (!product || !form) {
        return;
    }

    form.elements.id.value = product.id;
    form.elements.name.value = product.name;
    form.elements.price.value = product.price;
    form.elements.recipeId.value = product.recipeId;
    form.elements.enabled.value = String(product.enabled);
}

function setState(patch) {
    const nextPatch = { ...patch };
    if (patch.orders) {
        nextPatch.dispensedItem = deriveDispensedItem(state.orders, patch.orders, patch.dispensedItem ?? state.dispensedItem);
    }
    return applyState(nextPatch);
}

function deriveDispensedItem(previousOrders, nextOrders, currentDispensedItem) {
    const previousOrdersById = new Map(previousOrders.map(order => [order.id, order]));
    const activeOrder = nextOrders.find(order => !terminalOrderStatuses.includes(order.status));
    const newlyCompletedOrder = nextOrders.find(order => {
        if (order.status !== "Completed") {
            return false;
        }

        const previousOrder = previousOrdersById.get(order.id);
        return previousOrder && previousOrder.status !== "Completed";
    });

    if (newlyCompletedOrder) {
        return {
            orderId: newlyCompletedOrder.id,
            productName: newlyCompletedOrder.productName,
            completedAt: newlyCompletedOrder.completedAtUtc ?? new Date().toISOString(),
            vessel: getVesselType(newlyCompletedOrder.productName),
            intensity: getDrinkIntensity(newlyCompletedOrder.productName)
        };
    }

    if (activeOrder) {
        return null;
    }

    if (currentDispensedItem) {
        return currentDispensedItem;
    }

    return patchHasDispensedItem(nextOrders, currentDispensedItem) ? currentDispensedItem : null;
}

function patchHasDispensedItem(nextOrders, currentDispensedItem) {
    if (!currentDispensedItem) {
        return false;
    }

    return nextOrders.some(order => order.id === currentDispensedItem.orderId && order.status === "Completed");
}

function getVesselType(productName) {
    const normalizedName = String(productName || "").toLowerCase();
    return normalizedName.includes("espresso") || normalizedName.includes("ristretto") ? "cup" : "glass";
}

function getDrinkIntensity(productName) {
    const normalizedName = String(productName || "").toLowerCase();
    if (normalizedName.includes("latte") || normalizedName.includes("milk") || normalizedName.includes("macchiato")) {
        return "latte";
    }
    if (normalizedName.includes("chocolate") || normalizedName.includes("mocha")) {
        return "mocha";
    }
    return "classic";
}

function findProductByDashboardCode(products, input) {
    const normalizedCode = String(input || "").replace(/\D/g, "").padStart(2, "0");
    return products.find((product, index) => String(index + 1).padStart(2, "0") === normalizedCode);
}

function fillRecipeForm(id) {
    const recipe = state.recipes.find(item => item.id === id);
    const form = document.getElementById("recipe-form");
    if (!recipe || !form) {
        return;
    }

    form.elements.id.value = recipe.id;
    form.elements.name.value = recipe.name;
    form.elements.targetTemperature.value = recipe.targetTemperature;
    form.elements.steps.value = JSON.stringify(recipe.steps, null, 2);
}

function resetForm(id) {
    const form = document.getElementById(id);
    if (form) {
        form.reset();
        if (form.elements.id) {
            form.elements.id.value = "";
        }
    }
}

async function downloadState() {
    const content = await api.exportState();
    const blob = new Blob([content], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = "coffee-machine-state.json";
    anchor.click();
    URL.revokeObjectURL(url);
}

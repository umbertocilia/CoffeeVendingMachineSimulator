import { money, percentage, relativeTime } from "./utils.js";

const terminalOrderStatuses = ["Completed", "Failed", "Cancelled"];
const machinePanelKeys = ["machineStatus", "machineComponents", "orders", "ingredients", "diagnostics", "dispensedItem", "credit", "tank", "dashboardKeypadInput", "products", "recipes"];
const quickPanelKeys = ["products", "credit"];
const eventsPanelKeys = ["eventFeed", "connectionStatus"];

export function renderDashboard(root, appState, patch = {}) {
    const patchKeys = Object.keys(patch || {});

    if (!root.querySelector("[data-dashboard-shell]")) {
        root.innerHTML = `
            <div class="dashboard-stack" data-dashboard-shell>
                <div class="card hero-card dashboard-machine-card">
                    <div id="dashboard-machine-panel"></div>
                </div>
                <div class="card dashboard-quick-card">
                    <div id="dashboard-quick-panel"></div>
                </div>
                <div class="card realtime-card dashboard-events-card">
                    <div id="dashboard-events-panel"></div>
                </div>
                <div class="grid three dashboard-bottom-grid">
                    <div class="card" id="dashboard-ingredients-panel"></div>
                    <div class="card" id="dashboard-products-panel"></div>
                    <div class="card" id="dashboard-pickup-panel"></div>
                </div>
            </div>
        `;
    }

    const machinePanel = root.querySelector("#dashboard-machine-panel");
    ensureDashboardMachinePanel(machinePanel);

    if (!patchKeys.length || patchKeys.some(key => machinePanelKeys.includes(key))) {
        updateDashboardMachinePanel(machinePanel, appState);
    }

    if (!patchKeys.length || patchKeys.some(key => quickPanelKeys.includes(key))) {
        root.querySelector("#dashboard-quick-panel").innerHTML = renderDashboardQuickPanel(appState);
    }

    if (!patchKeys.length || patchKeys.some(key => eventsPanelKeys.includes(key))) {
        root.querySelector("#dashboard-events-panel").innerHTML = renderDashboardEventsPanel(appState);
    }

    if (!patchKeys.length || patchKeys.includes("ingredients")) {
        root.querySelector("#dashboard-ingredients-panel").innerHTML = renderDashboardIngredientsPanel(appState);
    }

    if (!patchKeys.length || patchKeys.includes("products")) {
        root.querySelector("#dashboard-products-panel").innerHTML = renderDashboardProductsPanel(appState);
    }

    if (!patchKeys.length || patchKeys.includes("dispensedItem")) {
        root.querySelector("#dashboard-pickup-panel").innerHTML = renderDashboardPickupPanel(appState);
    }
}

function ensureDashboardMachinePanel(container) {
    if (!container || container.querySelector("[data-machine-shell]")) {
        return;
    }

    container.innerHTML = `
        <div class="section-header">
            <div>
                <p class="eyebrow">Front Office Machine</p>
                <h3 class="section-title">Distributore bevande calde</h3>
            </div>
            <span class="badge" data-machine-badge>Ready</span>
        </div>
        <div class="dashboard-machine-layout" data-machine-shell>
            <div class="dashboard-machine-main">
                <div class="modern-machine idle" data-machine-visual>
                    <div class="machine-body">
                        <div class="machine-top">
                            <div class="machine-screen">
                                <span class="screen-label">Machine OS</span>
                                <strong data-screen-status>Ready</strong>
                                <span data-screen-temperature>0.0&#176;C / 0.0&#176;C</span>
                            </div>
                            <div class="machine-control-panel">
                                <div class="machine-keypad-display" data-keypad-display>
                                    <span>Selezione</span>
                                    <strong data-keypad-code>--</strong>
                                    <small data-keypad-product>Digita il codice prodotto e premi OK</small>
                                </div>
                                <div class="machine-dial-panel">
                                    <div class="machine-dial">
                                        <div class="machine-dial-core">
                                            <span class="machine-dial-indicator" data-dial-indicator></span>
                                        </div>
                                    </div>
                                    <span class="machine-control-caption">Brew selector</span>
                                </div>
                                <div class="machine-light-row">
                                    <span class="control-light green"></span>
                                    <span class="control-light amber" data-light="amber"></span>
                                    <span class="control-light red" data-light="red"></span>
                                </div>
                                <div class="machine-keypad">
                                    ${["1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "0", "OK"].map(key => renderKeypadKey(key)).join("")}
                                </div>
                            </div>
                        </div>
                        <div class="canister-row">
                            <div class="canister">
                                <span>Coffee</span>
                                <strong data-canister="coffee">0%</strong>
                            </div>
                            <div class="canister">
                                <span>Milk</span>
                                <strong data-canister="milk">0%</strong>
                            </div>
                            <div class="canister">
                                <span>Chocolate</span>
                                <strong data-canister="chocolate">0%</strong>
                            </div>
                        </div>
                        <div class="dispense-bay">
                            <div class="brew-group">
                                <div class="brew-manifold"></div>
                                <div class="nozzle-head left"></div>
                                <div class="nozzle-head right"></div>
                            </div>
                            <div class="pour-stream-group" data-pour-group>
                                <div class="pour-stream left stream-coffee" data-stream="left"></div>
                                <div class="pour-stream right stream-coffee" data-stream="right"></div>
                            </div>
                            <div class="steam-group" data-steam-group>
                                <span class="steam-line left"></span>
                                <span class="steam-line right"></span>
                            </div>
                            <div class="cup-slot">
                                <div class="vessel glass idle" data-vessel>
                                    <div class="vessel-fill drink-classic" data-vessel-fill></div>
                                    <div class="vessel-foam" data-vessel-foam hidden></div>
                                    <div class="cup-handle" data-cup-handle hidden></div>
                                </div>
                            </div>
                            <div class="cup-platform"></div>
                            <div class="pickup-stage" data-pickup-stage>
                                <span class="pickup-placeholder">Vano consegna</span>
                            </div>
                        </div>
                        <div class="machine-base">
                            <span>Office Series 24</span>
                            <span data-machine-base-status>Ready</span>
                        </div>
                    </div>
                </div>
            </div>
            <div class="dashboard-machine-side">
                <div class="machine-kpis">
                    <div class="stat-tile accent">
                        <span>Credito</span>
                        <strong data-kpi-credit>${money(0)}</strong>
                    </div>
                    <div class="stat-tile">
                        <span>Acqua disponibile</span>
                        <strong data-kpi-water>0 ml</strong>
                    </div>
                    <div class="stat-tile">
                        <span>Temperatura boiler</span>
                        <strong data-kpi-temperature>0.0&#176;C</strong>
                    </div>
                    <div class="stat-tile">
                        <span>Ordine in lavorazione</span>
                        <strong data-kpi-order>Nessuno</strong>
                    </div>
                </div>
                <div class="live-panel">
                    <div class="inline-between">
                        <strong>Preparazione live</strong>
                        <span data-live-status>Idle</span>
                    </div>
                    <div class="progress-track prominent">
                        <div class="progress-bar" data-live-progress></div>
                    </div>
                    <p class="muted" data-live-message>La macchina e pronta a ricevere un codice prodotto e simulare l'erogazione.</p>
                </div>
            </div>
        </div>
    `;
}

function updateDashboardMachinePanel(container, appState) {
    if (!container) {
        return;
    }

    const status = appState.machineStatus;
    const activeOrder = getActiveOrder(appState);
    const activeRecipe = getActiveRecipe(appState, activeOrder);
    const drinkPresentation = getDrinkPresentation(activeOrder?.productName ?? appState.dispensedItem?.productName, activeRecipe);
    const flowState = getFlowState(activeOrder, activeRecipe, drinkPresentation);
    const fillHeight = getFillHeight(activeOrder);
    const selectedProduct = findDashboardProductByCode(appState.products, appState.dashboardKeypadInput);
    const pickupReady = Boolean(appState.dispensedItem && !activeOrder);
    const currentCredit = status?.currentCredit ?? appState.credit?.currentCredit ?? 0;
    const currentWater = status?.waterLevelMl ?? appState.tank?.currentLevelMl ?? 0;
    const boilerTemperature = formatTemperature(status?.currentTemperature);
    const statusLabel = activeOrder ? activeOrder.productName : pickupReady ? "Pronto al ritiro" : "Nessuno";
    const liveStatus = activeOrder?.status ?? (pickupReady ? "Pickup" : "Idle");
    const liveProgress = activeOrder ? clampPercentage(activeOrder.progressPercentage) : pickupReady ? 100 : 0;
    const badgeClass = getMachineBadgeClass(appState.diagnostics);
    const machineStage = getMachineStage(status?.status, activeOrder);
    const dialRotation = activeOrder ? 24 : pickupReady ? 54 : -32;

    const badge = container.querySelector("[data-machine-badge]");
    badge.className = `badge ${badgeClass}`.trim();
    setText(badge, status?.status ?? "Unknown");

    const machineVisual = container.querySelector("[data-machine-visual]");
    machineVisual.className = `modern-machine ${machineStage}`;

    setText(container.querySelector("[data-screen-status]"), activeOrder?.status ?? (pickupReady ? "Pickup Ready" : status?.status ?? "Standby"));
    setText(container.querySelector("[data-screen-temperature]"), `${formatTemperature(status?.currentTemperature)} / ${formatTemperature(status?.targetTemperature)}`);

    const keypadDisplay = container.querySelector("[data-keypad-display]");
    keypadDisplay.classList.toggle("active", Boolean(appState.dashboardKeypadInput));
    setText(container.querySelector("[data-keypad-code]"), appState.dashboardKeypadInput || "--");
    setText(container.querySelector("[data-keypad-product]"), selectedProduct?.name ?? "Digita il codice prodotto e premi OK");

    const dialIndicator = container.querySelector("[data-dial-indicator]");
    dialIndicator.style.transform = `translateX(-50%) rotate(${dialRotation}deg)`;

    const amberLight = container.querySelector('[data-light="amber"]');
    const redLight = container.querySelector('[data-light="red"]');
    amberLight.className = `control-light amber${activeOrder ? " pulsing" : ""}`;
    redLight.className = `control-light red${machineStage === "alert" ? " pulsing" : ""}`;

    setText(container.querySelector('[data-canister="coffee"]'), `${ingredientPercentage(appState.ingredients, "coffee")}%`);
    setText(container.querySelector('[data-canister="milk"]'), `${ingredientPercentage(appState.ingredients, "milk")}%`);
    setText(container.querySelector('[data-canister="chocolate"]'), `${ingredientPercentage(appState.ingredients, "chocolate")}%`);

    const pourGroup = container.querySelector("[data-pour-group]");
    pourGroup.classList.toggle("visible", flowState.visible);
    container.querySelector('[data-stream="left"]').className = `pour-stream left ${flowState.leftStreamClass}`;
    container.querySelector('[data-stream="right"]').className = `pour-stream right ${flowState.rightStreamClass}`;
    container.querySelector("[data-steam-group]").classList.toggle("visible", flowState.showSteam);

    const vessel = container.querySelector("[data-vessel]");
    vessel.className = `vessel ${drinkPresentation.vessel} ${activeOrder ? "filling" : "idle"}`;

    const vesselFill = container.querySelector("[data-vessel-fill]");
    vesselFill.className = `vessel-fill ${drinkPresentation.fillClass}`;
    vesselFill.style.height = `${fillHeight}%`;

    const vesselFoam = container.querySelector("[data-vessel-foam]");
    vesselFoam.hidden = !(drinkPresentation.hasFoam && fillHeight > 16);
    if (!vesselFoam.hidden) {
        vesselFoam.style.bottom = `calc(${Math.max(fillHeight - 8, 8)}% + 8px)`;
    }

    container.querySelector("[data-cup-handle]").hidden = drinkPresentation.vessel !== "cup";

    const pickupStage = container.querySelector("[data-pickup-stage]");
    pickupStage.className = `pickup-stage${pickupReady ? " loaded" : ""}`;
    setHtml(pickupStage, pickupReady ? renderPickupProduct(appState.dispensedItem) : '<span class="pickup-placeholder">Vano consegna</span>');

    setText(container.querySelector("[data-machine-base-status]"), activeOrder ? activeOrder.status : status?.status ?? "Ready");
    setText(container.querySelector("[data-kpi-credit]"), money(currentCredit));
    setText(container.querySelector("[data-kpi-water]"), `${currentWater} ml`);
    setText(container.querySelector("[data-kpi-temperature]"), boilerTemperature);
    setText(container.querySelector("[data-kpi-order]"), statusLabel);
    setText(container.querySelector("[data-live-status]"), liveStatus);
    container.querySelector("[data-live-progress]").style.width = `${liveProgress}%`;
    setText(container.querySelector("[data-live-message]"), describeMachineMessage(status?.status, activeOrder, appState.dispensedItem));
}

function renderDashboardQuickPanel(appState) {
    const catalog = buildDashboardCatalog(appState.products);

    return `
        <div class="section-header">
            <div>
                <h3 class="section-title">Credito e Catalogo</h3>
                <span class="muted">Inserisci monete e usa il tastierino sulla macchina</span>
            </div>
            <span class="muted">${catalog.length} prodotti</span>
        </div>
        <div class="coin-strip">
            <button class="coin-chip" data-action="add-credit-preset" data-amount="0.2">+ EUR 0,20</button>
            <button class="coin-chip" data-action="add-credit-preset" data-amount="0.5">+ EUR 0,50</button>
            <button class="coin-chip" data-action="add-credit-preset" data-amount="1">+ EUR 1,00</button>
        </div>
        <div class="product-code-grid">
            ${catalog.map(product => {
                const reason = getProductReason(product, appState);
                return `
                    <div class="product-code-card ${reason ? "disabled" : ""}">
                        <span class="product-code-badge">${product.code}</span>
                        <strong>${product.name}</strong>
                        <span>${money(product.price)}</span>
                        <small>${reason || "Selezione da tastierino"}</small>
                    </div>
                `;
            }).join("")}
        </div>
    `;
}

function renderDashboardEventsPanel(appState) {
    return `
        <div class="section-header">
            <h3 class="section-title">Eventi realtime</h3>
            <span class="muted">${appState.connectionStatus}</span>
        </div>
        <div class="feed scrollable">
            ${appState.eventFeed.length ? appState.eventFeed.map(item => `
                <div class="feed-item">
                    <div class="inline-between">
                        <strong>${item.type}</strong>
                        <span class="muted">${relativeTime(item.at)}</span>
                    </div>
                    <div>${item.message}</div>
                </div>`).join("") : '<div class="empty-state">Nessun evento realtime ricevuto.</div>'}
        </div>
    `;
}

function renderDashboardIngredientsPanel(appState) {
    return `
        <h3>Ingredienti principali</h3>
        <div class="resource-list">
            ${appState.ingredients.slice(0, 4).map(item => resourceBar(item.name, item.currentLevel, item.capacity, item.unit)).join("")}
        </div>
    `;
}

function renderDashboardProductsPanel(appState) {
    const catalog = buildDashboardCatalog(appState.products);

    return `
        <h3>Prodotti disponibili</h3>
        <div class="list">
            ${catalog.slice(0, 5).map(product => `
                <div class="list-item">
                    <div class="inline-between">
                        <strong>${product.code} · ${product.name}</strong>
                        <span>${money(product.price)}</span>
                    </div>
                    <div class="muted">${product.enabled ? "Disponibile" : "Disabilitato"}</div>
                </div>`).join("")}
        </div>
    `;
}

function renderDashboardPickupPanel(appState) {
    return `
        <h3>Consegna prodotto</h3>
        ${renderPickupPanel(appState.dispensedItem)}
    `;
}

function renderKeypadKey(key) {
    if (key === "OK") {
        return `<button type="button" class="confirm" data-action="keypad-ok">${key}</button>`;
    }

    if (key === "*") {
        return `<button type="button" data-action="keypad-clear">${key}</button>`;
    }

    return `<button type="button" data-action="keypad-digit" data-digit="${key}">${key}</button>`;
}

function renderPickupPanel(dispensedItem) {
    if (!dispensedItem) {
        return '<div class="empty-state compact">Nessun prodotto pronto nel vano di ritiro.</div>';
    }

    return `
        <div class="pickup-panel compact">
            <div class="pickup-panel-visual">
                ${renderPickupProduct(dispensedItem)}
            </div>
            <div class="pickup-panel-copy">
                <strong>${dispensedItem.productName}</strong>
                <div class="muted">Erogazione completata: pronto per il ritiro.</div>
                <button class="btn primary" data-action="pickup-product">Ritira prodotto</button>
            </div>
        </div>
    `;
}

function renderPickupProduct(dispensedItem) {
    return `
        <div class="pickup-product ${dispensedItem.vessel} drink-${dispensedItem.intensity}">
            <div class="pickup-fill"></div>
            ${dispensedItem.intensity === "latte" ? '<div class="pickup-foam"></div>' : ""}
            ${dispensedItem.vessel === "cup" ? '<div class="cup-handle"></div>' : ""}
        </div>
    `;
}

function resourceBar(label, current, max, unit) {
    const ratio = percentage(current, max);
    return `
        <div>
            <div class="resource-item-header">
                <span>${label}</span>
                <strong>${current} / ${max} ${unit}</strong>
            </div>
            <div class="progress-track"><div class="progress-bar" style="width:${ratio}%"></div></div>
        </div>`;
}

function getActiveOrder(appState) {
    return appState.orders.find(item => !terminalOrderStatuses.includes(item.status));
}

function getActiveRecipe(appState, activeOrder) {
    if (!activeOrder) {
        return null;
    }

    return appState.recipes.find(recipe => recipe.id === activeOrder.recipeId) ?? null;
}

function getMachineStage(machineStatus, activeOrder) {
    const normalizedStatus = String(machineStatus || "").toLowerCase();
    if (activeOrder?.status === "WaitingForHeat" || activeOrder?.status === "Validating") {
        return "heating";
    }
    if (activeOrder) {
        return "dispensing";
    }
    if (normalizedStatus.includes("error") || normalizedStatus.includes("warning") || normalizedStatus.includes("service")) {
        return "alert";
    }
    if (normalizedStatus.includes("heat")) {
        return "heating";
    }
    return "idle";
}

function describeMachineMessage(machineStatus, activeOrder, dispensedItem) {
    if (dispensedItem && !activeOrder) {
        return `${dispensedItem.productName} pronto nel vano di consegna. Usa il pulsante di ritiro per liberare la macchina.`;
    }

    if (activeOrder?.status === "WaitingForHeat" || activeOrder?.status === "Validating") {
        return "La macchina sta portando il boiler in temperatura prima di iniziare l'erogazione del prodotto selezionato.";
    }

    if (activeOrder?.status === "DispensingIngredient") {
        return `Erogazione in corso per ${activeOrder.productName}. Il riempimento della tazza segue l'avanzamento reale della ricetta.`;
    }

    if (activeOrder?.status === "Mixing") {
        return `La bevanda ${activeOrder.productName} sta terminando la fase finale di miscelazione prima di uscire nel vano di consegna.`;
    }

    if (String(machineStatus || "").toLowerCase().includes("heat")) {
        return "Il boiler sta raggiungendo la temperatura target prima di avviare l'erogazione.";
    }

    return "La macchina e pronta a ricevere un codice prodotto, accettare credito e simulare un nuovo ciclo di erogazione.";
}

function getFillHeight(activeOrder) {
    if (!activeOrder) {
        return 0;
    }

    if (activeOrder.status === "Mixing") {
        return 84;
    }

    if (activeOrder.status !== "DispensingIngredient") {
        return 0;
    }

    return clampPercentage(Math.max(12, activeOrder.progressPercentage * 0.82));
}

function getFlowState(activeOrder, activeRecipe, drinkPresentation) {
    if (!activeOrder) {
        return {
            visible: false,
            showSteam: false,
            leftStreamClass: "stream-coffee",
            rightStreamClass: "stream-coffee"
        };
    }

    const currentStep = activeOrder.status === "DispensingIngredient"
        ? activeRecipe?.steps?.[activeOrder.currentStepIndex]
        : null;
    const currentIngredient = normalizeIngredientKey(currentStep?.ingredientKey);

    if (currentIngredient === "milk") {
        return {
            visible: true,
            showSteam: true,
            leftStreamClass: "stream-milk",
            rightStreamClass: "stream-milk"
        };
    }

    if (currentIngredient === "chocolate" || currentIngredient === "sugar") {
        return {
            visible: true,
            showSteam: true,
            leftStreamClass: "stream-chocolate",
            rightStreamClass: "stream-chocolate"
        };
    }

    if (activeOrder.status === "DispensingIngredient") {
        return {
            visible: true,
            showSteam: true,
            leftStreamClass: "stream-coffee",
            rightStreamClass: drinkPresentation.intensity === "latte" ? "stream-milk" : "stream-coffee"
        };
    }

    return {
        visible: false,
        showSteam: activeOrder.status === "Mixing",
        leftStreamClass: "stream-coffee",
        rightStreamClass: "stream-coffee"
    };
}

function getDrinkIntensity(productName, recipe) {
    const normalizedName = String(productName || "").toLowerCase();
    const ingredientKeys = new Set((recipe?.steps ?? []).map(step => normalizeIngredientKey(step.ingredientKey)));

    if (ingredientKeys.has("milk") || normalizedName.includes("latte") || normalizedName.includes("milk") || normalizedName.includes("macchiato") || normalizedName.includes("cappuccino")) {
        return "latte";
    }

    if (ingredientKeys.has("chocolate") || normalizedName.includes("chocolate") || normalizedName.includes("mocha")) {
        return "mocha";
    }

    return "classic";
}

function getServingVessel(productName) {
    const normalizedName = String(productName || "").toLowerCase();
    return normalizedName.includes("espresso") || normalizedName.includes("ristretto") ? "cup" : "glass";
}

function getDrinkPresentation(productName, recipe) {
    const intensity = getDrinkIntensity(productName, recipe);
    const vessel = intensity === "classic" ? getServingVessel(productName) : "glass";

    if (intensity === "latte") {
        return {
            intensity,
            vessel,
            fillClass: "drink-latte",
            hasFoam: true
        };
    }

    if (intensity === "mocha") {
        return {
            intensity,
            vessel: "glass",
            fillClass: "drink-mocha",
            hasFoam: false
        };
    }

    return {
        intensity: "classic",
        vessel,
        fillClass: "drink-classic",
        hasFoam: false
    };
}

function buildDashboardCatalog(products) {
    return products.map((product, index) => ({
        ...product,
        code: String(index + 1).padStart(2, "0")
    }));
}

function findDashboardProductByCode(products, input) {
    const normalizedCode = String(input || "").replace(/\D/g, "").padStart(2, "0");
    return buildDashboardCatalog(products).find(product => product.code === normalizedCode);
}

function getProductReason(product, appState) {
    if (!product.enabled) {
        return "Prodotto disabilitato";
    }
    if (!appState.machineStatus?.powerOn) {
        return "Macchina spenta";
    }
    if (appState.machineStatus?.maintenanceRequired) {
        return "Macchina in manutenzione";
    }
    if (["Error", "OutOfService"].includes(appState.machineStatus?.status)) {
        return "Macchina non disponibile";
    }
    if ((appState.machineStatus?.currentCredit ?? 0) < product.price) {
        return "Credito insufficiente";
    }
    return "";
}

function getMachineBadgeClass(diagnostics) {
    const warnings = diagnostics?.activeWarnings ?? [];
    const errors = diagnostics?.activeErrors ?? [];

    if (errors.length) {
        return "error";
    }

    if (warnings.length) {
        return "warning";
    }

    return "";
}

function ingredientPercentage(ingredients, ingredientName) {
    const ingredient = ingredients.find(item => normalizeIngredientKey(item.name) === normalizeIngredientKey(ingredientName));
    return percentage(ingredient?.currentLevel ?? 0, ingredient?.capacity ?? 0);
}

function formatTemperature(value) {
    const numericValue = Number(value ?? 0);
    return `${numericValue.toFixed(1)}\u00B0C`;
}

function normalizeIngredientKey(value) {
    return String(value || "").trim().toLowerCase();
}

function clampPercentage(value) {
    const numericValue = Number(value ?? 0);
    return Math.max(0, Math.min(100, numericValue));
}

function setText(element, value) {
    if (!element) {
        return;
    }

    const nextValue = String(value ?? "");
    if (element.textContent !== nextValue) {
        element.textContent = nextValue;
    }
}

function setHtml(element, value) {
    if (!element) {
        return;
    }

    if (element.innerHTML !== value) {
        element.innerHTML = value;
    }
}

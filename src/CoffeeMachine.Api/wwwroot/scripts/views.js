import { money, percentage, relativeTime, safeText, statusClass, createOptions } from "./utils.js";

const terminalOrderStatuses = ["Completed", "Failed", "Cancelled"];

export function renderDashboard(root, appState, patch = {}) {
    const patchKeys = Object.keys(patch || {});

    root.innerHTML = `
        <div class="grid two dashboard-grid">
            <div class="card hero-card">
                <div class="section-header">
                    <div>
                        <p class="eyebrow">Front Office Machine</p>
                        <h3 class="section-title">Distributore bevande calde</h3>
                    </div>
                    <span class="badge ${errors.length ? "error" : warnings.length ? "warning" : ""}">${status?.status ?? "Unknown"}</span>
                </div>
                <div class="machine-showcase ${machineStage}">
                    ${renderMachineIllustration(appState, activeOrder)}
                    <div class="machine-hero-copy">
                        <div class="machine-kpis">
                            <div class="stat-tile accent">
                                <span>Credito</span>
                                <strong>${money(status?.currentCredit)}</strong>
                            </div>
                            <div class="stat-tile">
                                <span>Acqua disponibile</span>
                                <strong>${status?.waterLevelMl ?? 0} ml</strong>
                            </div>
                            <div class="stat-tile">
                                <span>Temperatura boiler</span>
                                <strong>${status?.currentTemperature?.toFixed?.(1) ?? "0"}°C</strong>
                            </div>
                            <div class="stat-tile">
                                <span>Ordine in lavorazione</span>
                                <strong>${activeOrder?.productName ?? "Nessuno"}</strong>
                            </div>
                        </div>
                        <div class="live-panel">
                            <div class="inline-between">
                                <strong>Preparazione live</strong>
                                <span>${activeOrder?.status ?? "Idle"}</span>
                            </div>
                            <div class="progress-track prominent">
                                <div class="progress-bar" style="width:${activeOrder?.progressPercentage ?? 0}%"></div>
                            </div>
                            <p class="muted">${describeMachineMessage(status?.status, activeOrder, appState.dispensedItem)}</p>
                        </div>
                        <div class="dashboard-actions">
                            <div class="inline-between">
                                <strong>Selezione rapida</strong>
                                <span class="muted">${appState.products.length} prodotti</span>
                            </div>
                            <div class="product-chip-grid">
                                ${renderDashboardQuickProducts(appState)}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="card realtime-card">
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
            </div>
        </div>
        <div class="grid three" style="margin-top:18px;">
            <div class="card">
                <h3>Ingredienti principali</h3>
                <div class="resource-list">
                    ${ingredients.map(item => resourceBar(item.name, item.currentLevel, item.capacity, item.unit)).join("")}
                </div>
            </div>
            <div class="card">
                <h3>Prodotti disponibili</h3>
                <div class="list">
                    ${appState.products.slice(0, 5).map(product => `
                        <div class="list-item">
                            <div class="inline-between">
                                <strong>${product.name}</strong>
                                <span>${money(product.price)}</span>
                            </div>
                            <div class="muted">${product.enabled ? "Disponibile" : "Disabilitato"}</div>
                        </div>`).join("")}
                </div>
            </div>
            <div class="card">
                <h3>Consegna prodotto</h3>
                ${renderPickupPanel(appState.dispensedItem)}
            </div>
        </div>
    `;
}

export function renderPurchase(root, appState) {
    root.innerHTML = `
        <div class="grid two">
            <div class="card purchase-card">
                <div class="section-header">
                    <div>
                        <p class="eyebrow">Self Service</p>
                        <h3 class="section-title">Acquisto bevande</h3>
                    </div>
                    <span class="badge ${statusClass(appState.machineStatus?.status)}">${appState.machineStatus?.status ?? "-"}</span>
                </div>
                <div class="control-row" style="margin-bottom:14px;">
                    <button class="btn primary" data-action="add-credit-preset" data-amount="0.5">+ EUR 0,50</button>
                    <button class="btn primary" data-action="add-credit-preset" data-amount="1">+ EUR 1,00</button>
                    <button class="btn secondary" data-action="reset-credit">Reset credito</button>
                </div>
                <div class="list product-list">
                    ${appState.products.map(product => {
                        const unavailableReason = getProductReason(product, appState);
                        return `
                            <div class="list-item product-tile ${unavailableReason ? "disabled" : ""}">
                                <div class="inline-between">
                                    <div>
                                        <strong>${product.name}</strong>
                                        <div class="muted">${money(product.price)}</div>
                                    </div>
                                    <button class="btn ${unavailableReason ? "secondary" : "primary"}" data-action="order-product" data-product-id="${product.id}" ${unavailableReason ? "disabled" : ""}>Ordina</button>
                                </div>
                                <div class="muted">${unavailableReason || "Disponibile per l'ordine."}</div>
                            </div>`;
                    }).join("")}
                </div>
            </div>
            <div class="card">
                <h3>Ordine in corso</h3>
                ${renderActiveOrder(appState)}
                <div class="pickup-summary">
                    <h4>Prodotto pronto</h4>
                    ${renderPickupPanel(appState.dispensedItem, true)}
                </div>
            </div>
        </div>
    `;
}

export function renderMachine(root, appState) {
    const components = appState.machineComponents;
    const status = appState.machineStatus;

    root.innerHTML = `
        <div class="grid two">
            <div class="card">
                <div class="section-header">
                    <h3 class="section-title">Stato generale</h3>
                    <span class="badge ${statusClass(status?.status)}">${status?.status ?? "-"}</span>
                </div>
                <div class="list">
                    ${statusRow("Power", status?.powerOn ? "On" : "Off")}
                    ${statusRow("Boiler", `${components?.boiler?.currentTemperature?.toFixed?.(1) ?? 0}°C`)}
                    ${statusRow("Target", `${components?.boiler?.targetTemperature?.toFixed?.(1) ?? 0}°C`)}
                    ${statusRow("Erogatore", components?.dispensingUnit?.isBusy ? "Busy" : "Idle")}
                    ${statusRow("Maintenance", status?.maintenanceRequired ? "Required" : "OK")}
                </div>
            </div>
            <div class="card">
                <h3>Comandi macchina</h3>
                <div class="control-row">
                    <button class="btn primary" data-action="power-on">Accensione</button>
                    <button class="btn secondary" data-action="power-off">Spegnimento</button>
                    <button class="btn warning" data-action="reset-machine">Reset</button>
                </div>
                <div class="muted" style="margin-top:12px;">Disponibilita operativa globale: ${status?.status ?? "-"}</div>
            </div>
        </div>
    `;
}

export function renderIngredients(root, appState) {
    root.innerHTML = `
        <div class="grid two">
            <div class="card">
                <h3 class="section-title">Ingredienti</h3>
                <div class="resource-list">
                    ${appState.ingredients.map(item => `
                        <div class="resource-item">
                            <div class="resource-item-header">
                                <strong>${item.name}</strong>
                                <button class="btn secondary" data-action="refill-ingredient" data-id="${item.id}">Refill</button>
                            </div>
                            ${resourceBar(item.name, item.currentLevel, item.capacity, item.unit)}
                            <div class="muted">Soglia warning: ${item.lowLevelThreshold} ${item.unit}</div>
                        </div>`).join("")}
                </div>
            </div>
            <div class="card">
                <h3>Serbatoio acqua</h3>
                <div class="resource-item">
                    ${resourceBar("Water", appState.tank?.currentLevelMl ?? 0, appState.tank?.capacityMl ?? 0, "ml")}
                    <div class="control-row" style="margin-top:12px;">
                        <button class="btn primary" data-action="refill-water" data-amount="250">+250 ml</button>
                        <button class="btn primary" data-action="refill-water" data-amount="1000">+1000 ml</button>
                    </div>
                </div>
            </div>
        </div>
    `;
}

export function renderRecipes(root, appState) {
    root.innerHTML = `
        <div class="grid two">
            <div class="card">
                <div class="section-header">
                    <h3 class="section-title">Ricette</h3>
                    <button class="btn primary" data-action="new-recipe">Nuova ricetta</button>
                </div>
                <div class="list">
                    ${appState.recipes.map(recipe => `
                        <div class="list-item">
                            <div class="inline-between">
                                <strong>${recipe.name}</strong>
                                <div>
                                    <button class="btn secondary" data-action="edit-recipe" data-id="${recipe.id}">Modifica</button>
                                    <button class="btn danger" data-action="delete-recipe" data-id="${recipe.id}">Elimina</button>
                                </div>
                            </div>
                            <div class="muted">Target ${recipe.targetTemperature}°C</div>
                            <ul>
                                ${recipe.steps.map(step => `<li>${step.sequence}. ${step.ingredientKey} - ${step.quantity} ${step.unit} - ${step.durationMs} ms</li>`).join("")}
                            </ul>
                        </div>`).join("")}
                </div>
            </div>
            <div class="card">
                <h3>Editor ricetta</h3>
                <form class="form-panel" id="recipe-form">
                    <input type="hidden" name="id">
                    <label>Nome<input name="name" required></label>
                    <label>Temperatura target<input type="number" step="0.1" name="targetTemperature" required></label>
                    <label>Step JSON<textarea name="steps" rows="10" required placeholder='[{"sequence":1,"ingredientKey":"water","quantity":40,"unit":"ml","durationMs":1500}]'></textarea></label>
                    <button class="btn primary" type="submit">Salva ricetta</button>
                </form>
            </div>
        </div>
    `;
}

export function renderProducts(root, appState) {
    root.innerHTML = `
        <div class="grid two">
            <div class="card">
                <div class="section-header">
                    <h3 class="section-title">Prodotti</h3>
                    <button class="btn primary" data-action="new-product">Nuovo prodotto</button>
                </div>
                <table class="table">
                    <thead><tr><th>Nome</th><th>Prezzo</th><th>Ricetta</th><th>Stato</th><th></th></tr></thead>
                    <tbody>
                        ${appState.products.map(product => `
                            <tr>
                                <td>${product.name}</td>
                                <td>${money(product.price)}</td>
                                <td>${safeText(recipeName(product.recipeId, appState.recipes))}</td>
                                <td><span class="badge">${product.enabled ? "Enabled" : "Disabled"}</span></td>
                                <td>
                                    <button class="btn secondary" data-action="edit-product" data-id="${product.id}">Modifica</button>
                                    <button class="btn danger" data-action="delete-product" data-id="${product.id}">Elimina</button>
                                </td>
                            </tr>`).join("")}
                    </tbody>
                </table>
            </div>
            <div class="card">
                <h3>Editor prodotto</h3>
                <form class="form-panel" id="product-form">
                    <input type="hidden" name="id">
                    <label>Nome<input name="name" required></label>
                    <label>Prezzo<input type="number" step="0.01" name="price" required></label>
                    <label>Ricetta
                        <select name="recipeId">${createOptions(appState.recipes, null, item => item.id, item => item.name)}</select>
                    </label>
                    <label>Abilitato
                        <select name="enabled">
                            <option value="true">Si</option>
                            <option value="false">No</option>
                        </select>
                    </label>
                    <button class="btn primary" type="submit">Salva prodotto</button>
                </form>
            </div>
        </div>
    `;
}

export function renderOrders(root, appState) {
    root.innerHTML = `
        <div class="card">
            <div class="section-header">
                <h3 class="section-title">Ordini recenti</h3>
                <span class="muted">${appState.orders.length} ordini</span>
            </div>
            <div class="list">
                ${appState.orders.map(order => `
                    <div class="order-row">
                        <div class="inline-between">
                            <div>
                                <strong>${order.productName}</strong>
                                <div class="muted">${order.id}</div>
                            </div>
                            <span class="badge ${statusClass(order.status)}">${order.status}</span>
                        </div>
                        <div class="muted">Creato: ${relativeTime(order.createdAtUtc)}</div>
                        <div class="muted">Step: ${order.currentStepIndex} - Progresso ${order.progressPercentage}%</div>
                        <div class="muted">${order.failureReason || "Nessun errore"}</div>
                        ${terminalOrderStatuses.includes(order.status) ? "" : `<button class="btn danger" data-action="cancel-order" data-id="${order.id}">Annulla ordine</button>`}
                    </div>`).join("") || '<div class="empty-state">Nessun ordine presente.</div>'}
            </div>
        </div>
    `;
}

export function renderDiagnostics(root, appState) {
    root.innerHTML = `
        <div class="grid two">
            <div class="card">
                <h3 class="section-title">Errori e warning</h3>
                <div class="list">
                    ${[...(appState.diagnostics?.activeErrors ?? []), ...(appState.diagnostics?.activeWarnings ?? [])].map(item => `
                        <div class="list-item">
                            <div class="inline-between">
                                <strong>${item.code}</strong>
                                <span class="badge ${item.severity === "Error" ? "error" : "warning"}">${item.severity}</span>
                            </div>
                            <div class="muted">${item.message}</div>
                        </div>`).join("") || '<div class="empty-state">Nessun allarme attivo.</div>'}
                </div>
            </div>
            <div class="card">
                <h3>Metriche e connessione</h3>
                <div class="list">
                    ${statusRow("Ordini totali", appState.metrics?.totalOrders)}
                    ${statusRow("Completati", appState.metrics?.completedOrders)}
                    ${statusRow("Falliti", appState.metrics?.failedOrders)}
                    ${statusRow("Snapshot save", appState.metrics?.snapshotSaveCount)}
                    ${statusRow("Snapshot restore", appState.metrics?.snapshotRestoreCount)}
                    ${statusRow("Realtime", appState.connectionStatus)}
                </div>
            </div>
        </div>
        <div class="grid two" style="margin-top:18px;">
            <div class="card">
                <h3>Eventi recenti</h3>
                <div class="feed">
                    ${(appState.diagnostics?.recentEvents ?? []).slice(0, 15).map(item => `
                        <div class="feed-item">
                            <div class="inline-between">
                                <strong>${item.category}</strong>
                                <span class="muted">${relativeTime(item.timestampUtc)}</span>
                            </div>
                            <div>${item.message}</div>
                        </div>`).join("")}
                </div>
            </div>
            <div class="card">
                <h3>Log recenti</h3>
                <div class="feed">
                    ${appState.recentLogs.map(line => `<div class="feed-item"><code>${line}</code></div>`).join("") || '<div class="empty-state">Nessun log disponibile.</div>'}
                </div>
            </div>
        </div>
    `;
}

export function renderMaintenance(root, appState) {
    root.innerHTML = `
        <div class="grid two">
            <div class="card">
                <h3 class="section-title">Stato manutenzione</h3>
                <div class="list">
                    ${statusRow("Dispense count", appState.maintenance?.dispenseCount)}
                    ${statusRow("Wear", `${appState.maintenance?.wearPercentage ?? 0}%`)}
                    ${statusRow("Threshold", appState.maintenance?.maintenanceThreshold)}
                    ${statusRow("Required", appState.maintenance?.maintenanceRequired ? "Yes" : "No")}
                    ${statusRow("Last service", relativeTime(appState.maintenance?.lastMaintenanceAtUtc))}
                </div>
            </div>
            <div class="card">
                <h3>Operazioni</h3>
                <div class="control-row">
                    <button class="btn warning" data-action="reset-maintenance">Reset manutenzione</button>
                    <button class="btn secondary" data-action="save-state">Salva snapshot</button>
                    <button class="btn secondary" data-action="reload-state">Reload snapshot</button>
                </div>
            </div>
        </div>
    `;
}

export function renderConfig(root, appState) {
    root.innerHTML = `
        <div class="grid two">
            <div class="card">
                <h3 class="section-title">Configurazione simulazione</h3>
                <form class="form-panel" id="simulation-form">
                    <label>Tick interval ms<input type="number" name="tickIntervalMs" value="${appState.simulationConfig?.tickIntervalMs ?? 500}"></label>
                    <label>Heating rate<input type="number" step="0.1" name="heatingRatePerTick" value="${appState.simulationConfig?.heatingRatePerTick ?? 3}"></label>
                    <label>Cooling rate<input type="number" step="0.1" name="coolingRatePerTick" value="${appState.simulationConfig?.coolingRatePerTick ?? 1.2}"></label>
                    <label>Heating timeout sec<input type="number" name="heatingTimeoutSeconds" value="${appState.simulationConfig?.heatingTimeoutSeconds ?? 90}"></label>
                    <label>Random fault probability<input type="number" step="0.01" name="processFailureProbability" value="${appState.simulationConfig?.processFailureProbability ?? 0.02}"></label>
                    <label>Max boiler temperature<input type="number" step="0.1" name="maximumBoilerTemperature" value="${appState.simulationConfig?.maximumBoilerTemperature ?? 98}"></label>
                    <label>Auto save
                        <select name="autoSaveEnabled">
                            <option value="true" ${appState.simulationConfig?.autoSaveEnabled ? "selected" : ""}>Enabled</option>
                            <option value="false" ${appState.simulationConfig?.autoSaveEnabled === false ? "selected" : ""}>Disabled</option>
                        </select>
                    </label>
                    <label>Auto save sec<input type="number" name="autoSaveIntervalSeconds" value="${appState.simulationConfig?.autoSaveIntervalSeconds ?? 15}"></label>
                    <button class="btn primary" type="submit">Salva configurazione</button>
                </form>
            </div>
            <div class="card">
                <h3>Fault injection e stato</h3>
                <form class="form-panel" id="fault-form">
                    <label>Tipo fault
                        <select name="faultType">
                            <option>PowerFailure</option>
                            <option>HeatingFailure</option>
                            <option>Overheat</option>
                            <option>WaterEmpty</option>
                            <option>IngredientDepleted</option>
                            <option>DispensingFailure</option>
                            <option>MaintenanceLock</option>
                        </select>
                    </label>
                    <label>Messaggio<input name="message" placeholder="Fault injected from dashboard"></label>
                    <button class="btn danger" type="submit">Inietta fault</button>
                </form>
                <div class="control-row" style="margin-top:16px;">
                    <button class="btn secondary" data-action="save-state">Salva JSON</button>
                    <button class="btn secondary" data-action="reload-state">Reload JSON</button>
                    <button class="btn primary" data-action="export-state">Export JSON</button>
                </div>
            </div>
        </div>
    `;
}

function renderMachineIllustration(appState, activeOrder) {
    const status = appState.machineStatus;
    const progress = activeOrder?.progressPercentage ?? 0;
    const stage = getMachineStage(status?.status, activeOrder);
    const activeVesselType = getServingVessel(activeOrder?.productName);
    const activeVesselClass = activeVesselType === "cup" ? "cup" : "glass";
    const activeDrinkClass = `drink-${getDrinkIntensity(activeOrder?.productName)}`;
    const fillHeight = activeOrder ? Math.max(8, progress * 0.72) : 0;
    const dialRotation = activeOrder ? 25 : appState.dispensedItem ? 55 : -35;

    return `
        <div class="modern-machine ${stage}">
            <div class="machine-body">
                <div class="machine-top">
                    <div class="machine-screen">
                        <span class="screen-label">Machine OS</span>
                        <strong>${status?.status ?? "Standby"}</strong>
                        <span>${status?.currentTemperature?.toFixed?.(1) ?? "0"}°C / ${status?.targetTemperature?.toFixed?.(1) ?? "0"}°C</span>
                    </div>
                    <div class="machine-controls">
                        <div class="machine-dial">
                            <div class="machine-dial-core">
                                <span class="machine-dial-indicator" style="transform: translateX(-50%) rotate(${dialRotation}deg);"></span>
                            </div>
                        </div>
                        <span class="control-light green"></span>
                        <span class="control-light amber ${stage === "dispensing" ? "pulsing" : ""}"></span>
                        <span class="control-light red ${stage === "alert" ? "pulsing" : ""}"></span>
                        <div class="machine-keypad">
                            ${Array.from({ length: 9 }, (_, index) => `<span>${index + 1}</span>`).join("")}
                        </div>
                    </div>
                </div>
                <div class="canister-row">
                    <div class="canister"><span>Coffee</span><strong>${percentage(appState.ingredients[0]?.currentLevel ?? 0, appState.ingredients[0]?.capacity ?? 0)}%</strong></div>
                    <div class="canister"><span>Milk</span><strong>${percentage(appState.ingredients[1]?.currentLevel ?? 0, appState.ingredients[1]?.capacity ?? 0)}%</strong></div>
                    <div class="canister"><span>Chocolate</span><strong>${percentage(appState.ingredients[2]?.currentLevel ?? 0, appState.ingredients[2]?.capacity ?? 0)}%</strong></div>
                </div>
                <div class="dispense-bay">
                    <div class="brew-group">
                        <div class="nozzle-head left"></div>
                        <div class="nozzle-head right"></div>
                    </div>
                    <div class="pour-stream ${activeOrder ? "visible" : ""}"></div>
                    <div class="steam-group ${activeOrder || appState.dispensedItem ? "visible" : ""}">
                        <span class="steam-line left"></span>
                        <span class="steam-line right"></span>
                    </div>
                    <div class="cup-slot">
                        <div class="vessel ${activeVesselClass} ${activeOrder ? "filling" : "idle"}">
                            <div class="vessel-fill ${activeDrinkClass}" style="height:${fillHeight}%"></div>
                            ${activeVesselType === "cup" ? '<div class="cup-handle"></div>' : ""}
                        </div>
                    </div>
                    <div class="pickup-stage ${appState.dispensedItem ? "loaded" : ""}">
                        ${appState.dispensedItem
                            ? `<div class="pickup-product ${appState.dispensedItem.vessel} drink-${appState.dispensedItem.intensity}">
                                    <div class="pickup-fill"></div>
                                    ${appState.dispensedItem.vessel === "cup" ? '<div class="cup-handle"></div>' : ""}
                               </div>`
                            : '<span class="pickup-placeholder">Vano consegna</span>'}
                    </div>
                </div>
                <div class="machine-base">
                    <span>Office Series 24</span>
                    <span>${appState.machineComponents?.dispensingUnit?.isBusy ? "Dispensing" : "Ready"}</span>
                </div>
            </div>
        </div>
    `;
}

function renderPickupPanel(dispensedItem, compact = false) {
    if (!dispensedItem) {
        return `<div class="empty-state ${compact ? "compact" : ""}">Nessun prodotto pronto nel vano di ritiro.</div>`;
    }

    return `
        <div class="pickup-panel ${compact ? "compact" : ""}">
            <div class="pickup-panel-visual">
                <div class="pickup-product ${dispensedItem.vessel} drink-${dispensedItem.intensity}">
                    <div class="pickup-fill"></div>
                    ${dispensedItem.vessel === "cup" ? '<div class="cup-handle"></div>' : ""}
                </div>
            </div>
            <div class="pickup-panel-copy">
                <strong>${dispensedItem.productName}</strong>
                <div class="muted">Erogazione completata: pronto per il ritiro.</div>
                <button class="btn primary" data-action="pickup-product">Ritira prodotto</button>
            </div>
        </div>
    `;
}

function renderDashboardQuickProducts(appState) {
    return appState.products.map(product => {
        const unavailableReason = getProductReason(product, appState);
        return `
            <button
                class="product-chip ${unavailableReason ? "disabled" : ""}"
                data-action="order-product"
                data-product-id="${product.id}"
                ${unavailableReason ? "disabled" : ""}>
                <strong>${product.name}</strong>
                <span>${money(product.price)}</span>
            </button>`;
    }).join("") || '<div class="empty-state compact">Nessun prodotto disponibile.</div>';
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

function renderActiveOrder(appState) {
    const order = getActiveOrder(appState);
    if (!order) {
        return '<div class="empty-state">Nessun ordine attivo.</div>';
    }

    return `
        <div class="list-item">
            <div class="inline-between">
                <strong>${order.productName}</strong>
                <span class="badge ${statusClass(order.status)}">${order.status}</span>
            </div>
            <div class="progress-track prominent" style="margin:12px 0;">
                <div class="progress-bar" style="width:${order.progressPercentage}%"></div>
            </div>
            <div class="muted">Step corrente: ${order.currentStepIndex}</div>
            <div class="muted">${order.failureReason || "Preparazione in corso."}</div>
        </div>`;
}

function getActiveOrder(appState) {
    return appState.orders.find(item => !terminalOrderStatuses.includes(item.status));
}

function getMachineStage(machineStatus, activeOrder) {
    const normalizedStatus = String(machineStatus || "").toLowerCase();
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
    if (dispensedItem) {
        return `${dispensedItem.productName} pronto nel vano di consegna. Usa il pulsante di ritiro per liberare la macchina.`;
    }
    if (activeOrder) {
        return `La macchina sta riempiendo il contenitore con ${activeOrder.productName}. Il livello cresce in tempo reale fino al completamento.`;
    }
    if (String(machineStatus || "").toLowerCase().includes("heat")) {
        return "Il boiler sta raggiungendo la temperatura target prima di avviare l'erogazione.";
    }
    return "La macchina e pronta a simulare un nuovo ordine e visualizzare il flusso di erogazione.";
}

function getServingVessel(productName) {
    const normalizedName = String(productName || "").toLowerCase();
    return normalizedName.includes("espresso") || normalizedName.includes("ristretto") ? "cup" : "glass";
}

function getDrinkIntensity(productName) {
    const normalizedName = String(productName || "").toLowerCase();
    if (normalizedName.includes("latte") || normalizedName.includes("milk") || normalizedName.includes("macchiato")) {
        return "light";
    }
    if (normalizedName.includes("chocolate") || normalizedName.includes("mocha")) {
        return "dark";
    }
    return "classic";
}

function recipeName(recipeId, recipes) {
    return recipes.find(item => item.id === recipeId)?.name;
}

function statusRow(label, value) {
    return `<div class="status-row"><div class="muted">${label}</div><strong>${safeText(value)}</strong></div>`;
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

import { pushEvent, setState } from "./state.js";

export async function connectRealtime(onEvent) {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/machine")
        .withAutomaticReconnect()
        .build();

    const bind = eventName => {
        connection.on(eventName, payload => {
            pushEvent({ type: eventName, message: formatMessage(eventName, payload), payload });
            onEvent(eventName, payload);
        });
    };

    [
        "MachineStateChanged",
        "TemperatureChanged",
        "IngredientLevelChanged",
        "CreditChanged",
        "OrderCreated",
        "OrderStatusChanged",
        "DispensingProgressChanged",
        "ErrorRaised",
        "ErrorResolved",
        "MaintenanceStatusChanged",
        "ProductAvailabilityChanged",
        "SnapshotSaved",
        "SnapshotRestored",
        "ConfigurationChanged"
    ].forEach(bind);

    connection.onreconnecting(() => setState({ connectionStatus: "reconnecting" }));
    connection.onreconnected(() => setState({ connectionStatus: "connected" }));
    connection.onclose(() => setState({ connectionStatus: "disconnected" }));

    await connection.start();
    setState({ connectionStatus: "connected" });
    pushEvent({ type: "Realtime", message: "Connessione realtime attiva." });
    return connection;
}

function formatMessage(eventName, payload) {
    switch (eventName) {
        case "MachineStateChanged":
            return `Stato macchina: ${payload.status}`;
        case "TemperatureChanged":
            return `Temperatura aggiornata: ${payload.currentTemperature?.toFixed?.(1) ?? payload.currentTemperature}°C`;
        case "CreditChanged":
            return "Credito aggiornato";
        case "OrderCreated":
            return `Ordine creato: ${payload.productName ?? payload.id}`;
        case "OrderStatusChanged":
            return `Ordine ${payload.id ?? ""}: ${payload.status}`;
        case "DispensingProgressChanged":
            return `Erogazione in corso: ${payload.progressPercentage ?? 0}%`;
        case "ErrorRaised":
            return `Errore: ${payload.message ?? payload.code ?? "fault raised"}`;
        case "ErrorResolved":
            return `Errore risolto: ${payload.code ?? ""}`;
        case "SnapshotSaved":
            return "Snapshot salvato";
        case "SnapshotRestored":
            return "Snapshot ripristinato";
        default:
            return eventName;
    }
}

# CoffeeMachine Digital Twin

Digital twin di una coffee vending machine da ufficio costruito con ASP.NET Core, SignalR e frontend vanilla JavaScript.

Il progetto simula il comportamento di una macchina per bevande calde con:
- stato macchina e componenti fisiche virtuali
- ordini e avanzamento ricette
- credito e transazioni
- ingredienti, acqua e consumi
- diagnostica, fault injection e manutenzione
- persistenza snapshot su file JSON
- dashboard web con aggiornamenti realtime

## Overview

Il repository contiene un'applicazione full-stack composta da:
- backend ASP.NET Core Web API
- frontend statico servito da `wwwroot`
- canale realtime con SignalR
- simulation loop eseguito in background
- test unitari e di integrazione

Il progetto e pensato come base solida per scenari di:
- simulazione didattica
- digital twin di una vending machine
- demo architetturale layered
- futura evoluzione verso persistenza SQL e multi-machine

## Feature principali

- Simulazione del ciclo macchina: heating, dispensing, mixing, completamento ordine.
- Catalogo prodotti e ricette modificabile via API.
- Gestione credito con ricarica e tracciamento transazioni.
- Gestione ingredienti, acqua e refill.
- Fault injection e diagnostica attiva.
- Snapshot di stato con save, reload ed export.
- Log applicativi e metriche di runtime.
- Dashboard web moderna con stato realtime della macchina.

## Stack tecnologico

- `.NET 9`
- `ASP.NET Core Web API`
- `SignalR`
- `Vanilla JavaScript`
- `HTML / CSS`
- `Serilog`
- `xUnit + FluentAssertions`

## Avvio rapido

### Prerequisiti

- `.NET SDK 9.0`

### Esecuzione locale

```powershell
dotnet restore
dotnet build CoffeeMachine.sln
dotnet run --project .\src\CoffeeMachine.Api\CoffeeMachine.Api.csproj
```

L'applicazione usa di default:

- URL locale: `http://localhost:5184`
- frontend statico servito direttamente dall'API
- hub realtime: `/hubs/machine`

Apri il browser sull'URL:

```powershell
Start-Process "http://localhost:5184"
```

## Utilizzo rapido

Flusso tipico:

1. accendi la macchina
2. inserisci credito
3. seleziona un prodotto
4. osserva l'avanzamento realtime
5. ritira il prodotto dalla dashboard

Esempi API:

```http
POST /api/machine/power/on
```

```http
POST /api/credit/add
Content-Type: application/json

{
  "amount": 1.00,
  "description": "Top up"
}
```

```http
POST /api/orders
Content-Type: application/json

{
  "productId": "espresso"
}
```

## Seed iniziale

All'avvio, se non esiste uno snapshot valido, il sistema usa un seed iniziale con:

### Prodotti

- `Espresso` - `EUR 0.50`
- `Cappuccino` - `EUR 0.80`
- `Hot Chocolate` - `EUR 1.20`

### Ingredienti

- `Coffee`
- `Milk`
- `Chocolate`
- `Sugar`

## Architettura

Il progetto e organizzato in layer:

- `CoffeeMachine.Domain`
  - entita, enum, value objects e regole di dominio
- `CoffeeMachine.Application`
  - use case, DTO, interfacce e servizi applicativi
- `CoffeeMachine.Infrastructure`
  - repository runtime, persistenza JSON, simulation engine, SignalR, hosted services
- `CoffeeMachine.Api`
  - controller REST, bootstrap ASP.NET Core e frontend statico
- `CoffeeMachine.Tests`
  - test unitari e integration test

### Repository layout

```text
CoffeeMachine.sln
README.md
src/
  CoffeeMachine.Api/
  CoffeeMachine.Application/
  CoffeeMachine.Domain/
  CoffeeMachine.Infrastructure/
tests/
  CoffeeMachine.Tests/
```

### Frontend

Non esiste una build frontend separata. La GUI vive in:

```text
src/CoffeeMachine.Api/wwwroot/
  index.html
  styles/main.css
  scripts/
    api.js
    dashboard.js
    main.js
    realtime.js
    state.js
    ui.js
    utils.js
    views.js
```

## Come funziona la simulazione

La simulazione gira nel background service `SimulationBackgroundService`.

A ogni tick il motore:
- aggiorna temperatura boiler e raffreddamento
- verifica timeout di riscaldamento e overheat
- avanza l'ordine attivo passo per passo
- consuma ingredienti e acqua secondo la ricetta
- aggiorna warning e diagnostica
- aggiorna stato macchina e componenti
- pubblica eventi realtime

### Stati macchina

- `Off`
- `Initializing`
- `Ready`
- `Heating`
- `Dispensing`
- `OutOfService`
- `MaintenanceRequired`
- `Error`

### Stati ordine

- `Pending`
- `Validating`
- `WaitingForHeat`
- `DispensingIngredient`
- `Mixing`
- `Completed`
- `Failed`
- `Cancelled`

## API principali

### Machine

- `GET /api/machine/status`
- `GET /api/machine/diagnostics`
- `GET /api/machine/components`
- `POST /api/machine/power/on`
- `POST /api/machine/power/off`
- `POST /api/machine/reset`
- `POST /api/machine/maintenance/reset`

### Credit

- `GET /api/credit`
- `POST /api/credit/add`
- `POST /api/credit/reset`
- `GET /api/transactions`

### Products

- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products`
- `PUT /api/products/{id}`
- `DELETE /api/products/{id}`

### Recipes

- `GET /api/recipes`
- `GET /api/recipes/{id}`
- `POST /api/recipes`
- `PUT /api/recipes/{id}`
- `DELETE /api/recipes/{id}`

### Ingredients e water

- `GET /api/ingredients`
- `GET /api/ingredients/{id}`
- `PUT /api/ingredients/{id}`
- `POST /api/ingredients/{id}/refill`
- `GET /api/tanks`
- `GET /api/tanks/status`
- `POST /api/water/refill`

### Orders

- `POST /api/orders`
- `GET /api/orders`
- `GET /api/orders/{id}`
- `GET /api/orders/{id}/progress`
- `POST /api/orders/{id}/cancel`

### Config, diagnostics e state

- `GET /api/config`
- `PUT /api/config`
- `GET /api/config/simulation`
- `PUT /api/config/simulation`
- `GET /api/logs/recent`
- `GET /api/errors/active`
- `GET /api/warnings/active`
- `GET /api/maintenance/status`
- `GET /api/metrics`
- `POST /api/faults/inject`
- `POST /api/state/save`
- `POST /api/state/reload`
- `GET /api/state/export`

## Realtime

Hub SignalR:

- `/hubs/machine`

Eventi principali:

- `MachineStateChanged`
- `TemperatureChanged`
- `IngredientLevelChanged`
- `CreditChanged`
- `OrderCreated`
- `OrderStatusChanged`
- `DispensingProgressChanged`
- `ErrorRaised`
- `ErrorResolved`
- `MaintenanceStatusChanged`
- `ProductAvailabilityChanged`
- `SnapshotSaved`
- `SnapshotRestored`
- `ConfigurationChanged`

## Persistenza e log

Lo stato runtime e mantenuto in memoria, ma puo essere salvato e ripristinato tramite snapshot JSON.

Percorsi principali:

- snapshot: `src/CoffeeMachine.Api/data/machine-state.json`
- log: `src/CoffeeMachine.Api/logs/coffee-machine-*.log`

Configurazione di default:

```json
{
  "Persistence": {
    "SnapshotPath": "data/machine-state.json"
  },
  "Logging": {
    "File": {
      "Path": "logs/coffee-machine-.log"
    }
  }
}
```

## Test

Esegui tutta la suite con:

```powershell
dotnet test CoffeeMachine.sln
```

I test coprono:
- dominio
- servizi applicativi
- simulation engine
- persistenza JSON
- integrazione HTTP minima

## Possibili evoluzioni

- persistenza SQLite o database relazionale
- storico telemetria e replay eventi
- analytics e reporting
- multi-machine / fleet management
- autenticazione per area operatore e manutentore
- hardening di concorrenza, idempotenza e audit trail

## Note

- Il frontend e servito direttamente dalla Web API.
- OpenAPI e abilitato lato applicazione.
- Il progetto puo essere usato sia come demo architetturale sia come base per un simulatore piu esteso.

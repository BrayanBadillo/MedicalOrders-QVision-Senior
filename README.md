# MedicalOrders-QVision-Senior API de órdenes médicas
PRUEBA TÉCNICA SENIOR .NET

API REST en **.NET 10** para registrar órdenes médicas y procesarlas de forma **asíncrona** mediante un
**Worker Service**. Construida con **Clean Architecture**, **CQRS**, **Repository** y **Unit of Work**.

```
POST /api/orders ──► orden en estado Pendiente ──► Worker (polling) ──► EnProceso ──► Procesada | Fallida
```

## Requisitos

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* (Opcional) `sqlite3` para ejecutar el script DDL manualmente y herramienta `dotnet-ef` para migraciones:
  `dotnet tool install --global dotnet-ef --version 10.0.401`

## Ejecución rápida

```bash
git clone <url-del-repositorio>
cd MedicalOrders

dotnet restore
dotnet build

# Terminal 1 — API (crea la base de datos y aplica migraciones al arrancar)
dotnet run --project src/MedicalOrders.Api

# Terminal 2 — Worker (arrancar DESPUÉS de la API)
dotnet run --project src/MedicalOrders.Worker
```

* Swagger UI: <http://localhost:5080/swagger> (también en `/`)
* Health check: <http://localhost:5080/health>
* Ejemplos de peticiones: [`docs/requests.http`](docs/requests.http)

### Probar el flujo completo

```bash
curl -s -X POST http://localhost:5080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"patientId":"12345","patientName":"Juan Perez","serviceCode":"LAB001","serviceDescription":"Hemograma","priority":"Normal"}'
# → 201 Created, "status": "Pendiente"

# Pasados unos segundos (el Worker consulta cada 5 s y simula 2 s de procesamiento):
curl -s "http://localhost:5080/api/orders?patientId=12345"
# → "status": "Procesada"
```

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/orders` | Registra una orden (estado inicial `Pendiente`). `201` + header `Location`. |
| `GET` | `/api/orders` | Lista órdenes. Filtros: `patientId`, `status`. Paginación: `page` (1), `pageSize` (20, máx. 100). |
| `GET` | `/api/orders/{id}` | Detalle de una orden. `404` si no existe. |

**Reglas de validación**: `patientId` y `serviceCode` obligatorios; `priority` ∈ {`Normal`, `Urgente`}.
**Estados**: `Pendiente` → `EnProceso` → `Procesada` (o `Fallida` si el procesamiento lanza una excepción).
Los errores se devuelven como `application/problem+json` (RFC 7807) con `traceId` y `correlationId`.

La especificación OpenAPI está en [`docs/swagger.json`](docs/swagger.json) y, con la API en ejecución,
en `http://localhost:5080/swagger/v1/swagger.json`.

## Configuración

| Qué | Dónde | Valor por defecto |
|---|---|---|
| Cadena de conexión | `ConnectionStrings:Default` en `appsettings.json` de **Api** y **Worker** | `Data Source=database/orders.db` |
| Puerto de la API | `src/MedicalOrders.Api/Properties/launchSettings.json` o `ASPNETCORE_URLS` | `http://localhost:5080` |
| Aplicar migraciones al iniciar la API | `Database:MigrateOnStartup` | `true` |
| Carpeta de logs | `FileLogging:Directory` | `logs` |
| Lote / intervalo del Worker | `Worker:BatchSize`, `Worker:PollingIntervalSeconds` | `10` / `5` |
| Duración simulada del procesamiento | `SimulatedProcessing:DelayMilliseconds` | `2000` |

Cualquier valor puede sobrescribirse con variables de entorno, por ejemplo:
`ConnectionStrings__Default="Data Source=/ruta/orders.db"` o `Worker__PollingIntervalSeconds=2`.

### Cadena de conexión

SQLite (por defecto): `Data Source=database/orders.db`. Las rutas relativas se resuelven contra la raíz
de la solución, así **la API y el Worker usan siempre el mismo archivo** sin importar desde dónde se ejecuten.
La base de datos usa el modo **WAL** para permitir lecturas/escrituras concurrentes entre ambos procesos.

## Base de datos

* **Modelo**: tabla `Orders` (ver [`database/schema.sql`](database/schema.sql)) con índices por
  `PatientId` y `(Status, Priority, CreatedAt)`.
* **Migraciones** (EF Core) en `src/MedicalOrders.Infrastructure/Persistence/Migrations`. La API las
  aplica automáticamente al iniciar. Comandos manuales:

```bash
# aplicar
dotnet ef database update -p src/MedicalOrders.Infrastructure -s src/MedicalOrders.Api

# crear una nueva migración tras cambiar el modelo
dotnet ef migrations add NombreMigracion -p src/MedicalOrders.Infrastructure -s src/MedicalOrders.Api \
  -o Persistence/Migrations

# regenerar el script DDL
dotnet ef migrations script --idempotent -p src/MedicalOrders.Infrastructure -s src/MedicalOrders.Api \
  -o database/schema.sql
```

* **Script DDL**: alternativa sin EF — `sqlite3 database/orders.db < database/schema.sql`
  (registra también la migración inicial, por lo que es compatible con el arranque de la API).

## Logging

Serilog escribe en **consola** y en **archivos con rotación diaria** (se conservan 14 días):

* `logs/api-YYYYMMDD.log`
* `logs/worker-YYYYMMDD.log`

Se registran: creación de órdenes (id, paciente, servicio, prioridad), cada cambio de estado
(`Pendiente → EnProceso → Procesada/Fallida`), errores y excepciones con *stack trace*, y las solicitudes
HTTP. Cada solicitud lleva un `X-Correlation-Id` (se acepta del cliente o se genera) presente en los logs
y en la respuesta. El nombre del paciente **no** se escribe en los logs.

Evidencia de ejecución (logs reales de API y Worker): [`logs/evidence/`](logs/evidence/).

## Tests

```bash
dotnet test
```

* **Domain.Tests**: creación válida, rechazo de `PatientId`/`ServiceCode` vacíos y prioridad inválida,
  transiciones de estado válidas e inválidas.
* **Application.Tests**: validador del comando, pipeline de validación, handler de creación y handler
  del Worker (procesada, fallida, conflicto de concurrencia).

## Estructura

```
src/
  MedicalOrders.Api/             Controllers, middleware global de excepciones, Swagger
  MedicalOrders.Application/     CQRS (MediatR), validadores, interfaces (IOrderRepository, IUnitOfWork)
  MedicalOrders.Domain/          Entidad Order, enums, DomainException
  MedicalOrders.Infrastructure/  EF Core + SQLite, repositorio, UnitOfWork, migraciones, logging
  MedicalOrders.Worker/          BackgroundService que procesa órdenes pendientes
tests/                             Pruebas unitarias (xUnit, FluentAssertions, NSubstitute)
database/schema.sql                Script DDL
docs/                              swagger.json, requests.http
```

Las decisiones de diseño y la evolución propuesta (cola + Outbox, recuperación de órdenes atascadas,
idempotencia) están en [`ARCHITECTURE.md`](ARCHITECTURE.md).

## Notas de diseño

* **Worker seguro con varias instancias**: el `Status` es un *concurrency token*; reclamar una orden
  (`Pendiente → EnProceso`) solo tiene éxito para un proceso. Los conflictos se omiten y se registran.
* **Prioridad**: el Worker toma primero las órdenes `Urgente` y luego las más antiguas.
* **Resiliencia**: un error en una orden la marca `Fallida` (con el motivo) sin detener el lote;
  un error de ciclo se registra y se reintenta en el siguiente intervalo.
* **Limitación conocida**: si el Worker se apaga abruptamente a mitad de una orden, esta queda en
  `EnProceso` (ver recuperación propuesta en `ARCHITECTURE.md`).

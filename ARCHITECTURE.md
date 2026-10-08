# Decisiones de arquitectura

## Capas (Clean Architecture)

```
Api ───────────┐
Worker ────────┼──► Application ──► Domain
Infrastructure ┘        ▲
      └─────────────────┘  (Infrastructure implementa las interfaces definidas en Application)
```

* **Domain**: `Order` (entidad rica), enums y `DomainException`. Sin dependencias.
* **Application**: CQRS con MediatR, validación con FluentValidation (pipeline behavior), interfaces
  `IOrderRepository`, `IUnitOfWork`, `IOrderProcessor`. Contiene también el caso de uso del Worker
  (`ProcessPendingOrdersCommand`), por lo que la lógica es testeable sin infraestructura.
* **Infrastructure**: EF Core + SQLite, repositorio, unit of work, procesador simulado, logging.
* **Api / Worker**: composición, controllers, middleware, hosting. Sin lógica de negocio.

## Decisiones y compromisos

| Decisión | Motivo | Compromiso |
|---|---|---|
| Polling a la base de datos en el Worker | Simple y suficiente para el alcance; sin infraestructura extra | Latencia = intervalo de polling |
| Estado como *concurrency token* | Reclamo atómico de órdenes (`UPDATE … WHERE Status = 'Pendiente'`); soporta varias instancias del Worker | Una orden en `EnProceso` tras un apagado abrupto requiere recuperación (ver abajo) |
| Reclamo + procesamiento en dos *saves* | El estado `EnProceso` es visible y trazable | Un guardado más por orden |
| `IOrderProcessor` como puerto | La integración real se reemplaza sin tocar el caso de uso | — |
| `TimeProvider` inyectado | Pruebas deterministas | — |
| SQLite + WAL | Cero configuración; API y Worker comparten archivo | Escrituras concurrentes limitadas; en producción usar SQL Server/PostgreSQL |
| La API aplica las migraciones | Un único responsable del esquema | El Worker espera (reintenta) si arranca antes |

## Evolución propuesta

1. **Patrón Outbox + cola** (RabbitMQ / Azure Service Bus): al crear la orden se guarda un mensaje
   en una tabla `Outbox` dentro de la misma transacción; un publicador lo envía al broker y los
   consumidores reemplazan el polling.
2. **Recuperación de órdenes atascadas**: marcar como `Pendiente` de nuevo las órdenes en
   `EnProceso` con `ProcessingStartedAt` mayor a un umbral, y contar reintentos con un máximo.
3. **Idempotencia** en `POST /api/orders` con una cabecera `Idempotency-Key`.
4. **Observabilidad**: OpenTelemetry (trazas y métricas) además de logs.
5. **Datos sensibles**: los logs registran `PatientId` pero no el nombre del paciente; en producción
   evaluar cifrado/enmascaramiento según la normativa aplicable.

# Roadmap: Distribution Multi-Runtime Para KnOwl

## Contexto
KnOwl qued� con una simplificaci�n incorrecta: un release despliega directamente a un �nico storage runtime (`RuntimeContractArtifacts`). Ese modelo no corresponde al flujo de Orchestrator. En Orchestrator, Distribution trabaja con runtime nodes, modos de entrega push/pull/hybrid y targets por runtime. El Runtime Host es un proyecto aparte que recibe o consulta artifacts, no una secci�n operativa dentro del host principal.

## Objetivo
Alinear KnOwl con el modelo de Distribution de Orchestrator para soportar multiples runtimes. El deploy de una version de Event/Command debe generar artifacts automaticamente, y los releases deben crear planes/targets de entrega por runtime node. El runtime host conservara su propio storage y recibira/pulleara artifacts desde Distribution.

## Principios
- Las versiones de Events y Commands conservan su lifecycle: `Draft -> InReview -> Approved -> Deployed -> Deprecated -> Archived`.
- Al mover una version a `Deployed`, se genera automaticamente su `ContractArtifact`.
- Un release agrupa artifacts y crea targets de distribucion hacia uno o mas runtime nodes.
- Un release no tiene flujo de aprobacion. No debe tener `InReview` ni `Approved` como estados funcionales.
- Runtime no es unico. Todo despliegue debe apuntar a runtime nodes.
- Runtime Host es un host/proyecto aparte. KnOwl no debe asumir que puede escribir directamente al storage runtime final como destino unico.
- Nada se borra fisicamente; se desactiva, cancela o archiva segun aplique.

## Modelo Target

### RuntimeNode
Crear entidad de Distribution:
- `Id`
- `Name`
- `Code`
- `EnvironmentName`
- `DistributionMode`: `Push`, `Pull`, `Hybrid`
- `EndpointBaseUri`
- `EndpointApiPath`
- `AuthenticationMode`: `None`, `ClientCredentials`, `ApiKey`
- `ClientId`
- `SecretReference`
- `ApiKeyReference`
- `Status`: `Active`, `Disabled`, `Revoked`
- `IsEnabled`
- `Description`
- `RegisteredAtUtc`
- `LastUpdatedAtUtc`

### ContractRelease
Ajustar semantica:
- `Draft`
- `InProgress`
- `Completed`
- `Failed`
- `Cancelled`

Compatibilidad:
- Mantener lectura temporal de estados legacy `InReview` y `Approved` si existen datos anteriores.
- No generar nuevos releases con estados legacy.
- Preparar script SQL para normalizar releases legacy si conviene.

### ContractReleaseItem
Mantener bundle de artifacts:
- `ReleaseId`
- `ArtifactId`
- `CreatedAtUtc`

### ContractReleaseTarget
Crear entidad nueva:
- `Id`
- `ReleaseId`
- `ReleaseItemId`
- `RuntimeNodeId`
- `ArtifactId`
- `RolloutGroup`
- `Status`: `Pending`, `AvailableForPull`, `PushScheduled`, `InProgress`, `Delivered`, `Acknowledged`, `Activated`, `Failed`, `Cancelled`
- `ActivationStatus`: `NotActivated`, `Activating`, `Activated`, `ActivationFailed`
- `AssignedAtUtc`
- `AvailableAtUtc`
- `DeliveredAtUtc`
- `AcknowledgedAtUtc`
- `ActivatedAtUtc`
- `FailedAtUtc`
- `FailureReason`
- `RuntimeVersionApplied`
- `CorrelationId`

### RuntimeContractArtifacts
Revisar ubicacion:
- No debe representar el runtime final dentro del host principal de KnOwl.
- Si se mantiene en el repo, debe pertenecer al Runtime Host/storage runtime separado.
- La UI principal de KnOwl no debe exponerlo como seccion operativa de Async Contracts.

## Fases De Implementacion

### Fase 1: Modelo De Distribution Multi-Runtime
- Agregar enums de Distribution: `RuntimeNodeStatus`, `DistributionMode`, `RuntimeAuthenticationMode`, `ReleaseTargetStatus`, `ActivationStatus`.
- Agregar entidad `RuntimeNode`.
- Agregar entidad `ContractReleaseTarget`.
- Ajustar `ContractReleaseStatus` a estados de release reales.
- Mantener estados legacy solo para compatibilidad de lectura.
- Agregar repositorios:
  - `IRuntimeNodeRepository`
  - `IContractReleaseTargetRepository`
- Implementar storage SQL Server.
- Crear migracion EF.
- Generar script SQL ordenado.
- Verificar con `dotnet build`.

### Fase 2: Release Planning
- Cambiar `ContractReleaseInteractionService` para que al ejecutar un release cree targets por runtime node seleccionado.
- Permitir seleccionar runtime nodes al crear/ejecutar release.
- Validar que todos los artifacts seleccionados existan.
- Validar runtime nodes activos/habilitados.
- Para cada artifact/runtime node crear `ContractReleaseTarget`.
- Estado inicial por modo:
  - `Pull` -> `AvailableForPull` con `AvailableAtUtc`.
  - `Push` -> `PushScheduled`.
  - `Hybrid` -> `PushScheduled` y tambien puede exponerse para pull si falla push, segun siguiente fase.
- Cambiar release a `InProgress` al ejecutar.
- Marcar `Completed` cuando todos los targets esten `Activated` o ya entregados correctamente.
- Marcar `Failed` si todos fallan o si la politica decide bloquear.
- Agregar pruebas unitarias.

### Fase 3: Artifact Delivery Service
- Crear `IArtifactDeliveryInteractionService` equivalente al de Orchestrator.
- Implementar operaciones:
  - `Push(releaseTargetId, initiatedBy)`.
  - `GetPendingForPull(runtimeNodeId)`.
  - `AcknowledgePull(runtimeNodeId, releaseTargetId, runtimeArtifactId)`.
- Para `Push`, construir package del artifact y enviarlo al endpoint configurado del runtime node.
- Para `Pull`, retornar packages pendientes del runtime node.
- Para acknowledge, marcar target como `Activated`/`Acknowledged`.
- Registrar attempts si se agrega tabla de intentos.
- Agregar pruebas unitarias.

### Fase 4: Runtime Host Boundary
- Quitar la seccion `Runtime` del menu principal de KnOwl o esconderla como diagnostico interno.
- Preparar endpoints de Distribution para runtime pull:
  - `GET /distribution/runtime-nodes/{runtimeNodeId}/artifacts/pending`
  - `POST /distribution/runtime-nodes/{runtimeNodeId}/artifacts/{releaseTargetId}/ack`
- Preparar contrato HTTP para push:
  - runtime recibe artifact package desde Distribution.
- Confirmar que Runtime Host maneja su propio storage runtime.
- No escribir desde KnOwl principal directo a `RuntimeContractArtifacts` como destino unico.

### Fase 5: UI Minima Para Pruebas
- Agregar seccion `Runtime Nodes` dentro de Distribution/Async Contracts.
- CRUD minimo sin delete fisico:
  - crear runtime node
  - editar runtime node
  - habilitar/deshabilitar
  - cambiar status
- En Release:
  - seleccionar artifacts
  - seleccionar runtime nodes destino
  - ejecutar release
  - ver targets por runtime con status
  - boton manual `Push` para targets `PushScheduled` o `Failed` si aplica
- Mantener UI sencilla por ahora; el pulido visual queda para despues.

### Fase 6: SQL Y Compatibilidad
- Crear migracion EF para tablas nuevas.
- Generar script SQL incremental.
- Si existen releases con `Approved`/`InReview`, preparar script de normalizacion:
  - `Approved` -> `Draft` o `InProgress`, segun regla final.
  - `InReview` -> `Draft`.
- No eliminar columnas historicas si eso complica rollback inmediato; pueden quedar sin uso y retirarse despues con migracion controlada.

### Fase 7: Tests
- `dotnet build`.
- `dotnet test`.
- Tests clave:
  - Deploy de version genera artifact automaticamente.
  - Crear release con varios artifacts.
  - Ejecutar release contra varios runtime nodes.
  - Runtime pull genera targets `AvailableForPull`.
  - Runtime push genera targets `PushScheduled` y actualiza status.
  - Acknowledge cambia target a `Activated`.
  - Release no usa `InReview`/`Approved`.
  - Runtime node disabled/revoked no recibe targets.

## Orden Sugerido De Commits
1. `Add multi-runtime distribution model`
2. `Add runtime node and release target storage`
3. `Create release plans for runtime nodes`
4. `Add artifact delivery push pull services`
5. `Update release and runtime node UI`
6. `Add multi-runtime distribution migrations`
7. `Add multi-runtime distribution tests`
8. `Remove runtime singleton assumptions from KnOwl UI`

## Riesgos
- Hay migraciones ya aplicadas con `ContractReleases` y `RuntimeContractArtifacts`; hay que mantener compatibilidad y evitar romper datos locales/productivos.
- Si existen releases creados con estados legacy, el enum debe poder leerlos hasta que se normalicen.
- Push real requiere definir autenticacion y endpoint del Runtime Host. Si no esta listo, se puede dejar el target en `PushScheduled` sin llamada HTTP inicial.
- Hybrid necesita una regla explicita: push primero y pull fallback, o push y pull disponible al mismo tiempo.

## Decision Pendiente
Definir comportamiento exacto de `Hybrid` en KnOwl:
- Opcion A: programar push y dejar disponible para pull solo si push falla.
- Opcion B: programar push y dejar disponible para pull desde el inicio.

Recomendacion inicial: Opcion A, porque evita doble entrega accidental.

# WARBackend

# Combat System Architecture

## 1. Vision general del sistema de combate

El combate de WAR se resuelve a partir de un `CombatEventResolver`, que actua como orquestador del evento pero delega el trabajo real en servicios especializados. La arquitectura separa claramente calculo de magnitud, validacion de recursos, checks probabilisticos, resolucion de condiciones e interacciones, y proyeccion de recursos, para que el pipeline pueda crecer sin convertir el combate en una sola clase monolitica.

El pipeline actual contempla estas etapas:

1. resolucion de magnitud de accion
2. validacion de costos de recursos
3. hit check
4. critical check
5. calculo de dano
6. mitigacion
7. reducciones del receptor
8. modificadores por estado
9. interacciones especiales
10. aplicacion de condiciones
11. proyeccion de recursos

## 2. Combat Event Pipeline

El flujo parte de un `CombatEventContext` y termina en un `CombatResolutionResult`.

1. `CombatActionMagnitudeService` resuelve la magnitud base de la accion a partir de una base fija y, opcionalmente, de `PhysicalAttack` o `MagicAttack`.
2. `CombatActionResourceService` valida si el actor puede pagar los costos declarados y proyecta el gasto si la accion es viable.
3. Si la accion no es viable por recursos, el pipeline aborta antes de hit, crit, dano, curacion o efectos.
4. Si el evento requiere precision, el resolver ejecuta `Accuracy` vs `Evasion`.
5. Si la accion es de dano y puede criticar, se resuelve `CritChance` vs `CriticalEvasion`.
6. Con la magnitud base ya resuelta, el sistema calcula dano o restauracion base.
7. Para dano, aplica aumentos ofensivos por fuente, penetracion, mitigacion defensiva, reducciones del receptor y modificadores por estado.
8. Luego intenta aplicar condiciones iniciales y evalua interacciones especiales entre condiciones ya activas y condiciones recien aplicadas.
9. Finalmente proyecta los cambios de recursos resultantes sobre actor y objetivo sin persistirlos todavia.

## 3. Archivos principales del sistema de combate

- `War.Core/Combat/CombatEventResolver.cs`
  Responsabilidad: orquestador principal del evento de combate.
  Logica: encadena validacion del contexto, resolucion de magnitud, validacion de costos, hit/crit, dano o restauracion, condiciones, interacciones y proyeccion de recursos.
  Participacion: es el punto de entrada del pipeline y devuelve el `CombatResolutionResult` final.

- `War.Core/Combat/CombatEventModels.cs`
  Responsabilidad: contrato de entrada y salida del sistema de combate.
  Logica: define `CombatEventContext`, `CombatResolutionResult`, resoluciones de costos, propuestas de cambio de recurso, resultados de condiciones e indicadores de aborto.
  Participacion: conecta todos los servicios a traves de modelos comunes y deja trazabilidad para depuracion y futuras fases.

- `War.Core/Combat/CombatProbabilityChecks.cs`
  Responsabilidad: encapsular checks probabilisticos del combate.
  Logica: recibe una probabilidad normalizada, la clampa al rango valido y ejecuta la tirada aleatoria.
  Participacion: se usa tanto en hit/crit como en aplicacion y evasion de condiciones.

- `War.Core/Combat/CombatActionResourceService.cs`
  Responsabilidad: validar y proyectar los costos de una accion antes del resto del pipeline.
  Logica: agrupa costos declarados, ignora costos cero, verifica suficiencia de recursos y genera proyecciones de gasto sobre el actor.
  Participacion: decide si una accion aborta por recurso insuficiente y separa explicitamente el gasto de la accion de los cambios por impacto.

- `War.Core/Combat/CombatActionMagnitudeService.cs`
  Responsabilidad: resolver la magnitud base efectiva de una accion.
  Logica: combina base fija, tipo de escalado y coeficiente para obtener una magnitud final antes del resto del combate.
  Participacion: alimenta al pipeline con una magnitud base ya construida y deja trazabilidad del aporte fijo y del aporte escalado.

- `War.Core/Combat/CombatConditionApplicationService.cs`
  Responsabilidad: aplicar estados y CC iniciales.
  Logica: resuelve apply chance, evade chance cuando corresponde y duracion efectiva para CC afectados por `Tenacity`.
  Participacion: corre despues de conocer si el impacto conecto y genera resultados detallados por efecto intentado.

- `War.Core/Combat/CombatConditionInteractionService.cs`
  Responsabilidad: evaluar interacciones especiales entre condiciones.
  Logica: compara condiciones activas del objetivo con condiciones recien aplicadas, activa solo reglas definidas y puede generar condiciones adicionales o bonus de dano final.
  Participacion: se ejecuta despues de la aplicacion inicial de condiciones y antes de cerrar el dano final del evento.

- `War.Core/Combat/CombatResourceProjectionService.cs`
  Responsabilidad: proyectar cambios de recursos sin persistirlos.
  Logica: calcula valor previo, delta, valor unclamped, valor propuesto, clamp a cero o maximo y posible agotamiento.
  Participacion: se usa tanto para costos aprobados del actor como para dano, curacion o restauracion sobre el objetivo.

## 4. Sistema de escalado de habilidades

El escalado base de acciones se modela con `CombatActionMagnitudeProfile`. Ese perfil describe:

- `FixedBaseMagnitude`
- `CombatActionScalingType`
- `ScalingCoefficient`
- `ConfigurationName` opcional

`CombatActionScalingType` soporta actualmente:

- `FixedOnly`
- `PhysicalAttack`
- `MagicAttack`

La formula base implementada es:

`finalMagnitude = fixedBase + (stat * coefficient)`

Si la accion usa `PhysicalAttack`, el servicio toma `StatType.PhysicalAttack` desde las stats finales del actor. Si usa `MagicAttack`, toma `StatType.MagicAttack`. Si la accion es puramente fija, la contribucion por escalado es `0`.

Para mantener compatibilidad temporal, si no se envia `MagnitudeProfile`, el sistema usa `BaseMagnitude` como fallback legacy y lo marca explicitamente en la resolucion de magnitud.

## 5. Sistema de costos de recursos

Antes de resolver impacto o dano, el pipeline valida los costos declarados de la accion. Esto permite representar si una accion requiere `Mana`, `UltimateCharge` u otro recurso runtime soportado por el catalogo.

La validacion produce dos conceptos distintos:

- `ActionResourceCosts`
  En codigo se representan por `DeclaredResourceCosts` en el contexto y por `ActionResourceEvaluation` en el resultado. Describen el costo que el actor intenta pagar para ejecutar la accion.

- `ImpactResourceChanges`
  En codigo se exponen como `ImpactResourceChanges` dentro del resultado. Describen los cambios provocados por el efecto de la accion, por ejemplo dano al `HP` del objetivo o restauracion de `Mana`.

Esta separacion es intencional: el gasto del actor no se mezcla con el dano o la curacion sobre el objetivo. Si el costo bloqueante no alcanza, la accion aborta antes del resto del pipeline y el resultado devuelve el motivo `InsufficientResources`.

## 6. Sistema de condiciones y CC

La arquitectura de condiciones esta dividida en dos niveles.

- `CombatConditionApplicationService` resuelve la aplicacion inicial del efecto.
  Primero verifica si el impacto era requisito, luego ejecuta la chance de aplicar, despues la chance de evadir cuando la definicion del efecto lo exige, y finalmente calcula la duracion efectiva si es un CC afectado por `Tenacity`.

- `CombatConditionInteractionService` resuelve sinergias entre condiciones.
  Consulta las reglas definidas en `CombatConditionInteractions`, activa solo reglas ejecutables y deja fuera por defecto cualquier regla parcial o ambigua.

Con esto, el pipeline ya soporta:

- aplicacion inicial de estados y CC
- evasion de condiciones
- reduccion de duracion de CC por `Tenacity`
- bonus especiales por interacciones entre condiciones

## 7. Estado actual del sistema

Actualmente implementado:

- resolucion completa del evento de combate
- escalado base de acciones
- validacion de recursos
- calculo de dano y restauracion
- aplicacion inicial de estados
- interacciones de condiciones
- proyeccion de recursos

Pendiente de implementacion:

- runtime completo de estados
- persistencia final de cambios
- sistema de habilidades completo
- colas de combate por actor
- networking

## Demo de Combate y Snapshot de Personaje (Estado Actual del Proyecto)

### 1. Resumen de lo implementado

El proyecto ahora incluye una primera demo visible del backend de WAR con:

- sistema de progresion de personaje con niveles
- snapshot real de personaje desde backend
- calculo de estadisticas finales
- calculo de Power Score
- dos personajes demo persistidos
- ejecucion real de combate entre ellos
- persistencia de cambios de recursos (`HP`, `Mana`, `Ultimate Charge`)
- interfaz web simple para visualizar personajes y ejecutar acciones de combate

Esta fase representa una demo conceptual del backend de combate, no el cliente final del juego.

### 2. Backend agregado o modificado

#### Sistema de progresion

- `CharacterLevelRules.cs`
- `CharacterLevelModels.cs`
- `CharacterLevelProgression.cs`
- `ClassLevelGrowthProfiles.cs`
- `CharacterLevelStatSource.cs`
- `CharacterFinalStatsBuilder.cs`
- `CharacterSnapshotModels.cs`
- `CharacterSnapshotFactory.cs`
- `LevelProgressionFairnessAudit.cs`

Estos archivos implementan:

- progresion de nivel hasta 80
- formula de experiencia
- crecimiento de estadisticas por clase
- integracion del nivel dentro del calculo de `FinalStats`
- snapshot visible del personaje

#### Snapshot de personaje

- `CharacterSnapshotQueryService.cs`
- `CharacterSnapshotDtos.cs`
- `CharacterSnapshotPresentationCatalog.cs`
- `CharactersController.cs`

Estos componentes permiten obtener un snapshot completo del personaje incluyendo:

- progreso
- recursos actuales
- estadisticas finales
- Power Score

#### Sistema de combate demo

Archivos principales:

- `DemoCharacterCatalog.cs`
- `PersistedCharacterRuntimeService.cs`
- `CombatDemoDtos.cs`
- `DemoCombatRosterService.cs`
- `CombatResultPersistenceService.cs`
- `DemoCombatExecutionService.cs`
- `CombatDemoController.cs`
- `BasicAttackCatalog.cs`

Estos archivos implementan:

- roster de personajes demo
- ejecucion de combate entre personajes persistidos
- aplicacion de resultados del combate
- persistencia de cambios en recursos
- endpoints de combate demo

#### Persistencia

Archivos relevantes:

- `CharacterEntity.cs`
- `WarDbContext.cs`
- `DemoCharacterSeedService.cs`
- migracion inicial en `Migrations`

Estos archivos preparan la persistencia para:

- progreso de personaje
- recursos actuales
- personajes demo iniciales

### 3. Endpoints disponibles

#### Snapshots de personajes

`GET /api/characters/{id}/snapshot`

Obtiene el snapshot completo de un personaje.

`GET /api/characters/demo/a/snapshot`  
`GET /api/characters/demo/b/snapshot`  
`GET /api/characters/demo/snapshot`

Accesos rapidos a los personajes demo.

`GET /api/characters/demo/combatants`

Devuelve ambos personajes demo junto con los comandos disponibles para la UI.

#### Combate demo

`POST /api/combat/demo/execute`

Ejecuta una accion de combate entre dos personajes.

Payload conceptual:

- `attackerId`
- `targetId`
- `actionType`
- `skillId` (opcional)

El endpoint:

1. carga ambos personajes persistidos
2. ejecuta el resolver de combate
3. aplica cambios a recursos
4. devuelve snapshots actualizados y log del combate

`POST /api/combat/demo/reset`

Restablece ambos personajes demo a su estado inicial.

### 4. Interfaz demo

La API ahora sirve una interfaz web simple para visualizar la demo.

La UI permite:

- ver dos personajes demo simultaneamente
- consultar sus snapshots
- ejecutar ataques basicos
- ejecutar la skill piloto del Sorcerer
- ver el log del combate
- abrir el panel de estadisticas con el boton `i`

Archivos principales de la UI:

- `index.html`
- `app.js`
- `styles.css`

### 5. Como ejecutar la demo

#### 1. Restaurar dependencias

```bash
dotnet restore
```

#### 2. Compilar el proyecto

```bash
dotnet build
```

#### 3. Ejecutar la API

```bash
dotnet run --project War.Api
```

#### 4. Abrir la demo

Abrir en navegador:

```text
http://localhost:XXXX/
```

Usa el puerto configurado por la aplicacion. La interfaz mostrara automaticamente los dos personajes demo.

### 6. Acciones disponibles en la demo

Desde la interfaz se puede:

- ejecutar ataque basico A -> B
- ejecutar ataque basico B -> A
- ejecutar skill piloto del Sorcerer
- ver resultados del combate en el log
- observar cambios en `HP` y `Mana`
- reiniciar la demo con el boton `Reset Demo`

### 7. Limitaciones actuales

- esta demo no incluye runtime completo de estados temporales
- no hay sistema de colas por actor
- no hay concurrencia multiplayer
- no hay autenticacion de cuentas
- la UI es solo conceptual

El objetivo actual es demostrar el funcionamiento del backend de combate y del sistema de progresion.

### 8. Verificacion

El flujo fue verificado con:

- compilacion del proyecto
- endpoints de snapshot
- endpoints de combate
- ejecucion de la UI demo

## Development Timeline

### March 14, 2026 — Major Systems Expansion

#### 1. Skill Administration Panel

Se incorporo un panel administrativo completo para gestion de habilidades sobre `/admin/`, pensado como primera capa operativa para equipo de diseño y balance. El panel ya no trabaja con formularios libres: compone `SkillDefinition` usando solamente modulos existentes del backend, incluyendo clases, slots, tipos de accion, damage types, scaling types, targeting, condiciones, protecciones, triggered actions, sinergias, materiales y overrides de ascension.

La persistencia administrativa se resolvio mediante registros JSON en `admin_skill_records`, con un documento completo por skill mas columnas denormalizadas para consulta y validacion rapida. Esto permite CRUD real sin desmontar el catalogo programado actual. El panel soporta:

- listado por clase y slot
- vista detallada completa de la habilidad
- creacion y edicion total de la definicion
- editor de ascensiones 1 a 10
- preview de traducibilidad a combate
- preview de impacto referencial en Power Score
- comparacion administrativa entre habilidades
- borrado logico para no romper referencias futuras

El backend administrativo valida las skills con el dominio real antes de guardarlas, de modo que el panel no pueda inventar estructuras no soportadas por combate o por el traductor de skills.

#### 2. Skill Publication Flow

Se introdujo un flujo editorial separado del guardado administrativo. A partir de esta fase ya existe una diferencia explicita entre:

- skill programada en codigo
- skill editable persistida
- skill publicada para runtime

El estado editorial actual se representa con:

- `Draft`
- `Published`
- `PublishedWithDraft`
- `Archived`

Guardar una skill ya no equivale a publicarla. La operacion de publicacion toma el draft persistido, ejecuta validaciones de dominio, verifica que la skill siga siendo traducible al pipeline de combate, comprueba compatibilidad de catalogo en el contexto de su clase y bloquea pending data marcada como bloqueante. Solo despues genera un snapshot publicado para runtime.

Los drafts nunca entran al runtime. Las skills archivadas tampoco. Si existe una skill publicada y luego el admin sigue editando el draft, el runtime sigue usando la version publicada hasta que haya una nueva publicacion valida.

#### 3. Runtime Skill Catalog Provider

El runtime de skills dejo de depender exclusivamente del catalogo hardcodeado. Ahora existe una capa proveedora central que separa y compone tres fuentes:

- catalogo programado en codigo
- catalogo persistido editable del panel admin
- catalogo publicado para runtime

`SkillRuntimeCatalogProvider` es la unica entrada para resolver skills efectivas del runtime, mientras que `RuntimeSkillCatalogComposer` aplica la politica de resolucion:

1. usar overrides publicados persistidos cuando la clase resultante sigue siendo valida
2. hacer fallback al catalogo programado si la skill publicada no existe o la fusion deja la clase invalida
3. excluir siempre drafts
4. excluir siempre archivadas

Esta capa deja la transicion auditable y reversible. Combate, snapshots y demo ya pueden consumir skills resueltas por provider sin acoplarse directamente al panel admin.

#### 4. Sorcerer Skill Kit Completion

Se completo el kit base de la Sorcerer dentro del sistema maestro de habilidades:

- 12 habilidades base
- 1 habilidad definitiva

El kit quedo organizado alrededor de tres dominios elementales:

- fuego
- hielo
- electricidad

Las skills ahora definen de manera explicita si pueden aplicar `Heat`, `Cold`, `Electrified`, `Freeze` u otros estados. Esto refuerza la regla central del sistema: las stats del personaje pueden mejorar la probabilidad o la duracion de un efecto, pero no pueden hacer que una skill aplique un estado que no esta declarado en su propia definicion.

Tambien se reforzo el desbloqueo progresivo por ascension, de modo que los efectos superiores aparecen en ascensiones medias o altas y no en la base. La definitiva existente fue actualizada a `Tempestad Draconica` conservando su logica funcional previa.

#### 5. Elemental Interaction Matrix

Se introdujo una matriz elemental centralizada para organizar reacciones entre estados como:

- `Heat`
- `Cold`
- `Electrified`
- `Freeze`
- `Weaken`

La separacion conceptual ahora es clara:

- la skill decide que estados puede aplicar o que condiciones previas requiere
- la matriz decide que reaccion sistemica ocurre cuando esos estados coinciden

Esto evita duplicidad de logica entre catalogos de skills y servicios de combate. Las reacciones y sus efectos secundarios quedan descritos en una sola fuente reusable, mas facil de auditar y extender en futuras fases.

#### 6. Basic Attack System

Se implemento el primer sistema completo de ataques basicos para las 4 clases iniciales. Ya no existe un unico “golpe basico” plano por clase: ahora cada clase tiene un combo de 6 golpes con progresion de dano por etapa y tiempos de casteo propios.

Arquitectura principal:

- `BasicAttackCatalog`: perfiles por clase
- `BasicAttackModels`: contratos de combo, magnitud y runtime state
- `BasicAttackMagnitudeResolver`: calculo del dano base por etapa
- `BasicAttackComboResolver`: resolucion de continuidad o reset del combo

Reglas implementadas:

- combo de `6` etapas
- ventana de continuacion de `2` segundos
- la ventana se mide desde el fin del basico anterior al inicio del siguiente
- el casteo del siguiente golpe no consume la ventana
- cada etapa aumenta `1.5%` respecto a la anterior por defecto
- el posible override del sexto golpe quedo centralizado y documentado en un solo lugar
- el critico sigue usando el pipeline real existente con `CritChance`, `CriticalEvasion` y `CritDamage`

Perfiles base por clase:

- Sorcerer: dano magico, cast `0.30s`, `10% MagicAttack + 1% PhysicalAttack`
- Juramentada: dano magico, cast `0.25s`, `6% MagicAttack + 4% PhysicalAttack`
- Lancero: dano fisico, cast `0.23s`, `4% MagicAttack + 6% PhysicalAttack`
- Bruiser: dano fisico, cast `0.20s`, `1% MagicAttack + 9% PhysicalAttack`

Los basicos resuelven magnitud y etapa en su propia capa, pero siguen entrando al `CombatEventResolver` real para hit, crit, mitigacion, efectos y proyeccion de recursos.

#### 7. Combo Persistence

El estado del combo ahora vive en persistencia dentro de `CharacterEntity`, con los campos:

- `LastBasicComboStage`
- `LastBasicComboCompletedAtUtc`

Esto permite que la secuencia continue entre requests de la demo y que el siguiente basico se resuelva correctamente segun el ultimo golpe completado y el tiempo transcurrido. Tambien permite que el reset de demo limpie el estado de combo de forma coherente.

#### 8. Demo Combat Improvements

La demo visible de combate evoluciono desde un tablero de acciones simples a una ejecucion real de basicos por combo sobre personajes persistidos. Los cambios principales fueron:

- ataques basicos reales de backend
- persistencia del estado de combo entre requests
- logs con fases `Basic 1/6`, `Basic 2/6`, etc.
- tags visibles de `Combo Continue` y `Combo Reset`
- snapshots actualizados con el siguiente golpe esperado del combo
- integracion completa con el resolver de combate ya existente

La demo sigue sin ser un runtime autoritativo completo de servidor, pero ya no simula ataques basicos en frontend. El flujo visible responde al backend real, persiste HP/Mana/Ultimate y ahora tambien el progreso del combo.

#### 9. Power Score Improvements

El sistema de Power Score fue ajustado para consumir los basicos reales de las 4 clases en lugar de depender de placeholders neutros. Esto mejora especialmente la lectura contextual de:

- `MagicAttack`
- `PhysicalAttack`
- penetraciones
- precision y critico

Tambien se introdujo soporte para multiples componentes de escalado dentro del mismo basico, de forma que las clases hibridas no queden mal valoradas cuando convierten dano desde mas de una stat ofensiva.

#### 10. Database Changes

Se añadieron nuevas migraciones y extensiones de persistencia para soportar el runtime expandido:

- `AdminSkillPublicationFlow`: agrega el snapshot publicado de skill, versionado minimo de draft/publicacion, timestamps y metadata de publicacion para el flujo editorial del panel admin
- `BasicAttackComboState`: agrega el estado persistido del combo basico al personaje (`last_basic_combo_stage`, `last_basic_combo_completed_at_utc`)

Estas migraciones complementan el modelo existente sin romper el seed demo actual ni el fallback al catalogo programado.

#### 11. Admin UI

La nueva UI administrativa queda servida sobre `/admin/` y funciona como una herramienta operativa para inspeccion y edicion del catalogo. La interfaz incluye:

- listado agrupado por clase
- editor completo de skill
- preview de validacion
- preview de impacto en Power Score
- publicacion y despublicacion
- archivado logico
- comparacion de habilidades

El objetivo de esta UI no es reemplazar la autoria tecnica del dominio, sino ofrecer una capa segura de composicion y publicacion que reuse los contratos existentes del backend y haga visible el estado editorial de cada skill.

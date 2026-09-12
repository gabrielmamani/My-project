# Estado de Progreso: Gunbound Mobile 2.5D

## 1. Módulos y Tareas Pendientes (Backlog Inmediato)

### Bloque A: Pulido de Controles y Balística Local (Prioridad Inmediata)
- [x] **A1. Corrección Multi-Input**: Configurar `EventSystem` para procesar toques en móvil y ratón en PC sin bloqueos de raycast.
- [x] **A2. Giro del Vehículo (Flip)**: Invertir el sprite y el pivote del cañón al pulsar las direcciones izquierda/derecha.
- [x] **A3. Calibración de Potencia y Viento**: Asegurar que la fuerza al 1% caiga cerca y que el viento en contra curve la bala gradualmente sin regresarla de inmediato.
- [x] **A4. Visualizador de Ángulo**: Mostrar el arco visual de puntería (ángulo mín/máx) alrededor del tanque.

### Bloque B: Reglas Competitivas Locales (Cierre de Fase 3)
- [x] **B1. Selector de Munición (T1 / T2 / SS)**: Conectar los 3 botones en el Canvas con recarga de cooldown en el disparo SS.
- [x] **B2. Shift List Dinámica**: Mostrar la lista de turnos ordenada dinámicamente según el delay acumulado.
- [x] **B3. Audio & VFX**: Integrar efectos de sonido para disparos, carga e impactos.

### Bloque C: Multijugador Autoritativo en Red (Fase 4 - COMPLETADO 100%)
- [x] **C1. Framework de Red**: Configurar NetworkManager (Fish-Net / Photon Fusion / Netcode) y panel de Lobby Host/Client.
- [x] **C2. Spawning Autoritativo**: Instanciar móviles con autoridad de cliente y sincronizar ángulos.
- [x] **C3. RPC de Disparo en Servidor**: Validar entradas y calcular parábola balística en el host.
- [x] **C4. Sincronización de Cráteres**: Emitir RPC con coordenadas de impacto para deformar el terreno en ambos clientes.

### Bloque E: Autenticación, Login, Menú Principal, Lista de Salas y Sala de Espera (Fase 6)
- [x] **E1. Login & Perfil de Jugador**: Pantalla de Login, Nickname, Avatar y guardado local en `PlayerPrefs` (`UserDataManager.cs`).
- [x] **E2. Menú Principal (Main Hub)**: Navegación principal estilo Gunbound Mobile 16:9 (Partida Rápida, Buscar Sala, Garaje, Ajustes).
- [x] **E3. Buscador y Lista de Salas**: Panel de exploración de salas activas (*Room List*) y modal de creación de sala personalizada.
- [x] **E4. Sala de Espera (Room Ready Screen)**: Tarjetas de P1/P2 con selección de móvil y botón de **"¡LISTO! / INICIAR"** autoritativo en red.

---

## 2. Historial de Módulos Completados
- [x] Física balística determinista básica con vector de viento.
- [x] Terreno destructible con borrado de textura alfa y actualización de colisionadores.
- [x] Sistema de salud con daño radial proporcional al punto de impacto.
- [x] Zonas de muerte inferior (DeathZone) para eliminación por bunge.
- [x] Satélite Thor funcional con rayo vertical en coordenadas de impacto.
- [x] Ítems tácticos base (Dual, Teleport, Heal).
- [x] **Calibración de Potencia y Viento (A3)**: Fuerza mínima al 1% calibrada a 3.0 u/s para impactos a corta distancia en frente del móvil. Inercia progresiva de viento (`_windRampDuration = 0.4s`) que domina el tramo de salida sin giros bruscos en el primer frame y viento radial 360°.
- [x] **Visualizador de Ángulo (A4)**: Componente `AimArcVisualizer.cs` y generador de editor `Phase7AimArcSetup.cs` para renderizar con `LineRenderer` el arco de elevación (mín/máx), los rayos límite de ángulo y la línea activa de puntería adaptada a orientación horizontal (flip) y estado de turnos.
- [x] **Selector de Munición (B1)**: Integración completa de botones UI (`Btn_T1`, `Btn_T2`, `Btn_SS`) y atajos numéricos teclado (1, 2, 3). Sistema de recarga de cooldown (2 turnos) para disparo especial SS con bloqueo interactivo de botón, indicador de turnos restantes `"SS (N)"` y reversión automática a Tiro 1 tras disparar.
- [x] **Shift List Dinámica (B2)**: Componente `ShiftListUI.cs` y setup de editor `Phase8ShiftListSetup.cs` que construye el panel `Panel_ShiftList` en la esquina superior izquierda del Canvas. Simula y muestra la secuencia proyectada de turnos ordenada dinámicamente según el acumulador de delay (`RF-04.1`), destacando el turno actual, colores por jugador y alertas de `¡DOBLE TURNO!`.
- [x] **Audio & VFX (B3)**: Integración de sintetizador procedimental en `AudioManager.cs` con clips específicos para Tiro 1, Tiro 2 y SS. Modulación dinámica de pitch (0.9x a 1.8x) durante la carga de potencia, efectos de sonido para clics UI y uso de ítems. Destello de boca de cañón (*muzzle flash*) y sistema de explosión de dos capas (núcleo blanco incandescente + ráfaga de fuego) escalado proporcionalmente según el radio de daño.
- [x] **Framework de Red & Lobby (C1)**: Integración del paquete `com.unity.netcode.gameobjects`, gestor autoritativo `NetworkLobbyManager.cs` y panel UI `Panel_NetworkLobby` (`NetworkLobbyUI.cs` y `Phase10NetworkSetup.cs`) para crear partida (`Host`), unirse (`Client`) vía IP/Puerto, y detección/evaluación automática de victoria tras temporizador de desconexión de 10 segundos (`RF-07.1.1`, `RF-07.1.2`, `RF-07.1.3`).
- [x] **Spawning Autoritativo (C2)**: Implementación de `NetworkSpawnerManager.cs`, `NetworkPlayerSync.cs` y `Phase11SpawningSetup.cs`. Posicionamiento autoritativo en el Host para P1 ($X = -5.0$) y P2 ($X = 5.0$) (`RF-07.2.1`), asignación de `OwnerClientId` y sincronización bidireccional mediante `NetworkVariable` con permisos de escritura por el propietario para posición, `flipX` y ángulo de puntería turret (`RF-07.2.2`, `RF-07.2.3`).
- [x] **RPC de Disparo en Servidor (C3)**: Implementación de `NetworkShotManager.cs` y actualización de `PlayerController.cs` y `Phase12ShotRpcSetup.cs`. Envío de solicitudes de disparo desde clientes vía `ServerRpc` (`CmdFireServerRpc`) con validación antitrampas centralizada en servidor (`RF-07.3.1`, `RF-07.3.2`) comprobando turno del jugador, rango de potencia [0,1], límites de ángulo y cooldown de SS. Ejecución autoritativa de la trayectoria física en el Host y replicación del evento a clientes mediante `ClientRpc` (`RF-07.3.3`).
- [x] **Sincronización de Cráteres (C4)**: Implementación de `NetworkCraterManager.cs`, actualización de `Projectile.cs` y `Phase13CraterSyncSetup.cs`. Transmisión de coordenadas de impacto `(Vector2 impactPosition, float explosionRadius)` desde el servidor mediante `ClientRpc` (`RpcCarveHoleClientRpc`) para ejecutar la perforación circular idéntica en la textura de terreno `DestructibleTerrain` y reconstrucción de `PolygonCollider2D` en ambos clientes sin transmitir binarios de textura por la red (`RF-07.4.1`), junto con la evaluación autoritativa de daño radial y bunge (`RF-07.4.2`, `RF-07.4.3`).
- [x] **Módulo 4.1: Infraestructura de Red, Conexión y Emparejamiento (RF-4.1.1, RF-4.1.2, RF-4.1.3)**: Implementación de `NetworkGameManager.cs` con sincronización autoritativa de estados de juego (`Offline`, `Connecting`, `InLobby`, `InGame`, `PostMatch`) mediante `NetworkVariable`. Configuración de `[NetworkManager]` con Netcode for GameObjects y `UnityTransport` (UDP/7777). Panel UI `Panel_NetworkLobby` con controles de `Btn_Host`, `Btn_JoinClient`, IP/Puerto configurable y temporizador autoritativo de desconexión de 10s con declaración de victoria técnica por abandono.
- [x] **Módulo 4.2: Spawning Autoritativo y Replicación de Móviles (RF-4.2.1, RF-4.2.2, RF-4.2.3)**: Instanciación e integración autoritativa de `NetworkPlayerSync.cs` derivado de `NetworkBehaviour` con `NetworkVariable<Vector3>` para posición, `NetworkVariable<int>` para orientación `flipX` y `NetworkVariable<float>` para ángulo de elevación de torreta (`TurretAim`). Posicionamiento autoritativo en $X = -5.0$ (P1) y $X = 5.0$ (P2) con asignación de `OwnerClientId` y `NetworkObject`.
- [x] **Módulo 4.3: Disparo y Balística Autoritativa en Servidor (RF-4.3.1, RF-4.3.2, RF-4.3.3)**: Actualización de `NetworkShotManager.cs` derivado de `NetworkBehaviour` con comandos RPC de red `CmdFireServerRpc` (`[ServerRpc(RequireOwnership = false)]`) y replicación de disparos a clientes mediante `BroadcastShotClientRpc` (`[ClientRpc]`). Sistema de validación antitrampas centralizado en el servidor comprobando turno activo, límites de ángulo $[-90°, 90°]$, potencia $[0.0, 1.0]$, disponibilidad de ítems y cooldown de munición especial SS.
- [x] **Módulo 4.4: Sincronización de Terreno Destructible y Salud (RF-4.4.1, RF-4.4.2, RF-4.4.3)**: Actualización de `NetworkCraterManager.cs` derivado de `NetworkBehaviour` con emisión de `RpcCarveHoleClientRpc` (`[ClientRpc]`) para la perforación idéntica en la textura de terreno `DestructibleTerrain` en ambos clientes sin transmisión binaria de imágenes. Procesamiento autoritativo de daño radial en el servidor y evaluación autoritativa de caída en la zona de muerte inferior (`DeathZone`) para eliminación por bunge.
- [x] **Módulo 4.5: Cola de Delay y Pase de Turnos en Red (RF-4.5.1, RF-4.5.2)**: Creación e integración de `NetworkTurnManager.cs` derivado de `NetworkBehaviour` con la variable síncrona `ActivePlayerNetworkId` (`NetworkVariable<int>`) y temporizador síncrono de 20s (`NetworkVariable<float>`). Evaluación centralizada del acumulador numérico de delay en el servidor para otorgar autoridad de turno al jugador con menor puntaje acumulado y habilitación remota de controles de entrada.


## 3 Tareas de Corrección de Controles y HUD
- [x] **Fix 1: Input Unificado PC/Móvil (Sin botón de disparar)**:
  - PC: Barra espaciadora carga potencia al mantener y dispara al soltar.
  - Móvil: Toque/arrastre táctil en la zona transparente `TouchShootArea` (cuadrante inferior derecho) que carga potencia y dispara al levantar el dedo (`OnPointerUp`).
- [x] **Fix 2: Movimiento y Elevación Operativos**:
  - PC: `Input.GetAxisRaw("Horizontal")` (A/D) para mover e `Input.GetAxisRaw("Vertical")` (W/S) para ajustar ángulo.
  - Móvil: Botones dedicados en la esquina inferior izquierda (`Move_Left`, `Move_Right`, `Angle_Up`, `Angle_Down`) con `HoldButtonHandler` para movimiento y elevación continua.
  - Auditoría de raycast target en el Canvas para desmarcar imágenes decorativas y de fondo.
- [x] **Fix 3: Reorganización del HUD (Layout Groups)**:
  - Fila de Munición (`Btn_T1`, `Btn_T2`, `Btn_SS` de 70x70) con `HorizontalLayoutGroup` (10px de espaciado) en la esquina inferior derecha.
  - Fila de Ítems (`Btn_Dual`, `Btn_Teleport`, `Btn_Heal` de 60x60) con `HorizontalLayoutGroup` (10px de espaciado) ubicados justo encima de la munición.
- [x] **Fix 4: Sistema de Disparo por Arrastre (Drag to Power)**:
  - `TouchShootArea.cs` implementa `IBeginDragHandler`, `IDragHandler` y `IEndDragHandler`.
  - Mapeo interactivo de la distancia horizontal `deltaX = currentPos.x - startDragPos.x` para subir/bajar la potencia (0% a 100%) en tiempo real y disparar al soltar.
  - Soporte unificado de física en `PlayerController.cs` con `Rigidbody2D` para movimiento continuo sin atravesar pendientes y ajuste fluido del ángulo de elevación de puntería con eventos `OnAngleChanged`.
  - Depuración y eliminación de botones flotantes residuales en el cuadrante superior derecho del Canvas y desmarcado de `Raycast Target` en paneles de fondo transparentes.
- [x] **Fase 14: UI/UX de Fin de Partida, Salud Overhead y Daño Flotante (Opción 1)**:
  - **Texto de Daño Flotante (`FloatingDamageText.cs`)**: Animación de números de daño emergentes en World Space con movimiento ascendente, escalado pop y desvanecimiento `alpha`. Colores según contexto (Rojo normal, Dorado crítico, Morado ¡BUNGE!).
  - **Barras de Vida Flotantes (`OverheadHealthBar.cs`)**: Canvas World Space anclado dinámicamente sobre los vehículos (`Vector3.up * 1.6f`) con indicador de nombre (`P1 (Tú)` / `P2 (Rival)`), Slider con relleno en degradado (Verde -> Amarillo -> Rojo) y texto numérico de vida (`850 / 1000`).
  - **Modal de Fin de Partida (`MatchResultUI.cs` y `Phase14MatchResultSetup.cs`)**: Panel UI `Panel_MatchResult` con título dinámico (`¡VICTORIA!` / `¡DERROTA!`), motivo de victoria (`K.O.`, `Bunge` o `Desconexión`), estadísticas de turnos y botones interactivos (`Revancha` y `Lobby`).
  - **Pulido de Advertencias de Consola**: Detención explícita de `ParticleSystem` (`StopEmittingAndClear`) antes de configurar la duración en `PlayerController.cs` y `Projectile.cs`.
- [x] **Fase 15: Sistema Completo de Clima y Satélites (Opción 2)**:
  - **Gestor de Satélites (`SatelliteManager.cs`)**: Modificadores dinámicos de juego para cada fase climática (`DamageMultiplier`, `WindMultiplier`, `IsThorActive`) con rotación autoritativa en cada cambio de turno.
  - **Rayo Celestial Thor (`ThorBeam.cs` y `Projectile.cs`)**: Despliegue automático del rayo láser orbital vertical al impactar una bala durante la fase Thor, destruyendo terreno extra e infligiendo 150 HP de daño radial con efectos SFX (`PlayThorBeamSFX`).
  - **Bonificadores Climáticos**: Incremento de daño $+25\%$ en fase `Force` y multiplicador de viento $2.0x$ en fase `Hurricane`.
  - **HUD de Satélites (`SatelliteUI.cs` y `Phase15SatelliteSetup.cs`)**: Panel UI `Panel_SatelliteUI` en la parte superior central con insignia coloreada por tipo de satélite, títulos dinámicos y animación pop de transición.
- [x] **Cierre de Fase 4 (Multijugador Autoritativo Online 1v1)**:
  - Registro de finalización del 100% de los requerimientos de red (RF-07.1 al RF-07.4 y Módulos 4.1 al 4.5).
  - Apertura oficial de la **Fase 5: Garaje de Móviles, Sistema de Ítems Avanzados y Múltiples Escenarios 2.5D**.
- [x] **Fase 16: Sistema de Garaje y Selección de Móviles (Módulo 5.1 / RF-5.1.1, RF-5.1.2, RF-5.1.3)**:
  - **ScriptableObject `MobileData.cs`**: Definición de parametrización de estadísticas por tanque (Salud máxima, Armadura %, Rangos de elevación angular `minAngle`/`maxAngle`, velocidad de movimiento, delay base y sprites).
  - **Assets de Tanques**: Creación de `Mobile_Mage.asset` (1000 HP, 5% Armadura, 20°–70°, Speed 4.0, Delay 250), `Mobile_Armor.asset` (1100 HP, 15% Armadura, 15°–60°, Speed 3.2, Delay 270) y `Mobile_Boomer.asset` (900 HP, 0% Armadura, 10°–75°, Speed 4.5, Delay 230).
  - **Sincronización Netcode de Selección**: Inclusión de `NetworkVariable<int> P1MobileType` y `NetworkVariable<int> P2MobileType` en `NetworkGameManager.cs` con llamadas `SelectMobileServerRpc` para transmisión autoritativa en red.
  - **Interfaz de Garaje (`NetworkLobbyUI.cs`)**: Panel UI `Panel_MobileGarage` en el Lobby con botones interactivos para la selección de Mage, Armor y Boomer, y panel de vista previa de estadísticas en tiempo real.
  - **Instanciación y Aplicación Autoritativa (`NetworkSpawnerManager.cs` y `PlayerController.cs`)**: Aplicación automática de `MobileData` (`ApplyMobileData`) en el spawn de vehículos, configurando velocidad, salud en `Health.cs` con reducción de daño por armadura, límites angulares en `TurretAim.cs` y sprites.
  - **Generador Editor (`Phase16MobileSelectionSetup.cs`)**: Menú `Gunbound/Setup Phase 16 (Mobile Selection & Garage)` para generación automática de assets y montaje de UI en Canvas.
- [x] **Fase 17: Balística y Disparos Únicos por Vehículo (Módulo 5.2 / RF-5.2.1, RF-5.2.2)**:
  - **Comportamientos Físicos Especiales (`SpecialProjectileType.cs` y `Projectile.cs`)**:
    - **Boomerang (Boomer)**: Fuerza aerodinámica de viento ampliada ($2.5x$) con curvatura progresiva en `FixedUpdate()` para tiros de bucle que regresan hacia atrás.
    - **Shrapnel (Armor Shot 2)**: Separación autoritativa en 2 sub-misiles de metralla secundarios tras $0.5s$ de vuelo con dispersión angular de $\pm 15^\circ$.
    - **Plasma (Mage)**: Proyectil energético brillante de alta velocidad con resistencia de viento reducida al 50%.
  - **Asignación por Móvil (`PlayerController.cs` y `MobileData.cs`)**: Método `GetActiveProjectilePrefab` para seleccionar autoritativamente el prefab de proyectil Tiro 1, Tiro 2 o SS correspondiente al móvil activo.
  - **Generador Editor (`Phase17UniqueBallisticsSetup.cs`)**: Menú `Gunbound/Setup Phase 17 (Unique Mobile Ballistics)` para generación de prefabs específicos de proyectiles y su vinculación en `Mobile_Mage.asset`, `Mobile_Armor.asset` y `Mobile_Boomer.asset`.
- [x] **Fase 18: Autenticación, Menú Principal, Buscador de Salas y Sala de Espera (Fase 6 / Módulos 6.1 a 6.4)**:
  - **Perfil y Persistencia (`UserDataManager.cs`)**: Manejo de credenciales de usuario, Nickname personalizado, selección de avatar e integración con `PlayerPrefs` (`Gunbound_Nickname`, `Gunbound_AvatarId`).
  - **Navegación de Pantallas (`UINavigationManager.cs`)**: Control centralizado de transiciones entre `Panel_Login`, `Panel_MainMenu`, `Panel_RoomBrowser`, `Panel_RoomReadyScreen` e InGame HUD.
  - **Pantalla de Login (`LoginUI.cs`)**: Interfaz de autenticación `Panel_Login` con selector de avatar, campo de entrada de usuario y validación.
  - **Menú Principal Hub (`MainMenuUI.cs`)**: Dashboard `Panel_MainMenu` con banner de jugador en tiempo real (Nickname, Avatar, Oro) y botones para Partida Rápida, Buscar Salas, Garaje y Cerrar Sesión.
  - **Buscador de Salas (`RoomBrowserUI.cs` & `RoomData.cs`)**: Lista interactiva scrolleable de salas activas, barra de conexión directa por IP/Puerto y modal para crear sala personalizada (`Modal_CreateRoom`).
  - **Sala de Espera y Ready Screen (`RoomReadyUI.cs`)**: Vista previa de tarjetas de jugadores (P1 Host y P2 Client), selección de tanque desde el Garaje y botón de **"¡LISTO! / INICIAR COMBATE"** con sincronización Netcode (`_p1Ready`, `_p2Ready`).
  - **Generador Editor (`Phase18FullUINavigationSetup.cs`)**: Menú `Gunbound/Setup Phase 18 (Full Mobile UI Flow)` para el montaje automático de la jerarquía UI completa en Canvas 16:9 Landscape.
- [x] **Fase 19: Sistema de Ítems Avanzados e Inventario (Módulo 5.3 / RF-5.3.1, RF-5.3.2)**:
  - **Definición de Ítems (`ItemType.cs`)**: Definición del enum de ítems tácticos (`Dual`, `Teleport`, `Heal`, `ChangeWind`, `Shield`, `DualPlus`).
  - **Escudo Absorbente de Daño (`Health.cs` y `PlayerController.cs`)**: Barrera protectora de $200\text{ HP}$ (`ShieldHP`) que absorbe el daño de proyectiles entrantes antes de restar vida del tanque, con domo translúcido visual interactivo.
  - **Cambio Autoritativo de Viento (`ItemManager.cs` y `SatelliteManager.cs`)**: Ítem *Change Wind* que recalcula la velocidad y dirección del vector de viento en el servidor con recargo de delay ($+150$).
  - **Disparo Especial Doble (`DualPlus`)**: Habilita dos disparos seguidos de proyectil especial ($T2$ o $SS$) con la penalización de delay de $+500$.
  - **Comandos RPC de Red (`NetworkShotManager.cs`)**: Envío de solicitudes de uso de ítems desde clientes vía `CmdUseItemServerRpc` con validación antitrampas y transmisión `BroadcastItemUsedClientRpc`.
  - **Persistencia de Carga Táctica (`UserDataManager.cs`)**: Carga y guardado local de hasta 6 ítems tácticos en el perfil del jugador.
  - **HUD e Interfaz de Combate (`UIManager.cs`)**: Botones interactivos para los 6 ítems tácticos (`Btn_Dual`, `Btn_Teleport`, `Btn_Heal`, `Btn_ChangeWind`, `Btn_Shield`, `Btn_DualPlus`), estados activo/usado e integración visual.
  - **Generador Editor (`Phase19AdvancedItemsSetup.cs`)**: Menú `Gunbound/Setup Phase 19 (Advanced Tactical Items & Inventory)` para generación automática de la barra de ítems en Canvas y vinculación a `UIManager`.
- [x] **Fase 20: Múltiples Escenarios y Selección de Mapa (Módulo 5.4 / RF-5.4.1, RF-5.4.2)**:
  - **Assets de Mapa (`MapType.cs` y `MapData.cs`)**: Definición de ScriptableObjects para *Miramo Town*, *Metamine* y *Cozy Cave* parametrizando sprites de terreno, fondo parallax 16:9, atmósfera y multiplicadores de viento/gravedad.
  - **Sincronización Netcode de Escenario (`NetworkGameManager.cs`)**: Inclusión de `NetworkVariable<int> _selectedMapType` y llamadas `SelectMapServerRpc` para permitir al Host elegir el escenario autoritativamente.
  - **Carga Dinámica de Terreno (`DestructibleTerrain.cs`)**: Método `ApplyMapData` que intercambia la textura base del suelo en tiempo de ejecución, regenerando la textura clonada RGBA32 de lectura/escritura y recalculando el `PolygonCollider2D`.
  - **Gestor de Entorno (`MapEnvironmentLoader.cs`)**: Componente que escucha eventos de red y actualiza dinámicamente el fondo 2D, el color de atmósfera de la cámara y los modificadores de gravedad de `Physics2D`.
  - **Selector de Mapa en UI (`RoomReadyUI.cs`)**: Botones interactivos de selección para el Host (`Btn_MapMiramo`, `Btn_MapMetamine`, `Btn_MapCozy`) con tarjeta visual del mapa elegido.
  - **Generador Editor (`Phase20MapSelectionSetup.cs`)**: Menú `Gunbound/Setup Phase 20 (Multiple Maps & Environment Selection)` para creación automática de assets `MapData` por defecto y montaje en Canvas.
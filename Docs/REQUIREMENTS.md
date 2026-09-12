# Especificación de Requerimientos del Sistema: Gunbound Mobile 2.5D

## 1. Visión y Plataforma
Adaptación táctica por turnos inspirada en las reglas clásicas de Gunbound, desarrollada en Unity 6 (URP) para plataformas móviles (Android/iOS) en relación 16:9 Landscape y PC con controles híbridos.

---

## 2. Requerimientos Funcionales (RF)

### Módulo 1: Control, Orientación y Puntería Móvil
- **RF-01.1 (Soporte Táctil y Ratón)**: EventSystem configurado con soporte multi-puntero para registrar clics en PC y toques en pantalla móvil simultáneamente.
- **RF-01.2 (Movimiento y Flip)**: Al moverse a la izquierda o derecha, el chasis invierte su orientación (`flipX` o escala en X), y el pivote de la torreta refleja su ángulo hacia el nuevo frente.
- **RF-01.3 (Elevación y Arco de Puntería)**: Control de elevación angular (grados relativos al chasis). Renderizado visual del arco clásico de grados mínimos y máximos alrededor del cañón.
- **RF-01.4 (Sensibilidad de Carga)**: Barra de fuerza acumulativa (0% a 100%). La velocidad de salida es lineal y proporcional: un disparo con carga mínima (1%) debe caer a corta distancia frente al móvil.

### Módulo 2: Balística, Viento y Munición Canónica
- **RF-02.1 (Viento Radial 360°)**: Vector de viento definido por dirección angular en 360° e intensidad (0–50 m/s).
- **RF-02.2 (Inercia Progresiva)**: La velocidad inicial de salida domina el primer tramo; el vector de viento aplica su aceleración acumulativa en `FixedUpdate()`, curvando la trayectoria sin devolver la bala en el primer frame.
- **RF-02.3 (Munición Tiro 1)**: Proyectil base con daño medio, cráter regular y bajo costo de delay (+250).
- **RF-02.4 (Munición Tiro 2)**: Proyectil doble o pesado con mayor daño y radio de impacto, con recargo moderado de delay (+400 a +500).
- **RF-02.5 (Disparo Especial SS)**: Proyectil de alto impacto bloqueado por rondas de recarga (cooldown), con la penalización de delay más elevada (+800).

### Módulo 3: Terreno Destructible y Bunge
- **RF-03.1 (Deformación de Textura)**: Impacto balístico que perfora círculos en la textura dinámica en memoria (`Texture2D`), recalculando dinámicamente el `PolygonCollider2D`.
- **RF-03.2 (Muerte por Caída - Bunge)**: Si el suelo bajo un móvil es destruido por completo, el vehículo cae por gravedad al vacío activando la zona de muerte inferior (`DeathZone`).

### Módulo 4: Delay, Turnos e Ítems Tácticos
- **RF-04.1 (Línea de Turnos por Delay)**: El orden de juego se calcula según el acumulador temporal de cada jugador (movimiento + tipo de tiro + potencia consumida). Quien tenga menor delay toma el siguiente turno (permitiendo dobles turnos).
- **RF-04.2 (Ítems de Turno)**:
  - `Dual`: Permite dos disparos consecutivos en el mismo turno sumando recargo de delay (+250).
  - `Teleport`: Proyectil baliza que transporta el móvil al punto de impacto sin destruir terreno ni hacer daño.
  - `Heal`: Restaura un porcentaje de vida al móvil activo.

### Módulo 5: Satélites y Clima Global
- **RF-05.1 (Rotación de Climas)**: `SatelliteManager` cambia de estado entre turnos (Normal, Thor, Force, Viento Extremo).
- **RF-05.2 (Rayo Celestial Thor)**: Al impactar una bala durante la fase Thor, cae un rayo vertical en las coordenadas X del proyectil causando daño extra y un segundo cráter.

### Módulo 6: Red y Multijugador Autoritativo (Fase 4)
- **RF-06.1 (Topología Cliente-Servidor)**: El cliente solo envía inputs validados (ángulo, potencia, munición e ítem).
- **RF-06.2 (Simulación Balística en Servidor)**: El servidor calcula la trayectoria física, resuelve las colisiones y actualiza la salud.
- **RF-06.3 (Replicación de Terreno)**: El servidor transmite las coordenadas de impacto vía RPC para que cada cliente perfore su textura local de manera idéntica.

## Módulo 7: Fase 4 - Multijugador Autoritativo en Red (Online 1v1)

### RF-07.1: Conexión, Lobby y Transporte de Red
- **RF-07.1.1 (Framework de Red)**: Integración del gestor de red (`NetworkManager`) utilizando un framework autoritativo (Fish-Net o Photon Fusion en modo Host/Server).
- **RF-07.1.2 (Lobby de Partida Rápida)**: Panel UI para crear sala (`Host`) o conectarse (`Client`) a través de IP local o matchmaking de prueba.
- **RF-07.1.3 (Gestión de Desconexión)**: Si un jugador pierde conexión por más de 10 segundos, el servidor declara victoria automática para el rival.

### RF-07.2: Spawning Autoritativo y Replicación de Entrada
- **RF-07.2.1 (Spawn de Tanques)**: El servidor instancia los vehículos en los extremos del mapa ($X = -5$ y $X = 5$) y asigna la autoridad de cliente (`OwnerClientId`) al conectar cada usuario.
- **RF-07.2.2 (Sincronización de Chasis)**: Replicación del desplazamiento horizontal mediante `NetworkTransform` con interpolación suave de movimiento.
- **RF-07.2.3 (Sincronización de Cañón y Giro)**: La rotación angular del cañón y la orientación (`flipX`) se replican como variables sincrónicas (`[SyncVar]` o `NetworkVariable`) para que el rival vea hacia dónde apunta el cañón en tiempo real.

### RF-07.3: Balística Autoritativa en Servidor
- **RF-07.3.1 (Comando RPC de Disparo)**: El cliente no crea la bala en local. Envía al servidor un `ServerRpc`:
  `CmdFire(float angle, float powerRatio, WeaponType weapon, bool itemActive);`
- **RF-07.3.2 (Validación Antitrampas)**: El servidor comprueba si es el turno del jugador, valida los rangos de potencia ($0.0$ a $1.0$), ángulos permitidos y disponibilidad de ítems.
- **RF-07.3.3 (Proyectil en Red)**: El proyectil físico solo calcula gravedad y viento dentro del servidor; los clientes reciben la posición para interpolar la estela visual y la cámara de seguimiento.

### RF-07.4: Sincronización de Terreno y Estado de Salud
- **RF-07.4.1 (RPC de Destrucción de Suelo)**: Al impactar la bala, el servidor calcula las coordenadas de colisión y emite un `ObserversRpc` / `ClientRpc`:
  `RpcCarveHole(Vector2 impactPosition, float explosionRadius);`
  Ambos clientes ejecutan la perforación en su propia `Texture2D` local sin transferir texturas por la red.
- **RF-07.4.2 (Salud y Bunge Autoritativo)**: Solo el servidor evalúa el daño radial, actualiza la salud sincronizada y detecta caídas en la `DeathZone` inferior.
- **RF-07.4.3 (Ciclo de Turnos por Delay Centralizado)**: El servidor acumula el delay de cada acción y otorga la autoridad del siguiente turno al jugador con menor puntaje temporal acumulado.

---

# Fase 4: Multijugador Autoritativo en Red (Online 1v1)

## 1. Visión Técnica y Arquitectura de Red
- **Topología**: Servidor Autoritativo / Host-Client utilizando Netcode moderno (Fish-Net o Photon Fusion).
- **Filosofía Antitrampas**: "Nunca confiar en el cliente". El cliente móvil/PC únicamente envía comandos de entrada de usuario validados (ángulo, potencia e identificadores de disparo). La simulación balística matemática, la aplicación del viento, las colisiones y el daño residen y se calculan exclusivamente en el servidor[cite: 1].

---

## 2. Requerimientos Funcionales y Tareas Detalladas

### Módulo 4.1: Infraestructura de Red, Conexión y Emparejamiento
- [x] **RF-4.1.1 (Network Manager y Transporte)**:
  - Configurar el GameObject raíz `[NetworkManager]` con transporte UDP de baja latencia[cite: 1].
  - Crear `NetworkGameManager.cs` derivado del comportamiento de red para gestionar estados de partida (`Offline`, `Connecting`, `InLobby`, `InGame`, `PostMatch`).
- [x] **RF-4.1.2 (Lobby UI de Conexión)**:
  - Crear un panel visual en Canvas con los botones:
    - `Btn_Host`: Inicia como Servidor autoritativo + Cliente local[cite: 1].
    - `Btn_JoinClient`: Conexión de cliente por IP local (pruebas) o código de sala.
  - Etiqueta de estado en tiempo real (`Txt_ConnectionStatus`).
- [x] **RF-4.1.3 (Gestión de Desconexión y Abandono)**:
  - Detección de pérdida de paquetes o desconexión abrupta de un cliente[cite: 1].
  - Si un jugador se desconecta, el servidor otorga la victoria técnica al jugador restante tras 10 segundos.

### Módulo 4.2: Spawning Autoritativo y Replicación de Móviles
- [x] **RF-4.2.1 (Instanciación de Móviles por Servidor)**:
  - El servidor instancia los dos prefabs de tanques en puntos de partida opuestos ($X = -5$ y $X = 5$)[cite: 1].
  - Asigna `OwnerClientId` para que cada cliente solo posea autoridad de entrada sobre su propio vehículo[cite: 1].
- [x] **RF-4.2.2 (Sincronización Cinemática del Móvil)**:
  - Integrar `NetworkTransform` en el prefab del tanque para sincronizar movimiento horizontal en el terreno con interpolación[cite: 1].
  - Sincronizar la orientación visual (`flipX` / escala X del sprite)[cite: 1].
- [x] **RF-4.2.3 (Sincronización de Puntería)**:
  - Replicar la rotación del cañón y el indicador visual de ángulo hacia los clientes mediante variable de red sincronizada (`NetworkVariable` o `[SyncVar]`)[cite: 1].

### Módulo 4.3: Disparo y Balística Autoritativa en Servidor
- [x] **RF-4.3.1 (Comando RPC de Disparo)**:
  - El cliente NO genera la bala localmente al soltar la barra/pantalla[cite: 1].
  - Emite un comando hacia el servidor:
    `CmdFire(float angle, float powerRatio, WeaponType weapon, ItemType activeItem)`[cite: 1].
- [x] **RF-4.3.2 (Validación Antitrampas en Servidor)**:
  - El servidor comprueba si es el turno legítimo del cliente emisor[cite: 1].
  - Valida que `powerRatio` esté en el rango legal $[0.0, 1.0]$[cite: 1].
  - Comprueba en base de datos si el ítem solicitado está disponible en el inventario[cite: 1].
- [x] **RF-4.3.3 (Simulación Balística en Host)**:
  - El servidor instancia el proyectil físico en red[cite: 1].
  - Aplica la aceleración de gravedad y el vector de viento actual del servidor[cite: 1].
  - Los clientes reciben actualizaciones continuas de posición para renderizar el sprite, la estela (`TrailRenderer`) y guiar la cámara dinámica[cite: 1].

### Módulo 4.4: Sincronización de Terreno Destructible y Salud
- [x] **RF-4.4.1 (RPC de Deformación de Mapa)**:
  - Al impactar la bala, el servidor calcula las coordenadas exactas y emite un `ObserversRpc` / `ClientRpc`:
    `RpcCarveHole(Vector2 impactPoint, float holeRadius)`[cite: 1].
  - Ambos clientes ejecutan `CarveHole()` de manera local en su textura `Texture2D`, manteniendo el mapa sincronizado al píxel sin transmitir imágenes pesadas por la red[cite: 1].
- [x] **RF-4.4.2 (Salud Autoritativa)**:
  - `Health.cs` se procesa únicamente en el servidor[cite: 1].
  - Al recibir daño radial, el servidor descuenta los puntos y actualiza una variable sincronizada que refresca la barra de vida (`HealthBar`) en los clientes[cite: 1].
- [x] **RF-4.4.3 (Detección de Caída / Bunge en Servidor)**:
  - Solo el servidor evalúa la colisión con la `DeathZone` inferior[cite: 1].
  - Si un tanque cae por falta de tierra, el servidor emite el evento de muerte instantánea por bunge[cite: 1].

### Módulo 4.5: Cola de Delay y Pase de Turnos en Red
- [x] **RF-4.5.1 (Contador de Delay Central)**:
  - El servidor lleva el registro numérico de delay acumulado de ambos jugadores (distancia movida + tipo de arma disparada + potencia empleada)[cite: 1].
- [x] **RF-4.5.2 (Asignación Remota de Turno)**:
  - Tras el impacto, el servidor determina quién acumuló menor delay y actualiza `ActivePlayerNetworkId`[cite: 1].
  - El cliente seleccionado recibe la habilitación de sus controles táctiles/teclado, reiniciando el temporizador de 20 segundos.

---

# Fase 5: Garaje de Móviles, Sistema de Ítems Avanzados y Múltiples Escenarios 2.5D

## 1. Visión Técnica y Arquitectura
- **Personalización y Variedad Táctica**: Permitir a los jugadores seleccionar su móvil (ej. Armor, Mage, Boomer) antes de entrar a la sala multijugador o durante la fase de preparación en el Lobby.
- **Diferenciación Balística y Estadísticas**: Cada móvil posee estadísticas propias (HP, Armadura, Ángulo de elevación mín/máx, Movilidad) y proyectiles con comportamientos físicos únicos (ej. boomerang curvado por viento, rayo láser energético).
- **Múltiples Mapas 2.5D**: Soporte para cambio de mapas destructibles con diferentes texturas de terreno, biomas y física de fondo parallax.

---

## 2. Requerimientos Funcionales y Tareas Detalladas

### Módulo 5.1: Garaje y Selección de Móviles (Mobile Selection System)
- [x] **RF-5.1.1 (Selector de Tanque en Lobby)**: Interfaz UI en el lobby antes de iniciar la partida donde P1 y P2 eligen su vehículo (ej. *Armor Tank*, *Mage Mobile*, *Boomer Mobile*).
- [x] **RF-5.1.2 (Sincronización en Red de Selección)**: El servidor almacena el `MobileType` de cada jugador y replica los prefabs correspondientes al instanciar los vehículos (`NetworkSpawnerManager`).
- [x] **RF-5.1.3 (Estadísticas y Límites Angulares por Móvil)**: Configuración vía `ScriptableObject` (`MobileData.cs`) con salud máxima, armadura, rango de elevación angular (ej. Armor: 15°–60°, Boomer: 10°–75°) y delay base.

### Módulo 5.2: Balística y Disparos Únicos por Vehículo
- [x] **RF-5.2.1 (Munición Específica de Móvil)**: Implementación del comportamiento especial de proyectiles Tiro 1, Tiro 2 y SS según el tanque seleccionado:
  - *Armor*: Misiles de metralla pesada de alto cráter.
  - *Mage*: Proyectil de plasma energético que ignora parte del terreno o penetra en línea recta.
  - *Boomer*: Proyectil liviano ultrasensible al viento con trayectoria parabólica de retorno.
- [x] **RF-5.2.2 (Sincronización Autoritativa de Proyectiles Especiales)**: El servidor valida los identificadores de proyectil y ejecuta los algoritmos físicos autoritativos en host, replicando los VFX/SFX a clientes.

### Módulo 5.3: Inventario de Ítems Avanzados y Carga Táctica
- [ ] **RF-5.3.1 (Selección de Ítems de Combate)**: Menú de selección de ítems en el Lobby (máximo 6 slots de inventario con peso de ítems).
- [ ] **RF-5.3.2 (Nuevos Ítems Tácticos)**:
  - `Change Wind`: Cambia la dirección e intensidad del viento autoritativamente.
  - `Shield / Barrier`: Otorga un escudo de fuerza temporal que absorbe 200 HP de daño.
  - `Dual+`: Permite disparar dos tiros especiales consecutivamente acumulando alto delay (+500).

### Módulo 5.4: Múltiples Escenarios y Selección de Mapa
- [ ] **RF-5.4.1 (Selector de Mapas en Lobby)**: El Host selecciona el escenario antes de iniciar (ej. *Miramo Town*, *Metamine*, *Cozy Cave*).
- [ ] **RF-5.4.2 (Carga Dinámica de Terreno)**: Sistema de `DestructibleTerrain` que carga texturas de colisión y sprites parallax dinámicamente según la elección del mapa en red.

---

# Fase 6: Autenticación, Menú Principal, Lista de Salas (Room Browser) y Sala de Espera

## 1. Visión Técnica y Flujo de Usuario
- **Experiencia de Usuario Móvil Estilo Gunbound**: Flujo completo de navegación UI desde el arranque del juego: Pantalla de Login -> Menú Principal -> Lista de Salas / Matchmaking -> Sala de Espera (Room Ready Screen) -> Escenario de Combate.
- **Persistencia y Perfil**: Almacenamiento local del nombre de jugador (`PlayerPrefs`) y estado de autenticación.
- **Transición de Pantallas Sincronizada**: Estado de preparación (Ready) en la sala de espera autoritativo en red antes de permitir la transición al mapa de juego.

---

## 2. Requerimientos Funcionales y Tareas Detalladas

### Módulo 6.1: Pantalla de Login y Perfil de Usuario
- [x] **RF-6.1.1 (Login & Nickname)**: Panel de inicio donde el jugador ingresa su Nickname, selecciona su avatar e inicia sesión.
- [x] **RF-6.1.2 (Persistencia Local)**: Guardado automático de credenciales y avatar en `PlayerPrefs` (`UserDataManager.cs`).

### Módulo 6.2: Menú Principal (Main Hub UI)
- [x] **RF-6.2.1 (Navegación Principal)**: Panel de inicio con opciones de *Partida Rápida*, *Buscador de Salas*, *Garaje de Tanques* y *Ajustes*.
- [x] **RF-6.2.2 (Banner de Jugador)**: Header con avatar, nombre de usuario y estado de conexión en tiempo real.

### Módulo 6.3: Buscador y Lista de Salas (Room Browser)
- [x] **RF-6.3.1 (Lista de Salas Activas)**: Panel visual con la lista de salas disponibles (ID, Nombre de Sala, Jugadores 1/2, Mapa y Estado).
- [x] **RF-6.3.2 (Creación de Sala Personalizada)**: Modal para crear sala con nombre personalizado, configuración de IP/Puerto o Matchmaking y contraseña opcional.

### Módulo 6.4: Sala de Espera (Room Ready Screen)
- [x] **RF-6.4.1 (Slots de Jugadores P1/P2)**: Tarjetas visuales para P1 (Host) y P2 (Client) mostrando avatar, Nickname, vehículo elegido y estado de preparación.
- [x] **RF-6.4.2 (Sincronización de Ready)**: Botón de **"¡LISTO! / INICIAR"** con sincronización `NetworkVariable` en Netcode que habilita al Host para lanzar el combate solo cuando ambos están listos.
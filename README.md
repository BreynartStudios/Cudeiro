# Cudeiro

> Un homenaje jugable a **Humor Amarillo**, el mítico programa de los 90 en el que cientos de concursantes se lanzaban a superar pruebas imposibles… y casi siempre acababan en el agua.

**Cudeiro** es un juego multijugador en tercera persona desarrollado en Unity. El objetivo es recrear el espíritu de *Humor Amarillo* (*Takeshi's Castle*): pruebas absurdas y muy difíciles, caídas espectaculares y muchos jugadores intentando llegar al final a la vez.

![Un concursante dentro de una de las celdas del laberinto](docs/images/laberinto.png)

El juego se publicará en dos versiones:

- 🥽 **VR**: para vivir las pruebas en primera persona con visores de realidad virtual.
- 🖥️ **No VR**: para PC (y otras plataformas) con mando o teclado y ratón.

---

## Estado actual

El proyecto está en una **fase temprana**. Por ahora tiene:

- **Base multijugador**: el proyecto parte de la plantilla oficial *Multiplayer Third Person* de Unity, que incluye:
  - Jugador en tercera persona con `CharacterController`, cámara Cinemachine y animaciones.
  - Red con **Netcode for GameObjects** y **Unity Multiplayer Services** (sesiones, Quick Join, unirse por código o desde una lista).
  - Módulos de ejemplo *Core*, *Platformer* y *Shooter*, con sus escenas de prueba.
- **Primera prueba: El Laberinto** (`Assets/Prefabs/EscenaLaberinto.prefab`)
  - Laberinto de celdas conectadas por puertas.
  - **Puertas batientes estilo cantina del oeste** (`Assets/Scripts/Laberinto/PuertaBatiente.cs`): se abren hacia el lado contrario al que llega el jugador y, cuando ha pasado, se cierran balanceándose adelante y atrás hasta pararse. Cada cliente simula las puertas por su cuenta a partir de las posiciones sincronizadas de los jugadores, así que no generan tráfico de red.

### Escenas principales

| Escena | Descripción |
| --- | --- |
| `Assets/Core/TestScenes/[BB] Core.unity` | Escena de pruebas con el laberinto |
| `Assets/Core/TestScenes/[BB] Core MultiplayerSession.unity` | Escena de pruebas con sesión multijugador |
| `Assets/Blocks/MultiplayerSession/Scenes/*` | Ejemplos de conexión (Quick Join, código, listado) |

## Hoja de ruta

- [x] Base multijugador en tercera persona
- [x] Laberinto con puertas batientes
- [ ] Más pruebas inspiradas en el programa
- [ ] Eliminación de concursantes y flujo de partida completo
- [ ] Narración y ambientación con humor
- [ ] Versión VR (XR Interaction Toolkit / OpenXR)
- [ ] Versión no VR pulida y publicación

## Requisitos

- **Unity 6000.3.9f1** (Unity 6.3)
- Universal Render Pipeline (URP)
- Proyecto vinculado a Unity Cloud para usar Multiplayer Services

## Cómo empezar

1. Clona el repositorio:
   ```bash
   git clone https://github.com/BreynartStudios/Cudeiro.git
   ```
2. Abre la carpeta con Unity Hub usando la versión **6000.3.9f1**.
3. Abre `Assets/Core/TestScenes/[BB] Core.unity` y pulsa **Play**.
4. Para probar varios jugadores en local, usa **Multiplayer Play Mode** (`Window > Multiplayer > Multiplayer Play Mode`).

## Comunidad

El desarrollo del juego se sigue y se comenta en nuestro canal de Telegram:

👉 **[t.me/+R7MH5LoKTJXMdkK0](https://t.me/+R7MH5LoKTJXMdkK0)**

## Tecnologías

- Unity 6 · URP · Input System · Cinemachine 3
- Netcode for GameObjects 2 · Unity Transport · Multiplayer Services
- Multiplayer Play Mode y Multiplayer Tools

---

*Homenaje independiente, sin relación oficial con los titulares de los derechos de* Takeshi's Castle *ni de* Humor Amarillo.

© Breynart Studios

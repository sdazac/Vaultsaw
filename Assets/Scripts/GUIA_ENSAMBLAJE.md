# 🎮 SawBall Game — Guía de Ensamblaje en Unity 6

## Scripts generados

| Script | Carpeta | Descripción |
|---|---|---|
| `GameManager.cs` | Core | Singleton central: estado, monedas, score, high score |
| `CameraController.cs` | Core | Cámara lateral 2.5D con suavizado |
| `PlayerController.cs` | Player | Control de la bola, salto, propulsión, colisiones |
| `SawBallMeshGenerator.cs` | Player | Genera la malla de sierra proceduralmente |
| `LevelManager.cs` | Level | Spawn infinito de chunks y monedas |
| `TuneSection.cs` | Level | Segmento de cajas con costo calculado según monedas |
| `ChunkBuilder.cs` | Level | Helper para construir chunks modulares |
| `ObstacleBehavior.cs` | Level | Obstáculos estáticos/oscilantes/rotativos |
| `DestructibleBox.cs` | Destruction | Caja con HP de monedas, shake, materiales |
| `CoinBehavior.cs` | Coins | Moneda con flotación, giro y magnetismo |
| `UIManager.cs` | UI | HUD, Game Over, inicio, animaciones de UI |
| `AudioManager.cs` | Audio | Singleton de audio, música, SFX |

---

## PASO 1 — Configurar la escena base

### Hierarchy inicial:
```
Main Camera
  └─ [CameraController.cs]
GameManager (Empty)
  └─ [GameManager.cs]
  └─ [AudioManager.cs]
LevelManager (Empty)
  └─ [LevelManager.cs]
Player
  └─ [PlayerController.cs]
  └─ [SawBallMeshGenerator.cs]
  └─ [Rigidbody] (Use Gravity = OFF)
  └─ [SphereCollider] (Is Trigger = ON)
Canvas (UI)
  └─ [UIManager.cs]
  └─ HUDPanel/
  └─ StartScreen/
  └─ GameOverScreen/
```

---

## PASO 2 — Configurar el Jugador (Player)

1. Crea un **Empty GameObject** → nombra `Player`
2. Agrega los componentes:
   - `SawBallMeshGenerator` (genera la malla automáticamente al Play)
   - `PlayerController`
   - `Rigidbody` → `Use Gravity = OFF`, `Freeze Z Position`, `Freeze X/Y Rotation`
   - `SphereCollider` → `Is Trigger = ON`, `Radius ≈ 0.45`
3. En `PlayerController` Inspector:
   - `Floor Y = -2.5` / `Ceiling Y = 2.5`
   - `Jump Duration = 0.18`
   - Asigna los materiales `normalMaterial` (gris) y `propulsionMaterial` (azul brillante)
4. En `SawBallMeshGenerator`:
   - `Inner Radius = 0.35` / `Outer Radius = 0.55` / `Teeth Count = 12`

**Tags necesarios** (Edit → Project Settings → Tags):
- `Coin`
- `Obstacle`
- `DestructibleBox`

---

## PASO 3 — Crear Prefabs de Chunks Normales

Crea 3 variaciones (para variedad visual):

### Chunk_A (básico con obstáculos bajos):
```
Chunk_A (Empty)
  └─ [ChunkBuilder.cs]
  └─ Floor (Cube, scale: 20 x 0.5 x 2, pos: 10, -3, 0)
  └─ Ceiling (Cube, scale: 20 x 0.5 x 2, pos: 10, 3, 0)
  └─ Obstacle_01 (Cube, tag: Obstacle, [ObstacleBehavior])
       pos: 6, -2.5, 0 — Type: Static
```

### Chunk_B (obstáculo oscilante):
```
Chunk_B (Empty)
  └─ Floor + Ceiling
  └─ Obstacle_Swing [ObstacleBehavior]
       Type: Oscillating, Amplitude: 2.0, Frequency: 0.8
```

### Chunk_C (vacío / respiración):
```
Chunk_C (Empty)
  └─ Floor + Ceiling (sin obstáculos)
```

**Importante para obstáculos:**
- Agrega `BoxCollider` con `Is Trigger = ON`
- Asigna tag `Obstacle`

Guarda cada uno como **Prefab** en `Assets/Prefabs/Chunks/`

---

## PASO 4 — Crear Prefab de Moneda

```
Coin (Empty)
  └─ [CoinBehavior.cs]
  └─ Cylinder (scale: 0.3 x 0.05 x 0.3) — Material: amarillo metálico
  └─ SphereCollider: Is Trigger = ON, Radius = 0.3
```

- Tag: `Coin`
- Layer: `Coins` (opcional para optimización)

---

## PASO 5 — Crear Prefab TuneSection (zona de destrucción)

```
TuneSection (Empty)
  └─ [TuneSection.cs]
  └─ Floor + Ceiling (igual que chunks normales)
  └─ BoxSpawn_01 (Empty, localPos: -2.5, 0, 0)
  └─ BoxSpawn_02 (Empty, localPos: 0, 0, 0)
  └─ BoxSpawn_03 (Empty, localPos: 2.5, 0, 0)
  └─ WarningSign (Quad con sprite "⚠", localPos: -8, 0, 0)
```

En `TuneSection` Inspector:
- Arrastra los 3 `BoxSpawn` transforms al array `Box Spawn Points`
- Asigna el prefab `DestructibleBox` en `Destructible Box Prefab`

---

## PASO 6 — Crear Prefab DestructibleBox

```
DestructibleBox (Empty)
  └─ [DestructibleBox.cs]
  └─ Cube (scale: 1 x 1 x 1) — Ref en meshRenderer
  └─ [BoxCollider]: Is Trigger = ON
  └─ CostLabel (TextMeshPro 3D — World Space)
       localPos: 0, 0.8, 0, scale: 0.5
```

3 materiales para la caja:
- `intactMaterial` → Color normal (ej. naranja)
- `damagedMaterial` → Color amarillo
- `criticalMaterial` → Color rojo

Tag: `DestructibleBox`

---

## PASO 7 — Configurar LevelManager

Selecciona el `LevelManager` y en el Inspector:
- `Normal Chunk Prefabs`: arrastra Chunk_A, Chunk_B, Chunk_C
- `Tune Section Prefab`: arrastra TuneSection
- `Coin Prefab`: arrastra Coin
- `Chunk Width`: `20`
- `Initial Chunks`: `5`
- `Distance Between Tunes`: `80`
- `Scroll Speed`: `8`
- `Coins Per Chunk`: `4`
- `Player Transform`: arrastra el Player

---

## PASO 8 — Configurar la Cámara

En `CameraController`:
- `Target`: arrastra el Player
- `Offset`: `(0, 0, -15)`
- `Lead Offset`: `3`
- `Smooth Speed`: `8`

Configura la cámara en modo **Perspectiva**, `Field of View: 60`
Posición inicial: `(0, 0, -15)`

---

## PASO 9 — UI (Canvas)

### HUDPanel contiene:
- `TextMeshProUGUI` → Monedas (top-left)
- `TextMeshProUGUI` → Score (top-right)
- `Image` → Indicador de Propulsión (circle, bottom-right)
  - Color azul cuando activo, gris cuando inactivo
- `Image` → Ícono de moneda (junto al contador)
- `GameObject` → BoxWarningPanel (centrado, desactivado por defecto)

### StartScreen contiene:
- Título, instrucciones, botón `Jugar`

### GameOverScreen contiene:
- "GAME OVER"
- `TextMeshProUGUI` → Puntuación final
- `TextMeshProUGUI` → Récord
- `TextMeshProUGUI` → "¡Nuevo Récord!" (desactivado por defecto)
- Botón `Reintentar` / `Menú`
- Agrega `CanvasGroup` para el fade-in

---

## PASO 10 — Controles

| Acción | Teclado | Mouse |
|---|---|---|
| Saltar entre pisos | `Space` | Clic izquierdo |
| Activar Propulsión | `B` | Clic derecho |

---

## PASO 11 — Materiales recomendados (URP/Built-in)

| Material | Color | Shader |
|---|---|---|
| Jugador Normal | Gris metálico (#808080) | Standard / Lit |
| Jugador Propulsión | Azul eléctrico (#2277FF) + Emission | Standard Emission |
| Moneda | Dorado (#FFD700) + Metallic 0.9 | Standard |
| Caja Intacta | Naranja (#FF6600) | Standard |
| Caja Dañada | Amarillo (#FFAA00) | Standard |
| Caja Crítica | Rojo (#FF2200) | Standard |
| Piso/Techo | Gris oscuro (#333333) | Standard |
| Obstáculo | Rojo oscuro (#990000) + Emission | Standard Emission |

---

## PASO 12 — Partículas

Crea los siguientes **Particle Systems** como prefabs:

### CoinPickupParticles:
- Shape: Sphere, Radius 0.1
- Start Color: Gold gradient
- Start Speed: 3-6, Start Lifetime: 0.5
- Emission burst: 8 partículas

### DeathParticles:
- Shape: Sphere
- Start Color: Rojo → Naranja
- Start Speed: 5-10, Start Lifetime: 1.0
- Emission burst: 20 partículas

### PropulsionTrail (adjunto al Player):
- Renderer: Trail
- Start Color: Azul (#0055FF, alpha 0.7 → 0)
- Start Size: 0.3 → 0, Lifetime: 0.3

---

## Flujo de Juego (resumen)

```
[StartScreen] → Jugador presiona Jugar
        ↓
GameManager.StartGame()
        ↓
LevelManager genera chunks + monedas al vuelo
        ↓
Cada ~80u de distancia → spawn TuneSection
   TuneSection.InitializeSection()
   → Lee GameManager.Coins
   → Calcula 1-3 cajas con costo ≈ monedas del jugador
        ↓
Jugador activa Propulsión (B / Clic derecho) → azul
Choca con caja → DestructibleBox.TakeHit()
   → GameManager.SpendCoins()
   → Si 0 monedas → GameOver
   → Si HP = 0 → Caja destruida
        ↓
GameOver → UIManager.ShowGameOver()
   → Guarda HighScore en PlayerPrefs
```

---

## Notas adicionales

- **Unity 6**: usa `FindFirstObjectByType<T>()` en lugar del obsoleto `FindObjectOfType<T>()` ✅ (ya incluido en los scripts)
- **Audio**: Los `AudioClip` deben asignarse manualmente. Puedes usar sonidos libres de royalties de freesound.org
- **Dificultad**: El `scrollSpeed` aumenta automáticamente con el tiempo vía `speedIncreasePerSecond`
- **Chunks modulares**: añade más prefabs de chunk al array de `LevelManager` para más variedad sin cambiar código

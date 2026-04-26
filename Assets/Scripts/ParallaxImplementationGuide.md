# EasyParallax Implementation Guide for Vaultsaw

## Step 1: Create MovementSpeedType Scriptable Objects

Create 4 new MovementSpeedType assets for different parallax layers:

### Location: `Assets/Settings/ParallaxSpeeds/` (create this folder)

1. **Right-click** in the folder → Create → Easy Parallax → Movement Speed Type
2. Name it `BackgroundSky`
   - Set Speed: `1.0` (or match your slowest layer)
3. Create `BackgroundFarMountain`
   - Set Speed: `2.0` (slower than gameplay)
4. Create `BackgroundMidClouds`
   - Set Speed: `4.0` (medium speed)
5. Create `BackgroundCloseMountain`
   - Set Speed: `6.0` (faster than far layer)

**Speed Reference:**
- Your game scrolls at: 6-20 units/sec
- Parallax should be slower:
  - Far layer: 1-2 units/sec
  - Mid layer: 3-5 units/sec
  - Close layer: 5-8 units/sec

---

## Step 2: Set Up Sprites in Your Scene

### Create a new parent GameObject: `ParallaxLayers`
- Position: (0, 0, 1) ← Put it BEHIND gameplay chunks (Z > -5)

### Add 4 Child GameObjects with Sprites:

**Layer 1: Sky (Far Background)**
- Create GameObject: `Sky_Layer`
- Add **SpriteRenderer** component
  - Sprite: `Assets/Scripts/EasyParallax/Demo/Assets/Sky.png`
  - Order in Layer: 0
- Add **SpriteMovement** component
  - Movement Speed Type: `BackgroundSky`
- Position: (0, 0, 2)
- Scale: Adjust to fill screen width (try 6x6)

**Layer 2: Far Mountains**
- Create GameObject: `MountainFar_Layer`
- Add **SpriteRenderer** component
  - Sprite: `MountainFar.png`
  - Order in Layer: 1
- Add **SpriteMovement** component
  - Movement Speed Type: `BackgroundFarMountain`
- Add **SpriteDuplicator** component (enable seamless scrolling)
  - Pool Size: 3
  - Sprite Reposition Index: 2
  - Sprite Reposition Correction: 0.03
- Position: (0, -0.5, 1)

**Layer 3: Mid Clouds**
- Create GameObject: `Clouds_Layer`
- Add **SpriteRenderer** component
  - Sprite: `Clouds.png`
  - Order in Layer: 2
- Add **SpriteMovement** component
  - Movement Speed Type: `BackgroundMidClouds`
- Add **SpriteDuplicator** component
- Position: (0, 1.5, 0.5)

**Layer 4: Close Mountains**
- Create GameObject: `MountainClose_Layer`
- Add **SpriteRenderer** component
  - Sprite: `MountainClose.png`
  - Order in Layer: 3
- Add **SpriteMovement** component
  - Movement Speed Type: `BackgroundCloseMountain`
- Add **SpriteDuplicator** component
  - Pool Size: 3
- Position: (0, -1.5, 0)

---

## Step 3: Z-Axis Ordering (Critical!)

Ensure proper depth ordering:

```
Z = +2.0  → Sky (stationary or very slow)
Z = +1.0  → Far Mountains (slow)
Z = +0.5  → Mid Clouds (medium)
Z = 0.0   → Close Mountains (faster)
Z = -5.0  → Game Chunks (gameplay)
Z = -6.0  → Player (saw-blade)
Z = -100  → UI/Canvas
```

---

## Step 4: Synchronize with LevelManager Scrolling

### Important: Independent Movement

The EasyParallax system moves sprites independently at fixed speeds. Your LevelManager scrolls chunks at increasing speed (6→20 units/sec).

**Solution Options:**

**Option A: Static Parallax (Simplest)**
- Keep parallax speeds constant (1-8 units/sec)
- Parallax moves slower than gameplay
- Result: Parallax "lags behind" as game speeds up (realistic depth effect)
- ✅ Recommended for polish

**Option B: Dynamic Parallax (Advanced)**
- Modify MovementSpeedType scripts to read from LevelManager.GetScrollSpeed()
- Parallax scales proportionally with game speed
- Result: Parallax always maintains fixed ratio

---

## Step 5: Testing Checklist

- [ ] All 4 sprite layers visible
- [ ] Sprites scroll left smoothly
- [ ] No gaps between duplicated sprites (adjust SpriteDuplicator.spriteRepositionCorrection)
- [ ] Closer sprites move faster than distant ones
- [ ] Sprites don't clip through gameplay chunks
- [ ] Camera background no longer visible (or is hidden behind parallax)
- [ ] Play for 30+ seconds without visual issues

---

## Step 6: Fine-Tuning

### Adjust Sprite Scale
If sprites are too small or too large:
- Select layer GameObject
- Adjust Scale.x and Scale.y

### Adjust Vertical Position
If sprites don't align well:
- Move Y position up/down
- Rotate slightly if needed

### Close Gaps Between Sprites
If SpriteDuplicator creates gaps:
- Increase `Sprite Reposition Correction` (0.03 → 0.05)
- Or decrease `Sprite Reposition Index` (2 → 1)

### Change Colors
If you want to tint the parallax layers:
- Add a SpriteRenderer Color override
- Or use a material with color properties

---

## Demo Scene Reference

The EasyParallax package includes `Demo/ParallaxTest.unity` which shows:
- How to arrange layers
- Proper positioning
- Example speed values

You can open it to reference the setup!

---

## Troubleshooting

**Problem:** Sprites disappear after a while
- Solution: SpriteDuplicator not working. Make sure component is added and enabled.

**Problem:** Sprites move at wrong speed
- Solution: Check MovementSpeedType is assigned to SpriteMovement. Verify speed value.

**Problem:** Sprites have gaps
- Solution: Adjust `spriteRepositionCorrection` in SpriteDuplicator (0.01 to 0.1 range).

**Problem:** Parallax overlaps with gameplay
- Solution: Check Z-axis values. Parallax should be > 0, chunks at -5 to -6.

**Problem:** Parallax speeds don't match game speed
- Solution: This is intentional! Parallax should move slower for depth effect.
  - If you want them to sync, see "Option B" in Step 4.

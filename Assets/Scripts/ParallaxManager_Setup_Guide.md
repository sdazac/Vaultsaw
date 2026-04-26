# 3D Parallax Manager Implementation Guide

## Overview

The `ParallaxManager` script creates and manages 3D planes positioned at different depths to create a parallax scrolling effect that works perfectly with your perspective 3D camera.

## How It Works

- **Planes at different Z depths**: Sky (Z=20), Far (Z=15), Mid (Z=10), Close (Z=5)
- **Automatic scaling**: Each plane scales proportionally to its depth so distant objects look correct
- **Speed multipliers**: Each layer scrolls at a different speed based on its depth (Sky: 10%, Far: 30%, Mid: 60%, Close: 80%)
- **Automatic recycling**: Planes reposition as they scroll off-screen to maintain seamless scrolling

## Setup Instructions

### Step 1: Create ParallaxManager GameObject

1. In your scene hierarchy, right-click → **Create Empty**
2. Name it `ParallaxManager`
3. Position at **(0, 0, 0)**
4. Add the **ParallaxManager** script component to it

### Step 2: Create Materials for Each Layer

You need 4 materials to apply your parallax textures. For each:

1. Right-click in Assets → **Create → Material**
2. Name: `ParallaxSky`, `ParallaxFarMountain`, `ParallaxMidClouds`, `ParallaxCloseMountain`
3. For each material:
   - Set Shader to **Standard**
   - Drag your texture into the **Albedo** slot
   - Optional: Adjust Metallic to 0, Smoothness to 1 for best appearance

**Textures to use:**
- Sky.png → ParallaxSky material
- MountainFar.png → ParallaxFarMountain material
- Clouds.png → ParallaxMidClouds material
- MountainClose.png → ParallaxCloseMountain material

(All found in `Assets/Scripts/EasyParallax/Demo/Assets/`)

### Step 3: Configure ParallaxManager in Inspector

1. Select **ParallaxManager** GameObject
2. In the Inspector, find the **ParallaxManager** component
3. Set **Parallax Layers** size to **4**

#### Layer 0: Sky (Background)
- **Layer Name**: Sky
- **Material**: ParallaxSky
- **Z Position**: 20
- **Base Scale**: 15
- **Speed Multiplier**: 0.1 (very slow, far background)
- **Height Scale**: 6
- **Pool Size**: 2

#### Layer 1: Far Mountains
- **Layer Name**: Far Mountains
- **Material**: ParallaxFarMountain
- **Z Position**: 15
- **Base Scale**: 12
- **Speed Multiplier**: 0.3 (slow)
- **Height Scale**: 6
- **Pool Size**: 3

#### Layer 2: Mid Clouds
- **Layer Name**: Mid Clouds
- **Material**: ParallaxMidClouds
- **Z Position**: 10
- **Base Scale**: 10
- **Speed Multiplier**: 0.6 (medium)
- **Height Scale**: 6
- **Pool Size**: 3

#### Layer 3: Close Mountains
- **Layer Name**: Close Mountains
- **Material**: ParallaxCloseMountain
- **Z Position**: 5
- **Base Scale**: 8
- **Speed Multiplier**: 0.8 (fast, closest to camera)
- **Height Scale**: 6
- **Pool Size**: 3

### Step 4: Play and Test

1. Click **Play**
2. Trigger gameplay and watch the parallax layers scroll
3. As the game speeds up, parallax layers naturally scroll slower = depth effect!

## Fine-Tuning

### If layers look too stretched/compressed:
- Adjust `Base Scale` for that layer (higher = wider, lower = narrower)
- Or adjust the Material's texture tiling

### If parallax effect is too subtle:
- Increase `Speed Multiplier` for distant layers (e.g., Sky: 0.2 instead of 0.1)
- Or make close layers even faster (e.g., Close: 0.95 instead of 0.8)

### If you see gaps between planes:
- Increase `Base Scale` slightly
- Increase `Pool Size` (adds more planes in the pool)

### If planes overlap with chunks:
- Adjust `Z Position` values (chunks are at -5 to -6)
- Move parallax further back (higher Z) or to different Z values

## Advanced: Changing Speeds at Runtime

The script provides methods to adjust speeds dynamically:

```csharp
// Get the ParallaxManager reference
ParallaxManager parallax = GetComponent<ParallaxManager>();

// Change a layer's speed multiplier (0-1)
parallax.SetLayerSpeedMultiplier(0, 0.2f); // Sky layer faster
parallax.SetLayerSpeedMultiplier(3, 0.9f); // Close layer faster

// Change a layer's material
parallax.SetLayerMaterial(0, newMaterial);
```

## Material Settings Recommendations

For best visual results on each material:

**Sky Material:**
- Shader: Standard
- Color: Light cyan/white
- Metallic: 0
- Smoothness: 1
- Emission: Optional, can add slight glow

**Mountain Materials:**
- Shader: Standard
- Metallic: 0
- Smoothness: 0.5-0.7 (slightly less shiny than sky)

**Cloud Material:**
- Shader: Transparent Standard (if transparent clouds)
- Or Standard with alpha-enabled texture

## Troubleshooting

**Problem:** Parallax layers are black or not visible
- Solution: Check materials are assigned. Verify materials have textures in Albedo slot.
- Check Z positions don't conflict with camera clipping planes.

**Problem:** Planes appear at wrong depth
- Solution: Adjust Z Position values. Higher Z = further from camera.
- Verify camera's far clipping plane is large enough (should be >100).

**Problem:** Layers move too fast or too slow
- Solution: Adjust Speed Multiplier. Lower = slower, higher = faster.
- Sky should be slowest (0.1-0.2), close should be fastest (0.7-0.95).

**Problem:** Parallax doesn't sync with chunk scrolling
- Solution: This is intentional! Parallax moves slower = depth effect.
- If you want perfect sync, set all Speed Multipliers to 1.0.

**Problem:** Gaps appear between recycled planes
- Solution: Increase Base Scale or Pool Size.

## Performance Notes

- Each layer uses 2-3 planes (configurable via Pool Size)
- Total planes: ~8-12 across all layers (very lightweight)
- No per-frame instantiation/destruction (planes recycled)
- Should have minimal performance impact

## Next Steps

1. Create the materials with the parallax textures
2. Configure the ParallaxManager with the 4 layers
3. Adjust Z positions so parallax is behind chunks (Z > -5)
4. Play and fine-tune speeds and scales until it looks right!

# GyroMaze3D - Unity Mobile Maze Game

## Overview
A mobile maze game with 100 levels featuring gyroscopic controls, where players tilt their device to guide a metal ball through increasingly complex mazes. Every 10 levels, the theme and visual style change dramatically.

## Features

### Core Gameplay
- **100 Unique Levels**: Procedurally generated mazes with increasing difficulty
- **Gyroscopic Controls**: Tilt your device to roll the metal ball through the maze
- **Touch Joystick**: Alternative touch controls for precise movement
- **Progressive Difficulty**: Mazes become larger and more complex as you advance

### Visual Themes (Changes Every 10 Levels)
1. **Classic Steel** (Levels 1-10): Industrial metallic environment
2. **Neon City** (Levels 11-20): Cyberpunk glowing aesthetics
3. **Ancient Stone** (Levels 21-30): Historical stone temple
4. **Ice Palace** (Levels 31-40): Frozen crystalline structures
5. **Lava Factory** (Levels 41-50): Industrial heat and danger
6. **Crystal Cavern** (Levels 51-60): Magical purple crystals
7. **Forest Temple** (Levels 61-70): Natural wooden environment
8. **Space Station** (Levels 71-80): Futuristic technology
9. **Shadow Realm** (Levels 81-90): Dark mysterious atmosphere
10. **Divine Paradise** (Levels 91-100): Heavenly golden aesthetics

### Visual Effects
- Dynamic lighting and fog that matches each theme
- Glowing goal objects with particle effects
- Ball trail renderer for motion visualization
- Animated goal with pulsing lights
- Completion particle explosions
- Metallic and reflective materials

## Project Structure

```
MazeGame/
├── Assets/
│   ├── Scripts/
│   │   ├── BallController.cs      # Player ball physics and gyro input
│   │   ├── MazeGenerator.cs       # Procedural maze generation
│   │   ├── GameManager.cs         # Level management and game state
│   │   ├── CameraFollow.cs        # Smooth camera tracking
│   │   ├── GoalController.cs      # Goal behavior and effects
│   │   └── MobileInputHandler.cs  # Gyro and touch input handling
│   ├── Prefabs/                   # Game object prefabs
│   ├── Materials/                 # Custom materials
│   ├── Scenes/                    # Unity scenes
│   ├── Textures/                  # Texture assets
│   ├── Sprites/                   # 2D sprite assets
│   └── Audio/                     # Sound effects and music
└── ProjectSettings/
    └── ProjectSettings.asset      # Unity project configuration
```

## Installation & Setup

### Prerequisites
- Unity 2021.3 LTS or newer
- Android SDK & NDK (for Android builds)
- Xcode (for iOS builds)

### Setup Instructions

1. **Open Project in Unity**
   ```
   - Launch Unity Hub
   - Click "Add" and select the MazeGame folder
   - Open the project
   ```

2. **Configure Build Settings**
   ```
   - File > Build Settings
   - Select Android or iOS platform
   - Switch Platform
   ```

3. **Player Settings**
   ```
   - Edit > Project Settings > Player
   - Set Company Name and Product Name
   - Configure orientation: Landscape Left
   - Enable gyroscope permissions
   ```

4. **Scene Setup**
   - Create a new scene or open the main game scene
   - Add an empty GameObject named "GameManager"
   - Attach the `GameManager` script
   - Add the `MazeGenerator` script to the same object
   - Add the `MobileInputHandler` script to handle input

5. **Camera Setup**
   - Select Main Camera
   - Attach the `CameraFollow` script
   - The camera will automatically find and follow the player

## How to Play

### Controls
- **Mobile (Gyro)**: Tilt device to roll the ball
- **Mobile (Touch)**: Use on-screen joystick (bottom-left)
- **Desktop Testing**: Arrow keys or WASD

### Objective
- Navigate the metal ball through the maze
- Reach the glowing goal sphere
- Complete all 100 levels!

### Tips
- Calibrate gyro by pressing 'C' if controls feel off
- Adjust sensitivity in MobileInputHandler settings
- Use pinch gesture to zoom camera in/out
- Press 'N' to skip to next level (testing)
- Press 'R' to restart current level

## Technical Details

### Maze Generation Algorithm
- Uses recursive backtracking for perfect maze generation
- Adds random loops based on difficulty level
- Ensures solvable path from start to goal
- Maze size increases with level progression

### Difficulty Progression
- **Levels 1-10**: Small mazes (8x8 to 12x12), low complexity
- **Levels 11-50**: Medium mazes (12x12 to 18x18), moderate complexity
- **Levels 51-100**: Large mazes (18x18 to 25x25+), high complexity
- Extra passages increase with level number
- Time limits may be added for challenge

### Physics Configuration
- Continuous collision detection prevents tunneling
- Realistic ball rolling with friction
- Velocity clamping for stability
- Smooth interpolation for visual quality

## Customization

### Adjusting Difficulty
Edit `MazeGenerator.cs`:
```csharp
public float baseComplexity = 0.3f;        // Starting complexity
public float complexityPerLevel = 0.008f;  // Increase per level
public float maxComplexity = 0.9f;         // Maximum cap
```

### Modifying Themes
Edit the `CreateDefaultThemes()` method in `MazeGenerator.cs` to customize colors, materials, and lighting for each theme.

### Ball Physics
Edit `BallController.cs`:
```csharp
public float moveSpeed = 15f;           // Movement speed
public float gyroSensitivity = 1.5f;    // Gyro sensitivity
public float maxVelocity = 20f;         // Speed limit
```

## Building for Mobile

### Android Build
1. File > Build Settings > Android
2. Configure keystore for signing
3. Set minimum API level to 22 (Android 5.1)
4. Enable IL2CPP scripting backend
5. Build APK or AAB

### iOS Build
1. File > Build Settings > iOS
2. Set bundle identifier
3. Configure signing team
4. Build and open in Xcode
5. Deploy to device

## Troubleshooting

### Gyro Not Working
- Ensure device has gyroscope hardware
- Check permissions in Player Settings
- Try manual calibration (press 'C')
- Verify landscape orientation lock

### Performance Issues
- Reduce maze size in later levels
- Lower particle effect quality
- Disable shadows on mobile devices
- Use simpler materials

### Ball Falls Through Floor
- Increase physics iteration count
- Enable continuous collision detection
- Check wall/floor collider integrity

## License
This project is provided as-is for educational and commercial use.

## Credits
Developed with Unity Engine
Procedural maze generation algorithm based on recursive backtracking
Custom visual themes and effects

---

For questions or support, please refer to Unity documentation or community forums.

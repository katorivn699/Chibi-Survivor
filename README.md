# Chibi-Survivor

## Description

This project is a survivor-style game developed in Unity. It features wave-based gameplay, an upgradeable player character, a shop system, and audio management for both background music and sound effects. The core functionality includes enemy spawning, player statistics, game state management (pause, resume, game over), and resolution settings.

## Features and Functionality

*   **Wave-Based Gameplay:** Enemies spawn in waves, with increasing difficulty as the game progresses.
*   **Player Stats:** Manages player health, money, and other relevant stats.
*   **Enemy Spawning:** Spawns different types of enemies, including bosses, at specified intervals.
*   **Shop System:** Allows the player to purchase upgrades between waves. Pauses game when the shop is opened.
*   **Audio Management:** Controls background music and sound effects, with adjustable volumes via UI sliders.
*   **Game State Management:** Manages game states like pause, resume, and game over.
*   **Resolution Settings:** Allows players to adjust the game's resolution and toggle fullscreen mode.
*   **Object Pooling:** Improves performance by reusing game objects instead of instantiating and destroying them repeatedly.
*   **Event System:** Provides a centralized way for different parts of the game to communicate with each other.

## Technology Stack

*   **Unity:** Game engine used for development.
*   **C#:** Programming language.
*   **TextMeshPro (TMP):**  Used for enhanced text rendering in UI elements.
*   **Unity Audio Mixer:**  Used to manage audio levels and apply effects.
*   **PlayerPrefs:**  Used for saving and loading volume, resolution and fullscreen settings.

## Prerequisites

*   Unity (version not specified, but likely 2020 or newer due to the use of `Object.FindFirstObjectByType`)
*   Basic understanding of C# and the Unity game engine.

## Installation Instructions

1.  Clone the repository:

    ```bash
    git clone https://github.com/katorivn699/Chibi-Survivor.git
    ```

2.  Open the project in Unity.
3.  Ensure that all dependencies (if any) are resolved by Unity's Package Manager.

## Usage Guide

1.  Open the desired scene in Unity (likely a "Gameplay" scene and "MainMenu" Scene).
2.  Run the scene.
3.  Use the in-game UI to interact with the game (e.g., start the game, open the shop, adjust volume).
4.  The game controls are not explicitly defined in the provided code, implying they are handled by other scripts not provided.
5.  Use the settings menu to adjust resolution and fullscreen.
6.  The resolution and volume settings are saved using `PlayerPrefs` and are loaded on game start.

## API Documentation

While there's no formal API documentation, here's a breakdown of key classes and their public methods:

### AudioController.cs

This script manages the game's audio.

*   **`PlayBGM(string name, bool fadeIn = false, float fadeTime = 1f)`**: Plays background music with an optional fade-in effect.  The `name` parameter corresponds to the name defined in the `bgmSounds` array.
*   **`StopBGM(bool fadeOut = false, float fadeTime = 1f)`**: Stops the currently playing background music, with an optional fade-out effect.
*   **`PlaySFX(string name, float volumeScale = 1f)`**: Plays a sound effect. The `name` parameter corresponds to the name defined in the `sfxSounds` array.
*   **`SetMasterVolume(float volume)`**: Sets the master volume level (0-1).
*   **`SetBGMVolume(float volume)`**: Sets the background music volume level (0-1).
*   **`SetSFXVolume(float volume)`**: Sets the sound effects volume level (0-1).

### EventManager.cs

This script implements a simple event system.

*   **`GameStarted()`**:  Invokes the `OnGameStarted` event.
*   **`WaveChanged(int waveNumber)`**:  Invokes the `OnWaveChanged` event, passing the current wave number.
*   **`GameOver()`**:  Invokes the `OnGameOver` event.
*   **`PlayerHealthChanged(float health)`**: Invokes the `OnPlayerHealthChanged` event, passing the player's current health.
*   **`MoneyChanged(int money)`**:  Invokes the `OnMoneyChanged` event, passing the player's current money.
*   **`BossSpawned()`**:  Invokes the `OnBossSpawned` event.
*   **`ShopOpened()`**: Invokes the `OnShopOpened` event.
*   **`GamePaused()`**: Invokes the `OnGamePaused` event.
*   **`GameResumed()`**: Invokes the `OnGameResumed` event.

### GameManager.cs

This script manages the overall game flow.

*   **`StartGame()`**: Starts a new game.
*   **`StartNextWave()`**: Starts the next wave of enemies.
*   **`OpenShop()`**: Opens the in-game shop.  Pauses the game.
*   **`CloseShop()`**: Closes the in-game shop. Resumes the game and starts the next wave.
*   **`PauseGame()`**: Pauses the game.
*   **`ResumeGame()`**: Resumes the game.
*   **`GameOver()`**: Ends the game.
*   **`RestartGame()`**: Restarts the game.  Uses `CircleSceneTransition` to transition back to the Gameplay scene.
*   **`LoadMainMenu()`**: Loads the main menu scene.  Uses `CircleSceneTransition` to transition back to the MainMenu scene.
*   **`QuitGame()`**: Quits the game.

### ObjectPooler.cs

This script manages object pooling for performance optimization.

*   **`SpawnFromPool(string tag, Vector3 position, Quaternion rotation)`**: Spawns an object from the pool with the given tag at the specified position and rotation.

### ResolutionManager.cs

This script handles the game's resolution settings.

*   **`SetResolution(int resolutionIndex)`**: Sets the game's resolution based on the selected index in the resolution dropdown.
*   **`SetFullscreen(bool isFullscreen)`**: Toggles fullscreen mode.
*   **`ApplyCurrentSettings()`**: Applies the currently saved resolution and fullscreen settings.

## Contributing Guidelines

1.  Fork the repository.
2.  Create a new branch for your feature or bug fix.
3.  Make your changes and commit them with clear, concise messages.
4.  Test your changes thoroughly.
5.  Submit a pull request with a detailed description of your changes.

## License Information

No license information is provided in the repository.  Therefore, all rights are reserved.

## Contact/Support Information

For questions or support, please contact the repository owner through GitHub.  Specific contact details are not provided in the repository.
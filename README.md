# Sprite Matching Game (Unity + C#)

A small Unity memory/matching-style game using sprites.
The game selects a random target sprite and the player must identify and click it within the time limit.

##  Gameplay Features

✔ Target sprite at the top  
✔ 9 random sprites per round  
✔ Timer per round  
✔ Feedback (Correct / Wrong / Time Up)  
✔ Score and highscore system  
✔ Sound effects  
✔ Restart button  

## Game Logic

- Each round generates 9 unique sprites
- One of them is randomly selected as the target
- Player clicks the correct one to gain score
- Game ends after X rounds (configurable)
- Highscore is saved using PlayerPrefs

## Tech Stack

- Unity (C#)
- TextMeshPro
- PlayerPrefs
- UI System (Buttons, Images, Canvas)

## Key Scripts

**GameManager.cs**
Handles:
- round logic
- scoring system
- timers
- UI feedback
- audio feedback

**ShowResult() & Coroutine**
Displays temporary feedback using coroutine and hides it after delay.

## Purpose

Built as a simple mini Unity game to explore:
- UI systems
- state management
- event interactions (button.onClick)
- data persistence (highscore)
- simple SFX integration

## Platform

- Windows / Mac / WebGL compatible
- (No mobile-specific functionality used)


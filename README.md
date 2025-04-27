# Sight Readia

Sight Readia is a game to improve your sheet music reading skills. Play on a PC keyboard or connect
your *MIDI* controller. Use the generator to create unlimited music scores for you. 

You can find the generator as a separate part of the project [here](https://github.com/FilipRuta/musicxml-sheet-music-generator)

## Installation

1. Download the game build from releases
2. Unpack the zip file
3. Run the game

## How to play

### Playing with PC keyboard

It is possible to use the PC keyboard as an input controller. The mapping of keys is similar to the *MIDI* keyboard layout.
- The bottom row (Z–M) corresponds to the white piano keys C3–B3
- The row above corresponds to the black piano keys (S for C#3, D for D#3, G for F#3, etc.)
- The upper row (Q–P) corresponds to the white piano keys C5–E6
- The row above similarly maps to the black keys

The game adjusts to the limited playable range and skips notes that are not within the range.

### Playing with MIDI controller

To use a *MIDI* controller:
- **Connect the keyboard to the PC before starting the game**
- **Ensure no other application (e.g., MuseScore) is using the *MIDI* keyboard**
- The game expects that the *MIDI* device supports the full piano range

### Output device

For output, you can use either:
- A *MIDI* controller with internal speakers
- Computer audio (emulated piano sounds)

If using a *MIDI* controller with its own speakers, disable direct speaker playback to avoid duplicated sound. This setting must be configured on the keyboard itself and may vary by model (check the product manual).

### Playing songs from own library

You can play songs from your library by selecting **"Select Song"** in the main menu.

When opening the game for the first time, no songs will be present. The top of the screen shows the directory where you can add your songs.

Once songs are placed in the directory, reopen the "Select Song" screen — all available songs should appear. You can scroll through the list with a mouse wheel, scrollbar, or by dragging.

After pressing play on a selected song, the game options screen will appear. These include:

- **Input Device**: Choose between *Computer Keyboard* and your *MIDI Device*
- **Output Device**: Choose between *Computer Audio* and your *MIDI Device*
- **Game Mode**:
    - **Classic**: Play the song in sequence chronologically, playing all notes with the same onset
    - **Training**: Randomize notes within a measure and repeat each note twice before advancing
- **Use Timer**: Set a time limit per note (3, 5, 10, or 20 seconds). Enables a high score system.
- **Play grand staff**: Toggle between playing a single staff or grand staff (if available)

#### Helper Options

- **Show notehead names**: Display note names inside the noteheads
- **Show staff head in every measure**: Show clef, key signature, and time signature at the beginning of every measure (always enabled in Training mode)

### Generate songs to play

To install the generator follow the instructions [here](https://github.com/FilipRuta/sight-readia). 

To generate new songs:
1. Start the generator (as described in the Generator instructions)
2. Choose **"Generate Song"** in the game’s main menu

You will see a menu with game and generation options, similar to playing songs from your library. Additional generator-specific options:

- **Generate grand staff**: Generate both treble and bass clefs
- **Key**: Choose a specific key signature or select *Random*

Note: Song generation time may vary based on hardware and whether the generator uses CPU or GPU.

### Gameplay

Once the song is loaded, sheet music will be displayed.

- Press any key (on the computer or piano keyboard) to start
- Play the displayed notes to earn points
- The score appears in the top-right corner
- If *Use Timer* is enabled, the remaining time is shown below the score
- The current game mode appears in the top-left corner

You can:
- Adjust screen size with the `+` and `−` buttons
- Pause with the `ESC` key or the **Pause** button

**Pause menu options**:
- Restart the game
- Save the song (as `.musicxml` in the songs directory). You’ll be prompted for a filename.

Saving the song is necessary to track high scores. You can also save the song from the final game screen.

Have fun!
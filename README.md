# Tap-Poica

By: ICT1011 GroupP3C

Tap-Poica is a fun rhythm game where players wirelessly connect their TinyScreen as a controller to their computer, and hit notes by shaking or tapping their TinyScreen. Tap-Poica is able to parse classic Osu! beatmaps and load them as Tap-poica levels.

## Arduino

Setup instructions:

1. File > Preferences > Additional board manager URLs > Add `https://files.tinycircuits.com/ArduinoBoards/package_tinycircuits_index.json`
2. Board manager > install `TinyCircuits SAMD Boards`
3. Library manager > install `TinyScreen` and `RTCZero`
4. Sketch > Include Library > Add .ZIP Library > Select `arduino/STBLE.zip`
5. Set board to `TinyScreen+`

Now you can compile and upload stuff to the TinyScreen+.

## Unity

Unity version: 6000.3.5f1

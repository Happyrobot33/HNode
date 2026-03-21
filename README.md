# HNode
<img src="Assets/logo.png" width="256" alt="pixelated H as HNode logo">
Open source Artnet DMX to video grid node renderer, built in unity 6, created for maximum data standard flexibility

## Features
- Optional manual change of ArtNet Address/Port
- Spout2 output as serializer
- Spout2 input as deserializer
- Many generators
- MIDIDMX
- Nerdy statistics
- Support for VRSL, Binary Stage Flight [and others](Assets/Plugin/Serializers)

## Install
Get recent version of HNode in [releases tab](https://github.com/Happyrobot33/HNode/releases)

## Usage

There are a few ways you can use HNode to run lights in world:

### HNode with OBS
<details>
<summary>Setting up HNode to work with OBS Studio</summary>
<br>

OBS Studio is a free-to-use software avaliable at [GitHub](https://github.com/obsproject/obs-studio) or Steam.
1. You have to get plugin [Spout2 for OBS](https://github.com/Off-World-Live/obs-spout2-plugin)
2. Add `Spout2 Capture` source
3. Set field `SpoutSenders` to `HNode Output` or the one you set in HNode application
4. Set Composite mode to Default to allow transparancy from HNode
    - Also you can (but not required):
        - hold `LeftAlt` and `Left Mouse Button` to crop unused part of Spout2 Capture, just don't over do it. 
        - Also you can `Right Click` on it Transform > Edit and set Crop values from there.

So now we have HNode that outputs texture into OBS, but how do we put this into VRC World or similar?

There are a few ways to do this:
- YouTube Live: High latency, possibly bad compression and okay bitrate
- Twitch: High latency, possibly bad compression and okay bitrate
- VRCDN: Low latency, okay compression and okay bitrate
- Other streaming service (Please check their ToS before doing anything)
- Local streaming ([MediaMTX](https://github.com/bluenviron/mediamtx)): Low latency, okay compression and good bitrate (depends on your hardware)

For an local-test example we'll go with [MediaMTX](https://github.com/bluenviron/mediamtx)

1. Get latest binary for your system from [MediaMTX releases](https://github.com/bluenviron/mediamtx/releases)
2. Extract it in empty folder, run main file (on Windows it's `mediamtx.exe`)
3. Go to your OBS Studio settings > Stream
4. Set Service to Custom, Server: `rtmp://localhost/`
- tip: if you want to have a custom text after your base url, you can add any text you want (it needs to be in ASCII format), if you get any errors when you try to stream - you put something bad in there probably.
5. Go to Output > Streaming
6. Set things below:
    1. Streaming Settings:
        1. Audio Encoder: `FFmpeg AAC`
        2. Video Encoder: For NVIDIA: `NVIDIA NVENC H.264`. (This is mostly depends on how good your hardware is, with great CPU you can go with `x264`)
        3. Rescale Output: Disabled
    2. Encoder Settings:
        1. Rate Control: `Constant Bitrate`
        2. Bitrate `5000 Kbps` or above for crazy visuals 
        - only if you use Binary or Ternary gridnode, otherwise it is mostly not useful
        - And don't set it too low or too high or you'll experience issues like high stream delay, low FPS etc.
        3. Keyframe interval: `1s`
        4. Preset: `Slow (Good Quality)`
        - You can set it higher if your hardware is way too good but not recomended in most cases
        5. Tuning: `High Quality`
        - You can try Ultra low latency but there's no guarantee that it'll be actually low latency since it may give you issues.
        6. Multipass Mode: `Two Passes (Quarter Resolution)`
        7. Profile: `high`
        8. Look-ahead: `Off`
        9. Adaptive Quantization (or Psycho-Visual Tuning): `Off`
        10. B-Frames: `2`
8. Go to Output > Audio
9. Set on each track `Audio Bitrate` to `320`
- If you experience audio issues, try lower values (192 works good on MediaMTX).
10. Go to Audio
11. Set things below:
- Sample Rate: `48 kHz`
- Channels: `Stereo`
12. Go to Video and set things below:
- Base (Canvas) Resolution: `1920x1080`
- Output (Scaled) Resolution: `1920x1080`
- Common FPS Values: `30` (You can set higher but if your client has less frames, you will experience stream delays and other issues, but `30` is the most common around clubs)
13. Go to Advanced and set things below:
- Color Range: `Full`

This should be good to go, Apply your settings and try Start Streaming.
Go to your world and put in video player url `rtspt://localhost:8554/(anything you put in Stream key/Server)`
</details>

### HNode with MIDIDMX
[VRC-MIDIDMX](https://github.com/micksam7/VRC-MIDIDMX)

## Troubleshooting
1. I don't have any options in `Spout2 Capture` `SpoutSenders`
- You most likely need to restart your PC
2. Lights don't do anything when I run sequences in Lighting console
- Make sure you use correct gridnode for your world
- Check if you have put your gridnode in correct place according to your world
3. I have lights run in Lighting console but not in HNode
- Make sure you have ArtNet output set on correct address and port.

## LICENSES
[HNode](LICENSE)

[VRC-MIDIDMX](https://github.com/micksam7/VRC-MIDIDMX/blob/main/LICENSE)

[KlakSpout](Packages/jp.keijiro.klak.spout@3be27c34696f/LICENSE)

[DryWetMIDI](https://github.com/melanchall/drywetmidi/blob/develop/LICENSE)

[CrcSharp](https://github.com/derek-will/CrcSharp/blob/main/LICENSE.md)

[YamlDotNet](Assets/Dependencies/YamlDotNet/YamlDotNet.license.txt)

[Graphy](Assets/Dependencies/Graphy%20-%20Ultimate%20Stats%20Monitor/LICENSE)
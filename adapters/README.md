# Adapters

| Adapter | Requirements and codecs | Tests | Status |
|---|---|---|---|
| Synthetic | FFmpeg with lavfi, x264, Opus and AAC; 720p25, two renditions | Edge browser; see HARDWARE-REPORT | Experimental: short test only |
| DeckLink input | Desktop Video and Windows SDK; SDI format to be detected | None | Planned |
| DeckLink output | Playback-capable card/port, Windows SDK, output format | None | Planned |
| NDI input | Standard SDK and compatible runtime, reachable network interface | None | Planned |
| NDI output | Standard SDK, video/audio format conversion and runtime | None | Planned |
| SRT / RTMP input | MediaMTX listeners, codecs compatible with selected outputs | None | Planned in the UI |

Synthetic capture is implemented in the Windows gateway manager. Hardware adapters will run as separate processes with their own discovery, error handling, signal-loss handling and retries. Status does not imply compatibility with every codec or capture card.

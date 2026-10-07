# Licensing and redistribution

The repository's MIT license covers **original NXTROUTE code**. It grants no additional rights to third-party components. Communication with separate programs through processes and protocols preserves component separation; it does not remove binary redistribution obligations.

| Component | Tested version | License / redistribution |
|---|---|---|
| MediaMTX | 1.21.1 | MIT; preserve LICENSE and dependency notices in public packages |
| .NET / ASP.NET Core | SDK 8.0.319; runtime selected by publish | MIT and runtime third-party notices; self-contained packaging still has licensing obligations |
| Microsoft.Extensions.Hosting.WindowsServices | 8.0.1 | MIT; dependencies pinned in packages.lock.json |
| FFmpeg BtbN | n8.1.3-14-g330caae0c1-20261006 | Build uses --enable-gpl --enable-version3: GPLv3+, not LGPL. No --enable-nonfree observed. Distribution requires licenses and corresponding sources, including libraries and build material |
| Playwright | 1.56.1 | Apache-2.0; development dependency, not installed with the product |
| Edge | Existing workstation browser | Not included in the repository or package |
| Inno Setup | Compiler 6.4.3 for local testing | Respect compiler licensing; compiler is not included in the product |
| DeckLink SDK / Desktop Video | Local DLLs 15.1; SDK not obtained | Proprietary; no Blackmagic SDK, driver or binary included. Review the specific package EULA before redistribution or automated installation |
| Standard NDI SDK/runtime | Not obtained | Proprietary license; a free SDK is not open source. Review terms, notices and redistributable runtime components before packaging |

The build script accepts local component directories and copies binaries and licenses for private testing. **The locally generated installer is not cleared for public binary release**: first prepare corresponding FFmpeg and dependency sources, .NET/MediaMTX notices and review all other component terms. A generic FFmpeg link is not sufficient GPL compliance. The public repository can contain original code and build metadata without third-party binaries.

The initial installer does not download or install NDI Tools or Blackmagic drivers. Future prerequisite handling will distinguish required runtimes, development SDKs and optional tools. Existing drivers must not be replaced automatically.

Primary sources: [MediaMTX MIT](https://github.com/bluenviron/mediamtx/blob/v1.21.1/LICENSE), [FFmpeg legal](https://ffmpeg.org/legal.html), [BtbN builds](https://github.com/BtbN/FFmpeg-Builds), [.NET runtime](https://github.com/dotnet/runtime), [NDI SDK licensing](https://docs.ndi.video/all/developing-with-ndi/sdk/licensing), [Blackmagic developer](https://www.blackmagicdesign.com/developer/products/capture-and-playback), [Inno Setup](https://jrsoftware.org/isinfo.php).

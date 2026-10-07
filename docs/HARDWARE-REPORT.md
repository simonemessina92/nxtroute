# Workstation inspection and tests — October 7, 2026

## Verified observations

| Item | Result | Method / limitation |
|---|---|---|
| Operating system | Windows, kernel 10.0.26200 | RuntimeInformation.OSDescription; edition not determined |
| CPU | AMD Ryzen 9 7900 12-Core Processor | CPU registry entry; runtime reports 24 logical processors |
| RAM / GPU | Not determined | CIM reads denied by sandbox |
| Desktop Video | Installation present; DeckLinkAPI64, Decklink64 and audio DLL product version 15.1 | Program Files inspection; does not establish the version of every loaded driver |
| Blackmagic cards | Models, count and connectors not determined | PnP/CIM inventory: Access denied |
| Docker | Not found in PATH or the checked standard directory | Does not rule out a remote VM |
| WSL | Command present, component not installed | wsl --list --quiet |
| FFmpeg / GStreamer | Not found in PATH | May exist elsewhere; personal documents were not searched |
| Node / Git | 24.13.0 / 2.53.0.windows.1 | Version commands |
| .NET SDK | 8.0.319 and 9.0.310 | dotnet --list-sdks |
| Browser | Edge 154.0.4258.62 | Launched with Playwright |
| Networking | NIC inventory unavailable | Get-NetAdapter: Access denied |
| Ports | Demo ports absent from sampled listeners; actual binding succeeded | netstat and subsequent startup; no LAN-access verification |
| Repository | Initial main branch contained only README, commit 9c0c3d5 | Remote clone; existing work preserved |

## Portable test tools

Tools were downloaded locally, not installed system-wide or included in Git. MediaMTX 1.21.1 Windows amd64 archive SHA256 matched upstream checksums.sha256:

`faa97974861eb75a68b5aa326c78e7e7a6f670b5ef191bace78e715130381f23`.

FFmpeg BtbN n8.1.3-14-g330caae0c1-20261006, GPL with --enable-version3. The n8.1 win64 GPL archive SHA256 matched the GitHub asset digest:

`ae302cb27f0f1eceab8441f7e9a0d8cab4daf5130aa4d024a76d0655a4aea97d`.

The FFmpeg asset uses a mutable latest URL. Keep the verified archive to reproduce this exact test; do not silently substitute a newer archive from the same URL.

FFmpeg lists x264, AAC, Opus, NVENC, QSV and AMF. Only software x264, AAC and Opus were exercised. Listing an encoder does not demonstrate available GPU hardware or successful hardware encoding. This build does not list DeckLink devices; it lists DirectShow. No SDI or NDI source has been captured.

## Initial video test

Portable MediaMTX and FFmpeg on Windows loopback, first with a Node backend and then with the .NET executable. Edge headless 154.0.4258.62, 1280x720 at 25 fps, H.264 baseline without B-frames, Opus for WHEP and AAC for HLS.

The first .NET test decoded 176 WebRTC frames and 128 HLS frames. Both advanced for approximately five seconds during sampling, with no dropped frames or JavaScript errors. WebRTC exposed a live audio track; this initial test did not verify listening or decoded audio samples. The updated integration test also analyzes decoded audio.

The HLS player was approximately 0.73 seconds behind its seekable-window edge. **This is not end-to-end latency**: it excludes the age of the latest segment and other pipeline delays. Glass-to-glass latency has not been demonstrated; a shared timing reference is required. Updated measurements are saved to `test-results/browser.json`, excluded from Git.

The initial portable-process sample showed approximately 86 MB working set for FFmpeg, 76 MB for MediaMTX and 55 MB for Node. Get-Process CPU is cumulative CPU time, not a percentage. These short synthetic tests do not demonstrate long-term stability, maximum throughput, speaker output, other browsers or LAN behavior.

## Observed blockers and alternatives

- CIM, card and NIC inspection: Access denied. Alternative: run inventory as the workstation user or enumerate devices through the future SDK adapter.
- No working Docker/WSL installation. Architecture explicitly changed to native Windows after user confirmation; virtualization was not installed.
- Git HTTPS using schannel: SEC_E_NO_CREDENTIALS. Clone succeeded with the per-command http.sslBackend=openssl override; no global setting changed.
- npm and NuGet attempted unwritable external caches. Caches were moved into the workspace. .NET HTTPS access to NuGet was unavailable in the sandbox; compilation used Node HTTPS retrieval through a temporary loopback proxy. TLS validation was not disabled and system proxy settings were not changed. The lockfile retains package hashes; the temporary proxy is not part of the product.
- Adding a channel during the first integration test did not trigger MediaMTX file reload on Windows; RTSP returned path is not configured. Fixed by configuring the synthetic-channel regex namespace, without depending on hot reload or interrupting active channels.
- Service installation, drivers, LAN access and hardware passthrough were not tested.

## Complete Windows gateway test

`npm test`, October 7, 2026, 28.3 seconds: passed. Two FFmpeg processes; intentional termination of the second; recovery with a new PID; the first channel remained ready with the same PID. Stop persisted enabled=false. After gateway restart, the demo restarted and the second channel remained stopped. External HTTP origins were rejected with 403. Gateway termination removed encoders through Job Objects.

| Browser measurement | WebRTC/WHEP | HLS |
|---|---|---|
| Codecs | H.264 + Opus confirmed through RTP stats | H.264 + AAC, MediaMTX API and encoder output |
| Video | 1280x720, 176 frames, 1 dropped | 1280x720, 129 frames, 0 dropped |
| Progress during five seconds | 5.006 seconds | 4.964 seconds |
| Decoded audio RMS | 0.06266 | 0.06247 |
| Time until playback criterion | 2399 ms | 220 ms |
| Mean RTP jitter buffer | Video 16 ms, audio 59 ms | Not applicable |
| Player distance from live edge | Not measured | 0.722 seconds |

The playback criterion includes currentTime>2; it is not the exact first-frame time. HLS segments were already prepared by the router. Startup and buffer measurements **are not end-to-end latency**. Audio was analyzed as decoded PCM after unmuting and connecting WebAudio; speakers were not tested. The first attempt with muted HLS playback and an audio graph without an output produced zero RMS. The measurement method was corrected without changing codecs or streams.

Inno Setup 6.4.3 compiled `dist/NXTROUTE-Setup-0.1.0.exe`. This private test package contains self-contained .NET, MediaMTX and FFmpeg. It has not been run with administrator privileges. Installation/removal, startup at boot and hardware access from NetworkService remain untested.

A separate resource sample used one synthetic channel, two renditions and no connected viewer, over 3.001 seconds after startup. Working sets: FFmpeg 80.6 MiB, MediaMTX 45.2 MiB, NXTROUTE 51.4 MiB. CPU normalized across 24 logical processors: 0.02%, 0.04% and 0.00% rounded, respectively. This short, low-load sample is not a benchmark or capacity estimate; repeat with viewers and real inputs.

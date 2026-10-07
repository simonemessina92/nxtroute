# Roadmap

Every milestone must leave a repeatable demo. Do not claim a feature works before testing it. Do not start the next hardware adapter until the current one meets its acceptance criteria.

## M1 — Synthetic Windows channel

- [x] Local MediaMTX 1.21.1 and FFmpeg, runnable without system installation.
- [x] WebRTC/WHEP and HLS video playback in Edge.
- [x] .NET backend published as a self-contained executable.
- [x] Original dashboard with real status, test-channel creation, start/stop and logs.
- [x] Integration test: decoded audio, isolation, recovery, persistence and cleanup.
- [x] Installer compiled.
- [ ] Installer tested as a service under an ordinary user after administrator installation.
- [ ] Public binary package: corresponding sources and all redistribution obligations satisfied.

Acceptance: at least one browser plays video and audio from both renditions; an encoder failure does not change the other channel's PID or readiness; gateway restart restores configuration; shutdown removes child processes; measurements and limitations are documented. Service/installer testing is a separate checkpoint and cannot be inferred from foreground operation.

## M2 — One SDI / DeckLink source

Identify cards and driver versions. Obtain the SDK without committing it. Choose a direct C++ adapter or GStreamer based on workstation tests. Do not redistribute a nonfree FFmpeg build without reviewing its terms.

Acceptance: one real source with documented format, verified SDI audio and audio/video synchronization, 30 minutes without crashes, signal loss/recovery and restart without interrupting a synthetic channel. SDI output is a later adapter and must not be declared working merely because capture works.

## M3 — One standard NDI source

Discovery, source selection and reception through the standard SDK, using an independent adapter. Specify runtime versions and licenses; the complete NDI Tools package is not mandatory.

Acceptance: discovery on the selected NIC, verified video/audio, 30-minute test, disconnection/reconnection, accurate source-loss status and no effect on SDI or other channels.

## M4 — Additional inputs and outputs

Start with RTMP or SRT, then add protocols already implemented by MediaMTX. Test one combination at a time and document codecs, failures, reconnection and a repeatable procedure. Add NDI output and SDI output separately.

## M5 — Complete installation experience

Prerequisite detection, guided official-package installation within vendor permissions, device/port diagnostics, authenticated LAN access with TLS, multiview and updates with rollback. Any signing-certificate cost must be distinguished from the software's price.

Acceptance: test on a clean Windows installation, no terminal required for operators, uninstall leaves no child processes and explicitly preserves configuration. Do not automatically update drivers on a working workstation.

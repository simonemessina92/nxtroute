# NXTROUTE

Free video gateway with public original source code for Windows workstations. MediaMTX handles media protocols; NXTROUTE manages channels, processes and the operator interface. The original project scope covers NDI, SRT, RTMP, RTSP, HLS and broadcast SDI; implemented and planned features are documented separately.

**Initial demo:** synthetic 720p25 source, Edge playback over WebRTC/WHEP and HLS. SDI and NDI are not implemented yet. The installer is experimental: Windows service installation still needs testing. Access is restricted to the local computer.

## Run the local build

The local executable is `dist/portable/NXTROUTE.exe`. MediaMTX and FFmpeg must be in its adjacent `components` directory. The build is self-contained: operators do not need Docker, Node.js or a separate .NET installation.

For a development test, run:

```powershell
.\dist\portable\NXTROUTE.exe --data-dir "$PWD\.runtime\manual-data"
```

Open http://127.0.0.1:3000. The demo channel starts automatically. Use the WebRTC and HLS buttons and unmute the player to hear the tone. Add and start another test channel; stopping one must not stop the other. Press Ctrl+C to stop the gateway. Configuration and logs remain in the selected data directory.

Double-clicking the foreground executable uses `%LocalAppData%\NXTROUTE` and opens the browser automatically. The installer creates a browser shortcut and registers the service. This initial installer is not a production release.

## Build

Developer requirements: Windows x64, .NET SDK 8.0.319, local MediaMTX 1.21.1 and compatible FFmpeg directories. Inno Setup is optional for building the installer. Proprietary SDKs are not required for the synthetic demo.

```powershell
.\scripts\build-windows.ps1 `
  -MediaMtxDirectory 'C:\tools\mediamtx-1.21.1' `
  -FfmpegDirectory 'C:\tools\ffmpeg' `
  -InnoCompiler 'C:\tools\Inno Setup\ISCC.exe'
```

Output: `dist/portable` and, when Inno Setup is supplied, `dist/NXTROUTE-Setup-0.1.0.exe`. Archives, binaries, SDKs, logs and videos are excluded from Git. Review [dependency licenses and redistribution limits](docs/DEPENDENCIES.md) **before publishing binary packages**.

## Automated verification

Node.js is only a developer test dependency. Edge must already be installed; the tests do not download another browser.

```powershell
npm ci --cache .runtime/npm-cache
npm test
```

The test requires the local build and available demo ports. It starts the gateway with separate test data and checks start/stop, two channels, encoder recovery, channel isolation, persistence, video/audio playback in Edge and child-process cleanup. Screenshots and measurements are saved to `test-results`. To use a different Chromium browser, set `BROWSER_PATH` to its executable. To test browser playback against an already running gateway, use `npm run verify:browser`.

## Windows service installation

Administrator privileges are required. The installer registers NXTROUTE with automatic startup, the NetworkService account and data in `%ProgramData%\NXTROUTE`. The shortcut opens the dashboard; closing the browser does not stop channels. Installation testing still requires user intervention and has not been performed.

Run these commands in an administrator PowerShell session to inspect, start and stop the service:

```powershell
sc.exe query NXTROUTE
sc.exe start NXTROUTE
sc.exe stop NXTROUTE
```

Uninstall through Settings > Apps > NXTROUTE. ProgramData configuration is preserved. Do not run the foreground application alongside the service: they use the same ports. This initial version does not install or update drivers and does not open firewall ports.

## Documentation

- [Architecture and decisions](docs/ARCHITECTURE.md)
- [Roadmap and acceptance criteria](docs/ROADMAP.md)
- [Hardware inspection, tests and limitations](docs/HARDWARE-REPORT.md)
- [Dependency licensing](docs/DEPENDENCIES.md)
- [Adapter status](adapters/README.md)
- [Changelog](CHANGELOG.md)

Original code: MIT. Proprietary and GPL dependencies retain their own terms. Codec, GPU and capture-card compatibility is not universal.

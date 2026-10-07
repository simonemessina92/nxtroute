# NXTROUTE

Gateway video gratuito con codice originale pubblico, per workstation Windows. MediaMTX gestisce i protocolli; NXTROUTE gestisce canali, processi e interfaccia. Il progetto parte dal gateway NDI/SRT/RTMP/RTSP/HLS/SDI descritto nel README iniziale, ma distingue funzioni provate e previste.

**Prima demo:** sorgente sintetica 720p25, browser Edge, WebRTC/WHEP e HLS. SDI e NDI non sono ancora implementati. L'installer è sperimentale: installazione come servizio da collaudare. Accesso soltanto dal computer locale.

## Prova della build locale

Una build è in `dist/portable/NXTROUTE.exe`. MediaMTX e FFmpeg devono essere in `components` accanto all'eseguibile. La build è self-contained: all'operatore non servono Node.js, Docker o .NET installato separatamente.

Avviare dal terminale, per il collaudo di sviluppo:

```powershell
.\dist\portable\NXTROUTE.exe --data-dir "$PWD\.runtime\manual-data"
```

Aprire http://127.0.0.1:3000. Il canale demo parte automaticamente. Usare i pulsanti WebRTC e HLS, abilitare l'audio nel player per ascoltare il tono. Aggiungere un canale test e avviarlo; fermarne uno non deve fermare l'altro. Arrestare il gateway con Ctrl+C. Configurazioni e log restano nella directory dati scelta.

Il doppio clic dell'eseguibile foreground usa `%LocalAppData%\NXTROUTE` e apre automaticamente il browser. L'installer crea direttamente il collegamento browser e il servizio. Per questa prima prova il setup non va considerato una release di produzione.

## Compilazione

Requisiti sviluppatore: Windows x64, .NET SDK 8.0.319, cartelle locali MediaMTX 1.21.1 e FFmpeg compatibile. Inno Setup opzionale per generare il setup. Non installare SDK proprietari per la demo sintetica.

```powershell
.\scripts\build-windows.ps1 `
  -MediaMtxDirectory 'C:\tools\mediamtx-1.21.1' `
  -FfmpegDirectory 'C:\tools\ffmpeg' `
  -InnoCompiler 'C:\tools\Inno Setup\ISCC.exe'
```

Output: `dist/portable` e, con Inno, `dist/NXTROUTE-Setup-0.1.0.exe`. Gli archivi, binari, SDK, log e video non vengono commessi. Consultare [licenze e limiti di redistribuzione](docs/DEPENDENCIES.md) **prima di pubblicare un pacchetto binario**.

## Verifica automatica

Node serve soltanto agli sviluppatori per i test. Edge deve essere già installato; non si scarica un browser aggiuntivo.

```powershell
npm ci --cache .runtime/npm-cache
npm test
```

Il test richiede la build locale e porte della demo libere. Avvia il gateway con dati separati, verifica start/stop, due canali, riavvio encoder, isolamento, persistenza, video/audio in Edge e cleanup; salva screenshot e misure in `test-results`. Per usare un browser Chromium diverso impostare `BROWSER_PATH` al suo eseguibile. Per collaudare soltanto il browser su un gateway già acceso: `npm run verify:browser`.

## Installazione del servizio — richiede amministratore

Il setup registra NXTROUTE con avvio automatico, account NetworkService e dati in `%ProgramData%\NXTROUTE`. Dopo l'installazione, il collegamento apre la dashboard; chiudere il browser non ferma i canali. Il primo collaudo del setup richiede conferma/intervento dell'utente ed è ancora da eseguire.

Comandi esatti di verifica, avvio e arresto in PowerShell amministratore:

```powershell
sc.exe query NXTROUTE
sc.exe start NXTROUTE
sc.exe stop NXTROUTE
```

Disinstallare da Impostazioni > App > NXTROUTE. Le configurazioni ProgramData vengono preservate. Non avviare la modalità foreground contemporaneamente al servizio: usano le stesse porte. Nessun driver viene installato/aggiornato e nessuna porta firewall viene aperta dalla prima versione.

## Documentazione

- [Architettura e decisioni](docs/ARCHITECTURE.md)
- [Roadmap e criteri di accettazione](docs/ROADMAP.md)
- [Hardware, prove e limiti](docs/HARDWARE-REPORT.md)
- [Licenze delle dipendenze](docs/DEPENDENCIES.md)
- [Stato degli adattatori](adapters/README.md)
- [Changelog](CHANGELOG.md)

Codice originale: MIT. Dipendenze proprietarie e GPL hanno termini propri. Non promettiamo compatibilità universale dei codec, GPU o schede di acquisizione.

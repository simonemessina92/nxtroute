# Workstation e prove — 7 ottobre 2026

## Dati verificati

| Voce | Risultato | Metodo / limite |
|---|---|---|
| Sistema | Windows, versione kernel 10.0.26200 | RuntimeInformation.OSDescription; edizione non accertata |
| CPU | AMD Ryzen 9 7900 12-Core Processor | Registro CPU; 24 processori logici dal runtime |
| RAM / GPU | Non accertate | Letture CIM negate dal sandbox |
| Desktop Video | Installazione presente; DLL DeckLinkAPI64, Decklink64 e audio versione prodotto 15.1 | File in Program Files; non prova versione effettiva di ogni driver caricato |
| Schede Blackmagic | Modello, quantità e connettori non accertati | Inventario PnP/CIM: Access denied |
| Docker | Non trovato in PATH o directory standard verificata | Non dedurre assenza di una VM remota |
| WSL | Comando presente, componente non installato | `wsl --list --quiet` |
| FFmpeg / GStreamer | Non trovati in PATH | Potrebbero esistere altrove; nessuna ricerca nei documenti personali |
| Node / Git | 24.13.0 / 2.53.0.windows.1 | Comandi versione |
| .NET SDK | 8.0.319 e 9.0.310 | `dotnet --list-sdks` |
| Browser | Edge 154.0.4258.62 | Browser lanciato con Playwright |
| Rete | Inventario NIC non disponibile | Get-NetAdapter: Access denied |
| Porte | Porte della demo non presenti nel campione listener; binding reale riuscito | netstat e successivo avvio; non prova accesso LAN |
| Repository | Main iniziale con solo README, commit 9c0c3d5 | Clone remoto; nessun lavoro preesistente sovrascritto |

## Tool portabili scaricati per il test

Non installati nel sistema né inclusi in Git. MediaMTX 1.21.1 Windows amd64: checksum verificato contro `checksums.sha256` upstream:

`faa97974861eb75a68b5aa326c78e7e7a6f670b5ef191bace78e715130381f23`.

FFmpeg BtbN n8.1.3-14-g330caae0c1-20261006, GPL con `--enable-version3`; archivio n8.1 win64 GPL SHA256 verificato rispetto al digest dell'asset GitHub:

`ae302cb27f0f1eceab8441f7e9a0d8cab4daf5130aa4d024a76d0655a4aea97d`.

Questo asset proviene da un URL `latest` mutabile: mantenere l'archivio verificato per ripetere esattamente il test. Non usare silenziosamente un nuovo archivio allo stesso URL.

FFmpeg elenca x264, AAC, Opus, NVENC, QSV e AMF. Solo x264 software, AAC e Opus sono stati esercitati. La presenza di un encoder nell'elenco non dimostra GPU disponibile né successo della codifica hardware. DeckLink non compare tra i dispositivi di questa build; DirectShow compare. Nessuna sorgente SDI o NDI ancora acquisita.

## Primo test video

MediaMTX + FFmpeg portabili, prima backend Node e poi eseguibile .NET, loopback Windows. Edge headless 154.0.4258.62, sorgente 1280x720/25, H.264 baseline senza B-frame, Opus per WHEP e AAC per HLS.

Sul primo test .NET: WebRTC 176 frame decodificati, HLS 128 frame; entrambi avanzano per circa 5 secondi durante il campionamento, nessun frame scartato e nessun errore JavaScript. WebRTC ha una traccia audio live; il primo test non verifica ascolto o campioni audio. Un test aggiornato analizza anche l'audio decodificato.

Distanza osservata del player HLS dal bordo della finestra seekable: circa 0,73 s. **Non è una misura di latenza end-to-end**: non include età dell'ultimo segmento né tutta la pipeline. Nessuna latenza glass-to-glass dimostrata; misurarla in seguito con un riferimento temporale comune. Le misure aggiornate sono salvate dal test in `test-results/browser.json` (ignorato da Git).

Primo campione dei processi portabili: FFmpeg circa 86 MB working set, MediaMTX circa 76 MB, backend Node circa 55 MB. CPU riportata da Get-Process è tempo cumulativo, non percentuale; il consumo .NET va campionato separatamente. Il test è breve e sintetico: non dimostra stabilità di lunga durata, throughput massimo, audio udibile, browser diversi o comportamento in LAN.

## Blocchi e alternative osservati

- CIM, schede e NIC: Access denied. Alternativa: eseguire l'inventario come utente workstation o usare il futuro adattatore SDK per enumerare dispositivi.
- Nessun Docker/WSL funzionante. Dopo conferma dell'utente, architettura cambiata a Windows nativo; nessuna installazione di virtualizzazione.
- Git HTTPS con schannel: SEC_E_NO_CREDENTIALS. Clone riuscito con `git -c http.sslBackend=openssl`; nessuna impostazione globale cambiata.
- NuGet e npm provano cache esterne non scrivibili. Cache spostate nella workspace. NuGet HTTPS dal processo .NET non disponibile nel sandbox; recupero via Node HTTPS e proxy esclusivamente loopback per compilare. Nessuna eccezione TLS disabilitata e nessun proxy del sistema cambiato. Lockfile mantiene hash dei pacchetti; il proxy temporaneo non fa parte del prodotto.
- Aggiunta canale durante il primo test: MediaMTX non ha applicato il cambio file su Windows, RTSP ha risposto `path is not configured`. Risolto usando il namespace regex dei canali test nel router, senza dipendere da hot reload e senza interrompere quelli attivi.
- Installazione servizio, driver, accesso LAN e passthrough: non effettuati. Non dichiararli verificati.

## Collaudo completo del gateway Windows

`npm test`, 7 ottobre 2026, durata 28,3 s: passato. Due processi FFmpeg, terminazione intenzionale del secondo, recupero con PID diverso, primo canale pronto e PID invariato; stop salva enabled=false; dopo riavvio del gateway il demo riparte e il secondo resta fermo. Origine HTTP esterna rifiutata con 403. Terminazione del gateway rimuove gli encoder attraverso Job Objects.

| Misura browser | WebRTC/WHEP | HLS |
|---|---|---|
| Codec | H.264 + Opus confermati da RTP stats | H.264 + AAC, API MediaMTX e flusso encoder |
| Video | 1280x720, 176 frame, 1 dropped | 1280x720, 129 frame, 0 dropped |
| Avanzamento in 5 s | 5,006 s | 4,964 s |
| Audio RMS decodificato | 0,06266 | 0,06247 |
| Attesa del criterio di playback | 2399 ms | 220 ms |
| Buffer RTP medio | video 16 ms, audio 59 ms | Non applicabile |
| Distanza dal live edge del player | Non misurata | 0,722 s |

Il criterio di playback comprende currentTime>2: non è il tempo esatto del primo frame. HLS ha segmenti già preparati dal router. Le misure di buffer e startup **non sono latenza end-to-end**. L'audio è stato analizzato come campioni PCM dopo unmute e connessione WebAudio; non è un test d'ascolto degli altoparlanti. Il primo tentativo con player HLS mutato e grafo audio senza uscita produceva RMS zero: corretto il metodo di misura, senza cambiare codec o flusso.

Inno Setup 6.4.3 ha compilato `dist/NXTROUTE-Setup-0.1.0.exe`. Il pacchetto contiene .NET self-contained, MediaMTX e FFmpeg per prova locale; non è stato eseguito con privilegi amministrativi. Installazione/disinstallazione, avvio al boot e accesso hardware dall'account NetworkService restano da collaudare.

Campione separato del gateway .NET: un canale sintetico, due rendition, nessun player collegato, intervallo di 3,001 s dopo avvio. Working set: FFmpeg 80,6 MiB, MediaMTX 45,2 MiB, NXTROUTE 51,4 MiB. CPU normalizzata sui 24 processori logici: rispettivamente 0,02%, 0,04%, 0,00% arrotondati. È un campione breve a basso carico, non un benchmark né una stima di capacità; va ripetuto con spettatori e ingressi reali.

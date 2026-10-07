# Roadmap

Ogni traguardo deve produrre una demo ripetibile. Nessuna dichiarazione di funzionamento prima del test. Non iniziare l'adattatore successivo finché il precedente non supera i criteri.

## M1 — Canale sintetico Windows

- [x] MediaMTX 1.21.1 e FFmpeg locali, avviabili senza installazione di sistema.
- [x] Visione WebRTC/WHEP e HLS in Edge: primo test video passato.
- [x] Backend .NET compilato come eseguibile self-contained.
- [x] Dashboard originale con stato reale, creazione canali test, avvio, arresto e log.
- [x] Test completo aggiornato: audio decodificato, isolamento, recovery, persistenza e cleanup.
- [x] Installer compilato.
- [ ] Installer collaudato come servizio con utente ordinario dopo installazione amministrativa.
- [ ] Pacchetto pubblico: sorgenti corrispondenti e obblighi di tutti i binari soddisfatti.

Accettazione: almeno un browser riproduce video e audio per entrambe le rendition; errore di un encoder non cambia PID o stato del secondo; riavvio del gateway ripristina la configurazione; arresto elimina i figli; limiti e misure sono riportati nel report. Il collaudo service/installer è un sotto-traguardo distinto e non può essere dedotto dalla modalità foreground.

## M2 — Una sorgente SDI / DeckLink

Inventario preciso delle schede e delle versioni driver. Ottenere lo SDK senza commetterlo nel repository. Scegliere adattatore C++ diretto oppure GStreamer dopo prove sulla workstation; non distribuire una build FFmpeg nonfree senza verifica.

Accettazione: una sorgente reale, formato documentato, audio SDI verificato, sincronizzazione audio/video, 30 minuti senza crash, perdita/ripristino segnale e ripartenza senza fermare un canale sintetico. Uscita SDI è un adattatore successivo: non viene dichiarata funzionante insieme alla cattura.

## M3 — Una sorgente NDI standard

Discovery, selezione sorgente, ricezione tramite SDK standard, adattatore indipendente. Definire esattamente runtime, versioni e licenze necessarie: NDI Tools interi non obbligatori.

Accettazione: discovery sulla NIC scelta, video/audio verificati, 30 minuti, disconnessione/riconnessione, sorgente scomparsa mostrata correttamente, nessun effetto su SDI o altri canali.

## M4 — Ingressi e uscite progressivi

Prima RTMP o SRT, poi ulteriori protocolli già implementati da MediaMTX. Una combinazione alla volta con codec, errore, riconnessione e procedura ripetibile documentati. Aggiungere NDI out e SDI out separatamente.

## M5 — Esperienza di installazione completa

Rilevamento prerequisiti, installazione guidata dai pacchetti ufficiali solo nei limiti autorizzati dai vendor, diagnostica dispositivi/porte, configurazione LAN con autenticazione e TLS, multiview, aggiornamenti con rollback. Firma del pacchetto: eventuale costo del certificato non deve essere confuso con il prezzo del software.

Accettazione: test su Windows pulito, nessun terminale necessario all'operatore, disinstallazione senza processi residui e preservazione esplicita delle configurazioni. Niente aggiornamenti driver automatici su una workstation funzionante.

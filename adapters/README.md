# Adattatori

| Adattatore | Requisiti e codec | Test | Stato |
|---|---|---|---|
| Synthetic | FFmpeg con lavfi, x264, Opus e AAC; 720p25, due rendition | Browser Edge, vedere HARDWARE-REPORT | Sperimentale: collaudo breve |
| DeckLink input | Desktop Video e SDK Windows; formato SDI da rilevare | Nessuno | Previsto |
| DeckLink output | Scheda/porta con playback, SDK Windows, formato di uscita | Nessuno | Previsto |
| NDI input | SDK standard e runtime compatibile, NIC raggiungibile | Nessuno | Previsto |
| NDI output | SDK standard, conversione formato video/audio e runtime | Nessuno | Previsto |
| SRT / RTMP input | Listener MediaMTX, codec compatibili con uscite selezionate | Nessuno | Previsto nella UI |

Synthetic è implementato nel gestore Windows. Gli adattatori hardware saranno processi separati, con discovery, errori, perdita segnale e retry propri. La colonna stato non implica compatibilità con qualsiasi codec o scheda.

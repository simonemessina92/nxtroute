# Licenze e distribuzione

La licenza MIT del repository copre **il codice originale NXTROUTE**, non concede diritti aggiuntivi sui componenti di terzi. La comunicazione con programmi separati via processi e protocolli mantiene la separazione dei componenti; non modifica gli obblighi dei binari distribuiti.

| Componente | Versione usata | Licenza / distribuzione |
|---|---|---|
| MediaMTX | 1.21.1 | MIT; conservare LICENSE e notice delle dipendenze nel pacchetto pubblico |
| .NET / ASP.NET Core | SDK 8.0.319, runtime selezionato dal publish | MIT e notice terze parti dei runtime; self-contained non significa senza licenze |
| Microsoft.Extensions.Hosting.WindowsServices | 8.0.1 | MIT; dipendenze fissate nel packages.lock.json |
| FFmpeg BtbN | n8.1.3-14-g330caae0c1-20261006 | Build `--enable-gpl --enable-version3`: GPLv3+, non LGPL. Nessun `--enable-nonfree` osservato. Per distribuire occorrono licenze e sorgenti corrispondenti, incluse librerie e materiale di build |
| Playwright | 1.56.1 | Apache-2.0, dipendenza di sviluppo; non nel prodotto installato |
| Edge | Browser già installato | Non incluso nel repository o nel pacchetto |
| Inno Setup | Compiler 6.4.3 per prova locale | Licenza del compiler da rispettare; compiler non incluso nel prodotto |
| DeckLink SDK / Desktop Video | DLL locali 15.1; SDK non ancora acquisito | Proprietari; nessun SDK, driver o binario Blackmagic incluso. Verificare EULA del pacchetto specifico prima di redistribuire o automatizzare installazione |
| NDI standard SDK/runtime | Non ancora acquisito | Licenza proprietaria; gratuità dello SDK non equivale a open source. Verificare termini, notice e runtime distribuibili prima del packaging |

Il build script accetta directory locali dei componenti, copia i binari e le licenze per una prova privata. **Il setup prodotto localmente non è ancora autorizzato come release pubblica**: prima della pubblicazione dei binari occorre preparare il pacchetto dei sorgenti corrispondenti FFmpeg e dipendenze, i notice .NET/MediaMTX e verificare le condizioni delle altre dipendenze. Non basta mettere un link generico a FFmpeg per adempiere alla GPL. Il repository pubblico può contenere il codice originale e i metadati di build, senza binari terzi.

L'installer iniziale non scarica né installa NDI Tools o driver Blackmagic. Il futuro flusso prerequisiti distinguerà runtime necessario, SDK per sviluppo e strumenti opzionali. Non sostituire automaticamente driver già installati.

Fonti primarie: [MediaMTX MIT](https://github.com/bluenviron/mediamtx/blob/v1.21.1/LICENSE), [FFmpeg legal](https://ffmpeg.org/legal.html), [build BtbN](https://github.com/BtbN/FFmpeg-Builds), [runtime .NET](https://github.com/dotnet/runtime), [NDI SDK licensing](https://docs.ndi.video/all/developing-with-ndi/sdk/licensing), [Blackmagic developer](https://www.blackmagicdesign.com/developer/products/capture-and-playback), [Inno Setup](https://jrsoftware.org/isinfo.php).

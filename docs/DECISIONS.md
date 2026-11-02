# Decisioni tecniche

Ogni voce riporta la decisione, la motivazione e le condizioni per rivederla. Le voci marcate **[da verificare]** dipendono dalle misure della 0.1.0.

## D-001 — .NET 10 LTS
.NET 8 esce dal supporto il 10 novembre 2026, mentre .NET 10 è LTS con supporto fino a novembre 2028. WPF è disponibile in entrambe le versioni.

## D-002 — libmpv: build shinchiro `mpv-dev-x86_64-20261101-git-48fca90e8c`
- Commit mpv `48fca90e8ce5011d0a4c689178ad739329ea253d` (1 novembre 2026), client API 2.6.
- SHA-256 dell'archivio `30d3f9fa121ce24638610a9ae52f8957743f9dea416dd37dc4ff7e43589ef843`, calcolato su un download diretto il 2 novembre 2026. Contenuto verificato: `libmpv-2.dll` x64 (121 MB) e gli header; tutte le funzioni e le strutture usate da viBro corrispondono a `client.h`.
- Si usa la variante `x86_64` e non `x86_64-v3`: la v3 richiede AVX2 e la specifica ammette hardware meno potente.
- Non esiste una build ufficiale di libmpv per Windows; quella di shinchiro è la più diffusa.
- **Conservazione:** shinchiro tiene su GitHub solo le ultime ~30 build giornaliere. Per questo `dependencies.json` prevede anche il link SourceForge, e `build.ps1` riusa una copia locale in `deps\download\` se lo SHA-256 corrisponde. Prima della 1.0 conviene allegare l'archivio a una release del repository viBro, anche per gli obblighi della GPL.
- **Rischio — `vulkan-1.dll`:** la DLL importa `vulkan-1.dll` in modo statico, non a richiesta. Il file è installato dai driver GPU Intel, AMD e NVIDIA, ma può mancare su VM o Sandbox senza driver: in quel caso libmpv non si carica. Da verificare nella prova di portabilità. Se il problema si presenta, la soluzione è incorporare il loader Vulkan ufficiale Khronos (licenza Apache-2.0) accanto a libmpv.
- Per aggiornare: modificare `dependencies.json` (versione, nome dell'archivio, URL, SHA-256) e annotare la modifica nel CHANGELOG.

## D-003 — Licenza GPL-3.0-or-later
Le build disponibili di libmpv contengono componenti GPL, quindi un EXE che le incorpora va distribuito con una licenza compatibile. La scelta dell'utente è GPL-3.0; si adotta la forma "or-later" raccomandata dalla FSF. Se serve "only", basta modificare README, csproj e questa voce.

## D-004 — Video in una finestra nativa figlia (`HwndHost` + opzione `wid`)
Alternativa scartata: l'API di rendering di libmpv (`mpv_render_context`) con interoperabilità OpenGL o D3D verso WPF. È più complessa, introduce una copia o una sincronizzazione aggiuntiva per ogni fotogramma e WPF usa D3D9.
Con `wid`, mpv gestisce in autonomia D3D11, la decodifica hardware e il ridimensionamento, ed è il percorso più rapido e collaudato.
Limite: per il vincolo di "airspace", nessun controllo WPF può essere disegnato sopra il video. È accettabile perché la specifica non prevede sovrimpressioni.

## D-005 — Tastiera con la finestra video in primo piano
I tasti indirizzati alla finestra nativa non generano eventi WPF. Un hook su `ComponentDispatcher.ThreadPreprocessMessage` intercetta `WM_KEYDOWN`/`WM_SYSKEYDOWN` diretti alla finestra video e alle sue figlie, poi li passa allo stesso gestore usato da `PreviewKeyDown`. mpv ha i propri binding disattivati (`input-default-bindings=no`, `input-vo-keyboard=no`, `input-cursor=no`).

## D-006 — Un solo motore e caricamento in pausa
Un'unica istanza mpv resta attiva per tutta la sessione (`idle=yes`, `force-window=yes`). Ogni caricamento esegue `set pause yes` seguito da `loadfile <file> replace`; un nuovo caricamento interrompe quello in corso. Con `keep-open=yes` la riproduzione si ferma sull'ultimo fotogramma e non passa ad altri file.
Tutti i comandi sono asincroni (`mpv_command_async`), quindi il thread dell'interfaccia non attende mai mpv.

## D-007 — Salti: assoluti, accorpati, su fotogrammi chiave per impostazione predefinita **[da verificare]**
- L'applicazione calcola il bersaglio (posizione + delta, limitato all'intervallo [0, durata]) e invia un `seek` **assoluto**.
- Resta in corso al massimo un salto alla volta. Le richieste che arrivano nel frattempo sostituiscono quella in attesa, e i delta successivi si sommano al bersaglio in attesa. In questo modo la ripetizione automatica dei tasti non crea arretrati.
- Modalità predefinita `keyframes`: la più veloce, ma su video con GOP lunghi un salto di 1 s può atterrare più lontano. La casella *Exact seeks* (o `--exact-seeks`) usa `exact`. Le misure stabiliranno la modalità predefinita per ciascun tipo di salto, e potrebbe risultare `exact` per ±1 s.
- Timeout di sicurezza di 3 s: se mpv non segnala la ripresa, la coda si sblocca (`seek_timeout` nel CSV).

## D-008 — Definizione delle misure
- **Primo fotogramma / cambio video**: tempo tra la richiesta e l'evento `MPV_EVENT_PLAYBACK_RESTART` del file più recente. Il file viene riconosciuto tramite il `playlist_entry_id`, restituito dalla risposta a `loadfile` e riportato in `START_FILE`/`END_FILE`. Il valore esclude il tempo di composizione dello schermo (alcuni millisecondi).
  - Un primo approccio contava gli eventi `START_FILE`, ma il test con libmpv ha mostrato che i file scavalcati da un cambio rapido (D tenuto premuto) non emettono `START_FILE`: il conteggio si bloccava. Per questo è stato sostituito.
- **Salto**: tempo tra l'invio del `seek` e il successivo `PLAYBACK_RESTART`.
- **Memoria**: working set, memoria privata e heap gestito al momento di ogni misura.
- Nel CSV si registra solo il nome del file, senza percorso.

## D-009 — Cache contenuta **[da verificare]**
`cache=no`: mpv mantiene solo il read-ahead minimo del demuxer, senza letture massicce dal NAS. Il valore definitivo, e un eventuale limite in `viBro.ini` a partire dalla 0.2.0, dipende dalle prove con `--mpv-opt cache=yes`, `demuxer-max-bytes` e `demuxer-readahead-secs`.

## D-010 — EXE singolo
`PublishSingleFile` + `SelfContained` + `IncludeNativeLibrariesForSelfExtract`. Le DLL native (libmpv, WPF) vengono estratte al primo avvio in `%TEMP%\.net\viBro\<hash del bundle>`: una cache versionata e riutilizzata, come consentito dalla specifica. Se l'utente svuota `%TEMP%`, le DLL vengono semplicemente estratte di nuovo.
- ReadyToRun attivo per ridurre il tempo di avvio.
- Compressione disattivata per impostazione predefinita: rende l'EXE più piccolo ma rallenta ogni avvio. `build.ps1 -Compress` permette il confronto. **[da verificare]**
- `build.ps1` fallisce se `libmpv-2.dll` compare accanto all'EXE, cioè se non è stata incorporata.

## D-011 — Isolamento da un mpv già installato
`config=no`, `load-scripts=no`, `ytdl=no`, `terminal=no`, `osc=no`: non vengono letti `mpv.conf`, script o configurazioni dell'utente. Sottotitoli disattivati (`sid=no`, `sub-auto=no`), copertine audio non mostrate (`audio-display=no`).

## D-012 — Decodifica hardware `auto-safe`
Sceglie i decodificatori ritenuti sicuri (su Windows tipicamente D3D11VA) e non dipende da NVIDIA; se l'hardware non è disponibile passa al software. Il selettore nella barra superiore permette il confronto con `no` (software).

## D-013 — Ordinamento naturale con `StrCmpLogicalW`
È la funzione usata da Esplora risorse, quindi l'ordine coincide con quello che l'utente vede in Windows.

## D-014 — Lettura delle cartelle
La lettura usa un thread di lavoro (`DirectoryInfo.EnumerateFiles`, nessuna ricorsione), con dimensione e data prese dall'enumerazione stessa: nessun file viene aperto e non si leggono metadati video. Un risultato superato da una richiesta più recente viene ignorato; dopo 3 s l'interfaccia segnala l'attesa. I file nascosti e di sistema sono esclusi.

## D-015 — Lingua
Interfaccia in inglese, come da specifica. Documentazione in italiano, come la specifica; si può tradurre il README prima della pubblicazione.

## D-016 — Riproduzione alla fine del video
Se il video è fermo alla fine, Play riparte dall'inizio. La specifica non lo prevede esplicitamente; è il comportamento comune dei player.

## D-017 — Git
I commit sono locali. L'autore è un segnaposto (`viBro developer <vibro@users.noreply.github.com>`), da sostituire prima della pubblicazione. Il tag `v0.1.0` **non** viene creato finché la build e le prove non sono verificate.

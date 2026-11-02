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

## D-007 — Salti: assoluti, accorpati; brevi esatti, lunghi su fotogrammi chiave (misurato 2026-11-02)
- L'applicazione calcola il bersaglio (posizione + delta, limitato all'intervallo [0, durata]) e invia un `seek` **assoluto**.
- Resta in corso al massimo un salto alla volta. Le richieste che arrivano nel frattempo sostituiscono quella in attesa, e i delta si sommano al bersaglio in attesa: la ripetizione automatica dei tasti non crea arretrati. Misurato: fino a 11 richieste accorpate in un solo salto, nessun timeout.
- **±1 s e ±5 s: sempre `exact`.** Con `keyframes` l'immagine non si spostava: sui file reali i fotogrammi chiave distano da 5 a 24 s, quindi 14 pressioni di → lasciavano il video fermo e poi lo riportavano indietro. La latenza misurata di 2 ms era fittizia. In modalità `exact`, sul NAS, ±1 s ha mediana 22 ms (90%: 38 ms) e ±5 s ha mediana 35–66 ms (90%: 84–115 ms); su disco locale 18–19 ms.
- **±1 min, ±5 min e barra temporale: `keyframes`**, come chiede la specifica (mediana 16 ms, 90%: 140 ms per ±1 min sul NAS; possibile scarto fino all'intervallo tra fotogrammi chiave). La casella *Exact long seeks* (o `--exact-seeks`) li rende esatti: ±1 min esatto costa 67–242 ms di mediana sul NAS.
- La soglia (30 s) è una costante in `MainWindow`; dalla 0.2.0 potrà passare in `viBro.ini`.
- Il CSV registra `landed_s`, la posizione effettivamente raggiunta, per individuare salti che non spostano l'immagine.
- Timeout di sicurezza di 3 s: se mpv non segnala la ripresa, la coda si sblocca (`seek_timeout` nel CSV).

## D-008 — Definizione delle misure
- **Primo fotogramma / cambio video**: tempo tra la richiesta e l'evento `MPV_EVENT_PLAYBACK_RESTART` del file più recente. Il file viene riconosciuto tramite il `playlist_entry_id`, restituito dalla risposta a `loadfile` e riportato in `START_FILE`/`END_FILE`. Il valore esclude il tempo di composizione dello schermo (alcuni millisecondi).
  - Un primo approccio contava gli eventi `START_FILE`, ma il test con libmpv ha mostrato che i file scavalcati da un cambio rapido (D tenuto premuto) non emettono `START_FILE`: il conteggio si bloccava. Per questo è stato sostituito.
- **Salto**: tempo tra l'invio del `seek` e il successivo `PLAYBACK_RESTART`.
- **Memoria**: working set, memoria privata e heap gestito al momento di ogni misura.
- Nel CSV si registra solo il nome del file, senza percorso.

## D-009 — Cache contenuta (confermata per la 0.1.0)
`cache=no`: mpv mantiene solo il read-ahead minimo del demuxer, senza letture massicce dal NAS. Il log della 0.1.0 mostra che il tempo di caricamento sul NAS è dovuto all'apertura SMB e alla lettura dell'indice MP4, che una cache più grande non riduce; per ora resta `no`. Il valore definitivo, e un eventuale limite in `viBro.ini` a partire dalla 0.2.0, dipende dalle prove con `--mpv-opt cache=yes`, `demuxer-max-bytes` e `demuxer-readahead-secs`.

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

## D-018 — Albero delle cartelle (0.2.0)
- Radici: Desktop, Documents, Videos, Downloads (cartelle note di Windows, quindi anche se reindirizzate), tutte le unità restituite da `GetLogicalDrives` e, per la sessione, la radice `\\server\share` di ogni percorso UNC aperto. Nessun nodo "Rete": l'esplorazione della rete è lenta o vuota su molte reti. Scelta dell'utente.
- Costruire le radici non accede ai dischi. Le etichette dei volumi si leggono in background (un'unità di rete scollegata può impiegare decine di secondi).
- Le sottocartelle vengono elencate solo alla prima espansione di un nodo, su un thread di lavoro; un errore mostra "(unavailable: …)" e si ritenta alla successiva espansione. Le cartelle nascoste e di sistema sono escluse, come nell'elenco dei file.
- La sincronizzazione con la cartella aperta procede livello per livello in background e si ferma all'antenato più profondo visibile (per esempio sotto una cartella nascosta). Una nuova richiesta annulla quella precedente. La selezione fatta dal codice non riapre la cartella.
- TreeView virtualizzato.

## D-019 — viBro.ini (0.2.0)
- Formato INI con sezioni, UTF-8 senza BOM, numeri in cultura invariante. `[viBro] SchemaVersion=1`.
- Salvataggio alla chiusura: si scrive `viBro.ini.tmp`, si esegue il flush su disco, poi `File.Replace` sostituisce il file. Un'interruzione non lascia un INI troncato.
- Lettura tollerante: ogni valore mancante prende il predefinito; ogni valore non valido o fuori intervallo prende il predefinito e genera un avviso nella barra di stato. Le chiavi sconosciute (file di una versione successiva o modifiche manuali) vengono conservate. I commenti non vengono conservati.
- La scrivibilità si verifica all'avvio con un file di prova (`viBro.ini.probe`, cancellato alla chiusura dell'handle). Se non è scrivibile: avviso permanente e nessun salvataggio, né lì né altrove (specifica).
- `HardwareDecoding` accetta solo valori nella forma dei nomi `hwdec` di mpv (`[a-z0-9-]` separati da virgole), per non passare a mpv testo arbitrario.
- La posizione della finestra si ripristina solo se almeno 150×80 px cadono sul desktop virtuale; altrimenti la finestra viene centrata (per esempio dopo aver scollegato un monitor).
- `--exact-seeks` da riga di comando vale per la sessione e non viene salvato.

## D-020 — Frecce sempre dedicate ai salti
La specifica sospende le scorciatoie solo nei campi di testo e nelle finestre modali. Per questo le frecce e PgUp/PgDn eseguono i salti anche con lo stato attivo sull'albero o sull'elenco, e la navigazione nell'albero avviene con il mouse. Se in futuro si vuole la navigazione da tastiera nell'albero, serve una modifica della specifica.

## D-021 — Ordinamento dell'elenco
Clic sull'intestazione: ordine crescente; nuovo clic sulla stessa colonna: inverte. A parità di dimensione o data si ordina per nome (naturale). La selezione corrente resta selezionata e il video non viene ricaricato. `NaturalComparer` usa `StrCmpLogicalW` su Windows, con un'implementazione interna di ripiego usata nei test.

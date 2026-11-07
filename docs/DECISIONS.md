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

## D-022 — Operazioni sui file: scelte d'interfaccia (0.3.0, scelte dell'utente)
- W apre la finestra standard di Windows per la scelta della cartella, inizializzata sull'ultima destinazione (o sulla cartella corrente).
- Conferma di eliminazione con `MessageBox` Sì/No e **Sì predefinito**: Canc e poi Invio eliminano. Il testo indica nome e tipo (Cestino o definitiva); per la rete l'icona è di avviso e il testo dice che il file non sarà recuperabile.
- Rinomina in una piccola finestra (non nell'elenco virtualizzato).
- Conflitto di nome nello spostamento: *Mantieni entrambi* (predefinito), *Sovrascrivi* (solo come scelta esplicita), *Annulla*.

## D-023 — Cestino e "nessuna eliminazione definitiva senza nuova conferma"
I file locali vanno al Cestino con `SHFileOperation(FO_DELETE)` e i flag `FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT | FOF_WANTNUKEWARNING`. Con `FOF_WANTNUKEWARNING`, quando Windows non può cestinare un file (troppo grande, unità senza Cestino) chiede lui stesso prima di eliminarlo definitivamente: è la "nuova conferma esplicita" della specifica. Un rifiuto lascia il file. L'esito si verifica sempre controllando che il file non esista più. Limite: `SHFileOperation` non accetta percorsi oltre 260 caratteri, e in quel caso l'operazione fallisce e viene segnalata. L'alternativa `IFileOperation` (COM) è più complessa ed è rimandata.
I file UNC e delle unità mappate (`DriveType.Network`) si eliminano con `File.Delete`, perché in rete il Cestino non esiste; la conferma lo dichiara.

## D-024 — Rilascio del file prima delle operazioni
`MpvPlayer.ReleaseFileAsync()` invia `stop` e termina con `END_FILE` o `idle-active=true` (timeout 3 s). Poi `FileOps.WaitUntilReleasedAsync` prova ad aprire il file in modo esclusivo fino a 5 s, perché l'handle di Windows può chiudersi un attimo dopo l'evento. Se il file resta occupato da un altro programma, l'operazione prosegue e l'errore reale viene mostrato. In caso di fallimento il file viene ricaricato in pausa nella posizione precedente.

## D-025 — Spostamento sicuro
- Stesso volume (stessa radice; la stessa condivisione UNC conta come un volume): `File.Move`, cioè una rinomina atomica.
- Volumi diversi: copia in `<destinazione>.vibro-partial` a blocchi di 4 MB con avanzamento e annullamento, pre-allocazione (fallisce subito se il disco è pieno), flush su disco, controllo della dimensione, copia delle date, rinomina nel nome finale (senza sovrascrivere, salvo scelta esplicita) e **solo alla fine** eliminazione della sorgente. Annullamento o errore: si elimina il temporaneo e la sorgente resta intatta. Se la sorgente non si può eliminare dopo una copia riuscita, viene segnalato che il file è in entrambe le posizioni.
- Un'unità mappata e il percorso UNC della stessa condivisione hanno radici diverse, quindi passano per la copia: più lento, ma corretto.

## D-026 — Verifica della compilazione WPF fuori da Windows
Dalla 0.3.0 il progetto completo, XAML compreso, viene compilato anche su Linux (.NET SDK 10.0.114 con `EnableWindowsTargeting`), usando gli assembly di riferimento `Microsoft.WindowsDesktop.App.Ref` 10.0.14 presi dall'SDK installato da `build.cmd`. In questo modo gli errori di compilazione si scoprono prima della prova su Windows; il comportamento va comunque verificato su Windows.

## D-027 — Loader Vulkan incorporato (0.4.0)
`libmpv-2.dll` importa `vulkan-1.dll` in modo statico: senza quella DLL, libmpv non si carica. Il loader ufficiale Khronos 1.4.357.0 (Apache-2.0, binario conda-forge con provenienza e SHA-256 in `third_party/vulkan-loader/SOURCE.md`, controllato da `build.ps1`) è incorporato nell'EXE. All'avvio `NativePreload` carica prima `System32\vulkan-1.dll`, se esiste; altrimenti carica la copia incorporata. Poiché Windows collega l'import di libmpv al modulo `vulkan-1.dll` già caricato, il loader di sistema (aggiornato dai driver) mantiene la precedenza. Senza driver Vulkan, mpv usa Direct3D 11. La DLL dipende solo da ADVAPI32, CFGMGR32 e KERNEL32.

## D-028 — Temi con Fluent di WPF (0.4.0)
I temi usano `Application.ThemeMode` (tema Fluent integrato in WPF, segnalato come sperimentale: diagnostica WPF0001 soppressa solo in `ThemeManager`). Ristila in modo coerente tutti i controlli standard, le finestre di dialogo di viBro e la barra del titolo, senza mantenere modelli di controllo propri. Le `MessageBox` e la finestra di scelta cartella sono di Windows e seguono il tema di sistema. `Theme=System` (predefinito) segue Windows; il pulsante salva una scelta esplicita `Light` o `Dark`. Rischio: se una versione futura di .NET cambiasse l'API sperimentale, la compilazione lo segnalerebbe.
- **Correzione dopo la prima prova (testo nero nel tema Dark).** Due cause: (1) gli stili impliciti con chiave `Window` non si applicano alle classi derivate (`MainWindow`, dialoghi), quindi il testo semplice restava nero; (2) gli `ItemContainerStyle` di `ListViewItem` e `TreeViewItem` erano definiti senza `BasedOn` e *sostituivano* lo stile Fluent. Soluzione: `ThemeManager` pubblica tre pennelli di risorsa (`ViBro.Foreground`, `ViBro.Background`, `ViBro.Panel`) aggiornati a ogni cambio di tema e collegati con `SetResourceReference` a finestre e barra di stato; gli stili dei contenitori sono costruiti nel codice con `BasedOn` lo stile del tema corrente e ricostruiti a ogni cambio.
- **Seconda correzione (righe dell'elenco come `ViBro.Util.VideoItem`).** Per un `ListView` con `GridView` la base giusta non è lo stile implicito di `ListViewItem` (modello con `ContentPresenter`, che mostra `ToString()`), ma quello con chiave `GridView.GridViewItemContainerStyleKey` (modello con `GridViewRowPresenter`, che disegna le colonne). Il tema Fluent di .NET 10 lo definisce (verificato in `PresentationFramework.Fluent.dll` 10.0.14); se mancasse si ricade sullo stile di `ListViewItem`.

## D-029 — Barra dei controlli in alto e fascia a schermo intero (0.4.0, richiesta dell'utente)
- Finestra normale: tutta la barra dei controlli sta sopra il video.
- Schermo intero: la barra diventa una fascia nera *sopra* il video (scelta dell'utente: nulla viene coperto). Contiene solo la barra temporale e il tempo, centrati e larghi il 40% della finestra (minimo 320 px), interattivi. Appare al movimento del mouse (anche sopra il video: `WM_MOUSEMOVE` intercettato come i tasti, con filtro sui movimenti fittizi) e a ogni salto; si nasconde dopo 2 s, ma non mentre il mouse è sulla fascia o si trascina. La fascia resta (`Visibility.Hidden`) per non ridimensionare il video a ogni comparsa.
- Una barra sovrapposta all'immagine richiederebbe una finestra separata sopra quella nativa di mpv (vincolo di "airspace"); la fascia evita il problema.

## D-030 — Registro degli errori (0.4.0)
`viBro-errors.log` accanto all'EXE, UTF-8, al massimo 1 MB più un file `.1.log`. Registra eccezioni non gestite (thread UI, AppDomain, task non osservati), messaggi mpv di livello `error` (`mpv_request_log_messages`), file non apribili, riproduzione interrotta, avvisi dell'INI, avvio del motore fallito. Se la cartella non è scrivibile il log si disattiva senza errori, e la barra di stato continua a mostrare i problemi. Nota: annullando un caricamento con cambi molto rapidi mpv può registrare errori di apertura del file abbandonato; sono innocui.

## D-031 — Cestino per percorsi lunghi (0.4.0)
Per i percorsi di 260 o più caratteri si usa `IFileOperation` (COM, thread UI) con `FOFX_RECYCLEONDELETE`, `FOF_ALLOWUNDO` e `FOF_WANTNUKEWARNING`: stesso comportamento di D-023. Se la shell non riesce a interpretare il percorso, nemmeno con il prefisso `\\?\`, il file non viene eliminato e viene mostrato un errore. Sotto i 260 caratteri resta `SHFileOperation`, già verificato nella 0.3.0. L'esito è sempre controllato sul disco.

## D-032 — Dimensione dell'EXE: decisione rimandata alle misure (0.4.0)
L'EXE normale pesa ~256 MB, quello compresso ~110 MB (stima dalla dimensione dello zip). La compressione decomprime gli assembly a ogni avvio. Ogni avvio registra ora `startup` (ms dall'avvio del processo al primo disegno della finestra) con la dimensione dell'EXE, così le due varianti si distinguono nel CSV. La scelta predefinita si farà dopo la misura (TEST-PLAN-0.5.0).
- **Chiusa con D-047 (2026-11-03):** EXE normale.

## D-033 — Icona (0.4.0, richiesta dell'utente)
`src/viBro/viBro.ico`: disegno originale (quadrato arrotondato blu sfumato, triangolo play bianco, barra temporale con avanzamento), generato in modo programmatico in 9 dimensioni (16–256 px), con una versione semplificata per 16–24 px. È usata come `ApplicationIcon` (EXE, barra delle applicazioni, Esplora risorse) e come icona della finestra.

## D-034 — Icone dell'albero (0.4.0, richiesta dell'utente)
Icone vettoriali a colori disegnate nel codice (`Util/TreeIcons.cs`, griglia 16×16), scelte per tipo di nodo (`FolderNodeKind`) e, per le cartelle utente, per nome; la cartella appare aperta quando il nodo è espanso. Alternative scartate: glifi monocromi di Segoe Fluent Icons (meno colore) e icone reali di Windows con `SHGetFileInfo` (una chiamata alla shell per nodo, lenta o bloccante su NAS scollegati, contraria al caricamento progressivo dell'albero). Le icone sono oggetti congelati e condivisi, uguali in Light e Dark. Se un'icona non si potesse creare, il nodo resta senza icona e l'errore va in `viBro-errors.log`.

## D-035 — Righe compatte (0.4.0, richiesta dell'utente)
Righe di circa 24 px nell'elenco e nell'albero. Il tema Fluent di .NET 10 (sorgenti `ListViewItem.xaml` e `TreeViewItem.xaml`) usa per le righe di `GridView` un'altezza minima `GridViewItemContainerMinHeight` e un margine inferiore di 2 px; per i nodi dell'albero un margine interno di 4 px e un margine di 2 px applicato due volte (il modello riusa `Margin` sul bordo interno). Negli stili costruiti da `ApplyItemStyles()`: elenco `MinHeight=24`, `Margin=0`; albero `Padding=4,2.5`, `Margin=0`.

## D-036 — Clic e trascinamento sui cursori (0.4.0, richiesta dell'utente)
Con `IsMoveToPointEnabled` WPF sposta il valore al punto cliccato ma non avvia il trascinamento. `Util/SliderDrag` intercetta la pressione (anche se già gestita dallo `Slider`), aggiorna il layout e passa la stessa pressione al `Thumb`, che cattura il mouse e trascina come se fosse stato premuto direttamente. Il `Thumb` non prende il focus, che resta sull'elenco dei file (tasti di salto). Applicato a barra temporale e volume. Durante il trascinamento i salti restano accorpati come prima.

## D-037 — Ripetizione dei tasti letta da Windows (0.4.0, segnalazione dell'utente)
Le operazioni sui file (R/Canc, F2, W) ignorano la ripetizione automatica, così un tasto tenuto premuto non ripete l'operazione. `KeyEventArgs.IsRepeat` di WPF però non è il flag di Windows: WPF (`KeyboardDevice`, sorgente .NET 10) segna come ripetuta la seconda pressione dello stesso tasto se nel frattempo non ha visto il rilascio di nessun tasto. Il rilascio di R arriva alla finestra di conferma, non a WPF; se poi si usa solo il mouse, la R successiva risultava "ripetuta" e veniva ignorata. Ora `OnThreadPreprocessMessage` registra per ogni `WM_KEYDOWN`/`WM_SYSKEYDOWN` del thread il bit 30 di `lParam` (tasto già premuto), e sia l'hook sia `Window_PreviewKeyDown` usano quel valore.

## D-038 — Mouse sul video (0.5.0, richiesta dell'utente)
Il video è la finestra nativa di mpv (`input-cursor=no`), da cui WPF non riceve eventi del mouse. I messaggi si leggono in `OnThreadPreprocessMessage`, come i tasti, senza mai segnarli come gestiti (tranne la rotella a schermo intero), e si trattano in `MainWindow.VideoMouse.cs`. Sono accettati i messaggi diretti alla finestra del video o alle sue figlie e, per sicurezza, quelli diretti alla finestra principale con il puntatore sopra l'area del video.
- **Doppio clic:** riconosciuto da viBro con `GetDoubleClickTime` e il rettangolo `SM_CXDOUBLECLK`/`SM_CYDOUBLECLK`, perché non si sa se la finestra di mpv ha lo stile `CS_DBLCLKS`: sia `WM_LBUTTONDOWN` sia `WM_LBUTTONDBLCLK` contano come clic. Alterna lo schermo intero; un terzo clic inizia una nuova coppia. Il clic singolo fa Play/Pause (vedi D-040).
- **Tasto destro** (`WM_RBUTTONUP`): `ContextMenu` WPF nel punto del puntatore (un popup è una finestra a sé, quindi compare sopra il video). Voci: *Delete*, *Move...*, *Move to <cartella>* (solo se `LastMoveDestination` esiste; percorso completo nel tooltip). Scelta dell'utente fra: solo le due voci richieste, queste tre, oppure anche *Rename*. L'azione parte dopo la chiusura del menu e usa le stesse funzioni dei tasti (conferme, controlli, nessuna ripetizione).
- **Rotella a schermo intero:** `WM_MOUSEWHEEL` arriva alla finestra con il focus o a quella sotto il puntatore, secondo l'impostazione di Windows: viene presa ovunque dentro la finestra di viBro, solo a schermo intero e con la finestra attiva. Giù = successivo, su = precedente (scelta dell'utente); 120 unità = un video, con accumulo per i touchpad e azzeramento al cambio di direzione. A finestra normale la rotella non cambia comportamento.

## D-039 — La 0.4.0 confluisce nella 0.5.0 (scelta dell'utente)
La 0.4.0 non è stata provata né taggata prima della richiesta della 0.5.0. Non avrà un tag proprio: il CHANGELOG conserva la sua sezione ("non rilasciata separatamente") e il tag `v0.5.0` arriverà quando l'utente conferma tutte le prove. Il piano di prova si sposta di versione in versione (`docs/TEST-PLAN-0.4.0.md` → `docs/TEST-PLAN-0.5.0.md`) finché tutte le prove non sono confermate.

## D-040 — Clic singolo = Play/Pause (0.5.0, richiesta dell'utente)
Il primo clic di un doppio clic è un clic singolo. Scelta dell'utente fra due soluzioni standard: **azione immediata con annullamento** (scelta) oppure attesa del tempo del doppio clic prima del Play/Pause (circa 0,5 s di ritardo a ogni clic). Il clic singolo fa subito Play/Pause; se arriva il secondo clic, viBro ripete Play/Pause per tornare allo stato iniziale e alterna lo schermo intero. Il video si ferma per un istante durante il doppio clic.
Il clic che riporta viBro in primo piano non fa Play/Pause (scelta dell'utente). Windows attiva la finestra prima di consegnare il clic, che ha un orario di input precedente all'attivazione: `Window.Activated` registra l'istante (stessa base di `GetTickCount` di `MSG.time`) e un clic con orario non oltre 50 ms dopo l'attivazione è considerato il clic di attivazione. Se poi arriva un secondo clic entro il tempo del doppio clic, si ha comunque lo schermo intero.

## D-041 — Rotella sopra la barra temporale e diagnostica dell'input (0.5.0, segnalazione dell'utente)
L'utente ha osservato che la rotella sopra la barra temporale faceva saltare la posizione nello stesso video, sia a finestra normale sia a schermo intero, con l'EXE compilato dopo `f216c76`. Né WPF (`Slider.cs`, `RangeBase.cs`, stile Fluent `Slider.xaml` di .NET 10: nessuna gestione della rotella) né viBro lo prevedevano, e a schermo intero viBro avrebbe dovuto cambiare video: quindi `WM_MOUSEWHEEL` non arrivava all'hook. Ipotesi: (1) il driver del mouse o del touchpad converte la rotella in tasti freccia, che viBro tratta come salti di ±5 s; (2) la rotella va alla finestra di mpv, creata da mpv in un thread proprio, i cui messaggi l'hook del thread dell'interfaccia non vede. Da verificare con `--trace-input`.
Comportamento voluto (scelta dell'utente): sopra la barra temporale la rotella sposta la posizione. Con viBro: su = avanti 5 s, giù = indietro 5 s (come ↑/↓, e come la convenzione di mpv), in entrambe le modalità; altrove, solo a schermo intero, cambia video (giù = successivo). Il conteggio degli scatti si azzera quando cambiano direzione o destinazione.
- **Esito (2026-11-03):** con l'EXE successivo ad `a2bd495` tutte le prove del mouse e della rotella sono superate (anche schermo intero, doppio clic, menu e clic singolo), quindi i messaggi del mouse sul video arrivano all'hook e l'ipotesi (2) non si applica a questi casi. La causa del primo comportamento osservato (salto con la rotella sopra la barra anche a schermo intero) non è stata identificata; `--trace-input` resta disponibile se si ripresenta.

## D-042 — Chiusura della 0.5.0 (2026-11-03)
L'utente ha confermato tutte le prove di `TEST-PLAN-0.5.0.md` (comprese quelle della 0.4.0) tranne i 5+5 avvii per la misura del tempo di avvio, spostati in `TEST-PLAN-0.6.0.md`. La 0.5.0 viene rilasciata (tag `v0.5.0`); la scelta tra EXE normale e compresso (D-032) resta aperta finché non arrivano le misure. Fino ad allora la variante predefinita resta l'EXE normale.

## D-043 — Play/Pause conservato al cambio di file (0.6.0, richiesta dell'utente)
Modifica della SPEC ("nessun avvio automatico quando cambia la selezione"). `MpvPlayer.Load` riceve lo stato voluto e invia `set pause yes|no` prima di `loadfile`, nello stesso ordine di prima. `LoadItem` usa lo stato osservato del video corrente (`IsPaused`); per elimina, rinomina e sposta, che fermano il player per liberare il file, lo stato è annotato in `ReleaseAsync` prima di fermarlo (`_playAfterRelease`) e azzerato se non segue alcun caricamento. Scelta dell'utente: vale per ogni cambio di file (tasti, rotella, clic nell'elenco, file proposto dopo un'operazione). All'avvio e alla prima selezione il video si carica in pausa.

## D-044 — Copia (0.6.0, richiesta dell'utente)
Stessa procedura dello spostamento, estratta in `PrepareTransferAsync` (destinazione, raggiungibilità, stessa cartella rifiutata, conflitto di nome). `FileOps.CopyAsync` copia in `<nome>.vibro-partial` con avanzamento e annullamento, verifica la dimensione e rinomina; lo spostamento fra volumi la riusa e poi elimina la sorgente. La sorgente è solo letta: il player non viene fermato e il video continua. Destinazione propria (`LastCopyDestination`) e tasti C / Shift+C (scelte dell'utente fra destinazione propria con tasti, solo menu, o destinazione condivisa).

## D-045 — Trascinamento col tasto destro sul video (0.6.0, richiesta dell'utente)
Pressione del tasto destro sul video: inizio del trascinamento potenziale; la finestra principale prende la cattura del mouse (`SetCapture`), perché Windows non la prende per il tasto destro e il rilascio fuori dal video andrebbe perso. Oltre 6 DIP di spostamento orizzontale diventa trascinamento: posizione = posizione iniziale + (spostamento / larghezza del video) × durata (scelta dell'utente: larghezza = durata, invece di una velocità fissa), salti sui fotogrammi chiave durante il movimento (scelta dell'utente: il video segue il mouse) e salto esatto al rilascio; la barra e il tempo mostrano la destinazione. Senza trascinamento il rilascio apre il menu. Se il rilascio non arriva (per esempio dopo un cambio di finestra), il primo movimento senza `MK_RBUTTON` chiude il trascinamento.

## D-046 — Rotella, barra di stato, fascia in basso, conferma posizionata, Ctrl+W (0.6.0, richieste dell'utente)
- **Rotella:** sopra il video cambia video anche a finestra normale (scelta dell'utente: solo sopra il video; sopra elenco e albero scorre); sopra la barra temporale 3 s per scatto.
- **Barra di stato:** il tema Fluent mette 12 px di margine interno alla barra e 4 px a ogni elemento (sorgenti `StatusBar.xaml`, `StatusBarItem.xaml`); ora 4,0 e 4,1. Nuovo elemento a sinistra con numero e dimensione totale dei soli video elencati (scelta dell'utente: costo nullo, nessuna lettura in più sul NAS), aggiornato a ogni cambio dell'elenco.
- **Schermo intero:** la fascia della barra temporale è agganciata in basso (`DockPanel.Dock=Bottom`), in alto a finestra normale.
- **Conferma di eliminazione:** `DeleteConfirmDialog` sostituisce la `MessageBox` (che non si può posizionare) e si apre sotto e a destra del puntatore, dentro l'area di lavoro di quel monitor (`MonitorFromPoint`); scelta dell'utente: vicino al puntatore anche con Canc/R.
- **Ctrl+W:** chiude la finestra come il pulsante di chiusura (impostazioni salvate come sempre), anche con il cursore nella barra del percorso. Durante uno spostamento o una copia lunga la finestra principale è disattivata e Ctrl+W non agisce.

## D-047 — EXE normale come variante predefinita (2026-11-03, scelta dell'utente)
Misura su Windows 11 con l'EXE 0.6.0, 5 avvii per variante (righe `startup` di `viBro-metrics.csv`, ms dall'avvio del processo al primo disegno della finestra):

| | EXE normale | EXE compresso | Differenza |
|---|---|---|---|
| Dimensione | 269 629 542 B (257 MiB) | 113 723 616 B (108 MiB) | −58% |
| Avvio, singoli | 549 / 407 / 415 / 410 / 409 | 573 / 569 / 575 / 595 / 588 | |
| Avvio, mediana | 410 ms | 575 ms | +165 ms (+40%) |
| Memoria di lavoro | ~141 MB | ~210 MB | +69 MB |
| Memoria privata | ~108 MB | ~178 MB | +70 MB |

Il compresso decomprime in memoria gli assembly contenuti nell'EXE a ogni avvio: più tempo e più memoria per tutta la sessione, in cambio solo di spazio su disco e di trasferimento. Con la reattività come priorità della SPEC, la variante predefinita è l'**EXE normale** (`build.cmd`, `artifacts\publish`, zip in `dist\`). Il compresso resta disponibile con `build\build.ps1 -Compress`, non documentato come principale. Il primo avvio dell'EXE normale (549 ms) è più alto degli altri, probabilmente per la cache del disco di Windows; nessuno dei due primi avvii mostra un'estrazione nella cache del bundle (già presente).

## D-048 — Chiusura della 0.6.0 (2026-11-03)
L'utente ha confermato tutte le prove di `TEST-PLAN-0.6.0.md` con l'EXE compilato da `647beda` (codice invariato fino al tag). La 0.6.0 viene rilasciata (tag `v0.6.0`). Nessuna prova aperta passa alla versione successiva; il prossimo piano di prova nascerà con la definizione della 0.7.0 (o della 1.0.0).

## D-049 — Prove con 4K e MOV (2026-11-03, EXE 0.6.0)
Prove dell'utente sull'unità di rete K: (`viBro-metrics.csv`, 39 aperture e cambi, 1131 salti, nessun timeout, `hwdec=d3d11va` su tutti i file). Mediane (p90 / massimo):

| Formato | Aperture | Apertura / cambio | Salto rapido (fotogrammi chiave) | Salto esatto ±1–5 s |
|---|---|---|---|---|
| MKV HEVC 1080p | 22 | 92 ms (190 / 258) | 34 ms oltre 5 s, 24 ms fino a 5 s | 52 ms (117 / 148) |
| MP4 H.264 1080p/720p | 12 | 102 ms (213 / 224) | — | — |
| MOV H.264 1080p | 2 (un solo file) | 133 ms (171 max) | 41 ms (57 / 72) | 28 ms (37 / 42) |
| MP4 H.264 4K (3840×2160) | 5 (3 file, fino a 12 GB) | 93 ms (152 / 287) | 54 ms (82 / 162) | 87 ms (151 / 166) |

- **Esito:** 4K H.264 e MOV H.264 funzionano con tempi dello stesso ordine del 1080p; il 4K costa circa +20 ms sui salti rapidi e +35 ms su quelli esatti. Il massimo di 287 ms è la prima apertura del file da 12 GB.
- **Memoria:** con il 4K la memoria di lavoro arriva a ~550 MB e quella privata a ~790 MB (superfici di decodifica più grandi), contro 260–470 MB misurati nella 0.1.0 con il 1080p. Nel corso della sessione scende e risale con i file aperti (ultimo valore 465 / 686 MB): nessun segno di crescita continua, ma la prova di memoria lunga (50+ cambi) resta da fare.
- **Registro degli errori:** "Referenced QT chapter track not found" è un avviso di ffmpeg su un MP4 4K con riferimento a una traccia capitoli mancante; il file si riproduce. Alle 09:26:49 un file MP4/MOV della cartella non si è aperto ("error reading header", `avformat_open_input() failed`): intestazione illeggibile, probabilmente file incompleto o danneggiato. viBro non ha scritto "Cannot open", perché nel frattempo era già stato chiesto il file successivo (scorrimento veloce) e l'esito del caricamento superato viene ignorato: soffermandosi su quel file, la barra di stato mostra "Cannot play …".
- **Altri tipi di file:** su decisione dell'utente (2026-11-03) non sono previste altre prove di formato; le prove di 4K e MOV si considerano concluse.

## D-050 — Licenza dell'EXE distribuito (2026-11-03)
`packages/ffmpeg.cmake` di shinchiro (master, letto il 2026-11-03) configura FFmpeg con `--enable-gpl --enable-version3` e senza `--enable-nonfree`: `libmpv-2.dll` è ridistribuibile e la combinazione è **GPL-3.0**. `THIRD-PARTY-NOTICES.md` indicava "GPL-2.0-or-later" per la build: corretto. Il codice di viBro (GPL-3.0-or-later) è compatibile; Apache-2.0 (loader Vulkan, OpenSSL 3) e MIT (.NET) sono compatibili con la GPLv3.
Obblighi solo quando l'EXE viene trasmesso ad altri (non per l'uso personale o dentro un'organizzazione, secondo le FAQ della FSF): testi di licenza e avvisi (già nello zip) e **sorgente corrispondente** di tutto ciò che è nell'EXE per tutta la durata della distribuzione (GPLv3 §6), compresa la build di libmpv, che va archiviata in proprio perché shinchiro conserva solo ~30 build. Il workflow GitHub carica l'EXE come artifact a ogni push su `main` e crea una release in bozza a ogni tag: se il repository è pubblico, questo è già distribuzione. Da decidere con l'utente (dipende dall'uso previsto). Non è consulenza legale; i brevetti su H.264/HEVC sono un tema separato, da verificare prima di un rilascio pubblico o commerciale.

## D-051 — README in inglese (2026-11-03, richiesta dell'utente)
`README.md` è ora in inglese (pagina principale su GitHub e file incluso nello zip); la versione italiana è `README.it.md`, con collegamenti reciproci. Gli altri documenti (SPEC, CHANGELOG, DECISIONS, piani di prova, CONTEXT) restano in italiano. A ogni modifica del README vanno aggiornate entrambe le versioni.

## D-052 — Solo sorgente, nessun EXE pubblicato (2026-11-03, scelta dell'utente)
Il repository `rives1/viBro` è pubblico e il workflow caricava lo zip con l'EXE come artifact a ogni push su `main` (e in una release in bozza a ogni tag): l'EXE veniva quindi trasmesso a terzi. L'utente ha chiesto l'archivio del sorgente della libmpv inclusa, ma non è ricostruibile in modo esatto: il workflow di shinchiro (`mpv_gcc.yml`, commit `bda10a2` del 2026-10-30, l'ultimo prima della build `20261101`) esegue `ninja update` e compila l'ultima revisione di ogni libreria senza registrarla; la release non riporta le revisioni (neanche le build di zhongfly, che indicano solo il commit di mpv). Un archivio "alla data" sarebbe un'approssimazione, non il sorgente corrispondente.
Alternative presentate: (A) nessun EXE pubblico; (B) build di libmpv in proprio con revisioni registrate e archivio del sorgente (ore di CI, archivi di centinaia di MB, tentativi che richiedono il push dell'utente); (C) A subito e B dopo. Scelta dell'utente: **A**.
- `.github/workflows/build.yml`: resta la compilazione di verifica; tolti `upload-artifact` e la release in bozza; permessi `contents: read`.
- README (inglese e italiano): sezione "Getting viBro / Come ottenere viBro": si compila con `build.cmd`, che scarica libmpv direttamente da shinchiro.
- `THIRD-PARTY-NOTICES.md`: politica di distribuzione al posto della lista di obblighi.
- **Pulizia una tantum su GitHub (utente):** cancellare gli artifact delle esecuzioni precedenti (Actions → esecuzione → Artifacts, oppure `gh api` sull'endpoint `actions/artifacts`) e le release in bozza dei tag (contengono lo zip; le bozze sono visibili solo a chi ha accesso in scrittura, ma conviene eliminarle). Verificare che non esistano release pubblicate con lo zip.
- Per tornare a pubblicare un EXE: strada B.

## D-053 — Nessun percorso personale nel repository (2026-11-03, scelta dell'utente)
La SPEC vieta percorsi personali nel repository pubblico, ma `CONTEXT.md` e le fotografie `CONTEXT-v0.5.0.md` e `CONTEXT-v0.6.0.md` contenevano il percorso della cartella del progetto con il nome utente di Windows. Rimosso da tutti i file; il percorso resta solo nel documento privato del progetto claude.ai. Scelta dell'utente: nessuna riscrittura della storia Git già pubblicata (resta nei commit precedenti). Le fotografie, normalmente da non modificare, sono state corrette solo in questo punto.

## D-054 — Autore: rives1 (2026-11-03, scelta dell'utente)
I commit erano firmati con il segnaposto `viBro developer`. Dalla 1.0.0: `rives1 <rives1@users.noreply.github.com>` (configurazione Git locale del repository) e, nelle proprietà dell'EXE, `Company = rives1`, `Copyright (C) 2026 rives1. GPL-3.0-or-later.` L'indirizzo noreply nel formato `ID+rives1@users.noreply.github.com` (account creati dopo il 2017) collega i commit al profilo GitHub; quello usato non espone l'email ma potrebbe non collegarli: va sostituito con quello indicato in GitHub → Settings → Emails se l'utente lo desidera. I commit precedenti restano con il segnaposto.

## D-055 — Contenuto della 1.0.0 (2026-11-03)
Nessuna funzione nuova: la 1.0.0 è la 0.6.0 verificata nel suo insieme. Revisione rispetto alla SPEC: requisiti soddisfatti; WMV resta tra i formati (provato nella 0.1.0: VC-1 con decodifica hardware, WMV3 in software; l'assistente aveva detto per errore che non era mai stato provato, e l'utente ha confermato di tenerlo). Piano di prova `TEST-PLAN-1.0.0.md`: regressione compatta di tutte le funzioni, un file per formato (WMV compreso) e sessione di memoria di almeno 50 cambi di video (scelta dell'utente). Il tag `v1.0.0` si crea dopo la conferma di tutte le prove.

## D-056 — Chiusura della 1.0.0 (2026-11-03)
L'utente ha confermato tutte le prove di `TEST-PLAN-1.0.0.md` con l'EXE compilato da `7a7d0d5` (codice invariato fino al tag). Sessione di memoria: 90 caricamenti in 38 minuti, memoria di lavoro mediana 333 MB nei primi 10 e 325 MB negli ultimi 10, privata 364 → 400 MB, nessuna crescita continua; apertura/cambio mediana 86 ms, salti rapidi 17 ms, esatti 43 ms, nessun timeout. Limite: la sessione non conteneva file 4K (per il 4K vale D-049). Rilascio: tag `v1.0.0`, distribuzione solo come sorgente (D-052). Nessuna prova aperta passa alla versione successiva.

## D-057 — Contenuto della 1.1.0 (2026-11-03, richieste dell'utente)
Nuove funzioni compatibili, quindi versione MINOR.
- **Barra dei controlli sotto il video, sopra l'elenco** (annulla in parte D-029): la barra è agganciata in basso nella stessa cella del video (`DockPanel` nella riga 0 di `RightArea`) e non in una riga propria, perché il divisore `PreviousAndNext` avrebbe ridimensionato la barra (riga `Auto`) invece del video. A schermo intero elenco e divisore sono nascosti e la riga dell'elenco è a 0, quindi la fascia resta sotto il video come nella 0.6.0; non serve più spostarla con `DockPanel.SetDock`.
- **Rotella sopra il video a finestra normale = ±3 s** (scelta dell'utente: lo stesso passo della barra, invece di 5 o 10 s; su = avanti). A schermo intero resta il cambio di video, tranne sopra la barra temporale (±3 s). Il conteggio degli scatti si azzera quando cambia la funzione (salto o cambio video).
- **Icona della barra del percorso:** `TreeIcons.Folder`, decorativa (scelta dell'utente; per scegliere una cartella resta *Browse...*).
- **Rinomina:** cursore all'inizio del nome (`CaretIndex = 0`), senza selezione.

## D-058 — Contenuto della 1.2.0 (2026-11-03, richieste dell'utente)
Proposte dell'assistente scelte dall'utente, tutte senza costo sulle prestazioni (nessuna lettura dei file, nessuna animazione). La 1.1.0, non ancora provata, confluisce nella 1.2.0 senza tag proprio (come la 0.4.0, D-039); il piano `TEST-PLAN-1.1.0.md` diventa `TEST-PLAN-1.2.0.md`.
- **Pulsanti a icone:** font `Segoe Fluent Icons` (Windows 11) con ripiego su `Segoe MDL2 Assets` (stessi codici: E768 Play, E769 Pause, E767 Volume, E74F Mute, E838 cartella aperta, E706 sole, E708 luna). Il pulsante del tema mostra il tema attuale. Contenuto e tooltip cambiano solo quando cambia lo stato.
- **Tempo sotto il puntatore:** `Popup` sopra la barra (finestra a sé, quindi visibile anche sopra il video), valore da `Track.ValueFromPoint`; nessuna anteprima.
- **File caricato nell'elenco:** `VideoItem` notifica `IsCurrent` e `StateGlyph`; la colonna Name ha un modello con icona ▶/❚❚ e nome in grassetto (scelta dell'utente: icona e grassetto). Lo stato si aggiorna con il timer dell'interfaccia (100 ms), solo quando cambia; azzerato quando un'operazione sui file libera il player o un caricamento fallisce.
- **Posizione nella cartella:** "indice / totale" nella barra di stato, aggiornato a ogni cambio di selezione.
- **OSD di mpv:** comando `show-text` (testo letterale: i comandi passati come array non espandono le proprietà); tempo per 1 s dopo ogni salto completato (con la posizione raggiunta) e durante il trascinamento col tasto destro, nome del file per 0,8 s dopo ogni caricamento. Solo a schermo intero (scelta dell'utente: a finestra normale la barra è sempre visibile). Carattere OSD ridotto (`osd-font-size=36`, `osd-border-size=2`). Da osservare: il primo testo potrebbe richiedere un breve tempo per caricare i caratteri.
- **Ctrl+F2:** finestra *Commands* (modale, Esc/Invio chiude) con tastiera, mouse sul video e barra temporale. F2 da solo resta la rinomina.

## D-059 — Chiusura della 1.2.0 (2026-11-03)
L'utente ha confermato tutte le prove di `TEST-PLAN-1.2.0.md`, che comprende quelle della 1.1.0, con l'EXE compilato da `39d2eec` (codice invariato fino al tag). La 1.2.0 viene rilasciata (tag `v1.2.0`); la 1.1.0 non ha un tag proprio. Nessuna prova aperta passa alla versione successiva.

## D-060 — Contenuto della 1.3.0 (2026-11-03, richieste dell'utente)
- **Sottocartelle nell'elenco** (modifica della SPEC "solo file video"): `FolderScanner.Scan` usa `EnumerateFileSystemInfos` (una sola lettura, stessi filtri: niente nascosti o di sistema) e aggiunge `..` calcolato dalla stringa del percorso (assente nelle radici). Ogni riga ha un tipo (`EntryKind`: Parent, Folder, Video); l'ordinamento mette sempre `..`, poi le cartelle, poi i video, e applica colonna e direzione dentro ogni gruppo. Scelte dell'utente: cartelle in cima con `..`; apertura con doppio clic o Invio (il clic singolo seleziona soltanto, il video caricato resta). Tutto ciò che agisce sul file selezionato usa solo i video (`SelectedVideo`); D/E, rotella e la scelta del file dopo elimina/sposta saltano le cartelle; la barra di stato conta cartelle e video separatamente. Tornando con `..` viene selezionata la cartella appena lasciata. Le cartelle hanno la stessa icona dell'albero; colonna Size vuota.
- **Shift+Invio:** `Process.Start` con `UseShellExecute` su un thread di lavoro (la risoluzione dell'associazione può essere lenta su NAS); viBro va in pausa (scelta dell'utente: due audio insieme sarebbero confusi); errori nella barra di stato e in `viBro-errors.log`. Invio da solo apre la cartella selezionata; sul video non fa nulla.
- **Rotella (annulla D-057 per la rotella):** sopra la barra temporale cambia video, sopra il video sposta il tempo di 3 s, in entrambe le modalità; a schermo intero tutto ciò che non è la barra conta come video. Il tempo sotto il puntatore sulla barra resta.

## D-061 — Chiusura della 1.3.0 (2026-11-04)
L'utente ha confermato tutte le prove di `TEST-PLAN-1.3.0.md` con l'EXE compilato da `8623c22` (codice invariato fino al tag). La 1.3.0 viene rilasciata (tag `v1.3.0`). Nessuna prova aperta passa alla versione successiva.

## D-062 — Contenuto della 1.4.0 (2026-11-04, richieste dell'utente)
Richiesta: spostare nel file `.ini` le durate dei salti su tre livelli (piccolo: frecce ←/→ e rotella; medio: PgUp/PgDown; grande: Alt+PgUp/PgDown) e aggiungere un parametro per generare o no `viBro-metrics.csv`.
- **Cinque chiavi invece di tre** (scelte dell'utente): nel codice i passi erano 5, non 3. ↑/↓ (5 s) hanno una chiave propria; frecce (1 s) e rotella (3 s) restano separate, così nulla cambia finché l'utente non modifica i valori. Sezione `[Seek]`: `LeftRightSeconds`, `UpDownSeconds`, `WheelSeconds`, `PageSeconds`, `AltPageSeconds`, predefiniti 1/5/3/60/300. Limiti 0,1–3600 s (0,1 = precisione con cui l'INI salva i numeri); un valore non valido torna al predefinito con avviso, come gli altri parametri. Nessun vincolo d'ordine tra i livelli. Lo schema resta 1: le chiavi mancanti prendono il predefinito.
- **Esatto o su fotogramma chiave:** resta la regola di D-007: un passo più corto di `ExactSeekThresholdSeconds` (30) è esatto, gli altri vanno al fotogramma chiave salvo `ExactLongSeeks`. Un passo medio sotto 30 s diventa quindi esatto (più lento); documentato nel README invece di cambiare la regola.
- **Finestra dei comandi (Ctrl+F2):** mostra le durate in uso (minuti interi in `min`, il resto in `s`).
- **`viBro-metrics.csv`:** il parametro esisteva già (`[Diagnostics] MetricsLog`, predefinito `true`); con `false` il file non viene né creato né scritto. Scelta dell'utente: lasciarlo com'è, solo documentazione più chiara.
- Versione 1.4.0 (nuove opzioni compatibili = MINOR).

## D-063 — Chiusura della 1.4.0 (2026-11-04)
L'utente ha confermato tutte le prove di `TEST-PLAN-1.4.0.md` con l'EXE compilato da `2d3863c` (codice invariato fino al tag). La 1.4.0 viene rilasciata (tag `v1.4.0`). Nessuna prova aperta passa alla versione successiva.

## D-064 — Contenuto della 1.5.0 (2026-11-04, richieste dell'utente)
Richiesta: con I e O segnare inizio e fine di una parte del video e con F9 salvarla in un nuovo file nella stessa cartella, chiamato `nome-extract-` più data, ora e minuto.
- **Copia dei dati, senza ricodifica** (scelta dell'utente, invece della ricodifica precisa al fotogramma, lenta e con perdita di qualità, e del taglio misto, fragile). Conseguenza nota e documentata: l'estratto parte dal fotogramma chiave al marker I o prima; con i GOP misurati (5–24 s, D-007) l'anticipo può arrivare a 24 s.
- **Strumento: `ffmpeg.exe` esterno fornito dall'utente** nella cartella di `viBro.exe` (scelta dell'utente). Non è incluso né scaricato da `build.cmd`: l'EXE resta unico e la politica di distribuzione (D-052) non cambia; viBro lo avvia come programma separato. Scartati: i comandi `dump-cache` di libmpv (salvano solo ciò che è nella cache di lettura, che in viBro è spenta per il NAS; fragili secondo la documentazione di mpv) e la modalità di codifica di libmpv (non verificata). Senza `ffmpeg.exe` la funzione è disattivata: I/O/F9 scrivono un avviso nella barra di stato, Ctrl+F2 indica che manca. La presenza si controlla a ogni uso (`File.Exists`, locale).
- **Comando:** `-ss <I> -i <file> -t <O−I> -map 0:v -map 0:a? [-map 0:s? solo MKV] -c copy -avoid_negative_ts make_zero -n -progress pipe:1`. Il salto sull'ingresso è rapido anche sul NAS; con la copia ffmpeg tiene i fotogrammi dal fotogramma chiave precedente e la durata si conta da I, quindi la fine cade vicino a O. Sottotitoli solo in MKV: MP4/MOV possono contenere tracce di sottotitoli o di dati non copiabili. Provato con ffmpeg 9.0.2 su MP4, MOV e MKV H.264 (GOP 10 s): I = 23,4 s e O = 33,4 s danno un estratto di 13,6 s che parte da 20 s.
- **Nome:** `nome-extract-aaMMgg-OOmm.ext` con entrambi i trattini (scelta dell'utente; il primo esempio non aveva il secondo), ora locale; se esiste, `-2`, `-3` … (mai sovrascrivere: anche `-n` di ffmpeg). Stessa estensione dell'originale.
- **Marker:** posizione corrente (`time-pos`), legati al file caricato; cambiando file non valgono più. O prima di I, o un marker mancante, danno un avviso su F9. Sulla barra temporale (richiesta successiva dell'utente) una linea verde in I, una rossa in O e una fascia gialla semitrasparente tra i due, disegnate su un `Canvas` sopra lo `Slider` che non riceve il mouse; la posizione segue il `Track` del cursore (metà del pollice + frazione della corsa), quindi un marker cade sotto il pollice a quel tempo. Aggiornati col timer dell'interfaccia (100 ms) e al ridimensionamento; nascosti se i marker non sono del file caricato.
- **Esecuzione:** il file sorgente è solo letto, il player lo tiene aperto e continua. Avanzamento da `out_time_us` di `-progress`, finestra di avanzamento (riusata da sposta/copia) dopo 0,4 s; annullamento = chiusura del processo; in caso di errore o annullamento il file parziale viene cancellato (solo se creato da questa esecuzione). Il nuovo file viene aggiunto all'elenco senza cambiare selezione né caricamento. A estrazione riuscita, una finestra di messaggio (richiesta dell'utente) con nome, dimensione, intervallo e cartella; gli errori restano in una finestra di avviso.
- Versione 1.5.0 (nuova funzione compatibile = MINOR).

## D-065 — Chiusura della 1.5.0 (2026-11-04)
L'utente ha confermato tutte le prove di `TEST-PLAN-1.5.0.md` con l'EXE compilato da `7adb8d7` (codice invariato fino al tag). La 1.5.0 viene rilasciata (tag `v1.5.0`). `ffmpeg.exe` resta fornito dall'utente e non distribuito. Nessuna prova aperta passa alla versione successiva.

## D-066 — Contenuto della 1.6.0 (2026-11-04, richieste dell'utente)
Richiesta: con F10 una galleria dei video della cartella in una finestra separata, una riga per video con 5 immagini prese agli stessi intervalli (esempio dell'utente: 1, 2, 3, 4 min e un minuto prima della fine); selezionando la riga, il programma si posiziona sullo stesso file nell'elenco. Inoltre: preparare le release su GitHub o spiegare come farle.
- **Posizioni proporzionali** (scelta dell'utente): l'esempio con minuti fissi mostrerebbe di un film di 2 ore solo i primi 4 minuti e la fine, e su un video sotto i 5 minuti le posizioni si sovrapporrebbero. Predefinite 10/30/50/70/90 % della durata, modificabili in `[Gallery] PositionsPercent` (da 1 a 10 valori, scelta dell'utente: modificabili come `[Seek]`). Ogni tempo è limitato a durata − 0,5 s (un salto all'ultimo istante può non trovare fotogrammi).
- **Motore: lo stesso `ffmpeg.exe` dell'estratto** (scelta dell'utente; scartata una seconda istanza di libmpv con disegno fuori schermo, più complessa e non verificata). Senza `ffmpeg.exe` F10 dà un avviso nella barra di stato. Per ogni video: un'esecuzione `ffmpeg -i <file>` per durata e tempo di inizio (riga `Duration: …, start: …`; termina con codice 1 per l'assenza di un'uscita, atteso), poi una per immagine: `-noaccurate_seek -ss <t> -i <file> -copyts -map 0:v:0 -frames:v 1 -vf showinfo,scale=w=480:h=270:force_original_aspect_ratio=decrease -f image2pipe -c:v mjpeg -q:v 4 pipe:1`. Senza salto accurato ffmpeg decodifica il solo fotogramma chiave raggiunto (rapido; con GOP fino a 24 s l'immagine può distare fino a un GOP dalla percentuale). Il tempo reale del fotogramma si legge dalla prima riga `pts_time` di showinfo (con `-copyts` è il tempo del file) meno il tempo di inizio, cioè la posizione di mpv: è il tempo mostrato sotto l'immagine e quello raggiunto col clic (salto esatto), quindi il player mostra lo stesso fotogramma. Prove con ffmpeg 6.1 (Ubuntu) su MP4 H.264 (GOP 10 s): richiesta 36 s → fotogramma 30 s, 66–80 ms per immagine, JPEG di ~9 KB; MPEG-TS con tempo di inizio 2,78 s: tempi corretti, 100–400 ms. Sul PC dell'utente c'è ffmpeg 9.0.2: da confermare nel piano di prova. Ogni esecuzione ha un limite di 30 s (condivisione che non risponde), poi viene chiusa.
- **Lavoro in background:** `[Gallery] Jobs` esecuzioni contemporanee (predefinito 2, 1–8) su thread di lavoro; si parte dalla prima riga visibile e, dopo uno scorrimento, da quella nuova. Chiudendo la galleria (o viBro) i processi in corso vengono chiusi. La lettura della durata avviene solo su richiesta (F10), quindi resta valida la regola della SPEC di non leggere i metadati all'apertura della cartella.
- **Memoria** (scelta dell'utente: nessuna cache su disco): si conservano solo i JPEG (~10–30 KB); l'immagine viene decodificata quando la riga è visibile (elenco virtualizzato con riciclo), quindi una cartella grande non tiene centinaia di bitmap. Cache in memoria per tutta la sessione, chiave percorso + dimensione + data di modifica + posizioni, limite 64 MB di JPEG con rimozione dei meno usati: F10 su una cartella già vista è immediato.
- **Clic** (scelta dell'utente): riga (o frecce) = seleziona e carica nella finestra principale con Play/Pause conservato (come un clic nell'elenco); immagine = carica e salta al suo tempo (se il file è già caricato, solo il salto). Il file già caricato non viene ricaricato. La galleria resta sulla cartella di F10 (scelta dell'utente): se la finestra principale ne mostra un'altra, il clic riapre la cartella della galleria e poi seleziona il video. F10 di nuovo chiude la galleria e ne crea una della cartella corrente. Finestra senza proprietario (può stare dietro alla principale o su un altro schermo), tema dell'applicazione, Esc chiude; immagini in riquadri 16:9 che occupano la larghezza della finestra (scelta dell'utente), così le righe non cambiano altezza mentre arrivano le immagini.
- **F10** è un tasto di sistema per Windows (attiva la barra dei menu): viene gestito come scorciatoia, così Windows non entra in modalità menu.
- **Release su GitHub** (scelta dell'utente: automatiche al tag). Nuovo job `release` in `.github/workflows/build.yml`: dopo il job `build` riuscito, su un tag `v*` (oppure a mano con `workflow_dispatch` e `release_tag` per un tag esistente) estrae dal CHANGELOG la sezione `## [x.y.z]` (se manca, si ferma con errore), aggiunge una nota "solo sorgente" e crea la release con `gh release create --verify-tag`, o ne aggiorna le note se esiste già. Solo la versione più alta diventa "Latest". Permesso `contents: write` solo per questo job. Nessun file allegato: GitHub aggiunge da sé gli archivi del sorgente del tag; D-052 resta valida (nessun EXE). Istruzioni in `docs/RELEASE.md`.
- Correzioni della documentazione: la SPEC non riportava la 1.5.0 (versioni e tasti I/O/F9) e indicava come fisse le durate dei salti configurabili dalla 1.4.0.
- Versione 1.6.0 (nuova funzione compatibile = MINOR).

## D-067 — Chiusura della 1.6.0 (2026-11-04)
L'utente ha confermato le prove di `TEST-PLAN-1.6.0.md` con l'EXE compilato da `8366299` (codice di `b6d3c1f`, invariato fino al tag), compreso l'ordine della galleria uguale a quello dell'elenco al momento di F10. La 1.6.0 viene rilasciata (tag `v1.6.0`). **Aperta:** la sezione *Release su GitHub* (job `release` sul tag `v1.6.0` e avvio a mano per `v1.5.0`) si può verificare solo dopo il push di commit e tag; un eventuale difetto del workflow si corregge in una 1.6.1.

## D-068 — Contenuto della 1.7.0 (2026-11-05, richieste dell'utente)
Richiesta: nell'elenco selezionare più file come in Windows (Ctrl, Shift per un intervallo); copia, eliminazione e spostamento agiscono sulla selezione; con più file selezionati il video carica l'ultimo selezionato.
- **Selezione:** `ListView` in modalità `Extended` (Ctrl + clic, Shift + clic con l'ancora di WPF). **Ctrl+A** (scelta dell'utente) seleziona tutti i video, non le cartelle (`SelectAll` e poi la rimozione delle cartelle: un solo evento per la lista). Le frecce, per specifica, spostano sempre il tempo: Shift + frecce non estende la selezione, che si fa col mouse.
- **Video caricato = ultimo cliccato** (scelta dell'utente). Con più righe selezionate `SelectedItem` di WPF è il *primo* elemento, non l'ultimo: viBro registra la riga sotto il tasto sinistro (`PreviewMouseLeftButtonDown`) e alla variazione di selezione carica quella, se è un video selezionato. Un Ctrl + clic che deseleziona non carica nulla (il video resta). Un clic che non cambia la selezione (riga già selezionata) carica comunque quel video al rilascio del tasto. Per selezioni fatte da codice o tastiera vale l'ultimo video aggiunto. Il file già caricato o in caricamento non viene ricaricato.
- **Operazioni sulla selezione** (sempre in ordine di elenco; cartelle e `..` ignorate):
  - *Elimina:* una conferma con numero e primi 8 nomi; un file alla volta (Cestino con `SHFileOperation`/`IFileOperation` come prima, sul thread dell'interfaccia con una pausa tra i file; in rete eliminazione definitiva su un thread di lavoro). Se Windows non può cestinare un file chiede per quel file, come prima.
  - *Sposta / copia:* destinazione, raggiungibilità e "stessa cartella" una sola volta. I conflitti si risolvono **prima** di trasferire (scelta dell'utente: per file, con "Applica a tutti"): finestra di conflitto con *Skip* e la casella *Do the same for the next conflicts* quando i file sono più di uno; *Cancel* annulla tutto (niente trasferito). I nomi "keep both" non possono coincidere con quelli di altri file dello stesso trasferimento (`FileOps.UniqueName` con nomi riservati). Trasferimento un file alla volta con una finestra di avanzamento complessiva (dopo 0,4 s, `file k of n`, barra per byte). Annullando si ferma il file in corso (che resta com'era, come prima) e i successivi; i file completati restano. Un errore non ferma gli altri; alla fine un messaggio con i file non riusciti, scritti anche in `viBro-errors.log`. Dopo uno spostamento fallito si controlla se la sorgente esiste ancora.
  - *Player:* viene liberato solo se il file caricato (o in caricamento) è tra quelli eliminati o spostati; altrimenti continua. La copia non lo libera mai.
  - *Dopo elimina/sposta:* se il file liberato non è stato rimosso, viene riselezionato e ricaricato alla posizione; se il file caricato non era coinvolto resta caricato e selezionato; altrimenti si carica il video che seguiva l'ultimo rimosso, o il più vicino prima (Play/Pause conservato). Con un solo file i messaggi restano quelli di prima.
- **Comandi su un solo file disattivati con più video selezionati** (scelta dell'utente, invece di agire sul file caricato): F2, Shift+Invio, I, O, F9; avviso nella barra di stato. Invio apre una cartella solo se è l'unica riga selezionata.
- **D / E, rotella sulla barra, galleria:** partono dal video caricato e selezionano solo il nuovo file (`SelectAndLoad`), così la selezione multipla si chiude come con le frecce in Esplora file.
- **Barra di stato:** con più video selezionati `3 selected (4.2 GB) · 128 videos · …`; aggiornata una volta per le selezioni fatte da codice (Ctrl+A, riordino), non per ogni riga.
- Il riordino per colonna e l'aggiunta dell'estratto conservano l'intera selezione.
- Versione 1.7.0 (nuova funzione compatibile = MINOR).

## D-069 — Chiusura della 1.7.0 (2026-11-05)
L'utente ha confermato tutte le prove di `TEST-PLAN-1.7.0.md` con l'EXE compilato da `b69f059` (codice invariato fino al tag), comprese le prove della release su GitHub rimaste aperte dalla 1.6.0: commit e tag `v1.6.0` risultano pubblicati (`git ls-remote`); la release stessa non era verificabile da questa sessione (API di GitHub non disponibile senza credenziali) e si basa sulla conferma dell'utente. La 1.7.0 viene rilasciata (tag `v1.7.0`): al push del tag il job `release` crea `viBro 1.7.0`. Nessuna prova aperta passa alla versione successiva.

## D-070 — Contenuto della 1.8.0 (2026-11-06, richieste dell'utente)
Richieste: F5 aggiorna albero ed elenco; file .avi visibili e gestiti; eliminazione dal menu del tasto destro sul video senza conferma. (La coda delle operazioni con pannello in basso, proposta il 2026-11-05, è stata ritirata dall'utente.)
- **F5, elenco:** nuova lettura della cartella corrente (`FolderScanner.Scan`, stessa regola di non bloccare l'interfaccia) e fusione con l'elenco: le righe dei file ancora presenti restano gli **stessi oggetti** `VideoItem` (dimensione e data aggiornate con `UpdateInfo`), così restano validi selezione, file caricato o in caricamento (il risultato del caricamento in corso è legato all'oggetto), marker e righe della galleria; le nuove si aggiungono, le sparite si tolgono. Riordino con la colonna attuale, selezione conservata (senza le righe sparite), posizione di scorrimento ripristinata. Se il video caricato è sparito viene scaricato (il player chiude il file); marker di un file sparito annullati. Ignorato durante un'operazione sui file; un'apertura di cartella successiva prevale (contatore di generazione).
- **F5, albero:** solo i nodi già letti vengono riletti (gli altri si leggono quando si aprono), in parallelo per livello; nodi esistenti conservati (stato aperto, selezione, figli), nuove cartelle inserite in ordine naturale, sparite rimosse; un nodo aperto la cui lettura era fallita viene riprovato. Ogni lettura ha un limite di 10 s: una condivisione lenta resta com'era e non ritarda l'elenco (l'albero si aggiorna in background). Unità: aggiunte quelle nuove (`DriveInfo.GetDrives`, nessun accesso alle unità), rimosse quelle scomparse. Provato su Linux con un modello di cartelle: inserimenti, rimozioni, nodo aperto conservato, cartella diventata irraggiungibile.
- **AVI:** `.avi` aggiunto alle estensioni. libmpv e ffmpeg lo leggono; provato con ffmpeg 6.1 su AVI Xvid + MP3 (GOP 10 s): immagini della galleria ai fotogrammi chiave, estratto senza ricodifica dal fotogramma chiave precedente (I = 23,4 s → estratto di 13,4 s da 20 s), come per gli altri formati. Limite noto: AVI con indice incompleto o assente possono avere salti lenti o imprecisi.
- **Elimina dal menu del video senza conferma** (scelte dell'utente): vale per un solo file, **anche in rete** dove l'eliminazione è definitiva (l'assistente aveva sconsigliato quest'ultima parte); con più file selezionati la conferma resta; Canc / R confermano sempre. Se Windows non può cestinare un file locale, la sua domanda di eliminazione definitiva resta. Configurabile con `[Delete] ConfirmFromVideoMenu` (predefinito `false` = senza conferma; scelta dell'utente: configurabile). Modifica della SPEC ("conferma sempre richiesta").
- **Tasto destro nella galleria** (richiesta successiva dell'utente, nella stessa versione): su una riga o un'immagine seleziona e carica il video come il clic sinistro (scelta dell'utente), poi apre lo stesso menu del video, costruito dallo stesso codice (`BuildFileMenu`). Ogni voce, quando viene scelta, riseleziona solo quel video nella finestra principale (riaprendo la cartella della galleria se nel frattempo ne è aperta un'altra) e poi esegue il comando normale: le regole sono identiche, compresa l'eliminazione senza conferma di un file (scelta dell'utente). Le righe di file eliminati o spostati (da qualsiasi comando) o spariti con F5 escono dalla galleria (scelta dell'utente); una riga ancora in lavorazione termina senza essere mostrata né contata come errore. Poiché il video viene caricato, l'operazione aspetta (fino a 5 s, come sempre) che il file sia libero, anche da un `ffmpeg.exe` della galleria che lo stia leggendo.
- Versione 1.8.0 (nuove funzioni compatibili = MINOR).

## D-071 — Chiusura della 1.8.0 (2026-11-06)
L'utente ha confermato tutte le prove di `TEST-PLAN-1.8.0.md` con l'EXE compilato da `dcb4559` (codice di `0d25047`, invariato fino al tag), comprese quelle del tasto destro nella galleria. La 1.8.0 viene rilasciata (tag `v1.8.0`): al push del tag il job `release` crea `viBro 1.8.0`. Nessuna prova aperta passa alla versione successiva.

## D-072 — Contenuto della 1.9.0 (2026-11-07, richieste dell'utente)
- **Rotella con viBro non in primo piano:** la rotella arrivava già a viBro (Windows 10/11 la manda alla finestra sotto il puntatore se *Scorri le finestre inattive al passaggio del mouse* è attiva, predefinito), ma `OnWheel` la ignorava se la finestra principale non era attiva (`IsActive`). Ora il controllo è: nessuna finestra di dialogo modale di viBro (`ComponentDispatcher.IsThreadModal` per le finestre WPF, `IsWindowEnabled` per le finestre Win32 come i messaggi e la scelta della cartella) e finestra non disattivata da un trasferimento. Vale anche con la galleria in primo piano. Con l'impostazione di Windows disattivata la rotella va alla finestra attiva e viBro non può riceverla.
- **Rotella sulla barra invertita** (annulla in parte D-060): su = video successivo, giù = precedente.
- **Tasto destro tenuto + rotella:** durante la pressione del tasto destro sul video (già catturata dalla finestra per il trascinamento, 0.6.0) ogni scatto cambia video come sulla barra (stesso verso, stesso conteggio degli scatti). Dal primo scatto il trascinamento di quella pressione si interrompe e al rilascio non si apre il menu né si salta (il salto sarebbe stato sul file precedente).
- **.m4v:** aggiunta l'estensione; `.mov` c'era già dalla 0.1.0 (confronto senza maiuscole), confermato dall'utente. Per l'estratto ffmpeg riconosce `.m4v` sia come MP4 (muxer `ipod`) sia come flusso MPEG-4 grezzo (muxer `m4v`); ffmpeg 6.1 sceglie MP4, ma per non dipendere dalla versione viBro passa `-f mp4` (provato: estratto MP4 corretto). Galleria e riproduzione invariate.
- Versione 1.9.0 (nuove funzioni compatibili = MINOR).

## D-073 — Chiusura della 1.9.0 (2026-11-07)
L'utente ha confermato tutte le prove di `TEST-PLAN-1.9.0.md` con l'EXE compilato da `2e71b16` (codice invariato fino al tag). La 1.9.0 viene rilasciata (tag `v1.9.0`): al push del tag il job `release` crea `viBro 1.9.0`. Nessuna prova aperta passa alla versione successiva.

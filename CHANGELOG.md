# Changelog

Formato basato su [Keep a Changelog](https://keepachangelog.com/it/1.1.0/); il progetto usa il [Semantic Versioning](https://semver.org/lang/it/).

## [0.5.0] - non ancora rilasciata

Mouse sul video. Comprende anche le modifiche della 0.4.0, che non è stata rilasciata separatamente.

### Aggiunto
- Clic sul video: Play/Pause, a finestra normale e a schermo intero. Il clic che riporta viBro in primo piano non cambia la riproduzione.
- Doppio clic sul video: entra nello schermo intero; a schermo intero, doppio clic per tornare alla finestra normale. Tempo e distanza del doppio clic sono quelli impostati in Windows. Il Play/Pause del primo clic viene annullato, quindi la riproduzione resta com'era (il video si ferma solo per un istante).
- Tasto destro sul video: menu con *Delete* (come Canc/R), *Move...* (come W) e *Move to <cartella>* (come Shift+W, presente solo se esiste una destinazione precedente). Le voci sono disattivate se nessun video è selezionato o se un'operazione è in corso.
- A schermo intero, rotella del mouse: verso il basso video successivo, verso l'alto precedente (uno scatto = un video, come D/E). Gli scorrimenti fini dei touchpad si sommano fino a uno scatto.
- Rotella sopra la barra temporale, a finestra normale e a schermo intero: verso l'alto avanti di 5 s, verso il basso indietro di 5 s (come ↑/↓).
- Opzione `--trace-input`: scrive in `viBro-errors.log` ogni tasto e ogni messaggio della rotella ricevuti da viBro (diagnostica).

## [0.4.0] - non rilasciata separatamente (confluita nella 0.5.0)

Temi, gestione degli errori, ottimizzazione e portabilità.

### Aggiunto
- Temi **Light/Dark** con il tema Fluent integrato in WPF: pulsante *Theme* nella barra del percorso; scelta salvata in `viBro.ini` (`[Appearance] Theme`); al primo avvio si segue l'impostazione di Windows.
- `viBro-errors.log` accanto all'EXE (massimo 1 MB, con un file precedente `.1.log`): eccezioni impreviste, messaggi di errore di mpv, file non apribili, riproduzione interrotta (ad esempio NAS scollegato durante la visione), avvisi dell'INI.
- Riproduzione interrotta da un errore: messaggio nella barra di stato.
- Loader Vulkan ufficiale Khronos incorporato nell'EXE, usato solo se Windows non ne ha uno: viBro parte anche su PC o VM senza driver GPU.
- Cestino anche per i percorsi di 260 o più caratteri (`IFileOperation` di Windows).
- Icona dell'applicazione (EXE e finestra): disegno originale, play e barra temporale su fondo blu.
- Icone vettoriali a colori nell'albero delle cartelle: cartella (aperta quando il nodo è espanso), Desktop, Documents, Downloads, Videos, disco locale, unità di rete, unità rimovibile, unità ottica, condivisione di rete.
- Misura del tempo di avvio (`startup` in `viBro-metrics.csv`, con la dimensione dell'EXE) per confrontare EXE normale e compresso; `build.ps1 -Compress` produce la variante compressa in `artifacts\publish-compressed`.

### Corretto
- Tema Dark: testo nero, illeggibile. Le finestre di viBro (classi derivate da `Window`) non ricevevano il colore del testo dallo stile Fluent, e gli stili propri di righe e nodi lo sostituivano. Ora finestre e barra di stato prendono colori del tema aggiornati a ogni cambio, e gli stili di righe e nodi sono basati su quelli del tema.
- Elenco dei file vuoto di dati dopo la correzione del tema: ogni riga mostrava `ViBro.Util.VideoItem` invece di nome, dimensione e data. Lo stile delle righe era basato sullo stile generico di `ListViewItem` (senza colonne); ora è basato sullo stile delle righe di `GridView` del tema.
- R, Canc, F2 e W andavano a volte premuti due volte: dopo una conferma chiusa e un uso del solo mouse (ad esempio la barra temporale), la pressione successiva dello stesso tasto era scambiata per una ripetizione automatica e ignorata.

### Modificato
- La barra dei controlli (Play, barra temporale, tempo, Mute, volume) è **sopra il video**.
- Righe compatte (circa 24 px invece di circa 40) nell'elenco dei file e nell'albero.
- Barra temporale e volume: cliccando sulla traccia e tenendo premuto, l'indicatore segue subito il mouse (prima serviva un secondo clic).
- A schermo intero: fascia nera in alto con la sola barra temporale, centrata e larga circa il 40% dello schermo; compare muovendo il mouse o con un salto e scompare dopo 2 s di inattività; la fascia resta, quindi il video non cambia dimensione.

## [0.3.0] - 2026-11-02

Operazioni sui file.

### Aggiunto
- **Canc / R — elimina** con conferma, che indica il nome del file e il tipo di eliminazione (Invio = conferma). File locali: Cestino; se Windows non può cestinarli, chiede prima di eliminarli definitivamente e un rifiuto lascia il file dov'è. File di rete e unità mappate: eliminazione definitiva, dichiarata nella conferma.
- **F2 — rinomina** in una piccola finestra: si modifica solo il nome, l'estensione resta; caratteri non validi, nomi riservati e lunghezza vengono controllati; nessuna sovrascrittura (in caso di errore la finestra si riapre con il messaggio). Il file rinominato resta selezionato e viene ricaricato in pausa nella posizione precedente.
- **W — sposta** scegliendo la cartella nella finestra di Windows, aperta sull'ultima destinazione; **Shift+W** usa direttamente l'ultima destinazione, o chiede se non ce n'è una. La destinazione confermata viene salvata subito in `viBro.ini`. Destinazione non raggiungibile: messaggio e possibilità di sceglierne un'altra. Stessa cartella: nessuna operazione. Nome già presente: *Mantieni entrambi* (`nome (2).ext`), *Sovrascrivi* (solo come scelta esplicita) o *Annulla*.
- Spostamento tra volumi diversi: copia in un file temporaneo con avanzamento e annullamento (la finestra compare se dura più di 0,4 s), controllo della dimensione, poi rinomina e solo alla fine eliminazione della sorgente. Stesso volume: rinomina istantanea.
- Prima di ogni operazione il player rilascia il file e si attende che Windows ne chiuda l'handle. Dopo eliminazione o spostamento riusciti viene caricato in pausa il file successivo (o il precedente, se era l'ultimo); se la cartella resta vuota, l'area video si svuota. L'elenco riflette sempre lo stato reale del disco.
- La ripetizione automatica dei tasti e le operazioni concorrenti vengono ignorate.

### Corretto
- `build.ps1` / `package.ps1`: SHA-256 e zip calcolati con .NET anziché con `Get-FileHash` / `Compress-Archive`, che su PC con criteri restrittivi possono non essere disponibili.

## [0.2.0] - 2026-11-02

Interfaccia completa.

### Aggiunto
- Albero delle cartelle a sinistra: cartelle utente (Desktop, Documents, Videos, Downloads), tutte le unità comprese quelle di rete mappate, condivisioni UNC aperte dalla barra del percorso. Sottocartelle elencate in background al primo espandersi del nodo; cartelle nascoste e di sistema escluse; nomi dei volumi letti in background.
- Sincronizzazione: aprendo una cartella dalla barra del percorso, con Browse o all'avvio, l'albero si espande in background fino a quella cartella e la seleziona.
- Ordinamento per Name, Size e Date modified (clic sull'intestazione, nuovo clic inverte), con ordine naturale dei nomi; E/D seguono l'ordine visualizzato.
- Tre riquadri con divisori ridimensionabili.
- `viBro.ini` accanto all'EXE: versione dello schema, finestra (posizione, dimensioni, stato), dimensioni dei riquadri, ultima cartella, ordinamento e larghezze delle colonne, volume e mute, ultima destinazione di spostamento, decodifica hardware, salti esatti, limite della cache, registro delle misure. Salvataggio atomico; valori mancanti o non validi sostituiti dai predefiniti, con avviso; chiavi sconosciute conservate; se il file non è scrivibile, avviso e impostazioni solo in memoria.
- All'avvio si riapre l'ultima cartella, senza caricare alcun video.

### Modificato
- I controlli di prova (Exact long seeks, HW decoding) passano da interfaccia a `viBro.ini`.

## [0.1.0] - 2026-11-02

Prototipo tecnico.

### Aggiunto
- EXE singolo, self-contained, win-x64 (.NET 10, WPF), con libmpv incorporata.
- Video incorporato tramite finestra nativa figlia (opzione `wid` di mpv) e un unico motore riutilizzato fra un file e l'altro.
- Caricamento in pausa con visualizzazione del primo fotogramma; nessun avvio automatico al cambio di selezione.
- Salti di ±1 s, ±5 s, ±1 min e ±5 min, limitati all'intervallo del video, su fotogrammi chiave (predefinito) o esatti; accorpamento delle richieste ripetute.
- E/D per il file precedente/successivo, in pausa e senza ripartire dall'estremità opposta; Spazio, M, F11/Esc, Ctrl+L.
- Barra del percorso (locale, unità mappate, UNC) con lettura della cartella in background; elenco virtualizzato con Name, Size e Date modified in ordine naturale.
- Accelerazione hardware `auto-safe` con ripiego software; selettore per confrontarla con `no`.
- Salti ±1 s e ±5 s sempre esatti, ±1 min / ±5 min su fotogrammi chiave (casella *Exact long seeks* per renderli esatti); colonna `landed_s` nel registro.
- Registro delle misure `viBro-metrics.csv` e script `build/analyze-metrics.ps1`.
- Script di build e confezionamento, workflow GitHub Actions, documentazione di progetto.

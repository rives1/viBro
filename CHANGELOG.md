# Changelog

Formato basato su [Keep a Changelog](https://keepachangelog.com/it/1.1.0/); il progetto usa il [Semantic Versioning](https://semver.org/lang/it/).

## [1.12.0] - 2026-11-11

Versione di solidità, dalla revisione del progetto del 2026-11-11.

### Aggiunto
- **Gallerie più rapide:** tutte le immagini di un video in una sola esecuzione di ffmpeg invece di una per immagine (galleria F12: 2 avvii invece di 11). Misurato su un MP4 di 10 minuti in locale: 450 ms contro 1.000 ms per video. Se un file non lo permette, la riga ripiega da sola sul metodo precedente, e per quell'estensione la galleria non lo riprova. `[Gallery] Method=Combined` (predefinito) o `Separate`.
- **Immagine mancante:** se a una posizione non arriva nessuna immagine (può succedere vicino alla fine, es. al 99 %, nei formati in cui ffmpeg cerca il fotogramma chiave successivo), viene ripresa 10 s e poi 30 s prima.
- **Tastiera nelle gallerie (F10, F12):** sulla riga selezionata agiscono i tasti di elimina (con conferma, come Canc nell'elenco), rinomina, sposta, copia, Play/Pause e Mute, presi da `[Keys]`. Frecce, PgUp/PgDn e Home/Fine restano per spostarsi tra le righe. Ctrl+F2 ha la sezione *Galleries*.
- `docs/TEST-REGRESSION.md`: lista unica delle prove da ripetere prima di ogni rilascio; i piani delle versioni contengono solo le novità.

### Modificato
- **`[Diagnostics] MetricsLog` predefinito `false`:** `viBro-metrics.csv` non viene più scritto se non lo si attiva. Un `viBro.ini` già esistente conserva il valore scritto dalle versioni precedenti (`true`): va cambiato a mano. Oltre 10 MB il file diventa `viBro-metrics-old.csv`; l'intestazione si controlla una volta per sessione e non a ogni riga.
- **File coinvolti in un'eliminazione o in uno spostamento:** finché l'operazione è in corso non si possono caricare (prima un clic su uno di essi lo apriva nel player e faceva fallire l'operazione su quel file).
- `.gitignore`: esclusi anche `*.avi`, `*.m4v`, `*.ts` e `viBro-metrics*.csv`.

## [1.11.0] - 2026-11-10

### Aggiunto
- **F12: galleria dei video selezionati.** Con uno o più video selezionati nell'elenco, F12 apre una finestra come la galleria con una striscia per video, nell'ordine dell'elenco, e 10 immagini prese al 10, 20 … 90 % e al 99 % della durata. Strisce compatte, separate da 1 pixel; senza tempi e senza riga del nome (il nome compare nel tooltip della striscia). Funzioni come nella galleria F10: clic su una striscia = seleziona e carica il video, clic su un'immagine = lo carica a quel tempo, tasto destro = menu Delete / Move / Copy; le strisce dei file eliminati o spostati spariscono. Senza video selezionati, un avviso nella barra di stato. F12 di nuovo crea una nuova galleria; può restare aperta insieme a quella di F10.
- `viBro.ini`: `[SelectionGallery] PositionsPercent=10,20,30,40,50,60,70,80,90,99` (da 1 a 20 valori); `[Keys] SelectionGallery=F12`.
- Ctrl+F2 mostra F12.

## [1.10.0] - 2026-11-09

### Aggiunto
- **Scorciatoie da tastiera configurabili** nella sezione `[Keys]` di `viBro.ini`: una riga per ognuno dei 31 comandi della finestra principale (es. `Delete=Delete,R`, `MoveToLast=Shift+W`, `SeekForwardVeryLong=Alt+PageUp`), più tasti separati da virgole, `none` per nessun tasto. Alla chiusura viBro scrive tutte le righe con i tasti in uso. Valore non valido: avviso e predefinito; tasto assegnato a due comandi: avviso, resta al comando che viene prima nell'elenco. Ctrl+F2 mostra i tasti configurati. Mouse e rotella non cambiano.

### Modificato
- I modificatori devono corrispondere esattamente: per esempio Shift+← e Shift+PgUp non saltano più (prima Shift veniva ignorato su frecce, PgUp/PgDn, E/D, Spazio, M, F11, Canc/R e F2).
- Il comando *Close* (Ctrl+W) funziona anche mentre si scrive nella barra del percorso con il tasto configurato.

## [1.9.0] - 2026-11-07

### Aggiunto
- **Tasto destro tenuto + rotella sul video:** cambia video come la rotella sulla barra temporale (su = successivo, giù = precedente). Al rilascio del tasto il menu non compare e non avviene il salto del trascinamento.
- **File M4V** (`.m4v`) nell'elenco, con tutte le funzioni. L'estratto di un `.m4v` viene scritto in contenitore MP4 (`-f mp4`).

### Modificato
- La rotella su video e barra temporale funziona **anche quando viBro non è la finestra in primo piano** (se Windows manda la rotella alla finestra sotto il puntatore, impostazione predefinita di Windows 11), non con una finestra di dialogo di viBro aperta.
- **Rotella sulla barra temporale invertita:** su = video successivo, giù = video precedente.
- Ctrl+F2 aggiornato.

## [1.8.0] - 2026-11-06

### Aggiunto
- **F5:** aggiorna dal disco l'albero delle cartelle e l'elenco dei file. Nell'elenco compaiono file e cartelle nuovi, spariscono quelli eliminati, dimensioni e date si aggiornano; restano selezione, video caricato, marker dell'estratto, ordinamento e posizione di scorrimento. Se il video caricato non c'è più, viene scaricato. Nell'albero si aggiornano le cartelle già aperte (le altre si leggono comunque all'apertura), compaiono le unità nuove (es. chiavetta USB) e spariscono quelle scollegate; i rami aperti restano aperti. Una condivisione che non risponde entro 10 s resta com'era. Barra di stato: `Refreshed <cartella>: 2 new, 1 removed`.
- **Tasto destro nella galleria (F10):** su una riga o un'immagine seleziona e carica il video, come il clic sinistro, e apre lo stesso menu del tasto destro sul video: *Delete*, *Move...*, *Move to <ultima cartella>*, *Copy...*, *Copy to <ultima cartella>*. *Delete* segue la stessa regola del menu del video (nessuna conferma per un file, `[Delete] ConfirmFromVideoMenu`). Se la finestra principale mostra un'altra cartella, viene riaperta quella della galleria.
- Le righe della galleria dei file eliminati o spostati da viBro (da qualunque comando) o spariti con F5 escono dalla galleria; con la copia non cambia nulla.
- **File AVI** (`.avi`) nell'elenco: riproduzione, salti, elimina, rinomina, sposta, copia, estratto (F9) e galleria (F10). Su AVI vecchi con indice incompleto i salti possono essere più lenti o meno precisi.

### Modificato
- *Delete* nel menu del tasto destro sul video elimina **un solo file senza conferma**, anche in rete (eliminazione definitiva). Con più file selezionati la conferma resta. Canc / R chiedono sempre conferma. Nuova impostazione `[Delete] ConfirmFromVideoMenu` (predefinita `false`; `true` = conferma sempre).
- Ctrl+F2 mostra F5 e la regola del menu.

## [1.7.0] - 2026-11-05

### Aggiunto
- **Selezione multipla nell'elenco** come in Windows: Ctrl + clic aggiunge o toglie un file, Shift + clic seleziona un intervallo, **Ctrl+A** seleziona tutti i video (non le cartelle). Il riquadro video carica il file appena cliccato; un Ctrl + clic che toglie un file non cambia il video caricato. Barra di stato: `3 selected (4.2 GB) · 128 videos · 412.5 GB`.
- **Elimina** (Canc / R), **sposta** (W / Shift+W) e **copia** (C / Shift+C), anche dal menu del tasto destro (`Delete 3 files` …), agiscono su tutti i video selezionati:
  - elimina: una sola conferma con il numero di file e i primi nomi (Cestino se locale, definitiva in rete);
  - sposta / copia: una sola destinazione; i conflitti di nome si chiedono prima di iniziare, file per file, con *Skip* e la casella *Do the same for the next conflicts*; una finestra di avanzamento per tutto il trasferimento (`file 3 of 12`, barra per dimensione); annullando, i file già completati restano, quello in corso resta com'era;
  - un errore su un file non ferma gli altri: alla fine un messaggio elenca i file non riusciti (anche in `viBro-errors.log`);
  - dopo elimina o sposta viene caricato il video che seguiva l'ultimo rimosso (o il precedente). Se il video caricato non era tra i file, resta caricato e continua.
  - la copia non interrompe il video in riproduzione.
- Ctrl+F2 descrive la selezione multipla e Ctrl+A.

### Modificato
- Con più video selezionati, F2 (rinomina), Shift+Invio e I / O / F9 (estratto) non fanno nulla e lo dicono nella barra di stato: agiscono su un solo file.
- D / E, la rotella sulla barra temporale e la galleria (F10) partono dal video caricato e lasciano selezionato un solo file.

## [1.6.0] - 2026-11-04

### Aggiunto
- **Galleria (F10):** finestra separata con una riga per ogni video della cartella (nell'ordine dell'elenco): nome, durata e 5 immagini prese alle stesse percentuali della durata di ciascun video, predefinite 10, 30, 50, 70, 90 %, con il tempo sotto ogni immagine. Un clic su una riga (o le frecce) seleziona e carica il video nella finestra principale, con Play/Pause conservato; un clic su un'immagine lo carica e va al tempo dell'immagine. Esc chiude la galleria.
- Le immagini sono i fotogrammi chiave più vicini alle posizioni (rapide anche sul NAS; il tempo indicato è quello reale del fotogramma). Vengono create in background con lo stesso `ffmpeg.exe` dell'estratto, partendo dalle righe visibili; chiudendo la finestra tutto si ferma. Restano solo in memoria (nulla scritto su disco) e vengono riusate finché viBro è aperto, se il file non è cambiato (fino a 64 MB).
- La galleria resta sulla cartella in cui è stata aperta: se nel frattempo la finestra principale ne mostra un'altra, il clic riapre la cartella della galleria. F10 di nuovo crea una nuova galleria della cartella corrente.
- `viBro.ini`, sezione `[Gallery]`: `PositionsPercent=10,30,50,70,90` (da 1 a 10 valori tra 0 e 100) e `Jobs=2` (esecuzioni di ffmpeg contemporanee, 1–8).
- Ctrl+F2 mostra F10, e se `ffmpeg.exe` manca.
- **Release su GitHub automatiche:** al push di un tag `v*`, dopo la compilazione di verifica, il workflow crea la release con le note della versione prese da questo CHANGELOG. Solo note e archivi del sorgente generati da GitHub: nessun EXE (D-052). Per i tag precedenti si può avviare a mano (Actions → build → Run workflow). Istruzioni in `docs/RELEASE.md`.

## [1.5.0] - 2026-11-04

### Aggiunto
- Estratto di una parte del video: **I** segna l'inizio e **O** la fine nella posizione corrente del file caricato (barra di stato `In 00:01:23.4  ·  Out 00:02:10.0`; a schermo intero anche sopra il video); **F9** salva la parte in un nuovo file nella stessa cartella del video, `nome-extract-aaMMgg-OOmm.ext` (es. `clip.mp4` → `clip-extract-261104-1445.mp4`; se il nome esiste già, `-2`, `-3` …). Il nuovo file compare nell'elenco; la selezione e il video caricato non cambiano. I marker valgono solo per il file su cui sono stati messi.
- Copia dei dati senza ricodifica (rapida, qualità originale): l'estratto parte dal **fotogramma chiave al marker di inizio o prima** (anche diversi secondi prima); la fine è vicina al marker di fine. Tracce video e audio; in MKV anche i sottotitoli.
- Serve un `ffmpeg.exe` messo dall'utente nella cartella di `viBro.exe` (non incluso): senza, I/O/F9 mostrano un avviso e non fanno nulla. Il controllo avviene a ogni uso: non serve riavviare viBro dopo averlo aggiunto.
- Finestra di avanzamento con annullamento se l'estrazione dura più di 0,4 s; annullando o in caso di errore il file parziale viene cancellato. Errori di ffmpeg in un messaggio e in `viBro-errors.log`.
- Ctrl+F2 mostra I / O e F9, e se `ffmpeg.exe` manca.
- Marker sulla barra temporale: linea verde in I, rossa in O e fascia gialla tra i due, solo per il file su cui sono stati messi (anche a schermo intero).
- A estrazione completata, un messaggio conferma il successo con nome, dimensione, intervallo e cartella del nuovo file.

## [1.4.0] - 2026-11-04

### Aggiunto
- Sezione `[Seek]` in `viBro.ini`: durata in secondi di ogni salto, con i valori di prima come predefiniti: `LeftRightSeconds=1` (← / →), `UpDownSeconds=5` (↓ / ↑), `WheelSeconds=3` (rotella sopra il video), `PageSeconds=60` (PgDown / PgUp), `AltPageSeconds=300` (Alt + PgDown / PgUp). Valori ammessi 0,1–3600; un valore non valido torna al predefinito con avviso.
- La finestra dei comandi (Ctrl+F2) mostra le durate in uso.

### Invariato
- `[Diagnostics] MetricsLog` (predefinito `true`) continua a decidere se generare `viBro-metrics.csv`; documentato meglio.

## [1.3.0] - 2026-11-04

### Aggiunto
- Sottocartelle nell'elenco: in cima `..` (cartella superiore, assente nella radice di un'unità o di una condivisione), poi le cartelle con l'icona, poi i video. Doppio clic o Invio aprono la cartella (l'albero si sincronizza); tornando con `..` resta selezionata la cartella da cui si è usciti. Un clic su una cartella non carica nulla: il video corrente resta. D/E e la rotella saltano le cartelle; elimina, rinomina, sposta, copia e Shift+Invio valgono solo per i video. Barra di stato: `3 folders · 12 / 128 videos · 412.5 GB`. Le cartelle arrivano dalla stessa lettura della cartella: nessun accesso in più al disco o al NAS.
- Shift+Invio: apre il video selezionato con l'applicazione predefinita di Windows; viBro va in pausa.

### Modificato
- Rotella (in entrambe le modalità): sopra la barra temporale cambia video (giù = successivo, su = precedente); sopra il video, e a schermo intero ovunque tranne la barra, sposta il tempo di 3 s (su = avanti). Prima la barra spostava il tempo e a schermo intero il video cambiava file.

## [1.2.0] - 2026-11-03

Aspetto e indicazioni. Comprende anche le modifiche della 1.1.0, non rilasciata separatamente.

### Aggiunto
- Pulsanti a icone (Segoe Fluent Icons di Windows 11): Play/Pause, Mute (l'icona indica lo stato), Browse, Theme (sole/luna); tooltip con il tasto corrispondente.
- Tempo sotto il puntatore sulla barra temporale (es. `00:42:17`), senza anteprima dell'immagine.
- File caricato evidenziato nell'elenco: icona ▶ (in riproduzione) o ❚❚ (in pausa) davanti al nome e nome in grassetto, indipendente dalla selezione.
- Posizione nella cartella nella barra di stato: `12 / 128 videos · 412.5 GB`.
- A schermo intero: tempo sovrapposto al video (OSD di mpv) per circa 1 s dopo un salto, e nome del file per 0,8 s al cambio di file.
- Ctrl+F2: finestra *Commands* con i comandi di tastiera e mouse.

## [1.1.0] - non rilasciata separatamente (confluita nella 1.2.0)

### Aggiunto
- Icona della cartella davanti alla barra del percorso (decorativa, la stessa dell'albero).

### Modificato
- A finestra normale la barra dei controlli (Play, barra temporale, tempo, Mute, volume) è **sotto il video e sopra l'elenco dei file** (dalla 0.4.0 era sopra il video). Il divisore ridimensiona video e controlli insieme rispetto all'elenco. A schermo intero non cambia: fascia sotto il video.
- Rotella sopra il video a finestra normale: sposta il tempo di 3 s per scatto (su = avanti), come sopra la barra temporale; prima cambiava video. A schermo intero continua a cambiare video (tranne sopra la barra temporale, dove sposta di 3 s).
- Rinomina (F2): il cursore parte dall'inizio del nome, senza selezione (prima il nome era tutto selezionato).

## [1.0.0] - 2026-11-03

Prima versione completa e verificata. Nessuna funzione nuova rispetto alla 0.6.0.

### Modificato
- Autore e copyright: `rives1` (proprietà dell'EXE e commit).
- README in inglese (`README.md`), versione italiana in `README.it.md`.
- Distribuzione solo come sorgente: il workflow GitHub compila come verifica ma non pubblica l'EXE (vedi THIRD-PARTY-NOTICES).
- Avvisi di licenza corretti: la build di libmpv inclusa è GPL-3.0 (FFmpeg `--enable-version3`).

### Rimosso
- Percorsi personali dai documenti del repository.

## [0.6.0] - 2026-11-03

### Aggiunto
- Copia del file: tasti C / Shift+C e voci *Copy...* / *Copy to <cartella>* nel menu del tasto destro. Stesse regole dello spostamento (cartella raggiungibile, stessa cartella rifiutata, conflitto di nome con *Keep both* / *Overwrite*, avanzamento e annullamento, copia verificata in un file temporaneo). L'originale resta e il video continua a essere riprodotto. Ultima destinazione di copia separata (`[Folders] LastCopyDestination`).
- Tasto destro tenuto premuto sul video e trascinamento orizzontale: spostamento nel video (tutta la larghezza = tutta la durata), con il video che segue il mouse e salto esatto al rilascio. Un clic destro senza trascinamento apre il menu come prima.
- Barra di stato: numero di video nella cartella e spazio totale (es. `128 videos · 412.5 GB`).
- Ctrl+W chiude viBro (anche mentre si scrive nella barra del percorso).

### Modificato
- Lo stato Play/Pause si conserva al cambio di file: se il video è in riproduzione, il successivo (E/D, rotella, clic nell'elenco, dopo elimina/rinomina/sposta) parte in riproduzione; se è in pausa resta in pausa. Prima ogni file si caricava sempre in pausa.
- Rotella sopra il video anche a finestra normale: giù video successivo, su precedente. Sopra elenco e albero continua a scorrere la lista.
- Rotella sopra la barra temporale: 3 s per scatto (prima 5 s).
- Schermo intero: la fascia con la barra temporale è **sotto** il video (prima sopra).
- Barra di stato più bassa (poco più alta del testo).
- Conferma di eliminazione: finestra di viBro (tema Light/Dark) che si apre vicino al puntatore del mouse, sia dal menu sia con Canc/R. Invio = *Delete*, Esc = *Cancel*.

## [0.5.0] - 2026-11-03

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

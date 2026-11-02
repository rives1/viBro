# Changelog

Formato basato su [Keep a Changelog](https://keepachangelog.com/it/1.1.0/); il progetto usa il [Semantic Versioning](https://semver.org/lang/it/).

## [0.3.0] - non ancora rilasciata

Operazioni sui file.

### Aggiunto
- **Canc / R — elimina** con conferma, che indica il nome del file e il tipo di eliminazione (Invio = conferma). File locali: Cestino; se Windows non può cestinarli, chiede prima di eliminarli definitivamente e un rifiuto lascia il file dov'è. File di rete e unità mappate: eliminazione definitiva, dichiarata nella conferma.
- **F2 — rinomina** in una piccola finestra: si modifica solo il nome, l'estensione resta; caratteri non validi, nomi riservati e lunghezza vengono controllati; nessuna sovrascrittura (in caso di errore la finestra si riapre con il messaggio). Il file rinominato resta selezionato e viene ricaricato in pausa nella posizione precedente.
- **W — sposta** scegliendo la cartella nella finestra di Windows, aperta sull'ultima destinazione; **Shift+W** usa direttamente l'ultima destinazione, o chiede se non ce n'è una. La destinazione confermata viene salvata subito in `viBro.ini`. Destinazione non raggiungibile: messaggio e possibilità di sceglierne un'altra. Stessa cartella: nessuna operazione. Nome già presente: *Mantieni entrambi* (`nome (2).ext`), *Sovrascrivi* (solo come scelta esplicita) o *Annulla*.
- Spostamento tra volumi diversi: copia in un file temporaneo con avanzamento e annullamento (la finestra compare se dura più di 0,4 s), controllo della dimensione, poi rinomina e solo alla fine eliminazione della sorgente. Stesso volume: rinomina istantanea.
- Prima di ogni operazione il player rilascia il file e si attende che Windows ne chiuda l'handle. Dopo eliminazione o spostamento riusciti viene caricato in pausa il file successivo (o il precedente, se era l'ultimo); se la cartella resta vuota, l'area video si svuota. L'elenco riflette sempre lo stato reale del disco.
- La ripetizione automatica dei tasti e le operazioni concorrenti vengono ignorate.

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

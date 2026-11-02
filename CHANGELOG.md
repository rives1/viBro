# Changelog

Formato basato su [Keep a Changelog](https://keepachangelog.com/it/1.1.0/); il progetto usa il [Semantic Versioning](https://semver.org/lang/it/).

## [0.2.0] - non ancora rilasciata

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

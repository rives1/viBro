# Changelog

Formato basato su [Keep a Changelog](https://keepachangelog.com/it/1.1.0/); il progetto usa il [Semantic Versioning](https://semver.org/lang/it/).

## [0.1.0] - non ancora rilasciata

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

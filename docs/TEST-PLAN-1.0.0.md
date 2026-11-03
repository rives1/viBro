# Piano di prova 1.0.0

Prima versione completa e verificata. Nessuna funzione nuova rispetto alla 0.6.0: regressione di tutte le funzioni e sessione lunga per la memoria.
Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.0.0 (…)`; nelle proprietà dell'EXE (Dettagli): versione 1.0.0, copyright "Copyright (C) 2026 rives1".
Prima della sessione di memoria, cancellare (o rinominare) `viBro-metrics.csv` accanto all'EXE, così il file contiene solo questa prova.

## Avvio e impostazioni
- [ ] Avvio senza `viBro.ini`: valori predefiniti, tema come Windows, nessun video caricato.
- [ ] Chiusura (anche con Ctrl+W) e riapertura: finestra, riquadri, colonne, ordinamento, tema, volume, mute, ultima cartella ripristinati; nessun video caricato automaticamente.

## Cartelle ed elenco
- [ ] Barra del percorso con cartella locale, unità mappata e percorso UNC; una condivisione irraggiungibile non blocca l'interfaccia.
- [ ] Albero: icone, caricamento progressivo, sincronizzazione con la cartella aperta.
- [ ] Elenco: solo MP4, MKV, MOV, WMV; ordinamento per Name (naturale), Size, Date modified; righe compatte; barra di stato con "N videos · totale".

## Riproduzione e navigazione
- [ ] Un file per formato: MP4, MKV, MOV e **WMV**; apertura, salti e cambio video funzionano.
- [ ] Salti ←/→, ↓/↑, PgDown/PgUp, Alt+PgDown/PgUp in pausa (resta in pausa) e in riproduzione (continua); limiti di inizio e fine.
- [ ] E/D e rotella sopra il video: cambio video con Play/Pause conservato; nessun ritorno all'altra estremità.
- [ ] Barra temporale: clic e trascinamento; rotella ±3 s. Tasto destro tenuto + trascinamento sul video.
- [ ] Clic (Play/Pause), doppio clic (schermo intero), F11/Esc; a schermo intero fascia in basso che si nasconde; rotella ovunque cambia video.
- [ ] Volume, mute (M), Space; alla fine del video si ferma senza passare al successivo.

## Operazioni sui file
- [ ] Elimina (Canc/R e menu): conferma vicino al puntatore, Cestino in locale, definitiva in rete; poi il file successivo.
- [ ] Rinomina (F2), sposta (W, Shift+W e menu), copia (C, Shift+C e menu): conflitti, avanzamento, annullamento; l'elenco riflette l'esito reale.
- [ ] Un tasto tenuto premuto non ripete l'operazione; R/Canc/F2/W/C rispondono alla prima pressione.

## Temi ed errori
- [ ] Light/Dark: testi leggibili, dialoghi e menu nel tema scelto.
- [ ] File non valido: messaggio nella barra di stato e riga in `viBro-errors.log`.

## Memoria (sessione lunga)
- [ ] In un'unica sessione, almeno **50 cambi di video** (D/E, rotella o clic) alternando file 1080p e 4K, con qualche salto e qualche minuto di riproduzione; poi chiudere viBro e inviare `viBro-metrics.csv`.
  Criterio: la memoria (colonne `working_set_mb` e `private_mb`) varia con il file aperto ma non cresce in modo continuo; a parità di tipo di file, i valori degli ultimi cambi sono vicini a quelli dei primi (indicativamente entro 100 MB).

## Portabilità
- [ ] Il workflow GitHub dopo il push termina con successo e non pubblica artifact né release.

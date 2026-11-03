# Piano di prova 1.0.0

Prima versione completa e verificata. Nessuna funzione nuova rispetto alla 0.6.0: regressione di tutte le funzioni e sessione lunga per la memoria.
Il piano passa alla versione successiva finché tutte le prove non sono confermate.

**Esito (2026-11-03, utente, Windows 11, EXE compilato da `7a7d0d5`):** tutte le prove superate.
Sessione di memoria (`viBro-metrics.csv`, 38 minuti, 90 caricamenti, 4292 salti, nessun timeout): memoria di lavoro mediana 333 MB nei primi 10 caricamenti e 325 MB negli ultimi 10 (massimo 434 MB); memoria privata 364 → 400 MB (massimo 531 MB); memoria gestita .NET 9–25 MB. Nessuna crescita continua: criterio rispettato. Apertura/cambio mediana 86 ms (p90 140); salti rapidi 17 ms (p90 44), esatti 43 ms (p90 136). La sessione conteneva MKV HEVC e MP4 H.264 da 540p a 1080p ma nessun file 4K: per il 4K vale la sessione più breve della 0.6.0 (D-049), anch'essa senza crescita continua.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.0.0 (…)`; nelle proprietà dell'EXE (Dettagli): versione 1.0.0, copyright "Copyright (C) 2026 rives1".
Prima della sessione di memoria, cancellare (o rinominare) `viBro-metrics.csv` accanto all'EXE, così il file contiene solo questa prova.

## Avvio e impostazioni
- [x] Avvio senza `viBro.ini`: valori predefiniti, tema come Windows, nessun video caricato.
- [x] Chiusura (anche con Ctrl+W) e riapertura: finestra, riquadri, colonne, ordinamento, tema, volume, mute, ultima cartella ripristinati; nessun video caricato automaticamente.

## Cartelle ed elenco
- [x] Barra del percorso con cartella locale, unità mappata e percorso UNC; una condivisione irraggiungibile non blocca l'interfaccia.
- [x] Albero: icone, caricamento progressivo, sincronizzazione con la cartella aperta.
- [x] Elenco: solo MP4, MKV, MOV, WMV; ordinamento per Name (naturale), Size, Date modified; righe compatte; barra di stato con "N videos · totale".

## Riproduzione e navigazione
- [x] Un file per formato: MP4, MKV, MOV e **WMV**; apertura, salti e cambio video funzionano.
- [x] Salti ←/→, ↓/↑, PgDown/PgUp, Alt+PgDown/PgUp in pausa (resta in pausa) e in riproduzione (continua); limiti di inizio e fine.
- [x] E/D e rotella sopra il video: cambio video con Play/Pause conservato; nessun ritorno all'altra estremità.
- [x] Barra temporale: clic e trascinamento; rotella ±3 s. Tasto destro tenuto + trascinamento sul video.
- [x] Clic (Play/Pause), doppio clic (schermo intero), F11/Esc; a schermo intero fascia in basso che si nasconde; rotella ovunque cambia video.
- [x] Volume, mute (M), Space; alla fine del video si ferma senza passare al successivo.

## Operazioni sui file
- [x] Elimina (Canc/R e menu): conferma vicino al puntatore, Cestino in locale, definitiva in rete; poi il file successivo.
- [x] Rinomina (F2), sposta (W, Shift+W e menu), copia (C, Shift+C e menu): conflitti, avanzamento, annullamento; l'elenco riflette l'esito reale.
- [x] Un tasto tenuto premuto non ripete l'operazione; R/Canc/F2/W/C rispondono alla prima pressione.

## Temi ed errori
- [x] Light/Dark: testi leggibili, dialoghi e menu nel tema scelto.
- [x] File non valido: messaggio nella barra di stato e riga in `viBro-errors.log`.

## Memoria (sessione lunga)
- [x] In un'unica sessione, almeno **50 cambi di video** (D/E, rotella o clic) alternando file 1080p e 4K, con qualche salto e qualche minuto di riproduzione; poi chiudere viBro e inviare `viBro-metrics.csv`.
  Criterio: la memoria (colonne `working_set_mb` e `private_mb`) varia con il file aperto ma non cresce in modo continuo; a parità di tipo di file, i valori degli ultimi cambi sono vicini a quelli dei primi (indicativamente entro 100 MB).

## Portabilità
- [x] Il workflow GitHub dopo il push termina con successo e non pubblica artifact né release.

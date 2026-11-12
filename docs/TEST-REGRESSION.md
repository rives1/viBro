# Regressione di viBro

Lista unica delle funzioni da riprovare prima di ogni rilascio (dalla 1.12.0, D-078). Ogni versione ha in più il suo piano breve `TEST-PLAN-x.y.z.md` con le sole novità; questa lista si aggiorna quando una funzione cambia o se ne aggiunge una. Prove rapide: pochi secondi ciascuna. Usare copie dei video per elimina, sposta e rinomina. Serve `ffmpeg.exe` accanto a `viBro.exe` per estratto e gallerie.

## Avvio e impostazioni
- [ ] Avvio: titolo `viBro x.y.z (…)`, nessun video caricato, ultima cartella riaperta, tema e finestra come alla chiusura.
- [ ] `viBro.ini` riscritto alla chiusura; un valore non valido dà un avviso nella barra di stato e il predefinito.
- [ ] Tema Light / Dark con il pulsante; le finestre (dialoghi, gallerie) seguono il tema.

## Cartelle ed elenco
- [ ] Albero: unità e cartelle, apertura progressiva; una condivisione irraggiungibile non blocca.
- [ ] Barra del percorso a briciole: clic su una cartella del percorso; clic sulla parte vuota o Ctrl+L per scrivere un percorso (locale, unità di rete, `\\server\share`), Invio / Esc; Backspace = cartella superiore.
- [ ] Elenco: `..`, cartelle, video (MP4, MKV, MOV, WMV, AVI, M4V); ordinamento per Name / Size / Date in entrambi i versi; Invio o doppio clic aprono una cartella.
- [ ] Selezione: clic, Ctrl + clic, Shift + clic, Ctrl+A; barra di stato `… selected (… GB)` e `12 / 128 videos`.
- [ ] Filtro (Ctrl+F): `4K`, `.mp4`, `>1G` restringono l'elenco con le parti trovate evidenziate; con il filtro D / E, Ctrl+A ed F10 lavorano sui soli video visibili; Esc svuota; un'altra cartella si apre senza filtro.
- [ ] F5 aggiorna albero ed elenco senza perdere selezione, video caricato e posizione di scorrimento; righe nuove e scomparse in dissolvenza (con gli *Effetti di animazione* di Windows attivi).

## Riproduzione e salti
- [ ] Clic su un video: carica in pausa sul primo fotogramma (o in riproduzione se il precedente suonava); D / E, rotella sulla barra (su = successivo).
- [ ] Spazio / clic sul video = Play/Pause; M; volume con lo slider e con + / - (valore sopra il video); doppio clic e F11 = schermo intero, Esc per uscire; tempo e nome sopra il video a schermo intero; puntatore nascosto dopo 2 s fermo.
- [ ] Senza video caricato il riquadro mostra la guida; tempi ovunque nel formato `1:20` / `1:02:15`.
- [ ] Salti: ← / →, ↓ / ↑, PgUp / PgDn, Alt + PgUp / PgDn con le durate di `[Seek]`; rotella sul video ±3 s, anche con viBro non in primo piano; barra temporale (clic, trascinamento, tempo sotto il puntatore).
- [ ] Tasto destro trascinato sul video = spostamento nel tempo; tasto destro + rotella = cambio video, senza menu.
- [ ] Shift+Invio apre il video con l'applicazione di Windows.

## Operazioni sui file
- [ ] Elimina (Canc / R, conferma; un file dal menu del video senza conferma; più file con conferma); Cestino in locale, definitiva in rete.
- [ ] Rinomina (F2, un solo file); sposta (W / Shift+W) e copia (C / Shift+C) di uno e più file, con un conflitto (Keep both / Overwrite / Skip / Annulla) e un trasferimento lungo con avanzamento e annullamento.
- [ ] Durante un'eliminazione o uno spostamento i file coinvolti non si caricano (avviso); dopo, viene caricato il video successivo.
- [ ] Menu del tasto destro sul video: Delete, Move, Move to, Copy, Copy to.

## Estratto e gallerie
- [ ] I / O / F9: estratto nella stessa cartella con conferma; marker sulla barra.
- [ ] F10: galleria della cartella (5 immagini con il tempo dentro l'immagine), apertura e chiusura in dissolvenza (con gli *Effetti di animazione* di Windows attivi); posizione e dimensione delle gallerie ricordate; F12: galleria della selezione (10 immagini, strisce compatte, nome nel tooltip).
- [ ] Nelle gallerie: riga del video caricato con bordo colorato (segue D / E, in dissolvenza); clic = carica, clic su immagine = carica a quel tempo, tasto destro = menu, Canc / F2 / W / C / Spazio / M sulla riga selezionata, frecce tra le righe, Esc chiude; le righe dei file eliminati o spostati spariscono.

## Tastiera e comandi
- [ ] Ctrl+F2 mostra tutti i comandi con i tasti in uso; Ctrl+W chiude viBro (anche nella barra del percorso).
- [ ] Un tasto cambiato in `[Keys]` funziona e il vecchio no.

## Barra di stato e diagnostica
- [ ] Barra di stato: riepilogo a sinistra, messaggi al centro (errori arancio, esiti verdi, fermi 6 s; clic = ultimi 20), formato del video a destra.
- [ ] `viBro-errors.log` riceve gli errori (es. file non riproducibile); con `[Diagnostics] MetricsLog=true` viene scritto `viBro-metrics.csv` e la barra di stato mostra le misure.

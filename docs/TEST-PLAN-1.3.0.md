# Piano di prova 1.3.0

Prove delle modifiche della 1.3.0 e regressione rapida. Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.3.0 (…)`.

## Sottocartelle nell'elenco
- [ ] Una cartella con sottocartelle e video: in cima `..`, poi le cartelle (icona gialla, colonna Size vuota, data presente), poi i video. Ordinando per Name, Size e Date (in entrambe le direzioni) `..` e le cartelle restano sempre sopra i video.
- [ ] Nella radice di un'unità (es. `K:\`) o di una condivisione (`\\server\share`) non c'è `..`.
- [ ] Clic su una cartella: nessun video caricato, il video corrente resta (e continua se suonava).
- [ ] Doppio clic su una cartella, oppure selezione e Invio: si apre; barra del percorso e albero si aggiornano.
- [ ] `..` (doppio clic o Invio): si torna alla cartella superiore con selezionata la cartella da cui si è usciti, senza caricare nulla.
- [ ] D/E e rotella sulla barra temporale saltano le cartelle e `..`; ai limiti dell'elenco non si ricomincia dall'altra parte.
- [ ] Con una cartella selezionata: Canc/R, F2, W, C, Shift+Invio e le voci del menu del tasto destro non agiscono sulla cartella (menu con voci disattivate).
- [ ] Eliminando o spostando l'ultimo video, viene proposto il video precedente (non una cartella); se non restano video, il riquadro video si svuota.
- [ ] Barra di stato: `3 folders · 12 / 128 videos · 412.5 GB` (senza la parte delle cartelle se non ce ne sono); la dimensione conta solo i video.
- [ ] Cartella grande sul NAS: l'apertura non è più lenta di prima; una condivisione irraggiungibile non blocca l'interfaccia.

## Shift+Invio
- [ ] Con un video selezionato, Shift+Invio lo apre nell'applicazione predefinita di Windows (es. Lettore multimediale); viBro va in pausa se stava suonando; la barra di stato riporta "Opened …".
- [ ] Funziona anche con lo stato attivo sul video (dopo un clic sul video) e a schermo intero.
- [ ] Se non esiste un'applicazione associata, messaggio nella barra di stato e riga in `viBro-errors.log`; viBro non si blocca.

## Rotella
- [ ] Finestra normale e schermo intero, rotella sopra la barra temporale: giù = video successivo, su = precedente; il tempo sotto il puntatore continua a comparire.
- [ ] Rotella sopra il video: su = avanti 3 s, giù = indietro 3 s, in entrambe le modalità; a schermo intero il tempo compare sopra il video.
- [ ] Finestra normale, rotella sopra elenco e albero: scorrimento della lista come prima.
- [ ] Ctrl+F2: la finestra dei comandi descrive Invio, Shift+Invio, doppio clic sulle cartelle e la nuova rotella.

## Regressione
- [ ] Clic, doppio clic, menu e trascinamento col tasto destro sul video, D/E con Play/Pause conservato, elimina/rinomina/sposta/copia di video, temi, icone dei pulsanti, file caricato evidenziato, Ctrl+W.

# Piano di prova 1.7.0

Prove delle modifiche della 1.7.0, prove rimaste aperte della 1.6.0 e regressione rapida. Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.7.0 (…)`. Preparare una cartella di prova con almeno 8 video copiati (non originali), sia locale sia sul NAS.

**Esito (2026-11-05, utente, Windows 11, EXE compilato da `b69f059`):** tutte le prove superate, comprese quelle della release su GitHub rimaste aperte dalla 1.6.0 (commit e tag `v1.6.0` pubblicati). Nessuna prova passa alla versione successiva.

## Selezione
- [x] Clic su un video: selezionato e caricato, come prima.
- [x] Ctrl + clic su altri video: si aggiungono alla selezione e ogni video cliccato viene caricato (Play/Pause conservato).
- [x] Ctrl + clic su un video già selezionato: viene tolto dalla selezione e il video caricato non cambia.
- [x] Shift + clic: selezionato l'intervallo dal primo clic al file cliccato, che viene caricato.
- [x] Ctrl+A: selezionati tutti i video, non le cartelle né `..`; il video caricato non cambia. Con una cartella di molti video (centinaia) la selezione è immediata.
- [x] Clic senza tasti su un video già compreso in una selezione multipla: resta selezionato solo quello, che viene caricato.
- [x] Barra di stato: con un file `12 / 128 videos · …`; con più file `3 selected (4.2 GB) · 128 videos · …`.
- [x] D / E e rotella sulla barra temporale con più file selezionati: passano al video prima/dopo quello caricato e resta selezionato un solo file.
- [x] Ordinando per colonna con più file selezionati la selezione resta.
- [x] Ctrl+F2 descrive Ctrl + clic / Shift + clic e Ctrl+A.

## Comandi su un solo file (con più video selezionati)
- [x] F2, Shift+Invio, I, O, F9: nessuna azione, messaggio nella barra di stato `… works on one file: N videos are selected, select only one.`
- [x] Con un solo video selezionato funzionano come prima.

## Elimina più file
- [x] Locale, 3 video selezionati tra cui quello caricato, Canc: una sola conferma `Move these 3 files to the Recycle Bin?` con i nomi; confermando, i 3 file vanno nel Cestino (ripristinabili) e spariscono dall'elenco; viene caricato il video che seguiva l'ultimo eliminato.
- [x] Con più di 8 file la conferma mostra 8 nomi e `... and N more`.
- [x] NAS: conferma `Permanently delete these N files?` con l'avviso; i file vengono eliminati definitivamente.
- [x] Il video caricato non è tra quelli selezionati (Ctrl + clic per toglierlo): dopo l'eliminazione resta caricato e continua.
- [x] Un file bloccato da un altro programma: gli altri vengono eliminati, alla fine un messaggio indica il file non eliminato; riga in `viBro-errors.log`.
- [x] Menu del tasto destro sul video: `Delete 3 files` fa lo stesso.

## Sposta e copia più file
- [x] W con 4 video selezionati (tra cui il caricato): una sola scelta della cartella; i 4 file vengono spostati; si carica il video successivo all'ultimo spostato. Shift+W usa l'ultima destinazione.
- [x] C con 4 video: i file vengono copiati, l'elenco non cambia e il video in riproduzione continua senza interruzioni. Shift+C usa l'ultima destinazione di copia.
- [x] Spostamento sul NAS di file grandi: finestra di avanzamento `Moving 4 files`, testo `file 2 of 4: "…"`, barra che cresce per tutto il trasferimento.
- [x] Annulla durante il secondo file: il primo resta spostato, il secondo e i successivi restano nella cartella di origine, intatti; nella barra di stato `Moved 1 of 4 files … - cancelled …`.
- [x] Conflitti: 3 dei 4 file esistono già nella destinazione. Al primo conflitto: pulsanti *Keep both*, *Overwrite*, *Skip*, *Cancel* e la casella `Do the same for the next 2 conflicts`. Con *Keep both* + casella: nessun'altra domanda, i file in conflitto ricevono `nome (2).ext`.
- [x] *Skip* su un conflitto: quel file resta dov'è, gli altri vengono trasferiti; il messaggio indica `(1 skipped)`. *Cancel*: nulla viene trasferito.
- [x] Destinazione uguale alla cartella corrente: `The files are already in that folder …`, nulla cambia.
- [x] Menu del tasto destro sul video: `Move 4 files...`, `Move 4 files to <cartella>`, `Copy 4 files...`, `Copy 4 files to <cartella>`.

## Regressione su un solo file
- [x] Elimina, rinomina, sposta, copia di un solo video con conflitto: finestre e messaggi come nella 1.6.0 (il conflitto senza *Skip* e senza casella).
- [x] Galleria (F10): clic su una riga o su un'immagine con più file selezionati nell'elenco: resta selezionato solo quel video, che viene caricato.
- [x] Estratto (I / O / F9), Shift+Invio, doppio clic e Invio sulle cartelle, Ctrl+W.

## Dalla 1.6.0: release su GitHub (dopo il push)
Si possono provare solo dopo `git push --follow-tags` (commit e tag `v1.6.0` non sono ancora su GitHub).
- [x] Actions → build: i job `build` e `release` del tag `v1.6.0` riusciti; in Releases c'è `viBro 1.6.0` (*Latest*) con le note della 1.6.0 e gli archivi del sorgente, nessun EXE.
- [x] Facoltativo: Run workflow con `release_tag=v1.5.0` crea `viBro 1.5.0` con le sue note, senza togliere *Latest* alla 1.6.0.

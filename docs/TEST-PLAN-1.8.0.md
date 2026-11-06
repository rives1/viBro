# Piano di prova 1.8.0

Prove delle modifiche della 1.8.0 e regressione rapida. Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.8.0 (…)`. Usare copie dei video, non gli originali.

## F5: elenco dei file
- [ ] Con viBro aperto su una cartella, da Esplora file: copiare un video nella cartella, eliminarne un altro, creare una sottocartella. F5: il video nuovo e la cartella compaiono, quello eliminato sparisce; barra di stato `Refreshed …: 2 new, 1 removed`.
- [ ] Il video caricato continua a suonare (o resta in pausa) senza ricaricarsi; la selezione (anche multipla) resta; l'ordinamento attuale è applicato ai file nuovi; la lista non torna in cima.
- [ ] Un file che cresce (copia in corso da Esplora file): F5 aggiorna dimensione e data della sua riga.
- [ ] Il video caricato viene eliminato o rinominato da Esplora file (se Windows lo permette) o da un altro PC sul NAS: F5 lo toglie dall'elenco e il riquadro video si svuota; messaggio `… is no longer in the folder: unloaded`.
- [ ] Con I / O impostati su un video, F5 senza cambiamenti: i marker restano sulla barra temporale.
- [ ] F5 senza cambiamenti: `Refreshed …: no changes`.
- [ ] F5 su una cartella del NAS: nessun blocco dell'interfaccia.

## F5: albero delle cartelle
- [ ] Rami aperti a più livelli; da Esplora file creare e cancellare sottocartelle in un ramo aperto. F5: l'albero mostra le cartelle nuove al posto giusto (ordine naturale) e non quelle cancellate; i rami aperti restano aperti e la cartella selezionata resta selezionata.
- [ ] Una chiavetta USB inserita dopo l'avvio: F5 la aggiunge tra le unità; scollegata, F5 la toglie.
- [ ] Con un'unità di rete scollegata (o il NAS spento) e un suo ramo aperto: F5 non blocca l'interfaccia; il ramo resta com'era.
- [ ] Ctrl+F2 mostra F5.

## File AVI
- [ ] I file `.avi` compaiono nell'elenco con dimensione e data; si caricano, suonano, salti con frecce, PgUp/PgDn, barra e trascinamento col tasto destro.
- [ ] Elimina, rinomina, sposta e copia di un `.avi`.
- [ ] Estratto (I / O / F9) di un `.avi`: il nuovo `.avi` si apre e parte dal fotogramma chiave prima di I.
- [ ] Galleria (F10) in una cartella con `.avi`: immagini presenti.
- [ ] Un AVI vecchio (DivX/Xvid): annotare se i salti sono lenti o imprecisi.

## Elimina dal menu del video
- [ ] Un file locale selezionato, tasto destro sul video → *Delete*: il file va nel Cestino subito, senza conferma; viene caricato il video successivo.
- [ ] Un file sul NAS: *Delete* dal menu lo elimina definitivamente senza conferma.
- [ ] Più file selezionati, *Delete 3 files* dal menu: la conferma compare.
- [ ] Canc / R con un file: la conferma compare come prima.
- [ ] Con `[Delete] ConfirmFromVideoMenu=true` in `viBro.ini` (viBro chiuso, poi riaperto): *Delete* dal menu chiede conferma; Ctrl+F2 non indica più "no confirmation". Alla chiusura il file contiene `[Delete]` con il valore impostato.

## Tasto destro nella galleria (F10)
- [ ] Tasto destro su una riga: il video viene selezionato e caricato nella finestra principale e si apre il menu *Delete*, *Move...*, *Move to <cartella>*, *Copy...*, *Copy to <cartella>*. Lo stesso con il tasto destro su un'immagine.
- [ ] *Delete* (file locale): eliminato subito senza conferma (Cestino); la riga sparisce dalla galleria e dall'elenco; viene caricato il video successivo. Con `[Delete] ConfirmFromVideoMenu=true` la conferma compare.
- [ ] *Move...* e *Move to <cartella>*: il file viene spostato, la riga sparisce dalla galleria. *Copy...* / *Copy to*: il file viene copiato, la galleria non cambia.
- [ ] Nella finestra principale aprire un'altra cartella, poi tasto destro su una riga della galleria → *Delete*: viene riaperta la cartella della galleria e viene eliminato quel video (non un altro).
- [ ] Con più file selezionati nell'elenco, tasto destro in galleria → *Delete*: viene eliminato solo il video della riga, senza conferma.
- [ ] *Delete* subito dopo F10, mentre la riga sta ancora creando le immagini: il file viene eliminato (al massimo dopo qualche secondo di attesa), la riga sparisce e lo stato della galleria non segnala errori.
- [ ] Eliminando o spostando dall'elenco (Canc, W) un video presente nella galleria aperta, la sua riga sparisce dalla galleria; con F5 spariscono le righe dei file eliminati fuori da viBro.

## Regressione
- [ ] Selezione multipla con sposta e copia, galleria, estratto, D/E, Shift+Invio, Ctrl+W.

# Piano di prova 0.6.0

Prove ancora aperte dalle versioni precedenti, più quelle della 0.6.0 quando sarà definita.
Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Nel titolo deve comparire `viBro 0.6.0 (…)`.

Compilare con `build.cmd` (EXE normale, `artifacts\\publish`) e poi con
`powershell -ExecutionPolicy Bypass -File build\\build.ps1 -Compress` (EXE compresso, `artifacts\\publish-compressed`).

## Da versioni precedenti

### Tempo di avvio e dimensione dell'EXE (dalla 0.4.0, non eseguita nella 0.5.0)
- [x] Avviare e chiudere 5 volte l'EXE normale, poi 5 volte quello compresso; il primo avvio di ciascuno include l'estrazione nella cache. Inviare `viBro-metrics.csv` di entrambe le cartelle (righe `startup`) per decidere la variante predefinita (D-032).
  **Esito (2026-11-03, EXE 0.6.0):** normale 549 / 407 / 415 / 410 / 409 ms (mediana 410), memoria di lavoro ~141 MB; compresso 573 / 569 / 575 / 595 / 588 ms (mediana 575), ~210 MB. Scelta: EXE normale (D-047).

## 0.6.0

### Riproduzione conservata al cambio di file
- [ ] Video in pausa, D/E: il nuovo video si carica in pausa sul primo fotogramma.
- [ ] Video in riproduzione, D/E: il nuovo video parte subito in riproduzione.
- [ ] Stesso comportamento con la rotella sopra il video e con il clic su un altro file nell'elenco.
- [ ] In riproduzione: elimina (confermare), sposta e rinomina: il file proposto dopo l'operazione parte in riproduzione; in pausa resta in pausa. Se l'operazione viene annullata o fallisce, il file si ricarica nella stessa posizione e con lo stesso stato.
- [ ] All'avvio di viBro, la prima selezione si carica in pausa.
- [ ] Pressione veloce e ripetuta di D in riproduzione: viBro resta reattivo e l'ultimo video è in riproduzione.

### Copia
- [ ] C: scelta della cartella, copia; l'originale resta nell'elenco e il video continua senza interruzioni; la barra di stato riporta "Copied …".
- [ ] Shift+C e menu *Copy to <cartella>*: copia diretta nell'ultima cartella di copia, diversa da quella dello spostamento (W/Shift+W restano invariati).
- [ ] Copia nella stessa cartella: rifiutata con messaggio, nulla viene copiato.
- [ ] Copia su un nome già presente: *Keep both* crea "nome (2)", *Overwrite* sostituisce, *Cancel* non copia.
- [ ] Copia di un file grande verso il NAS: finestra di avanzamento "Copying file"; *Cancel* interrompe e nella destinazione non resta nessun file (né `.vibro-partial`).
- [ ] `viBro.ini` contiene `[Folders] LastCopyDestination`.

### Trascinamento col tasto destro
- [ ] Tasto destro tenuto sul video e movimento a destra/sinistra: il video segue il mouse; la barra e il tempo mostrano la posizione; al rilascio il fotogramma è quello esatto e non compare il menu.
- [ ] Trascinamento per tutta la larghezza del video ≈ tutta la durata; trascinando oltre il bordo del video (o fuori dalla finestra) il tempo si ferma all'inizio/fine e il rilascio viene comunque riconosciuto.
- [ ] Clic destro senza movimento: compare il menu come prima.
- [ ] Funziona in riproduzione (continua dopo il rilascio) e in pausa (resta in pausa), a finestra normale e a schermo intero (la fascia compare).

### Rotella, barra di stato, schermo intero, conferma, Ctrl+W
- [ ] Finestra normale, rotella sopra il video: giù = video successivo, su = precedente. Sopra elenco e albero la rotella scorre la lista come prima.
- [ ] Rotella sopra la barra temporale: ±3 s per scatto (su = avanti), in entrambe le modalità.
- [ ] Barra di stato bassa (poco più alta del testo), leggibile in Light e Dark; a sinistra "N videos · totale" corretto per la cartella; si aggiorna dopo elimina e sposta, e all'apertura di un'altra cartella.
- [ ] Schermo intero: la fascia con la barra temporale è in basso, sotto il video; compare e scompare come prima; il video non cambia dimensione.
- [ ] Conferma di eliminazione dal menu del tasto destro: si apre vicino al punto del clic. Con Canc/R: vicino al puntatore del mouse. Vicino al bordo dello schermo resta interamente visibile. Invio elimina, Esc annulla. Per un file in rete il testo è arancione e dice che l'eliminazione è definitiva.
- [ ] Ctrl+W chiude viBro (anche con il cursore nella barra del percorso); alla riapertura cartella, colonne e tema sono quelli lasciati.

## Regressioni
- [ ] Salti, albero, ordinamento, INI, elimina, rinomina, sposta, temi, clic e doppio clic sul video, menu, rotella a schermo intero come nella 0.5.0 (salvo le modifiche sopra).

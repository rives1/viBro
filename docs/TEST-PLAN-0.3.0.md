# Piano di prova 0.3.0

> **Usare solo copie.** Preparare una cartella locale (ad esempio `C:\\viBro-test`) e una sul NAS con 5-6 video **copiati apposta**, e due cartelle di destinazione (una locale e una sul NAS). Le eliminazioni sul NAS sono definitive.

Compilare con `build.cmd`; nel titolo deve comparire `viBro 0.3.0 (…)`.

## Eliminazione
- [ ] Locale, Canc → conferma "Move this file to the Recycle Bin?" con il nome → Invio: il file finisce nel Cestino; viene caricato in pausa il successivo.
- [ ] Locale, R → conferma → No: non succede nulla e il video resta caricato.
- [ ] Eliminare l'**ultimo** file dell'elenco: viene caricato il precedente.
- [ ] Eliminare tutti i file: con l'elenco vuoto l'area video è nera.
- [ ] NAS, Canc → la conferma dice "Permanently delete … cannot be recovered" (icona di avviso) → Sì: file eliminato.
- [ ] Tenere premuto Canc: compare **una sola** conferma; dopo averla chiusa non ne compaiono altre.
- [ ] Facoltativo: su una chiavetta USB (senza Cestino) Windows deve chiedere l'eliminazione definitiva; rispondendo No il file resta.

## Rinomina (F2)
- [ ] Il nome è proposto senza estensione e già selezionato; l'estensione è mostrata a destra.
- [ ] Nome nuovo → Invio: il file è rinominato, resta selezionato e viene ricaricato in pausa **nella stessa posizione** di prima.
- [ ] Nome con `?` o `:`, oppure `CON`: errore nella finestra, nessuna modifica.
- [ ] Nome uguale a un altro file della cartella: la finestra si riapre con "already exists"; nulla viene sovrascritto.
- [ ] Solo maiuscole/minuscole (`video.mp4` → `Video.mp4`): funziona.
- [ ] Esc: nessuna modifica.

## Spostamento
- [ ] W: si apre la finestra di Windows; scegliere la destinazione locale → il file si sposta all'istante (stesso disco) oppure con una finestra di avanzamento (disco diverso o NAS); viene caricato il successivo.
- [ ] Riaprire `viBro.ini` (a programma chiuso): `LastMoveDestination` è la destinazione scelta.
- [ ] Shift+W: sposta direttamente nella stessa destinazione, senza chiedere.
- [ ] Spostare un file grande (≥ 1 GB) tra disco locale e NAS: barra di avanzamento; **Cancel** a metà → "Move cancelled", il file è ancora nella cartella d'origine e nella destinazione non resta nulla (nemmeno `.vibro-partial`).
- [ ] Spostare un file con lo stesso nome di uno già presente nella destinazione: compare la scelta. *Keep both* → `nome (2).ext`; *Overwrite* → sostituisce; *Cancel* → non succede nulla.
- [ ] W scegliendo la cartella in cui il file si trova già: "nothing was moved".
- [ ] Shift+W con la destinazione su un NAS spento o scollegato: messaggio "not reachable" e proposta di sceglierne un'altra.

## Regressioni
- [ ] Salti, E/D, albero, ordinamento e INI funzionano come nella 0.2.0.

# Piano di prova 1.1.0

Prove delle modifiche della 1.1.0 e regressione rapida. Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.1.0 (…)`.

## 1.1.0
- [ ] Barra del percorso: icona gialla della cartella a sinistra del campo, nitida in Light e Dark; cliccandola non succede nulla; Ctrl+L, Invio ed Esc funzionano come prima.
- [ ] Finestra normale: Play, barra temporale, tempo, Mute e volume sono **sotto il video e sopra l'elenco dei file**.
- [ ] Il divisore fra video ed elenco ridimensiona il video (la barra resta della sua altezza e sempre sotto il video); la posizione del divisore si conserva alla riapertura.
- [ ] Schermo intero (F11 o doppio clic): fascia nera sotto il video con la sola barra temporale, che compare e scompare come prima; uscendo, la barra torna sotto il video sopra l'elenco.
- [ ] Finestra normale, rotella sopra il video: su = avanti 3 s, giù = indietro 3 s; il video non cambia. Sopra la barra temporale: ±3 s. Sopra elenco e albero: scorrimento della lista.
- [ ] Schermo intero, rotella sopra il video: giù = video successivo, su = precedente; sopra la barra temporale: ±3 s.
- [ ] F2: nella finestra di rinomina il cursore è all'inizio del nome e niente è selezionato; si può scrivere subito un prefisso; Invio rinomina, Esc annulla.

## Regressione
- [ ] Clic (Play/Pause), doppio clic, menu e trascinamento col tasto destro sul video, D/E con Play/Pause conservato, elimina/sposta/copia, temi, barra di stato con conteggio, Ctrl+W.

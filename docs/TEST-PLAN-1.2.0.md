# Piano di prova 1.2.0

Comprende le prove della 1.1.0 (non ancora eseguite: la 1.1.0 confluisce nella 1.2.0) più quelle della 1.2.0, e una regressione rapida. Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.2.0 (…)`.

## 1.2.0
- [ ] Pulsanti a icone: Play/Pause (▶ quando è in pausa, ❚❚ quando suona), Mute (altoparlante; barrato quando è muto), Browse (cartella aperta), Theme (sole in Light, luna in Dark). Icone nitide in Light e Dark; i tooltip indicano i tasti (Space, M) e il tema.
- [ ] Barra temporale: passando con il mouse compare sopra la barra il tempo sotto il puntatore (es. `00:42:17`), centrato sul puntatore; segue il movimento e scompare uscendo; nessun rallentamento. Funziona anche nella fascia a schermo intero.
- [ ] Elenco: il file caricato ha l'icona ▶ (in riproduzione) o ❚❚ (in pausa) davanti al nome e il nome in grassetto, anche quando la selezione è su un altro file; l'icona cambia con Space e con il clic sul video.
- [ ] Barra di stato: `12 / 128 videos · 412.5 GB`; il primo numero segue la selezione (D/E, clic, rotella a schermo intero); senza selezione solo `128 videos · …`.
- [ ] Schermo intero: dopo un salto con tasti, rotella, barra o trascinamento col tasto destro, il tempo `posizione / durata` compare sopra il video per circa 1 s. A finestra normale non compare.
- [ ] Schermo intero: al cambio di file (D/E, rotella) il nome del file compare sopra il video per circa 0,8 s. Il primo testo sovrapposto non deve far attendere (eventuale ritardo solo alla prima comparsa).
- [ ] Ctrl+F2: finestra *Commands* con i comandi di tastiera, del mouse sul video e della barra temporale; Esc o Invio la chiude; nel tema scelto. F2 da solo continua a rinominare.

## 1.1.0
- [ ] Barra del percorso: icona gialla della cartella a sinistra del campo, nitida in Light e Dark; cliccandola non succede nulla; Ctrl+L, Invio ed Esc funzionano come prima.
- [ ] Finestra normale: Play, barra temporale, tempo, Mute e volume sono **sotto il video e sopra l'elenco dei file**.
- [ ] Il divisore fra video ed elenco ridimensiona il video (la barra resta della sua altezza e sempre sotto il video); la posizione del divisore si conserva alla riapertura.
- [ ] Schermo intero (F11 o doppio clic): fascia nera sotto il video con la sola barra temporale, che compare e scompare come prima; uscendo, la barra torna sotto il video sopra l'elenco.
- [ ] Finestra normale, rotella sopra il video: su = avanti 3 s, giù = indietro 3 s; il video non cambia. Sopra la barra temporale: ±3 s. Sopra elenco e albero: scorrimento della lista.
- [ ] Schermo intero, rotella sopra il video: giù = video successivo, su = precedente; sopra la barra temporale: ±3 s.
- [ ] F2: nella finestra di rinomina il cursore è all'inizio del nome e niente è selezionato; si può scrivere subito un prefisso; Invio rinomina, Esc annulla.

## Regressione
- [ ] Clic (Play/Pause), doppio clic, menu e trascinamento col tasto destro sul video, D/E con Play/Pause conservato, elimina/sposta/copia, temi, Ctrl+W.
- [ ] Ordinamento per Name dopo il cambio della colonna Name (icona + nome): l'ordine è quello naturale come prima.

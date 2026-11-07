# Piano di prova 1.9.0

Prove delle modifiche della 1.9.0 e regressione rapida. Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.9.0 (…)`. In Windows: Impostazioni → Bluetooth e dispositivi → Mouse → *Scorri le finestre inattive al passaggio del mouse* attiva (predefinita).

**Esito (2026-11-07, utente, Windows 11, EXE compilato da `2e71b16`):** tutte le prove superate. Nessuna prova passa alla versione successiva.

## Rotella con viBro non in primo piano
- [x] Un altro programma in primo piano (es. Esplora file) e viBro visibile dietro: rotella sul video = avanti/indietro di 3 s, senza portare viBro davanti.
- [x] Nella stessa situazione, rotella sulla barra temporale: cambia video (su = successivo).
- [x] Con la galleria (F10) in primo piano: rotella sul video della finestra principale = salti.
- [x] Con una finestra di dialogo di viBro aperta (conferma di eliminazione, scelta della cartella con W, rinomina F2, Ctrl+F2): la rotella sul video non fa nulla.
- [x] Rotella su elenco e albero con viBro non in primo piano: scorrono come prima.

## Rotella sulla barra temporale invertita
- [x] Finestra normale e schermo intero: su = video successivo, giù = video precedente; ai limiti dell'elenco non ricomincia dall'altra parte.
- [x] Rotella sopra il video: invariata (su = avanti 3 s).

## Tasto destro + rotella sul video
- [x] Tasto destro tenuto sul video e rotella su: video successivo; giù: precedente; più scatti = più video. Rilasciando il tasto destro il menu non compare e il video resta quello scelto, nella posizione iniziale.
- [x] Trascinamento col tasto destro, poi rotella senza rilasciare: cambia video e il trascinamento si ferma; al rilascio nessun salto, nessun menu.
- [x] Senza rotella: clic destro = menu, trascinamento = spostamento nel tempo, come prima.
- [x] A schermo intero: stesso comportamento.

## File M4V
- [x] I file `.m4v` compaiono nell'elenco; si caricano e suonano; salti, galleria, elimina, rinomina, sposta, copia.
- [x] Estratto (I / O / F9) di un `.m4v`: il nuovo `.m4v` si apre in viBro e in un altro lettore.
- [x] I `.mov` (anche `.MOV`) compaiono come prima.

## Regressione
- [x] Rotella sul video a finestra attiva, D/E, F5, selezione multipla, Ctrl+F2 (descrive la nuova rotella e tasto destro + rotella).

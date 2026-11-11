# Piano di prova 1.13.0

Prove delle novità della 1.13.0, poi la regressione completa di [TEST-REGRESSION.md](TEST-REGRESSION.md). Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.13.0 (…)`. Usare copie dei video per elimina e sposta.

**Esito (2026-11-11, utente, Windows 11, EXE compilato da `1cd7180`):** tutte le prove superate, compresa la regressione di `TEST-REGRESSION.md` e il puntatore nascosto anche sopra il video a schermo intero. Nessuna prova passa alla versione successiva.

## Barra di stato
- [x] A sinistra `4 / 800 videos · 273 GB`; a destra, con un video caricato, il formato (`HEVC 1280×720`), con nome, hwdec e tempo di caricamento nel tooltip; il nome del file non è più ripetuto al centro.
- [x] Con `MetricsLog=false` niente `switch … ms` né `mem … MB`; con `MetricsLog=true` compaiono in una zona a destra.
- [x] Spostando un file: messaggio verde `Moved …` che resta almeno 6 s anche se nel frattempo si carica un altro video (D/E); poi la barra torna normale.
- [x] Un errore (es. F9 senza marker, o un video non riproducibile): messaggio arancio, resta 6 s.
- [x] Clic sul messaggio: elenco degli ultimi 20 messaggi con l'ora, il più recente in alto, errori arancio ed esiti verdi; clic fuori lo chiude. Nessun messaggio `Loading …` nell'elenco.

## Barra del percorso
- [x] La barra mostra `E: › HDreview › A` (l'ultima in grassetto); clic su `HDreview`: si apre quella cartella e l'albero si aggiorna.
- [x] Percorso lungo: si vede la parte finale.
- [x] Clic sulla parte vuota della barra: diventa testo con il percorso selezionato; scrivere un altro percorso e Invio lo apre; Esc (o clic altrove) torna alle briciole senza cambiare cartella. Ctrl+L fa lo stesso.
- [x] Percorso di rete `\\server\share\…`: la prima briciola è `\\server\share`.
- [x] Backspace con lo stato attivo sull'elenco o sul video: cartella superiore, con selezionata la cartella appena lasciata; nella radice dell'unità un messaggio. Mentre si scrive nella barra, Backspace cancella testo.

## Volume
- [x] `+` e `-` (anche del tastierino): volume di 5 % in 5 %, lo slider si muove e sopra il video compare `Volume 55 %` per circa 1 s, anche a finestra normale; con il muto attivo `(muted)`. Tenendo premuto il volume continua a cambiare.
- [x] Riavviando viBro il volume è quello impostato.
- [x] Nelle gallerie `+` / `-` cambiano il volume.

## Schermo intero
- [x] A schermo intero, mouse fermo: dopo 2 s spariscono la fascia **e il puntatore, anche quando è sopra il video**. Al primo movimento tornano entrambi.
- [x] Uscendo dallo schermo intero (Esc, doppio clic) e con Alt+Tab il puntatore è sempre visibile.

## Riquadro video vuoto
- [x] All'avvio, senza video caricato: al centro `Select a video · F10 gallery · Ctrl+F2 commands`. Clic su un video: il video compare normalmente. Con `[Keys] Gallery` cambiato, il testo mostra il nuovo tasto.
- [x] Eliminando l'ultimo video della cartella il riquadro torna alla guida.

## Gallerie
- [x] F10: la riga del video caricato ha un bordo colorato (colore di accento di Windows); con D / E il bordo passa alla riga successiva e la galleria la porta in vista. La riga selezionata ha uno sfondo leggero. Lo stesso in F12.
- [x] F10: il tempo è dentro l'immagine in basso a destra; nessuna riga di tempi sotto le immagini; le righe sono più vicine (più righe visibili di prima).
- [x] Spostare e ridimensionare la galleria F10, chiuderla e riaprirla: stessa posizione e dimensione; lo stesso, separatamente, per F12; massimizzata resta massimizzata. Con un monitor in meno (o una posizione fuori schermo nel file `.ini`) si apre al centro.

## Tempi
- [x] Barra: `1:20 / 15:16` (e `1:02:15` oltre l'ora), senza zeri iniziali; tempo sotto il puntatore, tempo sopra il video e gallerie nello stesso formato; marker `In 1:23.4`.

## Regressione
- [x] Tutte le prove di [TEST-REGRESSION.md](TEST-REGRESSION.md).

# Piano di prova 1.15.0

Prove delle novità della 1.15.0 (D-084), poi la regressione completa di [TEST-REGRESSION.md](TEST-REGRESSION.md). Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.15.0 (…)`. Usare copie dei video per elimina e sposta, in una cartella con molti file (idealmente qualche centinaio, nomi con e senza `4K`, estensioni e dimensioni diverse) e almeno una sottocartella.

**Esito (2026-11-12, utente, Windows 11, EXE compilato da `726605a`):** tutte le prove superate, compresa la regressione di `TEST-REGRESSION.md`. Nessuna prova passa alla versione successiva.

## Casella e sintassi
- [x] Sopra l'elenco c'è la casella con il suggerimento `Filter · name words .mp4 >1G <500M (Ctrl+F)`. Ctrl+F (da elenco, video o albero) porta il cursore nella casella.
- [x] `4K`: restano solo i video con 4K nel nome (anche `4k`), con `4K` evidenziato nel nome; l'elenco cambia poco dopo l'ultimo tasto, senza scatti anche scrivendo velocemente. Cartelle e `..` restano.
- [x] `.mp4`: solo MP4; `.mp4 .mkv`: MP4 e MKV; `mp4` senza punto: i nomi che contengono "mp4" (di solito tutti gli MP4, perché il nome comprende l'estensione).
- [x] `>1000` e `>1G`: solo file oltre circa 1 GB (confrontare con la colonna Size); `<500M`: sotto 500 MB; `>1G <3G`: tra 1 e 3 GB.
- [x] `4K .mkv >1G`: tutte e tre le condizioni insieme.
- [x] Un filtro senza risultati: l'elenco mostra solo le cartelle, la barra di stato `0 videos · … · filter: 0 of N`.
- [x] Barra di stato con il filtro: conteggio e dimensione dei soli video visibili, e `filter: 12 of 128`; con una selezione `3 / 12 videos`.

## Tastiera
- [x] Scrivendo nella casella, D, E, W, C, R, I, O, Spazio diventano testo e non fanno nulla al video o ai file.
- [x] Invio (o ↓) dopo `4K`: l'elenco ha la tastiera, il primo video trovato è selezionato e caricato (se nessuna riga era selezionata). Le frecce ← → tornano a spostare il tempo del video.
- [x] Esc nella casella con testo: il filtro si svuota e tornano tutte le righe; Esc di nuovo: tastiera all'elenco. Il pulsante ✕ svuota il filtro.

## Funzioni sui video visibili
- [x] Con il filtro attivo, D / E e la rotella (anche a schermo intero) passano solo tra i video visibili; dall'ultimo visibile D non va oltre.
- [x] Ctrl+A seleziona solo i video visibili (`N selected` = numero visibile); Canc chiede di eliminare **solo** quelli: verificare nel testo della conferma che non compaiano file nascosti.
- [x] Selezionare alcuni video, poi scrivere un filtro che ne nasconde una parte: i nascosti escono dalla selezione (conteggio nella barra di stato); W / Canc agiscono solo sui visibili.
- [x] Eliminare un video con il filtro: viene caricato il successivo **visibile**; eliminando l'ultimo visibile compare `No more videos match the filter`.
- [x] F10 con il filtro: la galleria contiene solo i video visibili e il titolo mostra `(filter: …)`.
- [x] Galleria aperta, poi un filtro che nasconde un suo video: clic su quella riga della galleria → il filtro si svuota con il messaggio `Filter cleared: … did not match it` e il video si carica.
- [x] Rinominare (F2) un video visibile con un nome che non corrisponde più al filtro: il filtro si svuota con il messaggio e il file resta selezionato.
- [x] Il video caricato nascosto dal filtro continua a suonare; tornando a un filtro vuoto è ancora evidenziato (▶ e grassetto).

## Cartelle e aggiornamento
- [x] Con un filtro attivo, aprire una sottocartella (doppio clic, albero, briciole): la casella si svuota e l'elenco è completo.
- [x] F5 con un filtro attivo: il filtro resta; un file nuovo che corrisponde compare (in dissolvenza), uno che non corrisponde no.
- [x] Ordinare per Name / Size / Date con il filtro attivo: il filtro resta e l'ordine cambia.
- [x] Ctrl+F2: la riga del filtro con il tasto in uso; con `[Keys] Filter` cambiato nel file `.ini`, funziona il nuovo tasto.

## Regressione
- [x] Tutte le prove di [TEST-REGRESSION.md](TEST-REGRESSION.md).

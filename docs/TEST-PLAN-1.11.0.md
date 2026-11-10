# Piano di prova 1.11.0

Prove delle modifiche della 1.11.0 e regressione rapida. Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.11.0 (…)`. Serve `ffmpeg.exe` accanto a `viBro.exe`. Usare copie dei video per le prove di elimina e sposta.

**Esito (2026-11-10, utente, Windows 11, EXE compilato da `c59c458`):** tutte le prove superate. Nessuna prova passa alla versione successiva.

## Galleria della selezione (F12)
- [x] Nessun video selezionato (solo una cartella o `..`): F12 scrive un avviso nella barra di stato e non apre nulla. Senza `ffmpeg.exe`: avviso di galleria non disponibile.
- [x] 4 video selezionati (Ctrl + clic), F12: si apre *Selection gallery - 4 videos - <cartella>* con 4 strisce nell'ordine dell'elenco; i video non selezionati e le cartelle non compaiono.
- [x] Ogni striscia ha 10 immagini, vicine al 10, 20 … 90 % e al 99 % della durata (fotogramma chiave più vicino); nessun tempo sotto le immagini, nessuna riga col nome.
- [x] Spazio tra le strisce minimo (circa 1 pixel); le immagini occupano la larghezza della finestra e si adattano al ridimensionamento.
- [x] Il tooltip della striscia mostra il nome del file.
- [x] Clic su una striscia: il video viene selezionato e caricato nella finestra principale. Clic su un'immagine: il video va al tempo di quell'immagine.
- [x] Tasto destro: stesso menu della galleria; *Delete* elimina (senza conferma, come il menu del video) e la striscia sparisce; *Move* sposta e la striscia sparisce; *Copy* non cambia nulla.
- [x] Galleria F10 e galleria F12 aperte insieme: eliminando un video presente in entrambe, sparisce da tutte e due. F12 di nuovo con un'altra selezione sostituisce la galleria F12; Esc la chiude; chiudendo viBro si chiudono entrambe.
- [x] Ctrl+F2 mostra F12.

## viBro.ini
- [x] Alla chiusura: `[SelectionGallery] PositionsPercent=10,20,30,40,50,60,70,80,90,99` e `[Keys] SelectionGallery=F12`.
- [x] Con `PositionsPercent=25,50,75`: 3 immagini per striscia. Con `PositionsPercent=10,x`: avviso e valori predefiniti.
- [x] Con `[Keys] SelectionGallery=Ctrl+G`: la galleria si apre con Ctrl+G e non più con F12.

## Regressione
- [x] Galleria F10 invariata (nome, durata, tempi sotto le immagini, 5 immagini), selezione multipla, operazioni sui file, F5, rotella.

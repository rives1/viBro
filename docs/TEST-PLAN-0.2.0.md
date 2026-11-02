# Piano di prova 0.2.0

Compilare con `build.cmd`; nel titolo della finestra deve comparire `viBro 0.2.0 (…)`.

## Albero
- [ ] All'avvio compaiono Desktop, Documents, Videos, Downloads e tutte le unità; le unità di rete compaiono subito, anche se scollegate, e il loro nome si aggiorna dopo.
- [ ] Espandendo un'unità o una cartella compaiono le sottocartelle (nessuna cartella nascosta); su NAS appare "Loading..." finché l'elenco non è pronto.
- [ ] Un'unità di rete scollegata mostra "(unavailable: …)" senza bloccare nulla; dopo averla ricollegata, una nuova espansione funziona.
- [ ] Un clic su una cartella apre l'elenco dei suoi video.
- [ ] Scrivendo un percorso profondo nella barra (anche UNC, ad esempio `\\nas\video\a\b`) l'albero si espande fino a quella cartella e la seleziona; per un UNC compare una nuova radice `\\nas\video`.

## Elenco
- [ ] Clic su Name, Size, Date modified: ordine crescente con ▲; nuovo clic: decrescente con ▼.
- [ ] Riordinando mentre un video è caricato, il video non si ricarica e resta selezionato.
- [ ] E/D seguono l'ordine visualizzato.

## Riquadri e INI
- [ ] Spostare i due divisori, ridimensionare le colonne, cambiare l'ordinamento e il volume, attivare il mute, spostare o massimizzare la finestra, aprire una cartella. Chiudere e riaprire: tutto viene ripristinato, compresa l'ultima cartella, e **nessun video** viene caricato.
- [ ] Aprire `viBro.ini` con un editor di testo: sezioni leggibili. Scrivere `Volume=abc` e riavviare: compare un avviso arancione e si usa il volume 100.
- [ ] Rendere `viBro.ini` di sola lettura (Proprietà → Sola lettura) e riavviare: avviso "Settings will not be saved"; chiudendo, il file non cambia. Togliere poi la sola lettura.
- [ ] F11: a schermo intero resta solo il video; Esc ripristina albero, elenco e barre.

## Regressioni 0.1.0
- [ ] Caricamento in pausa, salti, E/D, Spazio, M, Ctrl+L funzionano come prima; le frecce eseguono i salti anche dopo un clic sull'albero.

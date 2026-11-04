# Piano di prova 1.6.0

Prove delle modifiche della 1.6.0 e regressione rapida. Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.6.0 (…)`. Serve `ffmpeg.exe` accanto a `viBro.exe` (sul PC: ffmpeg 9.0.2).

## Galleria (F10)
- [ ] Senza `ffmpeg.exe`: F10 scrive nella barra di stato che la galleria non è disponibile; Ctrl+F2 lo indica. Con `ffmpeg.exe`: Ctrl+F2 descrive F10.
- [ ] F10 in una cartella con video (SSD): si apre la finestra *Gallery - <cartella>*, una riga per video, nell'ordine dell'elenco; le cartelle e `..` non compaiono. Nome, durata e 5 immagini per riga, con il tempo sotto ciascuna.
- [ ] Ordine: lo stesso dell'elenco al momento di F10, con ordinamento per Name, Size e Date modified, in entrambe le direzioni. Cambiando poi l'ordinamento nella finestra principale la galleria non cambia; F10 di nuovo la ricrea nel nuovo ordine.
- [ ] Le immagini sono vicine al 10, 30, 50, 70 e 90 % della durata (un video di 10 min: circa 1, 3, 5, 7, 9 min, con lo scarto del fotogramma chiave). Il tempo sotto l'immagine è quello del fotogramma mostrato.
- [ ] Prima compaiono le immagini delle righe visibili; scorrendo in basso, le righe nuove vengono riempite per prime. La finestra resta utilizzabile mentre lavora; in basso lo stato `images 12 / 128 ...`, poi `done in N s`.
- [ ] Ridimensionando la finestra le 5 immagini occupano sempre la larghezza, in riquadri 16:9 (video verticali con bande ai lati); le righe non cambiano altezza mentre arrivano le immagini.
- [ ] Clic su una riga (o frecce ↑/↓ nella galleria): nella finestra principale il video viene selezionato, l'elenco scorre fino a lui e il video si carica, con Play/Pause conservato. Cliccando la riga del video già caricato non si ricarica.
- [ ] Clic su un'immagine: il video si carica (se non lo era) e va al tempo dell'immagine; il player mostra lo stesso fotogramma dell'immagine. Su un video già caricato: solo il salto.
- [ ] Nella finestra principale si apre un'altra cartella, poi si clicca una riga della galleria: viene riaperta la cartella della galleria (albero e barra del percorso aggiornati) e il video viene selezionato e caricato.
- [ ] F10 con la galleria aperta: la galleria viene ricreata sulla cartella corrente. F10 di nuovo su una cartella già vista: le immagini compaiono subito (memoria).
- [ ] Esc chiude la galleria; chiudendola durante la creazione, i processi `ffmpeg.exe` terminano (Gestione attività). Chiudendo viBro con la galleria aperta, si chiudono entrambe e nessun `ffmpeg.exe` resta attivo.
- [ ] Un file non leggibile o rinominato in `.mp4` senza esserlo: la riga mostra `Cannot read: …` e le immagini `no image`; le altre righe proseguono; nello stato `with errors`; riga in `viBro-errors.log`.
- [ ] MP4, MKV, MOV e WMV: le immagini compaiono in tutti i formati.
- [ ] Cartella grande sul NAS (unità K:): tempo totale annotato (`done in N s`), interfaccia sempre reattiva; prova anche con `[Gallery] Jobs=1` e `Jobs=4` e annota il tempo.
- [ ] Tema Light/Dark: la galleria segue il tema, anche cambiandolo con la galleria aperta.

## viBro.ini
- [ ] Alla chiusura `viBro.ini` contiene `[Gallery]` con `PositionsPercent=10,30,50,70,90` e `Jobs=2`.
- [ ] Con `PositionsPercent=5,50,95`: 3 immagini per riga (Ctrl+F2 indica 3). Con `PositionsPercent=10,abc` o `Jobs=0`: avviso nella barra di stato e valori predefiniti.

## Release su GitHub (dopo il push del tag `v1.6.0`)
Si può provare solo dopo il tag, che si crea a prove superate: se fallisce si corregge in una 1.6.1.
- [ ] Actions → build: i job `build` e `release` riusciti; in Releases c'è `viBro 1.6.0` (*Latest*) con le note della 1.6.0 e gli archivi del sorgente, nessun EXE.
- [ ] Facoltativo: Run workflow con `release_tag=v1.5.0` crea `viBro 1.5.0` con le sue note, senza togliere *Latest* alla 1.6.0.

## Regressione
- [ ] I / O / F9 (estratto), D/E, rotella, trascinamento col tasto destro, elimina/rinomina/sposta/copia, Shift+Invio, Ctrl+W.

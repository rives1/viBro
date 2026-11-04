# Piano di prova 1.5.0

Prove dell'estratto (I / O / F9) e regressione rapida. Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.5.0 (…)`. Per le prove con ffmpeg copiare un `ffmpeg.exe` (per esempio quello di Gyan installato con winget, provato 9.0.2) nella cartella di `viBro.exe` (`artifacts\publish\`).

## Senza ffmpeg.exe
- [ ] Senza `ffmpeg.exe` nella cartella di viBro: I, O e F9 mostrano nella barra di stato "Extract not available: put ffmpeg.exe in the viBro folder (…)"; nessun file creato.
- [ ] Ctrl+F2 indica che l'estratto non è disponibile.
- [ ] Copiando `ffmpeg.exe` con viBro aperto, I/O/F9 funzionano senza riavviare.

## Marker
- [ ] Su un video caricato, I e O mostrano `In hh:mm:ss.d  ·  Out hh:mm:ss.d` nella barra di stato; con entrambi compare "F9 extracts". A schermo intero il testo compare anche sopra il video.
- [ ] Premendo di nuovo I o O il marker si sposta nella nuova posizione.
- [ ] F9 senza marker, con uno solo, o con O prima di I: avviso, nessun file.
- [ ] Cambiando video, i marker del video precedente non valgono più (F9 chiede di metterli).
- [ ] Sulla barra temporale: linea verde in I, rossa in O, fascia gialla tra i due; con il cursore portato al tempo di un marker, il pollice è sopra la linea. Le linee restano al loro posto ridimensionando la finestra e a schermo intero; spariscono cambiando video e ricompaiono tornando sul file solo dopo aver rimesso i marker. Clic e trascinamento sulla barra funzionano anche sopra le linee.

## Estratto (locale e su NAS, MP4, MKV, MOV)
- [ ] F9 crea `nome-extract-aaMMgg-OOmm.ext` nella cartella del video, con data e ora correnti; al termine una finestra *Extract* conferma il successo (nome, dimensione, intervallo, cartella); il file compare nell'elenco, la selezione e il video caricato non cambiano, la riproduzione continua.
- [ ] Un secondo F9 nello stesso minuto crea `…-2.ext`; nessun file viene sovrascritto.
- [ ] L'estratto si apre in viBro: parte dal fotogramma chiave al marker I o poco prima (anche diversi secondi) e finisce vicino a O; audio presente e sincronizzato.
- [ ] Estratto lungo su NAS (alcuni minuti di un 4K): compare la finestra di avanzamento; *Cancel* interrompe e non lascia file parziali.
- [ ] Cartella di sola lettura o file danneggiato: messaggio di errore con il testo di ffmpeg, riga in `viBro-errors.log`, nessun file parziale.
- [ ] Il tempo dell'estrazione resta breve anche su NAS (copia dei dati, nessuna ricodifica).

## Regressione
- [ ] I, O e F9 non interferiscono con la rinomina (F2) e con la barra del percorso (lettere digitate nel testo).
- [ ] Salti, D/E, rotella, elimina/rinomina/sposta/copia (con finestra di avanzamento), temi, Ctrl+F2, Ctrl+W.

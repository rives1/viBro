# Piano di prova 0.4.0

Compilare con `build.cmd` (EXE normale, `artifacts\\publish`) e poi con
`powershell -ExecutionPolicy Bypass -File build\\build.ps1 -Compress` (EXE compresso, `artifacts\\publish-compressed`).
Nel titolo deve comparire `viBro 0.4.0 (…)`.

## Temi
- [ ] Primo avvio con un `viBro.ini` senza la sezione `[Appearance]`: il tema segue Windows (Impostazioni → Personalizzazione → Colori).
- [ ] Il pulsante *Theme* passa da Light a Dark e viceversa: albero, elenco, intestazioni delle colonne, barre di scorrimento, pulsanti, cursori, barra di stato e barra del titolo cambiano in modo coerente; testi leggibili e selezione visibile in entrambi i temi.
- [ ] Rinomina (F2), conflitto di spostamento e avanzamento sono nel tema scelto.
- [ ] Chiudendo e riaprendo, il tema scelto resta (`[Appearance] Theme=Dark` o `Light`).

## Icone, righe e cursori
- [ ] Albero: ogni nodo ha l'icona del suo tipo (Desktop, Documents, Downloads, Videos, disco locale, unità di rete, chiavetta USB se presente, condivisioni di rete); le cartelle mostrano l'icona aperta quando sono espanse. Icone nitide anche con scala di Windows al 125-150%, leggibili in Light e Dark.
- [ ] Elenco e albero: righe basse (circa 24 px), testo non tagliato, selezione e indicatore blu visibili; anche dopo il cambio di tema.
- [ ] Barra temporale: cliccare in un punto della traccia e, senza rilasciare, trascinare: l'indicatore e il video seguono il mouse. Rilasciando, la riproduzione continua dal punto scelto.
- [ ] Dopo il trascinamento le frecce (salti) funzionano subito, senza cliccare sull'elenco.
- [ ] Stesso comportamento sul cursore del volume e sulla barra a schermo intero.
- [ ] R, rispondere No alla conferma, usare solo il mouse (ad esempio cliccare sulla barra temporale), poi R: la conferma compare alla **prima** pressione. Stessa prova con Canc, F2 (annullare la rinomina) e W (annullare la scelta della cartella).
- [ ] Tenendo premuto R non si apre più di una conferma.

## Barra dei controlli e schermo intero
- [ ] Finestra normale: Play, barra temporale, tempo, Mute e volume stanno sopra il video.
- [ ] F11: in alto una fascia nera; la barra temporale, larga circa il 40% e centrata, compare e scompare dopo circa 2 s.
- [ ] Muovendo il mouse sul video la barra ricompare; con il mouse fermo sul video non resta sempre visibile.
- [ ] Con il mouse sulla fascia la barra non scompare; si può cliccare e trascinare per spostarsi nel video.
- [ ] Le frecce e PgUp/PgDn a schermo intero fanno ricomparire la barra.
- [ ] Il video non cambia dimensione quando la barra compare o scompare.
- [ ] Esc: ritorno alla finestra normale con la barra sopra il video.

## Errori
- [ ] Aprire un file `.mp4` non valido (ad esempio un file di testo rinominato): messaggio nella barra di stato e riga in `viBro-errors.log` accanto all'EXE.
- [ ] Durante la riproduzione di un file sul NAS, scollegare il cavo di rete o spegnere il NAS: entro qualche secondo, messaggio "Playback … stopped" (se mpv segnala l'errore) e riga nel log; viBro non si blocca.

## Portabilità
- [ ] Copiare `viBro.exe` su un altro PC senza ambiente di sviluppo: si avvia, riproduce, salva l'INI.
- [ ] Facoltativo, test di `vulkan-1.dll`: in Windows Sandbox o in una VM senza driver GPU, dove `C:\\Windows\\System32\\vulkan-1.dll` **non** esiste, viBro deve avviare il video. `viBro-errors.log` riporta "using the bundled Vulkan loader".

## Cestino con percorso lungo (locale, solo copie)
- [ ] In PowerShell creare una cartella con percorso totale oltre 260 caratteri e copiarvi un video:
  `$d = "C:\\viBro-test\\" + ("a" * 120) + "\\" + ("b" * 120); New-Item -ItemType Directory -Force $d; Copy-Item <video> $d`
  Aprire quella cartella in viBro (incollare il percorso nella barra), Canc → Invio: il file va nel **Cestino** (verificare che compaia lì). Se compare un errore, il file non deve essere stato eliminato.

## Tempo di avvio e dimensione dell'EXE
- [ ] Avviare e chiudere 5 volte l'EXE normale, poi 5 volte quello compresso; il primo avvio di ciascuno include l'estrazione nella cache. Inviare `viBro-metrics.csv` di entrambe le cartelle (righe `startup`) per decidere la variante predefinita.

## Regressioni
- [ ] Salti, E/D, albero, ordinamento, INI, elimina, rinomina e sposta come nella 0.3.0.

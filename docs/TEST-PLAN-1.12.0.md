# Piano di prova 1.12.0

Prove delle novità della 1.12.0, poi la regressione completa di [TEST-REGRESSION.md](TEST-REGRESSION.md) (dalla 1.12.0 la regressione non si riscrive più in ogni piano). Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.12.0 (…)`. Serve `ffmpeg.exe` accanto a `viBro.exe`.

**Esito (2026-11-11, utente, Windows 11, EXE compilato da `934cd09`):** tutte le prove superate, compresa la regressione di `TEST-REGRESSION.md`. I tempi della misura sul NAS (Combined / Separate) non sono stati riportati. Nessuna prova passa alla versione successiva.

## Gallerie: un solo ffmpeg per video (`[Gallery] Method`)
- [x] Alla chiusura `viBro.ini` contiene `[Gallery] Method=Combined`.
- [x] F10 e F12 su una cartella locale: le immagini sono come nella 1.11.0 (stesse posizioni, tempi reali sotto le immagini in F10); clic su un'immagine = stesso fotogramma nel player.
- [x] **Misura sul NAS** (stessa cartella, almeno 20 video, viBro riavviato tra le prove perché le immagini restano in memoria): F12 con `Method=Combined` → `done in … s` = ____; con `Method=Separate` → ____. Ripetere con F10: Combined ____ / Separate ____.
- [x] Una cartella con MKV, MOV, WMV, AVI, M4V: tutte le righe hanno immagini; nessuna riga vuota o con `no image` che nella 1.11.0 c'era. Annotare se `viBro-errors.log` contiene `Combined images failed` e per quale estensione.
- [x] Con `Method=fast` (non valido): avviso e Combined.

## Gallerie: tastiera
- [x] In una galleria, con una riga selezionata: Canc (conferma) elimina quel video e la riga sparisce; F2 lo rinomina; W / Shift+W lo spostano; C / Shift+C lo copiano; Spazio e M agiscono sul video caricato.
- [x] ↑ / ↓, PgUp / PgDn, Home / End continuano a muovere tra le righe (e caricano il video); Esc chiude la galleria.
- [x] Con `[Keys] Delete=X` la galleria elimina con X.
- [x] Ctrl+F2 mostra la sezione *Galleries (F10, F12)*.

## File coinvolti in un'operazione
- [x] Eliminando 10 file (Ctrl+A, Canc), durante l'eliminazione clic su uno dei file selezionati: non viene caricato, la barra di stato dice `… is being deleted or moved …`; un file non selezionato si carica normalmente.
- [x] Spostando più file grandi, nei primi istanti (prima della finestra di avanzamento) clic su uno dei file: non viene caricato; lo spostamento termina senza errori.

## Misure (`viBro-metrics.csv`)
- [x] Con un `viBro.ini` nuovo (o senza la riga `MetricsLog`) il file `viBro-metrics.csv` non viene creato. Nel `viBro.ini` attuale c'è `MetricsLog=true` scritto dalle versioni precedenti: per spegnerle va messo `false` a mano.
- [x] Con `MetricsLog=true` il file riceve righe; oltre 10 MB viene rinominato `viBro-metrics-old.csv` e ne inizia uno nuovo (facoltativo: si può provare copiando un CSV grande al posto di quello attuale).

## Repository
- [x] `git status` non mostra un file `.avi` o `.m4v` copiato per prova nella cartella del progetto.

## Regressione
- [x] Tutte le prove di [TEST-REGRESSION.md](TEST-REGRESSION.md).

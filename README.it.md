# viBro

viBro è un'applicazione desktop per Windows 11 x64 che serve a esplorare cartelle di video, vederli all'istante e organizzarli (eliminare, rinominare, spostare). La priorità è la reattività: il primo fotogramma deve comparire subito, il passaggio da un video all'altro deve essere immediato e i salti temporali rapidi, anche su NAS.

Tecnologie: C#/WPF (.NET 10) per l'interfaccia, [libmpv](https://mpv.io) per la decodifica e la riproduzione. Il programma si distribuisce come un unico `viBro.exe` portabile.

> **Stato: 1.10.0 rilasciata** (scorciatoie da tastiera configurabili in `viBro.ini`). La 1.0.0 è stata la prima versione completa e verificata. La 0.1.0 ha validato la tecnologia su SSD e NAS. Il percorso fino alla 1.0.0 è descritto in [SPEC.md](SPEC.md).
>
> English version: [README.md](README.md).

## Funzionalità

- Un unico EXE autonomo: nessuna installazione, nessun privilegio di amministratore, nessun codec da installare.
- Barra del percorso: cartelle locali, unità mappate e percorsi UNC (`\\server\share`). L'apertura avviene in background, quindi una condivisione irraggiungibile non blocca l'interfaccia.
- Elenco delle sottocartelle e dei video della cartella (MP4, MKV, MOV, WMV, AVI dalla 1.8.0, M4V dalla 1.9.0), senza ricorsione: prima `..` (cartella superiore), poi le cartelle, poi i video, ogni gruppo nell'ordine scelto (nomi in ordine naturale, lo stesso di Esplora risorse). Doppio clic o Invio aprono una cartella; D/E, la rotella e le operazioni sui file saltano le cartelle.
- Al cambio di file lo stato **Play/Pause si conserva**: se il video era in pausa (o all'avvio) il nuovo si carica in pausa sul primo fotogramma; se era in riproduzione, il nuovo parte.
- Salti: per impostazione predefinita ±1 s e ±5 s esatti, ±1 min e ±5 min su fotogrammi chiave (rapidi, approssimati) oppure esatti con *Exact long seeks*; ogni durata si cambia in `[Seek]` di `viBro.ini`. Le richieste ripetute vengono accorpate, quindi non si accumula una coda.
- Play/Pause, barra temporale, volume, mute, schermo intero.
- **Misure automatiche** in `viBro-metrics.csv`, accanto all'EXE: tempo al primo fotogramma, cambio video, latenza dei salti, memoria.

- Albero delle cartelle (cartelle utente, unità locali e di rete, condivisioni UNC aperte), caricato progressivamente e sincronizzato con la cartella aperta.
- Ordinamento per Name, Size, Date modified; tre riquadri ridimensionabili; impostazioni in `viBro.ini`.

- Eliminazione, rinomina e spostamento dei file, con conferme, avanzamento e annullamento.
- **Estratto di una parte del video** (1.5.0): I e O segnano inizio e fine, F9 scrive la parte in un nuovo file nella stessa cartella. Usa un `ffmpeg.exe` messo dall'utente accanto a `viBro.exe` (non incluso; senza, la funzione è disattivata). Nessuna ricodifica (rapido, qualità originale): l'estratto parte dal fotogramma chiave al marker di inizio o prima, anche diversi secondi prima; la fine è vicina al marker di fine. I marker compaiono sulla barra temporale (I verde, O rosso, fascia gialla tra i due) e un messaggio conferma l'estratto a fine procedura.
- **Galleria** (1.6.0; menu del tasto destro 1.8.0): F10 apre una finestra separata con una riga per ogni video della cartella e 5 immagini prese alle stesse percentuali della durata di ciascun video (predefinite 10, 30, 50, 70, 90 %). Un clic su una riga seleziona e carica quel video nella finestra principale; un clic su un'immagine lo carica al tempo dell'immagine. Usa lo stesso `ffmpeg.exe` dell'estratto. Le immagini sono i fotogrammi chiave più vicini a quelle posizioni (rapide, anche sul NAS), create in background partendo dalle righe visibili, tenute solo in memoria (nulla su disco) e riusate finché viBro resta aperto. La galleria resta sulla cartella in cui è stata aperta; F10 di nuovo ne crea una nuova. Il tasto destro su una riga o un'immagine (1.8.0) seleziona e carica quel video e apre lo stesso menu del video (Delete, Move, Copy); le righe dei file eliminati o spostati escono dalla galleria.
- **Selezione multipla** (1.7.0): Ctrl + clic aggiunge o toglie un file, Shift + clic seleziona un intervallo, Ctrl+A seleziona tutti i video. Elimina, sposta e copia agiscono su tutti i video selezionati: una conferma, una destinazione, una finestra di avanzamento; i conflitti di nome si chiedono file per file, con "fai lo stesso per i conflitti successivi" e *Skip*. Il riquadro video carica il file appena cliccato. Rinomina (F2), Shift+Invio e l'estratto (I / O / F9) richiedono un solo video selezionato. Le frecce continuano a spostare il tempo, quindi la selezione si fa col mouse.

- Temi Light/Dark (pulsante *Theme*), barra dei controlli (barra temporale, volume) sotto il video e sopra l'elenco dei file, fascia con la barra temporale sotto il video a schermo intero.
- Icona della cartella davanti alla barra del percorso; pulsanti a icone per Play/Pause, Mute (indica lo stato), Browse e Theme.
- Tempo sotto il puntatore sulla barra temporale; il file caricato è evidenziato nell'elenco (▶ in riproduzione, ❚❚ in pausa, nome in grassetto); la barra di stato mostra la posizione, es. `12 / 128 videos · 412.5 GB`.
- A schermo intero il tempo compare sopra il video per circa 1 s dopo un salto e il nome del file per 0,8 s al cambio di file (OSD di mpv).
- Barra di stato compatta: numero di video della cartella e spazio totale, stato, misure.
- Registro degli errori `viBro-errors.log` accanto all'EXE.

## viBro.ini

Si trova accanto a `viBro.exe` e viene scritto alla chiusura. Va modificato solo a programma chiuso. Un valore non valido viene sostituito dal predefinito e segnalato nella barra di stato. Oltre alla finestra, ai riquadri, alle colonne, all'ultima cartella e all'audio, che il programma salva da solo, contiene:

| Sezione / chiave | Predefinito | Significato |
|---|---|---|
| `[Playback] HardwareDecoding` | `auto-safe` | Decodifica hardware (`no` = solo software, oppure un valore `hwdec` di mpv) |
| `[Playback] ExactLongSeeks` | `false` | Rende esatti tutti i salti e la barra temporale, qualunque sia la durata |
| `[Playback] ExactSeekThresholdSeconds` | `30` | I salti più corti sono sempre esatti; quelli più lunghi vanno al fotogramma chiave più vicino (rapidi, scarto di qualche secondo) |
| `[Seek] LeftRightSeconds` | `1` | Durata del salto di ← / → |
| `[Seek] UpDownSeconds` | `5` | Durata del salto di ↓ / ↑ |
| `[Seek] WheelSeconds` | `3` | Durata del salto della rotella sopra il video |
| `[Seek] PageSeconds` | `60` | Durata del salto di PgDown / PgUp |
| `[Seek] AltPageSeconds` | `300` | Durata del salto di Alt + PgDown / PgUp |
| `[Cache] MaxMB` | `0` | 0 = cache di mpv disattivata; >0 = cache attiva con questo limite |
| `[Gallery] PositionsPercent` | `10,30,50,70,90` | Posizioni delle immagini della galleria, in % della durata di ogni video (da 1 a 10 valori) |
| `[Gallery] Jobs` | `2` | Esecuzioni di ffmpeg contemporanee per la galleria (1–8; di più è più rapido su SSD, può essere più lento sul NAS) |
| `[Diagnostics] MetricsLog` | `true` | Registro delle misure `viBro-metrics.csv`; `false` = il file non viene creato né scritto |
| `[Delete] ConfirmFromVideoMenu` | `false` | `false` = *Delete* nel menu del tasto destro sul video elimina un file senza conferma (anche in rete, dove è definitiva); più file chiedono sempre conferma. `true` = conferma sempre |
| `[Appearance] Theme` | `System` | `Light`, `Dark`, oppure `System` (segue Windows finché non si sceglie con il pulsante *Theme*) |

## Scorciatoie da tastiera

Le durate dei salti sono quelle predefinite; si cambiano in `[Seek]` di `viBro.ini` (1.4.0) e Ctrl+F2 mostra i valori in uso.

I tasti qui sotto sono quelli predefiniti. Dalla 1.10.0 ogni scorciatoia da tastiera della finestra principale si cambia in `[Keys]` di `viBro.ini` (a viBro chiuso): una riga per comando, `Comando=Tasto`, più tasti separati da virgole, modificatori `Ctrl+`, `Alt+`, `Shift+` (devono corrispondere esattamente: `Left` non reagisce a Shift+←), `none` per nessun tasto. I nomi dei tasti sono quelli di Windows/WPF (`Left`, `PageUp`, `Enter`, `Escape`, `Space`, `F5`, lettere, cifre `0`–`9`; anche `Del`, `Esc`, `PgUp`, `PgDn`). Un valore non valido dà un avviso nella barra di stato e il predefinito; un tasto dato a due comandi resta a quello che viene prima nell'elenco e l'altro lo perde, con un avviso. Ctrl+F2 mostra i tasti in uso. Mouse e rotella non sono configurabili.

| Comando | Predefinito | Nota |
|---|---|---|
| `SeekBackShort / SeekForwardShort` | `Left / Right` | [Seek] LeftRightSeconds |
| `SeekBackMedium / SeekForwardMedium` | `Down / Up` | [Seek] UpDownSeconds |
| `SeekBackLong / SeekForwardLong` | `PageDown / PageUp` | [Seek] PageSeconds |
| `SeekBackVeryLong / SeekForwardVeryLong` | `Alt+PageDown / Alt+PageUp` | [Seek] AltPageSeconds |
| `PlayPause` | `Space` |  |
| `PreviousVideo / NextVideo` | `E / D` |  |
| `Mute` | `M` |  |
| `Fullscreen / ExitFullscreen` | `F11 / Escape` | ExitFullscreen solo a schermo intero |
| `FocusPath` | `Ctrl+L` |  |
| `SelectAll` | `Ctrl+A` |  |
| `Refresh` | `F5` |  |
| `OpenFolder` | `Enter` | solo con una cartella selezionata |
| `OpenWithDefaultApp` | `Shift+Enter` |  |
| `Delete` | `Delete,R` |  |
| `Rename` | `F2` |  |
| `Move / MoveToLast` | `W / Shift+W` |  |
| `Copy / CopyToLast` | `C / Shift+C` |  |
| `MarkIn / MarkOut` | `I / O` |  |
| `Extract` | `F9` |  |
| `Gallery` | `F10` |  |
| `Commands` | `Ctrl+F2` |  |
| `Close` | `Ctrl+W` | anche mentre si scrive un testo |


| Tasto | Azione | Da |
|---|---|---|
| ← / → | Indietro / avanti di circa 1 s | ✔ |
| ↓ / ↑ | Indietro / avanti di circa 5 s | ✔ |
| PgDown / PgUp | Indietro / avanti di circa 1 min | ✔ |
| Alt + PgDown / Alt + PgUp | Indietro / avanti di circa 5 min | ✔ |
| Spazio | Play/Pause (alla fine del video riparte dall'inizio) | ✔ |
| E / D | Video precedente / successivo (Play/Pause conservato), senza ricominciare dall'estremità opposta | ✔ |
| M | Mute on/off | ✔ |
| F11 / Esc | Entra in / esce da schermo intero | ✔ |
| Ctrl + L | Attiva la barra del percorso (Invio apre, Esc annulla) | ✔ |
| Ctrl + A | Seleziona tutti i video della cartella (il video caricato non cambia) | 1.7.0 |
| F5 | Aggiorna dal disco l'albero delle cartelle e l'elenco dei file (file e cartelle nuovi o eliminati, dimensioni e date), conservando cartelle aperte, selezione, video caricato e posizione di scorrimento | 1.8.0 |
| Invio | Apre la cartella selezionata nell'elenco (`..` = cartella superiore) | 1.3.0 |
| Shift + Invio | Apre il video selezionato con l'applicazione predefinita di Windows (viBro va in pausa) | 1.3.0 |
| Canc o R | Elimina i file selezionati con una sola conferma (Cestino se locale, definitiva se in rete) | 0.3.0 (più file 1.7.0) |
| F2 | Rinomina il file (un solo video selezionato; solo il nome; l'estensione resta; il cursore parte dall'inizio del nome) | 0.3.0 |
| W | Sceglie la cartella di destinazione e sposta i file selezionati | 0.3.0 (più file 1.7.0) |
| Shift + W | Sposta nell'ultima destinazione usata | 0.3.0 |
| C | Sceglie la cartella di destinazione e copia i file selezionati (stesse regole dello spostamento; gli originali restano) | 0.6.0 (più file 1.7.0) |
| Shift + C | Copia nell'ultima destinazione di copia (separata da quella dello spostamento) | 0.6.0 |
| I / O | Marker di inizio / fine dell'estratto nella posizione corrente (barra di stato; a schermo intero anche sopra il video) | 1.5.0 |
| F9 | Salva la parte tra I e O come nuovo file nella stessa cartella, `nome-extract-aaMMgg-OOmm.ext` (es. `clip.mp4` → `clip-extract-261104-1445.mp4`; `-2`, `-3` … se esiste già). Richiede `ffmpeg.exe` accanto a `viBro.exe` | 1.5.0 |
| F10 | Galleria dei video della cartella in una finestra separata (Esc la chiude). Richiede `ffmpeg.exe` accanto a `viBro.exe` | 1.6.0 |
| Ctrl + F2 | Mostra la finestra dei comandi (tastiera e mouse) | 1.2.0 |
| Ctrl + W | Chiude viBro | 0.6.0 |

## Mouse sul video

| Azione | Effetto | Da |
|---|---|---|
| Clic | Play/Pause (il clic che riporta viBro in primo piano non cambia la riproduzione) | 0.5.0 |
| Doppio clic | Entra in / esce da schermo intero; la riproduzione resta com'era | 0.5.0 |
| Tasto destro | Menu: *Delete* (Canc/R), *Move...* (W), *Move to <ultima cartella>* (Shift+W), *Copy...* (C), *Copy to <ultima cartella>* (Shift+C); con più file selezionati, es. *Delete 3 files*. Dalla 1.8.0 *Delete* su un solo file non chiede conferma (`[Delete] ConfirmFromVideoMenu`) | 0.5.0 (copia 0.6.0, più file 1.7.0) |
| Tasto destro tenuto + trascinamento a destra/sinistra | Spostamento nel video: tutta la larghezza del video = tutta la durata; il video segue il mouse, salto esatto al rilascio (nessun menu) | 0.6.0 |
| Rotella sopra la barra temporale | Su: video successivo; giù: video precedente (come D / E), a finestra normale e a schermo intero | 1.3.0 (invertita 1.9.0) |
| Tasto destro tenuto + rotella sul video | Su: video successivo; giù: video precedente, come sulla barra; al rilascio del tasto il menu non compare | 1.9.0 |
| Rotella sopra il video (a schermo intero: ovunque tranne la barra temporale) | Su: avanti 3 s; giù: indietro 3 s (`[Seek] WheelSeconds`) | 1.3.0 |

Dalla 1.9.0 la rotella su video e barra temporale funziona anche quando viBro non è la finestra in primo piano, se Windows manda la rotella alla finestra sotto il puntatore (Impostazioni → Bluetooth e dispositivi → Mouse → *Scorri le finestre inattive al passaggio del mouse*, attiva per impostazione predefinita). Non fa nulla mentre è aperta una finestra di dialogo di viBro.
| Clic sulla barra temporale e trascinamento | Spostamento nel video; l'indicatore segue il mouse | 0.4.0 |

Le scorciatoie restano sospese mentre si scrive nella barra del percorso. Come da specifica, le frecce eseguono sempre i salti, anche con lo stato attivo sull'albero o sull'elenco: la navigazione nell'albero avviene con il mouse.

## Uso

```
viBro.exe [cartella] [--exact-seeks] [--trace-input] [--mpv-opt nome=valore]...
```

- `cartella`: cartella da aprire all'avvio.
- `--trace-input`: diagnostica; scrive in `viBro-errors.log` ogni tasto e ogni messaggio della rotella ricevuti da viBro, con la finestra di destinazione.
- `--exact-seeks`: rende esatti anche i salti lunghi e la barra temporale per questa sessione (non viene salvato; l'impostazione permanente è `ExactLongSeeks` in `viBro.ini`).
- `--mpv-opt nome=valore`: sovrascrive un'opzione di mpv. Serve per le prove di prestazione, per esempio `--mpv-opt cache=yes --mpv-opt demuxer-max-bytes=64MiB`, oppure `--mpv-opt log-file=C:\temp\mpv.log` per la diagnostica.

Al primo avvio .NET estrae le librerie native, tra cui libmpv, nella cache per utente `%TEMP%\.net\viBro\<hash>`. Gli avvii successivi riutilizzano la cache; una nuova build usa un nuovo hash.

## Come ottenere viBro

Non viene pubblicato un EXE pronto: si compila con `build.cmd` (pochi minuti, senza privilegi di amministratore). Il motivo è la GPL: l'EXE incorpora una build di libmpv di terzi di cui non si può fornire il sorgente esatto, perché quella build non registra le revisioni delle librerie che contiene. Compilando in locale, libmpv viene scaricata direttamente da chi la pubblica. Vedi [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

La pagina [Releases](https://github.com/rives1/viBro/releases) (dalla 1.6.0) elenca ogni versione con le note e l'archivio del sorgente da compilare; viene creata automaticamente quando si pubblica il tag di una versione ([docs/RELEASE.md](docs/RELEASE.md)).

## Compilazione

Requisiti: Windows 10/11 x64 e connessione a Internet per il primo download. Non servono privilegi di amministratore.

```
build.cmd
```

Lo script:
1. usa il .NET 10 SDK, se presente; altrimenti, con `-InstallDotnet`, lo installa per l'utente corrente in `.dotnet\`;
2. scarica la build di libmpv fissata in `build/dependencies.json`, ne verifica lo SHA-256 ed estrae `libmpv-2.dll` in `deps\libmpv\`;
3. pubblica `artifacts\publish\viBro.exe` e controlla che sia l'unico file prodotto;
4. crea `dist\viBro-<versione>-win-x64.zip` con il relativo `.sha256`.

Comandi singoli: `build\build.ps1 [-Compress] [-DepsOnly]` e `build\package.ps1`. La variante predefinita è l'EXE normale; `-Compress` produce un EXE più piccolo (−58%) ma più lento ad avviarsi (+165 ms) e con circa 70 MB di memoria in più (misure in `docs/DECISIONS.md`, D-047). Su GitHub, il workflow `.github/workflows/build.yml` esegue gli stessi script come verifica della compilazione; non pubblica nulla.

La versione si trova **solo** in `Directory.Build.props` e viene riportata nel titolo della finestra e nelle proprietà dell'EXE.

## Misure

```
powershell -ExecutionPolicy Bypass -File build\analyze-metrics.ps1 -Path <cartella-exe>\viBro-metrics.csv
```

Il protocollo di prova è descritto in [docs/TEST-PLAN-0.1.0.md](docs/TEST-PLAN-0.1.0.md).

## Licenza

GPL-3.0-or-later (vedi [LICENSE](LICENSE)). L'EXE incorpora una build GPL di libmpv il cui FFmpeg è compilato con `--enable-gpl --enable-version3`, quindi l'EXE distribuito nel suo insieme è sotto GPL versione 3. Vedi [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

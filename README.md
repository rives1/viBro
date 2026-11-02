# viBro

viBro è un'applicazione desktop per Windows 11 x64 che serve a esplorare cartelle di video, vederli all'istante e organizzarli (eliminare, rinominare, spostare). La priorità è la reattività: il primo fotogramma deve comparire subito, il passaggio da un video all'altro deve essere immediato e i salti temporali rapidi, anche su NAS.

Tecnologie: C#/WPF (.NET 10) per l'interfaccia, [libmpv](https://mpv.io) per la decodifica e la riproduzione. Il programma si distribuisce come un unico `viBro.exe` portabile.

> **Stato: 0.3.0 in sviluppo** (operazioni sui file); ultima versione rilasciata: 0.2.0. La 0.1.0 ha validato la tecnologia su SSD e NAS. Il percorso fino alla 1.0.0 è descritto in [SPEC.md](SPEC.md).

## Funzionalità

- Un unico EXE autonomo: nessuna installazione, nessun privilegio di amministratore, nessun codec da installare.
- Barra del percorso: cartelle locali, unità mappate e percorsi UNC (`\\server\share`). L'apertura avviene in background, quindi una condivisione irraggiungibile non blocca l'interfaccia.
- Elenco dei soli video della cartella (MP4, MKV, MOV, WMV), senza ricorsione, in ordine naturale dei nomi (lo stesso di Esplora risorse).
- Il video selezionato viene caricato **in pausa**, mostrando il primo fotogramma; la selezione non avvia mai la riproduzione.
- Salti: ±1 s e ±5 s esatti, ±1 min e ±5 min su fotogrammi chiave (rapidi, approssimati) oppure esatti con *Exact long seeks*. Le richieste ripetute vengono accorpate, quindi non si accumula una coda.
- Play/Pause, barra temporale, volume, mute, schermo intero.
- **Misure automatiche** in `viBro-metrics.csv`, accanto all'EXE: tempo al primo fotogramma, cambio video, latenza dei salti, memoria.

- Albero delle cartelle (cartelle utente, unità locali e di rete, condivisioni UNC aperte), caricato progressivamente e sincronizzato con la cartella aperta.
- Ordinamento per Name, Size, Date modified; tre riquadri ridimensionabili; impostazioni in `viBro.ini`.

- Eliminazione, rinomina e spostamento dei file, con conferme, avanzamento e annullamento.

Non ancora presenti: temi Light/Dark (0.4.0).

## viBro.ini

Si trova accanto a `viBro.exe` e viene scritto alla chiusura. Va modificato solo a programma chiuso. Un valore non valido viene sostituito dal predefinito e segnalato nella barra di stato. Oltre alla finestra, ai riquadri, alle colonne, all'ultima cartella e all'audio, che il programma salva da solo, contiene:

| Sezione / chiave | Predefinito | Significato |
|---|---|---|
| `[Playback] HardwareDecoding` | `auto-safe` | Decodifica hardware (`no` = solo software, oppure un valore `hwdec` di mpv) |
| `[Playback] ExactLongSeeks` | `false` | Rende esatti anche ±1/±5 min e la barra temporale |
| `[Playback] ExactSeekThresholdSeconds` | `30` | I salti più brevi di questa soglia sono sempre esatti |
| `[Cache] MaxMB` | `0` | 0 = cache di mpv disattivata; >0 = cache attiva con questo limite |
| `[Diagnostics] MetricsLog` | `true` | Registro delle misure `viBro-metrics.csv` |

## Scorciatoie da tastiera

| Tasto | Azione | Da |
|---|---|---|
| ← / → | Indietro / avanti di circa 1 s | ✔ |
| ↓ / ↑ | Indietro / avanti di circa 5 s | ✔ |
| PgDown / PgUp | Indietro / avanti di circa 1 min | ✔ |
| Alt + PgDown / Alt + PgUp | Indietro / avanti di circa 5 min | ✔ |
| Spazio | Play/Pause (alla fine del video riparte dall'inizio) | ✔ |
| E / D | Video precedente / successivo, in pausa, senza ricominciare dall'estremità opposta | ✔ |
| M | Mute on/off | ✔ |
| F11 / Esc | Entra in / esce da schermo intero | ✔ |
| Ctrl + L | Attiva la barra del percorso (Invio apre, Esc annulla) | ✔ |
| Canc o R | Elimina il file con conferma (Cestino se locale, definitiva se in rete) | 0.3.0 |
| F2 | Rinomina il file (solo il nome; l'estensione resta) | 0.3.0 |
| W | Sceglie la cartella di destinazione e sposta il file | 0.3.0 |
| Shift + W | Sposta nell'ultima destinazione usata | 0.3.0 |

Le scorciatoie restano sospese mentre si scrive nella barra del percorso. Come da specifica, le frecce eseguono sempre i salti, anche con lo stato attivo sull'albero o sull'elenco: la navigazione nell'albero avviene con il mouse.

## Uso

```
viBro.exe [cartella] [--exact-seeks] [--mpv-opt nome=valore]...
```

- `cartella`: cartella da aprire all'avvio.
- `--exact-seeks`: rende esatti anche i salti lunghi e la barra temporale per questa sessione (non viene salvato; l'impostazione permanente è `ExactLongSeeks` in `viBro.ini`).
- `--mpv-opt nome=valore`: sovrascrive un'opzione di mpv. Serve per le prove di prestazione, per esempio `--mpv-opt cache=yes --mpv-opt demuxer-max-bytes=64MiB`, oppure `--mpv-opt log-file=C:\temp\mpv.log` per la diagnostica.

Al primo avvio .NET estrae le librerie native, tra cui libmpv, nella cache per utente `%TEMP%\.net\viBro\<hash>`. Gli avvii successivi riutilizzano la cache; una nuova build usa un nuovo hash.

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

Comandi singoli: `build\build.ps1 [-Compress] [-DepsOnly]` e `build\package.ps1`. Su GitHub, il workflow `.github/workflows/build.yml` esegue gli stessi script.

La versione si trova **solo** in `Directory.Build.props` e viene riportata nel titolo della finestra e nelle proprietà dell'EXE.

## Misure

```
powershell -ExecutionPolicy Bypass -File build\analyze-metrics.ps1 -Path <cartella-exe>\viBro-metrics.csv
```

Il protocollo di prova è descritto in [docs/TEST-PLAN-0.1.0.md](docs/TEST-PLAN-0.1.0.md).

## Licenza

GPL-3.0-or-later (vedi [LICENSE](LICENSE)). L'EXE incorpora libmpv in una build GPL; vedi [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

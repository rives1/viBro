# viBro

viBro è un'applicazione desktop per Windows 11 x64 che serve a esplorare cartelle di video, vederli all'istante e organizzarli (eliminare, rinominare, spostare). La priorità è la reattività: il primo fotogramma deve comparire subito, il passaggio da un video all'altro deve essere immediato e i salti temporali rapidi, anche su NAS.

Tecnologie: C#/WPF (.NET 10) per l'interfaccia, [libmpv](https://mpv.io) per la decodifica e la riproduzione. Il programma si distribuisce come un unico `viBro.exe` portabile.

> **Stato: prototipo tecnico 0.1.0.** Serve a confermare la scelta tecnologica su file reali (SSD e NAS) prima di sviluppare l'interfaccia completa. Il percorso fino alla 1.0.0 è descritto in [SPEC.md](SPEC.md).

## Contenuto della 0.1.0

- Un unico EXE autonomo: nessuna installazione, nessun privilegio di amministratore, nessun codec da installare.
- Barra del percorso: cartelle locali, unità mappate e percorsi UNC (`\\server\share`). L'apertura avviene in background, quindi una condivisione irraggiungibile non blocca l'interfaccia.
- Elenco dei soli video della cartella (MP4, MKV, MOV, WMV), senza ricorsione, in ordine naturale dei nomi (lo stesso di Esplora risorse).
- Il video selezionato viene caricato **in pausa**, mostrando il primo fotogramma; la selezione non avvia mai la riproduzione.
- Salti: ±1 s e ±5 s esatti, ±1 min e ±5 min su fotogrammi chiave (rapidi, approssimati) oppure esatti con *Exact long seeks*. Le richieste ripetute vengono accorpate, quindi non si accumula una coda.
- Play/Pause, barra temporale, volume, mute, schermo intero.
- **Misure automatiche** in `viBro-metrics.csv`, accanto all'EXE: tempo al primo fotogramma, cambio video, latenza dei salti, memoria.

Non ancora presenti: albero delle cartelle, ordinamento per colonna, `viBro.ini` (versione 0.2.0); eliminazione, rinomina e spostamento (0.3.0); temi (0.4.0).

## Scorciatoie da tastiera

| Tasto | Azione | 0.1.0 |
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
| Canc o R, F2, W, Shift + W | Elimina, rinomina, sposta | 0.3.0 |

Le scorciatoie restano sospese mentre si scrive nella barra del percorso.

## Uso

```
viBro.exe [cartella] [--exact-seeks] [--mpv-opt nome=valore]...
```

- `cartella`: cartella da aprire all'avvio.
- `--exact-seeks`: rende esatti anche i salti lunghi e la barra temporale (equivale alla casella *Exact long seeks*).
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

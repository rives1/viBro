# CONTEXT — stato del progetto per riprendere il lavoro

_Aggiornato: 2026-11-02 · versione in sviluppo: 0.1.0 (prototipo tecnico) · branch: `main`_

## Obiettivo corrente
Prototipo 0.1.0. Deve dimostrare che l'EXE è autonomo, che il video si carica in pausa e che la navigazione è abbastanza rapida sui file reali (SSD e NAS). Solo dopo questa verifica si passa alla 0.2.0 (interfaccia completa). Specifica: [SPEC.md](SPEC.md).

## Funzionalità completate (codice scritto)
- Progetto WPF .NET 10 `src/viBro`, pubblicazione come EXE singolo e self-contained, con libmpv incorporata.
- `Mpv/MpvPlayer.cs`: un solo motore, comandi asincroni, caricamento in pausa, salti assoluti accorpati, misure basate su `playlist_entry_id` e `PLAYBACK_RESTART`.
- `Mpv/MpvHost.cs`: finestra nativa figlia per l'opzione `wid` di mpv.
- `MainWindow`: barra del percorso (UNC e unità mappate, lettura in background), elenco virtualizzato in ordine naturale, controlli di trasporto, scorciatoie, schermo intero, registro `viBro-metrics.csv`.
- Script `build/build.ps1`, `build/package.ps1`, `build/analyze-metrics.ps1`, `build.cmd`; workflow `.github/workflows/build.yml`.

## Verifiche eseguite (2026-11-02, in un ambiente Linux, non su Windows)
| Verifica | Esito |
|---|---|
| Download della build libmpv fissata, SHA-256 calcolato, contenuto ispezionato (DLL x64, header) | ✔ |
| Enum, strutture e funzioni P/Invoke confrontati con il `client.h` reale (API 2.6) | ✔ |
| Compilazione con .NET SDK 10.0.114 del codice non-WPF (`Mpv/*`, `Util/*`) | ✔ 0 errori, 0 avvisi |
| `MpvPlayer` eseguito con libmpv per Linux (vo/ao null) su video generati: 12 scenari (caricamento in pausa, salti in pausa e in riproduzione, cambio da riproduzione, 20 cambi rapidi, file mancante, ripresa, fine video, Play alla fine) | ✔ 12/12 |
| Accorpamento dei salti: 30 richieste ravvicinate → 4 salti effettivi, posizione finale corretta | ✔ |
| Parsing degli script PowerShell; `analyze-metrics.ps1` su dati di prova | ✔ |
| Compilazione completa e pubblicazione su Windows (SDK 10.0.403), commit `aa0695f` | ✔ 2026-11-02: EXE singolo 255,9 MB, nessun file accessorio, versione 0.1.0+aa0695f |
| Esecuzione su Windows: motore avviato, primo fotogramma in pausa, tastiera (anche col focus sul video), NAS | ✔ 2026-11-02, verificato dall'utente |
| Misure su NAS (unità mappata, d3d11va, gpu-next), ~1100 campioni | ✔ cambio video mediana 140 ms (90%: 269 ms); salto esatto ±1 s 22 ms, ±5 s 35–66 ms; ±1 min su fotogrammi chiave 16 ms (90%: 140 ms); ±5 min 368 ms; memoria privata 300–470 MB, stabile |
| Misure su disco locale | parziali: salti esatti ±1/±5 s 18–19 ms, cambio video 74–211 ms (4 campioni) |
| Modalità mista dei salti (commit d540d1f), ~1140 campioni, NAS + locale | ✔ salti esatti: scarto dal bersaglio ≈ 0 s; locale ±1 s 32 ms, ±5 s 45 ms (mediana); NAS ±1 s 20 ms (90%: 99), ±5 s 56 ms (90%: 134). Fotogrammi chiave ±1/±5 min: 10–35 ms, scarto 2–9 s. Cambio video locale 38 ms, NAS 291 ms (90%: 647, max 1175: più variabile della prima sessione). Memoria 260–460 MB, stabile |
| Colonna `landed_s` | ✘→✔ la prima versione registrava la posizione del salto successivo quando c'era una richiesta in coda; corretto (lettura prima di avviare il salto in coda) |

Il test con libmpv ha trovato e fatto correggere un bug: con i cambi rapidi di file il rilevamento del caricamento si bloccava (vedi D-008).

## Problemi noti / rischi aperti
1. EXE di 255,9 MB (libmpv 121 MB + runtime .NET/WPF + ReadyToRun): valutare `build.ps1 -Compress` confrontando dimensione e tempo di avvio.
2. `libmpv-2.dll` importa `vulkan-1.dll` in modo statico: può non avviarsi su VM o Sandbox senza driver GPU (vedi D-002).
3. ~~Salti `keyframes` brevi~~: confermato sui file reali che non spostano l'immagine; risolto con la modalità mista (D-007). Da verificare dopo la ricompilazione.
4. shinchiro conserva su GitHub solo ~30 build: la build fissata sparirà da GitHub verso dicembre 2026 e resterà disponibile il link SourceForge. Conviene archiviare l'archivio (D-002).
5. Gli obblighi di licenza GPL (sorgente corrispondente di libmpv) vanno risolti prima del rilascio pubblico: vedi THIRD-PARTY-NOTICES.md.
6. Il tag `v0.1.0` non è ancora stato creato.

## Dipendenze
| Dipendenza | Versione | Dove è fissata |
|---|---|---|
| .NET SDK | 10.0.x | `build/dependencies.json` (nessun `global.json`) |
| libmpv (shinchiro) | 20261101-git-48fca90e8c, SHA-256 `30d3f9fa…ef843` | `build/dependencies.json` |
| 7zr.exe (solo build, solo ripiego) | 26.05, SHA-256 `9bf582b8…143b7` | `build/dependencies.json` |

## Comandi di compilazione
```
build.cmd          :: SDK per utente in .dotnet\, libmpv, publish, zip
build\build.ps1 -Compress         :: variante con EXE compresso (confronto)
build\analyze-metrics.ps1 -Path <csv>
```
Uscite: `artifacts\publish\viBro.exe`, `dist\viBro-0.1.0-win-x64.zip`.

## Riferimento al codice
Ultimo commit compilato con successo su Windows: `aa0695f4d79a0dc9b3edf826d2a38dc9b2b93b87` (non ancora testato in esecuzione).

## Prossimo passo
1. ~~Compilare su Windows~~ fatto (aa0695f).
2. ~~Verificare la modalità mista~~ fatto. Completare le misure su SSD locale, 4K, WMV/MOV e una sessione lunga per la memoria ([docs/TEST-PLAN-0.1.0.md](docs/TEST-PLAN-0.1.0.md)).
3. Riportare qui i risultati, aggiornare le decisioni [da verificare], creare il tag `v0.1.0` e la fotografia `docs/handover/CONTEXT-v0.1.0.md`.
4. Poi la 0.2.0: albero delle cartelle, ordinamento per colonna, `viBro.ini`.

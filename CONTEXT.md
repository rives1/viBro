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
| **Compilazione WPF (`MainWindow`, `MpvHost`, `App`) e pubblicazione dell'EXE** | ✘ non eseguita: gli assembly di riferimento WPF arrivano da NuGet, irraggiungibile da quell'ambiente |
| **Esecuzione su Windows, rendering D3D11, decodifica hardware, NAS** | ✘ da eseguire |

Il test con libmpv ha trovato e fatto correggere un bug: con i cambi rapidi di file il rilevamento del caricamento si bloccava (vedi D-008).

## Problemi noti / rischi aperti
1. La parte WPF non è mai stata compilata: possibili piccoli errori al primo `build.cmd`.
2. `libmpv-2.dll` importa `vulkan-1.dll` in modo statico: può non avviarsi su VM o Sandbox senza driver GPU (vedi D-002).
3. Salti `keyframes` da ±1 s su GOP lunghi: il test conferma che un salto di -1 s può tornare al fotogramma chiave precedente, anche diversi secondi indietro. Va valutato sui file reali (D-007).
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
Ultimo commit locale su `main`: vedere `git log -1`. Al primo `build.cmd` riuscito su Windows, annotare qui l'hash del commit compilato e testato.

## Prossimo passo
1. Eseguire `build.cmd` sul PC Windows e correggere eventuali errori di compilazione WPF.
2. Eseguire [docs/TEST-PLAN-0.1.0.md](docs/TEST-PLAN-0.1.0.md) su SSD e NAS.
3. Riportare qui i risultati, aggiornare le decisioni [da verificare], creare il tag `v0.1.0` e la fotografia `docs/handover/CONTEXT-v0.1.0.md`.
4. Poi la 0.2.0: albero delle cartelle, ordinamento per colonna, `viBro.ini`.

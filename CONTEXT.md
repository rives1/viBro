# CONTEXT — viBro: stato per riprendere il lavoro

_Aggiornato: 2026-11-03 · branch `main` · ultimo commit `cda48b2` · ultima versione rilasciata **0.6.0** (tag `v0.6.0`) · **prossima versione: contenuto da definire**_

## 1. Il progetto in breve
**viBro** è un'applicazione desktop per Windows 11 x64 per sfogliare cartelle di video, vederli all'istante (caricamento **in pausa**, salti rapidi) e organizzarli (elimina, rinomina, sposta). È distribuita come un unico `viBro.exe` portabile, senza installazione né privilegi di amministratore. Tecnologie: C#/WPF su .NET 10 e libmpv. Licenza GPL-3.0-or-later. Specifica approvata in [SPEC.md](SPEC.md); decisioni D-001…D-049 in [docs/DECISIONS.md](docs/DECISIONS.md).

- Cartella sul PC dell'utente: `C:\Users\ammin\Documents\Claude_vibro\viBro`. Il remote GitHub è `https://github.com/rives1/viBro`; il ramo locale ha commit e i tag `v0.3.0`, `v0.5.0` e `v0.6.0` forse non ancora pubblicati (`git push --follow-tags`), e il push lo fa l'utente.
- Compilazione: `build.cmd`, che installa per l'utente il .NET SDK 10 in `.dotnet\`, scarica e verifica libmpv, pubblica `artifacts\publish\viBro.exe` e crea lo zip in `dist\`. `build\build.ps1 -Compress` produce la variante compressa in `artifacts\publish-compressed`. **Variante predefinita: EXE normale** (D-047: avvio 410 ms contro 575, ~70 MB di memoria in meno).
- Versione: in un solo punto, `Directory.Build.props` (ora `0.6.0`). Appare nel titolo e nelle proprietà dell'EXE come `0.6.0+<commit>`.

## 2. Versioni
| Versione | Contenuto | Stato |
|---|---|---|
| 0.1.0 | Prototipo: EXE singolo, motore mpv persistente nella finestra nativa (`wid`), caricamento in pausa, salti (±1/±5 s esatti; ±1/±5 min su fotogrammi chiave), accorpamento dei salti, misure CSV | ✔ tag `v0.1.0`, misurato su SSD e NAS |
| 0.2.0 | Albero delle cartelle progressivo e sincronizzato, ordinamento per colonna (naturale), 3 riquadri, `viBro.ini` (atomico, valori predefiniti, avvisi) | ✔ tag `v0.2.0` |
| 0.3.0 | Elimina (Canc/R: Cestino locale, definitiva in rete), rinomina (F2), sposta (W/Shift+W, conflitti, avanzamento e annullamento, copia verificata prima di eliminare la sorgente) | ✔ tag `v0.3.0` |
| 0.4.0 | Temi Light/Dark (Fluent + pulsante *Theme*), barra dei controlli **sopra** il video, fascia a schermo intero con barra temporale centrata al 40% che si nasconde da sola, `viBro-errors.log`, loader Vulkan incorporato, Cestino per percorsi di 260+ caratteri, misura del tempo di avvio, icona, icone dell'albero, righe compatte, clic e trascinamento sui cursori | ✔ confluita nella 0.5.0 (D-039) |
| 0.5.0 | Mouse sul video: clic per Play/Pause, doppio clic per lo schermo intero, menu del tasto destro (Delete, Move..., Move to <ultima cartella>), rotella a schermo intero (giù = successivo), rotella sulla barra temporale (±5 s), `--trace-input` | ✔ tag `v0.5.0` (2026-11-03): prove superate tranne i tempi di avvio (spostati nella 0.6.0) |
| 0.6.0 | Play/Pause conservato al cambio di file, copia (C/Shift+C e menu), trascinamento col tasto destro per spostarsi nel video, rotella sul video anche a finestra normale, rotella sulla barra ±3 s, barra di stato compatta con numero e dimensione dei video, fascia in basso a schermo intero, conferma di eliminazione vicino al puntatore, Ctrl+W | ✔ tag `v0.6.0` (2026-11-03): tutte le prove superate |
| 1.0.0 | Prima versione completa e verificata | da fare |

## 3. Misure chiave (0.1.0, NAS con unità mappata, d3d11va)
- Cambio video: mediana 140–500 ms sul NAS (MP4 più lenti per la lettura dell'indice; MKV 66–120 ms), ~38 ms in locale. viBro aggiunge pochi ms rispetto a mpv (verificato con il log di mpv).
- Salti esatti: ±1 s mediana ~20 ms, ±5 s 35–66 ms sul NAS. Salti sui fotogrammi chiave: ±1 min 10–35 ms, con scarto di 2–9 s.
- 0.6.0 sull'unità di rete K: (D-049): apertura/cambio mediana 92 ms MKV HEVC 1080p, 102 ms MP4 H.264 1080p, 133 ms MOV H.264, 93 ms MP4 H.264 4K; salti rapidi 34 ms (1080p) e 54 ms (4K), esatti 52 ms (1080p) e 87 ms (4K). Avvio EXE 410 ms (D-047).
- Memoria: 260–470 MB, stabile. I salti sui fotogrammi chiave per ±1/±5 s **non spostavano l'immagine** (GOP di 5–24 s): da qui la modalità mista (D-007).

## 4. Stato attuale e prossimo passo
1. **0.6.0 rilasciata il 2026-11-03** (tag `v0.6.0`, D-048): tutte le prove di [docs/TEST-PLAN-0.6.0.md](docs/TEST-PLAN-0.6.0.md) superate. Contenuto: Play/Pause conservato al cambio di file (D-043), copia (D-044), trascinamento col tasto destro (D-045), rotella sul video e ±3 s sulla barra, barra di stato compatta con conteggio, fascia in basso a schermo intero, conferma vicino al puntatore, Ctrl+W (D-046).
2. EXE distribuito: **normale** (D-047, misure di avvio: 410 ms contro 575 del compresso). D-032 chiusa.
3. Nessuna prova aperta. Il prossimo piano di prova nasce con la prossima versione (`docs/TEST-PLAN-0.7.0.md` o `1.0.0`).
4. Prossimo passo: l'utente definisce la prossima versione. Punti aperti verso la 1.0.0: 4K HEVC (10 bit/HDR), MOV non H.264 e WMV non ancora provati (4K H.264 e MOV H.264 provati il 2026-11-03, D-049); sessione di memoria lunga (50+ cambi, con il 4K la memoria arriva a ~550 MB di lavoro / ~790 MB privata); picchi sul NAS (fino a 2 s, non riprodotti); obblighi GPL per libmpv prima di pubblicare binari (THIRD-PARTY-NOTICES); autore dei commit provvisorio (`viBro developer`); README in italiano, da tradurre prima della pubblicazione. Rotella: causa del primo comportamento della 0.5.0 non identificata (D-041), `--trace-input` disponibile.

## 5. Dipendenze fissate
| Dipendenza | Versione | Dove |
|---|---|---|
| .NET SDK / runtime | 10.0.x (usato 10.0.403 / runtime 10.0.14) | `build/dependencies.json` |
| libmpv (build shinchiro) | `20261101-git-48fca90e8c`, SHA-256 `30d3f9fa…ef843`, API 2.6 | `build/dependencies.json`. GitHub conserva solo ~30 build: il ripiego è SourceForge |
| Loader Vulkan Khronos | 1.4.357.0, SHA-256 `8878cdc6…96f8` | `third_party/vulkan-loader/` (incluso nel repository, controllato da `build.ps1`) |
| 7zr.exe (solo build, ripiego) | 26.05 | `build/dependencies.json` |

## 6. Mappa del codice (`src/viBro`)
- `Mpv/MpvNative.cs` (P/Invoke, verificato su `client.h`); `Mpv/MpvPlayer.cs` (comandi asincroni, caricamenti tracciati per `playlist_entry_id`, salti accorpati, `ReleaseFileAsync`, messaggi di errore); `Mpv/MpvHost.cs` (finestra nativa figlia).
- `MainWindow.xaml(.cs)` (layout, albero, elenco, tastiera con hook `ThreadPreprocessMessage` per tasti e mouse sul video, schermo intero, temi, misure); `MainWindow.VideoMouse.cs` (clic, doppio clic, menu del tasto destro, trascinamento col tasto destro, rotella); `MainWindow.FileOps.cs` (elimina, rinomina, sposta, copia); `Dialogs.cs` (conferma di eliminazione posizionata, rinomina, conflitto, avanzamento).
- `Settings/IniFile.cs`, `Settings/AppSettings.cs` (`viBro.ini`, schema 1); `Tree/FolderNode.cs`, `Tree/FolderTreeModel.cs`.
- `Util/`: `FileOps` (operazioni sicure, Cestino tramite `SHFileOperation` / `IFileOperation`), `FolderScanner`, `NaturalComparer`, `ThemeManager`, `ErrorLog`, `NativePreload` (Vulkan), `MetricsLog`, `AppInfo`, `StartupOptions`, `NativeMethods`, `TreeIcons` (icone dell'albero + convertitore), `SliderDrag` (clic e trascinamento sui cursori).

## 7. Note operative per l'assistente (come si è lavorato)
- **Preferenze dell'utente:** risposte dirette e critiche, in italiano; spiegare il perché; fare domande prima di decidere i punti aperti (usate scelte multiple); rispondere solo con certezze elevate. Interfaccia in inglese.
- **Regole dell'utente (dal 2026-11-02):** a ogni modifica o versione aggiornare sempre `CONTEXT.md` (e la copia nel progetto claude.ai) e preparare tutto per il commit, che esegue l'assistente (il push lo fa l'utente); aggiornare sempre il piano di prova e spostarlo alla versione successiva finché l'utente non conferma che tutte le prove sono passate.
- **Accesso al PC:** shell sul PC tramite device_bash (`$HOME/mnt/Claude_vibro/...`), senza .NET né accesso a NuGet; i file si copiano con device_commit_files. Il PC dell'utente è aziendale: alcuni moduli PowerShell di script sono bloccati (per esempio `Get-FileHash` e `Compress-Archive`), quindi negli script si usa .NET direttamente. Gli stili Fluent di riferimento si leggono da `raw.githubusercontent.com/dotnet/wpf/release/10.0/src/Microsoft.DotNet.Wpf/src/Themes/PresentationFramework.Fluent/Styles/<Controllo>.xaml`. Per copiare file dal PC al container: archivio `.tgz` in `artifacts\tmp` (ignorato da Git) e device_stage_files, poi cancellarlo.
- **Git:** per i commit serve il permesso di cancellazione nella cartella, perché Git rimuove i propri `.lock` (chiedere con device_request_delete_permission). Senza permesso restano `.lock` da spostare con `mv`. Messaggi di commit con la riga `Co-Authored-By`; tag annotati solo dopo i test dell'utente.
- **Verifica senza Windows:** nel container cloud si può installare `dotnet-sdk-10.0` (apt) e compilare **l'intero progetto WPF** con `EnableWindowsTargeting`. Servono gli assembly di riferimento `Microsoft.WindowsDesktop.App.Ref/10.0.14` copiati dal `.dotnet\packs` dell'utente in `/usr/lib/dotnet/packs` (cartelle `ref` e `data`). Su una copia del csproj vanno tolti RID, SelfContained, SingleFile e R2R. Il motore si prova con `libmpv2` di Ubuntu, reindirizzando `libmpv-2.dll` verso `libmpv.so.2`.
- **Flusso per ogni versione:** domande, codice, compilazione e test su Linux, copia sul PC, documentazione (CHANGELOG, DECISIONS, CONTEXT, TEST-PLAN), commit; poi l'utente esegue `build.cmd` e il piano di prova; infine chiusura con data, fotografia `docs/handover/CONTEXT-vX.Y.Z.md` e tag.

## 8. Messaggio per riprendere in una nuova chat
> Progetto viBro nella cartella `C:\Users\ammin\Documents\Claude_vibro\viBro`. Leggi `CONTEXT.md`, `SPEC.md` e `docs/DECISIONS.md`. La 0.6.0 è rilasciata (tag `v0.6.0`); ti descrivo cosa voglio nella prossima versione.

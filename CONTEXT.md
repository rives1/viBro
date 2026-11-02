# CONTEXT — stato del progetto per riprendere il lavoro

_Aggiornato: 2026-11-02 · ultima versione: 0.3.0 (tag `v0.3.0`) · **in sviluppo: 0.4.0** · branch: `main`_

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

| Formati (sessione 3, NAS) | ✔ MP4 H.264, MKV HEVC fino a 1080p (d3d11va); WMV VC-1 (d3d11va); WMV3 (decodifica software: salti esatti 18–375 ms secondo risoluzione) |
| Memoria, 21 cambi video | ✔ 290–460 MB, oscillante, nessuna crescita progressiva |
| Diagnosi cambio video NAS con log mpv (15 cambi) | ✔ viBro aggiunge pochi ms (tempi viBro ≈ tempi mpv); MKV 66–120 ms; MP4 135–486 ms, dominati dalla lettura dell'indice MP4 (28–218 ms) e dall'apertura SMB (18–92 ms). Picchi di 2 s della sessione 2 non riprodotti |

## Esito 0.1.0
Le tre condizioni della specifica sono dimostrate: EXE autonomo funzionante, caricamento in pausa, navigazione rapida su SSD e NAS. La scelta WPF + libmpv è confermata.

## Problemi noti / punti aperti (rimandati)
1. **Non provati:** MOV e 4K (nessun file disponibile), avvio su PC senza driver GPU (rischio `vulkan-1.dll`, D-002), sessione lunga di 50 o più cambi.
2. Cambio video sul NAS variabile: picchi fino a 2,1 s nella sessione 2, non riprodotti con il log. Causa probabile esterna (dischi del NAS in risparmio energetico, accesso a freddo); da osservare.
3. EXE di 256 MB: valutare `build.ps1 -Compress` misurando l'avvio.
4. La build di libmpv sparirà da GitHub verso dicembre 2026 (resta il link SourceForge): archiviarne una copia (D-002).
5. Obblighi GPL (sorgente corrispondente di libmpv) da soddisfare prima del rilascio pubblico (THIRD-PARTY-NOTICES.md).
6. Autore dei commit provvisorio (D-017).

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
Codice verificato su Windows (compilato e misurato): commit `147b85f` (prodotto `0.1.0+147b85f…`). Tag `v0.1.0` sul commit di chiusura della documentazione, senza modifiche al codice rispetto a `147b85f`.

## 0.2.0 — esito (chiusa il 2026-11-02)
Funzionalità: albero delle cartelle (`Tree/`), `viBro.ini` (`Settings/`), layout a tre riquadri, ordinamento per colonna, persistenza. Decisioni D-018…D-021.

| Verifica | Esito |
|---|---|
| Parte non-WPF su Linux (.NET 10): compilazione e 19 test su INI, impostazioni, ordinamento naturale, percorsi, caricamento dei nodi | ✔ 0 avvisi, 19/19 |
| Compilazione su Windows del commit `13095f5` (prodotto `0.2.0+13095f5…`) | ✔ |
| [docs/TEST-PLAN-0.2.0.md](docs/TEST-PLAN-0.2.0.md) eseguito dall'utente su Windows (albero, sincronizzazione, UNC, ordinamento, riquadri, INI con valore non valido e file in sola lettura, schermo intero, regressioni 0.1.0) | ✔ tutti i punti superati |
| `viBro.ini` scritto dal programma: sezioni e valori attesi | ✔ |

Codice verificato: commit `13095f5`. Il tag `v0.2.0` è sul commit di chiusura della documentazione, senza modifiche al codice.

## 0.3.0 — esito (chiusa il 2026-11-02)
Funzionalità: eliminazione (Canc/R), rinomina (F2), spostamento (W/Shift+W). Codice in `Util/FileOps.cs`, `MpvPlayer.ReleaseFileAsync`, `Dialogs.cs`, `MainWindow.FileOps.cs`. Decisioni D-022…D-026.

| Verifica | Esito |
|---|---|
| Progetto completo (WPF e XAML compresi) compilato su Linux con gli assembly di riferimento WindowsDesktop 10.0.14 | ✔ |
| 35 test su `FileOps` (nomi, conflitti, spostamento con copia, annullamento, sovrascrittura esplicita, rilascio degli handle) | ✔ |
| Rilascio del file con libmpv reale | ✔ |
| Compilazione e confezionamento su Windows, commit `77f5882` (prodotto `0.3.0+77f5882…`, `dist\viBro-0.3.0-win-x64.zip`) | ✔ dopo la correzione di `Get-FileHash`/`Compress-Archive` |
| [docs/TEST-PLAN-0.3.0.md](docs/TEST-PLAN-0.3.0.md) eseguito dall'utente su Windows, con file di prova su disco locale e NAS (Cestino, eliminazione definitiva in rete, ultimo file, cartella svuotata, ripetizione dei tasti, rinomina con errori e conflitti, spostamento istantaneo e con avanzamento, annullamento, conflitti, destinazione irraggiungibile, regressioni) | ✔ tutti i punti superati |

Codice verificato: commit `77f5882`. Il tag `v0.3.0` è sul commit di chiusura della documentazione, senza modifiche al codice.

## 0.4.0 — stato
Codice scritto: temi (`Util/ThemeManager.cs`, `[Appearance] Theme`), barra dei controlli in alto e fascia a schermo intero, `Util/ErrorLog.cs` con gestori delle eccezioni ed errori di mpv e di riproduzione, loader Vulkan incorporato (`third_party/vulkan-loader`, `Util/NativePreload.cs`), Cestino per percorsi lunghi (`IFileOperation`), misura `startup` e `build.ps1 -Compress` in una cartella separata. Decisioni D-027…D-032.

Verificato fuori da Windows:
- progetto completo (WPF, XAML, tema) compilato; la DLL Vulkan risulta incorporata nell'output;
- motore con libmpv reale, 21 controlli, compreso l'inoltro dei messaggi di errore di mpv;
- test di `FileOps` ancora superati. Il test di annullamento è stato reso indipendente dalla velocità del disco: prima, con un disco veloce, la copia poteva finire prima dell'annullamento.

Da verificare su Windows: aspetto dei temi, fascia a schermo intero, avvio senza `vulkan-1.dll` di sistema, Cestino con percorso lungo, log degli errori, tempi di avvio delle due varianti (vedi [docs/TEST-PLAN-0.4.0.md](docs/TEST-PLAN-0.4.0.md)).

## Prossimo passo
1. Compilare la 0.4.0 (`build.cmd`, poi `build\build.ps1 -Compress`) ed eseguire [docs/TEST-PLAN-0.4.0.md](docs/TEST-PLAN-0.4.0.md).
2. Decidere la compressione (D-032), chiudere la 0.4.0, poi preparare la 1.0.0 (verifiche complete, licenze, pubblicazione).

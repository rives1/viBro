# Piano di prova del prototipo 0.1.0

Il prototipo deve dimostrare tre cose:
1. l'EXE è autonomo e funziona su un sistema pulito;
2. il video si carica in pausa, mostrando il primo fotogramma;
3. la navigazione è abbastanza rapida sui file reali, su SSD e su NAS.

Per scelta della specifica non sono fissati tempi garantiti: le soglie si decidono **dopo** aver letto le misure. Al termine si annotano i risultati in `CONTEXT.md` e nelle decisioni marcate **[da verificare]** di `docs/DECISIONS.md`.

## 0. Preparazione
- Compilare con `build.cmd` e annotare la dimensione dell'EXE e la versione stampata.
- Copiare `viBro.exe` in una cartella scrivibile, per esempio `C:\Tools\viBro\`. Il file `viBro-metrics.csv` verrà creato lì.
- Preparare file campione (non vanno nel repository):

| # | Contenitore | Codec video | Risoluzione | Note |
|---|---|---|---|---|
| 1 | MP4 | H.264 | 1080p | caso principale |
| 2 | MP4 o MKV | HEVC (8 o 10 bit) | 1080p | |
| 3 | MKV | H.264 o HEVC | 4K | caso occasionale |
| 4 | MOV | H.264 o ProRes | 1080p | |
| 5 | WMV | VC-1 / WMV3 | 720p-1080p | |
| 6 | qualsiasi | GOP lungo (≥ 5 s) | 1080p | per valutare i salti sui fotogrammi chiave |

## 1. Portabilità
- [ ] Avvio su un PC senza ambiente di sviluppo (oppure con Windows Sandbox o una VM pulita): nessuna richiesta di amministratore, nessun runtime mancante.
- [ ] Riproduzione dei campioni 1-5: annotare quelli che non si aprono e il messaggio nella barra di stato.
- [ ] Se possibile, anche su una VM o in Windows Sandbox **senza driver GPU**: verificare che il motore video si avvii (rischio `vulkan-1.dll`, D-002). Se compare "The video engine could not be started", annotare il messaggio.
- [ ] Secondo avvio: confrontare il tempo di avvio a occhio (la cache delle DLL in `%TEMP%\.net\viBro` deve essere riutilizzata).

## 2. Comportamento funzionale
- [ ] Il clic su un file mostra il primo fotogramma **senza** avviare la riproduzione.
- [ ] Con un video in riproduzione, selezionarne un altro: deve comparire in pausa.
- [ ] Salti in pausa: viene mostrato il fotogramma raggiunto e la pausa resta.
- [ ] Salti in riproduzione: la riproduzione continua.
- [ ] Salti oltre l'inizio o la fine: restano entro i limiti del video.
- [ ] Alla fine del video la riproduzione si ferma e non passa al file successivo.
- [ ] E sul primo file e D sull'ultimo non hanno effetto.
- [ ] Tenere premuto → per 3 s, poi rilasciare: lo spostamento si ferma subito, senza salti arretrati.
- [ ] Clic sul video, poi le scorciatoie: devono funzionare ancora.
- [ ] Le scorciatoie non agiscono mentre si scrive nella barra del percorso.

## 3. Misure (per ogni supporto: SSD, unità mappata, UNC)
Per ogni combinazione di supporto e modalità (*Exact seeks* off/on, HW decoding auto-safe/no):
- [ ] **Primo fotogramma**: 10 clic su file diversi (`load`/`switch`).
- [ ] **Cambio video**: 20 pressioni di D, una alla volta e attendendo il fotogramma (`switch`).
- [ ] **Salti**: per ciascun tasto (←/→, ↓/↑, PgDn/PgUp, Alt+PgDn/PgUp) 10 pressioni singole in pausa e 10 in riproduzione (`seek`).
- [ ] **Raffica**: tenere premuto → e ↓ per 3 s; controllare `coalesced` e l'assenza di arretrati.
- [ ] **Memoria**: annotare `private_mb` dopo 50 cambi di video, per verificare che non cresca senza limite.
- [ ] **4K**: ripetere le misure di primo fotogramma e salti sul campione 3.

Analisi:
```
powershell -ExecutionPolicy Bypass -File build\analyze-metrics.ps1 -Path C:\Tools\viBro\viBro-metrics.csv
```

Prove facoltative sulla cache, su NAS:
```
viBro.exe \\nas\video --mpv-opt cache=yes --mpv-opt demuxer-max-bytes=32MiB
viBro.exe \\nas\video --mpv-opt demuxer-readahead-secs=0
```

## 4. Rete non raggiungibile
- [ ] Digitare nella barra del percorso un UNC inesistente o spento: l'interfaccia resta utilizzabile e compare "Still waiting for ...".
- [ ] Nel frattempo aprire una cartella locale: l'elenco mostra la cartella locale e il risultato tardivo del NAS viene ignorato.
- [ ] Scollegare il NAS mentre un video è caricato, poi provare un salto: annotare il comportamento.

## 5. Esito
Riportare in `CONTEXT.md`: tabella di `analyze-metrics.ps1`, problemi riscontrati, modalità di salto consigliata e decisione di procedere con la 0.2.0 (sì/no).

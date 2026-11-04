# Piano di prova 1.4.0

Prove delle modifiche della 1.4.0 e regressione rapida. Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.4.0 (…)`.

## Valori predefiniti (viBro.ini senza sezione `[Seek]`)
- [ ] Con il `viBro.ini` della 1.3.0: ← / → = 1 s, ↓ / ↑ = 5 s, rotella sopra il video = 3 s, PgDown / PgUp = 1 min, Alt + PgDown / PgUp = 5 min, come prima.
- [ ] Chiudendo viBro, `viBro.ini` contiene la sezione `[Seek]` con `LeftRightSeconds=1`, `UpDownSeconds=5`, `WheelSeconds=3`, `PageSeconds=60`, `AltPageSeconds=300`; le altre impostazioni sono invariate.

## Valori modificati (viBro chiuso, poi riaperto)
- [ ] Con `LeftRightSeconds=2`, `UpDownSeconds=10`, `WheelSeconds=1.5`, `PageSeconds=20`, `AltPageSeconds=600`: ogni comando salta della durata impostata, in avanti e indietro, a finestra normale e a schermo intero (il tempo dopo il salto compare sopra il video).
- [ ] Ctrl+F2 mostra `2 s`, `10 s`, `20 s`, `10 min` per la tastiera e `1.5 s` per la rotella sopra il video.
- [ ] Con `PageSeconds=20` (sotto `ExactSeekThresholdSeconds=30`) PgUp/PgDown arrivano al punto esatto (nessuno scarto di qualche secondo); con `PageSeconds=60` tornano rapidi, sul fotogramma chiave.
- [ ] Un valore non valido (es. `UpDownSeconds=abc`, `WheelSeconds=0`, `PageSeconds=5000`): avviso nella barra di stato, viene usato il predefinito e alla chiusura il valore viene riscritto con il predefinito.

## viBro-metrics.csv
- [ ] Con `[Diagnostics] MetricsLog=false` (dopo aver cancellato `viBro-metrics.csv`): aprendo video e saltando, il file non viene creato.
- [ ] Con `MetricsLog=true`: il file viene creato e riceve righe come prima.

## Regressione
- [ ] Rotella sopra la barra temporale = cambio video; trascinamento col tasto destro; clic e trascinamento sulla barra; D/E; elimina/rinomina/sposta/copia; temi; Ctrl+W.

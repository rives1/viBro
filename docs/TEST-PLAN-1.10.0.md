# Piano di prova 1.10.0

Prove delle modifiche della 1.10.0 e regressione rapida. Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.10.0 (…)`. `viBro.ini` si modifica solo a viBro chiuso.

## Valori predefiniti
- [ ] Avviare e chiudere viBro: `viBro.ini` contiene la sezione `[Keys]` con 31 righe (da `SeekBackShort=Left` a `Close=Ctrl+W`, `Delete=Delete,R`).
- [ ] Tutte le scorciatoie funzionano come nella 1.9.0 (frecce, PgUp/PgDn, Alt+PgUp/PgDn, Spazio, E/D, M, F11/Esc, Ctrl+L, Ctrl+A, F5, Invio su una cartella, Shift+Invio, Canc/R, F2, W/Shift+W, C/Shift+C, I/O, F9, F10, Ctrl+F2, Ctrl+W), anche con lo stato attivo sul video.
- [ ] Modificatori esatti: Shift+← e Shift+PgUp non saltano più; Shift+E non cambia video; Ctrl+R non elimina.
- [ ] Ctrl+F2 mostra i tasti predefiniti come prima (`← / →`, `Del or R`, `Ctrl + F2` …).

## Tasti modificati (viBro chiuso, poi riaperto)
- [ ] `SeekBackShort=J`, `SeekForwardShort=L`: J/L saltano di 1 s, ← / → non fanno più nulla; Ctrl+F2 mostra `J / L`.
- [ ] `PlayPause=Space,P`: Spazio e P mettono in pausa / riprendono.
- [ ] `Gallery=none`: F10 non fa nulla; Ctrl+F2 mostra `(no key)` per la galleria.
- [ ] `Close=Ctrl+Q`: Ctrl+Q chiude viBro, anche mentre si scrive nella barra del percorso; Ctrl+W non chiude più.
- [ ] `Delete=Del`, `Rename=R,F2`: R rinomina; Canc elimina.
- [ ] `Extract=Ctrl+Shift+X`: estratto con Ctrl+Shift+X.
- [ ] Conflitto `Rename=R` lasciando `Delete=Delete,R`: avviso nella barra di stato (`… R is already used by Delete; ignored for Rename.`); R continua a eliminare.
- [ ] Valore non valido (`Mute=Ctrl+Ctrl+M`, `NextVideo=Page Down`): avviso e tasto predefinito; alla chiusura il file contiene il valore predefinito.
- [ ] Alla chiusura `[Keys]` riscrive le righe con i valori in uso (nomi canonici, es. `Del` diventa `Delete`).

## Regressione
- [ ] Mouse e rotella invariati (clic, doppio clic, tasto destro e menu, trascinamento, rotella su video e barra, tasto destro + rotella), galleria, selezione multipla, F5.

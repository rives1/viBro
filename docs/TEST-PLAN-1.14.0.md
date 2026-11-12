# Piano di prova 1.14.0

Prove delle novità della 1.14.0 (D-082), poi la regressione completa di [TEST-REGRESSION.md](TEST-REGRESSION.md). Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd`. Nel titolo deve comparire `viBro 1.14.0 (…)`. Usare copie dei video per elimina e sposta. Serve `ffmpeg.exe` accanto a `viBro.exe` per gallerie ed estratto.

**Esito (2026-11-12, utente, Windows 11, EXE compilato da `42ccbb9`):** tutte le prove superate, compresa la regressione di `TEST-REGRESSION.md` e le animazioni di Windows spente (viBro segue l'interruttore *Effetti di animazione*). Nessuna prova passa alla versione successiva.

**Prima di tutto:** *Impostazioni › Accessibilità › Effetti visivi › Effetti di animazione* **attivo**. viBro segue questa impostazione: se è spenta non si vede nessuna delle dissolvenze qui sotto.

## Gallerie: apertura e chiusura
- [x] F10: la cornice della finestra compare, il contenuto (righe e barra in basso) compare in dissolvenza in circa 0,2 s, senza lampi.
- [x] Esc: il contenuto sparisce in dissolvenza, poi la finestra si chiude. Lo stesso con il pulsante di chiusura e con Alt+F4. Durante la dissolvenza i clic non fanno nulla.
- [x] Esc due volte di seguito: la seconda chiude subito, senza errori.
- [x] Lo stesso per F12 (galleria della selezione).
- [x] Con la galleria aperta, F10 di nuovo: la vecchia sparisce subito e la nuova compare in dissolvenza. Lo stesso con F12.
- [x] Con una galleria aperta, Ctrl+W (chiusura di viBro): tutto si chiude subito, senza attese né errori; la posizione della galleria è ricordata alla riapertura.
- [x] Galleria massimizzata: apertura e chiusura in dissolvenza; si riapre massimizzata.

## Gallerie: video caricato
- [x] F10, poi D / E più volte nella finestra principale: il bordo colorato sfuma dalla riga vecchia a quella nuova (breve dissolvenza), la galleria segue. Con D / E tenuti premuti nessun bordo resta a metà o su due righe.
- [x] Clic su un'altra riga della galleria: il video si carica **subito** (nessun ritardo rispetto alla 1.13.0), il bordo passa con la dissolvenza.
- [x] Scorrendo la galleria il bordo è sempre e solo sulla riga del video caricato.

## Elenco dei file
- [x] Eliminare un video (Canc): la riga sfuma in circa 0,15 s, poi sparisce e si carica il video successivo. Lo stesso spostando (W) e con più file selezionati.
- [x] Dopo diverse eliminazioni e spostamenti, scorrendo l'elenco nessuna riga resta trasparente o semitrasparente.
- [x] F5 dopo aver aggiunto due video alla cartella con Esplora file: le due righe nuove compaiono in dissolvenza; la posizione di scorrimento resta quella di prima. F5 dopo aver cancellato un file con Esplora file: la sua riga sfuma e sparisce.
- [x] Estratto (I, O, F9): la riga del nuovo file compare in dissolvenza.
- [x] Il cambio di video (D / E, clic, rotella) resta istantaneo come nella 1.13.0.

## Animazioni di Windows spente
- [x] Spegnere *Effetti di animazione* (anche con viBro aperto): gallerie, bordo e righe cambiano di colpo, come nella 1.13.0; tutte le funzioni restano uguali.

## Regressione
- [x] Tutte le prove di [TEST-REGRESSION.md](TEST-REGRESSION.md).

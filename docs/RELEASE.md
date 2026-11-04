# Release su GitHub

Dalla 1.6.0 (D-066) le release si creano da sole. Una release di viBro contiene **solo le note della versione e gli archivi del sorgente** che GitHub genera per ogni tag (`Source code (zip)` e `(tar.gz)`). Non contiene l'EXE (D-052: la libmpv inclusa è GPL e il suo sorgente esatto non è disponibile) né `ffmpeg.exe`.

## Flusso normale (a ogni versione)
1. L'assistente prepara il commit di chiusura (CHANGELOG con la data) e il tag annotato `vX.Y.Z`, dopo le prove.
2. Si pubblicano commit e tag:
   ```
   git push --follow-tags
   ```
3. Su GitHub parte il workflow `build`: prima la compilazione di verifica (job `build`), poi il job `release`, che:
   - prende da `CHANGELOG.md` la sezione `## [X.Y.Z]` (se manca, si ferma con errore e la release non viene creata);
   - aggiunge una nota "solo sorgente";
   - crea la release `viBro X.Y.Z` sul tag, oppure ne aggiorna le note se esiste già.
   Solo la versione più alta viene segnata come *Latest*.
4. La release compare su `https://github.com/rives1/viBro/releases`.

Se la compilazione di verifica fallisce, la release non viene creata: si corregge, si sposta il tag sul commit giusto, solo se non è già stato pubblicato, e si ripete il push.

## Release per i tag già esistenti (fino alla v1.5.0)
I tag pubblicati prima della 1.6.0 non hanno una release. Per crearla:
1. GitHub → **Actions** → workflow **build** → **Run workflow**.
2. Ramo `main`; nel campo **release_tag** scrivere il tag, per esempio `v1.5.0`.
3. **Run workflow**. Il job `release` usa il CHANGELOG di quel tag e non segna la release come *Latest*, a meno che non sia la versione più alta.

Ripetere per ogni tag desiderato. Si può fare anche dal terminale con GitHub CLI: `gh workflow run build -f release_tag=v1.5.0`.

## A mano, senza workflow
Dal sito: **Releases** → **Draft a new release** → scegliere il tag → titolo `viBro X.Y.Z` → incollare la sezione della versione dal CHANGELOG → **Publish release**. Non allegare l'EXE né lo zip di `dist\` (D-052).

Con GitHub CLI:
```
gh release create vX.Y.Z --verify-tag --title "viBro X.Y.Z" --notes-file note.md
```

## Permessi
Il job `release` chiede il permesso `contents: write`, necessario per creare la release; il job `build` resta `contents: read`. Il permesso scritto nel workflow vale anche se in Settings → Actions → General → *Workflow permissions* è impostata la sola lettura come predefinita. Un criterio di organizzazione può però vietarlo: allora il job fallisce con `HTTP 403` e la scrittura va consentita in quella pagina.

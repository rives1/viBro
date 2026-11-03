# Piano di prova 0.6.0

Prove ancora aperte dalle versioni precedenti, più quelle della 0.6.0 quando sarà definita.
Il piano passa alla versione successiva finché tutte le prove non sono confermate.

Compilare con `build.cmd` (EXE normale, `artifacts\\publish`) e poi con
`powershell -ExecutionPolicy Bypass -File build\\build.ps1 -Compress` (EXE compresso, `artifacts\\publish-compressed`).

## Da versioni precedenti

### Tempo di avvio e dimensione dell'EXE (dalla 0.4.0, non eseguita nella 0.5.0)
- [ ] Avviare e chiudere 5 volte l'EXE normale, poi 5 volte quello compresso; il primo avvio di ciascuno include l'estrazione nella cache. Inviare `viBro-metrics.csv` di entrambe le cartelle (righe `startup`) per decidere la variante predefinita (D-032).

## 0.6.0
_(da definire)_

## Regressioni
- [ ] Salti, E/D, albero, ordinamento, INI, elimina, rinomina, sposta, temi, mouse sul video e rotella come nella 0.5.0.

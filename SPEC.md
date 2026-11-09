Specifiche di viBro
Obiettivo

Sviluppare un’applicazione desktop per esplorare cartelle, visualizzare rapidamente i video selezionati e organizzare i file. La priorità è ridurre il tempo necessario per mostrare il primo fotogramma, cambiare video ed effettuare salti temporali.

Piattaforma e distribuzione
- Windows 11 x64; nessun supporto ARM richiesto.
- Un unico file viBro.exe da copiare ed eseguire senza installazione.
- Nessuna richiesta di privilegi amministrativi.
- Nessun runtime o pacchetto codec da installare separatamente.
- Consentita l’estrazione automatica delle dipendenze in una cache locale, versionata e riutilizzata negli avvii successivi.
- Impostazioni in viBro.ini, nella stessa cartella dell’eseguibile.
- Nessun aggiornamento automatico nella prima versione.
La portabilità deve essere verificata su un sistema senza ambiente di sviluppo installato.

Tecnologie
- C# e WPF per applicazione e interfaccia.
- libmpv per riproduzione, decodifica e navigazione video.
- Versioni delle dipendenze fissate e documentate.
- Verifica delle licenze delle dipendenze e della loro redistribuzione prima del rilascio pubblico.
- Nessuna dipendenza da un’installazione esterna di mpv.
Interfaccia
Interfaccia in inglese, con temi Light e Dark selezionabili e persistenti.
Area	Contenuto
Sinistra, intera altezza	Albero delle cartelle e unità
Destra, parte superiore	Visualizzazione video, area principale
Destra, parte inferiore	Elenco video della cartella corrente


Requisiti:
- Divisori ridimensionabili e posizioni memorizzate.
- Elenco con colonne Name, Size, Date modified.
- Selezione di più file (dalla 1.7.0) come in Windows: Ctrl + clic, Shift + clic per un intervallo, Ctrl+A per tutti i video. Eliminazione, spostamento e copia agiscono su tutti i video selezionati; il riquadro video carica il file cliccato per ultimo; rinomina, Shift+Invio ed estratto richiedono un solo video selezionato.
- Ordinamento per ogni colonna, con ordinamento naturale dei nomi.
- Video e sottocartelle della cartella corrente (dalla 1.3.0; prima solo i video); nessuna scansione ricorsiva. In cima `..` per la cartella superiore, poi le cartelle, poi i video; una cartella si apre con doppio clic o Invio; D/E, rotella e operazioni sui file valgono solo per i video.
- Accesso alle sottocartelle tramite albero.
- Barra del percorso modificabile, anche per percorsi UNC di rete.
- Barra temporale con posizione corrente e durata.
- Controlli essenziali: Play/Pause, volume e mute.
- Proporzioni originali del video mantenute.
- Nessun caricamento o visualizzazione dei sottotitoli.
- Nessuna miniatura nell’elenco nella prima versione.
Le unità mappate e i percorsi NAS devono essere utilizzabili senza bloccare l’interfaccia quando non sono raggiungibili.
Caricamento e riproduzione
Formati richiesti: MP4, MKV, MOV, WMV, AVI (dalla 1.8.0) e M4V (dalla 1.9.0). La compatibilità effettiva va verificata anche sui codec contenuti nei file campione.
- Caricare il video selezionato e mostrare il primo fotogramma disponibile, senza avviare la riproduzione se il video precedente era in pausa (o se non c'era un video).
- Dalla 0.6.0 (richiesta dell'utente) lo stato Play/Pause si conserva al cambio di file: se il video corrente è in riproduzione, il nuovo parte in riproduzione. Vale per ogni cambio (E/D, rotella, clic nell'elenco, file proposto dopo elimina, rinomina o sposta).
- Un salto temporale effettuato in pausa deve mostrare il fotogramma raggiunto e mantenere la pausa.
- Un salto durante la riproduzione deve mantenere la riproduzione.
- Alla fine del video, fermarsi senza passare al successivo.
- All’avvio ripristinare soltanto l’ultima cartella: nessun video caricato automaticamente.
- Audio supportato, con priorità secondaria rispetto alla reattività della navigazione.
Prestazioni
Scenario principale: video 1080p, occasionalmente 4K, su SSD oppure NAS con rete a 2,5 Gbit/s.
- Usare accelerazione hardware quando disponibile, senza dipendere da una GPU NVIDIA.
- Prevedere un ripiego software; su hardware meno potente è accettabile una riduzione delle prestazioni.
- Preferire salti rapidi verso fotogrammi chiave, anche approssimati.
- Conservare il motore video fra un caricamento e l’altro, evitando ricreazioni inutili.
- Caricare l’albero delle cartelle progressivamente.
- Usare un elenco virtualizzato.
- Non estrarre durata, risoluzione o altri metadati da tutti i video durante l’apertura della cartella.
- Eseguire accessi al filesystem e operazioni lente senza bloccare l’interfaccia.
- Annullare o ignorare caricamenti superati da una selezione più recente.
- Accorpare i salti ripetuti, evitando una coda di richieste arretrate.
- Limitare l’uso della cache; dimensionarla tramite prove su SSD e NAS.
- Evitare letture preventive massicce dei file sul NAS.
Non fissare tempi garantiti prima delle misurazioni. Registrare almeno tempo al primo fotogramma, latenza dei salti, cambio video e consumo di memoria sui file campione.
Comandi da tastiera
Tasto	Azione
← / →	Indietro / avanti di circa 1 secondo
↓ / ↑	Indietro / avanti di circa 5 secondi
PgDown / PgUp	Indietro / avanti di circa 1 minuto
Alt + PgDown / Alt + PgUp	Indietro / avanti di circa 5 minuti
Spazio	Play/Pause
E	Seleziona e carica il video precedente (Play/Pause conservato)
D	Seleziona e carica il video successivo (Play/Pause conservato)
Canc oppure R	Elimina i file selezionati con conferma (più file dalla 1.7.0)
F2	Rinomina il file
W	Sceglie la cartella di destinazione e sposta il file
Shift + W	Sposta nella destinazione utilizzata l’ultima volta
C	Sceglie la cartella di destinazione e copia il file (0.6.0)
Shift + C	Copia nell’ultima destinazione di copia (0.6.0)
Ctrl + W	Chiude viBro (0.6.0)
Ctrl + F2	Finestra dei comandi di tastiera e mouse (1.2.0)
M	Mute on/off
F11	Schermo intero
Esc	Esce dallo schermo intero
Ctrl + L	Attiva la barra del percorso
Ctrl + A	Seleziona tutti i video della cartella (1.7.0)
F5	Aggiorna l’albero delle cartelle e l’elenco dei file dal disco, conservando cartelle aperte, selezione e video caricato (1.8.0)
Invio	Apre la cartella selezionata nell’elenco (1.3.0)
Shift + Invio	Apre il video selezionato con l’applicazione predefinita di Windows; viBro va in pausa (1.3.0)
I / O	Marker di inizio / fine dell’estratto nella posizione corrente (1.5.0)
F9	Salva la parte tra I e O in un nuovo file nella stessa cartella, senza ricodifica; richiede ffmpeg.exe accanto a viBro.exe (1.5.0)
F10	Galleria dei video della cartella in una finestra separata: per ogni video una riga con 5 immagini alle stesse percentuali della durata (predefinite 10/30/50/70/90 %, [Gallery] in viBro.ini); clic su una riga = seleziona e carica il video, clic su un’immagine = lo carica a quel tempo; richiede ffmpeg.exe (1.6.0)

Le durate dei salti indicate sopra sono quelle predefinite: dalla 1.4.0 si cambiano nella sezione [Seek] di viBro.ini.

Mouse sul video (0.5.0, richiesta dell'utente):
Clic	Play/Pause (non il clic che riattiva la finestra)
Doppio clic	Schermo intero; a schermo intero, ritorno alla finestra normale (la riproduzione resta com'era)
Tasto destro	Menu con Delete (come Canc/R), Move... (come W), Move to <ultima destinazione> (come Shift+W), Copy... (come C), Copy to <ultima destinazione di copia> (come Shift+C)
Tasto destro tenuto + trascinamento orizzontale (0.6.0)	Spostamento nel video: tutta la larghezza del video = tutta la durata; il video segue il mouse, salto esatto al rilascio; nessun menu
Rotella sopra la barra temporale (1.3.0; invertita nella 1.9.0)	Su: video successivo; giù: video precedente (come D / E), a finestra normale e a schermo intero
Tasto destro tenuto + rotella sul video (1.9.0)	Su: video successivo; giù: video precedente; al rilascio del tasto nessun menu
Rotella sopra il video (1.3.0; a schermo intero ovunque tranne la barra temporale)	Su: avanti 3 s; giù: indietro 3 s
Dalla 1.9.0 la rotella su video e barra temporale funziona anche quando viBro non è in primo piano (finestra sotto il puntatore), tranne con una finestra di dialogo aperta.

Interfaccia (1.1.0): barra dei controlli (Play, barra temporale, tempo, Mute, volume) sotto il video e sopra l’elenco dei file a finestra normale; icona della cartella davanti alla barra del percorso; nella rinomina il cursore parte dall’inizio del nome, senza selezione.

Interfaccia (1.2.0): pulsanti Play/Pause, Mute, Browse e Theme a icone (Segoe Fluent Icons) con tooltip che riporta il tasto; tempo sotto il puntatore sulla barra temporale, senza anteprima dell’immagine; file caricato evidenziato nell’elenco (icona ▶/❚❚ e grassetto); posizione nella cartella nella barra di stato; a schermo intero, tempo sovrapposto al video per circa 1 s dopo un salto e nome del file per 0,8 s al cambio di file (OSD di mpv).

Interfaccia (0.6.0): barra di stato compatta con numero di video della cartella e spazio totale; a schermo intero la fascia con la barra temporale è sotto il video; la conferma di eliminazione si apre vicino al puntatore del mouse.


Dalla 1.10.0 (richiesta dell'utente) i tasti della tabella sono quelli predefiniti: ogni scorciatoia da tastiera della finestra principale si può cambiare nella sezione [Keys] di viBro.ini (modificatori esatti; conflitti segnalati, vince il comando che viene prima nell'elenco). Mouse e rotella restano fissi.

Regole:
- Le scorciatoie sono sospese durante la modifica di nomi, percorsi o altri campi di testo e quando è aperta una finestra modale.
- Ai limiti della lista, E/D non ricominciano dall’estremità opposta.
- I salti sono limitati all’intervallo fra inizio e fine video.
- La ripetizione automatica dei tasti non deve provocare eliminazioni o spostamenti multipli.
Operazioni sui file
Prima di rinominare, eliminare o spostare, il player deve rilasciare il file. L’interfaccia deve riflettere l’esito reale dell’operazione.

Eliminazione
- File locali: invio al Cestino.
- File di rete, comprese unità di rete mappate: eliminazione definitiva.
- Conferma sempre richiesta, con nome del file e tipo di eliminazione chiaramente indicati. Eccezione (1.8.0, scelta dell’utente): Delete nel menu del tasto destro sul video elimina un solo file senza conferma, anche in rete; con più file la conferma resta; configurabile ([Delete] ConfirmFromVideoMenu).
- Se un file locale non può essere cestinato, non eliminarlo definitivamente senza una nuova conferma esplicita.
- Dopo il successo: selezionare e caricare il file successivo, oppure il precedente se era l’ultimo (Play/Pause conservato dalla 0.6.0).
- Se la lista diventa vuota, svuotare il riquadro video.
Rinomina
- Modificare normalmente il solo nome, preservando l’estensione.
- Verificare nomi non validi e conflitti.
- Non sovrascrivere file esistenti.
- Dopo il successo, mantenere selezionato il file rinominato e ricaricarlo, possibilmente nella posizione precedente (Play/Pause conservato dalla 0.6.0).
Spostamento
- W apre la scelta della destinazione, inizializzata sull’ultimo percorso utilizzato.
- La destinazione confermata viene salvata nell’INI.
- Shift+W usa direttamente l’ultima destinazione; se non esiste una destinazione memorizzata, apre la scelta.
- Se la destinazione non è raggiungibile, mostrare l’errore e consentire di sceglierne un’altra.
- Nessuna sovrascrittura silenziosa in caso di conflitto.
- Per trasferimenti lunghi: avanzamento e possibilità di annullamento.
- Negli spostamenti che richiedono copia e cancellazione, preservare la sorgente fino al completamento corretto della copia.
- Dopo il successo, caricare il video successivo (Play/Pause conservato dalla 0.6.0).
- Nessuna operazione se sorgente e destinazione coincidono.
File INI
Memorizzare almeno:
- Versione dello schema di configurazione.
- Tema Light/Dark.
- Posizione, dimensioni e stato della finestra.
- Dimensioni dei tre riquadri.
- Ultima cartella aperta.
- Ordinamento e larghezze delle colonne.
- Volume e mute.
- Ultima destinazione di spostamento.
- Preferenza di accelerazione hardware.
- Eventuale limite della cache.
Usare salvataggio atomico e valori predefiniti per impostazioni mancanti o non valide. Se l’INI non è scrivibile, avvisare e continuare con impostazioni in memoria, senza salvarle silenziosamente altrove.
Sviluppo, Git e continuità
Lo sviluppo deve avvenire nella cartella locale del progetto, predisposta per un repository pubblico chiamato viBro.
- Creare commit locali per passi coerenti e verificati.
- Non includere percorsi personali, video campione privati, credenziali o configurazioni dell’utente.
- Usare versioni MAJOR.MINOR.PATCH, a partire da 0.1.0.
- Mantenere una sola fonte per il numero di versione.
- Mostrare la versione nell’applicazione e nelle proprietà dell’EXE.
- Preparare tag Git per i rilasci.
- Preparare compilazione automatica tramite GitHub Actions.
Documentazione obbligatoria:
File	Contenuto
README.md	Scopo, utilizzo, scorciatoie e compilazione
SPEC.md	Specifiche approvate
CHANGELOG.md	Cambiamenti per versione
docs/DECISIONS.md	Decisioni tecniche e motivazioni
CONTEXT.md	Stato aggiornato per riprendere il lavoro in un’altra chat
docs/handover/CONTEXT-vX.Y.Z.md	Fotografia del contesto a ogni rilascio


I file di contesto devono indicare funzionalità completate, problemi noti, dipendenze, comandi di compilazione, verifiche eseguite, riferimento al codice verificato e prossimo passo.

Preparare script di compilazione e confezionamento. Compilare localmente quando gli strumenti sono disponibili; in caso contrario, documentare precisamente come predisporre l’ambiente.
Percorso di sviluppo suggerito
Versione	Obiettivo
0.1.0	Prototipo tecnico: singolo EXE, caricamento in pausa, salti rapidi, prove SSD/NAS
0.2.0	Interfaccia completa, albero, elenco, ordinamento e INI
0.3.0	Eliminazione, rinomina e spostamento
0.4.0	Temi, gestione errori, ottimizzazione e verifiche di portabilità (confluita nella 0.5.0)
0.5.0	Mouse sul video: clic per Play/Pause, doppio clic per lo schermo intero, menu del tasto destro, rotella a schermo intero
0.6.0	Riproduzione conservata al cambio di file, copia, trascinamento col tasto destro, rotella sul video, barra di stato con conteggio, fascia in basso a schermo intero, conferma vicino al puntatore, Ctrl+W
1.0.0	Prima versione completa e verificata (rilasciata il 2026-11-03)
1.2.0	Pulsanti a icone, tempo sotto il puntatore, file caricato evidenziato, posizione nella barra di stato, tempo e nome del file sovrapposti a schermo intero, Ctrl+F2 (comprende la 1.1.0) (rilasciata il 2026-11-03)
1.3.0	Sottocartelle nell’elenco, Shift+Invio con l’applicazione predefinita, rotella: barra = cambio video, video = spostamento nel tempo (rilasciata il 2026-11-04)
1.4.0	Durata dei salti configurabile in viBro.ini (sezione [Seek]), Ctrl+F2 con le durate in uso (rilasciata il 2026-11-04)
1.5.0	Estratto di una parte del video con I / O / F9 e un ffmpeg.exe fornito dall’utente (rilasciata il 2026-11-04)
1.6.0	Galleria dei video della cartella con F10; release automatiche su GitHub (solo sorgente) (rilasciata il 2026-11-04)
1.7.0	Selezione di più file nell’elenco; elimina, sposta e copia sulla selezione (rilasciata il 2026-11-05)
1.8.0	F5 aggiorna albero ed elenco; file AVI; elimina dal menu del video senza conferma; menu del tasto destro nella galleria (rilasciata il 2026-11-06)
1.9.0	Rotella anche con viBro non in primo piano; rotella sulla barra invertita; tasto destro + rotella cambia video; file M4V (rilasciata il 2026-11-07)
1.10.0	Scorciatoie da tastiera configurabili in viBro.ini ([Keys]) (rilasciata il 2026-11-09)
1.1.0	(confluita nella 1.2.0) Controlli sotto il video, rotella per spostarsi nel video a finestra normale, icona della barra del percorso, cursore all’inizio nella rinomina


Prima di sviluppare tutta l’applicazione, il prototipo 0.1.0 deve dimostrare tre cose: EXE autonomo funzionante, caricamento del video in pausa e navigazione sufficientemente rapida sui tuoi file. Questo è il controllo più utile per confermare la scelta tecnica senza investire subito nell’intera interfaccia.
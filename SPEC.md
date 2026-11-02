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
- Ordinamento per ogni colonna, con ordinamento naturale dei nomi.
- Solo file video della cartella corrente; nessuna scansione ricorsiva.
- Accesso alle sottocartelle tramite albero.
- Barra del percorso modificabile, anche per percorsi UNC di rete.
- Barra temporale con posizione corrente e durata.
- Controlli essenziali: Play/Pause, volume e mute.
- Proporzioni originali del video mantenute.
- Nessun caricamento o visualizzazione dei sottotitoli.
- Nessuna miniatura nell’elenco nella prima versione.
Le unità mappate e i percorsi NAS devono essere utilizzabili senza bloccare l’interfaccia quando non sono raggiungibili.
Caricamento e riproduzione
Formati richiesti: MP4, MKV, MOV e WMV. La compatibilità effettiva va verificata anche sui codec contenuti nei file campione.
- Caricare il video selezionato e mostrare il primo fotogramma disponibile, senza avviare la riproduzione.
- Nessun avvio automatico quando cambia la selezione, anche se il video precedente era in riproduzione.
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
E	Seleziona e carica il video precedente, in pausa
D	Seleziona e carica il video successivo, in pausa
Canc oppure R	Elimina il file con conferma
F2	Rinomina il file
W	Sceglie la cartella di destinazione e sposta il file
Shift + W	Sposta nella destinazione utilizzata l’ultima volta
M	Mute on/off
F11	Schermo intero
Esc	Esce dallo schermo intero
Ctrl + L	Attiva la barra del percorso

Mouse sul video (0.5.0, richiesta dell'utente):
Doppio clic	Schermo intero; a schermo intero, ritorno alla finestra normale
Tasto destro	Menu con Delete (come Canc/R), Move... (come W) e Move to <ultima destinazione> (come Shift+W)
Rotella a schermo intero	Giù: video successivo; su: video precedente (come D / E)


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
- Conferma sempre richiesta, con nome del file e tipo di eliminazione chiaramente indicati.
- Se un file locale non può essere cestinato, non eliminarlo definitivamente senza una nuova conferma esplicita.
- Dopo il successo: selezionare e caricare il file successivo in pausa, oppure il precedente se era l’ultimo.
- Se la lista diventa vuota, svuotare il riquadro video.
Rinomina
- Modificare normalmente il solo nome, preservando l’estensione.
- Verificare nomi non validi e conflitti.
- Non sovrascrivere file esistenti.
- Dopo il successo, mantenere selezionato il file rinominato e ricaricarlo in pausa, possibilmente nella posizione precedente.
Spostamento
- W apre la scelta della destinazione, inizializzata sull’ultimo percorso utilizzato.
- La destinazione confermata viene salvata nell’INI.
- Shift+W usa direttamente l’ultima destinazione; se non esiste una destinazione memorizzata, apre la scelta.
- Se la destinazione non è raggiungibile, mostrare l’errore e consentire di sceglierne un’altra.
- Nessuna sovrascrittura silenziosa in caso di conflitto.
- Per trasferimenti lunghi: avanzamento e possibilità di annullamento.
- Negli spostamenti che richiedono copia e cancellazione, preservare la sorgente fino al completamento corretto della copia.
- Dopo il successo, caricare il video successivo in pausa.
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
0.5.0	Mouse sul video: doppio clic per lo schermo intero, menu del tasto destro, rotella a schermo intero
1.0.0	Prima versione completa e verificata


Prima di sviluppare tutta l’applicazione, il prototipo 0.1.0 deve dimostrare tre cose: EXE autonomo funzionante, caricamento del video in pausa e navigazione sufficientemente rapida sui tuoi file. Questo è il controllo più utile per confermare la scelta tecnica senza investire subito nell’intera interfaccia.
# Componenti di terze parti

viBro.exe incorpora i componenti seguenti.

| Componente | Versione | Licenza | Origine |
|---|---|---|---|
| libmpv (mpv) | build `20261101-git-48fca90e8c`, commit mpv `48fca90e8ce5011d0a4c689178ad739329ea253d` | GPL-2.0-or-later nella build GPL (mpv è LGPL-2.1+ solo se compilato con `-Dgpl=false`) | https://github.com/mpv-player/mpv |
| FFmpeg e librerie collegate staticamente in libmpv | fissate dalla build sopra | GPL (build con componenti GPL) | https://github.com/shinchiro/mpv-winbuild-cmake |
| .NET runtime e WPF | 10.0 | MIT | https://github.com/dotnet |

Strumento usato solo durante la compilazione (non incluso nell'EXE): 7-Zip `7zr.exe` 26.05 (LGPL-2.1), https://github.com/ip7z/7zip.

## Obblighi da soddisfare prima del rilascio pubblico (da verificare)

- [ ] Confermare l'elenco delle librerie incluse nella build di libmpv e le relative licenze (manifest della build shinchiro).
- [ ] Rendere disponibile il **sorgente corrispondente** della build di libmpv (GPL §6): pubblicare oppure collegare, insieme alla release, i sorgenti di mpv e delle dipendenze nelle revisioni usate e gli script di build.
- [ ] Verificare che nessun componente incluso sia incompatibile con la GPL (ad esempio librerie "nonfree").
- [ ] Inserire nella release questo file, LICENSE e il testo delle licenze dei componenti.

# Documentation impact EP-0015

Дата ревизии: 2026-10-26. Текущие контракты находятся в DB/Documentation; история исполнения и evidence — здесь в Board.

| Документ | Изменение | Основание |
|---|---|---|
| DB/Documentation/00-Process/CLAUDE.md | content/user paths, contentRoot, платформенные обязанности Client | AppPaths, EngineContentLoader |
| DB/Documentation/01-Requirements/EngineRequirements.md | разделение content/config и отсутствие платформенных side effects в Engine | contentRoot, factory |
| DB/Documentation/01-Requirements/FirstReleaseRequirements.md | граница Ubuntu реализации и отдельная приёмка | весь EP-0015 |
| DB/Documentation/04-Engineering/SaveFormat.md | пользовательские слоты, неизменный Save19/18 | Program/AppPaths |
| DB/Documentation/04-Engineering/DebugTuning.md | каталог пользователя вместо executable | AppPaths/DebugTuningFileWriter |
| DB/Documentation/04-Engineering/DebugTuningPanel.md | путь, launcher failure/path status | ShellLauncher/panel |
| DB/Documentation/04-Engineering/PointAimCombat.md | OpenAL worker, prewarm, epochs, unavailable | CombatAlertAudio/OpenAlTonePlayer |
| DB/Documentation/03-Design/TacticalMapSpecification.md | комплектные шрифты и ownership | UiFonts |
| DB/Documentation/06-Tooling/GameSessionScreenUI.md | общие typefaces, переносимый exporter | UiFonts/PortableAssetScripts |
| DB/Documentation/06-Tooling/DeepSpaceSaga.PortraitAssets.md | prepare/export, output/preview paths | PS wrappers/PortableAssetScripts |
| DB/Documentation/02-FirstRelease/Screens/Settings.md | пользовательский Settings.json | SkiaWindow/AppPaths |
| DB/Documentation/02-FirstRelease/Screens/Trade.md | fit viewport и input transform | TradeLayout/TradeScreen |
| DB/Documentation/02-FirstRelease/Screens/Save.md и Load.md | ссылка на актуальный путь слотов | SaveFormat |
| DB/Documentation/03-Design/PortraitGeneratorW4.md и M4.md | переносимая подготовка и alpha/collar guards | prepare.ps1 |
| DB/Documentation/README.md | актуальная навигация и дата ревизии | Ubuntu.md и текущие контракты |
| DB/Documentation/06-Tooling/Ubuntu.md | единый платформенный контракт, создан US-0004-TK-0003 | check/publish/AppPaths/fonts/audio |
| tools/graphify/rebuild.py и DB/Documentation/06-Tooling/Graphify.md | EP-0015 corpus, семантика и проверка графа | финальный docs ticket TK-0002 |

README.md в корне репозитория отсутствует. Корневой CLAUDE.md — корректный указатель на canonical guide, менять не требуется. DB/Documentation/00-Process/AGENTS.md имеет посторонние изменения и исключён из коммитов. Неизменные gameplay, Motion, balance и save schema не требуют переписывания остальных контрактов. Полный Documentation Markdown corpus прочитан на подготовке; локальные ссылки и Markdown-only проверяются после правок.

## История документации

- 2026-10-26: перенесён исходный EP-0015 из VData/Planning в единственный canonical Board; добавлена завершающая US-0005. Исходная папка больше не источник задач.
- 2026-10-26: Ubuntu guide отделил системные зависимости/команды от доказательств запусков. Контракты путей обновляются вместо противоречащего описания рядом с executable.
- 2026-10-26: миграция пользовательских путей не повышает SaveFormat19; contentRoot — конфигурационный корень установки, не новое поле сохранения. Поля gameSettings сохраняются.
- 2026-10-26: bundled fonts/OpenAL/Unix launcher/portable asset tools описываются как текущий код; native/physical/human и независимое review остаются отдельными gates.

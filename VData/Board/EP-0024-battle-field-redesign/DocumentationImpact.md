# Влияние EP-0024 на документацию

Инвентарь основан на diff от d7895f73, поиске TacticalBattleSkins/Xenon/ui.pilot-card/скинов/карточек во всём текущем Documentation, src/tools README и канонических связанных Board. Артефакты/история не становятся текущим контрактом.

| Документ/область | Изменение или проверенное отсутствие влияния |
|---|---|
| Screens/TacticalBattle.md | Tabletop поле/камера/карточки/панели/Space; baseline descriptions ограничены прежними скинами. Удалено устаревшее утверждение о 0: в текущем screen такого обработчика нет. |
| Screens/Settings.md, ScreenCatalog.md | Поставляемый Tabletop и отдельная панель команд. |
| Engineering/TacticalBattle.md | Флаги, renderer/cache ownership, camera, admitted snapshot/input,8 snapshots и numeric probes. |
| Engineering/TacticalBattleSkins.md |81 role/24 PNG; независимые opt-in, модели/FX/сталь/slim; честный статус rebake. |
| Skins/README.md | Установка/runtime Tabletop, свойства, отсутствие bake sources; происхождение готовых материалов. |
| Tooling/GameSessionScreenUI.md | Пакетные спрайты vs прежние кодовые BuildCard silhouettes, Panel locale, cloud cache. |
| Performance/README.md | GPU completion-aware timing, cold/warm/reuse, меньший монитор; без истории конкретного запуска. |
| Tooling/Graphify.md, GraphifyRebuild.md, tools/graphify/rebuild.py | EP24 corpus, fresh semantics/hashes, receipts excluded to avoid cycles. |
| Documentation/README.md | Навигационный label экрана. |
| EP24 Documentation/Tickets/13 stories/26 tickets | Реальные статусы/coverage/dependencies/SHA; Bake и12ms остаются открытыми. История не переписана как новый PASS. |
| EP24 Materials/Reference README | D-13 заменил реконструкцию; D-11 заменил старый отказ от панелей; готовые PNG неизменны. |
| FirstReleaseRequirements, EngineRequirements | Проверены: требования временного боя/паузы/мира прежние; skin — Client presentation. Изменение не нужно. |
| SaveFormat, Settings.json, Contracts, Engine/Motion | Версии и данные не изменены; миграция/новые настройки не нужны, используется существующий tacticalBattleSkinId. |
| Modding.md | Роли скинов уже описаны в общем виде со ссылкой на текущий контракт; архив DLL-мода не затрагивается. |
| TacticalMapSpecification, TacticalButtons/STYLE | Описывают world map/отдельные кнопочные ассеты, не Tabletop battle; изменений нет. |
| tools/tactical-battle/README, Fixtures/Saves README | Экспорт боевых данных/формат сохранений не менялись. |
| Board EP16/EP19/EP20/EP22/EP23 | Датированная история прежних результатов сохранена; актуальные описания приведены в текущих контрактах, старые отчёты не переименованы в результаты EP24. |
| Корневые руководства AGENTS/CLAUDE | Правила процесса не менялись. README в корне DSS отсутствует. |

Проверка: актуальные утверждения сопоставлены с исходниками; ссылки/IDs/PNG hashes и offline graph проверены; audit находится в DocumentationAudit.json и graphify/verification.json. В Documentation добавлены только Markdown; JSON/TRX/PNG/скрипты находятся вне него.

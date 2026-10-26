# Архитектура и работа с DSS

> **Контракт EP-0013.** Архитектурное уточнение EP-0013: [overlay](../04-Engineering/DebugTuningPanel.md) принадлежит Engine, общая schema — Contracts, аналитика независимых осей — Motion, ввод/фоновый writer/панель — Client. [ManualManeuver](../04-Engineering/ManualManeuver.md) описывает совместные controls и owner-only projection. Render не изменяет мир и не выполняет запись файла синхронно.

Рабочий репозиторий: `D:/DeepSpaceSaga/DSS`. Правила операций — в [AGENTS.md](AGENTS.md),
правила документации — в [DocumentationSystem.md](DocumentationSystem.md).
Требования движка — в [EngineRequirements](../01-Requirements/EngineRequirements.md).
Board — единственный источник задач. `$epic-writer` планирует, `$epic-executor`
исполняет весь явно выбранный эпик; `requirements-engineer` готовит отдельное ТЗ.

## Проекты и зависимости

| Проект | Ответственность |
|---|---|
| Contracts | Неизменяемые DTO, команды, receipts, snapshot и transport-интерфейсы |
| Motion | Общая чистая математика движения, геометрия и прогноз |
| Engine | Авторитетный мир, команды, RNG, экономика, бой и сохранение |
| Engine.LocalClient | Локальная сессия, transport, файлы сценариев/слотов |
| Client | Silk.NET/SkiaSharp, ввод, экраны, отображение принятых данных |

Client ссылается на Contracts, Motion и Engine.LocalClient, не напрямую на Engine.
Contracts не зависит от игровых проектов; графические библиотеки принадлежат Client.
Рендер не вызывает Engine синхронно. GameSessionHandle получает обновления,
SnapshotBuffer хранит историю представления, подготовленные кадры питают UI.
Клиентское предсказание не меняет авторитетный мир и не раскрывает скрытые планы NPC.

Модульный UI использует очищенные live combat DTO и собственный nullable
DestroyedModuleLimit. Legacy wire compatibility изолирована в save DTO Engine.
CommandsPanel кеширует подготовленную геометрию/ресурсы; panel profile memory
принадлежит owner/module в текущей сессии. Правила real-time и blocked aim —
в [PointAimCombat](../04-Engineering/PointAimCombat.md#панели-модулей).
Локальный producer ждёт выбора snapshot/combat reader, сохраняя report bootstrap.

## Запуск и content

Проекты нацелены на `net8.0`. `global.json`: SDK `8.0.100`,
`rollForward: latestMajor`, prerelease отключён; конкретный выбранный SDK зависит
от установки. Это не жёсткое закрепление версии 10.0.400.
AppPaths отделяет read-only content установки от пользовательских config/data.
Пользовательский Settings.json сохраняет preferences; управляемый contentRoot
задаёт EngineContentLoader корень каталогов, defaultScenario, generation и portraits.
Без contentRoot пути разрешаются относительно Settings.json. New Game, выбранный
сценарий и Load используют фабрику локальных сессий. Saves, DebugTuning,
CombatMetrics и Interface.log относятся к UserData; Scenarios — к content.
XDG/Windows, первичная миграция, portable.txt, OpenAL и комплектные шрифты
описаны в [Ubuntu](../06-Tooling/Ubuntu.md). Платформенное окно, звук и launcher
принадлежат Client; Engine не знает XDG и не воспроизводит сигналы.

## Проверки и изменения

Запускай build/test из репозитория. Выбирай owning test project и именованные
проверки, применяй process-tree cap 300000 ms; длинные corpus дели по точным ID.
Результаты направляй явным `--results-directory` в workspace artifacts.
Для документации проверяй содержание, ссылки и `git diff --check`; запуск игры
или полной suite сам по себе для правки Markdown не требуется.
Не включай чужие изменения в коммиты. Для изоляции используй управляемые
worktree Codex, сохраняя нужную работу перед архивированием.

Текущие технические контракты: [бой](../04-Engineering/PointAimCombat.md),
[сохранения](../04-Engineering/SaveFormat.md), [экономика и мир](../04-Engineering/TradingAndSolarSystem.md),
[отладочные параметры](../04-Engineering/DebugTuning.md).

Источники: [DeepSpaceSaga.sln](../../../DeepSpaceSaga.sln), [global.json](../../../global.json), [Directory.Build.props](../../../Directory.Build.props), [Program.cs](../../../src/DeepSpaceSaga.Client/Program.cs), [DeepSpaceSaga.Client.csproj](../../../src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj).

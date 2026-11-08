# Архитектура и работа с DSS

Рабочий репозиторий: `D:/DeepSpaceSaga/DSS`. Правила операций — в [AGENTS.md](AGENTS.md),
правила документации — в [DocumentationSystem.md](DocumentationSystem.md).
Требования движка — в [EngineRequirements](../01-Requirements/EngineRequirements.md).
Board — единственный источник задач. `$epic-writer` планирует, `$epic-executor`
исполняет весь явно выбранный эпик; `requirements-engineer` готовит отдельное ТЗ.

## Проекты и зависимости

DSS — одиночная десктопная игра: все проекты работают в одном процессе, сетевого
режима нет и не планируется ([EngineRequirements](../01-Requirements/EngineRequirements.md#архитектура-и-авторитет)).
Слой «transport» — граница in-process между фоновым тик-циклом мира и UI, а не сеть.

| Проект | Ответственность |
|---|---|
| Contracts | Неизменяемые DTO, команды, receipts, snapshot и интерфейсы сессий |
| Motion | Общая чистая математика движения, геометрия и прогноз |
| Engine | Авторитетный мир, команды, RNG, экономика, бой и сохранение |
| Engine.LocalClient | Локальная сессия, transport, файлы сценариев/слотов |
| Client | Silk.NET/SkiaSharp, ввод, экраны, отображение принятых данных |

Client ссылается на Contracts, Motion и Engine.LocalClient, не напрямую на Engine.
Contracts не зависит от игровых проектов; графические библиотеки принадлежат Client.
Рендер не вызывает Engine. Обработчики ввода могут синхронно вызывать быструю
in-process сессию, не трогающую мир (тактический бой). GameSessionHandle получает обновления,
SnapshotBuffer хранит историю представления, подготовленные кадры питают UI.
Клиентское предсказание не меняет авторитетный мир и не раскрывает скрытые планы NPC.

Карта и временный тактический бой имеют отдельные DTO и камеры. Engine владеет инициативой, постоянной очередью, немедленным уроном и результатом боя; Client получает immutable FireSteps через сессию и использует одну presentation-проекцию карты, карточек, журнала и итога. Прогноз не читает будущую ленту и не тратит RNG. Снимки мира не несут данные боевой песочницы. Исторический бой читается только на границе миграции Save18/19; текущий writer сохраняет мир в Save20. Ручные оси и отладка двигателей используют команды и проекции мира.

## Запуск и content

Проекты нацелены на `net8.0`. `global.json`: SDK `8.0.100`,
`rollForward: latestMajor`, prerelease отключён; конкретный выбранный SDK зависит
от установки. Это не жёсткое закрепление версии 10.0.400.
Settings.json рядом с executable перечисляет каталоги и defaultScenario.
EngineContentLoader валидирует данные и создаёт registry до публикации мира.
New Game, выбранный сценарий и Load используют фабрику локальных сессий.
Слоты, Scenarios и DebugTuning ищутся относительно `AppContext.BaseDirectory`.

## Проверки и изменения

Запускай build/test из репозитория. Выбирай owning test project и именованные
проверки, применяй process-tree cap 300000 ms; длинные corpus дели по точным ID.
Результаты направляй явным `--results-directory` в workspace artifacts.
Для документации проверяй содержание, ссылки и `git diff --check`; запуск игры
или полной suite сам по себе для правки Markdown не требуется.
Не включай чужие изменения в коммиты. Для изоляции используй управляемые
worktree Codex, сохраняя нужную работу перед архивированием.

Текущие технические контракты: [тактический бой](../04-Engineering/TacticalBattle.md),
[сохранения](../04-Engineering/SaveFormat.md), [экономика и мир](../04-Engineering/TradingAndSolarSystem.md),
[отладочные параметры](../04-Engineering/DebugTuning.md).

Источники: [DeepSpaceSaga.sln](../../../DeepSpaceSaga.sln), [global.json](../../../global.json), [Directory.Build.props](../../../Directory.Build.props), [Program.cs](../../../src/DeepSpaceSaga.Client/Program.cs), [DeepSpaceSaga.Client.csproj](../../../src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj).

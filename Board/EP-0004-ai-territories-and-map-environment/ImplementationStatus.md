# EP-0004 — отчёт исполнения

Дата начала: 2026-10-08. Исполнение разрешено пользователем по `D:/DeepSpaceSaga/DSS/Board/EpicExecutionPrompt.md`. Статус: подготовка; ни один тикет реализации пока не завершён.

## База и изоляция

- Исходная ветка DSS: `base-fight`; опубликованный upstream `origin/base-fight` проверен через `git ls-remote`: `4233f52346b7d77edc4e04d677f52d7328353fda`.
- Отдельная рабочая копия: `D:/DeepSpaceSaga/DSS-EP-0004`, ветка `codex/ep-0004-ai-territories`, база `4233f52`.
- Remote: `origin`, `https://github.com/SuhilovVitaly/DSS`. Публикация только новой ветки; `push --dry-run` прошёл.
- Чужой неопубликованный `02c89d4`, изменения EP-0008 и untracked `Board/EP-0001-trading-system/ImplementationStatus.md` / `Board/EpicExecutionPrompt.md` остались в исходной рабочей копии и исключены.

## Порядок

US-0001 → US-0002 → US-0003 → US-0004 → US-0005 → US-0006 → US-0007 → US-0008 → US-0009. В каждой истории — порядок зависимостей TK; отдельная проверка/review/коммит/push до следующего тикета. Всего 9 историй, 26 тикетов.

## Подготовительные проверки

- `dotnet restore DeepSpaceSaga.sln --ignore-failed-sources`: PASS после разрешённого запуска вне sandbox (в sandbox чтение пользовательского NuGet.Config недоступно).
- `dotnet test tests/DeepSpaceSaga.Engine.Tests --no-restore --filter "FullyQualifiedName~FullCluster|FullyQualifiedName~KnownMap" --logger "trx;LogFileName=ep4-dependencies.trx"`: 3/3 PASS. Это узкая проверка зависимостей, не приёмка EP-0004.
- Native EP-0004: NOT RUN; функциональных слоёв ещё нет.

## Публикации

Тикеты реализации ещё не опубликованы. Подготовительный коммит Board не заменяет коммиты тикетов.

## Технические допущения

Текущие требования имеют приоритет над историческими planning-only ограничениями. Поля и территории не создают gameplay effects; базы ИИ не становятся человеческими рынками. Конкретные API сверяются с текущим кодом; необходимые изменения scope фиксируются в карточке до реализации. Граф — навигация, не runtime evidence.

## US-0001 / TK-0001

Контракт AiMap реализован, Contracts 167/167 PASS, scoped format PASS. Подготовительный commit 20ea959 опубликован; remote SHA проверен. Первый auto-review отказ снят после read-only проверки public origin и явного разрешения в промте. Dependency probes: Engine 7/7, Client 11/11 PASS. Native не требуется для DTO. Graphify документации: 150 nodes/166 edges; 3 обратные пары объединены undirected graph, dangling endpoints 0; граф не является runtime evidence.

## Проверка внешних зависимостей

Полностью прочитаны карточки непосредственно используемых EP-0002 US-0006/US-0008 и EP-0003 US-0002/US-0003/US-0004/US-0006/US-0008, их тикеты; внешние входы прослежены до EP-0001 resource/trade/save и EP-0002 orbital contracts. Коммиты поставок EP-0002 являются ancestors базы; код, именованные тесты и итоговые отчёты EP-0002/EP-0003 сверены. Свежие узкие dependency tests: Engine 7/7, Client 11/11 PASS. Исторические native/corpus цифры не считаются EP-0004 acceptance.

Расхождения планов учтены: текущий SaveFormat=15; шесть сценариев с PlayerShipOnly; BeltMapData.DecorationSamples optional; ClusterResourceBinding является поставленным именем; continuation хранит revision high-water и следующий календарный час, а не новый allocator. Открытая география не раскрывает удалённые котировки и не заменяет scan; открытые cluster-resource данные ограничены cluster mode. Финальные native EP-0003 результаты scripted, human playthrough не заявлен.

## US-0001 / TK-0002

Генерация баз и materialized Save/Load wiring завершены. Targeted 5/5; Engine full 1765/1765 PASS (12m12s); scoped format и diff check PASS. TK-0001 опубликован 798226f. Геометрия территорий и доступ к базам — следующие тикеты.

## US-0001 / TK-0003

AI dock/trade gates завершены; Engine без correctness corpus 1669/1669 PASS, format/diff PASS. TK-0002 опубликован 580e9e6. Legacy/quoted/quote/forged dock/dialogue grant paths проверены.

## US-0001 / TK-0004

Штатные базы включены во всех шести сценариях. Content matrix 36 миров с control без AI; targeted 42/42 и Client full 1737/1737 PASS. TK-0003 опубликован a35f101.

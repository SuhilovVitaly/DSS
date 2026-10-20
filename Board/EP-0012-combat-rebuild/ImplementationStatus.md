# EP-0012 — журнал автономного исполнения

Дата начала: 2026-10-20. Состояние: последовательная реализация по ticket DAG; эпик не завершён.

## Полномочия и исходное состояние

Текущий запрос пользователя на исполнение по `Board/EpicExecutionPrompt.md` разрешает реализацию draft, необходимые изменения scope/Board, проверки, отдельные коммиты и немедленный push каждого тикета. Planning-only ограничения исходных карточек описывают прежнее поручение и не ограничивают это исполнение.

Baseline: `c894925d187c90a658d1e25958feed69a45d7e41`. Исходная ветка `fight-rebuild` без upstream. Единственное исходное изменение — untracked папка данного эпика; исходные карточки сохранены. Рабочая ветка: `codex/ep-0012-combat-rebuild`; подтверждённый remote: `origin`, `https://github.com/SuhilovVitaly/DSS`.

Выполнение следует 73 шагам [Tickets.md](Tickets.md). Последней остаётся US-0015, зависящая от всех функциональных историй. Подготовительные коммиты не заменяют публикации тикетов.

## Подготовка и фактические проверки

- До кода завершено полное чтение всех 88 Markdown Documentation и 95 документов исходного эпика: 183/183. Навык Graphify явно предусматривает semantic-extraction подагентов; три подагента прочитали соответственно 57/68/58 файлов и подготовили 12 проверенных пакетов. Исполнитель сверил сводки и первичные контракты текущего шага. Это извлечение документации, не независимое review кода.
- Инвентаризация: 88 Markdown в Documentation, 95 Markdown в исходном пакете эпика; 19 историй, 73 тикета. Структурная проверка с добавленным журналом: 96 документов, 638 локальных ссылок, 0 ошибок ID/parents/порядка/DAG/ссылок. Итоговый граф ещё не перестроен.
- Навигация через существующий `src/graphify-out/graph.json`: запрос TorpedoSnapshot/ObjectMotionSnapshot/CountermeasureSnapshot. Граф — навигация; consumers дополнительно проверяются по `.cs`, без машинных TRX-журналов.
- `git ls-remote --heads origin fight-rebuild main`: GitHub доступен вне песочницы, main совпадает с baseline; удалённой fight-rebuild нет. В песочнице DNS недоступен.
- `git push --dry-run origin HEAD:refs/heads/codex/ep-0012-combat-rebuild`: PASS; remote этим действием не изменён.
- `dotnet build DeepSpaceSaga.sln --no-restore --verbosity minimal` вне песочницы: PASS, 0 warnings/errors, 18.36 s.
- `dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --no-build --verbosity minimal` вне песочницы: PASS, 212 passed, 0 failed/skipped, 176 ms тестов. Процесс ограничен 300000 ms; при превышении завершается его дерево.
- Исходный запуск Contracts tests в песочнице: ABORTED, VSTest не подключился к testhost за 90 s. Это ограничение среды, не подтверждённый дефект продукта. Внешний повтор прошёл.

## Правила перехода

Каждый шаг: актуальный контракт и scope → production + matching tests → обязательные проверки → self-review → evidence → отдельный commit → push → проверка remote SHA → следующий шаг. Self-review не называется независимым review.

Старые DTO сохраняются только на время явно описанной миграции потребителей; совместимость промежуточной сборки не означает закрытия итогового удаления legacy. Общий build/test gate обязателен после миграции.

## Публикации и приёмка

Подготовительный Board/journal commit `cb6d67253d6fe192ee4f4bbcc24002b0cc6fa5af` опубликован; local/remote SHA проверены. Первый push отклонён auto-review из-за неподтверждённой принадлежности destination и scope подготовительной публикации. Дополнительная read-only проверка GitHub подтвердила user/owner `SuhilovVitaly`, ID98092169, admin/push=true и совпадение origin; раздел 2 EpicExecutionPrompt явно разрешает служебный Board commit/push. Повтор с этими доказательствами разрешён и выполнен, обхода отказа не было.

| Шаг / тикет | Реализация и проверки | Публикация |
|---|---|---|
| 1 / US-0001-TK-0001 projectile-contracts | PASS 12 named / 224 Contracts; build/format/diff PASS | `090b3635822f100bbe11105e2840f585d1294ec8`, origin SHA verified |
| 2 / US-0001-TK-0002 command-contracts | PASS 6 named / 230 Contracts; build/format/diff PASS | `be0078caf61fe4f08ed92a081ce932f5a441f9d6`, origin SHA verified |
| 3 / US-0001-TK-0003 encounter-contracts | PASS 6 named / 236 Contracts; build/format/diff PASS | `c3978c9cb3dda2c030bbe10e42d6c3839827387c`, origin SHA verified |
| 4 / US-0001-TK-0004 combat-rules-schema | PASS 24 named / 65 targeted regression; build/format/diff PASS; full Engine TIMEOUT 300s, gate OPEN | `c19dafdf185ac279133cb78afcdef795969912d5`, origin SHA verified |
| 5 / US-0001-TK-0005 launcher-profile-schema | PASS 39 Engine named / 387 bounded regression; 3 Contracts named / 239 full; solution build/format/diff PASS | `f06c235c84d5010d8497731b68be549930874b77`, origin SHA verified |
| 6 / US-0017-TK-0001 combat-event-contracts | PASS 6 named / 245 full Contracts; build/format/diff PASS | `7abd93815e6765586f5cc684752cec887ca18c4e`, origin SHA verified |
| 7 / US-0017-TK-0004 turn-and-summary-command-contracts | PASS 6 named / 251 full Contracts; build/format/diff PASS | `012963afb6a531e767d5e6ef531cc60c5ddd4ae8`, origin SHA verified |
| 8 / US-0003-TK-0001 blast-math | PASS 32 named / 222 full Motion; build/format/diff PASS | `b197271fe27c121bb98bd31ad81d0b1af1c0ab2d`, origin SHA verified |
| 9 / US-0006-TK-0001-blast-threat-math | PASS 8 named / 230 full; build/format/diff PASS | `52355e437b9395a24fc9e46e83358e61ea1bdb6f`, origin SHA verified |
| 10 / US-0018-TK-0004-shared-point-forecast | PASS 7 named / 237 full; build/format/diff PASS | SHA и remote проверяются сразу после коммита |

Публикация следующего тикета начинается только после проверки SHA предыдущего. Собственный SHA текущего коммита заранее не записывается.

Automated epic acceptance OPEN; native/product/balance NOT RUN / OPEN. Игра не запускалась. Исторические результаты исходных документов не считаются результатами этого исполнения.

## Catalog activation ordering correction

US-0002-TK-0001: экспериментальные source/deployed catalog checks PASS 2/2, owning build/format PASS. Настоящие scenario tests FAIL 3/4: старый BuildCombatState требует TorpedoSpeedKmS/TurnRate, которых в v3 нет. Production JSON и экспериментальный test возвращены к исходному состоянию до публикации. Тикет остаётся draft, активация перенесена после Engine runtime. Фактические результаты проверки сохранены отдельно; это дефект исходного порядка зависимостей, не baseline failure.

Полный Client experimental run: 1934 passed / 197 failed / 0 skipped, 110792 ms, без timeout. [Исходные команды и вывод](evidence/catalog-activation-ordering-probe.json). Отдельный scenario probe: 1 passed / 3 failed. Ни один failed run не назван PASS.

После восстановления production JSON повторная сборка и настоящий scenario probe PASS 4/4, без failed/skipped. Восстановлен работавший путь загрузки; новый каталог ожидает Engine. Board validation: 19 stories / 73 tickets, 651 references, 0 errors.

Служебная коррекция DAG опубликована `9de88536c6c311d0b464c622e6e73fe3c76ace9a`, origin SHA verified. Новая нумерация шагов соответствует текущему Tickets.md, идентичности тикетов сохранены.

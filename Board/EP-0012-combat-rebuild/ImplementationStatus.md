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
| 10 / US-0018-TK-0004-shared-point-forecast | PASS 7 named / 237 full; build/format/diff PASS | `5c9cac3f0104c2baa6209fb37c618a62bb82fd4e`, origin SHA verified |
| Fix / US-0017-TK-0001 locale consumer | Reproduced full Client 2128 passed / 1 failed; locale correction PASS 3/3, original assertions retained | `809bd18e3d2d226927efe7438eccb74a2d9d1c12`, origin SHA verified |
| 11 / US-0003-TK-0002-torpedo-point-launch | PASS 15 named / 242 bounded regression; build/format/diff PASS | `ce2dcf9d55c9656cd32a6158551647dcb002ec5d`, origin SHA verified |
| 12 / US-0003-TK-0003-torpedo-area-detonation | PASS 13 named / 273 bounded regression; build/format/diff PASS | `e1e34582d43d87194c0352e8103f31d02572adca`, origin SHA verified |
| 13 / US-0003-TK-0004-manual-detonation | PASS 4 named / 289 bounded regression; build/format/diff PASS | `40ded2cbb65ee98eca483b889238e465a1f041bf`, origin SHA verified |
| 14 / US-0004-TK-0001-countermissile-point-launch | PASS 11 named / 341 bounded regression; build/format/diff PASS | `8a92cac30bfae7e7c148362254fce9a51baf97fa`, origin SHA verified |
| 15 / US-0004-TK-0002-countermissile-detonation | PASS 5 named / 346 bounded regression; build/format/diff PASS | `d834c15e8341c273a82fda39375a3dd7fb27f280`, origin SHA verified |
| 16 / US-0006-TK-0002-evade-command | PASS 14 named / 487 bounded regression; build/format/diff PASS | `2cd6a430af37d4a8196a8ca5ca03c11ac7bcec2f`, origin SHA verified |
| 17 / US-0006-TK-0003-continuous-turn-in-combat | PASS 7 named / 494 bounded regression; build/format/diff PASS | `04e173b7f4fc3ac30380c53f6bdab45e8c70aaa3`, origin SHA verified |
| 18 / US-0007-TK-0001-orbit-lead-fix | PASS 6 named / 249 full owning tests; build/format/diff PASS | `5db5347c9b628db31c5d16ca8bbf93926739e979`, origin SHA verified |
| 19 / US-0007-TK-0002-npc-aim-and-fire | PASS 16 named / 523 bounded regression; build/format/diff PASS | `2cddcf035e23c8e29fbb67f0cbda9e4b9c4d47cf`, origin SHA verified |
| 20 / US-0007-TK-0003-npc-evade-and-reaction | PASS 12 named / 627 bounded regression; build/format/diff PASS | `d340dccbfce90182d04f8f37c17e2252b8d75f9b`, origin SHA verified |
| 21 / US-0008-TK-0001-encounter-state | PASS 13 named / 640 bounded regression; build/format/diff PASS | `a4a9acd31dce15ad17b5cce7ffb412723e229a8c`, origin SHA verified |
| 22 / US-0008-TK-0002-combat-speed-lock | PASS 11 named / 688 bounded regression; build/format/diff PASS | `ca3eda12838d90908f6a64e4b53528eea059230c`, origin SHA verified |
| 23 / US-0008-TK-0003-encounter-stats | PASS 8 named / 696 bounded regression; build/format/diff PASS | `1db6e9c3682b337ff2431152a819c1e6aa575552`, origin SHA verified |

| fix / US-0018-TK-0004 | Reproduced4 failures +nonship3; PASS17/259 Motion,9/705 bounded Engine,256 Contracts; solution/format/diff PASS | `ebf66d6585269f22477e422c4f4df0a074008e57`, origin SHA verified |
| 24 / US-0018-TK-0003-point-combat-forecasts | PASS 4 named / 709 bounded regression; build/format/diff PASS | `b23f38cd7a28634f7af1c44b7632f89b886fe8d3`, origin SHA verified |
| 25 / US-0017-TK-0002-engine-report-producers | PASS 12 named / 775 bounded regression; build/format/diff PASS | `891fe86209c41d8031b279780ecf3eacfb83ebc6`, origin SHA verified |

| fix / US-0008-TK-0003 | Reproduced stale-owner counter0 vs expected1; PASS21 relevant /776 bounded Engine; solution/format/diff PASS | `91b5c79ab12c1325bee68d1778b4525843a806e2`, origin SHA verified |
| 26 / US-0005-TK-0001-snapshot-masking | PASS 5 named / 781 bounded regression; build/format/diff PASS | `479fef0765e7981c0a078cea2f9b00e7f419ac57`, origin SHA verified |

| fix / US-0008-TK-0002 | Reproduced Active compact frames Speed4 vs1; PASS12 named /782 bounded Engine; solution/format/diff PASS | `6842980485c140e2b3328b722e8186065f396a36`, origin SHA verified |
| 27 / US-0005-TK-0002-hidden-fields-leak-test | PASS 3 named / 785 bounded regression; build/format/diff PASS | `533db328038655df67260e15582d391de1fc5ecf`, origin SHA verified |
| 28 / US-0017-TK-0003-local-client-delivery | PASS 1 named / 2124 bounded regression; build/format/diff PASS | `e0822e55287708680c6d0f885dfcd99af4d7ac2e`, origin SHA verified |
| 29 / US-0019-TK-0003-client-forecast-cache-migration | PASS 15 named / 2138 bounded regression; build/format/diff PASS | `5a129a9bbf4780f9959a1ecc3ff512d73048414c`, origin SHA verified |
| 30 / US-0010-TK-0001-combat-visuals-v3 | PASS 10 named / 2148 bounded regression; build/format/diff PASS | `a6dfe06add453668b0c3f3b95c9bef697b4c5168`, origin SHA verified |
| 31 / US-0010-TK-0002-projectiles-and-blasts-render | PASS 11 named / 2159 bounded regression; build/format/diff PASS | `44a8c6fbc2ede01ed4b9852f8903238b82225ff9`, origin SHA verified |
| 32 / US-0010-TK-0003-persistent-range-rings | PASS 4 named / 2163 bounded regression; build/format/diff PASS | `dbee25bf9e0455003a0f6c2c1c875fc34cbee1f3`, origin SHA verified |
| 33 / US-0010-TK-0004-own-path-forecast-and-fit | PASS 8 named / 2171 bounded regression; build/format/diff PASS | `b0e219218c748c22601ceb336a429282d3fe9c8f`, origin SHA verified |
| 34 / US-0011-TK-0001-aim-mode-and-input-map | PASS 13 named / 2184 bounded regression; build/format/diff PASS | `5478dfefb3a1d13d65ae3a1297b19c600ba89e7a`, origin SHA verified |
| 35 / US-0011-TK-0002-torpedo-aim-preview | PASS 14 named / 2198 bounded regression; build/format/diff PASS | `99c5f13b31864f602a0c9999bcd6eca6f5470736`, origin SHA verified |
| 36 / US-0011-TK-0003-countermissile-aim-preview | PASS 10 named / 2208 bounded regression; build/format/diff PASS | `d9fb3a66144d950b62bb6c92d9a3d7ec57db6e12`, origin SHA verified |
| 37 / US-0011-TK-0004-launch-commands-and-reasons | PASS 7 named / 2215 bounded regression; build/format/diff PASS | `762723844c69b1a455ff037274e95e0a6fec8842`, origin SHA verified |

US11 TK0004 dispatcher locale follow-up: PASS 7 named/scoped format; runtime unchanged. `2d59fe4b659ac3341d4fc5431d7218d520faccfc`, origin SHA verified.

| 38 / US-0012-TK-0001-evade-input-and-threat-select | PASS 12 named / 2227 bounded regression; build/format/diff PASS | `4ed46b208c9e14a8adf838dc1587133a70abccfd`, origin SHA verified |
| 39 / US-0012-TK-0002-threat-panel | PASS 9 named / 2236 bounded regression; build/format/diff PASS | `f2b2b08daa61ca6ae253ca58280854f0e4a2ddfd`, origin SHA verified |
| 40 / US-0012-TK-0003-threat-alerts | PASS 22 named / 2258 bounded regression; build/format/diff PASS | `52c22c428249e3cf1c68a75d3e473e9a6a8d9411`, origin SHA verified |
| 41 / US-0013-TK-0001-module-diagram | PASS 7 named / 2265 bounded regression; build/format/diff PASS | `e8871cb460b1302e076c68187973ffc0baeab6b9`, origin SHA verified |
| 42 / US-0013-TK-0002-weapon-panels-v3 | PASS 8 named / 2273 bounded regression; build/format/diff PASS | `ce323d30fc8e999e2436f2ac814a56e07b42e5e4`, origin SHA verified |
| 43 / US-0013-TK-0003-enemy-badges-and-hit-labels | PASS 13 named / 2286 bounded regression; build/format/diff PASS | `7ca95b70d6708405b9792fc31dc66f42e4eef311`, origin SHA verified |
| 44 / US-0013-TK-0004-journal-and-reports-v3 | PASS 21 named / 2307 bounded regression; build/format/diff PASS | `ff74e9bbe6a584b96e483fbd875fff862ff3f0df`, origin SHA verified |
| 45 / US-0013-TK-0005-combat-start-timer-summary | PASS 15 named / 2322 bounded regression; build/format/diff PASS | `69c5d34090e3072fa35eedb8ce0d60be1bbb2ae9`, origin SHA verified |
| 46 / US-0016-TK-0001-combat-localization | PASS 8 named / 2330 bounded regression; build/format/diff PASS | `d112cb2536790a7b65d4a98ea6f716a4a450023f`, origin SHA verified |
| 47 / US-0009-TK-0003-save-format-18 | PASS 19 named / 809 bounded regression; build/format/diff PASS | `babcc7c6a4903e6271382a253c6fcba1b55908c4`, origin SHA verified |
| 48 / US-0009-TK-0004-save18-capture-and-restore | PASS 16 named / 825 bounded regression; build/format/diff PASS | `d55a19b02a54f7f81b3f3fc9c902198f2155b81d`, origin SHA verified |
| 49 / US-0009-TK-0005-save18-defense-validation | PASS 26 named / 851 bounded regression; build/format/diff PASS | `cc94ea5f8b403c71f1d9b918cb61f5d801351e0b`, origin SHA verified |
| 50 / US-0002-TK-0001-torpedo-launchers-v3 | PASS 2 named / 2332 bounded regression; build/format/diff PASS | `32dc20289c598edbd59b342db3fd11b621c119c4`, origin SHA verified |
| 51 / US-0002-TK-0002-countermissile-launcher-v3 | PASS 2 named / 2334 bounded regression; build/format/diff PASS | `4c760ea66b7da27e6842a3d8fa77b61096a8f1ce`, origin SHA verified |
| 52 / US-0002-TK-0003-engines-v3 | PASS 2 named / 2336 bounded regression; build/format/diff PASS | `5454d93d3c5791fe3b3f5886402732d22fed6670`, origin SHA verified |
| 53 / US-0002-TK-0004-remove-modes-and-legacy-commands | PASS 2 named / 2338 bounded regression; build/format/diff PASS | SHA и remote проверяются сразу после коммита |

Публикация следующего тикета начинается только после проверки SHA предыдущего. Собственный SHA текущего коммита заранее не записывается.

Automated epic acceptance OPEN; native/product/balance NOT RUN / OPEN. Игра не запускалась. Исторические результаты исходных документов не считаются результатами этого исполнения.

## Catalog activation ordering correction

US-0002-TK-0001: экспериментальные source/deployed catalog checks PASS 2/2, owning build/format PASS. Настоящие scenario tests FAIL 3/4: старый BuildCombatState требует TorpedoSpeedKmS/TurnRate, которых в v3 нет. Production JSON и экспериментальный test возвращены к исходному состоянию до публикации. Тикет остаётся draft, активация перенесена после Engine runtime. Фактические результаты проверки сохранены отдельно; это дефект исходного порядка зависимостей, не baseline failure.

Полный Client experimental run: 1934 passed / 197 failed / 0 skipped, 110792 ms, без timeout. [Исходные команды и вывод](evidence/catalog-activation-ordering-probe.json). Отдельный scenario probe: 1 passed / 3 failed. Ни один failed run не назван PASS.

После восстановления production JSON повторная сборка и настоящий scenario probe PASS 4/4, без failed/skipped. Восстановлен работавший путь загрузки; новый каталог ожидает Engine. Board validation: 19 stories / 73 tickets, 651 references, 0 errors.

Служебная коррекция DAG опубликована `9de88536c6c311d0b464c622e6e73fe3c76ace9a`, origin SHA verified. Новая нумерация шагов соответствует текущему Tickets.md, идентичности тикетов сохранены.

2026-10-20: уточнены реальные prerequisites legacy deletion по source consumers Save/forecasts/Client. Пять deletion/regression tickets отложены до завершения consumer migration, перед balance. Новый следующий шаг — evade-command, уже использует готовые public observed trace и BlastThreatMath; отсутствие legacy deletion не является его runtime prerequisite. Legacy-removal AC остаются OPEN, ничего не объявлено удалённым.

2026-10-20: consumer prerequisite DAG correction before data activation; next Engine point-forecast integration, then real report/privacy/transport/Client consumers. US2 rules/Settings/modes switch kept atomic; all production activation, Save18, legacy-removal and native gates remain OPEN. See Tickets.md consumer correction.

Save18 prerequisite correction: second catalog activation probe reverted; schema/capture/validation now follow localization before US2. Results preserved; no data ticket closed.

US2 atomic staging correction before component implementation: Data/CombatV3 components are source/deployed loader-verified but inactive until US2-TK5, which switches the complete catalog and category definitions. Full production integration remains OPEN until that switch and scenario/fixture migration.

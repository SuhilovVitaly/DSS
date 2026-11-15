# EP-0019 — частичное исполнение 2026-11-08

**BLOCKED, не завершён.** Выполнены 2 из 21 тикета; ни одна история целиком не завершена. Новый UI ещё не реализован. Последний опубликованный implementation ticket: EP-0019-US-0001-TK-0002.

## Ветка и публикация

- Ветка: `codex/ep-0019-tactical-battle-ui-redesign`.
- Remote: `origin`, `https://github.com/SuhilovVitaly/DSS`.
- База: `f9fcbd45`; исходное дерево чистое. После fetch `origin/main` = `50d96441`, его tree совпадает с базой; перенос чужих изменений не требовался.
- Отдельный push сразу после каждого завершённого тикета; SHA проверены через `git ls-remote`. Force push и изменение опубликованной истории не применялись.

| Тикет | SHA | Push |
|---|---|---|
| EP-0019-US-0001-TK-0001 | `33ad15adacf7408d111476f7a6551301b91d9bbf` | подтверждён в origin |
| EP-0019-US-0001-TK-0002 | `476bc097388d65b3e9b08f77b471e06b054fb02d` | подтверждён в origin |

Поздний отдельный Board-коммит фиксирует блокер и этот отчёт; его собственный SHA проверяется после создания через Git, а не записывается заранее.

## Реализованное и покрытие

| Критерий | Результат |
|---|---|
| US1-AC1 | Два вызова градиента используют явные SKColor[] с прежними цветами/позициями/режимом. На установленном SDK 10.0.400 сборка PASS; SDK 8.0.4xx NOT RUN, не установлен. Baseline на SDK 10 тоже собирался, воспроизведение CS0121 на SDK 8 не заявляется. |
| US1-AC2, часть TK-0002 | Точные DejaVu Regular/Bold и лицензия поставляются в build/publish. Factory задаёт настройки DesignSpec, глифы и ширины проверены. Подключение всего экрана к factory ещё не выполнено (US-0002). |
| US1-AC3–AC5 | BLOCKED до реализации: отсутствует reference данных для харнесса. |
| US2–US6 | NOT RUN / не реализованы, зависят от US1 TK-0003. |
| US7 | Только подготовительное расширение scope по §6 EpicExecutionPrompt; история не выполнена, актуализация контрактов и Graphify rebuild не проводились. |

TK-0001 меняет только форму перегрузки в двух существующих строках Client. TK-0002 добавляет file-backed typography, семь тестов и точные font assets; остальная игра продолжает использовать прежние шрифты. Engine, Contracts, Motion, RNG и Save20 не менялись. Карта тикетов восстановлена из канонических карточек (21 строка вместо пустых).

## Блокер и попытки восстановления

`Assets/battle-data-seed1.txt` отсутствует в рабочей копии, исходном `f9fcbd45`, актуальном `origin/main` и всей доступной Git-истории; поиск workspace, включая ignored artifacts, также не нашёл файл. В исходном Assets отслеживался только README. TK-0003, шаги 1/4, и DesignSpec §12.1 требуют независимый reference поз, приказов, целей/шансов и FireSteps ходов 1/3. Полных значений в DesignSpec §12.3 нет. Подстановка output текущего Engine вместо неизвестного reference не подтверждает требуемое совпадение; такой oracle и фиктивный PASS не созданы.

Шрифты тоже отсутствовали, но их удалось восстановить из официального Ubuntu `fonts-dejavu-core_2.37-2build1_all.deb`: оба исходных SHA совпали побайтно. Локальный шрифт Poppler и новый Ubuntu `2.37-8` проверены и отвергнуты из-за других хешей. Font stanza лицензии восстановлена из copyright старого пакета; удалён только лишний whitespace. Происхождение записано в [Assets/README](Assets/README.md).

Для продолжения нужен исходный `Assets/battle-data-seed1.txt` из сессии планирования либо независимая восстановленная версия с проверяемым происхождением. Следующий тикет — **EP-0019-US-0001-TK-0003-scene-harness**. После него остаётся ещё 18 тикетов (US-0002: 4; US-0003: 3; US-0004: 5; US-0005: 3; US-0006: 2; US-0007: 1). Ожидания DesignSpec и допуски сохранены.

## Фактические проверки

ОС Windows, SDK 10.0.400; каждый тестовый процесс ограничен 300000 ms с завершением дерева при превышении. Полные команды сборки/форматирования также указаны в карточках.

| Проверка | Наблюдаемый результат |
|---|---|
| `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore -v:normal` | PASS, 0 warnings/errors, baseline до изменения градиентов |
| `dotnet build DeepSpaceSaga.sln --no-restore` после TK-0001 | PASS, 0 errors, 3 исходных MSB9008 о ссылке на отсутствующий архивный ModApi |
| `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~BattleTypographyTests --results-directory D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-setup/tk2-results --logger trx;LogFileName=focused.trx` | PASS 7/7, skip 0; test build PASS, исходный warning MSB9008 |
| Scoped `dotnet format whitespace … --verify-no-changes` для двух Gradient-файлов, затем для BattleTypography и BattleTypographyTests | PASS; при загрузке test workspace предупреждения об исходной ссылке ModApi |
| `dotnet publish src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Debug --no-build --no-restore -o D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-setup/publish` | PASS, оба SHA совпали с Board, лицензия присутствует |
| `git diff --check`, `git diff f9fcbd45 --check` | PASS после исправления whitespace восстановленной лицензии |
| Self-review исходников, тестов и diff | Выполнен, независимое review не заявляется |
| SDK 8 / macOS / Ubuntu | NOT RUN: недоступны; WSL не установлен |
| Native UI/GPU/физический ввод, семь сцен, пробы, regression snapshots, полная tactical suite | NOT RUN; экран не изменён, харнесс заблокирован |

## Evidence и уборка

Корень текущего evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-setup/`.

Сохраняются только используемые этим отчётом файлы: `baseline-build.log`, `tk1-solution-build.log`, `tk1-format.log`, `tk2-tests.log`, `tk2-format.log`, `tk2-publish.log`, `input-audit.txt`, `tk2-results/focused.trx`, `cleanup.txt`. `input-audit.txt` содержит SHA runtime/publish файлов и исходные Git tree/history проверки. Оригинальные TTF и лицензия сохранены в Board/runtime и опубликованы.

Временные пакеты, распакованные кандидаты и publish удаляются после проверки; task-owned фоновых процессов нет. Диагностические каталоги в корнях workspace/repository не создавались; дополнительный checkout/worktree не создавался. Итоговая проверка уборки и списка корневых каталогов сохраняется в `cleanup.txt` перед публикацией этого отчёта.

Текущая документация игрового экрана не переписана под ещё не существующее поведение. История документации остаётся последней и незавершённой.

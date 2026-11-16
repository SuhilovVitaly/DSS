---
epic: EP-0030-debug-console
story: EP-0030-US-0003-kill-drone
title: Уничтожение дрона или тэга по имени
stage: approved
dependencies: [EP-0030-US-0001-global-console]
revision: 2
---

# US-0003 — Уничтожение дрона или тэга по имени

## Связь с техническим заданием

D-05 [эпика](../Documentation.md): «добавь команду kill_drone name которая уничтожает в бою дрона или тэга по имени». Область — текущий активный бой, а не только обучение; правила A-07/A-08 предложены для review.

## User story

Как оператор, я хочу выполнить `kill_drone name`, чтобы уничтожить указанного по имени дрона или тэга в текущем бою. Команда применима к своим и вражеским целям в обучении и обычном бою. Я получаю подтверждение с именем уничтоженной цели либо понятную ошибку без случайного выбора другого объекта. Уничтожение сразу отражается в боевом состоянии, карте, карточках и очереди действий; при выполнении условий завершения бой получает обычный исход. Потеря учитывается при штатном завершении боя, в том числе через `exit_battle`, и не отменяется закрытием консоли.

## Зависимости

EP-0030-US-0001-global-console — ввод команд и вывод результата. US-0002 не блокирует реализацию этой истории; совместная проверка с `exit_battle` выполняется после обеих историй.

## Предлагаемый контракт команды

- Синтаксис: `kill_drone name`. `name` — весь непустой остаток строки с удалёнными краевыми пробелами. Например, `kill_drone Alpha 1` обозначает одну цель с именем `Alpha 1`, а не несколько аргументов. Кавычки не обязательны и отдельный язык shell не вводится.
- Поиск (G-04, решение пользователя 2026-11-16): точное Ordinal-совпадение trimmed имени с Unit Id ИЛИ с подписью на карте `Build?.PilotName ?? Id`, с учётом регистра. Не используется подстрока или автоматический выбор первого совпадения. Совпадение с несколькими разными юнитами — отказ с сообщением о неоднозначности. Цели — юниты `Kind=Drone` любой стороны; носители отклоняются.
- Проверка принадлежности нужному бою и уничтожение выполняются авторитетно; UI показывает успех только после подтверждения. Engine-граница: `DebugDestroyTacticalUnitRequest` (TK-0001..TK-0003).
- Уничтожение обходит попадание и защиту, не создаёт обычный выстрел, не расходует торпеды или RNG и не выполняет следующий ход. Применяются штатные последствия гибели и проверка исхода, а не только изменение видимости или одной цифры корпуса.
- Консоль остаётся открытой с результатом, кроме перехода, который необходим по штатному исходу боя. Пауза сама по себе не препятствует команде. При незавершённой конкурентной операции команда получает «занято», а не применяется к устаревшему состоянию.
- Повтор для уже уничтоженной цели сообщает «Цель уже уничтожена» без повторных эффектов. Нет боя, нет имени, цель не найдена, неоднозначное имя и неподдерживаемый тип (например, носитель) — ошибки без мутации.

## Completion evidence

- **AC-01:** в учебном и обычном бою команда по уникальному имени уничтожает ровно указанного дрона или тэга любой стороны; остальные юниты не получают прямого урона от команды.
- **AC-02:** имя с пробелами выбирает одну цель; пустое, неизвестное, неоднозначное имя и неподдерживаемый тип возвращают ошибку без изменения боя.
- **AC-03:** уничтоженная цель больше не двигается и не стреляет; карта, карточка, доступные цели и очередь согласованы с подтверждённым состоянием. Во время анимации старые кадры не возвращают цель к жизни и не выполняют её отменённые действия.
- **AC-04:** команда работает на паузе и в доступных фазах, не меняет скорость, не продвигает ход и не расходует боезапас/RNG. Конкурентная операция даёт явный отказ или завершается до применения команды с повторной проверкой цели.
- **AC-05:** уничтожение последней необходимой цели запускает штатную проверку исхода боя; потери своей цели сохраняются при завершении/прерывании боя, включая `exit_battle`, без повторного settlement.
- **AC-06:** повтор/повторная доставка не дублирует последствия; устаревшая команда из прежнего боя/сессии не уничтожает одноимённую цель нового боя. Вне боя команда безопасно отклоняется.

## Ограничения и gaps

**G-04 решён** пользователем 2026-11-16: «Id или подпись на карте». Отдельного типа «тэг» в данных нет (`TacticalBattleSnapshot.cs:7` — Drone и Carrier); «дрон или тэг» = юнит `Kind=Drone`. A-07/A-08 приняты исполнителем как технические допущения автономного исполнения 2026-11-16 (это не решение пользователя). Носители, массовое уничтожение, поиск по маске и произвольное изменение здоровья вне объёма. Проверки запланированы в тикетах.

## Решения 2026-11-16

- **G-04** (пользователь): «Id или подпись на карте» — сопоставление по Unit Id или подписи `Build?.PilotName ?? Id`; несколько разных юнитов → ошибка неоднозначности; носители отклоняются.
- Пользователь о декомпозиции: «Декомпозировать сам».
- Оператор: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».

Технические допущения исполнителя (не решения пользователя): Disabled-дроны допустимые цели; во время показа результатов залпа команда отвечает «занято».

## Ticket map

| Тикет | Layer | Depends on | Criteria |
|---|---|---|---|
| [TK-0001 — destroy-contract](EP-0030-US-0003-TK-0001-destroy-contract/EP-0030-US-0003-TK-0001-destroy-contract.md) | contracts | none; prerequisite US-0001 | AC-04, AC-06 |
| [TK-0002 — engine-force-destroy](EP-0030-US-0003-TK-0002-engine-force-destroy/EP-0030-US-0003-TK-0002-engine-force-destroy.md) | engine | EP-0030-US-0003-TK-0001-destroy-contract | AC-01, AC-03..AC-06 |
| [TK-0003 — local-passthrough](EP-0030-US-0003-TK-0003-local-passthrough/EP-0030-US-0003-TK-0003-local-passthrough.md) | local-client | EP-0030-US-0003-TK-0002-engine-force-destroy | AC-01, AC-06 |
| [TK-0004 — handle-flow](EP-0030-US-0003-TK-0004-handle-flow/EP-0030-US-0003-TK-0004-handle-flow.md) | client | EP-0030-US-0003-TK-0003-local-passthrough | AC-03, AC-04, AC-06 |
| [TK-0005 — kill-command](EP-0030-US-0003-TK-0005-kill-command/EP-0030-US-0003-TK-0005-kill-command.md) | client | EP-0030-US-0003-TK-0004-handle-flow | AC-01..AC-06 |

## Story test evidence 2026-11-16

Ветка `feature/ep-0030-debug-console`. Артефакты: `D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/`.

| Проект | Фильтр | Итог | trx |
|---|---|---|---|
| Contracts | весь проект | 33/33 passed | `us0003-20261116T073301Z/contracts/sushi_VITA-CASTLE_2026-11-16_09_33_04_net8.0.trx` |
| Engine (первый прогон, `fded9644`) | `DebugDestroyTests\|LocalSessionBattleTests\|ConnectionApiTests\|SessionTests\|GoldenBattleTests` | 243 total, 240 passed, 3 failed | `us0003-20261116T073301Z/engine/sushi_VITA-CASTLE_2026-11-16_09_37_26_net8.0.trx` |
| Engine (финал, `c4dae7da`) | тот же | 243 total, 242 passed, 1 failed (pre-existing) | `us0003-20261116T074117Z/engine/sushi_VITA-CASTLE_2026-11-16_09_41_30_net8.0.trx` |
| Client (первый прогон, `c4dae7da`) | `DebugDestroyFlowTests\|KillDroneDebugCommandTests\|DebugConsole\|LocalizationCoverageTests\|SessionHandleBattleTests` | 69 total, 63 passed, 6 failed | `us0003-20261116T074117Z/client/sushi_VITA-CASTLE_2026-11-16_09_43_52_net8.0.trx` |
| Client (финал, `3269d98a`) | тот же | 69 total, 64 passed, 5 failed (все pre-existing) | `us0003-20261116T074520Z/client/sushi_VITA-CASTLE_2026-11-16_09_45_32_net8.0.trx` |

Новые тесты истории (`DebugDestroyTests` 10, `DebugDestroyFlowTests`, `KillDroneDebugCommandTests`, `DebugConsole*`) в финальных прогонах зелёные.

**Исправления (обе — ошибки тестов, реализация не менялась):**

- `c4dae7da` EP-0030 US-0003 TK-0002 — `DebugDestroyTests.LastEnemyDroneEndsBattleWithOutcome`: сравнение `TacticalOutcomeSnapshot` через record-равенство падало из-за ссылочного сравнения `ImmutableArray`; теперь сравнение по значению через JSON.
- `3269d98a` EP-0030 US-0003 TK-0005 — `KillDroneDebugCommandTests.LastEnemyKillEndsBattleThenExitBattleSettlesOnce`: тест опирался на `TacticalUnitSnapshot.SourceDroneId`, который не заполняется; settlement сообщает tactical unit id — теперь проверка `LostDroneIds == [lost.Id]`.

**Pre-existing падения (не вызваны EP-0030), доказательство на базе `43a6fecf` (временный worktree вне репозитория, удалён):**

- Engine `SessionTests.OptionsAndSnapshotsAreCompleteImmutableAndQueriesConsumeNoRandom` — на базе то же падение (Expected 880, Actual 892): `us0003-20261116T073301Z/baseline-43a6fecf/sushi_VITA-CASTLE_2026-11-16_09_40_35_net8.0.trx`.
- Client `SessionHandleBattleTests.EveryRefusalHasRealLocalizedTextAndUnknownCodesUseGenericMessage` (Russian, English; ключ `TacticalBattle.Refusal.NoAmmo`), `LocalizationCoverageTests.ActualRussianAndEnglishFireTextsContainNoResourceKeys` (Russian, English; `TacticalBattle.Skip.NoAmmo`), `LocalizationCoverageTests.RequiredScreenKeysResolveWithMatchingFormatsInBothPackagedLanguages` (`OutcomeReason.NoCombatCapability`) — на базе те же 5 падений: `us0003-20261116T073301Z/baseline-43a6fecf/client/sushi_VITA-CASTLE_2026-11-16_09_44_26_net8.0.trx`.
- Engine `LocalSessionBattleTests.RealWorldConnectionRejectsSaveAtEveryActivePhaseAndRestoresItOnExit(phase: Move)` — однократное нестабильное падение в первом прогоне (`ScenarioException`: tradingMap saved edge 'SPC-0005'/'SPC-0006' metadata mismatch при перезагрузке сейва). Не воспроизводится: зелёный изолированно на HEAD дважды, в финальном прогоне Engine и на базе `43a6fecf` (тот же baseline trx). EP-0030 не меняет trading map/scenario/save-код (`git diff 43a6fecf..HEAD -- src` затрагивает только debug destroy и консоль). Классифицировано как pre-existing flaky; полный Engine-прогон на базе (`baseline-43a6fecf/engine-full`, 632 из 3040 падений, в основном cluster corpus/content sync) не используется как доказательство: worktree, по-видимому, без неотслеживаемого контента.
- Известные pre-existing падения вне фильтра (не запускались): `BattleNavigationTests.ExitOfAStaleBattle...`, 2 `TutorialFlowTests`.

**Native/manual run:** NOT RUN (интерактивный запуск клиента с консолью `kill_drone` не выполнялся).

## Review log

2026-11-16T06:50:00Z — G-04 решён, история декомпозирована на пять тикетов, stage → approved (autonomous-execution).

2026-11-16T07:46:00Z — story test run: Contracts 33/33, Engine 242/243, Client 64/69; падения pre-existing (доказательство на базе 43a6fecf); исправлены тесты c4dae7da (TK-0002), 3269d98a (TK-0005).

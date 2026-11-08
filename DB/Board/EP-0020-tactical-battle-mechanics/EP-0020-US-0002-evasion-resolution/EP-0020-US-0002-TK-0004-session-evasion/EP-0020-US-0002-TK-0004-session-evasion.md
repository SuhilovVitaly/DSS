---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0002-evasion-resolution
ticket: EP-0020-US-0002-TK-0004-session-evasion
title: Сессия: приказ уклонения и открытие фазы стрельбы
stage: done
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0002-TK-0003-salvo-evasion]
files_touched: 3
serves: [US2-AC1, US2-AC5, US2-AC6]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Сессия: приказ уклонения и открытие фазы стрельбы

## Why

Игрок отдаёт уклонение через команду Fire. Без открытия фазы при угрозе он не смог бы уклониться именно при отходе (D-10).

## Decisions

Применяются [D-01–D-13](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-11: фазу открывает только угроза дронам игрока; угроза одному носителю её не открывает. A-03: дрон, пропущенный в запросе, не стреляет и не уклоняется (как раньше).

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs` | :92-97 пропуск фазы, :102-122 `Fire`, :124-176 `ResolveFire`, :200 `Own` | Новое условие пропуска; валидация `Evade`; план игрока `(TargetId, Evade)`, противника — `DefaultPlan`; журнал `FireSkipped` с `SkipReason.Evading`. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SessionTests.cs` | :311, :345 | Новые сценарии; обновить ожидания seed 1 с записью причины. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/JournalEventsTests.cs` | — | Событие пропуска с причиной `Evading`. |

## Public API after the change

`FireTacticalBattleRequest` принимает `Evade`; новая причина отказа `EvasionUnavailable`.

## Implementation steps

1. Валидация по US2-AC1 до мутаций.
2. Колбэк планов в `ResolveFire`.
3. Условие открытия фазы по US2-AC5.
4. Тесты сессии: отказы, уклонение игрока, фаза при угрозе без целей (запрос только с `Evade` или пустой), журнал.
5. Перезаписать ожидания `CompleteSeedOneThroughEngineApiMatchesNewDirectRules`, если seed 1 изменился (ход 13 открывает фазу; ИИ уклоняется при низком корпусе); причину записать в карточку.

## Out of scope

Клиент.

## Invariants

Запрос без `Evade` обрабатывается как раньше.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `SessionTests.Evade_order_is_validated_atomically`
- `SessionTests.Fire_phase_opens_when_enemy_threatens_player_drones_without_player_targets`
- `SessionTests.Carrier_only_threat_still_skips_fire_phase`
- `SessionTests.Enemy_evasion_is_hidden_until_salvo_resolves`
- `JournalEventsTests.Evading_step_is_logged_with_reason`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~SessionTests|FullyQualifiedName~JournalEventsTests|FullyQualifiedName~LocalSessionBattleTests|FullyQualifiedName~ConnectionApiTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us2-tk4" --logger "trx;LogFileName=engine.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC1, US2-AC5, US2-AC6 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Строки и условия указаны.

## Исполнение

2026-11-08: Fire атомарно отвергает Evade+Target и недостаток сознания; допускает jam/нет целей; пропущенный приказ остаётся hold. Move открывает Fire при угрозе дронам, carrier-only не открывает. Противник использует DefaultPlan, journal хранит Evading. Session/Journal/LocalSession/Connection 137/137 PASS, build/scoped format/diff-check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us2-tk4/session-parity/tests.trx. Self-review по identity/revision/отказам/скрытию/RNG выполнен. Начальные 28 и затем 10 FAIL были устаревшим равенством разных приказов (игрок не уклоняется автоматически), а также двумя фикстурами с оставшимся AI-поворотом; исправлены явные приказы/контрольные пути, полные outcome/units assertions сохранены. Direct/connection oracle учитывает hold игрока в пропущенных фазах; это сохраняет A-05. US2-AC1/5/6 покрыты.

## Scope проверки connection

Добавлен LocalSessionBattleTests.cs: полный direct/connection oracle предполагает одинаковые приказы. С введением уклонения он обязан явно выбирать уклонение для игрока по правилу ИИ; игра сама этого за игрока не делает (A-05). Сохраняются полные сравнения outcome/units/RNG. Исторические seed1 ожидания обновляются по той же причине, без ослабления asserts.

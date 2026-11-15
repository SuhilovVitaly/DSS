---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0007-decoy-ui
ticket: EP-0020-US-0007-TK-0002-fire-panel-decoy
title: Строка ловушки, значок карточки, метка на карте, строки
stage: done
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0007-TK-0001-fire-orders-decoy]
files_touched: 7
serves: [US7-AC2]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Строка ловушки, значок карточки, метка на карте, строки

## Why

Вид пуска ловушки задан UiSpec и переносится буквально.

## Decisions

Применяются [D-15–D-23](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Токены `GreenDark`, `DecoyIdle` и примитивы `DecoyRow`, `DecoyMark` добавляются в `BattleDesign`. Строки RU/EN — UiSpec §14.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs` | после US-0004 TK-0002: три состояния | План выстрела и «нет целей» по UiSpec §9. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | токены и примитивы | `GreenDark`, `DecoyIdle`, `DecoyRow(state)`, `DecoyMark`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | маркеры и кольцо уклонения | Метка UiSpec §11 для своих дронов с включённой ловушкой. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | EP-0019: отрисовка карточек пилотов | Значок UiSpec §10. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | `TacticalBattle.*` | Ключи UiSpec §14. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | `TacticalBattle.*` | Ключи UiSpec §14. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePanelDecoyTests.cs` | нет | Пробы UiSpec §13 и тексты через модель панели. |

## Public API after the change

`BattleDesign.DecoyRow(SKCanvas, SKRect, string, int charges, int maxCharges, DecoyRowState)`; `BattleDesign.DecoyMark(SKCanvas, SKPoint, double courseDeg)`.

## Implementation steps

1. Примитивы и токены.
2. План выстрела: объединённое поле цели и строка ловушки (§9.1).
3. «Нет целей»: новая раскладка и строка ловушки (§9.2).
4. Итог действий (§9.3), значок карточки (§10), метка на карте (§11).
5. Строки RU/EN; `LocalizationCoverageTests`.
6. Пробы UiSpec §13 для сцен фазы стрельбы.

## Out of scope

Полёт залпа и результаты (TK-0003).

## Invariants

План уклонения M1 не меняется.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `FirePanelDecoyTests.Shot_plan_with_decoy_matches_ui_spec`
- `FirePanelDecoyTests.No_target_with_decoy_matches_ui_spec`
- `FirePanelDecoyTests.Decoy_row_states`
- `FirePanelDecoyTests.Card_and_map_decoy_marks`
- `LocalizationCoverageTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~FirePanelDecoyTests|FullyQualifiedName~FirePanelEvasionTests|FullyQualifiedName~LocalizationCoverageTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us7-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US7-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Единственный источник вида — UiSpec §8–§11, §14.

## Исполнение

В процессе. Scope уточнён до реализации: фактический renderer — BattleMapRenderer.cs; Overlay.cs нужен для клика строки. UiSpec §8 и M4 Build.Module требуют max charges из данных, но DTO содержал только текущие: добавляется init DecoyMaxCharges(default0) в Build, Engine projection и compatibility/hash tests; UI не хранит константу2. Полные ключи §14, Refusal.DecoyUnavailable и formatter DecoyLaunched добавляются здесь для LocalizationCoverage; reveal/flight filtering остаётсяTK3.

Технический gap A-22: production seed1/Turn3 после +10кг имеет Incoming=[] у всех своих, поэтому fallback на другого своего невозможен (no-threat-probe/no-threat.json). Растровые пробы остаются seed1/Turn3 и проверяют ветку §8 «Ловушка выпущена[D]»; отдельный тест первого следующего реального Fire с Incoming проверяет строку Chance→ChanceIfDecoy. Геометрия/палитра/пороги неизменны, фиктивные входящие не добавляются.

UiSpec §8/§13 уточняется тем же техническим gap: источник max — DecoyMaxCharges; при Incoming=[] несуществующие угрозы не создаются. Ветка со стрелкой проверена реальным ходом4. Завершено: PanelDecoy/Localization18/18PASS,EngineGolden/state94/94PASS,Contracts27/27PASS; build/все scoped-format/diff-checkPASS. Визуальная самопроверка production render выполнена. US7-AC2 покрыт: точные прямоугольники/цвета/ромбы/карточка/map occupied mark, без ослабления ΔRGB40 и процентных проб; EV layout не менялся. Локализация§14полная. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us7-tk2/real-scenes/tests.trx,engine/tests.trx,contracts/tests.trx,format-*.log; gap evidence no-threat-probe/no-threat.json; реальные кадры в real-scenes/. Native/human приёмка не заявлена.

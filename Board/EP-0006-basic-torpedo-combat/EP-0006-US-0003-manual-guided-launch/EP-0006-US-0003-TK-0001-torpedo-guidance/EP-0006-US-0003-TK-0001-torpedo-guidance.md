---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0003-manual-guided-launch
ticket: EP-0006-US-0003-TK-0001-torpedo-guidance
title: "Общая математика наведения на центр с упреждением"
stage: draft
layer: motion
test_project: tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj
depends_on: ["EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts"]
files_touched: 3
serves: ["AC-0001","AC-0002"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0003-TK-0001-torpedo-guidance

Общая математика наведения на центр с упреждением.

## Why

Результат тикета: **Общая математика наведения на центр с упреждением**. Он нужен для истории [Ручной пуск торпеды с упреждением](../EP-0006-US-0003-manual-guided-launch.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Пуск из центра по текущему курсу корабля; скорость ровно 3 км/с независимо от носителя, поворот ограничен 90°/с, движение с упреждением.
- **AC-0002** — Выбранная цель закреплена при пуске; исключён собственный корабль. Отсутствие решения не запрещает пуск; нет дальности/таймера/самоуничтожения.

## Decisions

- **D04:** «самонаведение с предсказанием»; «фиксированным. пусть будет 3 километра в секунду пока»; «давай по курсу корабля»; 90°/с — «хорошо»; точка пуска — «с центра». Торпеда стартует из центра по текущему курсу носителя, мгновенно имеет фиксированную собственную скорость 3 км/с и поворачивает максимум 90°/с; скорость носителя не прибавляется.
- **D05:** «Кнопка пуск должна быть активной только при выбранной цели»; допустимая цель — «любой объект»; исключить свой корабль — «да»; сохранить цель после смены выбора — «да». Цель — существующий выбранный объект, кроме собственного корабля. TargetObjectId фиксируется при принятии launch command, hover/смена selection не перенаводит активную торпеду.
- **D06:** Полёт следует паузе/ускорению — «да»; пуск на паузе — «да». Физика следует MotionTimeMs/SimulationTimeMs. На паузе команда создаёт торпеду и busy, но не продвигает полёт; никаких UI-tick gameplay updates.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Референс для независимого oracle: прямолинейный head-on перехват, статичная цель, цель сбоку/сзади, почти равные скорости, совпавшие позиции; R=v/omega=3/(pi/2) км. Тестировать реальный helper, не копию алгоритма.

## Code context

Полный write allowlist от корня DSS; 3 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/TorpedoGuidanceMath.cs` | Новый файл этого тикета; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs` | Существует: `src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs:1` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Motion.Tests/TorpedoGuidanceTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0003-manual-guided-launch.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts](../../EP-0006-US-0002-tetrarch-launcher-room/EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts/EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

Предлагается TorpedoGuidanceMath.Plan(projectilePose,targetPose,speedKmS,turnRateDegPerSec)->TorpedoRoute и PredictPose(route,elapsedMs). Route из contracts. Использовать геометрические helpers ApproachPursuitMath, но отдельный torpedo endpoint без trail offset и обязательного конечного курса цели.

## Implementation steps

1. Выбрать детерминированный поворот/прямой путь с учётом времени встречи с линейной целью; контракт не требует глобально кратчайшего решения среди всех возможных управляющих законов.
2. Совпадающее движение цели переиспользует план. Реальное изменение движения — перепланирование из достигнутой позы; для иных доступных типов использовать текущую кинематику общего predictor без расширения задачи до новой орбитальной физики.
3. При отсутствии решения продолжать ограниченное по turn rate преследование текущей прогнозной позиции; ETA=null, ложный маркер встречи не рисовать. Нет artificial TTL/range.
4. Подключить projectile branch в LinearMotionPredictor до generic navigation; полёт и дуги одинаковы в Engine и Client. Не изменять ApproachLineCaptureMath/SolveInterceptFlyThroughPlan.

## Out of scope

Файлы вне allowlist; production слои вне `motion`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `Torpedo_intercepts_linear_target_from_carrier_heading`
- [ ] `Torpedo_turn_rate_and_speed_are_bounded`
- [ ] `Torpedo_endpoint_has_no_trail_offset_or_heading_match`
- [ ] `No_intercept_returns_null_eta_without_stopping_motion`
- [ ] `Guidance_is_time_partition_invariant`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore --filter 'FullyQualifiedName~Torpedo_intercepts_linear_target_from_carrier_heading|FullyQualifiedName~Torpedo_turn_rate_and_speed_are_bounded|FullyQualifiedName~Torpedo_endpoint_has_no_trail_offset_or_heading_match|FullyQualifiedName~No_intercept_returns_null_eta_without_stopping_motion|FullyQualifiedName~Guidance_is_time_partition_invariant'
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Motion/TorpedoGuidanceMath.cs src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs tests/DeepSpaceSaga.Motion.Tests/TorpedoGuidanceTests.cs
git diff --check
```

Изменение общего predictor дополнительно требует Motion/Engine/Client regression suites; не менять их source вне allowlist.

## Definition of Done

- [ ] Все шаги выполнены только в Code context; число файлов/слой совпадают с frontmatter.
- [ ] Наблюдаемый end state и вклад в каждый `serves` подтверждены named tests.
- [ ] Точные команды/результаты test/build/scoped format/diff check записаны при реализации; baseline debt отделён.
- [ ] Согласованные API, units/time, deterministic ordering, legacy defaults и out-of-scope соблюдены.
- [ ] Для визуального результата пройден native smoke, либо тикет явно остаётся без окончательной приёмки.
- [ ] Нет скрытых зависимостей/дополнительных файлов, незаписанных assumptions или фиктивного evidence.

## Self-containment check

Родительские epic/story связаны; решения, scope, proposed API, шаги, tests, layer и dependencies указаны. Grounding различает существующий source и будущие helpers. Чтение контекста допустимо, но новые продуктовые решения и расширение allowlist не требуются без изменения baseline.

**Текущий статус:** draft planning; implementation, automated tests и native validation не выполнялись.


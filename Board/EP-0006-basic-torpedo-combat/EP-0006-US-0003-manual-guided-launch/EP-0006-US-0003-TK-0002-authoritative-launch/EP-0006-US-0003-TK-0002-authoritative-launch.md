---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0003-manual-guided-launch
ticket: EP-0006-US-0003-TK-0002-authoritative-launch
title: "Авторитетный пуск и занятость оператора"
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: ["EP-0006-US-0003-TK-0001-torpedo-guidance"]
files_touched: 4
serves: ["AC-0001","AC-0002","AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0003-TK-0002-authoritative-launch

Авторитетный пуск и занятость оператора.

## Why

Результат тикета: **Авторитетный пуск и занятость оператора**. Он нужен для истории [Ручной пуск торпеды с упреждением](../EP-0006-US-0003-manual-guided-launch.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Пуск из центра по текущему курсу корабля; скорость ровно 3 км/с независимо от носителя, поворот ограничен 90°/с, движение с упреждением.
- **AC-0002** — Выбранная цель закреплена при пуске; исключён собственный корабль. Отсутствие решения не запрещает пуск; нет дальности/таймера/самоуничтожения.
- **AC-0003** — Один аппарат сопровождает не более одной торпеды. UI и Engine блокируют повторный пуск; на паузе команда принимается без продвижения MotionTimeMs.

## Decisions

- **D02:** «пока она летит вторую он выпустить не может»; «после взрыва выпущенной торпеды следующая уже готова»; условной занятости — «пока условной достаточно»; боезапас — «бесконечные». Одна активная торпеда на аппарат, без реального crew assignment, расхода боезапаса и отдельного ожидания КД. Готов → Наведение → Готов; pending submit не позволяет double click.
- **D03:** «у нас уже есть сценарий с кораблем пиратом и кораблем игрока»; на вопрос о движении игрока — «может»; пиратский аппарат неактивен — «пока да». Использовать PlayerShipOnly/Player Ship and Pirate. Пират летит прямо и не стреляет/не защищается; игрок управляет навигацией обычным способом.
- **D04:** «самонаведение с предсказанием»; «фиксированным. пусть будет 3 километра в секунду пока»; «давай по курсу корабля»; 90°/с — «хорошо»; точка пуска — «с центра». Торпеда стартует из центра по текущему курсу носителя, мгновенно имеет фиксированную собственную скорость 3 км/с и поворачивает максимум 90°/с; скорость носителя не прибавляется.
- **D05:** «Кнопка пуск должна быть активной только при выбранной цели»; допустимая цель — «любой объект»; исключить свой корабль — «да»; сохранить цель после смены выбора — «да». Цель — существующий выбранный объект, кроме собственного корабля. TargetObjectId фиксируется при принятии launch command, hover/смена selection не перенаводит активную торпеду.
- **D06:** Полёт следует паузе/ускорению — «да»; пуск на паузе — «да». Физика следует MotionTimeMs/SimulationTimeMs. На паузе команда создаёт торпеду и busy, но не продвигает полёт; никаких UI-tick gameplay updates.
- **D07:** Ограничить дальность — «нет»; недостижимая цель — «пока этим принебрегаем потом добавим кнопку самоуничтожения». Нет range, TTL, fuel limit, проверки достижимости как условия пуска или кнопки self-destruct. Бесконечное преследование и занятый аппарат допустимы для MVP.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 4 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:528` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Создаётся зависимостью `EP-0006-US-0002-TK-0001-combat-bootstrap`; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs` | Существует: `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:12` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Engine.Tests/TorpedoLaunchTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0003-manual-guided-launch.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0003-TK-0001-torpedo-guidance](../../EP-0006-US-0003-manual-guided-launch/EP-0006-US-0003-TK-0001-torpedo-guidance/EP-0006-US-0003-TK-0001-torpedo-guidance.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

TryStartTorpedoFire(PlayerCommand,long motionTimeMs); AdvanceCombatTo(long motionTimeMs). Command target передаётся явно; projectile ID/launch sequence детерминированны и не расходуют RNG.

## Implementation steps

1. Маршрутизировать torpedo.fire через существующий command journal после общих guards; проверить owner/player, модуль, power/structure/operational readiness, target existence/not self и отсутствие active torpedo.
2. Атомарно создать Missile в центре/по курсу носителя и назначить active projectile ID аппарату; повтор того же CommandId не создаёт вторую торпеду. Отказ не изменяет ресурсы, RNG или busy state.
3. В AdvanceMotionTo включить combat в fast-path guard и последовательную обработку motion boundaries; не добавлять независимый календарный clock. Пауза не двигает мир, но применяет commands и публикует snapshot.
4. NPC launcher пассивен; обычные команды движения игрока не отменяют наведение. No-intercept/target-lost policy из Documentation реализуется без скрытого cooldown.

## Out of scope

Файлы вне allowlist; production слои вне `engine`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `Paused_launch_creates_one_stationary_projectile`
- [ ] `Second_launch_and_duplicate_command_do_not_create_projectile`
- [ ] `Missing_self_or_invalid_target_is_rejected_without_effect`
- [ ] `Carrier_maneuver_does_not_retarget_torpedo`
- [ ] `Motion_clock_controls_torpedo_under_all_speeds`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~Paused_launch_creates_one_stationary_projectile|FullyQualifiedName~Second_launch_and_duplicate_command_do_not_create_projectile|FullyQualifiedName~Missing_self_or_invalid_target_is_rejected_without_effect|FullyQualifiedName~Carrier_maneuver_does_not_retarget_torpedo|FullyQualifiedName~Motion_clock_controls_torpedo_under_all_speeds'
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs tests/DeepSpaceSaga.Engine.Tests/TorpedoLaunchTests.cs
git diff --check
```

При затрагивании shared DTO/loader выполнить соответствующие consumer regression suites. Не расширять production scope ради зелёного полного прогона.

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


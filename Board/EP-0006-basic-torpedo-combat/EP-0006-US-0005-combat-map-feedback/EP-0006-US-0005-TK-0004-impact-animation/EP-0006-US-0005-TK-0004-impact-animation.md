---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0005-combat-map-feedback
ticket: EP-0006-US-0005-TK-0004-impact-animation
title: "Двухсекундное кольцо взрыва на реальном времени"
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: ["EP-0006-US-0005-TK-0003-combat-markers-and-hp"]
files_touched: 4
serves: ["AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0005-TK-0004-impact-animation

Двухсекундное кольцо взрыва на реальном времени.

## Why

Результат тикета: **Двухсекундное кольцо взрыва на реальном времени**. Он нужен для истории [Игрок видит торпеду, прочность, взрыв и врек](../EP-0006-US-0005-combat-map-feedback.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0003** — Взрыв: красное кольцо 0→50 px радиуса за 2000 мс реального времени с плавным угасанием, независимо от паузы/ускорения.

## Decisions

- **D11:** След «исчезает через 2 секунды», время — «реального»; взрыв — «расходящийся из точки взрыва круг красного цвета»; «2 секунды от 0 пикселей до 50 радиуса плавно». После контакта прогноз/крест убираются, след живёт2000мс реального времени. Красное кольцо плавно расширяется0→50px радиуса и тускнеет до нуля за2000мс, независимо от pause/speed.
- **D12:** Сохранять торпеду, busy и урон — «да»; сохранять весь активный след — «да»; «при сохранении взрыв сохранять не нужно». Сохраняются authoritative flight/target/owner/module/route/history/distance/captured parameters, HP, wreck и ID counters. Взрыв/real-time animation state не сохраняются.
- **D15:** «цветовые решения нужно вынести в отдельный файл который можно будет редактировать не компилируя код»; способ применения — «перезапуск». Отдельный combat palette JSON для torpedo/trail/prediction/intercept/preview/HP/explosion/wreck; читать при startup, без hot reload или build.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 4 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs` | Новый файл этого тикета; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs` | Создаётся зависимостью `EP-0006-US-0003-TK-0003-launcher-command-panel`; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Существует: `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:706` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Client.Tests/CombatEffectTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0005-combat-map-feedback.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0005-TK-0003-combat-markers-and-hp](../../EP-0006-US-0005-combat-map-feedback/EP-0006-US-0005-TK-0003-combat-markers-and-hp/EP-0006-US-0005-TK-0003-combat-markers-and-hp.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

CombatEffectStore consumes impact EventId once using injectable monotonic clock. u=clamp((now-start)/2000ms,0,1), radiusPx=50*u, alpha=paletteAlpha*(1-u); remove at u>=1.

## Implementation steps

1. Запускать кольцо один раз при первом новом authoritative impact; переводить world point в текущую камеру каждый кадр. Отсчёт от первого получения факта выбран как техническое решение при 1Hz snapshots.
2. Истёкшие эффекты не перезапускать повторными snapshots. При повторном OnActivated той же сессии monotonic deadlines сохраняются; скрытая карта не останавливает real-time lifetime.
3. Использовать тот же real-time deadline для финального следа (US-0006 подключит геометрию). На Load/New Game baseline reset: исторические события не проигрывать.
4. Не сериализовать анимацию/таймер. Тестировать fake clock на 0/1000/2000мс, Speed0/Speed4 и повторных кадрах.

## Out of scope

Файлы вне allowlist; production слои вне `client`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `Explosion_radius_and_alpha_progress_for_two_real_seconds`
- [ ] `Pause_and_acceleration_do_not_change_effect_lifetime`
- [ ] `Repeated_impact_snapshot_does_not_restart_explosion`
- [ ] `Session_reset_does_not_replay_old_effects`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~Explosion_radius_and_alpha_progress_for_two_real_seconds|FullyQualifiedName~Pause_and_acceleration_do_not_change_effect_lifetime|FullyQualifiedName~Repeated_impact_snapshot_does_not_restart_explosion|FullyQualifiedName~Session_reset_does_not_replay_old_effects'
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatEffectStore.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Combat.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs tests/DeepSpaceSaga.Client.Tests/CombatEffectTests.cs
git diff --check
```

Визуальные пункты требуют native/manual smoke и записанного evidence; headless tests не заменяют этот gate. Сроки эффектов проверять fake monotonic clock, без sleep/flaky wall-time assertions.

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


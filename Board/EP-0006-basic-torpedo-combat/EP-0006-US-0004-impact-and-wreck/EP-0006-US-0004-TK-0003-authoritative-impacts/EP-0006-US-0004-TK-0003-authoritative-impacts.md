---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0004-impact-and-wreck
ticket: EP-0006-US-0004-TK-0003-authoritative-impacts
title: "Однократное попадание, урон и освобождение аппарата"
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: ["EP-0006-US-0004-TK-0002-impact-event-contract"]
files_touched: 4
serves: ["AC-0001","AC-0002","AC-0004"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0004-TK-0003-authoritative-impacts

Однократное попадание, урон и освобождение аппарата.

## Why

Результат тикета: **Однократное попадание, урон и освобождение аппарата**. Он нужен для истории [Три попадания превращают Тетрарх во врек](../EP-0006-US-0004-impact-and-wreck.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Каждое столкновение с любым чужим объектом в радиусе 5 world units завершает торпеду ровно один раз; носитель и сама торпеда исключены.
- **AC-0002** — Только явный класс ship.tetrarch теряет 150 HP: 450→300→150→0. Нет броска попадания или damage RNG, поражения модулей и area damage.
- **AC-0004** — Любое ускорение и разбиение времени сохраняют первый фактический контакт, HP и единственный врек.

## Decisions

- **D02:** «пока она летит вторую он выпустить не может»; «после взрыва выпущенной торпеды следующая уже готова»; условной занятости — «пока условной достаточно»; боезапас — «бесконечные». Одна активная торпеда на аппарат, без реального crew assignment, расхода боезапаса и отдельного ожидания КД. Готов → Наведение → Готов; pending submit не позволяет double click.
- **D08:** «гарантированно попадает»; «давай центр»; «нужна погрешность скажем 5 пикселей»; фиксированный масштаб — «хорошо»; «может столкнуться. но не с кораблем игрока»; одинаковый допуск/урон для препятствий — «да». Цель наведения — центр. Контакт при расстоянии <=5 world units (500 м, 5 px при 100 м/px) с любым чужим объектом; self projectile и owner исключены. Первый контакт взрывает торпеду, без случайного броска и без сквозного полёта.
- **D09:** «корабль пиратов был уничтожен с трех попаданий»; «пока только на корабль типа Тетрарх. Все остальные объекты неуязвимые»; «превращается в новый объект врек со скоростью и направлением 0». Только явный ship.tetrarch имеет HP=450 и теряет по150: 450→300→150→0. При нуле живой корабль удаляется, новый stationary Wreck имеет новый ID, координаты погибшего корабля на момент контакта, speed=0, heading=0°.
- **D11:** След «исчезает через 2 секунды», время — «реального»; взрыв — «расходящийся из точки взрыва круг красного цвета»; «2 секунды от 0 пикселей до 50 радиуса плавно». После контакта прогноз/крест убираются, след живёт2000мс реального времени. Красное кольцо плавно расширяется0→50px радиуса и тускнеет до нуля за2000мс, независимо от pause/speed.

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
| `tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0004-impact-and-wreck.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0004-TK-0002-impact-event-contract](../../EP-0006-US-0004-impact-and-wreck/EP-0006-US-0004-TK-0002-impact-event-contract/EP-0006-US-0004-TK-0002-impact-event-contract.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

AdvanceCombatTo применяет FirstContact; внутренний ApplyImpact(contact,targetId) атомарно removes projectile, releases launcher, updates HP and publishes impact fact. Порядок: event time, projectile ID ordinal, hit object ID ordinal.

## Implementation steps

1. На каждом физическом интервале искать первый контакт со всеми подходящими существующими объектами, включая неуязвимые; исключить owner ID и self projectile ID на всём пути.
2. На контакте фиксировать фактические координаты торпеды и цели, отрезать route/trail; apply damage только explicit tetrarch class и без RNG. Полёт не продолжается сквозь неуязвимый объект.
3. Сразу освободить аппарат при любом взрыве; новый пуск возможен при том же MotionTimeMs после завершения предыдущего, отдельного cooldown нет.
4. Соблюсти порядок со steering/survey/security boundaries. Изменение коллекции объектов не должно сдвигать index-based market caches внутри AdvanceWorldTo; безопасно применять removals после закрытия boundary или по stable IDs.
5. Факты попаданий удерживать до гарантированной доставки: session-local журнал с монотонными ID в snapshots, не одноразовый drain. В MVP не обрезать неподтверждённые факты по произвольному числу кадров; scalability/ack — отдельный backlog.

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

- [ ] `Three_contacts_apply_150_without_rng_draws`
- [ ] `Invulnerable_obstacle_explodes_torpedo_and_releases_launcher`
- [ ] `Owner_is_ignored_for_entire_flight`
- [ ] `Equal_time_contacts_use_stable_order_and_apply_once`
- [ ] `Snapshot_coalescing_does_not_lose_impact_fact`
- [ ] `Calendar_boundaries_do_not_change_combat_result`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~Three_contacts_apply_150_without_rng_draws|FullyQualifiedName~Invulnerable_obstacle_explodes_torpedo_and_releases_launcher|FullyQualifiedName~Owner_is_ignored_for_entire_flight|FullyQualifiedName~Equal_time_contacts_use_stable_order_and_apply_once|FullyQualifiedName~Snapshot_coalescing_does_not_lose_impact_fact|FullyQualifiedName~Calendar_boundaries_do_not_change_combat_result'
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs
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


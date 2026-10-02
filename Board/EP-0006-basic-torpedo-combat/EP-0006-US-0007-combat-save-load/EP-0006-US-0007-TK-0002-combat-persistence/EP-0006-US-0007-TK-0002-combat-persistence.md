---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0007-combat-save-load
ticket: EP-0006-US-0007-TK-0002-combat-persistence
title: "Атомарное сохранение и восстановление реального боя"
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: ["EP-0006-US-0007-TK-0001-combat-save-schema"]
files_touched: 4
serves: ["AC-0001","AC-0002","AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0007-TK-0002-combat-persistence

Атомарное сохранение и восстановление реального боя.

## Why

Результат тикета: **Атомарное сохранение и восстановление реального боя**. Он нужен для истории [Продолжение боя после сохранения и загрузки](../EP-0006-US-0007-combat-save-load.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Реальная файловая загрузка восстанавливает активную торпеду, captured параметры, owner/module/target, route/history/distance, motion time, HP и IDs.
- **AC-0002** — Загрузка после 1/2 попаданий не лечит корабль; после 3-го сохраняет единственный wreck. Повтор команд и advancement не дублируют попадание/врек.
- **AC-0003** — Busy ↔ active torpedo — согласованная связь; malformed saves не создают частично загруженную сессию.

## Decisions

- **D02:** «пока она летит вторую он выпустить не может»; «после взрыва выпущенной торпеды следующая уже готова»; условной занятости — «пока условной достаточно»; боезапас — «бесконечные». Одна активная торпеда на аппарат, без реального crew assignment, расхода боезапаса и отдельного ожидания КД. Готов → Наведение → Готов; pending submit не позволяет double click.
- **D08:** «гарантированно попадает»; «давай центр»; «нужна погрешность скажем 5 пикселей»; фиксированный масштаб — «хорошо»; «может столкнуться. но не с кораблем игрока»; одинаковый допуск/урон для препятствий — «да». Цель наведения — центр. Контакт при расстоянии <=5 world units (500 м, 5 px при 100 м/px) с любым чужим объектом; self projectile и owner исключены. Первый контакт взрывает торпеду, без случайного броска и без сквозного полёта.
- **D09:** «корабль пиратов был уничтожен с трех попаданий»; «пока только на корабль типа Тетрарх. Все остальные объекты неуязвимые»; «превращается в новый объект врек со скоростью и направлением 0». Только явный ship.tetrarch имеет HP=450 и теряет по150: 450→300→150→0. При нуле живой корабль удаляется, новый stationary Wreck имеет новый ID, координаты погибшего корабля на момент контакта, speed=0, heading=0°.
- **D12:** Сохранять торпеду, busy и урон — «да»; сохранять весь активный след — «да»; «при сохранении взрыв сохранять не нужно». Сохраняются authoritative flight/target/owner/module/route/history/distance/captured parameters, HP, wreck и ID counters. Взрыв/real-time animation state не сохраняются.
- **D13:** Показывать до выстрела при hover Пуск — «да но отличающимся цветом. например серым». При hover доступного Пуск показывается gray preview с точкой встречи и нужной траекторией цели. Чистая client prediction, без launch side effects.
- **D17:** «для тетрарха в настройки тетрарха, для торпеды в настройки торпедного аппарата который является модулем тетрарха»; скорость и turn rate в модуле — «да». HP450 принадлежит class config Тетрарха; damage150/speed3/turnRate90 — конфигурации module.torpedo.launcher.basic, а не экземпляру палитры/константам Client.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Тест обязан писать файл, вызывать EngineContentLoader.CreateEngineFromSaveFile и продолжать настоящие commands/time advance; roundtrip одного DTO, ручное создание cargo/торпеды или подмена результата не считаются доказательством.

## Code context

Полный write allowlist от корня DSS; 4 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:528` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Создаётся зависимостью `EP-0006-US-0002-TK-0001-combat-bootstrap`; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs` | Новый файл этого тикета; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Engine.Tests/CombatSaveLoadTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0007-combat-save-load.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0007-TK-0001-combat-save-schema](../../EP-0006-US-0007-combat-save-load/EP-0006-US-0007-TK-0001-combat-save-schema/EP-0006-US-0007-TK-0001-combat-save-schema.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

CaptureCombatState / RestoreCombatState connected to CaptureSaveStateCore and LoadScenario. Runtime uses only restored captured weapon/HP values for continuing flight. Combat animation journal establishes new presentation baseline after load and is not replayed.

## Implementation steps

1. Перед capture продвинуть world до текущего MotionTimeMs и применить pending commands тем же порядком, что BuildSnapshot; сериализовать согласованный после команд state под world lock.
2. Восстановить projectile identity/pose/route phase/history, HP, wreck, counters и journal без нового launch/trajectory generation/random draws.
3. Проверить совпадение непрерывного и сохранённого/загруженного прогона реальных трёх пусков на всех speed/time partitions, включая паузу после command enqueue и save у границы контакта.
4. Изменение content после save применяется к новым играм; параметры уже сохранённого combat state не меняются. Несовместимость отсутствующего обязательного класса/модуля — явная ошибка, не silent fallback.

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

- [ ] `File_roundtrip_midflight_matches_continuous_three_hit_run`
- [ ] `Save_after_enqueue_preserves_exactly_one_launch`
- [ ] `Save_at_contact_does_not_duplicate_damage_or_wreck`
- [ ] `Saved_hp_and_weapon_values_survive_content_edit`
- [ ] `Busy_and_full_trail_restore_without_new_rng_draw`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~File_roundtrip_midflight_matches_continuous_three_hit_run|FullyQualifiedName~Save_after_enqueue_preserves_exactly_one_launch|FullyQualifiedName~Save_at_contact_does_not_duplicate_damage_or_wreck|FullyQualifiedName~Saved_hp_and_weapon_values_survive_content_edit|FullyQualifiedName~Busy_and_full_trail_restore_without_new_rng_draw'
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatPersistence.cs tests/DeepSpaceSaga.Engine.Tests/CombatSaveLoadTests.cs
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


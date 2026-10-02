---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0001-combat-configuration
ticket: EP-0006-US-0001-TK-0001-combat-contract
title: "Контракт корабля, торпеды и занятости аппарата"
stage: draft
layer: contracts
test_project: tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj
depends_on: []
files_touched: 5
serves: ["AC-0001","AC-0002","AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0001-TK-0001-combat-contract

Контракт корабля, торпеды и занятости аппарата.

## Why

Результат тикета: **Контракт корабля, торпеды и занятости аппарата**. Он нужен для истории [Настраиваемые характеристики Тетрарха и торпеды](../EP-0006-US-0001-combat-configuration.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Тетрарх имеет явный идентификатор класса и настройку 450 HP; другие классы не становятся уязвимыми из-за совпавшей картинки.
- **AC-0002** — Модуль задаёт урон 150, скорость 3 км/с и скорость поворота 90°/с; загрузчик проверяет значения и выдаёт ошибку с путём.
- **AC-0003** — Старый контент без новых необязательных секций загружается; новые данные попадают в output и доступны после перезапуска.

## Decisions

- **D01:** «нам нужно добавить новый модуль торпедного аппарата в схему Тетрарха»; «для ланчера нужна своя панель»; новое помещение — «новое», место — «выбери сам». Один launcher в новом помещении стандартного Тетрарха; отдельная command panel с кнопкой Пуск.
- **D04:** «самонаведение с предсказанием»; «фиксированным. пусть будет 3 километра в секунду пока»; «давай по курсу корабля»; 90°/с — «хорошо»; точка пуска — «с центра». Торпеда стартует из центра по текущему курсу носителя, мгновенно имеет фиксированную собственную скорость 3 км/с и поворачивает максимум 90°/с; скорость носителя не прибавляется.
- **D10:** «предсказательная траектория с местом предполагаемого пересечения с траекторией цели и отрисованная собственная пройденная траектория»; «желтая с размытием диаметр 5 пикселей»; оформление yellow solid/dashed/cross — «пока да»; показывать всегда — «все время». Жёлтая точка диаметром5px с blur; executed path solid yellow, remaining prediction dashed yellow, encounter cross yellow. Прогноз исходной цели до встречи тоже виден независимо от selection. Viewport clipping допустим.
- **D13:** Показывать до выстрела при hover Пуск — «да но отличающимся цветом. например серым». При hover доступного Пуск показывается gray preview с точкой встречи и нужной траекторией цели. Чистая client prediction, без launch side effects.
- **D16:** Выделение торпеды — «должна быть»; к цели/скорости/пройденному пути/ETA — «добавь еще шанс попадания пока 100%». Торпеда selectable; info panel показывает цель, скорость, пройденный путь, ETA (— если неизвестно), шанс100%. Это вероятность при достижении, не обещание игнорировать препятствия.
- **D17:** «для тетрарха в настройки тетрарха, для торпеды в настройки торпедного аппарата который является модулем тетрарха»; скорость и turn rate в модуле — «да». HP450 принадлежит class config Тетрарха; damage150/speed3/turnRate90 — конфигурации module.torpedo.launcher.basic, а не экземпляру палитры/константам Client.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 5 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs` | Новый файл этого тикета; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Contracts/SpaceObjectType.cs` | Существует: `src/DeepSpaceSaga.Contracts/SpaceObjectType.cs:5` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs` | Существует: `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs:15` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs` | Существует: `src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs:13` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0001-combat-configuration.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

none

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

Предлагаемые DTO в CombatSnapshot.cs: HullCombatSnapshot(ShipClassId, CurrentHp, MaxHp); TorpedoSnapshot(OwnerObjectId, LauncherModuleId, TargetObjectId, LaunchMotionTimeMs, SpeedKmS, TurnRateDegPerSec, Damage, DistanceTravelledWorldUnits, Route, Trail, PredictedImpactMotionTimeMs?, HitChancePercent=100); LauncherCombatSnapshot(ActiveTorpedoObjectId?, SpeedKmS, TurnRateDegPerSec, Damage). TorpedoRoute/TrailSegment — immutable world-space геометрия со временем/версией. ObjectMotionSnapshot получает optional HullCombat/Torpedo, InstalledModuleSnapshot — optional LauncherCombat. CombatCommandTypes.Fire = torpedo.fire; SpaceObjectType.Wreck; существующий Missile используется для торпеды.

## Implementation steps

1. Добавить optional поля с backward-compatible defaults и JSON-safe коллекциями; текущие конструкторы snapshots должны продолжать компилироваться.
2. В Route отделить геометрические сегменты, текущую фазу и признак отсутствия решения; nullable ETA не подменять нулём.
3. Зафиксировать единицы и MotionTimeMs в XML-комментариях. Контракт не исполняет урон, RNG или UI-анимации.

## Out of scope

Файлы вне allowlist; production слои вне `contracts`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `CombatSnapshot_roundtrips_route_trail_and_launcher`
- [ ] `Legacy_snapshot_without_combat_fields_stays_valid`
- [ ] `No_intercept_has_null_eta_not_zero`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter 'FullyQualifiedName~CombatSnapshot_roundtrips_route_trail_and_launcher|FullyQualifiedName~Legacy_snapshot_without_combat_fields_stays_valid|FullyQualifiedName~No_intercept_has_null_eta_not_zero'
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Contracts/CombatSnapshot.cs src/DeepSpaceSaga.Contracts/SpaceObjectType.cs src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs
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


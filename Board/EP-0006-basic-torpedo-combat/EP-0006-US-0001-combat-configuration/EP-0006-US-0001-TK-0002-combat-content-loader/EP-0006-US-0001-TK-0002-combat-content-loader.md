---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0001-combat-configuration
ticket: EP-0006-US-0001-TK-0002-combat-content-loader
title: "Загрузка параметров класса корабля и торпедного модуля"
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: ["EP-0006-US-0001-TK-0001-combat-contract"]
files_touched: 5
serves: ["AC-0001","AC-0002","AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0001-TK-0002-combat-content-loader

Загрузка параметров класса корабля и торпедного модуля.

## Why

Результат тикета: **Загрузка параметров класса корабля и торпедного модуля**. Он нужен для истории [Настраиваемые характеристики Тетрарха и торпеды](../EP-0006-US-0001-combat-configuration.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Тетрарх имеет явный идентификатор класса и настройку 450 HP; другие классы не становятся уязвимыми из-за совпавшей картинки.
- **AC-0002** — Модуль задаёт урон 150, скорость 3 км/с и скорость поворота 90°/с; загрузчик проверяет значения и выдаёт ошибку с путём.
- **AC-0003** — Старый контент без новых необязательных секций загружается; новые данные попадают в output и доступны после перезапуска.

## Decisions

- **D01:** «нам нужно добавить новый модуль торпедного аппарата в схему Тетрарха»; «для ланчера нужна своя панель»; новое помещение — «новое», место — «выбери сам». Один launcher в новом помещении стандартного Тетрарха; отдельная command panel с кнопкой Пуск.
- **D04:** «самонаведение с предсказанием»; «фиксированным. пусть будет 3 километра в секунду пока»; «давай по курсу корабля»; 90°/с — «хорошо»; точка пуска — «с центра». Торпеда стартует из центра по текущему курсу носителя, мгновенно имеет фиксированную собственную скорость 3 км/с и поворачивает максимум 90°/с; скорость носителя не прибавляется.
- **D17:** «для тетрарха в настройки тетрарха, для торпеды в настройки торпедного аппарата который является модулем тетрарха»; скорость и turn rate в модуле — «да». HP450 принадлежит class config Тетрарха; damage150/speed3/turnRate90 — конфигурации module.torpedo.launcher.basic, а не экземпляру палитры/константам Client.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 5 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Content/ShipClassDefinition.cs` | Новый файл этого тикета; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs` | Существует: `src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs:5` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs` | Существует: `src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:101` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs` | Существует: `src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs:58` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Engine.Tests/CombatContentLoaderTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0001-combat-configuration.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0001-TK-0001-combat-contract](../../EP-0006-US-0001-combat-configuration/EP-0006-US-0001-TK-0001-combat-contract/EP-0006-US-0001-TK-0001-combat-contract.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

Предлагается ShipClassDefinition(TypeId, HullHitPointsMax); optional TypeDataPaths.ShipClasses; GameDataRegistry.ShipClasses; optional ModuleTypeDefinition.TorpedoDamage/TorpedoSpeedKmS/TorpedoTurnRateDegPerSec с одноимёнными JSON полями в camelCase. Добавлять параметры в конец совместимых конструкторов.

## Implementation steps

1. Подключить реестр ship classes через существующий EngineContentLoader во всех путях New Game/explicit scenario/save; отсутствие path допустимо для legacy, объявленный отсутствующий/пустой файл — ContentException.
2. Расширить module implementation DTO и mapping: три torpedo-поля обязательны вместе и только для launcher category; положительный конечный damage/speed/turnRate, HP > 0, дубли ID запрещены.
3. Не использовать sprite/displayName как class identity; не изменять fingerprint торгового каталога из-за добавления неэкономических combat полей.

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

- [ ] `Combat_content_loads_ship_and_launcher_values`
- [ ] `Partial_nonfinite_or_nonpositive_weapon_config_is_rejected`
- [ ] `Legacy_registry_without_ship_classes_is_unchanged`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~Combat_content_loads_ship_and_launcher_values|FullyQualifiedName~Partial_nonfinite_or_nonpositive_weapon_config_is_rejected|FullyQualifiedName~Legacy_registry_without_ship_classes_is_unchanged'
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Content/ShipClassDefinition.cs src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs tests/DeepSpaceSaga.Engine.Tests/CombatContentLoaderTests.cs
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


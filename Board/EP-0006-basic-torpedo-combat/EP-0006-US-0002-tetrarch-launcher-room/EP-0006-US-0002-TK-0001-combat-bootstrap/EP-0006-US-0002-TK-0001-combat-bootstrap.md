---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0002-tetrarch-launcher-room
ticket: EP-0006-US-0002-TK-0001-combat-bootstrap
title: "Инициализация класса, HP и аппарата в Engine"
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: ["EP-0006-US-0001-TK-0004-tetrarch-class-content"]
files_touched: 5
serves: ["AC-0001","AC-0002","AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0002-TK-0001-combat-bootstrap

Инициализация класса, HP и аппарата в Engine.

## Why

Результат тикета: **Инициализация класса, HP и аппарата в Engine**. Он нужен для истории [Тетрарх получает помещение торпедного аппарата](../EP-0006-US-0002-tetrarch-launcher-room.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — В canonical hull grid появляется новая structural cell (3,2) и один launcher; старые модули остаются на местах.
- **AC-0002** — PlayerShipOnly сохраняет исходные позиции, скорость 0,4 км/с и курс 120° пирата; оба корабля явно ship.tetrarch и имеют полные 450 HP.
- **AC-0003** — Стандартные Тетрархи других существующих сценариев используют согласованную комплектацию; специализированный MarketProfiles с трёхклеточным корпусом не переклассифицируется по изображению.

## Decisions

- **D01:** «нам нужно добавить новый модуль торпедного аппарата в схему Тетрарха»; «для ланчера нужна своя панель»; новое помещение — «новое», место — «выбери сам». Один launcher в новом помещении стандартного Тетрарха; отдельная command panel с кнопкой Пуск.
- **D02:** «пока она летит вторую он выпустить не может»; «после взрыва выпущенной торпеды следующая уже готова»; условной занятости — «пока условной достаточно»; боезапас — «бесконечные». Одна активная торпеда на аппарат, без реального crew assignment, расхода боезапаса и отдельного ожидания КД. Готов → Наведение → Готов; pending submit не позволяет double click.
- **D03:** «у нас уже есть сценарий с кораблем пиратом и кораблем игрока»; на вопрос о движении игрока — «может»; пиратский аппарат неактивен — «пока да». Использовать PlayerShipOnly/Player Ship and Pirate. Пират летит прямо и не стреляет/не защищается; игрок управляет навигацией обычным способом.
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
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs` | Существует: `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:102` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs` | Существует: `src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs:12` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Существует: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:528` | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs` | Новый файл этого тикета; на baseline отсутствует | Только заявленные ниже API/интеграционные точки; без соседнего refactoring |
| `tests/DeepSpaceSaga.Engine.Tests/CombatBootstrapTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0002-tetrarch-launcher-room.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0001-TK-0004-tetrarch-class-content](../../EP-0006-US-0001-combat-configuration/EP-0006-US-0001-TK-0004-tetrarch-class-content/EP-0006-US-0001-TK-0004-tetrarch-class-content.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

SpaceObjectData.ShipClassId?/HullHitPoints?; runtime combat state keyed by ObjectId/ModuleId; optional projection HullCombat/LauncherCombat. Ship class задаёт max HP; сохранённое current HP не перезаписывается максимумом.

## Implementation steps

1. Ввести явную идентичность класса в scenario/runtime, положить initialization/projection helpers в новый partial Combat, подключить LoadScenario/BuildSnapshot/BuildInstalledModuleProjection.
2. При New Game класса ship.tetrarch без instance HP установить content maximum; непомеченный класс оставлять без боевой уязвимости. Не выводить класс из PlayerShip/NpcShip, имени, hull shape или sprite.
3. Проверить разрешённые клетки и отсутствие перекрытий через текущую module placement validation; новый модуль не назначает реального члена экипажа.
4. Проецировать характеристики launcher, HP и ready state; при смене сессии очищать combat state. Сохранение новых полей и миграции завершаются US-0007.

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

- [ ] `Tetrarch_starts_at_configured_hp_and_launcher_ready`
- [ ] `Unclassified_ship_remains_invulnerable`
- [ ] `Invalid_class_hp_and_overlapping_launcher_are_rejected`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter 'FullyQualifiedName~Tetrarch_starts_at_configured_hp_and_launcher_ready|FullyQualifiedName~Unclassified_ship_remains_invulnerable|FullyQualifiedName~Invalid_class_hp_and_overlapping_launcher_are_rejected'
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs tests/DeepSpaceSaga.Engine.Tests/CombatBootstrapTests.cs
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


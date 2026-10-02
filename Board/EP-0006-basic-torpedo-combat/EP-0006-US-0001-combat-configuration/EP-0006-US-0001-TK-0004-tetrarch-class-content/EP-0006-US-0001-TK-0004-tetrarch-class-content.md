---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0001-combat-configuration
ticket: EP-0006-US-0001-TK-0004-tetrarch-class-content
title: "Настройки прочности класса Тетрарх"
stage: draft
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: ["EP-0006-US-0001-TK-0003-launcher-content"]
files_touched: 3
serves: ["AC-0001","AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 2
validation_status: not-run
---

# EP-0006-US-0001-TK-0004-tetrarch-class-content

Настройки прочности класса Тетрарх.

## Why

Результат тикета: **Настройки прочности класса Тетрарх**. Он нужен для истории [Настраиваемые характеристики Тетрарха и торпеды](../EP-0006-US-0001-combat-configuration.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0001** — Реестр конфигурации содержит класс `ship.tetrarch` с `hullHitPointsMax = 450`, загружаемый через `typeData.shipClasses`. Идентичность класса задаётся `typeId`, независимо от изображения и отображаемого имени. Назначение класса экземплярам кораблей и инициализация их HP принимаются в US-0002.
- **AC-0003** — Старый контент без новых необязательных секций загружается; новые данные попадают в output и доступны после перезапуска.

## Decisions

- **D01:** «нам нужно добавить новый модуль торпедного аппарата в схему Тетрарха»; «для ланчера нужна своя панель»; новое помещение — «новое», место — «выбери сам». Один launcher в новом помещении стандартного Тетрарха; отдельная command panel с кнопкой Пуск.
- **D17:** «для тетрарха в настройки тетрарха, для торпеды в настройки торпедного аппарата который является модулем тетрарха»; скорость и turn rate в модуле — «да». HP450 принадлежит class config Тетрарха; damage150/speed3/turnRate90 — конфигурации module.torpedo.launcher.basic, а не экземпляру палитры/константам Client.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 3 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Ships/ship-classes.json` | Новый файл этого тикета; на baseline отсутствует | Только данные, явно названные в Public API и шагах этого тикета |
| `src/DeepSpaceSaga.Client/Settings.json` | Существует: `src/DeepSpaceSaga.Client/Settings.json:1` | Только данные, явно названные в Public API и шагах этого тикета |
| `tests/DeepSpaceSaga.Client.Tests/TetrarchClassContentTests.cs` | Новый файл этого тикета; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0001-combat-configuration.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0001-TK-0003-launcher-content](../../EP-0006-US-0001-combat-configuration/EP-0006-US-0001-TK-0003-launcher-content/EP-0006-US-0001-TK-0003-launcher-content.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

Новая секция shipClasses=[{typeId: ship.tetrarch, hullHitPointsMax: 450}] в Data/Ships/ship-classes.json; typeData.shipClasses — путь в Settings.json.

## Implementation steps

1. Объявить файл класса в Settings без переноса HP в launcher или UI.
2. Проверить recursive Data/**/*.json copy rule текущего csproj и загрузку из build output.
3. Загрузить копию content с изменёнными значениями, проверить обновлённый реестр через bootstrap loader и успешный запуск нового Engine; исходные файлы тест не переписывает. Это evidence повторной загрузки конфигурации. Назначение класса кораблям и их runtime HP, включая `HullCombat` в snapshot, проверяются в US-0002.

## Out of scope

Файлы вне allowlist; production слои вне `content-data`; AI пирата, защита/ПР, вероятность/баланс RNG, дальность/боезапас/TTL, реальные операторы, self-destruct, area damage, salvage, звуки, victory UI и общий refactor карты. Не менять существующие Board-статусы/requirements или commit/push в рамках реализации без соответствующего поручения.

## Invariants

- Gameplay HP, projectile lifecycle и collision принадлежат Engine; UI работает через immutable Contracts/session и shared Motion (CLAUDE.md, Architecture).
- Flight time — MotionTimeMs; real-time эффекты — monotonic UI clock (SimulationClock.Update; AuthoritativeSnapshot.MotionTimeMs). Пауза не продвигает мир, но session infrastructure работает (§52).
- `TargetObjectId` фиксируется в явной команде; current selection не заменяет цель полёта (PlayerCommand, строка7).
- Owner/self исключены из контактов; первый contact применяется один раз; chance100% не расходует RNG. Размеры5px/50px UI не являются collider world radius.
- Корабль Тетрарх определяется class identity, не картинкой; неизвестные свойства остаются masked. Legacy defaults не дают скрытой новой боевой способности.
- Сохраняются независимые worktree изменения. При ошибке baseline тестов записать конкретный результат, не объявлять весь suite зелёным и не править чужой fixture вне scope.

## Tests

Имена ниже — планируемые тесты, а не уже существующие результаты:

- [ ] `Tetrarch_class_defines_450_hull_hp`
- [ ] `Restart_loads_edited_class_content_without_compilation`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~Tetrarch_class_defines_450_hull_hp|FullyQualifiedName~Restart_loads_edited_class_content_without_compilation'
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/TetrarchClassContentTests.cs
git diff --check
```

Проверить реальные loader и output копии JSON, не только синтаксис. Tests изменяют временные копии данных, не исходный пользовательский контент.

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


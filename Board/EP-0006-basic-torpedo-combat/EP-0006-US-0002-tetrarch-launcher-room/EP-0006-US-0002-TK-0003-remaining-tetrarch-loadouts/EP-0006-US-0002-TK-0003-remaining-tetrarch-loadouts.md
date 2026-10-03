---
epic: EP-0006-basic-torpedo-combat
story: EP-0006-US-0002-tetrarch-launcher-room
ticket: EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts
title: "Согласование остальных стандартных Тетрархов"
stage: draft
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: ["EP-0006-US-0002-TK-0002-primary-scenario-loadout"]
files_touched: 4
serves: ["AC-0003"]
priority: P1
created: 2026-10-02T08:22:58Z
revision: 1
validation_status: not-run
---

# EP-0006-US-0002-TK-0003-remaining-tetrarch-loadouts

Согласование остальных стандартных Тетрархов.

## Why

Результат тикета: **Согласование остальных стандартных Тетрархов**. Он нужен для истории [Тетрарх получает помещение торпедного аппарата](../EP-0006-US-0002-tetrarch-launcher-room.md) и покрывает указанные `serves` в своей части; полные критерии story завершаются совокупностью её тикетов.

- **AC-0003** — Стандартные Тетрархи других существующих сценариев используют согласованную комплектацию; специализированный MarketProfiles с трёхклеточным корпусом не переклассифицируется по изображению.

## Decisions

- **D01:** «нам нужно добавить новый модуль торпедного аппарата в схему Тетрарха»; «для ланчера нужна своя панель»; новое помещение — «новое», место — «выбери сам». Один launcher в новом помещении стандартного Тетрарха; отдельная command panel с кнопкой Пуск.
- **D03:** «у нас уже есть сценарий с кораблем пиратом и кораблем игрока»; на вопрос о движении игрока — «может»; пиратский аппарат неактивен — «пока да». Использовать PlayerShipOnly/Player Ship and Pirate. Пират летит прямо и не стреляет/не защищается; игрок управляет навигацией обычным способом.

Полный журнал, исходный scope и определения D/A — [Documentation.md](../../Documentation.md). Не переносить ранние предложения о вероятностях/КД/дальности из черновика обратно в MVP.

## Assumptions

Применяются A01–A12 эпика в части этого тикета. Все API ниже **предлагаемые**, кроме прямо названных существующих методов. Naming/overload можно уточнить без смены семантики и allowlist. Если для решения нужен дополнительный production/test файл, сначала уточнить scope; не скрывать его за количеством `files_touched`.

Новых продуктовых решений сверх согласованного объёма этот тикет не вводит. Порядок тестов и внутренние helper names выбираются в пределах заявленного слоя.

## Code context

Полный write allowlist от корня DSS; 4 файлов, включая тесты. Другие файлы — read-only. Новые файлы из зависимостей должны реально существовать к началу исполнения.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Scenarios/Default_500/scenario.json` | Существует: `src/DeepSpaceSaga.Client/Scenarios/Default_500/scenario.json:1` | Только данные, явно названные в Public API и шагах этого тикета |
| `src/DeepSpaceSaga.Client/Scenarios/Docked/scenario.json` | Существует: `src/DeepSpaceSaga.Client/Scenarios/Docked/scenario.json:1` | Только данные, явно названные в Public API и шагах этого тикета |
| `src/DeepSpaceSaga.Client/Scenarios/Undocked/scenario.json` | Существует: `src/DeepSpaceSaga.Client/Scenarios/Undocked/scenario.json:1` | Только данные, явно названные в Public API и шагах этого тикета |
| `tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs` | Создаётся зависимостью `EP-0006-US-0002-TK-0002-primary-scenario-loadout`; на baseline отсутствует | Только проверки поведения, перечисленные в Tests; существующие независимые assertions сохранить |

Контекст для чтения: [grounding эпика](../../Documentation.md), [история](../EP-0006-US-0002-tetrarch-launcher-room.md), `Documentation/01-Requirements/EngineRequirements.md` (§2, §52, §55–57, §60), `Documentation/00-Process/CLAUDE.md`. Code context определяет границу записи, ссылки для чтения её не расширяют.

## Dependencies

- [EP-0006-US-0002-TK-0002-primary-scenario-loadout](../../EP-0006-US-0002-tetrarch-launcher-room/EP-0006-US-0002-TK-0002-primary-scenario-loadout/EP-0006-US-0002-TK-0002-primary-scenario-loadout.md)

Нужны реализованные и проверенные результаты зависимостей. При их отсутствии остановить этот implementation ticket с конкретным gap, не имитировать готовность mock-объектами и не дописывать зависимость за его пределами.

## Public API after the change

Тот же shipClassId/new cell/module loadout для трёх стандартных сценариев; MarketProfiles не меняется.

## Implementation steps

1. Применить ту же схему к явно стандартным 10-cell hulls; сохранить docking, speed, cargo, economy и world manifest.
2. Расширить table-driven проверку всех пяти стандартных сценариев; MarketProfiles проверить как неизменённую legacy конфигурацию.
3. Никакой runtime магии по имени сценария; новое состояние задаётся данными.

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

- [ ] `All_standard_tetrarch_scenarios_load_with_launcher`
- [ ] `Market_profiles_legacy_fixture_is_not_reclassified`

Из корня `D:/DeepSpaceSaga/DSS` (PowerShell):

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter 'FullyQualifiedName~All_standard_tetrarch_scenarios_load_with_launcher|FullyQualifiedName~Market_profiles_legacy_fixture_is_not_reclassified'
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs
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


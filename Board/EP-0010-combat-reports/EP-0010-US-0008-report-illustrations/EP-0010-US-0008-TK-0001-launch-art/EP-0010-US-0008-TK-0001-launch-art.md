---
epic: EP-0010-combat-reports
story: EP-0010-US-0008-report-illustrations
ticket: EP-0010-US-0008-TK-0001-launch-art
title: "Иллюстрации дальности и торпедных пусков"
stage: implemented
status: implemented
layer: client
depends_on: ["EP-0010-US-0007-TK-0002-localized-report-text"]
files_touched: 3
serves: ["US08-AC1"]
created: 2026-10-16
revision: 1
validation_status: artifact-verified
---

# Иллюстрации дальности и торпедных пусков

## Why

Подготовить первые три графических сюжета. [История](../EP-0010-US-0008-report-illustrations.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R12, R14, R15, R22.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-in-range.png` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-torpedo-launch.png` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/Images/UI/CombatReports/player-torpedo-launch.png` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0007-TK-0002-localized-report-text; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. Создать три оригинальные PNG 1024×576: enemy range, enemy launch, player launch; соблюдать описание R14/R15 и A08.
2. Силуэты Тетрара/Мух согласовать с существующими изображениями кораблей; не добавлять текст или UI рамки в рисунок.
3. Проверить размеры/формат и визуально посмотреть при ширине карточки; сохранить только финальные три файла. При генерации применять доступный imagegen workflow.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `Artifact dimensions and visual inspection recorded in ticket evidence` (планируемый сценарий проверки).

Команды ниже — исходный план воспроизведения из корня репозитория; реальные результаты — в Execution evidence и ImplementationStatus:

```powershell
# PowerShell, из D:\DeepSpaceSaga\DSS; bundled python/Pillow при необходимости взять через load_workspace_dependencies.
python -c "from PIL import Image; import sys; images=[Image.open(p) for p in sys.argv[1:]]; assert all(i.format == 'PNG' and i.size == (1024,576) for i in images)" src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-in-range.png src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-torpedo-launch.png src/DeepSpaceSaga.Client/Images/UI/CombatReports/player-torpedo-launch.png
git diff --check
# Визуально открыть каждый PNG в масштабе карточки; техническая проверка не заменяет art acceptance.
```

## Definition of Done

- Шаги выполнены только в 3 разрешённых файлах, выбранный layer соблюдён.
- Критерии US08-AC1 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Подготовить первые три графических сюжета. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Artifact evidence - 2026-10-16

Built-in imagegen, one generation per asset. References: Images/CelestialObjects/Spacecraft/ship-tetrarch-class.png and ship-zero-class.png (existing visual family; dedicated Mukha sprite absent). Final files in listed Images/UI/CombatReports paths, opaque RGB PNG1024x576, normalized from1672x941 to required dimensions. All three inspected together at320x180; detection/hostile launch/player launch remain distinct, no embedded text. Assistant visual QA PASS; human art acceptance OPEN. No runtime changes.

Prompt set: original cinematic isometric DSS hard-sci-fi combat illustrations; charcoal worn gunmetal hulls matching reference silhouettes, black stars, teal rim light, main action readable at280px, full principal ships in frame, opaque16:9, no text/letters/numbers/watermarks/UI borders. enemy-in-range: compact hostile contact ahead of Tetrarch under amber detection illumination; enemy-torpedo-launch: enemy swept-wing fighter visibly releases torpedo toward distant Tetrarch, amber threat exhaust; player-torpedo-launch: Tetrarch forward tube releases separated torpedo toward distant fighter, cyan own-action trail. Generated source files retained under Codex generated_images session folder.

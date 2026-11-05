---
epic: EP-0010-combat-reports
story: EP-0010-US-0008-report-illustrations
ticket: EP-0010-US-0008-TK-0003-damage-art
title: "Уничтожение противника и повреждение игрока"
stage: implemented
status: implemented
layer: client
depends_on: ["EP-0010-US-0008-TK-0002-defense-art"]
files_touched: 2
serves: ["US08-AC1"]
created: 2026-10-16
revision: 1
validation_status: artifact-verified
---

# Уничтожение противника и повреждение игрока

## Why

Завершить восемь вариантов и отличить попадание от уничтожения. [История](../EP-0010-US-0008-report-illustrations.md), [ТЗ](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R12, R14, R15, R22.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Execution, art и отдельные commit/push поручены EpicExecutionPrompt2026-10-16. Самостоятельный запуск игры запрещён последующей явной инструкцией пользователя; native/human/FPS OPEN. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Code context ниже фиксирует исходный planning baseline; актуальные API и результаты приведены в Execution evidence и engineering CombatReports.md. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-ship-destroyed.png` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/Images/UI/CombatReports/player-ship-damaged.png` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0008-TK-0002-defense-art; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. Создать два PNG: enemy destroyed и player damaged; отличать поражение цели от разрушения и сохранять узнаваемость сторон.
2. При повреждении показывать удар по внешним модулям, без введения условной полосы Hull HP.
3. Проверить размер, отсутствие встроенного текста и согласованность всей серии; зарегистрировать visual acceptance как OPEN до просмотра.

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
python -c "from PIL import Image; import sys; images=[Image.open(p) for p in sys.argv[1:]]; assert all(i.format == 'PNG' and i.size == (1024,576) for i in images)" src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-ship-destroyed.png src/DeepSpaceSaga.Client/Images/UI/CombatReports/player-ship-damaged.png
git diff --check
# Визуально открыть каждый PNG в масштабе карточки; техническая проверка не заменяет art acceptance.
```

## Definition of Done

- Шаги выполнены только в 2 разрешённых файлах, выбранный layer соблюдён.
- Критерии US08-AC1 подтверждены именованными проверками выше и ссылками на evidence.
- Focused проверки и соответствующий build/format проходят либо исходное падение записано отдельно; prose/art gates проверены подходящим способом.
- Сохранены public API/invariants/out-of-scope; нет скрытой работы или нерешённых блокирующих допущений.
- Native/human/FPS не помечены PASS по headless; статус NOT RUN/OPEN сохраняется до реального прогона.
- Обновлён status/evidence тикета, выполнен diff check; не заявлять реализацию на основании этого planning-документа.

## Self-containment check

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Завершить восемь вариантов и отличить попадание от уничтожения. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

## Artifact evidence - 2026-10-16

Two separate built-in imagegen outputs normalized to opaqueRGB PNG1024x576. All eight final assets rechecked for size/mode/format; damage pair inspected at320x180. Enemy destruction has clearly separated hull sections; player damage retains Tetrarch silhouette with localized external nacelle/armor damage, no Hull HP bar. Assistant visual QA PASS, human art acceptance OPEN.

Prompt set: same original cinematic isometric charcoal/teal DSS visual family and existing Tetrarch/zero-class silhouette references, no text/letters/numbers/borders/UI. enemy-ship-destroyed: swept-wing fighter decisively breaks into large wing/nacelle/fuselage pieces around contained warm explosion, distant Tetrarch intact. player-ship-damaged: intact Tetrarch with torn side module armor, exposed glowing localized damage, sparks/small debris, no catastrophic breakup. Final paths listed in scope; original sources retained in Codex generated_images.

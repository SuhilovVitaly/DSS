---
epic: EP-0010-combat-reports
story: EP-0010-US-0008-report-illustrations
ticket: EP-0010-US-0008-TK-0002-defense-art
title: "Иллюстрации ПРО, перехвата и попадания"
stage: draft
status: planned
layer: client
depends_on: ["EP-0010-US-0008-TK-0001-launch-art"]
files_touched: 3
serves: ["US08-AC1"]
created: 2026-10-16
revision: 1
validation_status: not-run
---

# Иллюстрации ПРО, перехвата и попадания

## Why

Продолжить общий стиль тремя отдельными сюжетами. [История](../EP-0010-US-0008-report-illustrations.md), [ТЗ](../../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R12, R14, R15, R22.

## Decisions

Пользователь поручил сохранить ТЗ и создать эпик с историями/тикетами (2026-10-16). Выполнение этого тикета, commit/push, генерация art или запуск игры пока не поручены. Продуктовые решения — §§1–10 ТЗ; не выдавать A01–A10 за дополнительные ответы пользователя.

## Assumptions

Технические A01–A10 ТЗ. Proposed API ниже — ожидаемый результат, не утверждение о текущем коде. Максимум 5 файлов из таблицы; Board evidence не разрешает скрытые изменения production. Layer `client`; matching coverage `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Для purely art/evidence runtime source не меняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Images/UI/CombatReports/player-countermeasure-launch.png` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-torpedo-intercepted.png` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |
| `src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-ship-hit.png` | NEW — proposed; файл отсутствует на planning baseline | Изменение только для шагов этого тикета; соседний scope запрещён |

Read-only общий [grounding](../../Documentation.md#grounding) содержит текущие точки входа и ограничения. Остальные файлы не разрешены к изменению. Зависимости: EP-0010-US-0008-TK-0001-launch-art; их новый код/DTO считаются доступными только после завершения.

## Public API after the change

No public API change.

## Implementation steps

1. Создать три PNG: player countermeasure launch, enemy torpedo intercepted, enemy ship hit; единый свет/палитра/масштаб первой серии.
2. Перехват визуально отличается от попадания по кораблю; без текста и чисел.
3. Проверить 1024×576 и читаемость в уменьшении; финальная оценка графики отдельно от технической валидации.

## Out of scope

Изменение баланса/конфигурации оружия, формул попадания, RNG, часов, поведения AI, discovery rules, посторонних журналов и незакоммиченных изменений. Никакой публикации или автоматического выполнения следующего тикета без execution-поручения. Root save version не менять в обход A07.

## Invariants

- Engine/renderer separation: `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs:10`; immutable snapshot: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11`. Через boundary нет Skia/GL.
- Физическое время/модульный бой — EngineRequirements §Phase Four Module Combat и §§7–8 ТЗ; UI не меняет исходы/часы/sequence журнала.
- Captured identity, privacy, once-per-target и ровно одна открытая карточка проверяются в соответствующих layers; этот тикет не ослабляет смежные гарантии.

## Tests

- `Artifact dimensions and visual inspection recorded in ticket evidence` (планируемый сценарий проверки).

Команды выполнять из корня репозитория **при реализации**, сейчас NOT RUN:

```powershell
# PowerShell, из D:\DeepSpaceSaga\DSS; bundled python/Pillow при необходимости взять через load_workspace_dependencies.
python -c "from PIL import Image; import sys; images=[Image.open(p) for p in sys.argv[1:]]; assert all(i.format == 'PNG' and i.size == (1024,576) for i in images)" src/DeepSpaceSaga.Client/Images/UI/CombatReports/player-countermeasure-launch.png src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-torpedo-intercepted.png src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-ship-hit.png
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

Вход: зависимости по ID, ТЗ, scope и Code context. Выход: Продолжить общий стиль тремя отдельными сюжетами. Проверки конкретизированы. Если текущий diff требует шестого файла или второго production layer, сначала переразбить/обновить тикет, не расширять allowlist молча.

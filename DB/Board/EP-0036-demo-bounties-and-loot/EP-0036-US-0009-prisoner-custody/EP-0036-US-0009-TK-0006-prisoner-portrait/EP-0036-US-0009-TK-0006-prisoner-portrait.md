---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0009-prisoner-custody
ticket: EP-0036-US-0009-TK-0006-prisoner-portrait
title: Изображение захваченного пилота в позе пленного
stage: approved
approval_kind: workflow
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0036-US-0009-TK-0005-custody-ui]
files_touched: 5
serves: [AC-04, AC-05, AC-06]
created: 2026-11-17T19:50:17Z
revision: 1
implementation_status: not-started
---

# Изображение захваченного пилота в позе пленного

## Why

После успешного ареста живого пилота Disabled enemy drone окно добычи должно показывать этого же человека в позе пленного: на коленях, бёдра и колени повёрнуты в сторону, руки за спиной. Подготовленные тела сами по себе не подключают эту картинку к игровому событию.

## Decisions

Пользователь: «создай тикет в нужной стори эпика D:\DeepSpaceSaga\DSS\DB\Board\EP-0036-demo-bounties-and-loot о том что при взятии в плен пилота выведенного из строя дрона показывалась нужная картинка».

Время записи 2026-11-17T19:50:17Z, не время отправки. Контекст: пользователь потребовал поворот бёдер, добавление тела в шортах и исправление слишком маленькой головы. Использовать текущую ревизию v3, а не отвергнутую v2. Это workflow approval планирования, не приёмка изображения или разрешение реализации/публикации.

По прямому запросу добавить отдельный тикет в существующую соответствующую историю сохранены US-0009 и TK-0001..0005; TK-0006 — локальное исключение из обычного ограничения epic-writer в пять тикетов на историю. Не переносить существующие задачи и зависимости ради этого дополнения.

## Assumptions

- A-09-06-01: место отображения — существующая карточка выбранного пилота в окне добычи TK-0005; отдельный модальный экран, анимация или новый игровой шаг не требуются.
- A-09-06-02: если внешность содержит назначенный костюм, использовать соответствующий вариант. Если сведений о костюме нет, пилоту дрона выбирать pilot. Shorts используется только при явном назначении этого комплекта; арест не переодевает персонажа в шорты.
- A-09-06-03: лицо определяется авторитетным source pilot identity / PortraitAssetId и существующим назначением внешности. Пример CHR-20261117-011947-JFG71U.png — только визуальная проверка ассетов; нельзя показывать его всем пленным.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Demo/BattleLootWindow.cs` | Planned prerequisite: TK-0005, Implementation steps 1..3 | Подключить изображение выбранного подтверждённо захваченного пилота и безопасный fallback. |
| `src/DeepSpaceSaga.Client/UI/Portraits/PrisonerPortraits.cs` | Новый файл | Композиция текущей головы с телом kneeling, профили v3, cache/disposal, защита путей и fallback. |
| `src/DeepSpaceSaga.Client/UI/Portraits/ModularCrewPortraits.cs` | Строки 15..37: Dialogue/Pilot/Assigned; 47..85: Compose читает composition-pack и standing transforms | Переиспользуемое чтение назначения head/outfit без подмены личности; сохранить существующие стоящие портреты. |
| `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` | Строки 91..95: изображения из Images копируются в output | При необходимости явно включить kneeling profile.json в build/publish; проверить файлы в чистом output. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0009Tk0006Tests.cs` | Новый файл | Состояние ареста, выбор личности/костюма, композиция и ошибки ассетов. |

Ровно пять implementation files, один production layer. Read-only inputs:

- [US-0009](../EP-0036-US-0009-prisoner-custody.md), [эпик](../../Documentation.md), [TK-0005](../EP-0036-US-0009-TK-0005-custody-ui/EP-0036-US-0009-TK-0005-custody-ui.md).
- `src/DeepSpaceSaga.Client/Images/Persons/ModularCrew/kneeling/README.md`; семь папок pilot, engineer, medic, security, explorer, eva, shorts с body.png, body-back.png, collar-front.png и profile.json.
- Текущие назначения `Data/UI/modular-portraits.json` и `Images/Persons/ModularCrew/composition-pack.json`; использовать их формат головы/метаданные, не считать все головы одинаково кадрированными 235x235.
- `DB/Documentation/01-Requirements/EngineRequirements.md:11..13,44..46`: Engine владеет миром; pending не является успешным выполнением.

Ассеты подготовлены в предыдущей работе, но могут быть untracked: при исполнении проверить наличие в checkout и packaging, не заявлять готовность по локальным файлам вне репозитория. Перерисовка/изменение ассетов не входят в allowlist.

## Public API after the change

No public Engine/Contracts API change. Новый internal Client-композитор получает уже разрешённые head/outfit и профиль позы; не назначает RNG-личность и не создаёт PrisonerSnapshot. Точное internal API выбрать в пределах allowlist после TK-0005; это проект, не описание существующей реализации.

## Implementation steps

1. После TK-0005 использовать подтверждённую проекцию захваченного пилота. Не переключать картинку только по нажатию «Арестовать»: pending, rejection и отсутствие каюты сохраняют прежнее представление.
2. Разрешить исходное лицо по source pilot identity / PortraitAssetId, костюм — по существующему assignment. Поддержать все семь допустимых вариантов; при неизвестном костюме использовать pilot, сохранив лицо. Не получать лицо из временного порядка карточек.
3. Загрузить runtime PNG и профиль v3 относительно application root. Числа брать из профиля: canvas 1024x1536, подбородок (522.5,913.7), центр воротника (522.5,931.7), лицо глаза–подбородок 68 px, collar-to-ground 586.3 px. Это связанная калибровка; не подгонять тело обратно к прежним 715 px при неизменной голове.
4. Применить масштаб конкретного исходного лица по его формату и метаданным. example_head_transform относится только к демонстрационному портрету. Для костюмов порядок body-back → head → collar-front; для shorts — профиль skin-feather. Не обрезать подбородок, волосы и челюсть. Удалить chroma green только у соответствующих исходников.
5. Размещать готовую композицию в UI единым равномерным масштабом, показывая колени и руки за спиной в пределах изображения. Не растягивать по осям и не менять размер головы независимо при layout/resize.
6. При повторном открытии или Load восстановить ту же личность/костюм/позу из подтверждённого состояния без повторной команды ареста. Cache key включает head, outfit и pose/profile version; смена выбранного дрона не оставляет лицо предыдущего пленного.
7. Если лицо/ассет/профиль отсутствует или повреждён, не падать: показать имеющийся портрет того же пилота либо существующий нейтральный placeholder, не чужое лицо. Логировать проблему существующим механизмом. Проверить build/publish JSON рядом с PNG.

## Served acceptance criteria

- AC-04: изображение дополняет существующую карточку ареста в окне лута.
- AC-05: повторное открытие/Load не меняет личность и не инициирует повторный арест.
- AC-06: после подтверждённого ареста именно выбранный пилот показан на соответствующем теле пленного с пропорциями v3; pending/rejection не изображаются как успешное пленение.

## Out of scope

Engine/Contracts/persistence изменения, новые условия ареста, генерация новых лиц/тел, переодевание пленника, анимация, отдельное окно, пытки/вербовка, commit/push. Не переделывать весь ship roster или все стоящие портреты.

## Invariants

- Авторитет события — подтверждённое состояние из TK-0005; UI не создаёт пленника и не расходует каюту.
- Source identity стабильна; изображение не заменяет идентификатор человека.
- Пропорции и точки сочленения — единая калибровка; v2 отвергнута пользователем.
- Read-only ассеты и исходные головы не мутируют при композиции; одинаковый вход даёт одинаковый результат.

## Tests

Будущие проверки, NOT RUN:

- `ConfirmedArrest_ShowsSamePilotKneeling`
- `PendingOrRejected_DoesNotShowCapturedPose`
- `SwitchTarget_DoesNotReusePreviousFace`
- `ReopenAndLoad_PreserveIdentityWithoutCommand`
- `AllSevenOutfits_UsePoseProfileAndPreserveRatios` (параметризованная проверка, включая skin-feather shorts).
- `DifferentHeadFormats_DoNotReuseExampleTransform`
- `MissingOrCorruptAsset_UsesSafeFallback`
- `ExistingStandingPortraits_AreUnchanged`

Из корня DSS, после создания абсолютного run под `D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0009-TK-0006-prisoner-portrait/<UTC-run>/`:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0009Tk0006Tests --results-directory $run --logger trx
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path "$run/build"
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include src/DeepSpaceSaga.Client/UI/Demo/BattleLootWindow.cs src/DeepSpaceSaga.Client/UI/Portraits/PrisonerPortraits.cs src/DeepSpaceSaga.Client/UI/Portraits/ModularCrewPortraits.cs
```

Каждый process tree ограничить 300000 ms; stdout/stderr направить в тот же абсолютный run. Проверить копирование профилей в clean build/publish output. Native проверка по правилам малого монитора: арест, отказ, повторное открытие, смена цели, resize; показать все семь сборок и открытый стык shorts. Если запуск не выполнен — NOT RUN, product acceptance OPEN.

## Definition of Done

- Шаги выполнены в allowlist; подтверждённый арест отображает нужного человека, костюм и позу.
- Named tests и owning build проходят; исходные failures отделены от нового изменения.
- Чистая сборка содержит PNG и JSON; нет зависимости от DSS-Images или пользовательского .codex.
- Общий вид и сочленения показаны на визуальной проверке; числовые тесты не заменяют product acceptance.
- Фактические проверки и оставшиеся gaps записаны; planning не объявляется реализацией.

## Self-containment check

Prerequisite TK-0005 определяет источник состояния и UI. Ассеты, fallback, композиция, allowlist и проверки перечислены. Если prerequisite изменит пути/API, сначала актуализировать Board; не расширять production layers молча. Не запускать исполнение из факта создания этого документа.

## Asset drift and scope correction
The current kneeling README/index supersede the draft generic-composition assumption: four exact portrait IDs now have 28 repaired precomposed images (v3 proportions, repaired necks), and explicitly prohibit overlaying heads again. Use these current corrected assemblies by exact portrait identity/outfit; unknown/unprepared identities retain their own portrait or neutral placeholder. Do not reuse the example transform on other heads or alter existing pilot assignments. Package the existing prepared kneeling assets/index/profiles in this ticket, without modifying their pixels. Add TacticalBattleScreen.OnDeactivated disposal for the owned one-image cache. This is an explicit missing-asset fallback, not a claim that every possible portrait has approved kneeling artwork.

## Execution
Integrated exact-identity repaired v3 pose selection for all seven prepared outfits, default pilot only when no outfit assigned. Pending uses original portrait; missing/incompatible assembly uses original or neutral name card. Cache owns one selected image and disposes on target change/deactivation. Profiles/index copied to output and publish; existing prepared assets included unchanged. Client/test build PASS (D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us9-tk6-build.log). Native NOT RUN; generic unprepared head/body registrations are not inferred from example metadata.

Boundary/review correction: None Update could not restore JSON removed by the existing ModularCrew None Remove. Changed the owning Content Include; initial US9 portrait tests correctly failed on missing metadata. Rerun pending after engine provenance correction.

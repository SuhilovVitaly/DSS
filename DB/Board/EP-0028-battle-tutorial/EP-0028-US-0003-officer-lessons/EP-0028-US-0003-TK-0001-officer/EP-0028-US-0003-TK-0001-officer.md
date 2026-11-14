---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0003-officer-lessons
ticket: EP-0028-US-0003-TK-0001-officer
title: Офицер «Скитальца»
stage: approved
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0002-TK-0002-comms-panel, EP-0028-US-0002-TK-0001-ui-texts]
implementation_files: 3
files_touched:
  - src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json
  - src/DeepSpaceSaga.Client/Data/Locale/Russian.json
  - src/DeepSpaceSaga.Client/Data/Locale/English.json
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialContentTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialLessonFlowTests.cs
serves: [US-0003/AC-01, US-0003/AC-06]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Офицер «Скитальца»

[История](../EP-0028-US-0003-officer-lessons.md) · [Эпик](../../Documentation.md)

## Цель

Ввести единственного говорящего персонажа: имя и должность RU/EN, портрет из существующих ассетов, гайд тона и первая реплика — приветствие, которое сразу учит выбору пилота. Создать поставляемый `battle-tutorial.json`.

## Decisions

D-01 — женщина-офицер «Скитальца», не пилоты дронов; D-06 — RU/EN; A-05, A-06 [эпика](../../Documentation.md#допущения).

## Assumptions

- **Имя:** RU «Лейтенант Ирина Вольская», EN "Lieutenant Irina Volskaya". **Должность:** RU «офицер тактической связи „Скитальца“», EN "tactical comms officer, Skitalets". Значения можно уточнить без смены ключей.
- **Портрет (конкретный подход).** Новые бинарные арты не создаются. Голова — существующий файл W4 `Images/Persons/W4/Portraits/CHR-20260915-104309-12BAB0.png` (светлые волосы, каре; в репозитории не ссылается ни один текстовый файл; отличается от голов пилотов Вера `CHR-…-3C5C5D` и Марта `CHR-…-4DF131` в `Data/UI/tactical-battle-pilots.json` и от `CharacterPortraits.DefaultFemale` `CHR-…-A2570C`, `src/DeepSpaceSaga.Contracts/CharacterPortraits.cs:8`). Головы W4 обнаруживаются автоматически из папки `Portraits` (`src/DeepSpaceSaga.Client/Portraits/PortraitAssetRepository.cs:21,66-74`), костюм выбирается детерминированно по ключу `tutorial.officer` (`UI/Portraits/DialoguePortraitComposer.cs:32-33`), результат 300×300 (`:47`). Панель (US-0002-TK-0002) уже вызывает композитор и рисует заглушку при `null`. Если голова недоступна в сборке — заглушка через тот же путь, без новых файлов.
- Случайный выбор женских портретов Engine для экипажа (`CharacterPortraits.FemaleFolder`) теоретически может совпасть с этой головой; это допустимо (портрет в бою обучения виден без экипажа).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json` | Нет файла; схема — US-0001-TK-0001 | Создать: `schemaVersion: 1`, `officer` (`nameKey: Tutorial.Officer.Name`, `roleKey: Tutorial.Officer.Role`, `portrait` — путь выше, `portraitKey: tutorial.officer`), `limits` по умолчанию, шаг `intro` из [каталога](../EP-0028-US-0003-officer-lessons.md#каталог-уроков) |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Ключи `Tutorial.Panel.*` из US-0002-TK-0001 | `Tutorial.Officer.Name`, `Tutorial.Officer.Role`, `Tutorial.Line.Intro` |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | То же | Те же ключи |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialContentTests.cs` | Нет файла | Тесты содержимого (общий файл для TK-0001…0005) |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialLessonFlowTests.cs` | Нет файла. Живой бой: `BattleNavigationTests.Fixture` + Tabletop из `Skins` выхода; экран `new TacticalBattleScreen(flow, 1, tabletop) { Tutorial = new TutorialSession(TutorialScriptLoader.Load(AppContext.BaseDirectory)) }` | Тесты сценария уроков (общий файл для TK-0001…0005) |

Реплика `intro` (предложение):
- RU: «Командир, на связи Вольская, „Скиталец“. Начнём с манёвра: щёлкните карточку пилота слева.»
- EN: "Commander, Volskaya on comms from Skitalets. Let's start moving: click a pilot card on the left."

## Public API after the change

Поставляемый `Data/Tutorial/battle-tutorial.json` и ключи `Tutorial.Officer.*`, `Tutorial.Line.Intro`. Кода нет.

## Implementation steps

1. Создать `battle-tutorial.json` (officer, limits, `intro`).
2. Добавить ключи в обе локали.
3. `TutorialContentTests`:
   - `ShippedScriptLoadsWithoutWarnings` — `TutorialScriptLoader.Load(AppContext.BaseDirectory, warnings.Add)` без предупреждений, `Officer` не null;
   - `EveryLineKeyExistsInBothLocales` — каждый `LineKey` и ключи officer есть в RU и EN (`Localization.LoadLocaleFile`);
   - `EveryLineFitsTwoPanelLinesInBothLanguages` — для RU и EN `TutorialCommsPanel.Wrap(text).Length <= 2`;
   - `OfficerPortraitComposesDeterministicallyFromExistingHead` — `DialoguePortraitComposer.Compose(officer.Portrait, PersonSex.Female, officer.PortraitKey)` не null, 300×300, байты двух вызовов равны; путь головы не встречается в `Data/UI/tactical-battle-pilots.json` и не равен `CharacterPortraits.DefaultFemale`.
4. `TutorialLessonFlowTests.IntroGreetsOnFirstMoveAndClosesWhenPilotSelected` — живой Tabletop-бой: первый кадр Move показывает `intro`, имя офицера на текущем языке в `Layout.Texts`, подсветка `pilot:*`; Tab → `intro` Completed.

## Acceptance criteria

1. Поставляемый сценарий загружается без предупреждений.
2. Имя, должность и приветствие есть в RU и EN и помещаются в панель.
3. Портрет компонуется из существующей головы детерминированно; новых бинарных файлов в diff нет.
4. В бою обучения первым звучит приветствие, закрывающееся выбором пилота.

## Verification signal

Детерминированные проверки контента: первым — красный `TutorialContentTests.ShippedScriptLoadsWithoutWarnings` (файла нет), затем остальные тесты шага 3 и flow-тест шага 4. Дополнительно: `git diff --stat` не содержит `.png`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0003-tk0001-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~DialoguePortraitComposerTests|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=officer.trx"
```

Растровые снимки панели RU/EN — в `$RUN`. Process-tree cap 300000 ms.

## Out of scope

Остальные уроки (TK-0002…0005), правки кода, новые PNG.

## Invariants

- Только ключи `Tutorial.*`; существующие ключи не меняются.
- Паритет RU/EN (`TutorialLocaleParityTests`) зелёный.

## Known fallout

`TutorialFlowTests`: с появлением файла в бою обучения появится панель; assertions EP-0027 (тексты «На карту», клики подтверждения выхода) не должны меняться — регрессионный запуск.

## Definition of Done

- Файл и ключи добавлены; named tests зелёные; паритет зелёный.
- Отдельный коммит и `git push`.

## Self-containment check

Путь головы, ключ композиции, ключи локали, тексты и тестовые API указаны явно.

## Журнал

2026-11-14T15:04:43Z — тикет создан.

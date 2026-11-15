---
epic: EP-0022-tactical-battle-xenon-star
story: EP-0022-US-0001-select-xenon-skin
ticket: EP-0022-US-0001-TK-0002-validate-installed-skin
title: Проверять установленный Xenon Star без дополнительного флага
stage: implemented
layer: client
depends_on: [EP-0022-US-0001-TK-0001, EP-0022-US-0006-TK-0001]
files_touched: 2
serves: [AC-01]
---

# Проверять установленный Xenon Star без дополнительного флага

[История](../EP-0022-US-0001-select-xenon-skin.md) · [Эпик](../../Documentation.md)

Финальный review обнаружил, что сборка с `XenonStarSource` требует ещё одной переменной среды для прохождения теста поставки. Без неё парные тесты также пропускают уже установленный пакет.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ShippedSkinTests.cs` | Ожидает отсутствие пакета без флага | Проверять присутствующий пакет по умолчанию. Сохранить явный режим ожидания для проверки сборки. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0022SkinScenesTests.cs` | Возвращается до проверки фактической установки | Выполнять парные проверки при наличии пакета. Сигнализировать ошибку, если validation runner требует пакет, а он отсутствует. |

## Acceptance criteria

При `XenonStarSource` тесты поставки и сцен работают в обычной команде `dotnet test`. В сборке без набора проверяется Classic fallback. `DSS_EXPECT_XENON_STAR=1` по-прежнему выявляет отсутствие пакета, а `0` выявляет его непредусмотренное попадание в чистую поставку.

## Tests

Release `ShippedSkinTests` и `Ep0022SkinScenesTests` с набором и без дополнительной переменной. Проверка отсутствия пакета в итоговом стандартном publish. Процесс ограничен 300000 ms, результаты в workspace artifacts.

## Definition of Done

Пройдены тесты поставки и сцен, scoped format и diff. Код клиента и форматы прежние. Отдельный commit и push.

## Execution

- Опубликованный тест с уже установленным пакетом и без переменной воспроизвёл ошибку ожидания. После исправления тактическая регрессия с набором и без переменной — 428/428. Фильтр исключал два класса, которым нужен стандартный путь дерева сборки.
- Чистая Release-сборка без набора с явным ожиданием `DSS_EXPECT_XENON_STAR=0` — 2/2 теста поставки. Scoped formatter и `git diff --check` прошли.

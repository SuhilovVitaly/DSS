---
epic: EP-maintenance
story: EP-maintenance-US-0001-station-local-geography
ticket: EP-maintenance-US-0001-TK-0003-render-factory
title: Показать заводы на карте
stage: approved
implementation_status: implemented
depends_on: [EP-maintenance-US-0001-TK-0004-factory-object-type]
---

# Показать заводы на карте

[Критерии истории](../EP-maintenance-US-0001-station-local-geography.md).

## Область изменений

Client. Файлы `GameSessionScreen.MapView.cs`, `SolarSystemLayerRenderer.cs`, `SpaceMapColorResolver.cs`, `Settings.json` и `Images/CelestialObjects/Factory/factory.png`.

## Реализация и проверка

Показать заводы отдельными метками в локальном обзоре, подключить PNG для карточки объекта и учитывать смещение выбранного объекта при отрисовке орбиты. Увеличить четыре дальние ступени масштаба.

После проверки пользователем исправлена видимость окрестностей. Кнопка
«Станция: локально» вписывает в свободную область карты станцию и компактную
группу заводов. Заводы видны отдельными маркерами, пока их расстояние от
станции на экране составляет хотя бы 12 пикселей. Единственный астероид
рисуется отдельным маркером вместо счётчика поля «1».

Подпись завода рисуется только при наведении на его маркер. Между видимым
заводом и его станцией рисуется тонкая серая линия под маркерами. Для этой
правки затронуты `ObjectLabelRenderer.cs`, `GameSessionScreen.cs`,
`GameSessionScreen.MapView.cs` и `ClusterMapPresentation.cs`.

При включённом слое Orbits показывать кривую каждого видимого объекта с
орбитальным движением, включая неопознанные контакты. Заводы исключить. Объекты за
кадром и объекты, заменённые агрегатами, не добавляют кривых. Реализация
использует `SolarSystemLayerRenderer.cs`, `GameSessionScreen.cs` и
`TacticalMapMarkerPolicy.cs`. Доступность переключателя определяется в
`GameSessionScreen.MapView.cs`. Сборка и семь целевых проверок прошли, включая
растровую проверку видимых орбит, исключений и совпадающих кривых.

- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-visible-orbits/build.log`
- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-visible-orbits/orbit-checks.log`

После уточнения проверены орбита видимого неопознанного контакта и исключение
заводов. Сборка и обе растровые проверки прошли.

- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-unknown-orbits/build.log`
- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-unknown-orbits/orbit-checks.log`

При изменении масштаба объединять орбиты с расхождением до двух экранных
пикселей. Сравнивать с уже нарисованной кривой, чтобы цепочка соседних
линий не объединила далёкие крайние линии. Круговые дуги сравнивать в
границах видимой области. Широкая диагональная полоса на втором снимке —
штриховка пылевого поля слоя Fields. Цвета заливки и штрихов совпали с
палитрой Dust в `EnvironmentFieldRenderer.cs`.

Сборка и семь целевых проверок прошли, включая смену масштаба, близкие
видимые дуги со смещёнными центрами и отрисовку полей.

- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-orbit-merge/build.log`
- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-orbit-merge/orbit-checks.log`

Пылевые поля рисовать как мягкие фиолетовые сгустки с общей палитрой
пошагового боя и плавным спадом яркости от центра. Положение рисунка зависит
от seed поля и привязано к его геометрии. Граница сохраняется для выбора,
а контур рисуется у выбранного поля. Область правки — `EnvironmentFieldRenderer.cs`
и палитра в `AnomalyFieldLayer.cs`.

Сборка и шесть целевых проверок прошли. Проверены фиолетовая палитра,
мягкость рисунка, привязка к миру, выбор поля и контрольные пиксели боя.
В тесте генерации полей ожидания радиусов приведены к текущему масштабу.

- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-purple-dust/build.log`
- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-purple-dust/dust-checks.log`

Подпись кластера на масштабе Cluster привязана к видимой станции, ближайшей
к центру кадра. Раньше подпись стояла в среднем положении всех станций
и могла оставаться над пустой областью. Если в кадре нет станции кластера,
подпись скрывается. Область правки — `ClusterMapPresentation.cs`.

Сборка в отдельной папке и шесть целевых проверок прошли. Растровый тест
подтвердил подпись у видимой станции и её отсутствие при пустом кадре.
Файлы обычной сборки заняты открытой игрой.

- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-cluster-caption/build.log`
- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-cluster-caption/cluster-checks.log`

Радиационное поле слоя Fields использует общую красную палитру тепловой
аномалии боя. Вокруг Солнца рисуются мягкое центральное свечение и сгустки
с плавным затуханием. Контур служит подсветкой выбранного поля. Область
правки — `EnvironmentFieldRenderer.cs` и палитра `AnomalyFieldLayer.cs`.

Сборка в отдельной папке и семь целевых проверок прошли. Проверены красная
палитра, затухание, привязка к миру, выбор поля и контрольные пиксели боя.

- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-red-radiation/build.log`
- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-red-radiation/radiation-checks.log`

Базы ИИ временно скрыты в представлении карты вместе с их орбитами,
подписями и следами. В радиации убраны интерполяция к прозрачному белому
и дополнительное изменение прозрачности между цветовыми ступенями.
Яркость задаёт красный градиент. Проверки покрывают цвет края и скрытие
планетарных и орбитальных баз.

Сборка в отдельной папке и восемь целевых проверок прошли. Тесты
проверяют обе разновидности баз ИИ, обычную станцию, цвет радиации на
чёрном фоне и переключатели карты.

- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-hide-ai-clean-radiation/build.log`
- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-hide-ai-clean-radiation/map-checks.log`

На уровне System дистрикт с экранным диаметром до 220 пикселей показывает
сводную метку и усреднённую орбиту. Сводная метка приближает дистрикт.
На ближнем обзоре видны станции и их орбиты. Выбранная станция и
навигационная цель сохраняют маркеры на дальнем обзоре.

Последняя проверка покрыла переход к дистрикту по сводной метке, замену
индивидуальных орбит средней линией и их возврат при приближении.
Запуск в окне для этой правки не проводился.

- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-district-spread/Client-build.log`
- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-district-spread/client-checks.log`

Дополнительные файлы проверки и реализации: `ClusterMapPresentation.cs`
и `MapDetailPolicyTests.cs`. Сборка и пять целевых проверок прошли, включая
растровую проверку всех пяти маркеров в кадре 1280 × 720. Проверка в окне
игры остаётся открытой.

- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-raster/build.log`
- `D:/DeepSpaceSaga/artifacts/validation/station-map-visibility/20261110-raster/raster.log`

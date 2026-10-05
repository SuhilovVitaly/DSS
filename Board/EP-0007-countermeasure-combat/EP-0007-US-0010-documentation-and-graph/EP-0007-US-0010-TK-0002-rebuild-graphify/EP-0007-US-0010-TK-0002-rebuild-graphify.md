---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0010-documentation-and-graph
ticket: EP-0007-US-0010-TK-0002-rebuild-graphify
title: "Перестроить Graphify по итоговому коду и документации"
stage: draft
layer: tooling
depends_on: ["EP-0007-US-0010-TK-0001-update-affected-documentation"]
files_touched: generated-artifact-set
scope_exception: "Graphify configuration and generated output only"
serves: [AC-0003, AC-0004]
created: 2026-10-04
revision: 1
---

# Перестроить Graphify по итоговому коду и документации

STATUS: DRAFT

## Why и Dependencies

Шаг35, заключительный тикет эпика; выполнять после [обновления документации](../EP-0007-US-0010-TK-0001-update-affected-documentation/EP-0007-US-0010-TK-0001-update-affected-documentation.md). Построить пригодный для следующих задач граф реальной реализации, включая обновлённые текстовые контракты.

## Code context и scope

На момент планирования существующий граф находится в src/graphify-out/: graph.json, graph.html, GRAPH_REPORT.md, manifest.json и служебные файлы. Наличие проверено; свежесть и здоровье не подтверждены. Корневой graphify-out не считать автоматически актуальным.

Разрешена запись только конфигурации исключений .graphifyignore/src/.graphifyignore, генерируемого набора src/graphify-out/** и документационного run report Documentation/06-Tooling/CountermeasureGraphify.md. Допустим временный staging вывода внутри репозитория; прежний рабочий граф сохранять до успешной проверки нового. Размер generated набора не ограничивается5файлами. Не менять production-код и не удалять чужие артефакты.

## Implementation steps

1. Прочитать актуальный C:/Users/sushi/.codex/skills/graphify/SKILL.md. Проверить установленный CLI/interpreter и его версию/help; не предполагать, что историческая команда подходит текущей версии.
2. Зафиксировать исходный commit плюс состояние изменённых файлов, время, tool version, фактическую команду/параметры, manifest или hashes входного корпуса. Одна дата генерации не доказывает свежесть.
3. Построить явный code/text corpus: исходникиsrc, релевантныеtests, актуальная Documentation и итоговыйEP-0007. Не сканировать весь D:/DeepSpaceSaga. Исключить игровые изображения, аудио/видео, бинарные assets, bin/obj, .git, caches и сами graphify-out. Для изображений исключить как минимум png/jpg/jpeg/gif/webp/bmp/tif/tiff; сверить detect: media count=0.
4. Выполнить полную перестройку с повторным извлечением итогового корпуса, обновить communities/report/HTML/manifest. Одного cluster-only или смены timestamp недостаточно. Если инструмент требует отдельные extraction roots для кода и документации, использовать описанное навыком объединение с нормализованными source paths; не потерять документационные узлы.
5. Защититься от пустого извлечения и случайного уменьшения корпуса. Shrink guard не обходить без выяснения причины; известные удаления отражать в manifest/run report. Не заменять рабочий граф неуспешным результатом.
6. Выполнить graph health diagnostics: отсутствующие/dangling endpoints, duplicates, collapse/self-loops; записать реальные числа и ограничения. Не называть граф чистым при warnings.
7. Проверить запросами/трассировками: operator → rating → launch; auto defense → intercept → RNG resolution; missile phase → reload; save schema → restore; snapshot → UI/journal. Проверить связи на соответствующие обновлённые документы, а не только совпадение названий.
8. Проверить graph.json парсинг, согласованность manifest/report/HTML и существование source paths. После успешной проверки разместить окончательный набор в src/graphify-out и записать evidence в run report.

## Validation / Definition of Done

- [ ] Corpus содержит финальный код и обновлённые документы EP-0007; media исключены.
- [ ] JSON непустой, основные новые сущности/пути присутствуют; нет ссылок на временную staging-директорию.
- [ ] HTML открывается, отчёт и граф относятся к одному запуску; counts и diagnostics записаны.
- [ ] Проверочные запросы и source evidence приведены в run report.
- [ ] Предыдущий рабочий граф не утрачен при ошибке; незакрытые warnings явно видны.
- [ ] Документы и production-код не изменились после snapshot входов; иначе обновить результат перед закрытием.
- [ ] Нет runtime изменений, внешней публикации или ложного утверждения о прохождении игровых тестов.

## Assumptions / Out of scope

Точная CLI-команда выбирается по установленной версии и навыку при исполнении, а не выдумывается сейчас. Граф используется для навигации; edge inference не заменяет проверку исходников. Тикет не включает загрузку в Neo4j/облачный сервис, анализ игровых изображений, обновление памяти Codex или commit/push.

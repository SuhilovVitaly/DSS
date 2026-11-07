---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0009-project-documentation
title: "Контракты карты и передача результата отражают фактическую поставку"
stage: approved
approval_kind: delegated-planning
implementation_status: not_started
dependencies: [EP-0018-US-0001-unified-station-geography, EP-0018-US-0002-stable-orbital-clusters, EP-0018-US-0003-preserved-starts-and-resources, EP-0018-US-0004-materialized-world-compatibility, EP-0018-US-0005-local-map-scale, EP-0018-US-0006-multiscale-map-readability, EP-0018-US-0007-map-navigation-and-estimates, EP-0018-US-0008-system-map-acceptance]
created: 2026-11-07T17:21:08Z
source_request: "отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами"
current_review: complete
revision: 1
---
# Контракты карты и передача результата отражают фактическую поставку

## Входное техническое задание
После функциональных проверок синхронизировать текущие описания мира, карты и сохранений, сохранив прежние отчёты как историю.

## Связь с техническим заданием
Часть [эпика](../Documentation.md) и его [контракта переделки](../ImplementationContract.md). Новые численные настройки отделены от текущего поведения. Основание процесса: пользователь прямо поручил создать весь состав без дополнительных подтверждений.

## User story
Как разработчик, я хочу найти действующие правила расстояний, масштаба и сохранений в документации. Мне должен быть понятен порядок реализации и фактический остаток приёмки. Старые отчёты остаются доступными и не выдаются за проверку новой карты.

## Grounding
DB/Documentation/02-FirstRelease/Mechanics/SolarSystemMapConcept.md и04-Engineering/TradingAndSolarSystem.md описывают gen1 content. SaveFormat.md фиксирует Save20 и отсутствие повторной генерации. Старые эпики имеют датированные отчёты и planning stages.

## Acceptance criteria
- AC-01: Текущие контракты описывают фактически поставленные v2 distances/reference/LOD/compatibility и ограничения ETA.
- AC-02: Board хранит реальный execution ledger, coverage всех criteria и явный residual automated/native/human/FPS status.
- AC-03: Навигация документации и датированные supersession notices EP-0002/3/4 ведут к текущему контракту, не стирая историю.

## Non-goals
Реальный N-body, изменение единиц/часов, новая боевая карта, скрытая перебалансировка экономики и перегенерация старых сохранений не входят.

## Dependencies
- EP-0018-US-0001-unified-station-geography
- EP-0018-US-0002-stable-orbital-clusters
- EP-0018-US-0003-preserved-starts-and-resources
- EP-0018-US-0004-materialized-world-compatibility
- EP-0018-US-0005-local-map-scale
- EP-0018-US-0006-multiscale-map-readability
- EP-0018-US-0007-map-navigation-and-estimates
- EP-0018-US-0008-system-map-acceptance
Точные ticket dependencies в таблице ниже; итоговая публикация новой конфигурации запрещена до compatibility gate US-0004.

## Invariants
Engine владеет географией и RNG; Client только проецирует snapshot. 1 world unit = 100 м; календарь/motion = 300 (EngineRequirements, раздел «Время и единицы»). Seed/version/scenario/config воспроизводят результат. Подробнее: ImplementationContract.md, I-01–I-09.

## Completion evidence
Документированный impact inventory file->change->check, проверенные local links/anchors, Board DAG/coverage, реальные report paths и diff.
Планируемые проверки не являются выполненной приёмкой. Automated, native и human/product acceptance фиксируются отдельно.

## Approved ticket map
План и тикеты приняты автоматическим workflow по прямому поручению пользователя; индивидуальных человеческих approvals не было.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [EP-0018-US-0009-TK-0001-current-map-contracts](EP-0018-US-0009-TK-0001-current-map-contracts/EP-0018-US-0009-TK-0001-current-map-contracts.md) | Действующие правила географии, карты и сохранений | tooling | EP-0018-US-0008-TK-0003-native-map-evidence | AC-01 |
| [EP-0018-US-0009-TK-0002-board-handoff](EP-0018-US-0009-TK-0002-board-handoff/EP-0018-US-0009-TK-0002-board-handoff.md) | Реестр исполнения и передача эпика без ложной приёмки | tooling | EP-0018-US-0009-TK-0001-current-map-contracts | AC-02 |
| [EP-0018-US-0009-TK-0003-documentation-navigation](EP-0018-US-0009-TK-0003-documentation-navigation/EP-0018-US-0009-TK-0003-documentation-navigation.md) | Навигация и связь прежних эпиков с новой картой | tooling | EP-0018-US-0009-TK-0002-board-handoff | AC-03 |

## Decisions / Review log
2026-11-07T17:21:08Z — время записи, не время исходного сообщения. Точное поручение: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами». Результат: делегированное планирование всего состава; execution не разрешён этим документом. Общие D/A/G находятся в Documentation.md.

## Gaps and backlog
Блокирующих вопросов планирования нет. Численные assumptions проверяются по критериям истории; изменение пользовательского контракта требует явного пересмотра Board.

## Scope исполнения — 2026-11-07

Прямое поручение исполнить эпик по разделу6 EpicExecutionPrompt.md расширяет
первоначальные allowlists документационных тикетов. История выполняется последней.
В TK-0001 входят все релевантные текущие Markdown-контракты, включая screen/mechanics
карты, settings, architecture/config/scenario/save guides, найденные итоговым diff.
TK-0002 ведёт DocumentationImpact.md: полный inventory по всему репозиторию
(document → change → required update → observed check), включая README, документы
возле src/tests/tools/content/assets и значимые комментарии. TK-0003 актуализирует
найденные вне Documentation руководства/README и navigation, затем обновляет
offline граф через tools/graphify/rebuild.py; output/semantic/cache только в
абсолютном workspace artifacts/validation/ep-0018-map-geography-and-scale/<run>/.
Новые scope paths определяются inventory перед изменением и записываются в карточке.
Граф строится после финального кода/текстов; fresh hashes/coverage/limitations
проверяются, исторический src/graphify-out не заменяется. Пять файлов исходного
planning не ограничивают обязательный inventory/документацию всего проекта.
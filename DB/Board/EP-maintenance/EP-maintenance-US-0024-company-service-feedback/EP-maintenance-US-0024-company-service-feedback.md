---
epic: EP-maintenance
story: EP-maintenance-US-0024-company-service-feedback
title: Актуальное снабжение и видимое оснащение
stage: approved
implementation_status: in-progress
dependencies: []
current_review: complete
revision: 1
---
# Актуальное снабжение и видимое оснащение

## Входное техническое задание
Пользователь сообщил две P2-регрессии EP-0033 и поручил исправить, закоммитить и опубликовать. Связанные контракты: EP-0033-US-0002-TK-0004 и EP-0033-US-0003-TK-0004.

## User story
После станционных услуг игрок видит текущий трюм и магазины, состав предложения до оплаты и фактически установленное оснащение выбранного дрона после оплаты.

## Acceptance criteria
- AC-01: Ответ пополнения ловушек не скрывает дальнейшие изменения снабжения.
- AC-02: Верфь показывает все Items предложения и установленное оружие/броню выбранного дрона; результат меняется по authoritative carrier snapshot.

## Approved ticket map
| Ticket | Layer | Dependencies | Serves |
|---|---|---|---|
| TK-0001-live-supply | client | none | AC-01 |
| TK-0002-visible-equipment | client | TK-0001 | AC-02 |

## Non-goals
Правила Engine, цены, сохранения и полный демо-плейтест не меняются.

## Gaps and backlog
Прежние внешние/product gates EP-0033 остаются открытыми. Native-приёмка не следует из headless теста.

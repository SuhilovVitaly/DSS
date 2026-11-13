---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0001-skin-pack
title: Пакет скина «Tabletop» и режим поля
stage: draft
dependencies: []
created: 2026-11-12T13:00:00Z
source_request: "Добавить в клиент пакет скина `tabletop` из материалов эпика и режим поля, который включается метрикой скина; ввести роли листов спрайтов."
current_review: epic
revision: 1
---

# Пакет скина «Tabletop» и режим поля

## Входное техническое задание

D-02, D-07, A-01, A-02. Пакеты лежат в `src/DeepSpaceSaga.Client/Skins/<id>` и копируются правилами `DeepSpaceSaga.Client.csproj:43-61`; загрузчик — `TacticalBattleSkinCatalog.Load`, манифест — `TacticalBattleSkin.Parse` (`TacticalBattleSkin.cs:72-200`), метрики — `TacticalBattleSkin.Metric` (`:48`). Готовый пакет — `DB/Board/EP-0024-battle-field-redesign/Materials/Assets/skin` (манифест и 18 PNG: фон, 13 листов кораблей и торпед, 4 строки эффектов; D-10).

## User story

Как игрок, я хочу выбрать в настройках скин «Tabletop», чтобы играть на поле нового вида, не теряя прежних скинов.

## Dependencies

— (база: `main` @ d7895f73, после EP-0023)

## Completion evidence

- Пакет `tabletop` есть в выходной сборке и в списке скинов настроек.
- Код может спросить «включён ли режим tabletop» и получить лист спрайтов по модели и стороне.

## Acceptance criteria

- **US1-AC1.** После сборки `Skins/tabletop/skin.json`, `assets/field.png`, 13 листов `assets/units/pxd-*.png` (корабли и торпеды) 4 строки `assets/fx/*.png` (взрывы, пшик, шлейф) и 6 стальных градиентов `assets/ui/*.png` (панели, D-11) есть в выходной папке; `TacticalBattleSkinCatalog.Options` содержит `("tabletop", "Tabletop")`; загрузка проходит без предупреждений; повреждённый пакет откатывается на Classic, как Xenon Star.
- **US1-AC2.** `TacticalBattleSkinRenderScope` даёт признак режима `tabletop` (метрика `field.style.tabletop` = 1) лист спрайта по `(модель, сторона)` с метриками `columns`/`frames` (модели кораблей и `torpedo`) и строку эффекта по имени с метриками `frames`/`frame-ms` (`field.fx.*`); в Classic, Contrast и Xenon Star признак ложен и листов нет.
- **US1-AC3.** Контракт скинов (`DB/Documentation/04-Engineering/TacticalBattleSkins.md`) описывает роли `field.style`, `field.unit.sprite.<модель>.<сторона>` и `field.fx.<имя>`; `SkinManifestTests` проверяет их разбор и лимиты ресурсов.

## Non-goals

Отрисовка поля в режиме (US-0002…US-0008). Панели, их роли и текстуры (D-08).

## Invariants

Схема манифеста остаётся версии 1. Classic, Contrast и Xenon Star не меняются.

## Решения и согласование

Решения пользователя — [D-01–D-11](../Documentation.md#решения-пользователя), допущения — [A-01–A-19](../Documentation.md#технические-допущения). Эпик утверждён пользователем 2026-11-12 ([review log](../Documentation.md#review-log)).

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0024-US-0001-TK-0001-pack-files](EP-0024-US-0001-TK-0001-pack-files/EP-0024-US-0001-TK-0001-pack-files.md) | Пакет `tabletop` в клиенте и в сборке | client-data | — | US1-AC1 |
| [EP-0024-US-0001-TK-0002-tabletop-mode](EP-0024-US-0001-TK-0002-tabletop-mode/EP-0024-US-0001-TK-0002-tabletop-mode.md) | Признак режима и доступ к листам спрайтов | client-ui | `EP-0024-US-0001-TK-0001-pack-files` | US1-AC2, US1-AC3 |

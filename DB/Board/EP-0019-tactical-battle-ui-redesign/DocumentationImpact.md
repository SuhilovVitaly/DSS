# EP-0019 — инвентаризация итогового diff

| Документ/область | Влияние и выполненное обновление | Проверка |
|---|---|---|
| Documentation/02-FirstRelease/Screens/TacticalBattle.md | Полноэкранная карта, карточки/панель фазы, mouse/keys, New battle после итога | Сверено по production и native route |
| Documentation/04-Engineering/TacticalBattle.md | H/720, file typography, портреты, Mind placeholder, единый admitted frame, seven-scene/reference/regression методика | Сверено с исходниками и тестами |
| Documentation/04-Engineering/TacticalBattleSkins.md | Текущие активные ui.* роли и поддерживаемые свойства, отключённые legacy поверхности | Сверено с BattleDesign/renderer |
| Documentation/02-FirstRelease/Screens/ScreenCatalog.md, Settings.md | Новая навигация экрана; uiScale не масштабирует бой | Сверено с CaptureFrame |
| Documentation/01-Requirements/FirstReleaseRequirements.md | Журнал сохраняется в данных, активный UI использует cards/phase | Ownership/Engine контракт сохранён |
| Documentation/00-Process/CLAUDE.md | Название текущих presentation-поверхностей | Архитектура слоёв не меняется |
| Documentation/06-Tooling/GameSessionScreenUI.md | Шрифт, client portrait data и H/720 | Сверено с output/data |
| src/DeepSpaceSaga.Client/Skins/README.md | Новые роли, legacy controls отключены | Ссылка на актуальный контракт |
| tools/DeepSpaceSaga.Performance/README.md | Новый native workflow и отдельный монитор через task Settings; фактическая позиция | Native secondary PASS |
| Documentation/06-Tooling/Graphify.md, GraphifyRebuild.md; tools/graphify/rebuild.py | EP-0019 в корпусе, renderer owner обновлён, ledger exclusions | Fresh offline rebuild/verification |
| Board EP-0019 | 7 историй/21 тикет, статусы, AC/evidence/SHA, восстановленные inputs, snapshots decision и сохранённая начальная история | Git/links/status audit |
| Documentation README/EngineRequirements/SaveFormat/прочие механики и экраны | Проверены на влияние; правила, schema20, ownership и мир не изменены, изменения не требуются | Engine/Contracts/Motion diff пуст |
| README корня | Отсутствует; новый дубликат руководства не создаётся | Инвентаризация root |
| Датированные Board EP-0015/16/17/18 | Исторические результаты не переписаны. EP-0019 явно заменяет active UI, но не старые механические результаты | Historical accuracy |

Локальные Markdown links и Documentation Markdown-only проверяются в D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/links.json. Текущие Graphify цифры и warnings находятся в ExecutionStatus.md и artifacts graph/verification.json; навигационный граф не является доказательством native/physical acceptance.

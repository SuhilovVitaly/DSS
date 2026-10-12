---
epic: EP-0009-module-combat
story: EP-0009-US-0012-project-documentation
title: "Документация (обязательная завершающая история)"
stage: draft
created: 2026-10-12
validation_status: documentation-updated-links-pending-native-open
---

# Документация (обязательная завершающая история)

**Acceptance criteria**
- **US12-AC1:** Обновлены релевантные документы проекта:
  - `Documentation/00-Process/CLAUDE.md` (данные `combatRules`, `fireModes`);
  - `Documentation/04-Engineering/BasicTorpedoCombat.md`, `CountermeasureCombat.md` (или новый `ModuleCombat.md`);
  - `Documentation/02-FirstRelease/Mechanics`;
  - `Documentation/01-Requirements` — если там описаны формула или HP;
  - `Board/EP-0008-…/ImplementationStatus.md`: раздел «Superseded by EP-0009» со ссылкой на §2.1.
- **US12-AC2:** Ссылки проверены; Graphify rebuild SKIPPED по прямому указанию пользователя2026-10-12, граф остаётся прежним.

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 37 | [EP-0009-US-0012-TK-0001-update-project-documents](EP-0009-US-0012-TK-0001-update-project-documents/EP-0009-US-0012-TK-0001-update-project-documents.md) | documentation | US12-AC1 |
| 38 | [EP-0009-US-0012-TK-0002-refresh-graph-and-links](EP-0009-US-0012-TK-0002-refresh-graph-and-links/EP-0009-US-0012-TK-0002-refresh-graph-and-links.md) | tooling | US12-AC2 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. Planning text is not PASS/APPROVED. Actual results go into ticket cards and ImplementationStatus; deferred native/human acceptance stays OPEN. Graph rebuilding is SKIPPED by user instruction.

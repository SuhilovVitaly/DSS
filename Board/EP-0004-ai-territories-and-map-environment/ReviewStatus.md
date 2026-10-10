# EP-0004: сверка поставки и остатка — 2026-10-10

[Общий review и определения F/G](../../Documentation/04-Engineering/EpicReview20261010/README.md).

Поставка `76f500a` интегрирована в основную рабочую копию без commit/push. 25/26 выполнены, US-0008/TK-0003 остаётся OPEN по FPS.

Всего 26 тикетов. Все строки сопоставлены с Board-индексом; глубокое ревью выполнено для критических runtime/save/tooling путей, а не каждого AC каждой строки. Общий остаток: G05 (human playthrough NOT RUN; scripted native проверен отдельно), новые F/G ниже. Исторические execution notes сохраняются.

| Тикет | Реализация | Новый остаток / граница проверки |
|---|---|---|
| [EP-0004-US-0001-TK-0001-ai-base-contract](EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0001-ai-base-contract/EP-0004-US-0001-TK-0001-ai-base-contract.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0001-TK-0002-seeded-ai-bases](EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0002-seeded-ai-bases/EP-0004-US-0001-TK-0002-seeded-ai-bases.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0001-TK-0003-authoritative-hostile-access](EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0003-authoritative-hostile-access/EP-0004-US-0001-TK-0003-authoritative-hostile-access.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0001-TK-0004-ai-base-content](EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0004-ai-base-content/EP-0004-US-0001-TK-0004-ai-base-content.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0001-TK-0005-ai-base-presentation](EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0005-ai-base-presentation/EP-0004-US-0001-TK-0005-ai-base-presentation.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0002-TK-0001-territory-radii-contract](EP-0004-US-0002-moving-ai-territories/EP-0004-US-0002-TK-0001-territory-radii-contract/EP-0004-US-0002-TK-0001-territory-radii-contract.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0002-TK-0002-moving-territory-data](EP-0004-US-0002-moving-ai-territories/EP-0004-US-0002-TK-0002-moving-territory-data/EP-0004-US-0002-TK-0002-moving-territory-data.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0002-TK-0003-territory-rendering](EP-0004-US-0002-moving-ai-territories/EP-0004-US-0002-TK-0003-territory-rendering/EP-0004-US-0002-TK-0003-territory-rendering.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0003-TK-0001-temporal-placement-validation](EP-0004-US-0003-trade-compatible-ai-placement/EP-0004-US-0003-TK-0001-temporal-placement-validation/EP-0004-US-0003-TK-0001-temporal-placement-validation.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0003-TK-0002-placement-evidence-report](EP-0004-US-0003-trade-compatible-ai-placement/EP-0004-US-0003-TK-0002-placement-evidence-report/EP-0004-US-0003-TK-0002-placement-evidence-report.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0004-TK-0001-environment-field-contract](EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0001-environment-field-contract/EP-0004-US-0004-TK-0001-environment-field-contract.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0004-TK-0002-seeded-environment-fields](EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0002-seeded-environment-fields/EP-0004-US-0004-TK-0002-seeded-environment-fields.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0004-TK-0003-environment-field-content](EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0003-environment-field-content/EP-0004-US-0004-TK-0003-environment-field-content.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0004-TK-0004-environment-field-rendering](EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0004-environment-field-rendering/EP-0004-US-0004-TK-0004-environment-field-rendering.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0005-TK-0001-poi-contract](EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0001-poi-contract/EP-0004-US-0005-TK-0001-poi-contract.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0005-TK-0002-seeded-abandoned-objects](EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0002-seeded-abandoned-objects/EP-0004-US-0005-TK-0002-seeded-abandoned-objects.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0005-TK-0003-abandoned-object-content](EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0003-abandoned-object-content/EP-0004-US-0005-TK-0003-abandoned-object-content.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0005-TK-0004-poi-map-selection](EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0004-poi-map-selection/EP-0004-US-0005-TK-0004-poi-map-selection.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0006-TK-0001-map-layer-controls](EP-0004-US-0006-readable-map-layers/EP-0004-US-0006-TK-0001-map-layer-controls/EP-0004-US-0006-TK-0001-map-layer-controls.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0007-TK-0001-map-environment-save](EP-0004-US-0007-resume-territories-and-fields/EP-0004-US-0007-TK-0001-map-environment-save/EP-0004-US-0007-TK-0001-map-environment-save.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0007-TK-0002-full-map-local-load](EP-0004-US-0007-resume-territories-and-fields/EP-0004-US-0007-TK-0002-full-map-local-load/EP-0004-US-0007-TK-0002-full-map-local-load.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0008-TK-0001-full-map-correctness-corpus](EP-0004-US-0008-complete-map-evidence/EP-0004-US-0008-TK-0001-full-map-correctness-corpus/EP-0004-US-0008-TK-0001-full-map-correctness-corpus.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0008-TK-0002-full-map-performance-report](EP-0004-US-0008-complete-map-evidence/EP-0004-US-0008-TK-0002-full-map-performance-report/EP-0004-US-0008-TK-0002-full-map-performance-report.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0008-TK-0003-full-map-render-evidence](EP-0004-US-0008-complete-map-evidence/EP-0004-US-0008-TK-0003-full-map-render-evidence/EP-0004-US-0008-TK-0003-full-map-render-evidence.md) | Интегрировано локально | G01, G02 |
| [EP-0004-US-0009-TK-0001-sync-project-docs](EP-0004-US-0009-project-documentation/EP-0004-US-0009-TK-0001-sync-project-docs/EP-0004-US-0009-TK-0001-sync-project-docs.md) | Интегрировано локально | G01 закрыт локально |
| [EP-0004-US-0009-TK-0002-final-evidence-and-graph](EP-0004-US-0009-project-documentation/EP-0004-US-0009-TK-0002-final-evidence-and-graph/EP-0004-US-0009-TK-0002-final-evidence-and-graph.md) | Интегрировано локально | G01 закрыт локально |

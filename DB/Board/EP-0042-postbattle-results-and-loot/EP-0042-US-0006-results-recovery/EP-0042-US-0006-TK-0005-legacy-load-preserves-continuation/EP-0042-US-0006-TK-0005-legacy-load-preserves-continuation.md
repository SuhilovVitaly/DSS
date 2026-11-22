---
epic: EP-0042-postbattle-results-and-loot
story: EP-0042-US-0006-results-recovery
ticket: EP-0042-US-0006-TK-0005-legacy-load-preserves-continuation
title: Восстановление старого слота без перезаписи continuation
stage: implemented
layer: local-client
depends_on: [EP-0042-US-0006-TK-0003-load-routing]
serves: [AC-02, AC-03]
---

# Восстановление старого слота без перезаписи continuation

Legacy открытый ledger уже подтверждён в исходном файле. Его адаптация при Load открывает модаль в памяти. Открытие обычных результатов и дальнейшие операции проходят прежнюю checkpoint-запись. Проверка загрузки обоих форматов подтверждает отсутствие записи continuation при входе в восстановленную модаль.

## Исполнение

- При загрузке legacy открытая стадия адаптируется в памяти. Обычное открытие требует прежней записи, а Engine принимает признак адаптации только для совместимого открытого legacy ledger.
- Сборка Client Tests, scoped whitespace format и `git diff --check` прошли.

# EP-0020 — исполнение

Ветка: `codex/ep-0020-tactical-battle-mechanics`; remote: `origin` (`https://github.com/SuhilovVitaly/DSS`). База: `601ab778e710c5663de93bedf311f0f613405ef3`, опубликованная ветка EP-0019. Исходное дерево чистое; дополнительных worktree нет.

EP-0019 stage done, US6-AC2 техническая приёмка выполнена; открытые human/art/physical-input/other-OS gates не объявляются закрытыми. Запрос пользователя 2026-11-08 разрешает исполнение всех draft-карточек и публикацию по EpicExecutionPrompt.

План: US-0001 → US-0002 → US-0003 → US-0004 → US-0005 → US-0006 → US-0007 → US-0008 → US-0009 → US-0010 → US-0011 → US-0012 → US-0013. Документационная история расширена до §6 промта и выполняется последней. Индекс восстановлен из 44 канонических карточек вместо пустых строк.

## Публикация тикетов

| Тикет | SHA | Push |
|---|---|---|
| EP-0020-US-0001-TK-0001 | 024e5b72f1d6f3d58c9765fbc91442159c47d4b9 | origin проверен |

| EP-0020-US-0001-TK-0002 | e6f9c4a23351d466b41f29a26980d6244e17f766 | origin проверен | Служебная подготовка 9aad733e не считается выполнением тикета.

## Проверки

Git status/branch/remote и удалённая база проверены. Сборки, тесты, калибровки и native-проверки EP-0020 пока NOT RUN.

US-0001: 118/118 content, 35/35 focused Engine, 23/23 Contracts; 1000×5 exact direct/explicit series parity PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/. Остальные истории не начаты.

EP-0020-US-0002-TK-0001 → 80a74fe6b5034ded1c598b255021a8a78cfd0f78 → origin проверен. Contracts 25/25 PASS.

EP-0020-US-0002-TK-0002 → 58c531de740eecdba06e595cf707662f66c6c83e → origin проверен. Motion 6/6 PASS, включая V4 trace.

EP-0020-US-0002-TK-0003 → 8393a0571a16496e132a722320eb183838f52780 → origin проверен. Salvo focused 44/44 PASS.

EP-0020-US-0002-TK-0004 → 8413b1b2c8adc6a5fc818f7890eb45c428442d03 → origin проверен. Session/Connection/Journal 137/137 PASS. TK-0005 в процессе: Engine main2188/2188, Golden91/91, series1000×5 PASS; 630 geography corpus ещё выполняются, итоговый audit pending. Предварительный UI baseline после M1 Engine:26/29 PASS,3 ожидаемых FAIL только Outcome/data (разрешённое изменение D-10 и AI уклонения); UI ещё не менялся.

US-0002 завершена: Engine2818/2818 уникальных IDs PASS по36TRX; geography630/630 PASS; series1000×5 PASS. Итоговый audit: D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us2-tk5/coverage-audit.json. Начальные timeout/class batches не включены в успешный счёт. TK-0005 готов к commit/push.

EP-0020-US-0002-TK-0005 → 57624cffd59d2392626d2037bf817b5b706bb33c → origin проверен. EP-0020-US-0003-TK-0001 (инструмент) → 30aa693fd1ff80ec3ef8f8268ec7cc910baa3aa7 → origin проверен. Калибровка31/31 PASS,155000 конфигураций,310000 исполнений с повторами; R20=0.680413, ближайший штраф32(R=0.494949), production20. Default filter/scoped format PASS.

Калибровочный отчёт EP-0020-US-0003-TK-0001 → 9b9d2a326026162bec21feb8bd5144a3fc867a9b → origin проверен. UI US4/TK1:28/28 focused PASS, scoped format PASS.

EP-0020-US-0004-TK-0001 → 953cc4738c77ec84e389311a5739f9bfd8da693e → origin проверен. UI panel13/13 PASS с guards до первого Draw, пробами UiSpec и RU/EN; всё в карточке TK-0002.

EP-0020-US-0004-TK-0002 → 8644df07914a5053c1bb4bb578cea1cc7aa0389f → origin проверен. Results reveal/zero-shot21/21 PASS.

EP-0020-US-0004-TK-0003 → 80664d446e78e9bf56e15e5590f339b96179f904 → origin проверен. US4 завершена: Client2182/2182PASS, все29Ep0019PASS, masked6/6PASS; TK4готов к публикации.

EP-0020-US-0004-TK-0004 → db4e0ea3670cd96c5f4d2bde2b80e6959d45d087 → origin проверен. US5/TK1 Content131/131PASS, export30PASS; готов к публикации.

EP-0020-US-0005-TK-0001 → c27a18c3686f2fcb73df45709ae0a54c3bd0a8b0 → origin проверен. TK-0002 в процессе: main2204/2204PASS; Motion7/7,Contracts26/26; before/after5000 exact seriesPASS. Corpus630 ещё выполняется; тикет не объявлен завершённым. Evidence us5-tk2/series-comparison.json.

US5 завершена: Engine2834PASS(36TRX,no duplicate IDs),Motion7,Contracts26,seriesbefore/after5000PASS,legacy hashes5PASS. TK2 готов к публикации; переход к US6 после origin SHA.

EP-0020-US-0005-TK-0002 → 7895e0b910377f88b3d73b810a70a873abe9fe46 → origin проверен. US6/TK1 Contracts27PASS,готов к публикации.

EP-0020-US-0006-TK-0001 → 905b0cdef5cc70c5caf51046cab8676224ea79a3 → origin проверен. TK2Motion12PASS,готов к публикации.

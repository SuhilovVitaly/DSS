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

EP-0020-US-0006-TK-0002 → a57b0b7112a34a979293bac2903f5a45b60231d0 → origin проверен. TK3focused55,Golden+state94PASS; готов к публикации.

EP-0020-US-0006-TK-0003 → d5892a727d0df6bbd734ea2f6843db6dd51a4ead → origin проверен. TK4Session142/Contracts27PASS,готов к публикации.

EP-0020-US-0006-TK-0004 → bc3ba57f2c03a58bd700451838d6e58c9ade6c43 → origin проверен. TK5main2220PASS, series5000direct/explicitPASS,report5000PASS; corpus630 выполняется. Полная таблица эффекта ловушки записана в карточку/review log.

US6 завершена: Engine2850/2850 uniquePASS,series5000exactPASS,report5000PASS. TK5готов к публикации; далее US7 UIловушек.

EP-0020-US-0006-TK-0005 → 180c896d82f8f781173322ccb89c7fe7885889cd → origin проверен. US7/TK1UI28PASS,готов к публикации.

EP-0020-US-0007-TK-0001 → e3e213278a927321ae44347a5b71e12ef9299231 → origin проверен. TK2Panel18/Engine94/Contracts27PASS,готов к публикации.

EP-0020-US-0007-TK-0002 → ee7e1013ca289413558356ac849673dd67460236 → origin проверен. TK3UI31/Engine44/Contracts27PASS,готов к публикации.

EP-0020-US-0007-TK-0003 → 82314c8f4925efecc328f74ab41035917592f93e → origin проверен. US7завершена: Client2206PASS/29Ep0019PASS,masked7PASS; TK4готов к публикации,далееUS8.

EP-0020-US-0007-TK-0004 → 773da5407093b98d5b8496da9cf6e65cf8fdbfb8 → origin проверен. US8/TK1Content174PASS,export30PASS,готов к публикации.

EP-0020-US-0008-TK-0001 → 3d86c03f8df36b9f2ad84085c7a92f5656248007 → origin проверен. TK2Motion20PASS,готов к публикации.

EP-0020-US-0008-TK-0002 → f684ff1a3dc009937246f29aeea12ebd011c193e → origin проверен. US8 завершена: TK3Engine103/Contracts28/series5000exactPASS, готов к публикации.

EP-0020-US-0008-TK-0003 → aa4fc46d1bf6e1f50dbf768e25cbfa08424cdaad → origin проверен. US9/TK1Contracts30PASS,готов к публикации.

EP-0020-US-0009-TK-0001 → be5d12e456ba0b310c505ff9830f0dc2aae87257 → origin проверен. TK2Engine47PASS,готов к публикации.

EP-0020-US-0009-TK-0002 → 1c697ccd527a9076598a831aed19749cb6b60f8a → origin проверен. TK3Engine45PASS,готов к публикации.

EP-0020-US-0009-TK-0003 → 62f296bc6aa9dbbe78c18bfd8ee23395e2c80590 → origin проверен. TK4Engine146PASS,готов к публикации.

EP-0020-US-0009-TK-0004 → 82c0b1450c67ea358a1cdcba5cb6f05c2ed637eb → origin проверен. US9завершена:Engine2924uniquePASS,series5000exactPASS,report5000PASS,Contracts197PASS; Motion1282PASS/1knownUIguardFAIL. TK5готов к публикации.

EP-0020-US-0009-TK-0005 → d4c3d68455a5592eb6a86e34d7ef8c583f982f1e → origin проверен. US10/TK1Client5PASS,32pixels/formula/fullraster exactPASS,готов к публикации.

EP-0020-US-0010-TK-0001 → cee0349de9c1a39767ca7c33b287bde8ccb1f9c3 → origin проверен. TK2Client31PASS,готов к публикации; M3whole-viewbaseline остаётся TK4.

EP-0020-US-0010-TK-0002 → 0616d4383cad76005f05198fb2b43922617d1265 → origin проверен. TK3Client28PASS,готов к публикации; далее M3whole-viewregression TK4.

EP-0020-US-0010-TK-0003 → a87bea55c16d845c4f4c5bb68cf37e53e56d0fd6 → origin проверен. US10завершена:Client2230uniquePASS,29Ep0019PASS,12strictsnapshotdifferent0,готов к публикации TK4. ДалееUS11слоты.

EP-0020-US-0010-TK-0004 → 0dc878c33ca0311f58fc5937b5ede81366f54857 → origin проверен. US11/TK1Contracts32PASS,готов к публикации.

EP-0020-US-0011-TK-0001 → 5ab552dd80de1b5701c42e033f74e5347c8f7533 → origin проверен. US11 завершена: Slots24/Engine128/Client34PASS,5000series exactPASS. TK2 готов к публикации.

EP-0020-US-0011-TK-0002 → 14f1c2f7e5e33db687a351ab9fbe63ca1df9050f → origin проверен. US12/TK1 model13PASS, готов к публикации.

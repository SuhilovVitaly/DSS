---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0002-client-rendering
ticket: EP-0015-US-0002-TK-0002-bundled-fonts
title: "Шрифты в комплекте клиента"
stage: draft
execution_status: implemented-windows-validated
layer: client
depends_on: [EP-0015-US-0002-TK-0001-skiasharp-linux-native]
serves: [AC-02]
created: 2026-10-25
---

# Шрифты в комплекте клиента

История: [EP-0015-US-0002-client-rendering](../EP-0015-US-0002-client-rendering.md) · Эпик: [EP-0015](../../Documentation.md)

## Зачем

Код запрашивает системные шрифты: `SKTypeface.FromFamilyName("Segoe UI")` (2 места), `"Consolas"` (3), `"Verdana"` (4). На Ubuntu их нет, Skia молча подставляет шрифт по умолчанию, и раскладка плывёт.

## Шаги

1. Взять свободные шрифты с лицензией OFL: **Inter** (вместо Segoe UI, Regular и Bold), **JetBrains Mono** (вместо Consolas), **Noto Sans** (вместо Verdana, Regular, Bold, Italic). Положить в `src/DeepSpaceSaga.Client/UI/Fonts/` вместе с файлами лицензий; правило копирования `UI\Fonts\**\*.ttf;*.otf;*.txt` уже есть в `.csproj`.
2. Добавить единый класс `UiFonts` (рядом с `XenonStyle.cs`): лениво загружает `SKTypeface.FromFile` по роли (`Ui`, `UiBold`, `Mono`, `Body`, `BodyItalic`, `BodyWeight(int)`), кэширует, при отсутствии файла пишет в лог и падает назад на `SKTypeface.Default`.
3. Заменить все вызовы `FromFamilyName` в файлах: `ImageButton.cs`, `MenuStyle.cs`, `XenonStyle.cs`, `CommandsPanel.cs`, `DebugTuningPanel.cs`, `GameSessionScreen.cs`, `ObjectLabelRenderer.cs`, `TradePainter.cs`.
4. `humaroid.regular.otf` уже грузится из файла — не трогать.

## Зависимости

[EP-0015-US-0002-TK-0001-skiasharp-linux-native](../EP-0015-US-0002-TK-0001-skiasharp-linux-native/EP-0015-US-0002-TK-0001-skiasharp-linux-native.md)

## Definition of Done

- `git grep FromFamilyName -- src` не находит вызовов.
- На Windows и Ubuntu интерфейс рисуется одними и теми же шрифтами.
- Удаление файла шрифта не роняет клиент: в логе есть предупреждение, текст рисуется шрифтом по умолчанию.
- Проверено на Windows и Ubuntu; на Windows поведение не ухудшилось.

## Scope update and Code context 2026-10-26

Разрешены единыйUI/Controls/UiFonts.cs, все7 реальныхFromFamilyName consumers (MenuStyle, XenonStyle, TradePainter, GameSessionScreen, ObjectLabelRenderer, CommandsPanel, DebugTuningPanel), tests/DeepSpaceSaga.Client.Tests/UiFontsTests.cs и UI/Fonts assets/licenses/provenance. ImageButton уже используетMenuStyle и прямогоfamily query не имеет; менятьбезнеобходимости не нужно. Историческийallowlist расширен из-заединогоownership: CommandsPanel/DebugTuningPanel должны перестатьDispose shared cachedTypeface; локальныеSKFont/paint по-прежнемуосвобождаются.

InterRegular/Bold, JetBrainsMonoRegular, NotoSansRegular/Bold/Italic — OFL изофициальныхrepositories слицензиями иsource hashes. Если upstreamдаётvariableTTF, допустимдетерминированныйstaticinstance черезfontTools; это фиксируетсявprovenance, исходныйHumaroid не меняется. BodyWeight выбирает ближайшую изпоставленныхRegular/Bold ролей, SemiBold→Bold какпредыдущийfamily fallback. Ниодногоsystemfamilyquery вproductionC# неостаётся. Cachedfaces принадлежатprocess, consumers неDispose их. Проверки: fontglyphRU/EN/bar cells/markers, distinctroles/cache, fallbackmissing/corrupt сwarning, panel-lifecycle, targeted+fullClient regression, output copy иscopedformat.

## Glyph correction

Existing ModulePanelHealthTests reproduced missingU+2666 inupstreamJetBrainsMono. AddedOFL-derivedDSSMono-Regular.ttf (familyDSS Mono) withU+2666 cmap alias toexistingU+25C6 solid-diamondglyph. Monospacemetrics andotherglyphs unchanged; sourceJetBrainsMonoRegular.ttf andlicense retained. UiFonts.Mono usesDSS Mono. This is animplementationassumption, notuserapproval; existingcritical-markerassertion remainsunchanged. FONT-SOURCES.txt describes derivation andhash.

## Execution result 2026-10-26

UiFonts lazy cache и7 productionconsumersпереведенынаfile-backedroles. ВsrcC# нетFromFamilyName. ПоставленыInterRegular/Bold,NotoSansRegular/Bold/Italic, исходныйJetBrainsMonoRegular ипроизводныйDSSMono сcriticaldiamond;3OFLлицензии иFONT-SOURCES.txt сURL/axes/hashes. Fonts/лицензиикопируютсясуществующимcsprojrule, Humaroidнеизменён. SharedfacesнеDisposeвпанелях;локальныеpaint/fontпо-прежнемуосвобождаются. MissingfontвозвращаетDefault иwarning,непадает.

Первыйfocusedrun:5pass/1fail из-задействительногоотсутствияU+2666. Glyphисправленвпроизводномfont, assertionsнесняты. ПослеисправленияполныйClient2326/2326,0skips; newUiFontsTestsпроверяютRU/EN,roles/cache,missingfilewarning,повторноеиспользованиепослезакрытияпанели. ScopedwhitespaceverifyPASSпослеисправленияотступоввXenonStyle. DiffcheckPASS,self-reviewпроведён. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/20261026-fonts/ (client-fonts-full.log,client-fonts.trx,font-manifest.json,format-final.log). Изначальныйglyphfailurefonts-tests.log/fonts.trxсохранёнкакreproduction.

Cross-platformlayout/nativeverificationпринадлежитTK-0003. ПопыткаCodex-managedbaselineworktreeдлясравнениядо/послебылаотклоненаинструментомкакNot a git repository (chatcwdD:/DeepSpaceSaga,repoвDSS);ручнойworktree/копиянесоздавались. Этоограничениеbefore-nativecapture,непричинасчитатьеёвыполненной. Исходныйcommitдляbefore:f472e50f884407de645898cdcdda11eebf42ba36. Всеfontsworkнеиздругихзадач.

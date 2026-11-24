# W8 · Производство портретов для двух поз

Выполни заказ на указанное число персонажей. Для каждого создай одну игровую куклу с общей внешностью и тоном кожи, двумя ракурсами головы и пятью эмоциями в каждом. Готовый персонаж должен собираться с любым зарегистрированным костюмом своей позы без индивидуального смещения, растяжения или дорисовки тела.

Для нового W8 общий полутон всей открытой кожи — **sRGB `#C98E6E` (RGB 201, 142, 110)**, взятый из настроек EV4N8S. Он един для портретов, шеи и тела в обоих ракурсах, во всех размерах и эмоциях. В PNG сохраняй светотень и блики; в `settings.json` записывай этот же полутон.

## До начала партии

Работай из `D:/DeepSpaceSaga/DSS-Images/Characters/W8/Antropik` проектным Python. Источники геометрии — [манифест стойки](../../../../DSS-Images/Characters/W8/Antropik/three-quarter-right/pose.json) и [манифест пленного](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Clothes/Prisoner/Templates/pose.json). Цвет и порядок наложения слоёв описаны в [контракте skinTone](ThreeQuarterCharacterSkinTone.md).

| Поза | Холст тела | Ячейка | Вставка | Игровой атлас, RGBA8 sRGB |
|---|---|---|---|---|
| Три четверти вправо | 282×747 | 144×190 | (69, 0) | `threequarter.png`, 720×190 |
| Пленный | 416×747 | 136×144 | (48, 416) | `prisoner.png`, 680×144 |

1. Составь список заказанных внешностей и уникальных ID из шести символов `A–Z0–9`. Проверь каждый ID в партии, в клиентском `W8/Portraits`, в `pose.json → characters`, в `work/provenance.json` и в рабочей папке исходников. Существующий ID изменяй только при явно заказанной ревизии. В путях имя персонажа строчное.
2. Для каждого ID используй отдельный рабочий каталог `work/imagegen/<id>/`. Здесь сохраняй фактические запросы в `prompts.md`, оригиналы в `raw/standing/` и `raw/prisoner/`, подложки, принятые слои и маски. Зарезервируй по одному файлу на эмоцию в каждой папке исходников. Если эмоции получены листом, сохрани также оригинал листа.
3. Перед серией проверь рабочий эталон `work/outfit/GREEN.uncut.png`, который читает импортёр. Сверь ключи `outfits` двух манифестов. Проверь каждое зарегистрированное тело, направляющие и маски его позы. При пустом каталоге костюмов проверяй геометрию головы на рабочих эталонах обеих поз. Примерку с одеждой выполни после регистрации первой размерной серии. Костюм поставляет для каждой позы собственный `Templates/skin-mask-<id>.png` в режиме L. При производстве портрета загрузи и проверь эти маски; в папку персонажа записываются только атласы и `settings.json`. Зафиксируй хеши общих ресурсов и ранее готовых атласов для проверки в конце партии.

## Масштаб головы на стандартном теле

В стойке приложи [манекен SMALL с головой без волос](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/STANDING-MANNEQUIN.png) целиком как PROPORTION ONLY вместе с крупной подложкой. Его тело построено из SMALL-BUILD, а [контур головы без волос](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/HEAD-CONSTRUCTION.png) задаёт художественный масштаб. На холсте 282×747 анатомическая макушка находится на y=14 (допуск 3 px), подбородок на y=111 (допуск 2 px). Высота головы 98±3 px, ширина 73±3 px; обе величины включают крайние пиксели. Это около 7,5 высот головы от макушки до подошвы манекена. Размер ячейки 144×190 описывает упаковку атласа.

До генерации мимики обведи на собственном рисунке neutral анатомическую голову в ракурсе три четверти единым силуэтом от затылка до наиболее выступающей точки лица, от анатомической макушки до подбородка. Под волосами восстанови форму черепа по линии лба, виску и уху. Сохрани разметку как `standing-head-construction.png` (L, 282×747, белая голова на чёрном фоне, волосы и шея исключены). Наложи эту разметку поверх исходника при 1:1. Затем из Antropik запусти [проверку размеров](../../../../DSS-Images/Characters/W8/Antropik/tools/check_standing_proportions.py) проектным Python: `python tools/check_standing_proportions.py --head-mask work/imagegen/<id>/standing-head-construction.png`. При отклонении исправь единый масштаб и положение всех слоёв neutral, повторно проверь ухо, подбородок и открытый стык шеи. Если единым переносом и масштабом совпадения добиться нельзя, перерисуй neutral. Для остальных эмоций сохрани его регистрацию.

Для проверки масштаба наложи neutral на силуэт [SMALL-BUILD](../../../../DSS-Images/Characters/W8/Antropik/three-quarter-right/Templates/SMALL-BUILD.png) в исходных координатах. Покажи результат рядом с целым манекеном в масштабе 1:1. Посадку уха, челюсти и шеи отдельно проверь на GREEN. Используй одинаковый холст и опору. Высокая причёска может выступать за разметку головы в пределах ячейки. Для пленного используй [эталонную голову без волос](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/PRISONER-BALD-HEAD.png), [эталонное тело](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/REFERENCE-BODY.png) и опоры челюсти: наклон и перспективное сокращение меняют проекцию головы.

## Сначала две нейтральные головы

Для обеих поз и всех пяти эмоций бери образцы художественного оформления из [Portraits-Master](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Portraits-Master). При каждом запуске выбирай образцы из текущего содержимого папки по характеру рисовки лица. Передавай их генератору с пометкой `STYLE ONLY`. Это правило действует для любой эмоции независимо от имён файлов. Сверяй характер контура, переходы света и тени на коже, группировку прядей и количество деталей, различимых в игровом масштабе. Черты лица, возраст и причёску бери из заказа. Если приложено фото, используй его для уточнения внешности. Лицо, уши и открытая шея в обеих позах используют общий с телом полутон EV4N8S `#C98E6E`. Ракурс, масштаб и посадку в каждой позе бери из её подложки и манифеста.

К запросу на нейтральную голову каждой позы добавь общий блок.

```text
W8 semi-realistic hand-painted game portrait of <appearance>, natural adult proportions.
Use the art samples selected from the Portraits-Master folder (STYLE ONLY) for the warm fine outlines, soft
painted skin shadows, restrained highlights and grouped hair strands. Draw the
requested facial features and hairstyle. Use the shared W8 skin midtone sRGB #C98E6E
for the face, ears and visible neck in both poses. Preserve painted shadows and highlights. Use
the pose underlay for camera angle, head scale and neck placement. Calm expression,
closed lips.
Transparent antialiased cutout. Keep clothing, backdrop, labels and accessories out
of the portrait. Avoid photographic pores, glossy 3D, exaggerated eyes and rim light.
```

**Стойка.** Возьми ростовой манекен с головой для масштаба и `work/outfit/GREEN.uncut.png` с верхней точкой шеи y=78 для стыка. К чистому манекену приложи отдельную копию с [разметкой](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/STANDING-LANDMARKS.png) в (0, 0). Бирюзовые точки показывают суставы, золотые — направление лица, включая ухо, глаза и подбородок, розовая линия — стык слоёв. Координаты бери из `landmarks`, `headLandmarks` и `neckSeam` манифеста стойки. Если поза пленного уже готова, приложи её neutral как образец внешности. Вырежи (40, 0, 242, 260), увеличь ×5 методом NEAREST и сохрани подложку на фоне `#E8E2D4` как `underlay.png`. Приложи общий эталон среза лица как `CUT ONLY`. Из цельного `work/outfit/GREEN.uncut.png` вырежи тот же прямоугольник в том же масштабе и приложи отдельно как `NECK LIGHT`. На образце должны быть видны задний край за ухом, кожа под всей челюстью и оба края воротника. Приложи [примерку Лены в SURVEYSMALL](../../../../DSS-Images/Characters/W8/Antropik/work/imagegen/li8m2q/standing/neutral-nape-fit/LI8M2Q-SURVEYSMALL.png) как `NAPE CONTOUR ONLY` для перехода из-под ближнего уха к воротнику. Для первой примерки сохрани также этот участок заказанного костюма, если он уже готов.

```text
Use the unmarked bald standing mannequin for the upright camera and anatomical scale. Its POSE ONLY
overlay locates the eyes, ear and chin. Keep all landmarks out of the art. Match the existing
identity across poses. The CUT ONLY reference shows where the head cutout ends. Use the Portraits-Master samples for drawing style only.
Use the complete PROPORTION ONLY mannequin to set anatomical head size relative to the body.
At final game scale the hairless head is 98 px high and 73 px wide, with crown at y=14 and chin at y=111.
Keep hair volume separate from skull size. Use the close-up for painting detail.
Draw the requested person facing screen-right. Match skull scale, ear and chin
height to the underlay. The body supplies the extended neck behind the near ear
(SN7L4D “After”). Use NAPE CONTOUR ONLY to register the short return below the near ear. In the assembled silhouette the screen-left nape turns gently inward, then widens towards screen left and disappears behind the near collar. The body supplies that curve. Keep exposed neck transparent on the face layer. Keep the entire visible earlobe and the short return below
and behind it to the nape hairline. From there, follow the lower edge of the
shaded jaw plane down and forward into the full rounded chin, then up its front.
Keep the narrow shaded wedge below the ear. End the skin there; leave the throat
and everything below the cut transparent. Paint the underside of the near jaw inside
the opaque face. Use the actual body crop labelled NECK LIGHT to match the local hue,
midtone and direction of light on the lower cheek to the exposed neck. Turn the jaw
with a broad change of plane inside the cheek. Keep the deepest accent local to the
angle by the ear and ease it towards the chin. A dark stroke tracing the whole cut
makes the face look pasted on. Judge the shadow with the supplied neck visible in place.
Export the isolated head with that same shading and a clean antialiased edge.
Separate locks crossing the collar from the skin.
```

Привяжи голову один раз переносом и равномерным масштабом. На сборке neutral с рабочим эталоном GREEN отметь нижнюю точку подбородка, мочку ближнего уха и фиксированную точку края воротника GREEN. За исходную высоту подбородка возьми опору y=111. Разница просвета `y воротника − y подбородка` — **до 2 игровых пикселей**. Сверь масштаб черепа и высоту уха. Проверь оба края кожи от челюсти до воротника. При 1:1 и ×4 проследи левый на экране контур от затылка до передней кромки воротника по образцу `NAPE CONTOUR ONLY`. Если излом виден уже на цельном теле, исправление относится к общей шее. Перед выпуском используй [проверку подложки стойки](ThreeQuarterOutfitCodexPrompt.md#цвет-и-геометрия-стойки). Диапазон столбцов `neckSeam` определяет разделение слоёв. Ширину шеи определяет рисунок тела на каждой строке.

Если после приёмки высоты головы кожа выступает у ворота, исправь нижнюю кромку кожи, сохранив челюсть и боковые края общего тела. На полном холсте сделай маску `standing-neck-check.png` (L). Включи нижнюю шею, оба видимых края и полосу сразу за каждым из них. Исключи реальные перекрытия лица и прядей. Проверь маску поверх тела и зафиксируй до генерации эмоций.

### Срез лица в стойке

При каждой генерации стойки приложи [эталон среза](../../../tools/DeepSpaceSaga.PortraitAssets/References/W8/standing-face-cut.png) как отдельный вход с пометкой `CUT ONLY`. Он задаёт границу кожи снизу. Черты и пропорции лица берутся из внешности персонажа, поза и масштаб — из подложки, рисовка — из `Portraits-Master`.

На крупном neutral разметь срез до обработки прозрачности. На эталоне мочка видна целиком. Под ней у затылочной линии волос остаётся короткий затенённый клин кожи. От нижнего конца клина край плавно спускается вперёд по нижней границе тени челюсти, огибает округлый низ подбородка и поднимается по его переднему краю. Сохрани этот клин и объём нижней плоскости щеки. Плотность тени оцени на цветной подложке `NECK LIGHT`. Срез от самой мочки прямо к подбородку удаляет видимую на эталоне часть головы. За сглаженной кромкой слоя `face.png` альфа равна нулю. Открытая шея до воротника принадлежит телу. Пряди, пересекающие срез, остаются в слоях волос.

**Приёмка до генерации мимики.** Сопоставь сохранённый слой лица с эталоном по мочке, затылочному концу среза, углу челюсти и нижней точке подбородка. На светлом и тёмном фоне проверь полноту кромки и прозрачность ниже неё. Затем собери сохранённый neutral на зарегистрированном теле с открытой шеей и общим `#C98E6E` по [правилам тонирования](ThreeQuarterCharacterSkinTone.md). До выпуска подходящей одежды используй рабочий эталон позы для проверки стыка. Осмотри кадр по [проверке света под челюстью](ThreeQuarterCharacterSkinTone.md#проверка-света-под-челюстью-в-стойке). Если срез захватил горло или отсёк часть челюсти, исправь маску по исходнику. Если анатомическая кромка на месте, а голова выглядит наклеенной, перерисуй светотень нижней щеки на подложенной шее, сохрани посадку и снова примерь neutral. К генерации мимики переходи после приёмки геометрии и света.

Для остальных четырёх эмоций используй принятую кромку neutral. Вдоль среза от затылка до подбородка сравни RGBA всех пяти сохранённых `face.png` с neutral побайтно. После импорта проверь те же пять эмоций на тонированных телах по перечитанному атласу: в нём под срезом лица уже видна верхняя часть общей шеи.

**Пленный.** Используй [манекен SMALL с головой без волос](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/PRISONER-MANNEQUIN.png) на холсте 416×747 и [прозрачную разметку](../../../src/DeepSpaceSaga.Client/Images/Persons/W8/Templates/PRISONER-LANDMARKS.png) в (0, 0). Из чистого манекена вырежи `(41, 412, 211, 581)` и увеличь ×5 NEAREST на том же фоне. Передай крупный план для рисования, а копию с разметкой — как POSE ONLY. Бирюзовые точки обозначают суставы, золотые — ориентиры головы, розовые — скрытые под головой опоры верхнего среза шеи. Сверь наклон по макушке, линии глаз, ближнему уху и подбородку. Индивидуальную форму лица и объём волос бери из заказа. Для отдельной проверки стыка наложи голову на `referenceBody` из [производственного шаблона](../../../../DSS-Images/Characters/W8/Antropik/prisoner/Templates/template.json).

```text
Use the clean bald mannequin close-up for the bowed pose and the POSE ONLY overlay for orientation.
The unmarked full mannequin fixes head-to-body scale. Keep the landmark colours and lines out of the art.
If the standing neutral already exists, use it for identity. Otherwise use the supplied character description or identity reference. Use Portraits-Master for style.
Draw the requested person with head bowed deeply down-left, crown visible.
Match camera, foreshortening, skull scale and placement at the collar and raised
shoulder. The near ear sits above and right of the near eye. The bridge partly hides
the far eye. The nose tip is left of the chin. Preserve the full near cheekbone,
jaw angle and rounded chin. Trace the visible chin-to-cheek contour against the garment.
Where the jaw overlaps cloth, end the cutout exactly on that anatomical contour:
all pixels outside its antialiased edge must be transparent. Keep a crisp painted edge
on the face side. Upper throat may connect only over exposed body skin shared by all
registered prisoner outfits. Never paint neck skin or a cast shadow on the garment. Keep nape locks attached and skin behind the ear clean. Trace this
character’s own silhouette. Leave collar, sleeve and shoulder on the body layer.
```

Сверь обе головы по носу, скулам, подбородку, уху, линии роста волос и приметам. По исходному рисунку пленного проследи скулу до угла челюсти, шею под подбородком и пряди за ухом. Зарегистрируй голову по черепу, уху, подбородку и верхнему срезу шеи. На светлом и тёмном фоне проверь собственный непрерывный контур, затем примерь его на все тела из `outfits` пленного с `skinTone`. Разметь на зарегистрированном исходном neutral собственную линию подбородок–скула. Проверь её на каждом зарегистрированном теле. Там, где под этой линией хотя бы один костюм показывает ткань, доведи прозрачность точно до контура лица. Сохрани естественное сглаживание в граничном пикселе и ясный рисунок со стороны щеки. Снаружи края альфа должна быть 0. Короткий переход шеи разрешён только там, где все костюмы позы показывают открытую кожу. Край у ткани не растушёвывай. До обработки альфы разметь на полном холсте маску L `prisoner-jaw-clear.png`: белым отметь зону сразу за сглаженным краем под челюстью над тканью, включая место возможного выступа шеи. Сверь разметку с неизменённым исходником, исключи реальные пряди и обязательное перекрытие верхнего среза тела. Не выводи контрольную маску из готового альфа-канала. Для всех пяти эмоций используй одну разметку. У левого на экране плеча виден естественный переход в рукав. Ступенька под челюстью, прямая срезанная скула, прорези в волосах или отдельные пиксели на затылке требуют исправления нейтрального слоя. Сохрани принятый прозрачный слой нейтральной головы на полном холсте 416×747 как `prisoner-neutral-fit.png`. Он должен попиксельно совпадать с `prisoner-layers/neutral.png`. Примерку с телом сохраняй отдельно. Присваивать голове альфу чужого `portraitAlpha`, вычитать из неё силуэт тела или достраивать шею растяжением крайнего пикселя запрещено.

## Четыре изменения мимики на каждую позу

Начинай с принятого neutral **этой позы**. Общий порядок ячеек слева направо — `neutral, pain, anger, joy, pleasure`. Сохрани тот же источник света, положение головы, причёску, нос, ухо, подбородок и кожу у стыка. Можно получить четыре варианта одним листом на позу.

```text
Edit the accepted neutral of this pose. Use art samples from the Portraits-Master folder
for the drawing style of every expression. Lock hair, nose, ears, silhouette, pose,
neck, skin tone and lighting. In the standing pose keep the accepted jaw-to-neck shading. Change brows, lids and mouth as coherent features.
Pain: inner brows raised and drawn together, squint, tense or parted lips.
Anger: brows lowered towards the bridge, narrowed eyes, tight lips.
Joy: raised cheeks, broad smile, upper teeth read as one warm light shape.
Pleasure: relaxed brows, half-closed lids, soft closed smile.
```

Перед переносом совмести рисунки по переносице, кончику носа, уху и челюсти. На полном холсте каждой позы зафиксируй две маски L в рабочем каталоге: `standing-expression-mask.png` и `standing-nose-lock.png`, для пленного — те же имена с префиксом `prisoner-`. Белым отметь соответственно области изменения и неподвижный нос. Замени каждую черту целиком вместе с её прежним контуром и уголками. Смягчай переход только на соседней коже. При несовместимом ракурсе переделай исходник эмоции.

В масштабе 1:1 поставь сохранённую ячейку рядом с выбранными образцами рисовки лица из `Portraits-Master`. Проверь толщину линии века и брови, мягкость тени на щеке и способ рисовать волосы. Затем при 1:1 и ×4 NEAREST проверь каждую сохранённую ячейку. Брови одиночные, веки сходятся в уголках и охватывают радужку с учётом ракурса. Переносица, кончик и ноздри читаются как один нос. У рта одна линия смыкания или одно открытие, пропорциональные губы и согласованные уголки. Улыбка, прищур и напряжение читаются и в наклоне головы. Альфа всей ячейки, RGBA носа и RGBA за пределами мимики должны совпасть с neutral побайтно.

## Контрольные точки стойки

Для нового портрета зафиксируй на neutral три ориентира в координатах холста 282×747: нижний край мочки видимого уха, переход под ухом к челюсти и нижнюю точку подбородка. Покажи их художнику на подложке и отметь те же ориентиры на исходном слое лица до добавления волос. Точка внутри мочки — (117, 79), перекрытие за ухом — (123, 90), под углом челюсти — (130, 99), у подбородка — (152, 111). Последние три точки должны лежать внутри непрозрачного перекрытия лица и шеи. Контрольные точки открытой шеи — (125, 104) и (140, 117). Форма мочки и подбородка индивидуальна; оцени её визуально вместе с попаданием в опоры. Для пяти эмоций сохраняй регистрацию neutral и вставку полной ячейки в (69, 0).

Основной выпуск использует посадку «После». Верх шеи за ухом достигает y=78 при альфе ≥128. Импортёр берёт общую шею из GREEN и включает её верх в ячейку портрета.

## Подготовка слоёв и импорт

Убирай фон до уменьшения. Для исходника на цветной подложке проверь отсутствие цветной каймы на светлом и тёмном фоне. Уменьшай зарегистрированный рисунок фильтром BOX в premultiplied alpha, сохраняй PNG в **straight RGBA8 sRGB**. При работе с подложкой ×5 верни рисунок в её исходный прямоугольник. Оставь сглаживание внешнего контура и прозрачные поля. Волосы стойки доходят максимум до плеч. Если рисунок выходит за ячейку, исправь исходник до экспорта.

- **Стойка.** Сохрани `layers/<emotion>/{back-hair,face,front-hair}.png` размером 282×747. Задние волосы проходят за телом, передние пряди лежат поверх. Нижний край слоя лица и его собственную тень проверь по разделу [«Срез лица в стойке»](#срез-лица-в-стойке). Ткань и ворот принадлежат телу. Импортёр собирает мастер на GREEN, учитывает перекрытие задних волос и включает в портрет кожу тела выше шва.
- **Пленный.** Сохрани пять `prisoner-layers/<emotion>.png` размером 416×747. В слоях остаются голова, волосы и участок верхней шеи только над общей открытой кожей тел. Непрозрачные пиксели головы закрывают верхний срез тела. Эталонная маска контролирует именно перекрытие среза. Остальной силуэт сверяется с собственным `prisoner-neutral-fit.png`.

Для серии сначала собери комплект в рабочем каталоге кандидата. Команды ниже выполняй в одной сессии PowerShell из `Antropik`, подставив новый ID. Импорт одного персонажа выполняй последовательно: `add_character.py` блокирует на время записи общие рабочие манифесты. Даже с `--portraits-dir` он обновляет `characters`, `work/provenance.json` и рабочие мастера. Папка кандидата — место назначения игровых файлов.

```powershell
$py = 'D:/DeepSpaceSaga/DSS-Images/tools/modular-character-pilot/.venv/Scripts/python.exe'
$env:W8_CHARACTER = 'XXXXXX'
$env:W8_SKIN_TONE = '#C98E6E'
$idFolder = $env:W8_CHARACTER.ToLowerInvariant()
$workDir = Join-Path (Get-Location) "work/imagegen/$idFolder"
$candidates = Join-Path $workDir 'candidate'
& $py tools/add_character.py --id $env:W8_CHARACTER --layers "$workDir/layers" --skin-tone $env:W8_SKIN_TONE --portraits-dir $candidates
if ($LASTEXITCODE -ne 0) { throw 'Ошибка импорта стойки' }
```

### Экспорт пленного и проверка записанных атласов

Сохрани блок во временном файле `$workDir/check_portraits.py` и выполни из `Antropik` командой `& $py "$workDir/check_portraits.py"` с переменными текущего ID. Продолжай к приёмке только при коде завершения 0. Запускай с включёнными проверками `assert` (обычный режим Python). При ошибке исправь указанный слой того же ID и повтори сборку кандидата.

```python
import json, os, re, sys
from pathlib import Path
from tempfile import TemporaryDirectory

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path("tools").resolve()))
import dss3q as d

cid = os.environ["W8_CHARACTER"]
assert re.fullmatch(r"[A-Z0-9]{6}", cid)
src = Path("work/imagegen") / cid.lower()
dst = src / "candidate" / cid.lower()
tone = json.loads((dst / "settings.json").read_text(encoding="utf-8"))["skinTone"]
assert tone == "#C98E6E" == os.environ["W8_SKIN_TONE"].upper()
standing = d.Pose("three-quarter-right")
template = d.CLIENT_W8_ROOT / "Clothes/Prisoner/Templates"
pr = json.loads((template / "pose.json").read_text(encoding="utf-8"))
production = Path("prisoner/Templates")
refs = json.loads((production / "template.json").read_text(encoding="utf-8"))
assert standing.canvas == (282, 747) and standing.cell == (144, 190) and standing.origin == (69, 0)
assert pr["canvas"] == [416, 747] and pr["portrait"]["cellSize"] == [136, 144]
assert pr["portrait"]["drawOrigin"] == [48, 416]
assert pr["referenceSkinTone"] == tone
assert set(standing.m["outfits"]) == set(pr["outfits"])
for manifest, base, prefix in [(standing.m, d.CLIENT_W8_ROOT, "Clothes/ThreeQuarterOutfit/Templates/"),
                               (pr, template, "")]:
    assert set(manifest["outfits"]) == set(manifest["masks"]["skinByOutfit"])
    for outfit, rel in manifest["outfits"].items():
        mask_path = manifest["masks"]["skinByOutfit"][outfit]
        assert mask_path == f"{prefix}skin-mask-{outfit.lower()}.png", outfit
        with Image.open(base / rel) as body, Image.open(base / mask_path) as skin:
            assert skin.mode == "L" and skin.size == body.size == tuple(manifest["canvas"]), outfit
            assert not np.any(np.asarray(skin)[np.asarray(body.getchannel("A")) == 0]), outfit
emotions = standing.emotions
assert emotions == pr["expressions"] == ["neutral", "pain", "anger", "joy", "pleasure"]

def read(path, mode, size):
    with Image.open(path) as im:
        assert im.mode == mode and im.size == tuple(size), path
        return np.array(im)

def check_cells(path, pose_name, canvas, origin, cell):
    x, y = origin
    w, h = cell
    pixels = read(path, "RGBA", (w * len(emotions), h))
    frames = np.split(pixels, len(emotions), axis=1)
    masks = [read(src / f"{pose_name}-{k}.png", "L", canvas)[y:y+h, x:x+w] > 0
             for k in ("expression-mask", "nose-lock")]
    editable, nose = masks
    assert editable.any() and nose.any()
    fixed = ~editable | nose
    for emotion, frame in zip(emotions, frames):
        assert np.array_equal(frame[..., 3], frames[0][..., 3]), (pose_name, emotion, "alpha")
        assert np.array_equal(frame[fixed], frames[0][fixed]), (pose_name, emotion, "face")
    assert len({f.tobytes() for f in frames}) == len(emotions), (pose_name, "duplicate emotion")
    return frames

green = read("work/outfit/GREEN.uncut.png", "RGBA", standing.canvas)
guard = read(src / "standing-neck-check.png", "L", standing.canvas) > 0
assert guard.any()
assert np.flatnonzero((green[:106, 110:166, 3] >= 128).any(axis=1))[0] == 78, "standing After neck"
neck_mask = read("work/outfit/skin-mask.png", "RGBA", standing.canvas)[..., 3]
for row, first_x in [(101, 118), (106, 117), (110, 116), (114, 114), (117, 113)]:
    covered = np.flatnonzero(neck_mask[row, 110:140] >= 124)
    assert covered.size and 110 + covered[0] == first_x, ("standing nape", row, first_x)
upper = np.zeros_like(green[..., 3])
for x, y in enumerate(standing.seam_y, standing.seam_x0):
    upper[:y, x] = green[:y, x, 3]
x, y = standing.origin
w, h = standing.cell
for emotion, frame in zip(emotions, check_cells(dst / "threequarter.png", "standing",
                          standing.canvas, standing.origin, standing.cell)):
    alpha = np.zeros_like(upper)
    alpha[y:y+h, x:x+w] = frame[..., 3]
    assert np.array_equal(alpha[guard], upper[guard]), (emotion, "standing neck")
    face = read(src / "layers" / emotion / "face.png", "RGBA", standing.canvas)
    for ax, ay in [(117, 79), (123, 90), (130, 99), (152, 111)]:
        assert face[ay, ax, 3] == 255 and alpha[ay, ax] == 255, (emotion, "ear/jaw/chin", ax, ay)
    for ax, ay in [(125, 104), (140, 117)]:
        assert face[ay, ax, 3] == 0, (emotion, "body skin on face layer", ax, ay)

p = pr["portrait"]
x, y = p["drawOrigin"]
w, h = p["cellSize"]
canvas = pr["canvas"]
area = np.s_[y:y+h, x:x+w]
mask = lambda key: read(production / refs["masks"][key], "L", canvas)
front, back = (pr["landmarks"][k] for k in ("upperNeckFront", "upperNeckBack"))
yy, xx = np.indices((canvas[1], canvas[0]))
lo, hi = sorted((front[0], back[0]))
assert hi > lo
line = front[1] + (xx-front[0]) * (back[1]-front[1]) / (back[0]-front[0])
seam = (mask("neckProtect") > 0) & (mask("portraitAlpha") == 255)
seam &= (xx >= lo) & (xx <= hi) & (np.abs(yy-line) <= 4)
assert seam.sum() == 60 and seam[:, lo:hi+1].any(axis=0).all()
reference = read(production / refs["referenceBody"], "RGBA", canvas)
for outfit, rel in pr["outfits"].items():
    assert np.array_equal(read(template / rel, "RGBA", canvas)[seam], reference[seam]), outfit

neutral = read(src / "prisoner-neutral-fit.png", "RGBA", canvas)
jaw_clear = read(src / "prisoner-jaw-clear.png", "L", canvas) > 0
assert jaw_clear[area].any() and not (jaw_clear & seam).any(), "jaw mask"
cells = []
for emotion in emotions:
    head = read(src / "prisoner-layers" / f"{emotion}.png", "RGBA", canvas)
    assert head[..., 3].any() and np.count_nonzero(head[..., 3]) == np.count_nonzero(head[area][..., 3]), emotion
    if emotion == "neutral":
        assert np.array_equal(head, neutral), "accepted neutral"
    assert (head[..., 3][seam] == 255).all(), (emotion, "prisoner neck")
    assert not head[..., 3][jaw_clear].any(), (emotion, "neck over garment")
    cells.append(head[area])
with TemporaryDirectory(dir=dst) as tmp:
    candidate = Path(tmp) / p["file"]
    Image.fromarray(np.concatenate(cells, axis=1)).save(candidate)
    actual = check_cells(candidate, "prisoner", canvas, p["drawOrigin"], p["cellSize"])
    assert all(np.array_equal(a, b) for a, b in zip(actual, cells))
    candidate.replace(dst / p["file"])
assert {f.name for f in dst.iterdir()} == {"threequarter.png", "prisoner.png", "settings.json"}
```

## Приёмка и выдача основного комплекта

Перед выдачей перечитай оба реестра костюмов. Если серия костюмов уже опубликована после примерки на манекенах, при приёмке первого портрета выполни все 30 сборок по шагу 1 для каждой такой серии, включая открытый крой. Для открытого кроя осмотри границу ткани и кожи под челюстью и передними прядями. Если зарегистрированных костюмов пока нет, укажи результат примерки на шаблонах и статус «совместная примерка ожидает первого костюма». Первый выпуск одежды закрывает эту проверку по опубликованным PNG.

1. Перечитай **файлы кандидата**. Собери для каждой позы каждую эмоцию на каждом теле из её манифеста. Для одной полной размерной серии это 30 сочетаний на персонажа (3 размера × 2 позы × 5 эмоций). Положение ячейки и масштаб одинаковы на всех костюмах позы. Для стойки используй `Pose.portrait_underlay` и `dss3q.over`, для пленного — полное тело с ячейкой поверх. Примени [контракт тонирования](ThreeQuarterCharacterSkinTone.md). При базовом тоне кадр на рабочем эталоне GREEN стойки должен совпадать с рабочим мастером по видимым каналам с допуском 1 уровень на сглаживании.
2. Осмотри все выражения в игровом масштабе и крупно. Проверь пропорции, стык с обеих сторон, участок под подбородком, затылок и плечи. Для стойки повтори проверку света под челюстью на перечитанном атласе с каждым костюмом. Сравни нейтральные головы между позами. Для базовой проверки собери каждую из пяти ячеек стойки из файла кандидата на `work/outfit/GREEN.uncut.png` и сравни с `work/masters/<id>/<emotion>.GREEN.png`. У пленного наложи соответствующую ячейку на `referenceBody`. Эти две примерки доступны и при пустых реестрах костюмов. Собирай кадры один раз, для увеличения вырезай области из тех же сборок. На светлом и тёмном фоне должны сохраняться естественный контур и чистые промежутки между прядями. При дефекте исправь соответствующий исходник и повтори проверку этого ID.
3. После приёмки нового ID перенеси целиком проверенную папку кандидата в клиентский каталог на том же диске. Команда ниже завершается ошибкой, если целевая папка уже существует. После прерывания сверь рабочие исходники и запись в `imagegenCharacters` и продолжи тот же ID с незавершённого этапа. Если папка уже опубликована, проверь её содержимое и хеши. Ревизия существующего персонажа требует отдельного запроса и сохранения предыдущего комплекта.
4. Проверь клиентский путь, хеши трёх файлов и результат валидатора. Засчитывай персонажа только после успешной проверки обеих поз. По партии укажи число заказанных и готовых ID, причину каждого пропуска. Сверь сохранность ранее готовых ассетов. Временные матрицы держи в `D:/DeepSpaceSaga/artifacts/validation/<task>/<run>/` и удаляй после проверки. Итоговые картинки сохраняй и показывай по запросу, вне игровой папки. В `Portraits/<id>/` остаются ровно два атласа и один JSON.

```powershell
$clientRoot = 'D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/Images/Persons/W8/Portraits'
$sourceDir = Join-Path $candidates $idFolder
$gameDir = Join-Path $clientRoot $idFolder
$hashes = Get-ChildItem -LiteralPath $sourceDir -File | Get-FileHash
[IO.Directory]::Move($sourceDir, $gameDir)
foreach ($entry in $hashes) {
    $published = Join-Path $gameDir ([IO.Path]::GetFileName($entry.Path))
    if ((Get-FileHash -LiteralPath $published).Hash -ne $entry.Hash) { throw "Расхождение в $published" }
}
& $py tools/dss3q.py validate --pose three-quarter-right --character $env:W8_CHARACTER
if ($LASTEXITCODE -ne 0) { throw 'Ошибка проверки опубликованной стойки' }
```

# Ubuntu: сборка, запуск и пользовательские файлы

Клиент использует .NET 8, SkiaSharp и Silk.NET. Целевые Linux-среды — Ubuntu
22.04/24.04 x64 с X11 либо XWayland и OpenGL 3.3. Native Wayland и arm64
не входят в подтверждённую совместимость. Результаты конкретных запусков,
desktop/audio и человеческая приёмка учитываются в [Board EP-0015](../../Board/EP-0015-ubuntu-support/ExecutionStatus.md).

## Системные зависимости

Для графического клиента установите библиотеки окна, OpenGL, fontconfig и
открытия папок. Обычный desktop Ubuntu уже содержит часть пакетов:

```bash
sudo apt-get update
sudo apt-get install -y libgl1 libglx-mesa0 libegl1 libglfw3 libx11-6 \
  libxrandr2 libxi6 libxcursor1 libxinerama1 libfontconfig1 \
  libwayland-client0 libwayland-cursor0 libwayland-egl1 libxkbcommon0 xdg-utils
```

Для аудио нужны доступное устройство и PulseAudio/PipeWire либо ALSA;
пакет `libpulse0` предоставляет PulseAudio client ABI. ALSA-пакет называется
`libasound2t64` на Ubuntu 24.04 и `libasound2` на Ubuntu 22.04. OpenAL Soft
поставляется вместе с игрой. При недоступном устройстве клиент продолжает
работать и однократно пишет причину в Interface.log.

Self-contained сборке не нужен установленный .NET, но нужны системные
зависимости .NET: libc, libgcc, libstdc++, ICU, OpenSSL, zlib, tzdata и
ca-certificates. На 24.04 ICU/OpenSSL — `libicu74`/`libssl3t64`, на 22.04 —
`libicu70`/`libssl3`. Актуальный список и установка SDK приведены в
[официальном руководстве .NET для Ubuntu](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install).

## Сборка и проверки из исходников

Нужны .NET SDK 8+, Python 3 и Git. CI выбирает именно SDK 8.0.x; локальный
`global.json` разрешает более новый SDK. Проверяйте фактически выбранную версию.

```bash
sudo apt-get install -y dotnet-sdk-8.0 python3
cd ~/src/DSS
dotnet --info
results="$(realpath -m ../artifacts/validation/ubuntu/$(date -u +%Y%m%dT%H%M%SZ)-$$)"
bash tools/check-linux.sh "$results/tests"
```

Скрипт восстанавливает и собирает solution в Release, обнаруживает все тесты и
проверяет точное покрытие VSTest/TRX по именам и числу theory rows. Каждый
дочерний процесс ограничен 300 секундами. Без DISPLAY/WAYLAND_DISPLAY Client
явно помечается NOT RUN; это не успешная проверка клиента. Для автоматизированной
X11-сессии установите `xvfb xauth` и выполните:

```bash
xvfb-run -a bash tools/check-linux.sh "$results/xvfb-tests"
```

Xvfb с Mesa проверяет программный OpenGL. Это не проверка физической мыши,
слышимости звука, драйвера пользовательского GPU или native Wayland.

## Дистрибутив

Из корня репозитория выберите новый каталог вывода:

```bash
bash tools/publish-linux.sh "$results/distribution"
mkdir -p ~/Games
tar -xzf "$results/distribution/DeepSpaceSaga-linux-x64.tar.gz" -C ~/Games
~/Games/DeepSpaceSaga-linux-x64/DeepSpaceSaga.Client
```

Скрипт публикует self-contained `linux-x64`
без single-file, проверяет runtime, native Skia/GLFW/OpenAL и каталоги Data,
Images, UI/Fonts, Scenarios; пропуск обязательного файла завершает сборку ошибкой.
Архив tar.gz сохраняет executable bit. Уже существующий каталог пакета не
перезаписывается — укажите новый вывод.

После распаковки можно зарегистрировать пункт меню от своего пользователя:

```bash
cd ~/Games/DeepSpaceSaga-linux-x64
bash ./install-desktop.sh --install
```

Установщик создаёт desktop entry с абсолютным путём к текущему расположению
игры. Повторите его после перемещения каталога. `--generate-only` обновляет
entry рядом с программой; для него каталог программы должен быть доступен
на запись. Основной запуск и `--install` допускают read-only установку.

## Настройки и данные

| Файлы | Linux | Windows |
|---|---|---|
| Settings.json | `$XDG_CONFIG_HOME/DeepSpaceSaga` либо `~/.config/DeepSpaceSaga` | `%LOCALAPPDATA%\DeepSpaceSaga` |
| Saves, DebugTuning, CombatMetrics, Interface.log | `$XDG_DATA_HOME/DeepSpaceSaga` либо `~/.local/share/DeepSpaceSaga` | `%LOCALAPPDATA%\DeepSpaceSaga` |
| Data, Images, UI/Fonts, Scenarios | Каталог программы, только чтение | Каталог программы, только чтение |

XDG-переменные учитываются только с абсолютными путями. При первом запуске
Settings.json из комплекта становится шаблоном пользовательского файла.
Поле `contentRoot` управляется приложением и указывает на текущую установку;
остальные пользовательские настройки сохраняются. Engine разрешает каталоги,
default scenario, конфигурацию мира и портреты относительно этого корня.
При отсутствии `contentRoot` сохраняется разрешение относительно Settings.json.

Первичная миграция копирует старые Saves из каталога программы только в пустой
пользовательский Saves, ничего не удаляя и не перезаписывая. Маркер
`.saves-migrated` предотвращает повторный импорт после удаления игроком старых
слотов. Если в пользовательском Saves уже есть данные, они остаются без изменений.

Файл `portable.txt` рядом с executable переключает настройки, сохранения и
диагностику в каталог программы. Этот режим требует доступной на запись
установки; release-пакет по умолчанию не содержит маркера. Для разработки его
можно создать вручную. Диагностические переменные `DSS_INTERFACE_LOG_PATH` и
`DSS_DIAGNOSTICS_DIRECTORY` принимают абсолютные evidence-пути для проверок.

## Шрифты, интерфейс и инструменты

Inter используется для UI, Noto Sans — для текста, DSS Mono — для моноширинных
панелей. Файлы, лицензии OFL и происхождение находятся в
[UI/Fonts](../../../src/DeepSpaceSaga.Client/UI/Fonts). Отсутствующий файл шрифта
даёт предупреждение и fallback. Торговая панель целиком вписывается в окно,
а клики используют обратное преобразование координат.

Два asset-скрипта требуют PowerShell 7+ и SDK, используют существующий Skia
PortraitAssets tool и работают без System.Drawing. Установка PowerShell описана
в [официальном руководстве](https://learn.microsoft.com/en-us/powershell/scripting/install/install-ubuntu).
Команды и параметры смотрите в шапках
[Export-TacticalButtonAssets.ps1](../../../tools/Export-TacticalButtonAssets.ps1) и
[prepare.ps1](../../../src/DeepSpaceSaga.Client/Images/Persons/W4/Portraits/character-generator/scripts/prepare.ps1).
Для диагностического запуска задавайте output/preview под workspace artifacts;
проверка анатомии, прозрачности и стыковки воротников остаётся обязательной.

Если папка отладки не открылась, статус показывает полный путь и ошибка
записывается в Interface.log. На Linux требуется `xdg-open` из `xdg-utils`.
При проблемах запуска сначала проверяйте DISPLAY, графические библиотеки,
права пользовательских каталогов и этот лог.

Исходники: [AppPaths](../../../src/DeepSpaceSaga.Client/AppPaths.cs),
[EngineContentLoader](../../../src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs),
[check-linux.sh](../../../tools/check-linux.sh),
[publish-linux.sh](../../../tools/publish-linux.sh),
[CI](../../../.github/workflows/ubuntu-support.yml).

# Black Myth: Wukong Benchmark — отчёт

Дата: 2026-10-06 17:30  
Папка бенчмарка: `D:\SteamLibrary\steamapps\common\Black Myth Wukong Benchmark Tool`

## Характеристики ПК

| Компонент | Значение |
|---|---|
| CPU | 11th Gen Intel(R) Core(TM) i5-11400F @ 2.60GHz (6 ядер / 12 потоков, базовая 2592 МГц, L3 12 МБ) |
| GPU | NVIDIA GeForce RTX 3060 — 8 ГБ VRAM, драйвер 32.0.15.8142 |
| RAM | 16 ГБ (2 модул.) |
| — модуль | 8 ГБ @ 2666 МТ/с, Patriot Memory (PDP Systems) 3200 C16 Series |
| — модуль | 8 ГБ @ 2666 МТ/с, Patriot Memory (PDP Systems) 3200 C16 Series |
| Мат. плата | Gigabyte Technology Co., Ltd. Z590 AORUS PRO AX |
| ОС | Майкрософт Windows 10 Pro 64-разрядная (build 10.0.19045) |
| Монитор | 2560x1440 @ 165 Гц |
| Диск с игрой | D:\ NTFS, свободно 167 ГБ из 340 ГБ; диск: KINGSTON SKC3000S1024G; диск: Samsung SSD 980 PRO 500GB |
| План питания | High Performance |

## Результаты

| Тест | Разрешение | Avg FPS | Min FPS | Max FPS | FPS95 | 1% low* | 0.1% low* | Frametime, мс* | CPU frametime, мс* | GPU frametime, мс* | VRAM, ГБ* | Длительность |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **CPU** | 1280x720 | **87.0** | 52.0 | 104.0 | 79.0 | 74.5 | 61.4 | 11.5 | 6.8 | 10.2 | 4.3 | 03:32 |
| **GPU** | 2560x1440 | **16.0** | 11.0 | 19.0 | 14.0 | 14.0 | 12.6 | 60.5 | 8.1 | 58.6 | 6.5 | 03:38 |

\* посчитано утилитой из покадровых записей `Records` (если бенчмарк их сохранил).

## Настройки CPU-теста

- Разрешение: **1280x720**, полноэкранный режим
- Масштаб рендера (Super Resolution / sg.ResolutionQuality): **50%**
- Общее качество: **Low**, дальность прорисовки: **Low**
- Full Ray Tracing: **Выкл**
- Full Ray Tracing уровень: **—**
- Генерация кадров: выкл, VSync: выкл, лимит FPS: нет, динамическое разрешение: выкл

✅ Проверка по JSON бенчмарка: настройки применились.

<details><summary>Записанные в GameUserSettings.ini значения</summary>

| Ключ | Значение |
|---|---|
| `ResolutionSizeX` | 1280 |
| `LastUserConfirmedResolutionSizeX` | 1280 |
| `DesiredScreenWidth` | 1280 |
| `LastUserConfirmedDesiredScreenWidth` | 1280 |
| `ResolutionSizeY` | 720 |
| `LastUserConfirmedResolutionSizeY` | 720 |
| `DesiredScreenHeight` | 720 |
| `LastUserConfirmedDesiredScreenHeight` | 720 |
| `FullscreenMode` | 0 |
| `LastConfirmedFullscreenMode` | 0 |
| `PreferredFullscreenMode` | 0 |
| `bUseVSync` | False |
| `FrameRateLimit` | 0.000000 |
| `bUseDynamicResolution` | False |
| `sg.ResolutionQuality` | 50 |
| `sg.ViewDistanceQuality` | 0 |
| `sg.AntiAliasingQuality` | 0 |
| `sg.PostProcessQuality` | 0 |
| `sg.ShadowQuality` | 0 |
| `sg.TextureQuality` | 0 |
| `sg.EffectsQuality` | 0 |
| `sg.ShadingQuality` | 0 |
| `sg.FoliageQuality` | 0 |
| `sg.GlobalIlluminationQuality` | 0 |
| `sg.ReflectionQuality` | 0 |
| `sg.RayTracingQuality` | 0 |
| `UISettingData.ScreenMode` | 0 |
| `UISettingData.Vsync` | 0 |
| `UISettingData.LockFrameRate` | 0 |
| `UISettingData.InsertFrame` | 0 |
| `UISettingData.ImageQuality` | 360 |
| `UISettingData.QualityLevel` | 1 |
| `UISettingData.ViewDistance` | 1 |
| `UISettingData.Rtx` | 0 |
| `UISettingData.RtxLevel` | 4 |
| `UISettingData.AntiAliasing` | 1 |
| `UISettingData.PostProcessing` | 1 |
| `UISettingData.ShadowQuality` | 1 |
| `UISettingData.TextureQuality` | 1 |
| `UISettingData.FxQuality` | 1 |
| `UISettingData.MaterialQuality` | 1 |
| `UISettingData.VegetationQuality` | 1 |
| `UISettingData.GlobalIllumination` | 1 |
| `UISettingData.ReflectionQuality` | 1 |

</details>

<details><summary>Прочие поля из результата бенчмарка (загрузка CPU/GPU, фактические настройки и т.д.)</summary>

| Поле | Значение |
|---|---|
| `AntiAliasing` | 1 |
| `BenckMarkVersion` | 0 |
| `CPUAvg` | 1 |
| `CPUModel` | 11th Gen Intel(R) Core(TM) i5-11400F @ 2.60GHz |
| `Dlss` | 0 |
| `Dx12` | 1 |
| `GPUAvg` | 1 |
| `GPULeak` | 0 |
| `GPUModel` | NVIDIA GeForce RTX 3060 |
| `GameVer` | 1.0.3.14649 |
| `GpuDriverVer` | 581.42 |
| `ImageQuality` | 50 |
| `InsertFrame` | 0 |
| `MaterialQuality` | 1 |
| `MotionBlur` | 2 |
| `PostProcessing` | 1 |
| `QualityLevel` | 1 |
| `Rtx` | 0 |
| `ScreenMode` | 1 |
| `ScreenResolution` | 1280 × 720 |
| `ShadowQuality` | 1 |
| `SysMem` | 16GB |
| `SysVer` | 10.0.19045.1.256.64bit |
| `TextureQuality` | 1 |
| `TimeStamp` | 1791296771 |
| `VegetationQuality` | 1 |
| `VideoMem` | 4.3 |
| `VideoMemSize` | 8GB |
| `ViewDistance` | 1 |

</details>

Исходный файл: `C:\Users\Neo\AppData\Local\Temp\b1\BenchMarkHistory\Tool\1791296771` (записей кадров: 12341)

## Настройки GPU-теста

- Разрешение: **2560x1440**, полноэкранный режим
- Масштаб рендера (Super Resolution / sg.ResolutionQuality): **100%**
- Общее качество: **Cinematic**, дальность прорисовки: **Cinematic**
- Full Ray Tracing: **Вкл**
- Full Ray Tracing уровень: **Very High**
- Генерация кадров: выкл, VSync: выкл, лимит FPS: нет, динамическое разрешение: выкл

⚠️ Проверка по JSON бенчмарка:
  - Ray Tracing: ожидалось 1, бенчмарк использовал 4

<details><summary>Записанные в GameUserSettings.ini значения</summary>

| Ключ | Значение |
|---|---|
| `ResolutionSizeX` | 2560 |
| `LastUserConfirmedResolutionSizeX` | 2560 |
| `DesiredScreenWidth` | 2560 |
| `LastUserConfirmedDesiredScreenWidth` | 2560 |
| `ResolutionSizeY` | 1440 |
| `LastUserConfirmedResolutionSizeY` | 1440 |
| `DesiredScreenHeight` | 1440 |
| `LastUserConfirmedDesiredScreenHeight` | 1440 |
| `FullscreenMode` | 0 |
| `LastConfirmedFullscreenMode` | 0 |
| `PreferredFullscreenMode` | 0 |
| `bUseVSync` | False |
| `FrameRateLimit` | 0.000000 |
| `bUseDynamicResolution` | False |
| `sg.ResolutionQuality` | 100 |
| `sg.ViewDistanceQuality` | 4 |
| `sg.AntiAliasingQuality` | 4 |
| `sg.PostProcessQuality` | 4 |
| `sg.ShadowQuality` | 4 |
| `sg.TextureQuality` | 4 |
| `sg.EffectsQuality` | 4 |
| `sg.ShadingQuality` | 4 |
| `sg.FoliageQuality` | 4 |
| `sg.GlobalIlluminationQuality` | 4 |
| `sg.ReflectionQuality` | 4 |
| `sg.RayTracingQuality` | 3 |
| `UISettingData.ScreenMode` | 0 |
| `UISettingData.Vsync` | 0 |
| `UISettingData.LockFrameRate` | 0 |
| `UISettingData.InsertFrame` | 0 |
| `UISettingData.ImageQuality` | 1440 |
| `UISettingData.QualityLevel` | 5 |
| `UISettingData.ViewDistance` | 5 |
| `UISettingData.Rtx` | 1 |
| `UISettingData.RtxLevel` | 4 |
| `UISettingData.AntiAliasing` | 5 |
| `UISettingData.PostProcessing` | 5 |
| `UISettingData.ShadowQuality` | 5 |
| `UISettingData.TextureQuality` | 5 |
| `UISettingData.FxQuality` | 5 |
| `UISettingData.MaterialQuality` | 5 |
| `UISettingData.VegetationQuality` | 5 |
| `UISettingData.GlobalIllumination` | 5 |
| `UISettingData.ReflectionQuality` | 5 |

</details>

<details><summary>Прочие поля из результата бенчмарка (загрузка CPU/GPU, фактические настройки и т.д.)</summary>

| Поле | Значение |
|---|---|
| `AntiAliasing` | 5 |
| `BenckMarkVersion` | 0 |
| `CPUAvg` | 1 |
| `CPUModel` | 11th Gen Intel(R) Core(TM) i5-11400F @ 2.60GHz |
| `Dlss` | 0 |
| `Dx12` | 1 |
| `GPUAvg` | 1 |
| `GPULeak` | 1 |
| `GPUModel` | NVIDIA GeForce RTX 3060 |
| `GameVer` | 1.0.3.14649 |
| `GpuDriverVer` | 581.42 |
| `ImageQuality` | 100 |
| `InsertFrame` | 0 |
| `MaterialQuality` | 5 |
| `MotionBlur` | 2 |
| `PostProcessing` | 5 |
| `QualityLevel` | 5 |
| `Rtx` | 4 |
| `ScreenMode` | 1 |
| `ScreenResolution` | 2560 × 1440 |
| `ShadowQuality` | 5 |
| `SysMem` | 16GB |
| `SysVer` | 10.0.19045.1.256.64bit |
| `TextureQuality` | 5 |
| `TimeStamp` | 1791296990 |
| `VegetationQuality` | 5 |
| `VideoMem` | 6.5 |
| `VideoMemSize` | 8GB |
| `ViewDistance` | 5 |

</details>

Исходный файл: `C:\Users\Neo\AppData\Local\Temp\b1\BenchMarkHistory\Tool\1791296990` (записей кадров: 2342)


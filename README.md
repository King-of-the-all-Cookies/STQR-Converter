# STQR Converter

[English Version](#english-version) | [Русская версия](#русская-версия)

---

## English Version

### About STQR Converter

STQR Converter is a tool designed to extract data from STQR files and convert it to JSON format, as well as convert JSON back to STQR. This allows users to modify audio-related data within STQR files and repackage them.

### Features
- Extracts data from STQR and outputs JSON
- Converts JSON back to STQR
- Provides information such as file path, offset, size, samples, channels, sample rate, loop start, and loop end
- Supports modification and reinsertion of sound files

### Usage
#### Extracting STQR to JSON:
```sh
stqr_converter extract input.stqr output.json
```

#### Converting JSON back to STQR:
```sh
stqr_converter pack input.json output.stqr
```

### JSON Structure
Each entry in the JSON contains:
```json
{
    "file_path": "sound/stream/anime/wav/go_anime_1a",
    "offset": 12345,
    "size": 67890,
    "samples": 44100,
    "channels": 2,
    "sample_rate": 48000,
    "loop_start": 0,
    "loop_end": 123456
}
```
At the end of the JSON, the raw dump of the STQR file is also included.

---

## Русская версия

### О STQR Converter

STQR Converter — инструмент для извлечения данных из STQR-файлов и конвертации их в формат JSON, а также обратной конвертации JSON в STQR. Это позволяет изменять звуковые данные в STQR-файлах и запаковывать их обратно.

### Возможности
- Извлекает данные из STQR и сохраняет в JSON
- Конвертирует JSON обратно в STQR
- Предоставляет информацию, такую как путь к файлу, смещение, размер, количество сэмплов, количество каналов, частоту дискретизации, начало и конец зацикливания
- Поддерживает редактирование и замену звуковых файлов

### Использование
#### Извлечение STQR в JSON:
```sh
stqr_converter extract input.stqr output.json
```

#### Конвертация JSON обратно в STQR:
```sh
stqr_converter pack input.json output.stqr
```

### Структура JSON
Каждый элемент JSON содержит:
```json
{
    "file_path": "sound/stream/anime/wav/go_anime_1a",
    "offset": 12345,
    "size": 67890,
    "samples": 44100,
    "channels": 2,
    "sample_rate": 48000,
    "loop_start": 0,
    "loop_end": 123456
}
```
В конце JSON также присутствует полный дамп STQR-файла.

---

### License / Лицензия
This project is licensed under the Apache License 2.0.
Проект распространяется под лицензией Apache 2.0.


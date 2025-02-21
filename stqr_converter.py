#!/usr/bin/env python3
import argparse
import json
import struct
import sys

def extract_directories(content):
    """
    Ищет блок директорий по маркеру: 8 нулевых байт + b'sound'.
    После маркера идут нуль-терминированные строки.
    Возвращает список директорий и позицию начала блока (после маркера), если найден.
    """
    marker = b'\x00' * 8 + b'sound'
    pos = content.find(marker)
    if pos == -1:
        return [], None

    start = pos + len(marker)
    directories = []
    buffer = bytearray()
    i = start
    while i < len(content):
        b = content[i]
        i += 1
        if b == 0:
            if len(buffer) >= 4:
                try:
                    directories.append(buffer.decode('latin1'))
                except Exception:
                    directories.append("")
            buffer = bytearray()
        else:
            buffer.append(b)
    return directories, pos

def build_directory_block(directories):
    """
    Собирает блок директорий: каждая строка кодируется (latin1) и завершается нулевым байтом.
    """
    block = bytearray()
    for d in directories:
        block.extend(d.encode('latin1'))
        block.append(0)
    return bytes(block)

def stqr_to_json(stqr_file, json_file):
    with open(stqr_file, 'rb') as f:
        content = f.read()

    if content[:4] != b'STQR':
        print("Ошибка: файл не содержит заголовка 'STQR'.")
        sys.exit(1)

    elements = {}
    i = 4  # после заголовка
    element_index = 0
    while i <= len(content) - 24:
        block = content[i:i+24]
        if block[8:12] == b'\x02\x00\x00\x00' and block[12:16] == b'\x80\xBB\x00\x00':
            size = struct.unpack('<i', block[0:4])[0]
            samples = struct.unpack('<i', block[4:8])[0]
            channels = struct.unpack('<i', block[8:12])[0]
            sample_rate = struct.unpack('<i', block[12:16])[0]
            loop_start = struct.unpack('<i', block[16:20])[0]
            loop_end = struct.unpack('<i', block[20:24])[0]
            elements[str(element_index)] = {
                "file_path": "",  # будет заполнено ниже
                "offset": i,
                "size": size,
                "samples": samples,
                "channels": channels,
                "sample_rate": sample_rate,
                "loop_start": loop_start,
                "loop_end": loop_end
            }
            element_index += 1
            i += 24
        else:
            i += 1

    # Извлекаем пути из блока директорий и распределяем их по элементам
    directories, _ = extract_directories(content)
    for idx, elem in elements.items():
        index = int(idx)
        if index < len(directories):
            elem["file_path"] = directories[index]
        else:
            elem["file_path"] = ""

    # Полный дамп файла в виде HEX‑строки
    raw_data = content.hex().upper()

    json_obj = {
        "elements": elements,
        "raw_data": raw_data
    }

    with open(json_file, 'w', encoding='utf-8') as f:
        json.dump(json_obj, f, indent=4, ensure_ascii=False)
    print(f"Конвертация STQR → JSON завершена: {json_file}")

def json_to_stqr(json_file, output_stqr_file):
    with open(json_file, 'r', encoding='utf-8') as f:
        data = json.load(f)

    if "raw_data" not in data:
        print("Ошибка: JSON не содержит поле 'raw_data'.")
        sys.exit(1)
    content = bytearray.fromhex(data["raw_data"])

    if "elements" not in data:
        print("Ошибка: JSON не содержит поле 'elements'.")
        sys.exit(1)
    for key, element in data["elements"].items():
        try:
            offset = element["offset"]
            block = (
                int(element["size"]).to_bytes(4, byteorder='little', signed=True) +
                int(element["samples"]).to_bytes(4, byteorder='little', signed=True) +
                int(element["channels"]).to_bytes(4, byteorder='little', signed=True) +
                int(element["sample_rate"]).to_bytes(4, byteorder='little', signed=True) +
                int(element["loop_start"]).to_bytes(4, byteorder='little', signed=True) +
                int(element["loop_end"]).to_bytes(4, byteorder='little', signed=True)
            )
            content[offset:offset+24] = block
        except Exception as e:
            print(f"Ошибка при обработке элемента {key}: {e}")
            continue

    # Собираем новый список директорий из file_path каждого элемента (сортировка по ключам)
    new_directories = []
    for key in sorted(data["elements"], key=lambda x: int(x)):
        fp = data["elements"][key].get("file_path", "")
        if fp:
            new_directories.append(fp)

    # Обновляем блок директорий
    marker = b'\x00' * 8 + b'sound'
    pos = content.find(marker)
    new_dir_block = build_directory_block(new_directories)
    if pos != -1:
        content = content[:pos + len(marker)] + new_dir_block
    else:
        content.extend(marker + new_dir_block)

    with open(output_stqr_file, 'wb') as f:
        f.write(content)
    print(f"Конвертация JSON → STQR завершена: {output_stqr_file}")

def main():
    parser = argparse.ArgumentParser(
        description="Конвертер STQR ⇆ JSON с полным дампом и путями в каждом элементе."
    )
    subparsers = parser.add_subparsers(dest='command', required=True)

    parser_stqr2json = subparsers.add_parser("stqr2json", help="Преобразовать STQR в JSON")
    parser_stqr2json.add_argument("stqr_file", help="Путь к входному STQR файлу")
    parser_stqr2json.add_argument("json_file", help="Путь к выходному JSON файлу")

    parser_json2stqr = subparsers.add_parser("json2stqr", help="Преобразовать JSON в STQR")
    parser_json2stqr.add_argument("json_file", help="Путь к входному JSON файлу")
    parser_json2stqr.add_argument("output_stqr_file", help="Путь к выходному STQR файлу")

    args = parser.parse_args()

    if args.command == "stqr2json":
        stqr_to_json(args.stqr_file, args.json_file)
    elif args.command == "json2stqr":
        json_to_stqr(args.json_file, args.output_stqr_file)

if __name__ == '__main__':
    main()

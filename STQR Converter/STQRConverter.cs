using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Newtonsoft.Json;

namespace STQRConverterApp
{
    public static class STQRConverter
    {
        public static void StqrToJson(string stqrFile, string jsonFile)
        {
            byte[] content = File.ReadAllBytes(stqrFile);

            if (!content.Take(4).SequenceEqual(Encoding.ASCII.GetBytes("STQR")))
            {
                Console.WriteLine("Ошибка: файл не содержит заголовка 'STQR'.");
                return;
            }

            var elements = new Dictionary<string, Dictionary<string, object>>();
            int i = 4;  // после заголовка
            int elementIndex = 0;

            while (i <= content.Length - 24)
            {
                byte[] block = content.Skip(i).Take(24).ToArray();
                if (block.Skip(8).Take(4).SequenceEqual(new byte[] { 0x02, 0x00, 0x00, 0x00 }) &&
                    block.Skip(12).Take(4).SequenceEqual(new byte[] { 0x80, 0xBB, 0x00, 0x00 }))
                {
                    int size = BitConverter.ToInt32(block, 0);
                    int samples = BitConverter.ToInt32(block, 4);
                    int channels = BitConverter.ToInt32(block, 8);
                    int sampleRate = BitConverter.ToInt32(block, 12);
                    int loopStart = BitConverter.ToInt32(block, 16);
                    int loopEnd = BitConverter.ToInt32(block, 20);

                    elements[elementIndex.ToString()] = new Dictionary<string, object>
                    {
                        { "file_path", "" },  // будет заполнено ниже
                        { "offset", i },
                        { "size", size },
                        { "samples", samples },
                        { "channels", channels },
                        { "sample_rate", sampleRate },
                        { "loop_start", loopStart },
                        { "loop_end", loopEnd }
                    };

                    elementIndex++;
                    i += 24;
                }
                else
                {
                    i++;
                }
            }

            // Извлекаем пути из блока директорий и распределяем их по элементам
            var (directories, _) = ExtractDirectories(content);
            foreach (var elem in elements)
            {
                int index = int.Parse(elem.Key);
                if (index < directories.Count)
                {
                    elem.Value["file_path"] = directories[index];
                }
                else
                {
                    elem.Value["file_path"] = "";
                }
            }

            // Полный дамп файла в виде HEX‑строки
            string rawData = BitConverter.ToString(content).Replace("-", "").ToUpper();

            var jsonObj = new
            {
                elements,
                raw_data = rawData
            };

            File.WriteAllText(jsonFile, JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented));
            Console.WriteLine($"Конвертация STQR → JSON завершена: {jsonFile}");
        }

        public static void JsonToStqr(string jsonFile, string outputStqrFile)
        {
            string jsonContent = File.ReadAllText(jsonFile);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

            if (!data.ContainsKey("raw_data"))
            {
                Console.WriteLine("Ошибка: JSON не содержит поле 'raw_data'.");
                return;
            }

            byte[] content = Convert.FromHexString((string)data["raw_data"]);

            if (!data.ContainsKey("elements"))
            {
                Console.WriteLine("Ошибка: JSON не содержит поле 'elements'.");
                return;
            }

            var elements = (Dictionary<string, Dictionary<string, object>>)data["elements"];
            foreach (var element in elements)
            {
                try
                {
                    int offset = (int)element.Value["offset"];
                    byte[] block = new byte[24];
                    Buffer.BlockCopy(BitConverter.GetBytes((int)element.Value["size"]), 0, block, 0, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes((int)element.Value["samples"]), 0, block, 4, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes((int)element.Value["channels"]), 0, block, 8, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes((int)element.Value["sample_rate"]), 0, block, 12, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes((int)element.Value["loop_start"]), 0, block, 16, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes((int)element.Value["loop_end"]), 0, block, 20, 4);

                    Array.Copy(block, 0, content, offset, 24);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Ошибка при обработке элемента {element.Key}: {e.Message}");
                    continue;
                }
            }

            // Собираем новый список директорий из file_path каждого элемента (сортировка по ключам)
            var newDirectories = elements.OrderBy(x => int.Parse(x.Key))
                                         .Select(x => x.Value.GetValueOrDefault("file_path", "").ToString())
                                         .Where(fp => !string.IsNullOrEmpty(fp))
                                         .ToList();

            // Обновляем блок директорий
            byte[] marker = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }.Concat(Encoding.ASCII.GetBytes("sound")).ToArray();
            int pos = IndexOfSequence(content, marker);
            byte[] newDirBlock = BuildDirectoryBlock(newDirectories);

            if (pos != -1)
            {
                var newContent = new List<byte>();
                newContent.AddRange(content.Take(pos + marker.Length));
                newContent.AddRange(newDirBlock);
                newContent.AddRange(content.Skip(pos + marker.Length));
                content = newContent.ToArray();
            }
            else
            {
                content = content.Concat(marker).Concat(newDirBlock).ToArray();
            }

            File.WriteAllBytes(outputStqrFile, content);
            Console.WriteLine($"Конвертация JSON → STQR завершена: {outputStqrFile}");
        }

        private static (List<string>, int?) ExtractDirectories(byte[] content)
        {
            byte[] marker = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }.Concat(Encoding.ASCII.GetBytes("sound")).ToArray();
            int pos = IndexOfSequence(content, marker);
            if (pos == -1)
            {
                return (new List<string>(), null);
            }

            int start = pos + marker.Length;
            var directories = new List<string>();
            var buffer = new List<byte>();
            int i = start;

            while (i < content.Length)
            {
                byte b = content[i];
                i++;
                if (b == 0)
                {
                    if (buffer.Count >= 4)
                    {
                        try
                        {
                            directories.Add(Encoding.Latin1.GetString(buffer.ToArray()));
                        }
                        catch
                        {
                            directories.Add("");
                        }
                    }
                    buffer.Clear();
                }
                else
                {
                    buffer.Add(b);
                }
            }

            return (directories, pos);
        }

        private static byte[] BuildDirectoryBlock(List<string> directories)
        {
            var block = new List<byte>();
            foreach (var d in directories)
            {
                block.AddRange(Encoding.Latin1.GetBytes(d));
                block.Add(0);
            }
            return block.ToArray();
        }

        private static int IndexOfSequence(byte[] buffer, byte[] pattern)
        {
            int i = Array.IndexOf(buffer, pattern[0]);
            while (i >= 0 && i <= buffer.Length - pattern.Length)
            {
                if (buffer.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    return i;
                }
                i = Array.IndexOf(buffer, pattern[0], i + 1);
            }
            return -1;
        }
    }
}

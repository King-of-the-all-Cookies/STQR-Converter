using System;
using System.IO;
using Newtonsoft.Json;

namespace STQRConverterApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Использование: STQRConverter <command> <input_file> <output_file>");
                Console.WriteLine("Команды: stqr2json, json2stqr");
                return;
            }

            string command = args[0];
            string inputFile = args[1];
            string outputFile = args[2];

            switch (command)
            {
                case "stqr2json":
                    STQRConverter.StqrToJson(inputFile, outputFile);
                    break;
                case "json2stqr":
                    STQRConverter.JsonToStqr(inputFile, outputFile);
                    break;
                default:
                    Console.WriteLine("Неизвестная команда: " + command);
                    break;
            }
        }
    }
}

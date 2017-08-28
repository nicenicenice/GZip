using System;

// 0. проверяем аргументы, переданные в командной строке
// в соответствии с их значением, запускаем одну из двух функций: Compress или Decompress

// 1. в каждой из этих функций создаем объекты типа Funnel, каждый объект инкапсулирует в себе поток два буффера 
// число объектов равно числу процессоров на данной машине
// в каждом из потоков запускаем соответствующие методы startCompress или startDecompress

// 2. далее запускаем бесконечный цикл, до тех пор, пока не будут полностью прочитаны данные из исходного файла
// в цикле поочередно запускаем 3-и функции, схожие по смыслу для компресси и для декомпрессии:
//      - "заполнить буффер данными из исходного файла"
//      - "сконвертировать данные (компрессия или декомпрессия)"
//      - "записать, полученный результат, в файл назначения"
// доступы к потокам ввода и вывода синхронизированы, параллельно происходит только преобразование данных

namespace GZip
{
    public class Program
    {
        public static int Main(string[] args)
        {
            int result = 1;
            try
            {
                if (args.Length < 3)
                {
                    throw new ArgumentException("Invalid arguments");
                }
                switch (args[0])
                {
                    case "compress":
                        result = Funnel.Compress(@args[1], @args[2]);
                        break;
                    case "decompress":
                        result = Funnel.Decompress(@args[1], @args[2]);
                        break;
                    default:
                        throw new ArgumentException("Invalid arguments");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return 0;
        }
    }
}
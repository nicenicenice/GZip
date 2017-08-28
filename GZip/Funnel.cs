using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZip
{
    public class Funnel
    {
        private static uint counter = 0;
        private static uint numPartDataForWrite = 0;
        private static int numThreads = Environment.ProcessorCount;
        private static long bufferSize = 8192;
        private static FileStream sourceFileStream;
        private static FileStream destinationFileStream;
        private static object _locker = new object();
        private static bool isCompress;

        private uint numPartDataContaint = 0;
        private byte[] buffer;
        private MemoryStream streamBuffer;
        private Thread thread;

        public static int NumThreads
        {
            get { return numThreads; }
            set { numThreads = value; }
        }

        public Funnel()
        {
            streamBuffer = new MemoryStream();

            if (isCompress)
                thread = new Thread(startCompress);
            else
                thread = new Thread(startDecompress);
            thread.Start();
            thread.Join();
        }

        public static int Compress(string inputFile, string outputFile)
        {
            Console.WriteLine("Processing, Please Wait...");
            try
            {
                sourceFileStream = new FileStream(@inputFile, FileMode.Open, FileAccess.Read);
                destinationFileStream = new FileStream(@outputFile, FileMode.Append, FileAccess.Write);
                isCompress = true;

                Funnel[] compressors = new Funnel[NumThreads];
                for (int i = 0; i < compressors.Length; ++i)
                    compressors[i] = new Funnel();
                return 0;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("File not found.");
                Console.WriteLine(e.StackTrace);
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return 1;
            }
            finally
            {
                DisposeData();
            }
        }

        public static int Decompress(string inputFile, string outputFile)
        {
            Console.WriteLine("Processing, Please Wait...");
            try
            {
                sourceFileStream = new FileStream(@inputFile, FileMode.Open, FileAccess.Read);
                destinationFileStream = new FileStream(@outputFile, FileMode.Append, FileAccess.Write);
                isCompress = false;

                Funnel[] decompressors = new Funnel[NumThreads];
                for (int i = 0; i < decompressors.Length; ++i)
                    decompressors[i] = new Funnel();
                return 0;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("File not found.");
                Console.WriteLine(e.StackTrace);
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return 1;
            }
            finally
            {
                DisposeData();
            }
        }

        public void startCompress()
        {
            try
            {
                while (true)
                {
                    lock (_locker)
                    {
                        if (sourceFileStream.Length == sourceFileStream.Position)
                        {
                            streamBuffer.Close();
                            return;
                        }
                        ToFillRawDataBuffer();
                    }
                    CompressPartData();
                    WriteConvertedDataToFile();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                if (streamBuffer != null)
                    streamBuffer.Close();
            }
        }

        public void startDecompress()
        {
            try
            {
                while (true)
                {
                    lock (_locker)
                    {
                        if (sourceFileStream.Length == sourceFileStream.Position)
                        {
                            streamBuffer.Close();
                            return;
                        }
                        ToFillDecompressedDataBuffer();
                    }
                    DecompressPartData();
                    WriteConvertedDataToFile();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                if (streamBuffer != null)
                    streamBuffer.Close();
            }
        }

        private void ToFillRawDataBuffer()
        {
            long curBufferSize = bufferSize;
            lock (_locker)
            {
                if (sourceFileStream.Length - sourceFileStream.Position < bufferSize)
                    curBufferSize = (int)(sourceFileStream.Length - sourceFileStream.Position);
            }

            buffer = new byte[curBufferSize];

            lock (_locker)
            {
                sourceFileStream.Read(buffer, 0, buffer.Length);
                numPartDataContaint = counter++;
            }
        }

        private void ToFillDecompressedDataBuffer()
        {
            // считываем длину порции данных
            byte[] affix = new byte[8];
            lock (_locker)
                sourceFileStream.Read(affix, 0, 8);

            int resBufferLength = BitConverter.ToInt32(affix, 4);
            buffer = new byte[resBufferLength + 1];
            affix.CopyTo(buffer, 0);

            lock (_locker)
            {
                sourceFileStream.Read(buffer, 8, resBufferLength - 8);
                numPartDataContaint = counter++;
            }
        }

        private void CompressPartData()
        {
            streamBuffer.SetLength(0);
            using (GZipStream compressionStream = new GZipStream(streamBuffer, CompressionMode.Compress, true))
            {
                compressionStream.Write(buffer, 0, buffer.Length);
            }
        }

        private void DecompressPartData()
        {
            using (MemoryStream bufferStream = new MemoryStream(buffer))
            {
                using (GZipStream decompressionStream = new GZipStream(bufferStream, CompressionMode.Decompress))
                {
                    streamBuffer.SetLength(0);
                    decompressionStream.CopyTo(streamBuffer);
                }
            }
        }

        private void WriteConvertedDataToFile()
        {
            byte[] convertedData = streamBuffer.ToArray();

            while (true)
            {
                if (numPartDataForWrite == numPartDataContaint)
                {
                    lock (_locker)
                    {
                        // записать длину порции данных
                        if (isCompress)
                            BitConverter.GetBytes(convertedData.Length).CopyTo(convertedData, 4);

                        destinationFileStream.Write(convertedData, 0, convertedData.Length);
                        numPartDataForWrite++;
                    }
                    break;
                }
            }
        }

        private static void DisposeData()
        {
            if (destinationFileStream != null)
                destinationFileStream.Close();
            if (sourceFileStream != null)
                sourceFileStream.Close();
        }
    }
}

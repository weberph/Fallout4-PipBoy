using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using PipBoy;

namespace PipBoyDump
{
    // Uses Install-Package CommandLineParser
    // http://commandline.codeplex.com/

    class Options
    {
        // Input: Network

        [Option('c', "connect", MutuallyExclusiveSet = "Input", HelpText = "Host to connect to.")]
        public string Host { get; set; }

        [Option('p', "port", DefaultValue = 27000, HelpText = "Port to connect to.")]
        public int Port { get; set; }

        // Input: File

        [Option('f', "file", MutuallyExclusiveSet = "Input", HelpText = "Input file (instead of ip/port).")]
        public string InputFile { get; set; }


        // Output

        [Option('r', "raw", HelpText = "File to write raw data received via network.")]
        public string RawFile { get; set; }

        [Option('g', "gameobjects", HelpText = "File to write the structured game objects.")]
        public string GameobjectsFile { get; set; }


        // Helper

        [HelpOption(HelpText = "Dispaly this help screen.")]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        [ParserState]
        public IParserState LastParserState { get; set; }   // required for automatic inclusion of error text in help screen.
    }

    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (new Parser(settings =>
            {
                settings.MutuallyExclusive = true;
                settings.HelpWriter = Console.Error;
            }).ParseArguments(args, options))
            {
                var inputFile = options.InputFile;
                if (inputFile != null && !File.Exists(inputFile))
                {
                    Console.Error.WriteLine("Error: input file not found.");
                    return;
                }

                var rawFile = options.RawFile;
                if (rawFile != null && !ValidateOutputFile(rawFile))
                {
                    Console.Error.WriteLine("Error: invalid output file name for raw data.");
                    return;
                }

                var gameobjectsFile = options.GameobjectsFile;
                if (gameobjectsFile != null && !ValidateOutputFile(gameobjectsFile))
                {
                    Console.Error.WriteLine("Error: invalid output file name for gameobjects.");
                }

                try
                {
                    Dump(options);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Unexpected error: " + e.Message);
                }
            }
        }

        public static void Dump(Options options)
        {
            using (var streamProvider = new PipBoyStreamProvider(options.RawFile))
            {
                Stream stream;

                if (options.Host != null)
                {
                    stream = streamProvider.Connect(options.Host, options.Port);
                }
                else if (options.InputFile != null)
                {
                    stream = streamProvider.ReadFile(options.InputFile);
                }
                else
                {
                    throw new ArgumentException();
                }

                using (stream)
                {
                    var dumper = new Dumper(options.GameobjectsFile);
                    var dumpThread = new Thread(_ => dumper.Dump(stream));
                    dumpThread.Start();

                    Console.WriteLine("Dump running... press key to exit");
                    while (dumpThread.IsAlive && !Console.KeyAvailable)
                    {
                        Thread.Sleep(100);
                    }
                    dumper.SignalStop();
                    dumpThread.Join();
                }
            }
        }

        private static bool ValidateOutputFile(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && (directory.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || !Directory.Exists(directory)))
            {
                return false;
            }
            var fileName = Path.GetFileName(filePath);
            return !string.IsNullOrWhiteSpace(fileName) && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
    }
}

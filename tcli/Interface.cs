
using Microsoft.AnalysisServices.Tabular;
using System.Text.Json;

using System;
using System.IO;

namespace tcli
{
    class Interface
    {

        static void Main(string[] args)
        {
            Program program = new Program(args);

            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a command.");
                return;
            }

            switch (args[0].ToLower())
            {
                case "dp":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Please provide a model deployment name.");
                        return;
                    }
                    program.Deploy();
                    break;
                case "pe":
                    program.PrintEnv();
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }
    }
}


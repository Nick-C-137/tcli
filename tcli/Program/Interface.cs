﻿
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
            MainLogic program = new MainLogic(args);
            
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a command.");
                return;
            }

            switch (args[0].ToLower())
            {
                case "deploy":
                    program.Deploy();
                    break;
                case "printenv":
                    program.PrintEnv();
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }
    }
}

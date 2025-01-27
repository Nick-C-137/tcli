
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
                    if (args.Length > 1) {
                        if (args[1] == "-r") {
                            program.DeployTabular(deploy_with_refresh: true);
                        } else {
                            Console.WriteLine("Unknow deployment flag.");
                        }
                    } else {
                        program.DeployTabular(deploy_with_refresh: false);
                    }
                    break;
                case "printenv":
                    program.PrintEnv();
                    break;
                case "list":
                    if (args.Length > 1) {
                        if (args[1] == "-p") {
                            if (args.Length > 2) {
                                if (args[2] == "-nonready") {
                                    program.ListPartitons(only_non_ready: true);
                                } else {
                                    Console.WriteLine("Unknow partition list flag.");
                                }
                            } else {
                                program.ListPartitons(only_non_ready: false);
                            }
                        } else {
                            Console.WriteLine("Unknow list flag.");
                        }
                    } else {
                        Console.WriteLine("The list opretation requrires a flag.");
                    }
                    break;
                    break;
                case "activate":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Please provide a model name.");
                        return;
                    }
                    program.ActivateTcliModel(args[1]);
                    break;
                case "refresh":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Please provide a model name.");
                        return;
                    }
                    program.PbiRefresh(args[1]);
                    break;
                case "query":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Please provide a model name.");
                        return;
                    }
                    if (args[1].ToLower().EndsWith(".dax"))
                    {
                        program.ExecuteDaxQuery(args[1]);
                        break;
                    } else if (args[1].ToLower().EndsWith(".sql"))
                    {
                        program.ExecuteSqlQuery(args[1]);
                        break;
                    }

                    
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }
    }
}


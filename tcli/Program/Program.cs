
using Microsoft.AnalysisServices.Tabular;
using System.Text.Json;

using System;
using System.IO;

namespace tcli {
    public class Program {
            
            private Models models = new Models();
            private string? env_azure_tenant_id;
            private string? env_azure_app_id;
            private string? env_azure_app_secret;
            private string[]? args;

            public Program(string[] args) {
                LoadEnvironment();
                this.args = args;
            }
            
            public void LoadEnvironment() {
                // Environment variable
                env_azure_tenant_id                         = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
                env_azure_app_id                            = Environment.GetEnvironmentVariable("AZURE_APP_ID");
                env_azure_app_secret                        = Environment.GetEnvironmentVariable("AZURE_APP_SECRET");
                if (env_azure_tenant_id == null)            {throw new Exception("AZURE_TENANT_ID environment variable not defined.."); }
                if (env_azure_app_id == null)               {throw new Exception("AZURE_APP_ID environment variable not defined.."); }
                if (env_azure_app_secret == null)           {throw new Exception("AZURE_APP_SECRET environment variable not defined.."); }
                
                models.LoadModels(Directory.GetCurrentDirectory() + "/tcli-models.json");
            }

            public void PrintEnv() {
                Console.WriteLine("");
                Console.WriteLine("Environment Variables:");
                Console.WriteLine("\tAZURE_TENANT_ID: " + env_azure_tenant_id);
                Console.WriteLine("\tAZURE_APP_ID: " + env_azure_app_id);
                Console.WriteLine("\tAZURE_APP_SECRET: ****");
                Console.WriteLine();
                Console.WriteLine("Models:");
                foreach (var model in models.ModelsDictionary) {
                    Console.WriteLine("\t" + model.Key + ":");
                    Console.WriteLine("\t\tPbiWorkspaceString: " + model.Value.PbiWorkspaceString);
                    Console.WriteLine("\t\tPbiSemanticModelName: " + model.Value.PbiSemanticModelName);
                    Console.WriteLine("\t\tTmdlPath: " + model.Value.TmdlPath);
                    Console.WriteLine();
                }
                return;
            }

            public void Deploy() {
                Model deployModel = models.ModelsDictionary[args[1]];
                var PbiWorkspaceString = deployModel.PbiWorkspaceString;
                var PbiSemanticModelName = deployModel.PbiSemanticModelName;
                var TmdlPath = deployModel.TmdlPath;
                Server server = new Server();
                return;
            }

        }
}
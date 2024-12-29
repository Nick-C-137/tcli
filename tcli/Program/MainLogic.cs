
using Microsoft.AnalysisServices.Tabular;
using Microsoft.PowerBI.Api;
using Microsoft.Identity.Client;
using System;
using System.Text;
using CsvHelper;
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Diagnostics;

namespace tcli {
    public class MainLogic {
            
            private TcliModels models = new TcliModels();
            private EnvVariables env_variables = new EnvVariables();
            private TcliModel active_tcli_model;
            private string config_dir = "/tcli/_config";
            private string[]? args;
            private Server server = new Server();

            public MainLogic(string[] args) {
                
                Initialize();
                LoadEnvironment();
                this.args = args;
            }

            public void ActivateTcliModel (string modelName) {
                foreach (var model in models.ModelsDictionary) {
                    model.Value.IsActive = false;
                }
                if (models.ModelsDictionary.ContainsKey(modelName)) {
                    models.ModelsDictionary[modelName].IsActive = true;
                    models.SaveModels(Directory.GetCurrentDirectory() + config_dir + "/tcli-models.json");
                    Console.WriteLine("");
                    Console.WriteLine("Model activated - see new environment below... ");
                    PrintEnv();
                } else {
                    Console.WriteLine("");
                    Console.WriteLine("Model not found: " + modelName);
                    Console.WriteLine("");
                }
            }

            private void LoadEnvironment() {
                
                // Load environment variables
                env_variables.LoadEnvVariables(Directory.GetCurrentDirectory() + config_dir + "/tcli-env-variables.json");

                if (env_variables.AZURE_TENANT_ID == null)            {throw new Exception("AZURE_TENANT_ID environment variable not defined.."); }
                if (env_variables.AZURE_APP_ID == null)               {throw new Exception("AZURE_APP_ID environment variable not defined.."); }
                if (env_variables.AZURE_APP_SECRET == null)           {throw new Exception("AZURE_APP_SECRET environment variable not defined.."); }

                // Load default model
                models.LoadModels(Directory.GetCurrentDirectory() + config_dir + "/tcli-models.json");
                
                var avtiveModelsLoaded = new List<TcliModel>();

                foreach (var model in models.ModelsDictionary) {
                    if (model.Value.IsActive) {
                        avtiveModelsLoaded.Add(model.Value);
                    }
                }

                if (avtiveModelsLoaded.Count == 0) {
                    throw new Exception("No active model found.");
                }

                if (avtiveModelsLoaded.Count > 1) {
                    throw new Exception("Multiple active models found.");
                }

                active_tcli_model = avtiveModelsLoaded[0];

                
            }
            
            private void ConnectToServer() {
                string connectionString = 
                    $"DataSource={active_tcli_model.PBI_WORKSPACE_STRING};" +
                    $"User ID=app:{env_variables.AZURE_APP_ID}@{env_variables.AZURE_TENANT_ID};" +
                    $"Password={env_variables.AZURE_APP_SECRET};";

                // Connect to the Power BI workspace referenced in connect string
                Console.WriteLine("Connecting to Power BI workspace...");
                Console.WriteLine("");
                server.Connect(connectionString);
            }
            
            public void Initialize() {
                var configDirectoryPath = Directory.GetCurrentDirectory() + config_dir;
                
                // Config directory
                if (!Directory.Exists(configDirectoryPath)) {
                    Directory.CreateDirectory(configDirectoryPath);
                    Console.WriteLine("Config directory initialzed.");
                }
                var tcliModelsFilePath = configDirectoryPath + "/tcli-models.json";
                if (!File.Exists(tcliModelsFilePath)) {
                    File.WriteAllText(tcliModelsFilePath, GetStrings.ModelJson());
                }

                var tcliEnvVariablesFilePath = configDirectoryPath + "/tcli-env-variables.json";
                if (!File.Exists(tcliEnvVariablesFilePath)) {
                    File.WriteAllText(tcliEnvVariablesFilePath, GetStrings.EnvVariablesJson());
                }

                // Dax directory
                configDirectoryPath = Directory.GetCurrentDirectory() + "/tcli/queries";
                if (!Directory.Exists(configDirectoryPath)) {
                    Directory.CreateDirectory(configDirectoryPath);
                }
            }

            public void PrintEnv() {
                Console.WriteLine("");
                Console.WriteLine("Environment Variables:");
                Console.WriteLine("\tAZURE_TENANT_ID: " + env_variables.AZURE_TENANT_ID);
                Console.WriteLine("\tAZURE_APP_ID: " + env_variables?.AZURE_APP_ID);
                Console.WriteLine("\tAZURE_APP_SECRET: ****");
                Console.WriteLine();
                Console.WriteLine("Models:");
                foreach (var model in models.ModelsDictionary) {
                    Console.WriteLine("\t" + model.Key + ":");
                    Console.WriteLine("\t\tIsActive: " + model.Value.IsActive);
                    Console.WriteLine("\t\tPbiWorkspaceString: " + model.Value.PBI_WORKSPACE_STRING);
                    Console.WriteLine("\t\tPbiSemanticModelName: " + model.Value.PBI_SEMANTIC_MODEL_NAME);
                    Console.WriteLine("\t\tTmdlPath: " + model.Value.TMDL_PATH);
                    Console.WriteLine();
                }
                return;
            }

            public void Deploy() {
               

                Console.WriteLine("");
                Console.WriteLine("Deploying model: " + active_tcli_model.PBI_SEMANTIC_MODEL_NAME);
                Console.WriteLine("To workspace: " + active_tcli_model.PBI_WORKSPACE_STRING);
                Console.WriteLine("Using TMDL path: " + active_tcli_model.TMDL_PATH);
                Console.WriteLine("");

                ConnectToServer();
                Database remote_model;

                // Serialize the TMDL file
                Console.WriteLine("Serializing TMDL...");
                Console.WriteLine("");
                var local_model = TmdlSerializer.DeserializeModelFromFolder(active_tcli_model.TMDL_PATH);

                // Returns a mutation of the dataset name if it already exists e.g. [datasetName] - 1
                string new_model_name = server.Databases.GetNewName(active_tcli_model.PBI_SEMANTIC_MODEL_NAME);

                // Create new dataset if it does not already exist
                if (new_model_name == active_tcli_model.PBI_SEMANTIC_MODEL_NAME) {
                    Console.WriteLine("Creating new model as it does not exist...");
                    Console.WriteLine("");
                    remote_model = new Database() {
                        Name = active_tcli_model.PBI_SEMANTIC_MODEL_NAME,
                        Model = new Model()
                    };
                    server.Databases.Add(remote_model);
                    remote_model.Update(Microsoft.AnalysisServices.UpdateOptions.ExpandFull);
                } else {
                    remote_model = server.Databases.GetByName(active_tcli_model.PBI_SEMANTIC_MODEL_NAME);
                }

                // Deploy model
                Console.WriteLine("Copying local tmdl model to remote tabular model object...");
                Console.WriteLine("");
                local_model.CopyTo(remote_model.Model);

                Console.WriteLine("Saving changges to remote tabular model object...");
                Console.WriteLine("");
                remote_model.Model.SaveChanges();


                return;
            }

            public void ExecuteDaxQuery(string filePath) {
                
                 if (!File.Exists(filePath)) {
                    throw new FileNotFoundException("DAX query file not found: " + filePath);
                }

                string daxQuery = File.ReadAllText(filePath);

                string tenantId = env_variables.AZURE_TENANT_ID;
                string clientId = env_variables.AZURE_APP_ID;
                string clientSecret = env_variables.AZURE_APP_SECRET;

                // Initialize the authentication context
                var app = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                    .Build();

                // Acquire an access token
                var result = app.AcquireTokenForClient(new[] { "https://analysis.windows.net/powerbi/api/.default" }).ExecuteAsync().Result;
                var accessToken = result.AccessToken;

                using (HttpClient client = new HttpClient())
                {
                    // Define the URL
                    string url = $"https://api.powerbi.com/v1.0/myorg/datasets/{active_tcli_model.PBI_SEMANTIC_MODEL_ID}/executeQueries";

                    var jsonPayload = GetStrings.DaxQueryJsonPayload(daxQuery);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    // Make the POST request
                    HttpResponseMessage response = client.PostAsync(url, content).Result;
                    
                    try
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error executing DAX query: " + e.Message);
                        var errorResult = response.Content.ReadAsStringAsync().Result;
                        var formattedErrorResult = System.Text.Json.JsonSerializer.Serialize(JsonDocument.Parse(errorResult), new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine($"Response: \n {formattedErrorResult}");
                        return;
                    }
                

                    // Read the response content
                    string responseBody = response.Content.ReadAsStringAsync().Result;

                    

                    var serialized = System.Text.Json.JsonSerializer.Deserialize<DaxQueryResult>(responseBody);
                    var rows = serialized.results[0].tables[0].rows;

                    if (rows == null || !rows.Any())
                    {
                        Console.WriteLine("No rows returned.");
                        return;
                    }   

                    var csv = new StringBuilder();

                    var header = rows[0].Keys;
                    
                    var header_count = 0;
                    foreach (var key in header)
                    {
                        var clean_value = "\"" + key.Split("[")[1].Replace("]", "") + "\"";

                        if (header_count == header.Count - 1)
                        {
                            csv.Append(clean_value);
                        }
                        else
                        {
                            csv.Append(clean_value + ",");
                        }
                        header_count++;
                    }

                    csv.AppendLine();

                    foreach (var row in rows)
                    {
                        var values = row.Values;
                        var line = new StringBuilder();
                        var count = 0;
                        foreach (var jsonElement in values)
                        {
                            
                            var string_value = jsonElement + "";

                            var string_value_clean = "\"" + string_value.Replace("\"", "'").Replace("\n", " \\n ").Replace("    ", " \\t ") + "\"";

                            if (count == values.Count - 1)
                            {
                                line.Append(string_value_clean);
                            }
                            else
                            {
                                line.Append(string_value_clean + ",");
                            }

                            count++;
                        
                        }
                        csv.AppendLine(line.ToString());
                    }

                    File.WriteAllText($"{filePath}.csv", csv.ToString());
                    
                    File.WriteAllText($"{filePath}.raw.json", responseBody);
                }
            
                // Command to execute
                string command = "code";
                string arguments = $"{filePath}.csv"; // Example arguments (optional)

                // Create a process start info
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = command, // Command or executable
                    Arguments = arguments, // Command arguments
                    RedirectStandardOutput = true, // Capture the output
                    RedirectStandardError = true, // Capture errors
                    UseShellExecute = false, // Don't use the OS shell
                    CreateNoWindow = true // Don't create a terminal window
                };

                // Start the process
                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();

                    // Read the output and errors
                    string output = process.StandardOutput.ReadToEnd();
                    string errors = process.StandardError.ReadToEnd();

                    process.WaitForExit(); // Ensure the process has completed

                    if (!string.IsNullOrEmpty(errors))
                    {
                        Console.WriteLine("Errors:");
                        Console.WriteLine(errors);
                    }
                }

            }
                
            
                
        }
}
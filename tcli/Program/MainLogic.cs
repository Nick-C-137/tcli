
using Microsoft.AnalysisServices.Tabular;
using Microsoft.PowerBI.Api;
using Microsoft.Identity.Client;
using System;
using System.Text;
using CsvHelper;
using System.Globalization;
using Newtonsoft.Json.Linq;

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
                configDirectoryPath = Directory.GetCurrentDirectory() + "/tcli/dax";
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
                        Console.WriteLine("Response: " + response.Content.ReadAsStringAsync().Result);
                        return;
                    }
                

                    // Read the response content
                    string responseBody = response.Content.ReadAsStringAsync().Result;

                    

                    var serialized = System.Text.Json.JsonSerializer.Deserialize<DaxQueryResult>(responseBody);
                    var rows = serialized.results[0].tables[0].rows;

                    foreach (var row in rows)
                    {
                        foreach (var item in row)
                        {
                            Console.WriteLine(item.Key + ": " + item.Value);
                        }
                    }
                    
                    File.WriteAllText($"{filePath}.raw.json", responseBody);
                }
            }
                
            
                
        }
}
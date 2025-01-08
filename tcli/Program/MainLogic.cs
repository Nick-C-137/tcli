
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
using System.Data.Odbc;
using System.Text.RegularExpressions;
using System.ComponentModel.Design;


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
                    Console.WriteLine("\t\tDbType: " + model.Value.DB_TYPE);
                    Console.WriteLine("\t\tDbConnectionString: " + model.Value.DB_CONNECTION_STRING);
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
                    remote_model.Model.Culture = local_model.Culture; // Culture can onlty be defined at creation time
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

                var jsonPayload = GetStrings.DaxQueryJsonPayload(daxQuery);
                string url = $"https://api.powerbi.com/v1.0/myorg/datasets/{active_tcli_model.PBI_SEMANTIC_MODEL_ID}/executeQueries";
                string responseBody = Helpers.RequestPbiApi(env_variables, url, jsonPayload, Helpers.HttpRequestType.POST);

                var serialized = System.Text.Json.JsonSerializer.Deserialize<DaxQueryResult>(responseBody);
                var rows = serialized.results[0].tables[0].rows;

                if (rows == null || !rows.Any())
                {
                    Console.WriteLine("No rows returned.");
                    return;
                }   

                var table = new List<object[]>();
                var row_number = 0;

                foreach (var row in rows)
                {
                    var row_values = new List<object>();
                    if (row_number == 0) // Write Keys on first row to extract headers
                    {
                        foreach (var element in row)
                        {
                            var headerColValue = element.Key.Split("[")[1].Replace("]", ""); // Remove table name from header (DAX query result format)
                            row_values.Add(headerColValue);
                        }
                        table.Add(row_values.ToArray());
                        row_values.Clear();

                        // Then add values
                        foreach (var element in row)
                        {
                            row_values.Add(element.Value  + ""); // Only implicit cast to string of JsonElement works for some reason
                        }
                    } else {
                        foreach (var element in row)
                        {
                            row_values.Add(element.Value  + ""); // Only implicit cast to string of JsonElement works for some reason
                        }
                    }
                    
                    table.Add(row_values.ToArray());
                    row_number++;
                }

                Helpers.WriteCSV(filePath, table);

                File.WriteAllText($"{filePath}.raw.json", responseBody);
                

                string result_file_path = $"{filePath}.csv";
                Helpers.OpenFileInVsCode(result_file_path);

            }
            
            public void ExecuteSqlQuery(string filePath) {
                
                bool is_select_request = false;

                if (!File.Exists(filePath)) {
                    throw new FileNotFoundException("SQL query file not found: " + filePath);
                }

                string sqlQuery = File.ReadAllText(filePath);
                string describeQuery = "";

                // Check if query contains select request
                string text_to_be_replaced = "";
                string pattern = @"--# select\s+(.*?)\s+#";
                string extractedTableName = "";
                Match match = Regex.Match(sqlQuery, pattern);
                if (match.Success) {
                    extractedTableName = match.Groups[1].Value;
                    describeQuery = $"DESCRIBE {extractedTableName}";
                    is_select_request = true;
                    text_to_be_replaced = $"--# select {extractedTableName} #";
                }

                // DSN-less connection string
                string connectionString = active_tcli_model.DB_CONNECTION_STRING;

                using (OdbcConnection connection = new OdbcConnection(connectionString))
                {
                    
                        connection.Open();

                        string query;

                        if (is_select_request){
                            query = describeQuery;
                        } else {
                            query = sqlQuery;
                        }

                        OdbcCommand command = new OdbcCommand(query, connection);

                        using (OdbcDataReader reader = command.ExecuteReader())
                        {

                            var table = new List<object[]>();
                            var headerWritten = false;

                            while (reader.Read())
                            {
                                var row_values = new List<object>();
                                if (!headerWritten)
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        row_values.Add(reader.GetName(i).ToString());
                                    }
                                    table.Add(row_values.ToArray());
                                    row_values.Clear();

                                    // Then add values
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        row_values.Add(reader[i].ToString());
                                    }

                                    headerWritten = true;
                                } else {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        row_values.Add(reader[i].ToString());
                                    }
                                }

                                table.Add(row_values.ToArray());
                            }

                            if (is_select_request) {
                                string select_statement = "SELECT";
                                
                                var row_num = 0;
                                
                                foreach (var row in table) {
                                    Console.WriteLine(row[0]);
                                    if (row_num == 0) {
                                        // Do nothing 
                                    } else if (row_num == 1) {
                                        select_statement += $"\n\t\t `{row[0]}` -- {row[1]}"; // {column name} -- {column_type}
                                    } else {
                                        select_statement += $"\n\t\t,`{row[0]}` -- {row[1]}"; // {column name} -- {column_type}
                                    }
                                    row_num++;
                                }
                                
                                select_statement += "\n\tFROM";
                                select_statement += $"\n\t\t{extractedTableName}";
                                sqlQuery = sqlQuery.Replace(text_to_be_replaced, select_statement);
                                File.WriteAllText(filePath, sqlQuery);

                            } else {
                                Helpers.WriteCSV(filePath, table);    
                                string result_file_path = $"{filePath}.csv";
                                Helpers.OpenFileInVsCode(result_file_path); 
                            }

                        }
                    
                }

                

            }
        
            public void PbiRefresh(string argument) {
                
                if (active_tcli_model.RefreshDefinitions.TryGetValue(argument, out RefreshDefinition refresh_definition)) {
                    
                    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(refresh_definition);
                    Console.WriteLine($"\nStarted refresh of semantic model: {active_tcli_model.PBI_SEMANTIC_MODEL_NAME} \n");
                    var url = $"https://api.powerbi.com/v1.0/myorg/groups/{active_tcli_model.PBI_WORKSPACE_ID}/datasets/{active_tcli_model.PBI_SEMANTIC_MODEL_ID}/refreshes";
                    var response = Helpers.RequestPbiApi(env_variables, url, jsonPayload, Helpers.HttpRequestType.POST);
                    Console.WriteLine(response);

                } else {
                    if (argument == "get") {
                        var url = $"https://api.powerbi.com/v1.0/myorg/groups/{active_tcli_model.PBI_WORKSPACE_ID}/datasets/{active_tcli_model.PBI_SEMANTIC_MODEL_ID}/refreshes";
                        var response = Helpers.RequestPbiApi(env_variables, url, "", Helpers.HttpRequestType.GET);
                        var deserializedResponse = System.Text.Json.JsonSerializer.Deserialize<PbiRefreshList>(response);
                        Console.WriteLine($"Refresh history for semantic model: {active_tcli_model.PBI_SEMANTIC_MODEL_NAME}: \n");
                        var resultString = "";
                        var display_count = 1;
                        foreach (var refresh_result in deserializedResponse.value) {
                            resultString += $"Request ID: {refresh_result.requestId} \n";
                                resultString += $"\tid: {refresh_result.id} \n";
                                resultString += $"\trefreshType: {refresh_result.refreshType} \n";
                                resultString += $"\tstartTime: {refresh_result.startTime} \n";
                                resultString += $"\tendTime: {refresh_result.endTime} \n";
                                resultString += $"\tstatus: {refresh_result.status} \n";
                                resultString += $"\textendedStatus: {refresh_result.extendedStatus} \n";
                                resultString += $"\tRefresh Attempts: \n";
                                    foreach (var attempt in refresh_result.refreshAttempts) {
                                        resultString += $"\t\tattemptId: {attempt.attemptId} \n";
                                        resultString += $"\t\tstartTime: {attempt.startTime} \n";
                                        resultString += $"\t\tendTime: {attempt.endTime} \n";
                                        resultString += $"\t\ttype: {attempt.type} \n";
                                    }
                                resultString += "\n";
                            if (display_count == 1) {break;}
                            display_count --;
                        }
                        Console.WriteLine(resultString);
                    } else if (argument == "stop") {
                        var url = $"https://api.powerbi.com/v1.0/myorg/groups/{active_tcli_model.PBI_WORKSPACE_ID}/datasets/{active_tcli_model.PBI_SEMANTIC_MODEL_ID}/refreshes";
                        var response = Helpers.RequestPbiApi(env_variables, url, "", Helpers.HttpRequestType.GET);
                        var deserializedResponse = System.Text.Json.JsonSerializer.Deserialize<PbiRefreshList>(response);
                        var refresh_request_id = deserializedResponse.value[0].requestId;
                        Console.WriteLine($"Stopped refresh with request id: {refresh_request_id} \n");
                        url = $"https://api.powerbi.com/v1.0/myorg/groups/{active_tcli_model.PBI_WORKSPACE_ID}/datasets/{active_tcli_model.PBI_SEMANTIC_MODEL_ID}/refreshes/{refresh_request_id}";
                        response = Helpers.RequestPbiApi(env_variables, url, "", Helpers.HttpRequestType.DELETE);
                        Console.WriteLine(response);

                    } else {
                        Console.WriteLine("No valid arguments provided for command 'refresh' - valid ones are 'start', 'get' and '[refresh_definition_name]' ");
                    }
                }

            }
        }
                
    }

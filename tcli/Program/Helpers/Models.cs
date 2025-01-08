using System.Text.Json;

namespace tcli {
    public class TcliModel
    {
        public bool IsActive { get; set; }
        public string? PBI_WORKSPACE_STRING { get; set; }
        public string? PBI_WORKSPACE_ID { get; set; }
        public string? PBI_SEMANTIC_MODEL_NAME { get; set; }
        public string? PBI_SEMANTIC_MODEL_ID { get; set; }
        public string? TMDL_PATH { get; set; }
        public string? DB_TYPE { get; set; }
        public string? DB_CONNECTION_STRING { get; set; }
        public Dictionary<string, RefreshDefinition>? RefreshDefinitions { get; set; }
    }

    public class RefreshDefinition {
        public string? type {get; set;}
        public string? commitMode {get; set;}
        public int? maxParallelism {get; set;}
        public int? retryCount {get; set;}
        public string? timeout {get; set;}
        public List<RefreshTable>? tables {get; set;} 
    }

    public class RefreshTable {
        string? TableName {get; set;}
        string? PartitionName {get; set;}
    }

    public class TcliModels
    {
        public Dictionary<string, TcliModel>? ModelsDictionary { get; set; }
        public void LoadModels(string modelsPath)
        {
            string modelsJson = File.ReadAllText(modelsPath);
            ModelsDictionary = JsonSerializer.Deserialize<Dictionary<string, TcliModel>>(modelsJson);
            if (ModelsDictionary == null) { throw new Exception("Models could not be loaded."); }

            foreach (var model in ModelsDictionary.Values)
            {
            if (string.IsNullOrEmpty(model.PBI_WORKSPACE_STRING) || 
                string.IsNullOrEmpty(model.PBI_WORKSPACE_ID) || 
                string.IsNullOrEmpty(model.PBI_SEMANTIC_MODEL_NAME) || 
                string.IsNullOrEmpty(model.PBI_SEMANTIC_MODEL_ID) || 
                string.IsNullOrEmpty(model.TMDL_PATH))
            {
                throw new Exception("One or more model properties are null or empty.");
            }
            }
        }

        public void SaveModels(string modelsPath)
        {
            string modelsJson = JsonSerializer.Serialize(ModelsDictionary, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(modelsPath, modelsJson);
        }
    }

    
}
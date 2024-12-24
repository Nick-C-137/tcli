using System.Text.Json;

namespace tcli {
    public class TcliModel
    {
        public bool IsActive { get; set; }
        public string? PBI_WORKSPACE_STRING { get; set; }
        public string? PBI_SEMANTIC_MODEL_NAME { get; set; }
        public string? TMDL_PATH { get; set; }
    }

    public class TcliModels
    {
        public Dictionary<string, TcliModel>? ModelsDictionary { get; set; }
        public void LoadModels(string modelsPath)
        {
            string modelsJson = File.ReadAllText(modelsPath);
            ModelsDictionary = JsonSerializer.Deserialize<Dictionary<string, TcliModel>>(modelsJson);
            if (ModelsDictionary == null) { throw new Exception("Models could not be loaded."); }
        }

        public void SaveModels(string modelsPath)
        {
            string modelsJson = JsonSerializer.Serialize(ModelsDictionary, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(modelsPath, modelsJson);
        }
    }

    
}
using System.Text.Json;

namespace tcli {
    public class Model
    {
        public string? PBI_WORKSPACE_STRING { get; set; }
        public string? PBI_SEMANTIC_MODEL_NAME { get; set; }
        public string? TMDL_PATH { get; set; }
    }

    public class Models
    {
        public Dictionary<string, Model>? ModelsDictionary { get; set; }
        public void LoadModels(string modelsPath)
        {
            string modelsJson = File.ReadAllText(modelsPath);
            ModelsDictionary = JsonSerializer.Deserialize<Dictionary<string, Model>>(modelsJson);
            if (ModelsDictionary == null) { throw new Exception("Models could not be loaded."); }
        }
    }

    
}
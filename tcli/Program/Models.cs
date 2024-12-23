using System.Text.Json;

namespace tcli {
    public class Model
    {
        public string? PbiWorkspaceString { get; set; }
        public string? PbiSemanticModelName { get; set; }
        public string? TmdlPath { get; set; }
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
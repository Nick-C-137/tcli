using System.Text.Json;

namespace tcli {
    class EnvVariables {
        public string? AZURE_TENANT_ID { get; set; }
        public string? AZURE_APP_ID { get; set; }
        public string? AZURE_APP_SECRET { get; set; }
    

    public void LoadEnvVariables(string envVariablesPath) {
        string envVariablesJson = File.ReadAllText(envVariablesPath);
        EnvVariables? envVariables = JsonSerializer.Deserialize<EnvVariables>(envVariablesJson);
        if (envVariables == null) { throw new Exception("Env variables could not be loaded."); }
        AZURE_TENANT_ID = envVariables.AZURE_TENANT_ID;
        AZURE_APP_ID = envVariables.AZURE_APP_ID;
        AZURE_APP_SECRET = envVariables.AZURE_APP_SECRET;
    }

    }

}

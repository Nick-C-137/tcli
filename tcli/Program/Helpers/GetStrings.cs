namespace tcli {
class GetStrings {

public static string ModelJson() {
    return 
@"{
    ""modelName1"" : {
        ""IsActive"": true,
        ""PBI_WORKSPACE_STRING"": ""input_workspace_string1"",
        ""PBI_SEMANTIC_MODEL_NAME"": ""input_model_name1"",
        ""PBI_SEMANTIC_MODEL_ID"": ""input_model_id1"",
        ""TMDL_PATH"": ""input_tmdl_path1""
    },
    ""modelName2"" : {
        ""IsActive"": false,
        ""PBI_WORKSPACE_STRING"": ""input_workspace_string2"",
        ""PBI_SEMANTIC_MODEL_NAME"": ""input_model_name2"",
        ""PBI_SEMANTIC_MODEL_ID"": ""input_model_id2"",
        ""TMDL_PATH"": ""input_tmdl_path2""
    }
}
";
}

public static string EnvVariablesJson() {
    return 
@"{
    ""AZURE_TENANT_ID"": """",
    ""AZURE_APP_ID"": """",
    ""AZURE_APP_SECRET"": """"
}
";
}

public static string DaxQueryJsonPayload(string daxQuery) {
    daxQuery = daxQuery.Replace("\"", "\\\"");
    return $"{{\"queries\":[{{\"query\":\"{daxQuery}\"}}],\"serializerSettings\":{{\"includeNulls\":true}}}}";
}

}
}
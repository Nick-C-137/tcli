namespace tcli {
class GetStrings {

public static string ModelJson() {
    return 
@"{
    ""modelName1"" : {
        ""IsActive"": true,
        ""PBI_WORKSPACE_STRING"": ""input_workspace_string"",
        ""PBI_WORKSPACE_ID"": ""input_workspace_id"",
        ""PBI_SEMANTIC_MODEL_NAME"": ""input_model_name"",
        ""PBI_SEMANTIC_MODEL_ID"": ""input_model_id"",
        ""TMDL_PATH"": ""input_tmdl_path"",
        ""DB_TYPE"": ""Databricks"",
        ""DB_CONNECTION_STRING"": ""input_db_connection_string"",
        ""RefreshDefinitions"": {
            ""full"": {
                ""type"": ""Full"",
                ""commitMode"": ""transactional"",
                ""maxParallelism"": 8,
                ""retryCount"": 0,
                ""timeout"": ""05:00:00"",
                ""tables"": null
            },
            ""specific"": {
                ""type"": ""Full"",
                ""commitMode"": ""transactional"",
                ""maxParallelism"": 8,
                ""retryCount"": 0,
                ""timeout"": ""05:00:00"",
                ""tables"": null
            }
        }
    },
    ""modelName2"" : {
        ""IsActive"": false,
        ""PBI_WORKSPACE_STRING"": ""input_workspace_string"",
        ""PBI_WORKSPACE_ID"": ""input_workspace_id"",
        ""PBI_SEMANTIC_MODEL_NAME"": ""input_model_name"",
        ""PBI_SEMANTIC_MODEL_ID"": ""input_model_id"",
        ""TMDL_PATH"": ""input_tmdl_path"",
        ""DB_TYPE"": ""Databricks"",
        ""DB_CONNECTION_STRING"": ""input_db_connection_string"",
        ""RefreshDefinitions"": {
            ""full"": {
                ""type"": ""Full"",
                ""commitMode"": ""transactional"",
                ""maxParallelism"": 8,
                ""retryCount"": 0,
                ""timeout"": ""05:00:00"",
                ""tables"": null
            },
            ""specific"": {
                ""type"": ""Full"",
                ""commitMode"": ""transactional"",
                ""maxParallelism"": 8,
                ""retryCount"": 0,
                ""timeout"": ""05:00:00"",
                ""tables"": null
            }
        }
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
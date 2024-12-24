namespace tcli {
class GetStrings {

public static string ModelJson() {
    return 
@"{
    ""modelName1"" : {
        ""IsActive"": true,
        ""PBI_WORKSPACE_STRING"": """",
        ""PBI_SEMANTIC_MODEL_NAME"": """",
        ""TMDL_PATH"": """"
    },
    ""modelName2"" : {
        ""IsActive"": false,
        ""PBI_WORKSPACE_STRING"": """",
        ""PBI_SEMANTIC_MODEL_NAME"": """",
        ""TMDL_PATH"": """"
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

}
}
namespace tcli {
class GetStrings {

public static string ModelJson() {
    return @"
{
    ""modelName1"" : {
        ""PBI_WORKSPACE_STRING"": ""powerbi://api.powerbi.com/v1.0/myorg/workspace,"",
        ""PBI_SEMANTIC_MODEL_NAME"": ""modelName"",
        ""TMDL_PATH"": ""tmdlPath""
    },
    ""modelName2"" : {
        ""PBI_WORKSPACE_STRING"": ""powerbi://api.powerbi.com/v1.0/myorg/workspace,"",
        ""PBI_SEMANTIC_MODEL_NAME"": ""modelName"",
        ""TMDL_PATH"": ""tmdlPath""
    }
}
";
}

}
}
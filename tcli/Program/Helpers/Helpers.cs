using System.Diagnostics;
using System.Text;
using CsvHelper;
using Microsoft.Identity.Client;
using System.Text.Json;
using System.Net.Cache;

namespace tcli {
    class Helpers {

        public enum HttpRequestType 
        {
            POST,
            GET,
            DELETE
        }

        public static void WriteCSV (string filePath, List<object[]> table) {

                    
            var csv = new StringBuilder();
            var row_number = 0;

            foreach (var row in table)
            {;
                
                var line = new StringBuilder();
                var col_number = 0;

                foreach (var value in row)
                {

                    var dynamic_value = (dynamic) value;
                    string string_value;

                    // First cast to string so we can work with the value when parsing
                    try {
                        string_value = (string) dynamic_value;
                    } catch (Exception e) {
                        Console.WriteLine($"Initial cast to string for value: {dynamic_value} failed when writing to CSV.");
                        throw new Exception($"Error: {e.Message}");
                    }

                    dynamic value_to_write;

                    if (int.TryParse(string_value, out int intValue)) {
                        value_to_write = intValue.ToString("N0");
                    } else if (double.TryParse(string_value, out double doubleValue)) {
                        value_to_write = doubleValue.ToString("N");
                    } else if (bool.TryParse(string_value, out bool boolValue)) {
                        value_to_write = boolValue.ToString();
                    } else if (DateTime.TryParse(string_value, out DateTime dateValue)) {
                        value_to_write = dateValue.ToString("yyyy-MM-dd HH:mm:ss");
                    } else if (string_value == "" || string_value == null) {
                        value_to_write = "-";
                    } else {
                        value_to_write = 
                            string_value.Replace("\"", "'") // Replace double quotes with single quotes
                            .Replace("\n", " \\n ") // Replace new lines with '\n'
                            .Replace("    ", " \\t ") // Replace tabs with '\t'
                            .Replace("|", "^");
                    }

                    value_to_write = $"\"{value_to_write}\""; // Wrap in quotes
                    bool end_of_row = col_number == row.Length - 1;

                    if (end_of_row)
                    {
                        line.Append(value_to_write);
                    }
                    else
                    {
                        line.Append(value_to_write + ","); // CSV Separator
                    }

                    col_number++;
                
                }
                row_number++;
                csv.AppendLine(line.ToString());
            }

            File.WriteAllText($"{filePath}.csv", csv.ToString());
        }
    
        public static void OpenFileInVsCode(string filePath) {
            // Command to execute
            string command = "code";
            string arguments = filePath; 

            // Create a process start info
            var processStartInfo = new ProcessStartInfo
            {
                FileName = command, // Command or executable
                Arguments = arguments, // Command arguments
                RedirectStandardOutput = true, // Capture the output
                RedirectStandardError = true, // Capture errors
                UseShellExecute = false, // Don't use the OS shell
                CreateNoWindow = true // Don't create a terminal window
            };

            // Start the process
            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();

                // Read the output and errors
                string output = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();

                process.WaitForExit(); // Ensure the process has completed

                if (!string.IsNullOrEmpty(errors))
                {
                    Console.WriteLine("Errors:");
                    Console.WriteLine(errors);
                }
            }
        }
    
        public static string RequestPbiApi (EnvVariables env_variables, string httpUrl, string jsonBody, HttpRequestType request_type) {
                
                string tenantId = env_variables.AZURE_TENANT_ID;
                string clientId = env_variables.AZURE_APP_ID;
                string clientSecret = env_variables.AZURE_APP_SECRET;

                // Initialize the authentication context
                var app = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                    .Build();

                // Acquire an access token
                var result = app.AcquireTokenForClient(new[] { "https://analysis.windows.net/powerbi/api/.default" }).ExecuteAsync().Result;
                var accessToken = result.AccessToken;

                using (HttpClient client = new HttpClient())
                {
                    // Define the URL
                    string url = httpUrl;

                    var jsonPayload = jsonBody;
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    HttpResponseMessage response;

                    // Make the POST request
                    if (request_type == HttpRequestType.POST) {
                        response = client.PostAsync(url, content).Result;
                    } else if (request_type == HttpRequestType.DELETE) {
                        response = client.DeleteAsync(url).Result;    
                    } else {
                        response = client.GetAsync(url).Result;
                    }
                
                    try
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error executing request to Power BI API: " + e.Message);
                        var errorResult = response.Content.ReadAsStringAsync().Result;
                        var formattedErrorResult = System.Text.Json.JsonSerializer.Serialize(JsonDocument.Parse(errorResult), new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine($"Response: \n {formattedErrorResult}");
                        return "";
                    }
                

                    // Read the response content
                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    return responseBody;
                }
        }
    }
}
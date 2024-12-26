using Microsoft.PowerBI.Api.Models;
using Newtonsoft.Json;

namespace tcli {
    
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Result
    {
        [JsonProperty("tables")]
        public List<Table> tables { get; set; }
    }

    public class DaxQueryResult
    {
        [JsonProperty("results")]
        public List<Result> results { get; set; }
    }

    public class Table
    {
        [JsonProperty("rows")]
        public List<Dictionary<string, object>> rows { get; set; }
    }



}
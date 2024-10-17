using System;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace MafiaLib;

public class JsonSchemaValidator
{
    public static JSchema MessageSchema = JSchema.Parse(@"
        {
            ""type"": ""object"",
            ""properties"":
            {
                ""Sender"" : 
                {
                    ""type"": ""string"",
                    ""default"": """"
                },
                ""Message"" : 
                {
                    ""type"": ""string"",
                    ""default"": """"
                }
            },
            ""required"": [""Sender"",""Message""],
            ""additionalProperties"": false
        }");

    public static (bool, string) Validate(string json)
    {
        bool success = true;
        StringBuilder errs = new StringBuilder();
        try
        {
            JObject jobj = JObject.Parse(json);

            if (!jobj.IsValid(MessageSchema, out IList<ValidationError> errorMessages))
            {
                success = false;
                foreach (var error in errorMessages)
                {
                    errs.AppendLine($"Error: {error.Message} at: {error.Path}");
                }
            }
            return (success, errs.ToString());
        }
        catch (Exception)
        {
            return (false, "Invalid JSON - Validate in JsonSchemaValidator did not find valid JSON could not verify schema.");
        }
    }
}

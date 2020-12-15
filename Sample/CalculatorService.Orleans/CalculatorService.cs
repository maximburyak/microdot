using System;
using System.Threading.Tasks;
using CalculatorService.Interface;
using LanguageExt;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Orleans;
using Orleans.Concurrency;

namespace CalculatorService.Orleans
{

    [StatelessWorker, Reentrant]
    public class CalculatorService : Grain, ICalculatorService
    {
        public Task<int> Add(int a, int b)
        {
            return Task.FromResult(a + b);
        }

        public async Task<Either<ResponseWithSchema, SomeResponse>> GetResponse(string greeting)
        {
            if (greeting == "greeting")
                return new SomeResponse()
                {
                    Value = "Greetings"
                };
            
            return new ResponseWithSchema
            {
                JsonMessage = JObject.FromObject(new {message = $"You said: '{greeting}', I say: Hello!"}),
                MessageSchema = JSchema.Parse(@"{
                  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                  ""type"": ""object"",
                  ""properties"": {
                    ""message"": {
                      ""type"": ""string""
                    }
                  },
                  ""required"": [
                    ""message""
                  ]
                }")
            };
        }
    }
}

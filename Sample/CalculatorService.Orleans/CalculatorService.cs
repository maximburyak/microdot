using System;
using System.Threading.Tasks;
using CalculatorService.Interface;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Orleans;
using Orleans.Concurrency;

namespace CalculatorService.Orleans
{

    public interface ICalculatorServiceGrain : ICalculatorService, IGrainWithIntegerKey
    {
    }

    [StatelessWorker, Reentrant]
    public class CalculatorService : Grain, ICalculatorServiceGrain
    {
        public Task<int> Add(int a, int b)
        {
            return Task.FromResult(a + b);
        }

        public Task<ResponseWithSchema> GetResponse(string greeting) =>
            Task.FromResult(new ResponseWithSchema
            {
                JsonMessage = JObject.FromObject(new { message = $"You said: '{greeting}', I say: Hello!" }),
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
            });
    }
}

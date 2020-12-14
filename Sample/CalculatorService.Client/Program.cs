using System;
using System.Net;
using System.Threading.Tasks;
using CalculatorService.Interface;
using Gigya.Microdot.Common.Tests;
using Gigya.Microdot.Hosting.Environment;
using Gigya.Microdot.Interfaces.Configuration;
using Gigya.Microdot.Logging.NLog;
using Gigya.Microdot.Ninject;
using Gigya.Microdot.SharedLogic.SystemWrappers;
using Ninject;

namespace CalculatorService.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                using (var microdotInitializer = new MicrodotInitializer(
                    "test-client",
                    new NLogModule()))
                {
                    //NLog.LogManager.GlobalThreshold = NLog.LogLevel.Info;
                    var calculatorService = microdotInitializer.Kernel.Get<ICalculatorService>();
                    int sum = await calculatorService.Add(2, 3);
                    var res = await calculatorService.GetResponse("foobar");

                    Console.WriteLine(res.ToString());

                    /*while (true)
                    {
                        try
                        {
                            int sum = calculatorService.Add(2, 3).Result;

                            Console.WriteLine($"Sum: {sum}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }*/

                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
    }
}

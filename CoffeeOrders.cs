using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CoffeeOrders;
public static class CoffeeOrders
{
    [FunctionName("CoffeeOrders")]
    public static async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context
        )
    {
        var request = context.GetInput<string>();
        string[] orders = request.ToString().Split(' ');
        var outputs = new List<Order>();
        var badOrders = new List<Task>();

        foreach (var order in orders)
        {
            try {
                outputs.Add(await context.CallActivityAsync<Order>(nameof(ParseOrder), order));
            } catch {
                badOrders.Add(context.CallActivityAsync<Order>(nameof(FixOrder), order));
            }

            // await in loop?
        }

        // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
        return outputs.Select(order => order.ToString()).ToList();
    }

    // [FunctionName(nameof(SayHello))]
    // public static string SayHello([ActivityTrigger] string name, ILogger log)
    // {
    //     log.LogInformation("Saying hello to {name}.", name);
    //     return $"Hello {name}!";
    // }

    public record Order(string product, int quantity);

    [FunctionName(nameof(ParseOrder))]
    public static Order ParseOrder([ActivityTrigger] string request, ILogger log)
    {
        var body = request.Split(':');

        var product = body[0];
        var quantity = Convert.ToInt32(body[1]);

        return new Order(product, quantity);
    }

    []

    [FunctionName("CoffeeOrders_HttpStart")]
    public static async Task<HttpResponseMessage> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
    {
        var request = await req.Content.ReadAsAsync<object>();

        // Function input comes from the request content.
        string instanceId = await starter.StartNewAsync("CoffeeOrders", request);

        log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Graph;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using slashVote.Api.Models;
/// <summary>
/// Updated User Rollen korrespondierend zu Ihren Gruppen Zugehörigkeiten in AD
/// </summary>

namespace slashVote.Api
{
    public static class GetRoles
    {
        static Dictionary<string, string> roleGroupMapping = new Dictionary<string, string> { { "administrator", "77c5682f-348a-495e-a04e-3f9215912d48" }, { "customeradmin", "a263153e-d7e5-4825-ada7-9ea5bdbb317d" } };

        [FunctionName("GetRoles")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            
            string userRes;
            var user = req.Body;
            using (StreamReader bodyStream = new StreamReader(user))
            {
                userRes = await bodyStream.ReadToEndAsync();
            }
            log.LogInformation(userRes);
            AadUserResponse userData = JsonConvert.DeserializeObject<AadUserResponse>(userRes);

            IList<string> roles = new List<string>();
            foreach (var item in roleGroupMapping)
            {
                log.LogInformation(item.Value + " " + userData.AccessToken);
                if (isUserInGroup(item.Value, userData.AccessToken)) roles.Add(item.Key);
            }
            return new OkObjectResult(new { roles });
        }

        static private bool isUserInGroup(string groupID, string bearerToken) 
        {

            
            string uri = $"https://graph.microsoft.com/v1.0/me/memberOf?$filter=id eq '{groupID}'";
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            var res = httpClient.GetAsync(uri).Result;
            if (!res.IsSuccessStatusCode) return false;
            dynamic graphResponse = JsonConvert.DeserializeObject(res.Content.ReadAsStringAsync().Result);

            foreach (var item in graphResponse.value)
            {
                if (item.id == groupID) return true;
            }
            return false;

        }
    }
}

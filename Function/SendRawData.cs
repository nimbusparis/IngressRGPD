using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Http;

namespace IngressRGPD.Function
{
    public static class SendRawData
    {
        private static string None = "None";

        [FunctionName("SendRawData")]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Received data to process");
            if (!req.HasFormContentType)
            {
                log.LogError("Not file provided");
                return new BadRequestErrorMessageResult("No file provided in form-data, please check");
            }
            var gameActionFile = req.Form.Files.FirstOrDefault(f => f.FileName.Contains("game_log"));

            if (gameActionFile == null)
            {
                log.LogError("Bad file uploaded");
                return new BadRequestErrorMessageResult("The file send is not game_log.tsv, please check");
            }
            log.LogInformation("Parse file for unique portal captures and visits");
            var series = ParseGameAction(gameActionFile);
            return new OkObjectResult(series);
        }

        public struct UniqueSeries
        {
            [JsonProperty(PropertyName = "upc")]
            public IEnumerable<double[]> UPCs;
            [JsonProperty(PropertyName = "upv")]
            public IEnumerable<double[]> UPVs;
        }

        public static UniqueSeries ParseGameAction(IFormFile ingressFiles)
        {
            var file = ingressFiles;
            double lat, lng;
            ISet<LatLng> upv = new HashSet<LatLng>(0);
            ISet<LatLng> upc = new HashSet<LatLng>(0);
            //foreach (var file in ingressFiles)
            {
                using (var fileStream = file.OpenReadStream())
                using (var reader = new StreamReader(fileStream))
                {
                    var actions = new Dictionary<string, ISet<LatLng>>();
                    // Strip header
                    string line = null;
                    reader.ReadLine();
                    while ((line = reader.ReadLine()) != null)
                    {
                        var splittedLine = line.Split('\t');
                        if (splittedLine.Length < 1) continue;
                        if (!actions.ContainsKey(splittedLine[3]))
                        {
                            actions.Add(splittedLine[3], new HashSet<LatLng>());
                        }

                        if (splittedLine[1] == None || splittedLine[2] == None) continue;
                        if (!double.TryParse(splittedLine[1], NumberStyles.Float | NumberStyles.AllowThousands,
                                CultureInfo.InvariantCulture, out lat) ||
                            !double.TryParse(splittedLine[2], NumberStyles.Float | NumberStyles.AllowThousands,
                                CultureInfo.InvariantCulture, out lng))
                            continue;

                        actions[splittedLine[3]].Add(new LatLng(lat, lng));
                    }

                    upc.UnionWith(actions["captured portal"]);

                    upv.UnionWith(actions["hacked friendly portal"]);
                    upv.UnionWith(actions["created link"]);
                    upv.UnionWith(actions["captured portal"]);
                    upv.UnionWith(actions["resonator deployed"]);
                    upv.UnionWith(actions["resonator upgraded"]);
                    upv.UnionWith(actions["hacked enemy portal"]);
                    upv.UnionWith(actions["hacked neutral portal"]);
                }
            }


            return new UniqueSeries
            {
                UPCs = upc.Select(l => new[] {l.Lat, l.Lng}),
                UPVs = upv.Select(l => new[] { l.Lat, l.Lng }),
            };
        }
    }
}
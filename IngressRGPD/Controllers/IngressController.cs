using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IngressRGPD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngressController : ControllerBase
    {
        private const string None = "None";

        [HttpPost("{kindVisit}")]
        [DisableRequestSizeLimit]
        public IActionResult ParseGameAction(string kindVisit, IFormFile ingressFiles)
        {
            ISet<LatLng> a = new HashSet<LatLng>();
            var file = ingressFiles;
            double lat, lng;
            //foreach (var file in ingressFiles)
            {
                using (var fileStream = file.OpenReadStream())
                {
                    var actions = new Dictionary<string, ISet<LatLng>>();
                    var reader = new StreamReader(fileStream);
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
                        if (!double.TryParse(splittedLine[1], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out lat) ||
                          !double.TryParse(splittedLine[2], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out lng))
                            continue;

                        actions[splittedLine[3]].Add(new LatLng(lat, lng));
                    }
                    switch (kindVisit)
                    {
                        case "upc":
                            a = actions["captured portal"];
                            break;
                        case "upv":
                            a = actions["hacked friendly portal"];
                            a.UnionWith(actions["created link"]);
                            a.UnionWith(actions["captured portal"]);
                            a.UnionWith(actions["resonator deployed"]);
                            a.UnionWith(actions["resonator upgraded"]);
                            a.UnionWith(actions["hacked enemy portal"]);
                            a.UnionWith(actions["hacked neutral portal"]);
                            break;
                    }
                }
            }

            return Ok(a.Select(l => new[] { l.Lat, l.Lng }));
        }
    }
}

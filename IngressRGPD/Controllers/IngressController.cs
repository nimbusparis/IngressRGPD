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

        [HttpPost("actions")]
        [DisableRequestSizeLimit]
        public IActionResult ParseGameAction(IFormFile ingressFiles)
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
                        if (!double.TryParse(splittedLine[1], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out lat) ||
                          !double.TryParse(splittedLine[2], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out lng))
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

            return Ok(new 
            {
                upc = upc.Select(l => new[] { l.Lat, l.Lng }),
                upv = upv.Select(l => new[] { l.Lat, l.Lng })
            });
        }
    }
}

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

        private static LatLng GetCentralGeoCoordinate(ISet<LatLng> geoCoordinates)
        {
            if (geoCoordinates.Count == 0)
            {
                return default(LatLng);
            }

            if (geoCoordinates.Count == 1)
            {
                return geoCoordinates.Single();
            }

            double x = 0;
            double y = 0;
            double z = 0;

            foreach (var geoCoordinate in geoCoordinates)
            {
                var latitude = geoCoordinate.Lat * Math.PI / 180;
                var longitude = geoCoordinate.Lng * Math.PI / 180;

                x += Math.Cos(latitude) * Math.Cos(longitude);
                y += Math.Cos(latitude) * Math.Sin(longitude);
                z += Math.Sin(latitude);
            }

            var total = geoCoordinates.Count;

            x = x / total;
            y = y / total;
            z = z / total;

            var centralLongitude = Math.Atan2(y, x);
            var centralSquareRoot = Math.Sqrt(x * x + y * y);
            var centralLatitude = Math.Atan2(z, centralSquareRoot);

            return new LatLng(centralLatitude * 180 / Math.PI, centralLongitude * 180 / Math.PI);
        }

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

            var c = GetCentralGeoCoordinate(upv);

            return Ok(new
            {
                upc = upc.Select(l => new[] { l.Lat, l.Lng }),
                upv = upv.Select(l => new[] { l.Lat, l.Lng }),
                central = new[] { c.Lat, c.Lng }
            });
        }
    }
}

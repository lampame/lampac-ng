using Microsoft.AspNetCore.Mvc;
using Shared.Attributes;

namespace OnlineUKR.Controllers
{
    public class Ashdi : BaseOnlineController
    {
        public Ashdi() : base(ModInit.conf.Ashdi) { }

        [HttpGet]
        [Staticache]
        [Route("lite/ashdi")]
        async public Task<ActionResult> Index(string href, string imdb_id, long kinopoisk_id, string title, string original_title, int year, int clarification, int t = -1, int s = -1, bool rjson = false)
        {
            if (await IsRequestBlocked(rch: true))
                return badInitMsg;

            var oninvk = new AshdiInvoke
            (
               host,
               init.host,
               httpHydra,
               streamfile => HostStreamProxy(streamfile)
            );

        rhubFallback:
            string mkey = !string.IsNullOrEmpty(href)
                ? $"ashdi:view:{href}"
                : $"ashdi:view:{imdb_id}:{kinopoisk_id}:{title}:{year}:{clarification}";

            var cache = await InvokeCacheResult(mkey, 40,
                () => oninvk.EmbedKurwa(href, clarification, title, original_title, year, imdb_id, kinopoisk_id),
                textJson: true
            );

            if (IsRhubFallback(cache))
                goto rhubFallback;

            return ContentTpl(cache,
                () => oninvk.Tpl(cache.Value, href, imdb_id, kinopoisk_id, title, original_title, clarification, year, t, s, vast: init.vast, rjson: rjson)
            );
        }
    }
}

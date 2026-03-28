using Microsoft.AspNetCore.Mvc;
using Shared.Attributes;

namespace OnlineUKR.Controllers
{
    public class Kinoukr : BaseOnlineController
    {
        public Kinoukr() : base(ModInit.conf.Kinoukr) { }

        [HttpGet]
        [Staticache]
        [Route("lite/kinoukr")]
        async public Task<ActionResult> Index(string title, string original_title, int clarification, int year, string t, int s = -1, string href = null, bool rjson = false, string source = null, string id = null)
        {
            if (await IsRequestBlocked(rch: true))
                return badInitMsg;

            var oninvk = new KinoukrInvoke
            (
               host,
               init.host,
               httpHydra,
               onstreamtofile => HostStreamProxy(onstreamtofile)
            );

            if (string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(id))
            {
                if (source.Equals("kinoukr", StringComparison.OrdinalIgnoreCase))
                    href = await InvokeCache($"kinoukr:source:{id}", 180, () => oninvk.getIframeSource($"{init.host}/{id}"));
            }

        rhubFallback:
            var cache = await InvokeCacheResult($"kinoukr:view:{title}:{original_title}:{year}:{href}:{clarification}", 40,
                () => oninvk.EmbedKurwa(clarification, title, original_title, year, href),
                textJson: true
            );

            if (IsRhubFallback(cache))
                goto rhubFallback;

            return ContentTpl(cache,
                () => oninvk.Tpl(cache.Value, clarification, title, original_title, year, t, s, href, vast: init.vast, rjson: rjson)
            );
        }
    }
}

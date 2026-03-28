using Microsoft.AspNetCore.Mvc;
using Shared.Attributes;

namespace OnlineUKR.Controllers
{
    public class Eneyida : BaseOnlineController
    {
        public Eneyida() : base(ModInit.conf.Eneyida) { }

        [HttpGet]
        [Staticache]
        [Route("lite/eneyida")]
        async public Task<ActionResult> Index(string title, string original_title, int clarification, int year, int t = -1, int s = -1, string href = null, bool rjson = false, bool similar = false, string source = null, string id = null)
        {
            if (await IsRequestBlocked(rch: true))
                return badInitMsg;

            if (string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(id))
            {
                if (source.ToLower() == "eneyida")
                    href = $"{init.host}/{id}";
            }

            if (string.IsNullOrWhiteSpace(href) && (string.IsNullOrWhiteSpace(original_title) || year == 0))
                return OnError();

            var oninvk = new EneyidaInvoke
            (
               host,
               init.host,
               httpHydra,
               onstreamtofile => HostStreamProxy(onstreamtofile)
            );

        rhubFallback:
            var cache = await InvokeCacheResult($"eneyida:view:{title}:{year}:{href}:{clarification}:{similar}", 40,
                () => oninvk.Embed((similar || clarification == 1) ? title : original_title, year, href, similar),
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

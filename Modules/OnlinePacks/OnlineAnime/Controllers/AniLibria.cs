using Microsoft.AspNetCore.Mvc;
using OnlineAnime.Models.AniLibria;
using Shared.Attributes;

namespace OnlineAnime.Controllers
{
    public class AniLibriaOnline : BaseOnlineController
    {
        public AniLibriaOnline() : base(ModInit.conf.AnilibriaOnline) { }

        [HttpGet]
        [Staticache]
        [Route("lite/anilibria")]
        async public Task<ActionResult> Index(string title, string code, int year, bool rjson = false, bool similar = false)
        {
            if (await IsRequestBlocked(rch: true))
                return badInitMsg;

            if (string.IsNullOrEmpty(title))
                return OnError();

            var oninvk = new AniLibriaInvoke
            (
               host,
               init.host,
               ongettourl => httpHydra.Get<List<RootObject>>(ongettourl, IgnoreDeserializeObject: true),
               streamfile => HostStreamProxy(streamfile)
            );

        rhubFallback:
            var cache = await InvokeCacheResult($"anilibriaonline:{title}", 40,
                () => oninvk.Embed(title)
            );

            if (IsRhubFallback(cache))
                goto rhubFallback;

            return ContentTpl(cache, () => oninvk.Tpl(cache.Value, title, code, year, vast: init.vast, rjson: rjson, similar: similar));
        }
    }
}

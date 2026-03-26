using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Online.Config;
using Online.Controllers;
using Online.Models.KinoUkr;
using Online.SQL;
using Shared.Models.AppConf;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using Shared.Models.Online.Settings;
using System.Threading;

namespace Online
{
    public class ModInit : IModuleLoaded, IModuleConfigure
    {
        public static string modpath;
        public static ModuleConf conf;
        public static PidTorSettings PidTor;
        public static AnimeConf animeConf;
        public static EngConf engConf;
        public static UkrConf ukrConf;
        public static PremiumConf premiumConf;
        public static SiteConf siteConf;
        static Timer kinoukrTimer;

        public void Configure(ConfigureModel app)
        {
            app.services.AddDbContextFactory<ExternalidsContext>(ExternalidsContext.ConfiguringDbBuilder);
        }

        public void Loaded(InitspaceModel baseconf)
        {
            modpath = baseconf.path;

            updateConf();
            EventListener.UpdateInitFile += updateConf;

            KinoukrInvoke.KinoukrDb = JsonConvert.DeserializeObject<ConcurrentDictionary<string, Model>>(File.ReadAllText("data/kinoukr.json"));
            VeoVeo.database = JsonHelper.ListReader<Models.VeoVeo.Movie>("data/veoveo.json", 130_000).Result;
            //Kodik.database = JsonHelper.ListReader<Models.Kodik.Result>("data/kodik.json", 100_000);
            //Lumex.database = JsonHelper.ListReader<Models.Lumex.DatumDB>("data/lumex.json", 130_000).Result;
            //VDBmovies.database = JsonHelper.ListReader<Models.VDBmovies.MovieDB>("data/cdnmovies.json", 130_000).Result;

            ExternalidsContext.Initialization(baseconf.app.ApplicationServices);

            foreach (var m in conf.limit_map)
                CoreInit.conf.WAF.limit_map.Insert(0, m);

            kinoukrTimer = new Timer(KurwaCron.Kinoukr, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20));
        }


        void updateConf()
        {
            conf = ModuleInvoke.Init("online", new ModuleConf()
            {
                findkp = "all",
                checkOnlineSearch = true,
                spider = true,
                spiderName = "Spider",
                component = "lampac",
                name = CoreInit.conf.online.name,
                description = "Плагин для просмотра онлайн сериалов и фильмов",
                version = CoreInit.conf.online.version,
                btn_priority_forced = CoreInit.conf.online.btn_priority_forced,
                showquality = true,
                with_search = new List<string>() { "kinotochka", "kinobase", "kinopub", "lumex", "filmix", "filmixtv", "fxapi", "redheadsound", "animevost", "animego", "animedia", "animebesst", "anilibria", "aniliberty", "rezka", "rhsprem", "kodik", "remux", "animelib", "kinoukr", "rc/filmix", "rc/fxapi", "rc/rhs", "vcdn", "videocdn", "lumex", "collaps", "collaps-dash", "vdbmovies", "hdvb", "alloha", "veoveo", "rutubemovie", "vkmovie" },
                limit_map = new List<WafLimitRootMap>
                {
                    new("^/lite/", new WafLimitMap { limit = 10, second = 1 }),
                    new("^/(externalids|lifeevents)", new WafLimitMap { limit = 10, second = 1 })
                }
            });

            PidTor = ModuleInvoke.Init("PidTor", new PidTorSettings()
            {
                enable = true,
                min_sid = 15,
                emptyVoice = true,
                redapi = "http://ns3bg91xvuqfvq9h.cfhttp.top"
            });

            animeConf = ModuleInvoke.DeserializeInit(new AnimeConf());
            engConf = ModuleInvoke.DeserializeInit(new EngConf());
            ukrConf = ModuleInvoke.DeserializeInit(new UkrConf());
            premiumConf = ModuleInvoke.DeserializeInit(new PremiumConf());
            siteConf = ModuleInvoke.DeserializeInit(new SiteConf());
        }


        public void Dispose()
        {
        }
    }
}

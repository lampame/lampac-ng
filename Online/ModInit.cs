using Microsoft.Extensions.DependencyInjection;
using Online.Config;
using Online.Controllers;
using Online.SQL;
using Shared.Models.AppConf;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using Shared.Models.Online.Settings;

namespace Online
{
    public class ModInit : IModuleLoaded, IModuleConfigure
    {
        public static string modpath;
        public static ModuleConf conf;
        public static PidTorSettings PidTor;
        public static PremiumConf premiumConf;
        public static SiteConf siteConf;

        public void Configure(ConfigureModel app)
        {
            app.services.AddDbContextFactory<ExternalidsContext>(ExternalidsContext.ConfiguringDbBuilder);
        }

        public void Loaded(InitspaceModel baseconf)
        {
            modpath = baseconf.path;

            updateConf();
            EventListener.UpdateInitFile += updateConf;

            VeoVeo.database = JsonHelper.ListReader<Models.VeoVeo.Movie>("data/veoveo.json", 130_000).Result;

            ExternalidsContext.Initialization(baseconf.app.ApplicationServices);

            foreach (var m in conf.limit_map)
                CoreInit.conf.WAF.limit_map.Insert(0, m);
        }

        public void Dispose()
        {
            EventListener.UpdateInitFile -= updateConf;
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
                with_search = new List<string>()
                {
                    "kinotochka", "kinobase", "kinopub", "filmix", "filmixtv", "fxapi", "rezka", "rhsprem", "remux", "kinoukr", "collaps", "collaps-dash", "hdvb", "alloha", "veoveo", "rutubemovie", "vkmovie",
                    "animevost", "animego", "animedia", "animebesst", "anilibria", "aniliberty", "kodik", "animelib",
                },
                limit_map = new List<WafLimitRootMap>
                {
                    new("^/lite/", new WafLimitMap { limit = 10, second = 1 }),
                    new("^/(externalids|lifeevents)", new WafLimitMap { limit = 10, second = 1 })
                }
            });

            PidTor = ModuleInvoke.Init("PidTor", new PidTorSettings()
            {
                enable = true,
                displayindex = 551,
                min_sid = 15,
                emptyVoice = true,
                redapi = "http://jac.red"
            });

            premiumConf = ModuleInvoke.DeserializeInit(new PremiumConf());
            siteConf = ModuleInvoke.DeserializeInit(new SiteConf());
        }
    }
}

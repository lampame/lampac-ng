using Shared.Services;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using Shared.Models.Online.Settings;
using Shared.Models.SISI.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lamson
{
    public class ModInit : IModuleLoaded
    {
        public static OnlinesSettings KinoGram;

        public static SisiSettings PornGram;


        public void Loaded(InitspaceModel conf)
        {
            KinoGram = ModuleInvoke.Init("KinoGram", new OnlinesSettings("KinoGram", "kinogram.com", streamproxy: true));
            PornGram = ModuleInvoke.Init("PornGram", new SisiSettings("PornGram", "porngram.com"));

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                // асинхронные задачи
                await Task.Delay(1000);
            });


            var timer = new System.Timers.Timer(TimeSpan.FromMinutes(1).TotalMilliseconds)
            {
                AutoReset = true,
                Enabled = true
            };

            timer.Elapsed += async (s, e) =>
            {
                // cron
                await Task.Delay(1000);
            };

            EventListener.Middleware += (first, e) => Task.FromResult(Middlewares.Invoke(first, e.httpContext, e.memoryCache));
            EventListener.Middleware += async (first, e) => await Middlewares.InvokeAsync(first, e.httpContext, e.memoryCache);
        }


        public void Dispose()
        {
        }
    }
}

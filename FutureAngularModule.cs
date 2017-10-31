using Abp.Configuration.Startup;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Future.Angular.Proxying;
using System;

namespace Future.Angular
{
    /// <summary>
    /// Angular 脚本代理模块
    /// </summary>
    public class FutureAngularModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Modules.AbpWebCommon()
                .ApiProxyScripting.Generators[AngularProxyScriptGenerator.Name] = typeof(AngularProxyScriptGenerator);
            //base.PreInitialize();
        }
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(FutureAngularModule).GetAssembly());
        }
    }
}

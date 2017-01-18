using Microsoft.Xrm.Sdk;
using TSAD.CORE.D365.COM.AutoNumber.Generic;
using TSAD.XRM.Framework.Auto365.Plugin;

namespace TSAD.CORE.D365.COM.AutoNumber.Plugins.Generic
{
    public class PreGenericCustomAutoNumberCreate : Auto365BasePlugin, IPlugin
    {
        public PreGenericCustomAutoNumberCreate(string unsecure, string secure) : base(unsecure, secure)
        {
        }

        protected override void ExecuteCrmPlugin(IAuto365TransactionContext<Entity> context)
        {
            new CreateCounterCustomAutoNumber(context).Execute();
        }
    }
}

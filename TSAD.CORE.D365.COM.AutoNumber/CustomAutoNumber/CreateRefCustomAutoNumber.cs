using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSAD.CORE.D365.Entities;
using TSAD.XRM.Framework.Auto365.Plugin;
using TSAD.XRM.Framework;

namespace TSAD.CORE.D365.COM.AutoNumber.CustomAutoNumber
{
    public class CreateRefCustomAutoNumber : Auto365BaseOperation<xts_customautonumber>
    {
        public CreateRefCustomAutoNumber(IAuto365TransactionContext<xts_customautonumber> context) : base(context)
        {
        }

        protected override void HandleExecute()
        {
            CreateCustomAutoNumberIndex(Context.Input.Entity);
        }

        private Guid CreateCustomAutoNumberIndex(xts_customautonumber customAutoNumber)
        {
            var customAutoNumberIndex = new xts_customautonumberindex();
            customAutoNumberIndex.Set(e => e.xts_CustomAutonumberId, new EntityReference(xts_customautonumber.EntityLogicalName, customAutoNumber.Id));
            
            return Service.Create(customAutoNumberIndex);
        }
    }
}

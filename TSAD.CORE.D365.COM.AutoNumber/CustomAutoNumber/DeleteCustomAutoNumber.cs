using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSAD.CORE.D365.Entities;
using TSAD.XRM.Framework.Auto365.Plugin;
using TSAD.XRM.Framework.Data;
using TSAD.XRM.Framework;

namespace TSAD.CORE.D365.COM.AutoNumber.CustomAutoNumber
{
    public class DeleteCustomAutoNumber : Auto365BaseOperation<xts_customautonumber>
    {
        public DeleteCustomAutoNumber(IAuto365TransactionContext<xts_customautonumber> context) : base(context)
        {
        }

        protected override void HandleExecute()
        {
            #region get custom autonumber by id
            var cs = new ColumnSet<xts_customautonumber>(e => e.xts_pluginstepid, e => e.xts_entitynamevalue);
            var customAutoNumber = Service.Retrieve(xts_customautonumber.EntityLogicalName, Context.Input.Id, cs).ToEntity<xts_customautonumber>();

            if (customAutoNumber == null)
                throw new InvalidPluginExecutionException("ID is doesn't exist");
            #endregion

            #region delete step
            Service.Delete(SdkMessageProcessingStep.EntityLogicalName, new Guid(customAutoNumber.Get(e => e.xts_pluginstepid)));
            #endregion
        }
    }
}

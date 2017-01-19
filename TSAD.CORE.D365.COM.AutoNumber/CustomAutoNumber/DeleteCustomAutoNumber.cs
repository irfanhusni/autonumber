using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using TSAD.CORE.D365.Entities;
using TSAD.XRM.Framework;
using TSAD.XRM.Framework.Auto365.Plugin;
using TSAD.XRM.Framework.Data;

namespace TSAD.CORE.D365.COM.AutoNumber.CustomAutoNumber
{
    /// <summary>
    /// This class is used for delete custom auto number, once this called, it will delete also the step on plugin
    /// </summary>
    public class DeleteCustomAutoNumber : Auto365BaseOperation<xts_customautonumber>
    {
        public DeleteCustomAutoNumber(IAuto365TransactionContext<xts_customautonumber> context) : base(context)
        {
        }

        protected override void HandleExecute()
        {           
            #region query step by step name
            QueryByAttribute queryByAttribute = new QueryByAttribute()
            {
                EntityName = SdkMessageProcessingStep.EntityLogicalName,
                ColumnSet = new ColumnSet(true)
            };
            queryByAttribute.AddAttributeValue(Helper.Name<SdkMessageProcessingStep>(e => e.Name), Get(e => e.xts_pluginstepid));

            var step = Service.RetrieveMultiple(queryByAttribute);
            #endregion

            #region delete step
            if (step != null)
                Service.Delete(SdkMessageProcessingStep.EntityLogicalName, step[0].Id);
            #endregion
        }
    }
}

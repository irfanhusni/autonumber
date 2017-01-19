using System;
using TSAD.CORE.D365.COM.AutoNumber.CustomAutoNumber;
using TSAD.CORE.D365.Entities;
using TSAD.XRM.Framework;
using TSAD.XRM.TestFramework.Auto365;
using Xunit;

namespace TSAD.CORE.D365.COM.AutoNumber.Tests.CustomAutoNumber
{
    public class DeleteCustomAutoNumberTest : Auto365BaseTest<xts_customautonumber>
    {
        [Fact]
        public void DeleteCustomAutoNumber_ShouldReturnOK()
        {
            #region mock sdk message filter
            var sdkMessageProcStep = new SdkMessageProcessingStep { Id = Guid.NewGuid() };
            sdkMessageProcStep.Set(e => e.Name, "PreCarAutonumberCreate");
            Db["SDK-MESSAGEPROCESSINGSTEP-001"] = sdkMessageProcStep;
            #endregion

            #region define input parameters
            var customAutoNumber = new xts_customautonumber();
            customAutoNumber.Set(x => x.Id, Guid.NewGuid());
            customAutoNumber.Set(x => x.xts_pluginstepid, "PreCarAutonumberCreate");
            Reference = customAutoNumber;
            #endregion            

            #region call create custom auto number
            var ex = Record.Exception(() => new DeleteCustomAutoNumber(Context).Execute());
            #endregion

            #region assert
            Assert.Null(ex);
            #endregion
        }
    }
}

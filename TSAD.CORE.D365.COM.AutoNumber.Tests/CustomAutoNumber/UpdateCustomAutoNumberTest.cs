using System;
using TSAD.CORE.D365.COM.AutoNumber.CustomAutoNumber;
using TSAD.CORE.D365.Entities;
using TSAD.XRM.Framework;
using TSAD.XRM.TestFramework.Auto365;
using Xunit;

namespace TSAD.CORE.D365.COM.AutoNumber.Tests.CustomAutoNumber
{
    public class UpdateCustomAutoNumberTest : Auto365BaseTest<xts_customautonumber>
    {
        [Fact]
        public void UpdateCustomAutoNumber_ShouldReturnOK()
        {
            Guid id = Guid.NewGuid();         

            #region define input parameters
            var customAutoNumber = new xts_customautonumber() { Id  = id };
            customAutoNumber.Set(x => x.xts_resettype, xts_customautonumber.Options.xts_resettype.Monthly);
            customAutoNumber.Set(x => x.xts_pluginstepid, "PreCarAutonumberCreate");
            Reference = customAutoNumber;
            #endregion     

            #region mock custom auto number index
            var customAutonumberIndex = new xts_customautonumberindex();
            customAutonumberIndex.Set(x => x.xts_CustomAutonumberId, customAutoNumber.ToEntityReference());
            customAutonumberIndex.Set(x => x.xts_lastindexgenerateddate, DateTime.Now);
            customAutonumberIndex.Set(x => x.xts_name, "Car");
            Db["CUSTOM-AUTONUMBER-INDEX-001"] = customAutonumberIndex;
            #endregion

            #region call create custom auto number
            var ex = Record.Exception(() => new UpdateCustomAutoNumber(Context).Execute());
            #endregion

            #region assert
            Assert.Null(ex);
            #endregion
        }
    }
}

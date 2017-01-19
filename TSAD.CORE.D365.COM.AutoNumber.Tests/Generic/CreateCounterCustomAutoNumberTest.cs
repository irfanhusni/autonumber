using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSAD.CORE.D365.COM.AutoNumber.Generic;
using TSAD.CORE.D365.Entities;
using TSAD.XRM.TestFramework.Auto365;
using Xunit;
using TSAD.XRM.Framework;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using NSubstitute;

namespace TSAD.CORE.D365.COM.AutoNumber.Tests.Generic
{
    public class CreateCounterCustomAutoNumberTest : Auto365BaseTest<xts_rewardtransaction>
    {
        [Fact]
        public void CreateCounterCustomAutoNumber_FirstRecord_ShouldReturnOK()
        {
            #region define input
            var entity = new xts_rewardtransaction();
            Reference = entity;
            #endregion

            #region mock custom auto number
            var customAutoNumber = new xts_customautonumber();
            customAutoNumber.Id = Guid.NewGuid(); ;
            customAutoNumber.Set(e => e.xts_resettype, xts_customautonumber.Options.xts_resettype.None);
            customAutoNumber.Set(e => e.xts_entitynamevalue, "xts_rewardtransaction");
            customAutoNumber.Set(e => e.xts_segmentformat, "[##]");
            customAutoNumber.Set(e => e.xts_segmentformatnumber, "##");
            customAutoNumber.Set(e => e.xts_attributenamevalue, "xts_rewardnumber");
            Db["CUSTOM-AUTO-NUMBER-001"] = customAutoNumber;
            #endregion
           
            #region call create counter custom auto number
            new CreateCounterCustomAutoNumber(Context).Execute();
            #endregion

            #region assert
            Assert.Equal("01", Input.Get(e => e.xts_rewardnumber));
            #endregion
        }

        [Fact]
        public void CreateCounterCustomAutoNumber_NextRecord_ShouldReturnOK()
        {
            #region define input
            var entity = new xts_rewardtransaction();
            Reference = entity;
            #endregion

            #region mock custom auto number
            var customAutoNumber = new xts_customautonumber();
            customAutoNumber.Id = Guid.NewGuid(); ;
            customAutoNumber.Set(e => e.xts_resettype, xts_customautonumber.Options.xts_resettype.None);
            customAutoNumber.Set(e => e.xts_entitynamevalue, "xts_rewardtransaction");
            customAutoNumber.Set(e => e.xts_segmentformat, "[##]");
            customAutoNumber.Set(e => e.xts_segmentformatnumber, "##");
            customAutoNumber.Set(e => e.xts_attributenamevalue, "xts_rewardnumber");
            Db["CUSTOM-AUTO-NUMBER-001"] = customAutoNumber;
            #endregion

            #region mock custom auto number index
            var customAutonumberIndex = new xts_customautonumberindex();
            customAutonumberIndex.Set(x => x.xts_CustomAutonumberId, customAutoNumber.ToEntityReference());
            customAutonumberIndex.Set(x => x.xts_lastindex, 1);
            Db["CUSTOM-AUTO-NUMBER-INDEX-001"] = customAutonumberIndex;
            #endregion

            #region call create counter custom auto number
            new CreateCounterCustomAutoNumber(Context).Execute();
            #endregion

            #region assert
            Assert.Equal("02", Input.Get(e => e.xts_rewardnumber));
            #endregion
        }

        [Fact]
        public void CreateCounterCustomAutoNumber_FirstRecordWithBU_ShouldReturnOK()
        {
            #region mock business unit
            var businessUnit = new BusinessUnit();
            businessUnit.Id = Guid.NewGuid();
            businessUnit.Set(e => e.Name, "tsad");
            Db["BU-001"] = businessUnit;
            #endregion

            #region define input
            var entity = new xts_rewardtransaction();
            entity.Set(x => x.xts_businessunitid, businessUnit.ToEntityReference());
            Reference = entity;
            #endregion

            #region mock custom auto number
            var customAutoNumber = new xts_customautonumber();
            customAutoNumber.Id = Guid.NewGuid(); ;
            customAutoNumber.Set(e => e.xts_resettype, xts_customautonumber.Options.xts_resettype.None);
            customAutoNumber.Set(e => e.xts_entitynamevalue, "xts_rewardtransaction");
            customAutoNumber.Set(e => e.xts_segmentformat, "[##]/[BU]");
            customAutoNumber.Set(e => e.xts_segmentformatnumber, "##");
            customAutoNumber.Set(e => e.xts_attributenamevalue, "xts_rewardnumber");
            customAutoNumber.Set(e => e.xts_BusinessUnitAttributeNameValue, "xts_businessunitid"); 
            Db["CUSTOM-AUTO-NUMBER-001"] = customAutoNumber;
            #endregion

            #region define entity meta data response
            var entityMetaData = new EntityMetadata();
            entityMetaData.LogicalName = "businessunit";
            var property = typeof(EntityMetadata).GetProperty("PrimaryNameAttribute");
            property.SetValue(entityMetaData, "name", null);
            #endregion

            #region mock validate attributes
            var responseEntity = new RetrieveEntityResponse();
            responseEntity["EntityMetadata"] = entityMetaData;
            Test.Service.Execute(Arg.Any<RetrieveEntityRequest>())
                .Returns(responseEntity);
            #endregion

            #region call create counter custom auto number
            new CreateCounterCustomAutoNumber(Context).Execute();
            #endregion

            #region assert
            Assert.Equal("01/tsad", Input.Get(e => e.xts_rewardnumber));
            #endregion
        }

        [Fact]
        public void CreateCounterCustomAutoNumber_NextRecordWithBU_ShouldReturnOK()
        {
            #region mock business unit
            var businessUnit = new BusinessUnit();
            businessUnit.Id = Guid.NewGuid();
            businessUnit.Set(e => e.Name, "tsad");
            Db["BU-001"] = businessUnit;
            #endregion

            #region define input
            var entity = new xts_rewardtransaction();
            entity.Set(x => x.xts_businessunitid, businessUnit.ToEntityReference());
            Reference = entity;
            #endregion

            #region mock custom auto number
            var customAutoNumber = new xts_customautonumber();
            customAutoNumber.Id = Guid.NewGuid(); ;
            customAutoNumber.Set(e => e.xts_resettype, xts_customautonumber.Options.xts_resettype.None);
            customAutoNumber.Set(e => e.xts_entitynamevalue, "xts_rewardtransaction");
            customAutoNumber.Set(e => e.xts_segmentformat, "[##]/[BU]");
            customAutoNumber.Set(e => e.xts_segmentformatnumber, "##");
            customAutoNumber.Set(e => e.xts_attributenamevalue, "xts_rewardnumber");
            customAutoNumber.Set(e => e.xts_BusinessUnitAttributeNameValue, "xts_businessunitid");
            Db["CUSTOM-AUTO-NUMBER-001"] = customAutoNumber;
            #endregion

            #region mock custom auto number index
            var customAutonumberIndex = new xts_customautonumberindex();
            customAutonumberIndex.Set(x => x.xts_CustomAutonumberId, customAutoNumber.ToEntityReference());
            customAutonumberIndex.Set(x => x.xts_lastindex, 1);
            Db["CUSTOM-AUTO-NUMBER-INDEX-001"] = customAutonumberIndex;
            #endregion

            #region define entity meta data response
            var entityMetaData = new EntityMetadata();
            entityMetaData.LogicalName = "businessunit";
            var property = typeof(EntityMetadata).GetProperty("PrimaryNameAttribute");
            property.SetValue(entityMetaData, "name", null);
            #endregion

            #region mock validate attributes
            var responseEntity = new RetrieveEntityResponse();
            responseEntity["EntityMetadata"] = entityMetaData;
            Test.Service.Execute(Arg.Any<RetrieveEntityRequest>())
                .Returns(responseEntity);
            #endregion

            #region call create counter custom auto number
            new CreateCounterCustomAutoNumber(Context).Execute();
            #endregion

            #region assert
            Assert.Equal("02/tsad", Input.Get(e => e.xts_rewardnumber));
            #endregion
        }

        [Fact]
        public void CreateCounterCustomAutoNumber_FirstRecordWithTransactionDate_ShouldReturnOK()
        {            
            #region define input
            var entity = new xts_rewardtransaction();
            entity.Set(e => e.xts_transactiondate, DateTime.Now);
            Reference = entity;
            #endregion

            #region mock custom auto number
            var customAutoNumber = new xts_customautonumber();
            customAutoNumber.Id = Guid.NewGuid(); ;
            customAutoNumber.Set(e => e.xts_resettype, xts_customautonumber.Options.xts_resettype.None);
            customAutoNumber.Set(e => e.xts_entitynamevalue, "xts_rewardtransaction");
            customAutoNumber.Set(e => e.xts_segmentformat, "[##]/[MM]/[YY]");
            customAutoNumber.Set(e => e.xts_segmentformatnumber, "##");
            customAutoNumber.Set(e => e.xts_segmentformatdate, "yy-MM");
            customAutoNumber.Set(e => e.xts_attributenamevalue, "xts_rewardnumber");
            customAutoNumber.Set(e => e.xts_TransactionDateAttributeNameValue, "xts_transactiondate");
            Db["CUSTOM-AUTO-NUMBER-001"] = customAutoNumber;
            #endregion

            #region define entity meta data response
            var entityMetaData = new EntityMetadata();
            entityMetaData.LogicalName = "businessunit";
            var property = typeof(EntityMetadata).GetProperty("PrimaryNameAttribute");
            property.SetValue(entityMetaData, "name", null);
            #endregion

            #region mock validate attributes
            var responseEntity = new RetrieveEntityResponse();
            responseEntity["EntityMetadata"] = entityMetaData;
            Test.Service.Execute(Arg.Any<RetrieveEntityRequest>())
                .Returns(responseEntity);
            #endregion

            #region call create counter custom auto number
            new CreateCounterCustomAutoNumber(Context).Execute();
            #endregion

            #region assert
            Assert.Equal("01/01/17", Input.Get(e => e.xts_rewardnumber));
            #endregion
        }

        [Fact]
        public void CreateCounterCustomAutoNumber_NextRecordWithTransactionDate_ShouldReturnOK()
        {
            #region define input
            var entity = new xts_rewardtransaction();
            entity.Set(e => e.xts_transactiondate, DateTime.Now);
            Reference = entity;
            #endregion

            #region mock custom auto number
            var customAutoNumber = new xts_customautonumber();
            customAutoNumber.Id = Guid.NewGuid(); ;
            customAutoNumber.Set(e => e.xts_resettype, xts_customautonumber.Options.xts_resettype.None);
            customAutoNumber.Set(e => e.xts_entitynamevalue, "xts_rewardtransaction");
            customAutoNumber.Set(e => e.xts_segmentformat, "[##]/[MM]/[YY]");
            customAutoNumber.Set(e => e.xts_segmentformatnumber, "##");
            customAutoNumber.Set(e => e.xts_segmentformatdate, "yy-MM");
            customAutoNumber.Set(e => e.xts_attributenamevalue, "xts_rewardnumber");
            customAutoNumber.Set(e => e.xts_TransactionDateAttributeNameValue, "xts_transactiondate");
            Db["CUSTOM-AUTO-NUMBER-001"] = customAutoNumber;
            #endregion

            #region mock custom auto number index
            var customAutonumberIndex = new xts_customautonumberindex();
            customAutonumberIndex.Set(x => x.xts_CustomAutonumberId, customAutoNumber.ToEntityReference());
            customAutonumberIndex.Set(x => x.xts_lastindex, 1);
            Db["CUSTOM-AUTO-NUMBER-INDEX-001"] = customAutonumberIndex;
            #endregion

            #region define entity meta data response
            var entityMetaData = new EntityMetadata();
            entityMetaData.LogicalName = "businessunit";
            var property = typeof(EntityMetadata).GetProperty("PrimaryNameAttribute");
            property.SetValue(entityMetaData, "name", null);
            #endregion

            #region mock validate attributes
            var responseEntity = new RetrieveEntityResponse();
            responseEntity["EntityMetadata"] = entityMetaData;
            Test.Service.Execute(Arg.Any<RetrieveEntityRequest>())
                .Returns(responseEntity);
            #endregion

            #region call create counter custom auto number
            new CreateCounterCustomAutoNumber(Context).Execute();
            #endregion

            #region assert
            Assert.Equal("02/01/17", Input.Get(e => e.xts_rewardnumber));
            #endregion
        }

        [Fact]
        public void CreateCounterCustomAutoNumber_FirstRecordWithBUandTransactionDate_ShouldReturnOK()
        {
            #region mock business unit
            var businessUnit = new BusinessUnit();
            businessUnit.Id = Guid.NewGuid();
            businessUnit.Set(e => e.Name, "tsad");
            Db["BU-001"] = businessUnit;
            #endregion

            #region define input
            var entity = new xts_rewardtransaction();
            entity.Set(x => x.xts_businessunitid, businessUnit.ToEntityReference());
            entity.Set(e => e.xts_transactiondate, DateTime.Now);
            Reference = entity;
            #endregion

            #region mock custom auto number
            var customAutoNumber = new xts_customautonumber();
            customAutoNumber.Id = Guid.NewGuid(); ;
            customAutoNumber.Set(e => e.xts_resettype, xts_customautonumber.Options.xts_resettype.None);
            customAutoNumber.Set(e => e.xts_entitynamevalue, "xts_rewardtransaction");
            customAutoNumber.Set(e => e.xts_segmentformat, "[##]/[MM]/[YY]/[BU]");
            customAutoNumber.Set(e => e.xts_segmentformatnumber, "##");
            customAutoNumber.Set(e => e.xts_segmentformatdate, "yy-MM");
            customAutoNumber.Set(e => e.xts_attributenamevalue, "xts_rewardnumber");
            customAutoNumber.Set(e => e.xts_TransactionDateAttributeNameValue, "xts_transactiondate");
            customAutoNumber.Set(e => e.xts_BusinessUnitAttributeNameValue, "xts_businessunitid");
            Db["CUSTOM-AUTO-NUMBER-001"] = customAutoNumber;
            #endregion

            #region define entity meta data response
            var entityMetaData = new EntityMetadata();
            entityMetaData.LogicalName = "businessunit";
            var property = typeof(EntityMetadata).GetProperty("PrimaryNameAttribute");
            property.SetValue(entityMetaData, "name", null);
            #endregion

            #region mock validate attributes
            var responseEntity = new RetrieveEntityResponse();
            responseEntity["EntityMetadata"] = entityMetaData;
            Test.Service.Execute(Arg.Any<RetrieveEntityRequest>())
                .Returns(responseEntity);
            #endregion

            #region call create counter custom auto number
            new CreateCounterCustomAutoNumber(Context).Execute();
            #endregion

            #region assert
            Assert.Equal("01/01/17/tsad", Input.Get(e => e.xts_rewardnumber));
            #endregion
        }

        [Fact]
        public void CreateCounterCustomAutoNumber_NextRecordWithBUandTransactionDate_ShouldReturnOK()
        {
            #region mock business unit
            var businessUnit = new BusinessUnit();
            businessUnit.Id = Guid.NewGuid();
            businessUnit.Set(e => e.Name, "tsad");
            Db["BU-001"] = businessUnit;
            #endregion

            #region define input
            var entity = new xts_rewardtransaction();
            entity.Set(x => x.xts_businessunitid, businessUnit.ToEntityReference());
            entity.Set(e => e.xts_transactiondate, DateTime.Now);
            Reference = entity;
            #endregion

            #region mock custom auto number
            var customAutoNumber = new xts_customautonumber();
            customAutoNumber.Id = Guid.NewGuid(); ;
            customAutoNumber.Set(e => e.xts_resettype, xts_customautonumber.Options.xts_resettype.None);
            customAutoNumber.Set(e => e.xts_entitynamevalue, "xts_rewardtransaction");
            customAutoNumber.Set(e => e.xts_segmentformat, "[##]/[MM]/[YY]/[BU]");
            customAutoNumber.Set(e => e.xts_segmentformatnumber, "##");
            customAutoNumber.Set(e => e.xts_segmentformatdate, "yy-MM");
            customAutoNumber.Set(e => e.xts_attributenamevalue, "xts_rewardnumber");
            customAutoNumber.Set(e => e.xts_TransactionDateAttributeNameValue, "xts_transactiondate");
            customAutoNumber.Set(e => e.xts_BusinessUnitAttributeNameValue, "xts_businessunitid");
            Db["CUSTOM-AUTO-NUMBER-001"] = customAutoNumber;
            #endregion

            #region mock custom auto number index
            var customAutonumberIndex = new xts_customautonumberindex();
            customAutonumberIndex.Set(x => x.xts_CustomAutonumberId, customAutoNumber.ToEntityReference());
            customAutonumberIndex.Set(x => x.xts_lastindex, 1);
            Db["CUSTOM-AUTO-NUMBER-INDEX-001"] = customAutonumberIndex;
            #endregion

            #region define entity meta data response
            var entityMetaData = new EntityMetadata();
            entityMetaData.LogicalName = "businessunit";
            var property = typeof(EntityMetadata).GetProperty("PrimaryNameAttribute");
            property.SetValue(entityMetaData, "name", null);
            #endregion

            #region mock validate attributes
            var responseEntity = new RetrieveEntityResponse();
            responseEntity["EntityMetadata"] = entityMetaData;
            Test.Service.Execute(Arg.Any<RetrieveEntityRequest>())
                .Returns(responseEntity);
            #endregion

            #region call create counter custom auto number
            new CreateCounterCustomAutoNumber(Context).Execute();
            #endregion

            #region assert
            Assert.Equal("02/01/17/tsad", Input.Get(e => e.xts_rewardnumber));
            #endregion
        }

    }
}

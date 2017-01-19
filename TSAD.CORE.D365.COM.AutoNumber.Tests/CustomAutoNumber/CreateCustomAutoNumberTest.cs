using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using NSubstitute;
using System;
using TSAD.CORE.D365.COM.AutoNumber.CustomAutoNumber;
using TSAD.CORE.D365.Entities;
using TSAD.XRM.Framework;
using TSAD.XRM.Framework.Plugin;
using TSAD.XRM.TestFramework.Auto365;
using Xunit;

namespace TSAD.CORE.D365.COM.AutoNumber.Tests.CustomAutoNumber
{
    public class CreateCustomAutoNumberTest : Auto365BaseTest<xts_customautonumber>
    {
        [Fact]
        public void CreateCustomAutoNumber_ShouldReturnOK()
        {
            #region define entity meta data response
            var entityMetaData = new EntityMetadata();
            entityMetaData.LogicalName = "xts_car";
            entityMetaData.DisplayName = new Label()
            {
                UserLocalizedLabel = new LocalizedLabel()
                {
                    Label = "Car"
                }
            };
            StringAttributeMetadata sam = new StringAttributeMetadata()
            {
                LogicalName = "xts_carnumber"
            };

            AttributeMetadata[] ams = new AttributeMetadata[1] { sam };

            var property = typeof(EntityMetadata).GetProperty("Attributes");
            property.SetValue(entityMetaData, ams, null);
            #endregion

            #region mock validate entity
            var entityMetaDatas = new EntityMetadata[1];
            entityMetaDatas[0] = entityMetaData;
            var responseEntities = new RetrieveAllEntitiesResponse();
            responseEntities["EntityMetadata"] = entityMetaDatas;
            Test.Service.Execute(Arg.Any<RetrieveAllEntitiesRequest>())
                .Returns(responseEntities);
            #endregion

            #region mock validate attributes
            var responseEntity = new RetrieveEntityResponse();
            responseEntity["EntityMetadata"] = entityMetaData;
            Test.Service.Execute(Arg.Any<RetrieveEntityRequest>())
                .Returns(responseEntity);
            #endregion

            #region mock sdk message
            var sdkMessage = new SdkMessage { Id = Guid.NewGuid() };
            sdkMessage.Set(e => e.Name, PluginMessage.Create);
            Db["SDK-MESSAGE-001"] = sdkMessage;
            #endregion

            #region mock sdk message filter
            var sdkMessageFilter = new SdkMessageFilter { Id = Guid.NewGuid() };
            sdkMessageFilter.Set(e => e.SdkMessageId, sdkMessage.ToEntityReference());
            sdkMessageFilter.Set(e => e.PrimaryObjectTypeCode, "xts_car");
            Db["SDK-MESSAGE-FILTER-001"] = sdkMessageFilter;
            #endregion

            #region mock plugin type
            var pluginType = new PluginType { Id = Guid.NewGuid() };
            pluginType.Set(e => e.AssemblyName, "TSAD.CORE.D365.COM.AutoNumber.Plugins");
            pluginType.Set(e => e.TypeName, "TSAD.CORE.D365.COM.AutoNumber.Plugins.Generic.PreGenericCustomAutoNumberCreate");
            Db["PLUGIN-TYPE-001"] = pluginType;
            #endregion            

            #region define input parameters
            var customAutoNumber = new xts_customautonumber();
            customAutoNumber.Set(x => x.xts_attributenamevalue, "xts_carnumber");
            customAutoNumber.Set(x => x.xts_entitynamevalue, "xts_car");
            customAutoNumber.Set(x => x.xts_segmentformat, "[####]");
            Reference = customAutoNumber;
            #endregion

            #region call create custom auto number
            new CreateCustomAutoNumber(Context).Execute();
            #endregion

            #region assert
            Assert.Equal(string.Empty, Input.Get(e => e.xts_segmentformatdate));
            Assert.NotEqual(string.Empty, Input.Get(e => e.xts_segmentformatnumber));
            Assert.Equal("PreCarAutonumberCreate", Input.Get(e => e.xts_pluginstepid));
            #endregion
        }
    }
}

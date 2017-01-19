using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using TSAD.CORE.D365.Entities;
using TSAD.XRM.Framework;
using TSAD.XRM.Framework.Auto365.Plugin;

namespace TSAD.CORE.D365.COM.AutoNumber.CustomAutoNumber
{
    /// <summary>
    /// This class is used for create custom auto number, once it called, it will create step of specifiec entity
    /// </summary>
    public class CreateCustomAutoNumber : Auto365BaseOperation<xts_customautonumber>
    {
        #region constant of this class
        private const string CREATE_MESSAGE_NAME = "Create";
        private const string PROJECT_NAME = "TSAD.CORE.D365.COM.AutoNumber.Plugins";
        private const string CLASS_NAME = "TSAD.CORE.D365.COM.AutoNumber.Plugins.Generic.PreGenericCustomAutoNumberCreate";
        private const string PATTERN = @"(\[#{1,}\]|\[BU\]|\[M{2,4}\]|\[Y{2,4}\])";
        private const string SHARP = "#";
        private const string BU = "BU";
        private readonly string[] yearFormats = { "[YYYY]", "[YYY]", "[YY]" };
        private readonly string[] monthFormats = { "[MM]", "[MMM]", "[MMMM]" };
        #endregion

        #region main methods
        public CreateCustomAutoNumber(IAuto365TransactionContext<xts_customautonumber> context) : base(context)
        {
        }

        protected override void HandleExecute()
        {
            string entityName = Get(e => e.xts_entitynamevalue).ToString();
            string attributeName = Get(e => e.xts_attributenamevalue).ToString();
            string dateFormat, numberFormat, entityDisplayName;
            dateFormat = numberFormat = entityDisplayName = string.Empty;
            
            #region check is entity has custom auto number
            if (IsEntityHasCustomAutoNumber(entityName))
                throw Context.Error("CAN0001", entityName);
            #endregion

                #region validate entity
            if (!ValidateEntity(entityName, out entityDisplayName))
                throw Context.Error("CAN0002");
            #endregion

            #region validate attribute
            if (!ValidateAttributeName(entityName, attributeName))
                throw Context.Error("CAN0003");
            #endregion

            #region validate segment format            
            ValidateSegmentFormat(Get(e => e.xts_segmentformat), out dateFormat, out numberFormat);
            #endregion

            #region get step id
            var stepName = GetStepId(entityDisplayName).ToString();
            #endregion

            Set(e => e.xts_pluginstepid, stepName);
            Set(e => e.xts_segmentformatdate, dateFormat);
            Set(e => e.xts_segmentformatnumber, numberFormat);
        }
        #endregion

        #region private methods

        /// <summary>
        /// This method is for validate entity name
        /// </summary>
        /// <param name="entityName">entity name</param>
        /// <returns>true if valid</returns>
        private bool ValidateEntity(string entityName, out string displayName)
        {
            bool isValid = false;
            displayName = string.Empty;

            // Define entity request
            var entityRequest = new RetrieveAllEntitiesRequest
            {
                RetrieveAsIfPublished = true,
                EntityFilters = EntityFilters.Entity
            };

            // Call execute to retrieve entities
            var entityResult = (RetrieveAllEntitiesResponse)Service.Execute(entityRequest);
            int isFound = entityResult.EntityMetadata.Where(x => x.LogicalName == entityName).Count();
            
            // Check entity is exist or not
            if (isFound > 0)
            {
                displayName = entityResult.EntityMetadata.Where(x => x.LogicalName == entityName).Single().DisplayName.UserLocalizedLabel.Label;
                isValid = true;
            }

            return isValid;
        }

        /// <summary>
        /// This method is for validate attribute name on specifiec entity name
        /// </summary>
        /// <param name="entityName">entity name</param>
        /// <param name="attributeName">attribute name</param>
        /// <returns>true if valid</returns>
        private bool ValidateAttributeName(string entityName, string attributeName)
        {
            bool isValid = false;

            // Define entity request
            RetrieveEntityRequest entityRequest = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = entityName
            };

            // Call execute to retrieve entities
            var result = (RetrieveEntityResponse)Service.Execute(entityRequest);

            // Check field is exist or not
            if (result.EntityMetadata.Attributes.FirstOrDefault(element => element.LogicalName == attributeName) != null)
            {
                isValid = true;
            }

            return isValid;
        }

        /// <summary>
        /// This method is for validate segment format
        /// </summary>
        /// <param name="segmentFormat">segment format</param>
        /// <param name="segmentFormatDate">OUT : segment format date</param>
        /// <param name="segmentFormatNumber">OUT : segment format number</param>
        private void ValidateSegmentFormat(string segmentFormat, out string segmentFormatDate, out string segmentFormatNumber)
        {
            var matches = Regex.Matches(segmentFormat, PATTERN);

            string numberFormat, dateFormat, yearFormat, monthFormat;
            numberFormat = dateFormat = yearFormat = monthFormat = string.Empty;
            int checkCount = 0;

            if (matches.Count > 0)
            {
                for (int i = 0; i < matches.Count; i++)
                {
                    if (matches[i].Groups[1].ToString().Contains(SHARP))
                    {
                        numberFormat = matches[i].Groups[1].ToString();
                        checkCount += 1;
                    }
                    else if (yearFormats.Contains(matches[i].Groups[0].ToString()))
                    {
                        yearFormat = matches[i].Groups[1].ToString();
                        checkCount += 1;
                    }
                    else if (monthFormats.Contains(matches[i].Groups[0].ToString()))
                    {
                        monthFormat = matches[i].Groups[1].ToString();
                        checkCount += 1;
                    }
                    else if (matches[i].Groups[1].ToString().Contains(BU))
                    {
                        checkCount += 1;
                    }
                }

                if (string.IsNullOrEmpty(numberFormat))
                    throw Context.Error("CAN0004");

                if (matches.Count != checkCount)
                    if (string.IsNullOrEmpty(yearFormat) | string.IsNullOrEmpty(monthFormat))
                        throw Context.Error("CAN0005");
            }
            else
            {
                throw Context.Error("CAN0006");
            }

            segmentFormatDate = string.Join("-", new string[] { (!string.IsNullOrEmpty(yearFormat)) ? yearFormat.ToLower().Replace("[", string.Empty).Replace("]", string.Empty) : string.Empty, monthFormat.Replace("[", string.Empty).Replace("]", string.Empty) }.Where(s => !String.IsNullOrEmpty(s)));
            segmentFormatNumber = numberFormat.Replace("[", string.Empty).Replace("]", string.Empty);
        }

        /// <summary>
        /// This method is for generate step id for specifiec entity
        /// </summary>
        /// <returns>plugin step id</returns>
        private string GetStepId(string entityDisplayName)
        {
            string stepName = string.Format("Pre{0}AutonumberCreate", entityDisplayName.Replace(" ", string.Empty));
            
            var sdkMsgId = QuerySDKMessage();
            if (sdkMsgId != Guid.Empty)
            {
                var sdkMsgFilterID = QuerySDKMessageFilter(Get(e => e.xts_entitynamevalue).ToString(), sdkMsgId);
                if (sdkMsgFilterID != Guid.Empty)
                {
                    var pluginTypeId = QueryPluginType();
                    if (pluginTypeId != Guid.Empty)
                    {
                        var step = new SdkMessageProcessingStep();
                        step.Set(e => e.Mode, SdkMessageProcessingStep.Options.Mode.Synchronous);
                        step.Set(e => e.Name, stepName);
                        step.Set(e => e.Rank, new int?(1));
                        step.Set(e => e.SdkMessageId, new EntityReference(SdkMessage.EntityLogicalName, sdkMsgId));
                        step.Set(e => e.SdkMessageFilterId, new EntityReference(SdkMessageFilter.EntityLogicalName, sdkMsgFilterID));
                        step.Set(e => e.EventHandler, new EntityReference(PluginType.EntityLogicalName, pluginTypeId));
                        step.Set(e => e.Stage, SdkMessageProcessingStep.Options.Stage.PreOperation);
                        step.Set(e => e.SupportedDeployment, SdkMessageProcessingStep.Options.SupportedDeployment.ServerOnly);

                        Service.Create(step);
                    }
                }
            }
            return stepName;
        }

        /// <summary>
        /// This methos is for query sdk message
        /// </summary>
        /// <returns>Guid of sdk message</returns>
        private Guid QuerySDKMessage()
        {
            Guid resultId = Guid.Empty;

            // Define query attribute for sdk message
            QueryByAttribute queryByAttribute = new QueryByAttribute()
            {
                EntityName = SdkMessage.EntityLogicalName,
                ColumnSet = new ColumnSet()
                {
                    AllColumns = true
                }
            };

            // Add message name value to name attributes
            QueryByAttribute qba = queryByAttribute;
            qba.AddAttributeValue(Helper.Name<SdkMessage>(e => e.Name), CREATE_MESSAGE_NAME);

            var result = Service.RetrieveMultiple(qba);
            if (result.Entities.Count > 0)
            {
                resultId = result.Entities[0].Id;
            }

            return resultId;
        }

        /// <summary>
        /// This method is for query sdk message filter
        /// </summary>
        /// <param name="entityName">entity name</param>
        /// <param name="sdkMessageId">sdk message id</param>
        /// <returns>Guid of sdk message filter</returns>
        private Guid QuerySDKMessageFilter(string entityName, Guid sdkMessageId)
        {
            Guid resultId = Guid.Empty;

            // Define query attribute for sdk message filter
            QueryByAttribute queryByAttribute = new QueryByAttribute()
            {
                EntityName = SdkMessageFilter.EntityLogicalName,
                ColumnSet = new ColumnSet()
                {
                    AllColumns = true
                }
            };

            // Add entity name value to primaryobjecttypecode attributes
            // Add sdk message id to sdkmessageid attributes
            QueryByAttribute qba = queryByAttribute;
            qba.AddAttributeValue(Helper.Name<SdkMessageFilter>(e => e.PrimaryObjectTypeCode), entityName);
            qba.AddAttributeValue(Helper.Name<SdkMessageFilter>(e => e.SdkMessageId), sdkMessageId);

            var result = Service.RetrieveMultiple(qba);
            if (result.Entities.Count > 0)
            {
                resultId = result.Entities[0].Id;
            }

            return resultId;
        }

        /// <summary>
        /// This method is for query plugin type
        /// </summary>
        /// <returns>Guid of plugin type</returns>
        private Guid QueryPluginType()
        {
            Guid resultId = Guid.Empty;

            QueryByAttribute queryByAttribute = new QueryByAttribute()
            {
                EntityName = PluginType.EntityLogicalName,
                ColumnSet = new ColumnSet()
                {
                    AllColumns = true
                }
            };

            // Add project name value to assemblyname attributes
            // Add class name value to typename attributes
            QueryByAttribute qba = queryByAttribute;
            qba.AddAttributeValue(Helper.Name<PluginType>(e => e.AssemblyName), PROJECT_NAME);
            qba.AddAttributeValue(Helper.Name<PluginType>(e => e.TypeName), CLASS_NAME);

            var result = Service.RetrieveMultiple(qba);
            if (result.Entities.Count > 0)
            {
                resultId = result.Entities[0].Id;
            }

            return resultId;
        }

        /// <summary>
        /// This method is used for query auto number by soecifiec entity
        /// </summary>
        /// <param name="entityName">entity name</param>
        /// <returns>xts_customautonumber</returns>
        private bool IsEntityHasCustomAutoNumber(string entityName)
        {
            bool isExist = false;

            // Define query attribute for sdk message
            QueryByAttribute queryByAttribute = new QueryByAttribute()
            {
                EntityName = xts_customautonumber.EntityLogicalName,
                ColumnSet = new ColumnSet(true)
            };

            queryByAttribute.AddAttributeValue(Helper.Name<xts_customautonumber>(e => e.xts_entitynamevalue), entityName);

            var result = Service.RetrieveMultiple(queryByAttribute);
            if (result.Entities.Count > 0)
                isExist = true;

            return isExist;
        }

        #endregion
    }
}

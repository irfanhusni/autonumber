using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSAD.CORE.D365.Entities;
using TSAD.XRM.Framework.Auto365.Plugin;
using TSAD.XRM.Framework;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Text.RegularExpressions;

namespace TSAD.CORE.D365.COM.AutoNumber.CustomAutoNumber
{
    public class CreateCustomAutoNumber : Auto365BaseOperation<xts_customautonumber>
    {
        #region constant of this class
        private const string CREATE_MESSAGE_NAME = "Create";
        private const string PROJECT_NAME = "TSAD.CORE.D365.COM.AutoNumber.Plugins";
        private const string CLASS_NAME = "TSAD.CORE.D365.COM.AutoNumber.Plugins.Generic.PreGenericCustomAutoNumberCreate";
        private const string PATTERN = @"\[(.*?)\]";
        private const string SHARP = "#";
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
            #region validate entity
            if (!ValidateEntity(entityName))
                throw new InvalidPluginExecutionException("Entity is doesn't exist");
            #endregion

            #region validate attribute
            if (!ValidateAttributeName(entityName, attributeName))
                throw new InvalidPluginExecutionException("Attribute is doesn't exist");
            #endregion

            #region validate segment format
            string dateFormat, numberFormat;
            dateFormat = numberFormat = string.Empty;
            ValidateSegmentFormat(Get(e => e.xts_segmentformat), out dateFormat, out numberFormat);
            #endregion

            var stepId = GetStepId().ToString();
            

            Set(e => e.xts_pluginstepid, stepId);
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
        private bool ValidateEntity(string entityName)
        {
            bool isValid = false;
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
                    else if(monthFormats.Contains(matches[i].Groups[0].ToString()))
                    {
                        monthFormat = matches[i].Groups[1].ToString();
                        checkCount += 1;
                    }
                }

                if (string.IsNullOrEmpty(numberFormat))
                    throw new InvalidPluginExecutionException("Invalid format of number");

                if (matches.Count != checkCount)
                {
                    if (string.IsNullOrEmpty(yearFormat) | string.IsNullOrEmpty(monthFormat))
                        throw new InvalidPluginExecutionException("Invalid format on Date");
                }
            }
            else
            {
                throw new InvalidPluginExecutionException("Invalid segment format");
            }

            segmentFormatDate = string.Join("-", new string[] { yearFormat.ToLower(), monthFormat }.Where(s => !String.IsNullOrEmpty(s)));
            segmentFormatNumber = numberFormat;
        }

        /// <summary>
        /// This method is for generate step id for specifiec entity
        /// </summary>
        /// <returns>plugin step id</returns>
        private Guid GetStepId()
        {
            Guid resultId = Guid.Empty;

            var sdkMsgId = QuerySDKMessage();
            if (sdkMsgId != Guid.Empty)
            {
                var sdkMsgFilterID = QuerySDKMessageFilter(Get(e => e.xts_entitynamevalue).ToString(), sdkMsgId);
                if(sdkMsgFilterID != Guid.Empty)
                {
                    var pluginTypeId = QueryPluginType();
                    if(pluginTypeId != Guid.Empty)
                    {
                        var step = new SdkMessageProcessingStep();                        
                        step.Set(e => e.Mode, SdkMessageProcessingStep.Options.Mode.Synchronous);
                        step.Set(e => e.Name, string.Format("Counter for the {0} Entity ", Get(e => e.xts_entitynamevalue).ToString()));
                        step.Set(e => e.Rank, new int?(1));
                        step.Set(e => e.SdkMessageId, new EntityReference(SdkMessage.EntityLogicalName, sdkMsgId));
                        step.Set(e => e.SdkMessageFilterId, new EntityReference(SdkMessageFilter.EntityLogicalName, sdkMsgFilterID));
                        step.Set(e => e.EventHandler, new EntityReference(PluginType.EntityLogicalName, pluginTypeId));
                        step.Set(e => e.Stage, SdkMessageProcessingStep.Options.Stage.PreOperation);
                        step.Set(e => e.SupportedDeployment, SdkMessageProcessingStep.Options.SupportedDeployment.ServerOnly);
                        
                        resultId = Service.Create(step);
                    }                    
                }
            }

            return resultId;
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
            qba.Attributes.Add("name");
            qba.Values.Add(CREATE_MESSAGE_NAME);

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
            qba.Attributes.Add("primaryobjecttypecode");
            qba.Values.Add(entityName);
            qba.Attributes.Add("sdkmessageid");
            qba.Values.Add(sdkMessageId);

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
            qba.Attributes.Add("assemblyname");
            qba.Values.Add(PROJECT_NAME);
            qba.Attributes.Add("typename");
            qba.Values.Add(CLASS_NAME);

            var result = Service.RetrieveMultiple(qba);
            if (result.Entities.Count > 0)
            {
                resultId = result.Entities[0].Id;
            }

            return resultId;
        }        

       
        #endregion
    }
}

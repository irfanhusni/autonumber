using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using TSAD.XRM.Framework.Auto365.Plugin;
using TSAD.CORE.D365.Entities;
using Microsoft.Xrm.Sdk.Query;
using System.Globalization;
using Microsoft.Xrm.Sdk.Messages;
using TSAD.XRM.Framework;
using Microsoft.Xrm.Sdk.Metadata;

namespace TSAD.CORE.D365.COM.AutoNumber.Generic
{
    public class CreateCounterCustomAutoNumber : Auto365BaseOperation
    {
        #region constant
        private const string BU_PATTERN = "[BU]";
        private const string SEPARATOR = "-";
        private const string YEAR = "y";
        private const string MONTH = "M";
        #endregion

        public CreateCounterCustomAutoNumber(IAuto365TransactionContext<Entity> context) : base(context)
        {
        }

        protected override void HandleExecute()
        {
            int latestNumber = 0;
            int resetBy = 0;
            int newLatestNumber = 0;
            string latestDate, segmentFormat, segmentFormatDate, newLatestdate, buName, period;
            latestDate = segmentFormat = segmentFormatDate = newLatestdate = buName = period = string.Empty;
            DateTime transactionDate;
            bool isReset = false;

            var customAutoNumber = QueryCustomAutoNumber(Context.Input.LogicalName);
            var customAutonumberIndex = QueryCustomAutoNumberIndex(customAutoNumber.Id);
            if (customAutoNumber != null)
            {
                #region populate entity from query custom auto number response
                resetBy = customAutoNumber.Get(e => e.xts_resettype) != null ? ((OptionSetValue)customAutoNumber.Get(e => e.xts_resettype)).Value : 0;
                segmentFormatDate = customAutoNumber.Get(e => e.xts_segmentformatdate) != null ? customAutoNumber.Get(e => e.xts_segmentformatdate) : string.Empty;                
                buName = customAutoNumber.Get(e => e.xts_BusinessUnitAttributeNameValue) != null ? GetBusinessUnitName((EntityReference)Context.Input.Attributes[customAutoNumber.Get(e => e.xts_BusinessUnitAttributeNameValue)]) : string.Empty;
                transactionDate = customAutoNumber.Get(e => e.xts_TransactionDateAttributeNameValue) != null ? GetTransactionDate(customAutoNumber.Get(e => e.xts_TransactionDateAttributeNameValue)) : DateTime.Now;
                if(customAutonumberIndex != null)
                {
                    latestDate = customAutonumberIndex.Get(e => e.xts_Period) != null ? customAutonumberIndex.Get(e => e.xts_Period) : string.Empty;
                    latestNumber = (int)(customAutonumberIndex.Get(e => e.xts_lastindex) != null ? customAutonumberIndex.Get(e => e.xts_lastindex) : 0);
                }
                #endregion


                #region get latest number
                newLatestNumber = GetLatestNumber(
                    resetBy,
                    latestDate,
                    segmentFormatDate,
                    latestNumber, transactionDate, out isReset);
                #endregion

                #region get segment format
                segmentFormat = GetSegmentFormat(customAutoNumber, newLatestNumber, buName, transactionDate, out newLatestdate, out period);
                #endregion

                #region check if need reset number
                if (isReset | customAutonumberIndex == null)
                    CreateCustomAutoNumberIndex(customAutoNumber.Id, period, newLatestNumber, transactionDate, customAutoNumber.Get(e => e.xts_customautonumbercode));
                #endregion              

                if (!isReset && customAutonumberIndex != null)
                {
                    var autoNumberIndex = new xts_customautonumberindex();
                    autoNumberIndex.Set(e => e.Id, customAutonumberIndex.Id);
                    autoNumberIndex.Set(e => e.xts_lastindex, newLatestNumber + 1);
                    autoNumberIndex.Set(e => e.xts_lastindexgenerateddate, transactionDate);
                    autoNumberIndex.RowVersion = customAutonumberIndex.RowVersion;

                    if (!customAutonumberIndex.ContainsAny(e => e.xts_Period))
                    {
                        autoNumberIndex.Set(e => e.xts_Period, period);
                    }
                    if (!customAutonumberIndex.ContainsAny(e => e.xts_name))
                    {
                        autoNumberIndex.Set(e => e.xts_name, string.Format("{0}_{1}", customAutoNumber.Get(e => e.xts_customautonumbercode), period));
                    }

                    UpdateCustomAutoNumber(autoNumberIndex);
                }
                Context.Input.Attributes[customAutoNumber.Get(e => e.xts_attributenamevalue)] = segmentFormat;
            }
        }

        #region private methods
        /// <summary>
        /// This method is used for query auto number by soecifiec entity
        /// </summary>
        /// <param name="entityName">entity name</param>
        /// <returns>xts_customautonumber</returns>
        private xts_customautonumber QueryCustomAutoNumber(string entityName)
        {
            xts_customautonumber customAutoNumber = null;

            // Define query attribute for sdk message
            QueryByAttribute queryByAttribute = new QueryByAttribute()
            {
                EntityName = xts_customautonumber.EntityLogicalName,
                ColumnSet = new ColumnSet(true)
            };

            queryByAttribute.AddAttributeValue(Helper.Name<xts_customautonumber>(e => e.xts_entitynamevalue), entityName);
            
            var result = Service.RetrieveMultiple(queryByAttribute);
            if (result.Entities.Count > 0)
            {
                customAutoNumber = result.Entities[0].ToEntity<xts_customautonumber>();
            }

            return customAutoNumber;
        }

        /// <summary>
        /// This method is used for update latest custom auto number
        /// </summary>
        /// <param name="entity">Entity of custom auto number</param>
        private void UpdateCustomAutoNumber(Entity entity)
        {
            UpdateRequest uRequest = new UpdateRequest()
            {
                Target = entity,
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            };

            var result = Service.Execute(uRequest);
            if (result == null)
                throw new InvalidPluginExecutionException(string.Format("Update entity {0} was failed", entity.LogicalName));

        }

        /// <summary>
        /// This method is used for get latest auto number 
        /// And also to check if latest auto number is need to reset or not
        /// </summary>
        /// <param name="resetBy">reset by daily, monthly, yearly </param>
        /// <param name="latestDate">latest date of custom auto number</param>
        /// <param name="segmentFormatDate">segment format date of custom auto numbe</param>
        /// <param name="latestAutoNumber">latest number of custom auto number</param>
        /// <returns>latest number</returns>
        private int GetLatestNumber(int resetBy, string latestDate, string segmentFormatDate, int latestAutoNumber, DateTime transactionDate, out bool isReset)
        {
            #region Check if need to reset by monthly or yearly
            int latestNumber = 0;

            DateTime dateFromdb = DateTime.Now;
            isReset = false;
            if (resetBy > 1)
            {
                if (!string.IsNullOrEmpty(latestDate))
                {
                    dateFromdb = DateTime.ParseExact(latestDate, segmentFormatDate.Replace(SEPARATOR, ""), CultureInfo.InvariantCulture);
                    switch (resetBy)
                    {
                        case 2:
                            if (dateFromdb.Year != transactionDate.Year)
                            {
                                latestNumber = 1;
                                isReset = true;
                            }
                            break;
                        case 3:
                            if (dateFromdb.Month != transactionDate.Month)
                            {
                                latestNumber = 1;
                                isReset = true;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            if (latestAutoNumber > 0 && !isReset)
            {
                latestNumber = latestAutoNumber;
            }
            else
            {
                latestNumber = 1;
            }
            #endregion

            return latestNumber;
        }

        /// <summary>
        /// This method is used for get segment format
        /// </summary>
        /// <param name="autoNumberEntity">auto number entity</param>
        /// <param name="latestNumber">latest number of custom auto number</param>
        /// <param name="latestDate">out as a latest date</param>
        /// <returns>segment format</returns>
        private string GetSegmentFormat(xts_customautonumber autoNumberEntity, int latestNumber, string buName, DateTime transactionDate, out string latestDate, out string periodDate)
        {
            #region get segment format
            string segmentFormat = autoNumberEntity.Get(e => e.xts_segmentformat);
            string dateTime = string.Empty;
            string yearFormat = string.Empty;
            string monthFormat = string.Empty;
            string rplcSegmentFormat = string.Empty;
            string segmentFormatDate = autoNumberEntity.Get(e => e.xts_segmentformatdate) != null ? autoNumberEntity.Get(e => e.xts_segmentformatdate) : string.Empty;
            string segmentNumber = latestNumber.ToString().PadLeft(autoNumberEntity.Get(e => e.xts_segmentformatnumber).Count(), '0');

            if (segmentFormat.Contains(BU_PATTERN))
            {
                rplcSegmentFormat = segmentFormat
                    .Replace(BU_PATTERN, buName);
            }

            if (!string.IsNullOrEmpty(segmentFormatDate))
            {
                if (segmentFormatDate.Contains(SEPARATOR))
                {
                    var splitFormat = segmentFormatDate.Split(SEPARATOR.ToCharArray());
                    yearFormat = splitFormat[0];
                    monthFormat = splitFormat[1];
                    if (string.IsNullOrEmpty(rplcSegmentFormat))
                    {
                        rplcSegmentFormat = segmentFormat
                        .Replace("[" + yearFormat.ToUpper() + "]", transactionDate.ToString(yearFormat));
                        rplcSegmentFormat = rplcSegmentFormat
                       .Replace("[" + monthFormat + "]", transactionDate.ToString(monthFormat));
                    }
                    else
                    {
                        rplcSegmentFormat = rplcSegmentFormat
                        .Replace("[" + yearFormat.ToUpper() + "]", transactionDate.ToString(yearFormat));
                        rplcSegmentFormat = rplcSegmentFormat
                       .Replace("[" + monthFormat + "]", transactionDate.ToString(monthFormat));
                    }
                }
                else if (segmentFormatDate.StartsWith(YEAR))
                {
                    yearFormat = segmentFormatDate;

                    if (string.IsNullOrEmpty(rplcSegmentFormat))
                    {
                        rplcSegmentFormat = segmentFormat
                        .Replace("[" + yearFormat.ToUpper() + "]", transactionDate.ToString(yearFormat));
                    }
                    else
                    {
                        rplcSegmentFormat = rplcSegmentFormat
                        .Replace("[" + yearFormat.ToUpper() + "]", transactionDate.ToString(yearFormat));
                    }
                }
                else if (segmentFormatDate.StartsWith(MONTH))
                {
                    monthFormat = segmentFormatDate;

                    if (string.IsNullOrEmpty(rplcSegmentFormat))
                    {
                        rplcSegmentFormat = segmentFormat
                        .Replace("[" + monthFormat + "]", transactionDate.ToString(monthFormat));
                    }
                    else
                    {
                        rplcSegmentFormat = rplcSegmentFormat
                        .Replace("[" + monthFormat + "]", transactionDate.ToString(monthFormat));
                    }
                }                     
            }

            if (string.IsNullOrEmpty(rplcSegmentFormat))
            {
                rplcSegmentFormat = segmentFormat
                    .Replace("[" + autoNumberEntity.Get(e => e.xts_segmentformatnumber) + "]", DateTime.Now.ToString(segmentNumber));
            }
            else
            {
                rplcSegmentFormat = rplcSegmentFormat
                    .Replace("[" + autoNumberEntity.Get(e => e.xts_segmentformatnumber) + "]", DateTime.Now.ToString(segmentNumber));
            }
            latestDate = transactionDate.ToString(string.Format("{0}{1}", yearFormat, monthFormat));
            periodDate = !string.IsNullOrEmpty(monthFormat) ? transactionDate.ToString("yyyyMM") : transactionDate.ToString("yyyy");
            #endregion            

            return rplcSegmentFormat;
        }

        /// <summary>
        /// This mehthod is used for get business unit name value
        /// </summary>
        /// <param name="ef">entity reference of look up field</param>
        /// <returns>look up name</returns>
        private string GetBusinessUnitName(EntityReference ef)
        {
            #region get entity metadata
            RetrieveEntityRequest entityRequest = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Attributes,
                LogicalName = ef.LogicalName
            };
            // Call execute to retrieve entities
            var result = (RetrieveEntityResponse)Service.Execute(entityRequest);
            #endregion

            #region get lookup name 
            var response = Service.Retrieve(ef.LogicalName, ef.Id, new ColumnSet(new string[] { result.EntityMetadata.PrimaryNameAttribute }));
            #endregion

            return response.Attributes[result.EntityMetadata.PrimaryNameAttribute].ToString();
        }

        /// <summary>
        /// This method is used for get transaction date
        /// </summary>
        /// <param name="attributeName">attribute name of look up</param>
        /// <returns>transaction date</returns>
        private DateTime GetTransactionDate(string attributeName)
        {
            return (DateTime)Context.Input.Attributes[attributeName];
        }

        /// <summary>
        /// This method is used for query custom auto number index that has relation with custom auto number
        /// </summary>
        /// <param name="id">id of custom auto number id</param>
        /// <returns>xts_customautonumberindex</returns>
        private xts_customautonumberindex QueryCustomAutoNumberIndex(Guid id)
        {
            xts_customautonumberindex customAutonumberIndex = null;

            // Define query attribute for sdk message
            QueryByAttribute queryByAttribute = new QueryByAttribute()
            {
                EntityName = xts_customautonumberindex.EntityLogicalName,
                ColumnSet = new ColumnSet()
                {
                    AllColumns = true
                }
            };

            queryByAttribute.AddAttributeValue(Helper.Name<xts_customautonumberindex>(e => e.xts_CustomAutonumberId), id);
            queryByAttribute.AddOrder("modifiedon", OrderType.Descending);

            var result = Service.RetrieveMultiple(queryByAttribute);
            if (result.Entities.Count > 0)
            {
                customAutonumberIndex = result.Entities[0].ToEntity<xts_customautonumberindex>();
            }

            return customAutonumberIndex;
        }

        private void CreateCustomAutoNumberIndex(Guid id, string period, int latestNumber, DateTime transactionDate, string customAutoNumberCode)
        {
            var autoNumberIndex = new xts_customautonumberindex();
            autoNumberIndex.Set(e => e.xts_lastindex, latestNumber + 1);
            autoNumberIndex.Set(e => e.xts_lastindexgenerateddate, transactionDate);
            autoNumberIndex.Set(e => e.xts_Period, period);
            autoNumberIndex.Set(e => e.xts_name, string.Format("{0}_{1}", customAutoNumberCode, period));
            autoNumberIndex.Set(e => e.xts_CustomAutonumberId, new EntityReference(xts_customautonumber.EntityLogicalName, id));

            Service.Create(autoNumberIndex);
        }
        #endregion
    }
}

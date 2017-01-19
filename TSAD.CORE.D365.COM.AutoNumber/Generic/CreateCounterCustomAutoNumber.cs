using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using TSAD.CORE.D365.Entities;
using TSAD.XRM.Framework;
using TSAD.XRM.Framework.Auto365.Plugin;

namespace TSAD.CORE.D365.COM.AutoNumber.Generic
{
    /// <summary>
    /// This class is used for generate auto number of specifiec entity,
    /// once it generated, it will create/update a record to custom auto number index entity
    /// it will be update period, last index and business unit if any
    /// </summary>
    public class CreateCounterCustomAutoNumber : Auto365BaseOperation
    {
        #region constant
        private const string BU_PATTERN = "[BU]";
        private const string SEPARATOR = "-";
        private const string YEAR = "y";
        private const string MONTH = "M";
        private const string PERIOD_MONTH_PATTERN = "yyyyMM";
        private const string PERIOD_YEAR_PATTERN = "yyyy";
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
            Guid buId = Get<EntityReference>("xts_businessunitid") != null ? Get<EntityReference>("xts_businessunitid").Id : Guid.Empty;

            var customAutoNumber = QueryCustomAutoNumber(Context.Input.LogicalName);
            var customAutonumberIndex = QueryCustomAutoNumberIndex(customAutoNumber.Id);
            if (customAutoNumber != null)
            {
                #region populate entity from query custom auto number and custom autonumber index response
                resetBy = customAutoNumber.Get(e => e.xts_resettype) != null ? ((OptionSetValue)customAutoNumber.Get(e => e.xts_resettype)).Value : 0;
                segmentFormatDate = customAutoNumber.Get(e => e.xts_segmentformatdate) != null ? customAutoNumber.Get(e => e.xts_segmentformatdate) : string.Empty;
                buName = customAutoNumber.Get(e => e.xts_BusinessUnitAttributeNameValue) != null ? GetBusinessUnitName(Get<EntityReference>(customAutoNumber.Get(e => e.xts_BusinessUnitAttributeNameValue))) : string.Empty;
                transactionDate = customAutoNumber.Get(e => e.xts_TransactionDateAttributeNameValue) != null ? GetTransactionDate(customAutoNumber.Get(e => e.xts_TransactionDateAttributeNameValue)) : DateTime.Now;
                if (customAutonumberIndex != null)
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
                segmentFormat = GetSegmentFormat(customAutoNumber, newLatestNumber, buName, transactionDate, out period);
                #endregion

                #region check if need reset number
                if (isReset | customAutonumberIndex == null)
                {
                    CreateCustomAutoNumberIndex(customAutoNumber.Id, period, newLatestNumber, transactionDate, customAutoNumber.Get(e => e.xts_customautonumbercode), buId);
                }
                else if (!isReset && customAutonumberIndex != null)
                {
                    var autoNumberIndex = new xts_customautonumberindex();
                    autoNumberIndex.Set(e => e.Id, customAutonumberIndex.Id);
                    autoNumberIndex.Set(e => e.xts_lastindex, newLatestNumber);
                    autoNumberIndex.Set(e => e.xts_lastindexgenerateddate, transactionDate);
                    autoNumberIndex.RowVersion = customAutonumberIndex.RowVersion;

                    if (buId != Guid.Empty)
                        autoNumberIndex.Set(e => e.xts_BusinessUnitId, new EntityReference(BusinessUnit.EntityLogicalName, buId));
                    else
                        autoNumberIndex.Set(e => e.xts_BusinessUnitId, null);

                    if (!customAutonumberIndex.ContainsAny(e => e.xts_Period))
                        autoNumberIndex.Set(e => e.xts_Period, period);
                    if (!customAutonumberIndex.ContainsAny(e => e.xts_name))
                        autoNumberIndex.Set(e => e.xts_name, string.Format("{0}_{1}", customAutoNumber.Get(e => e.xts_customautonumbercode), period));

                    UpdateEntityWithRowVersion(autoNumberIndex);
                }
                #endregion

                #region assign generated auto number to specifiec attribute
                Context.Input.Attributes[customAutoNumber.Get(e => e.xts_attributenamevalue)] = segmentFormat;
                #endregion

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
        /// This method is used for update entity using concurrency behaviour
        /// </summary>
        /// <param name="entity">Entity of custom auto number</param>
        private void UpdateEntityWithRowVersion(Entity entity)
        {
            UpdateRequest uRequest = new UpdateRequest()
            {
                Target = entity,
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            };

            Service.Execute(uRequest);            
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
            isReset = false;

            if (resetBy > 1)
            {
                if (!string.IsNullOrEmpty(latestDate))
                {
                    switch (resetBy)
                    {
                        // reset type is yearly
                        case 2:
                            if (Int32.Parse(latestDate) < transactionDate.Year)
                            {
                                latestNumber = 1;
                                isReset = true;
                            }
                            break;
                        // reset type is monthly
                        case 3:
                            if (Int32.Parse(latestDate.Substring(4, 2)) < transactionDate.Year && Int32.Parse(latestDate.Substring(4, 2)) < transactionDate.Month)
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
                latestNumber = latestAutoNumber + 1;
            }
            else if (latestAutoNumber == 0)
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
        /// <param name="buName">business unit name</param>
        /// <param name="transactionDate">transaction date of record</param>
        /// <param name="periodDate">return period for custom auto number index</param>
        /// <returns>segment format</returns>
        private string GetSegmentFormat(xts_customautonumber autoNumberEntity, int latestNumber, string buName, DateTime transactionDate, out string periodDate)
        {
            #region get segment format
            string segmentFormat = autoNumberEntity.Get(e => e.xts_segmentformat);
            string dateTime = string.Empty;
            string yearFormat = string.Empty;
            string monthFormat = string.Empty;
            string rplcSegmentFormat = string.Empty;
            string segmentFormatDate = autoNumberEntity.Get(e => e.xts_segmentformatdate) != null ? autoNumberEntity.Get(e => e.xts_segmentformatdate) : string.Empty;
            string segmentNumber = latestNumber.ToString().PadLeft(autoNumberEntity.Get(e => e.xts_segmentformatnumber).Count(), '0');
            periodDate = string.Empty;

            #region check if segment format contain BU pattern
            if (segmentFormat.Contains(BU_PATTERN))
            {
                rplcSegmentFormat = segmentFormat
                    .Replace(BU_PATTERN, buName);
            }
            #endregion

            #region check if segment format contain date pattern
            if (!string.IsNullOrEmpty(segmentFormatDate))
            {
                // check if format date contains month and year pattern
                if (segmentFormatDate.Contains(SEPARATOR))
                {
                    var splitFormat = segmentFormatDate.Split(SEPARATOR.ToCharArray());
                    yearFormat = splitFormat[0];
                    monthFormat = splitFormat[1];

                    if (string.IsNullOrEmpty(rplcSegmentFormat))
                    {
                        rplcSegmentFormat = segmentFormat
                        .Replace(string.Format("[{0}]", yearFormat.ToUpper()), transactionDate.ToString(yearFormat));
                        rplcSegmentFormat = rplcSegmentFormat
                       .Replace(string.Format("[{0}]", monthFormat), transactionDate.ToString(monthFormat));
                    }
                    else
                    {
                        rplcSegmentFormat = rplcSegmentFormat
                        .Replace(string.Format("[{0}]", yearFormat.ToUpper()), transactionDate.ToString(yearFormat));
                        rplcSegmentFormat = rplcSegmentFormat
                       .Replace(string.Format("[{0}]", monthFormat), transactionDate.ToString(monthFormat));
                    }
                }

                // check if format date only contain year pattern
                else if (segmentFormatDate.StartsWith(YEAR))
                {
                    yearFormat = segmentFormatDate;

                    if (string.IsNullOrEmpty(rplcSegmentFormat))
                    {
                        rplcSegmentFormat = segmentFormat
                        .Replace(string.Format("[{0}]", yearFormat.ToUpper()), transactionDate.ToString(yearFormat));
                    }
                    else
                    {
                        rplcSegmentFormat = rplcSegmentFormat
                        .Replace(string.Format("[{0}]",yearFormat.ToUpper()), transactionDate.ToString(yearFormat));
                    }
                }

                // check if format date only contain month pattern
                else if (segmentFormatDate.StartsWith(MONTH))
                {
                    monthFormat = segmentFormatDate;

                    if (string.IsNullOrEmpty(rplcSegmentFormat))
                    {
                        rplcSegmentFormat = segmentFormat
                        .Replace(string.Format("[{0}]", monthFormat), transactionDate.ToString(monthFormat));
                    }
                    else
                    {
                        rplcSegmentFormat = rplcSegmentFormat
                        .Replace(string.Format("[{0}]",monthFormat), transactionDate.ToString(monthFormat));
                    }
                }
            }

            #endregion

            #region generate auto number if consist # pattern
            if (string.IsNullOrEmpty(rplcSegmentFormat))
            {
                rplcSegmentFormat = segmentFormat
                    .Replace(string.Format("[{0}]",autoNumberEntity.Get(e => e.xts_segmentformatnumber)), segmentNumber);
            }
            else
            {
                rplcSegmentFormat = rplcSegmentFormat
                    .Replace(string.Format("[{0}]", autoNumberEntity.Get(e => e.xts_segmentformatnumber)), segmentNumber);
            }
            #endregion
            
            #region check if reset type is not none
            if (((OptionSetValue)autoNumberEntity.Get(e => e.xts_resettype)).Value > 1)
                periodDate = ((OptionSetValue)autoNumberEntity.Get(e => e.xts_resettype)).Value == 2 ? transactionDate.ToString(PERIOD_YEAR_PATTERN) : transactionDate.ToString(PERIOD_MONTH_PATTERN);
            #endregion

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
            return Get<DateTime>(attributeName);
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

        /// <summary>
        /// This method is used for create entity reference
        /// To custom autonumber index from custom auto number
        /// </summary>
        /// <param name="id">custom auto number id</param>
        /// <param name="period">period (yyyyMM)</param>
        /// <param name="latestNumber">latest number of custom auto number</param>
        /// <param name="transactionDate">transaction date of specifiec entity</param>
        /// <param name="customAutoNumberCode">custom auto number name</param>
        /// <param name="buId">business unit id</param>
        private void CreateCustomAutoNumberIndex(Guid id, string period, int latestNumber, DateTime transactionDate, string customAutoNumberCode, Guid buId)
        {
            var autoNumberIndex = new xts_customautonumberindex();
            autoNumberIndex.Set(e => e.xts_lastindex, latestNumber);
            autoNumberIndex.Set(e => e.xts_lastindexgenerateddate, transactionDate);
            autoNumberIndex.Set(e => e.xts_Period, period);
            autoNumberIndex.Set(e => e.xts_name, (!string.IsNullOrEmpty(period)) ? string.Format("{0}_{1}", customAutoNumberCode, period) : customAutoNumberCode);
            autoNumberIndex.Set(e => e.xts_CustomAutonumberId, new EntityReference(xts_customautonumber.EntityLogicalName, id));
            if (buId != Guid.Empty)
                autoNumberIndex.Set(e => e.xts_BusinessUnitId, new EntityReference(BusinessUnit.EntityLogicalName, buId));
            Service.Create(autoNumberIndex);
        }
        #endregion
    }
}

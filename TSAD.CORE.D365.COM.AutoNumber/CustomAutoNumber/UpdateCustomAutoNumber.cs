using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
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
    /// This class is used for update custom auto number, once it called, it will validate the segment format
    /// </summary>
    public class UpdateCustomAutoNumber : Auto365BaseOperation<xts_customautonumber>
    {
        #region constant
        private const string PATTERN = @"(\[#{1,}\]|\[BU\]|\[M{2,4}\]|\[Y{2,4}\])";
        private const string SHARP = "#";
        private const string BU = "BU";
        private const string SEPARATOR = "_";
        private const string PERIOD_MONTH_PATTERN = "yyyyMM";
        private const string PERIOD_YEAR_PATTERN = "yyyy";
        private readonly string[] yearFormats = { "[YYYY]", "[YYY]", "[YY]" };
        private readonly string[] monthFormats = { "[MM]", "[MMM]", "[MMMM]" };        
        #endregion

        public UpdateCustomAutoNumber(IAuto365TransactionContext<xts_customautonumber> context) : base(context)
        {
        }

        protected override void HandleExecute()
        {
            int resetType = Context.Input.ContainsAny(e => e.xts_resettype) ? Get(e => e.xts_resettype).Value : 0;
            string period = string.Empty;
            DateTime dt;
            string dateFormat, numberFormat;
            dateFormat = numberFormat = string.Empty;

            #region check if there's a change on reset type
            if (resetType > 0)
            {
                var customAutonumberIndex = QueryCustomAutoNumberIndex(Get(e => e.Id));
                if (customAutonumberIndex != null)
                {
                    switch (resetType)
                    {
                        // reset type is none
                        case 1:
                            period = string.Empty;
                            break;
                        // reset type is yearly
                        case 2:
                            dt = (DateTime)customAutonumberIndex.Get(e => e.xts_lastindexgenerateddate);
                            period = dt.ToString(PERIOD_YEAR_PATTERN);
                            break;
                        // reset type is monthly
                        case 3:
                            dt = (DateTime)customAutonumberIndex.Get(e => e.xts_lastindexgenerateddate);
                            period = dt.ToString(PERIOD_MONTH_PATTERN);
                            break;
                        default:
                            break;
                    }

                    var updCustomAutonumberIndex = new xts_customautonumberindex();
                    updCustomAutonumberIndex.Set(e => e.Id, customAutonumberIndex.Get(e => e.Id));
                    updCustomAutonumberIndex.Set(e => e.xts_Period, period);
                    updCustomAutonumberIndex.RowVersion = customAutonumberIndex.RowVersion;

                    if (customAutonumberIndex.Get(e => e.xts_name).Contains(SEPARATOR))
                        updCustomAutonumberIndex.Set(e => e.xts_name, string.Format("{0}_{1}", customAutonumberIndex.Get(e => e.xts_name).Split(SEPARATOR.ToCharArray())[0], period));
                    else
                        updCustomAutonumberIndex.Set(e => e.xts_name, customAutonumberIndex.Get(e => e.xts_name).Split('_')[0]);

                    UpdateEntityWithRowVersion(updCustomAutonumberIndex);
                }
            }
            #endregion

            #region check if there's a change on segment format
            if (Get(e => e.xts_segmentformat) != null)
            {
                ValidateSegmentFormat(Get(e => e.xts_segmentformat), out dateFormat, out numberFormat);
                Set(e => e.xts_segmentformatdate, dateFormat);
                Set(e => e.xts_segmentformatnumber, numberFormat);
            }
            #endregion
        }

        #region private methods
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
                {
                    if (string.IsNullOrEmpty(yearFormat) | string.IsNullOrEmpty(monthFormat))
                        throw Context.Error("CAN0005");
                }
            }
            else
            {
                throw Context.Error("CAN0006");
            }

            segmentFormatDate = string.Join("-", new string[] { (!string.IsNullOrEmpty(yearFormat)) ? yearFormat.ToLower().Replace("[", string.Empty).Replace("]", string.Empty) : string.Empty, monthFormat.Replace("[", string.Empty).Replace("]", string.Empty) }.Where(s => !String.IsNullOrEmpty(s)));
            segmentFormatNumber = numberFormat;
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

        #endregion
    }
}

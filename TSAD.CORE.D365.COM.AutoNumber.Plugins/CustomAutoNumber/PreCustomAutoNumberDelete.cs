// <copyright file="PreCustomAutoNumberDelete.cs" company="">
// Copyright (c) 2017 All Rights Reserved
// </copyright>
// <author></author>
// <date>1/2/2017 11:36:15 AM</date>
// <summary>Implements the PreCustomAutoNumberDelete Plugin.</summary>
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
// </auto-generated>

using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using TSAD.XRM.Framework.Auto365.Plugin;
using TSAD.CORE.D365.Entities;
using TSAD.CORE.D365.COM.AutoNumber.CustomAutoNumber;

namespace TSAD.CORE.D365.COM.AutoNumber.Plugins.CustomAutoNumber
{

    /// <summary>
    /// PreCustomAutoNumberDelete Plugin.
    /// </summary>    
    public class PreCustomAutoNumberDelete : Auto365BasePlugin<xts_customautonumber>, IPlugin
    {
        protected PreCustomAutoNumberDelete(string unsecure = null, string secure = null) : base(unsecure, secure)
        {
        }

        protected override void ExecuteCrmPlugin(IAuto365TransactionContext<xts_customautonumber> context)
        {
            new DeleteCustomAutoNumber(context).Execute();
        }
    }
}

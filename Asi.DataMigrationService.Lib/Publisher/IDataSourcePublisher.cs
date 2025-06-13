using Asi.DataMigrationService.Lib.Data.Models;
using Asi.DataMigrationService.Lib.Services;
using Asi.Soa.Core.DataContracts;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.Lib.Publisher
{
    /// <summary>   Interface for data source publisher. </summary>
    public interface IDataSourcePublisher
    {
        /// <summary>   Gets the name of the data source type. </summary>
        ///
        /// <value> The name of the data source type. </value>
        string DataSourceTypeName { get; }

        /// <summary>   Gets a list of names of the dependent publisher types. </summary>
        ///
        /// <value> A list of names of the dependent publisher types. </value>
        IList<string> DependentPublisherTypeNames { get; }

        /// <summary>   Gets the description. </summary>
        ///
        /// <value> The description. </value>
        string Title { get; }

        /// <summary>   Gets a value indicating whether this  is harvester. </summary>
        ///
        /// <value> True if this  is harvester, false if not. </value>
        bool IsHarvester { get; }

        /// <summary>   Gets a value indicating whether this  is validatable. </summary>
        ///
        /// <value> True if this  is validatable, false if not. </value>
        bool IsValidatable { get; }

        /// <summary>   Gets the type of the component. </summary>
        ///
        /// <value> The type of the component. </value>
        public Type UIComponentType { get; }

        /// <summary>   Creates user interface component. </summary>
        ///
        /// <param name="project">                  Identifier for the project. </param>
        /// <param name="projectDataSource">        The project data source. </param>
        /// <param name="sourceLoginInformation">   Information describing the source login. </param>
        /// <param name="additionalParamters">      (Optional) The additional paramters. </param>
        ///
        /// <returns>   The new user interface component. </returns>
        RenderFragment CreateUIComponent(Project project, ProjectDataSource projectDataSource, LoginInformation sourceLoginInformation, Dictionary<string, object> additionalParamters = null);

        /// <summary>   Initializes the asynchronous. </summary>
        ///
        /// <param name="context">  The context. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        Task InitializeAsync(PublishContext context);

        /// <summary>   Publish asynchronous. </summary>
        ///
        /// <param name="context">  The context. </param>
        /// <param name="group">    The group. </param>
        ///
        /// <returns>   An asynchronous result that yields the publish. </returns>
        Task<IServiceResponse<GroupSuccess>> PublishAsync(PublishContext context, ManifestDataSourceType group);

        /// <summary>   Validates the asynchronous. </summary>
        ///
        /// <param name="context">  The context. </param>
        /// <param name="group">    The group. </param>
        ///
        /// <returns>   An asynchronous result that yields the validate. </returns>
        Task<IServiceResponse<GroupSuccess>> ValidateAsync(PublishContext context, ManifestDataSourceType group);
    }
}
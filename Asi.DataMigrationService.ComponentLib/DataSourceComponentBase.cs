using Asi.Core.Interfaces;
using Asi.DataMigrationService.Core;
using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Lib.Data;
using Asi.DataMigrationService.Lib.Data.Models;
using Asi.DataMigrationService.Lib.Messages.Commands;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Queries;
using Asi.DataMigrationService.MessageQueue.Interfaces;
using Blazored.Modal;
using Blazored.Modal.Services;
using CsvHelper;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.ComponentLib
{
    public abstract class DataSourceComponentBase : ComponentBase
    {
        [Parameter]
        public DataSourcePublisherBase DataSourcePublisherBase { get; set; }

        [Parameter]
        public virtual string DataSourceTypeName { get; set; }

        [Parameter]
        public ProjectDataSource ProjectDataSource { get; set; }

        public int ProjectDataSourceId
        {
            get => ProjectDataSource.ProjectDataSourceId;
            set => ProjectDataSource.ProjectDataSourceId = value;
        }

        [Parameter]
        public Project Project { get; set; }

        public Func<SourceDataInfo, RenderFragment> SelectorDetailFormatting => DataSourcePublisherBase.SelectorDetailFormatting;

        public Func<RenderFragment> SelectorHeaderFormatting => DataSourcePublisherBase.SelectorHeaderFormatting;

        [Parameter]
        public LoginInformation SourceLoginInformation { get; set; }

        [Parameter]
        public string Title { get; set; }

        protected internal int ImportCount { get; set; }

        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [Inject]
        protected ICommonServiceHttpClientFactory ClientFactory { get; set; }

        [Inject]
        protected ApplicationDbContext DbContext { get; set; }

        protected bool IsEditMode { get; set; }

        protected bool IsNew => ProjectDataSourceId == 0;

        [Inject]
        protected IMessageQueueEndpoint MessageQueueEndpoint { get; set; }

        [Inject]
        protected IModalService Modal { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected IServiceProvider ServiceProvider { get; set; }

        [Inject]
        protected IProjectQueries ProjectQueries { get; set; }

        protected string SourceLoginSummary
        {
            get
            {
                return SourceLoginInformation.IsValidated
                    ? $"Url: {SourceLoginInformation.Uri}, User Name: {SourceLoginInformation.UserCredentials.UserName}"
                    : null;
            }
        }

        [Inject]
        protected IMatToaster Toaster { get; set; }

        protected async Task DeleteImportDataAsync()
        {
            var command = new DeleteImports(ProjectDataSource.ProjectDataSourceId);
            await MessageQueueEndpoint.RequestAsync(command, new SendOptions { ServiceContext = await GetServiceContextAsync() });
        }

        protected async Task<bool> EnsureSourceCredentialsAsync()
        {
            if (SourceLoginInformation.IsValidated)
                return true;

            var parameters = new ModalParameters();
            parameters.Add("LoginInformation", SourceLoginInformation);
            var selector = Modal.Show<RemoteCredentials>("Source System Login", parameters, new ModalOptions { Position = ModalPosition.Middle, DisableBackgroundCancel = true, HideCloseButton = true });
            _ = await selector.Result;
            return SourceLoginInformation.IsValidated;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        protected async Task FilesReadyForContent(IMatFileUploadEntry[] files)
        {
            const int batchSize = 200;
            try
            {
                var file = files.FirstOrDefault();
                if (file == null)
                {
                    return;
                }

                using var stream2 = new MemoryStream();
                await file.WriteToStreamAsync(stream2);
                stream2.Seek(0, SeekOrigin.Begin);      // at the end of the copy method, we are at the end of both the input and output stream and need to reset the one we want to work with.
                var reader = new StreamReader(stream2);

                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Read();
                if (csv.ReadHeader())
                {
                    await DeleteImportDataAsync();
                    var headerRow = csv.HeaderRecord;

                    var command = new AddProjectImport(ProjectDataSource.ProjectDataSourceId, "_", headerRow);
                    var response = await MessageQueueEndpoint.RequestAsync(command, new SendOptions { ServiceContext = await GetServiceContextAsync() });
                    if (!response.IsSuccessStatusCode)
                    {
                        ShowError(response.Message);
                        return;
                    }
                    var projectImportId = (int)response.Result;

                    var list = new List<ProjectImportData>();
                    while (await csv.Parser.ReadAsync())
                    {

                        var data = csv.Parser.Record;

                        var importData = new ProjectImportData
                        {
                            ProjectImportId = projectImportId,
                            RowNumber = csv.Parser.Row
                        };
                        importData.SetData(data);
                        list.Add(importData);
                        if (list.Count >= batchSize)
                        {
                            await SaveDataSourceData(projectImportId, list);
                            list.Clear();
                        }
                    }
                    if (list.Count > 0)
                    {
                        await SaveDataSourceData(projectImportId, list);
                    }
                }
            }
            catch (Exception e)
            {
                ShowError(e.Message);
            }
            finally
            {
                await InvokeAsync(() => StateHasChanged());
            }
        }

        protected async Task<int> GetImportDataRowCountAsync(string name = "_")
        {
            return ProjectDataSourceId == default ? 0 : await ProjectQueries.GetImportDataRowCountAsync(ProjectDataSourceId, name);
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (IsNew)
            {
                ProjectDataSource = new ProjectDataSource();
                IsEditMode = true;
            }
            else
            {
                ProjectDataSource = await DbContext.ProjectDataSources.FindAsync(ProjectDataSourceId);
                await UpdateImportCount();
            }
        }

        protected virtual async Task RemoveAsync()
        {
            if (ProjectDataSourceId != 0)
            {
                var response = await MessageQueueEndpoint.RequestAsync(new DeleteProjectDataSource(Project.ProjectId, ProjectDataSourceId), new SendOptions { ServiceContext = await GetServiceContextAsync() });
                if (response.IsSuccessStatusCode)
                {
                    NavigationManager.NavigateTo($"/project/{Project.ProjectId}", true);
                }
                else
                {
                    ShowError(response.Message);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        protected static T SafeDeserializeObject<T>(string source)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(source, GlobalSettings.JsonSerializerSettings);
            }
            catch (Exception)
            {
                return default;
            }
        }

        protected virtual async Task SaveChangesAsync()
        {
            if (IsNew)
            {
                var command = new AddProjectDataSource(Project.ProjectId, ProjectDataSource.Name, DataSourceTypeName) { Data = ProjectDataSource.Data };
                var response = await MessageQueueEndpoint.RequestAsync(command, new SendOptions { ServiceContext = await GetServiceContextAsync() });
                if (response.IsSuccessStatusCode)
                {
                    ProjectDataSourceId = (int)response.Result;
                    ProjectDataSource = await DbContext.ProjectDataSources.FindAsync(ProjectDataSourceId);
                    ShowSuccess($"DataSource: {ProjectDataSource.Name} added.");
                    await UpdateProjectUpdatedOnAsync();
                }
                else
                {
                    ShowError(response.Message);
                }
            }
            else
            {
                await DbContext.SaveChangesAsync();
                await UpdateProjectUpdatedOnAsync();
            }
            IsEditMode = ProjectDataSourceId == 0;
        }

        protected void ShowError(string message)
        {
            Toaster.Add(message, MatToastType.Danger, "Error");
        }

        protected void ShowSuccess(string message)
        {
            Toaster.Add(message, MatToastType.Success);
        }

        protected async Task UpdateImportCount()
        {
            ImportCount = await GetImportDataRowCountAsync();
        }

        protected async Task<IServiceContext> GetServiceContextAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var identity = (ClaimsIdentity)authState.User.Identity;
            identity.AddClaim(new Claim(AppClaimTypes.TenantId, Lib.MigrationUtility.GetTenant(identity)));
            return new ServiceContext((ClaimsIdentity)authState.User.Identity);
        }

        private async Task SaveDataSourceData(int projectImportId, IList<ProjectImportData> list)
        {
            // create a new throw away db context, so we don't fill up memory with our import
            var command = new AddProjectImportData(projectImportId, list);
            var response = await MessageQueueEndpoint.RequestAsync(command, new SendOptions { ServiceContext = await GetServiceContextAsync() });
            if (!response.IsSuccessStatusCode)
            {
                ShowError(response.Message);
                return;
            }
            await UpdateProjectUpdatedOnAsync();
        }

        private async Task UpdateProjectUpdatedOnAsync()
        {
            if (Project?.ProjectId != null)
            {
                var command = new UpdateProjectUpdatedOn(Project.ProjectId);
                var response = await MessageQueueEndpoint.RequestAsync(command, new SendOptions { ServiceContext = await GetServiceContextAsync() });
                if (!response.IsSuccessStatusCode)
                {
                    ShowError(response.Message);
                    return;
                }
                await UpdateImportCount();
            }
        }
    }
}
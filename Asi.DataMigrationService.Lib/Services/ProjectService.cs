using Asi.DataMigrationService.Lib.Data;
using Asi.DataMigrationService.Lib.Data.Models;
using Asi.DataMigrationService.Lib.Messages.Commands;
using Asi.DataMigrationService.Lib.Messages.Events;
using Asi.Soa.Core.DataContracts;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asi.DataMigrationService.MessageQueue.Interfaces;

namespace Asi.DataMigrationService.Lib.Services
{
    public class ProjectService : IHandleMessages<CreateProject>, IHandleMessages<AddProjectDataSource>, IHandleMessages<DeleteProject>,
        IHandleMessages<DeleteProjectDataSource>, IHandleMessages<AddProjectImport>, IHandleMessages<AddProjectImportData>, IHandleMessages<DeleteImports>,
        IHandleMessages<UpdateProjectUpdatedOn>, IHandleMessages<CopyProject>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ProjectService> _logger;
        private readonly Lazy<IPublishService> _processorService;

        public ProjectService(ApplicationDbContext dbContext, Lazy<IPublishService> processorService, ILogger<ProjectService> logger)
        {
            _dbContext = dbContext;
            _processorService = processorService;
            _logger = logger;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task HandleAsync(CreateProject message, IMessageHandlerContext context)
        {
            var tenantId = context.TenantId;
            if (tenantId is null) throw new ArgumentNullException(nameof(context.TenantId));

            var tenant = await _dbContext.Tenants.FindAsync(tenantId);
            if (tenant == null)
            {
                tenant = new Tenant() { TenantId = tenantId };
                _dbContext.Tenants.Add(tenant);
            }

            var validator = new CreateProjectValidator();
            var result = await validator.ValidateAsync(message);

            if (!result.IsValid)
            {
                await context.ResponseAsync(result.ToServiceResponse());
                return;
            }

            var now = DateTime.UtcNow;
            var project = new Project()
            {
                Name = message.Name,
                Description = message.Desciption,
                TenantId = tenantId,
                CreatedOn = now,
                UpdatedOn = now
            };
            if (message.ProjectInfo != null) project.SetProjectInfo(message.ProjectInfo);
            _dbContext.Projects.Add(project);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error creating a project.");
                await context.ResponseAsync(new ServiceResponse<string> { Exception = exception });
                return;
            }
            await context.PublishEventAsync(new ProjectCreated(project.ProjectId));
            await context.ResponseAsync(new ServiceResponse<string> { Result = project.ProjectId });
        }

        public async Task HandleAsync(AddProjectDataSource message, IMessageHandlerContext context)
        {
            var tenantId = context.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(context.TenantId));

            var validator = new AddProjectDataSourceValidator(_dbContext, _processorService.Value);
            var result = await validator.ValidateAsync(message);

            if (!result.IsValid)
            {
                await context.ResponseAsync(result.ToServiceResponse());
                return;
            }

            var project = await _dbContext.Projects.Include(p => p.DataSources).Where(p => p.ProjectId.Equals(message.ProjectId)).FirstOrDefaultAsync();
            if (project is null) throw new ArgumentNullException(nameof(message.ProjectId));

            var ds = new ProjectDataSource() { Project = project, Name = message.Name.Trim(), DataSourceType = message.DataSourceType, Data = message.Data };
            project.DataSources.Add(ds);
            if (message.ImportData != null)
            {
                var data = new ProjectImport { Name = "default" };
                ds.Imports = new List<ProjectImport>
                {
                    data
                };
                if (message.PropertyNames != null)
                {
                    data.PropertyNames = string.Join(",", message.PropertyNames.Select(p => p?.Trim()));
                }

                data.Data = new List<ProjectImportData>();
                foreach (var item in message.ImportData)
                {
                    var serializedData = JsonConvert.SerializeObject(item, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
                    data.Data.Add(new ProjectImportData() { ProjectImport = data, Data = serializedData });
                }
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                if (exception.InnerException is SqlException innerException && (innerException.Number == 2627 || innerException.Number == 2601))
                {
                    await context.ResponseAsync(new ServiceResponse { StatusCode = StatusCode.DuplicateKey });
                    return;
                }
                else
                {
                    _logger.LogError(exception, "Error adding a project data source.");
                    await context.ResponseAsync(new ServiceResponse<int> { Exception = exception });
                    return;
                }
            }

            await context.PublishEventAsync(new DataSourceAdded(ds.ProjectDataSourceId));
            await context.ResponseAsync(new ServiceResponse<int> { Result = ds.ProjectDataSourceId });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task HandleAsync(DeleteProject message, IMessageHandlerContext context)
        {
            var sourceProject = await _dbContext.FindAsync<Project>(message.ProjectId);

            var projectId = message.ProjectId;

            var sql = $"DELETE pjm FROM {nameof(ProjectJobMessage)} pjm";
            sql += $" INNER JOIN {nameof(ProjectJob)} pj ON pj.{nameof(ProjectJob.ProjectJobId)} = pjm.{nameof(ProjectJobMessage.ProjectJobId)}";
            sql += $" WHERE pj.{nameof(ProjectJob.ProjectId)} = @projectId;";
            sql += $" DELETE {nameof(ProjectJob)} WHERE {nameof(ProjectJob.ProjectId)} = @projectId;";
            sql += $" DELETE pid FROM {nameof(ProjectImportData)} pid";
            sql += $" INNER JOIN {nameof(ProjectImport)} pi ON pi.{nameof(ProjectImport.ProjectImportId)} = pid.{nameof(ProjectImportData.ProjectImportId)}";
            sql += $" INNER JOIN {nameof(ProjectDataSource)} pds ON pds.{nameof(ProjectDataSource.ProjectDataSourceId)} = pi.{nameof(ProjectImport.ProjectDataSourceId)}";
            sql += $" WHERE pds.{nameof(ProjectDataSource.ProjectId)} = @projectId;";
            sql += $" DELETE pi FROM {nameof(ProjectImport)} pi";
            sql += $" INNER JOIN {nameof(ProjectDataSource)} pds on pds.{nameof(ProjectDataSource.ProjectDataSourceId)} = pi.{nameof(ProjectImport.ProjectDataSourceId)}";
            sql += $" WHERE pds.{nameof(ProjectDataSource.ProjectId)} = @projectId;";
            sql += $" DELETE {nameof(ProjectDataSource)} WHERE {nameof(ProjectDataSource.ProjectId)} = @projectId;";
            sql += $" DELETE {nameof(Project)} WHERE {nameof(Project.ProjectId)} = @projectId;";
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(sql, new SqlParameter("@projectId", projectId));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error deleting a project.");
                await context.ResponseAsync(new ServiceResponse { Exception = exception });
                return;
            }
            await context.PublishEventAsync(new ProjectDeleted(projectId));
            await context.ResponseAsync(new ServiceResponse());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task HandleAsync(DeleteProjectDataSource message, IMessageHandlerContext context)
        {
            var projectId = message.ProjectId;
            var projectDataSourceId = message.ProjectDataSourceId;

            var sql = $"DELETE pid FROM {nameof(ProjectImportData)} pid";
            sql += $" INNER JOIN {nameof(ProjectImport)} pi ON pi.{nameof(ProjectImport.ProjectImportId)} = pid.{nameof(ProjectImportData.ProjectImportId)}";
            sql += $" INNER JOIN {nameof(ProjectDataSource)} pds ON pds.{nameof(ProjectDataSource.ProjectDataSourceId)} = pi.{nameof(ProjectImport.ProjectDataSourceId)}";
            sql += $" WHERE pds.{nameof(ProjectDataSource.ProjectId)} = @projectId AND pds.{nameof(ProjectDataSource.ProjectDataSourceId)} = @projectDataSourceId;";
            sql += $" DELETE pi FROM {nameof(ProjectImport)} pi";
            sql += $" INNER JOIN {nameof(ProjectDataSource)} pds on pds.{nameof(ProjectDataSource.ProjectDataSourceId)} = pi.{nameof(ProjectImport.ProjectDataSourceId)}";
            sql += $" WHERE pds.{nameof(ProjectDataSource.ProjectId)} = @projectId AND pds.{nameof(ProjectDataSource.ProjectDataSourceId)} = @projectDataSourceId;";
            sql += $" DELETE {nameof(ProjectDataSource)} WHERE {nameof(ProjectDataSource.ProjectId)} = @projectId AND {nameof(ProjectDataSource.ProjectDataSourceId)} = @projectDataSourceId;";
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(sql, new SqlParameter("@projectId", projectId), new SqlParameter("@projectDataSourceId", projectDataSourceId));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error deleting a project data source.");
                await context.ResponseAsync(new ServiceResponse { Exception = exception });
                return;
            }
            await context.PublishEventAsync(new ProjectDataSourceDeleted(projectId, projectDataSourceId));
            await context.ResponseAsync(new ServiceResponse());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task HandleAsync(AddProjectImport message, IMessageHandlerContext context)
        {
            var tenantId = context.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(context.TenantId));

            var validator = new AddProjectImportValidator(_dbContext);
            var result = await validator.ValidateAsync(message);

            if (!result.IsValid)
            {
                await context.ResponseAsync(result.ToServiceResponse());
                return;
            }

            var projectImport = new ProjectImport()
            {
                ProjectDataSourceId = message.ProjectDataSourceId,
                Name = message.Name
            };
            if (message.PropertyNames != null)
            {
                projectImport.SetPropertyNames(message.PropertyNames);
            }

            _dbContext.ProjectImports.Add(projectImport);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error creating a project import.");
                await context.ResponseAsync(new ServiceResponse<int> { Exception = exception });
                return;
            }
            await context.ResponseAsync(new ServiceResponse<int> { Result = projectImport.ProjectImportId });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task HandleAsync(AddProjectImportData message, IMessageHandlerContext context)
        {
            var validator = new AddProjectImportDataValidator(_dbContext);
            var result = await validator.ValidateAsync(message);

            if (!result.IsValid)
            {
                await context.ResponseAsync(result.ToServiceResponse());
                return;
            }

            var projectImport = await _dbContext.ProjectImports.FindAsync(message.ProjectImportId);
            if (projectImport is null)
            {
                var response = new ServiceResponse();
                response.ValidationResults.AddError("Could not find ProjectImportId");
                await context.ResponseAsync(response);
                return;
            }

            if (message.ProjectImportData != null)
            {
                if (projectImport.Data is null)
                    projectImport.Data = new List<ProjectImportData>();
                foreach (var item in message.ProjectImportData)
                {
                    item.ProjectImportId = message.ProjectImportId;
                    projectImport.Data.Add(item);
                }
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error creating project import data.");
                await context.ResponseAsync(new ServiceResponse { Exception = exception });
                return;
            }
            await context.ResponseAsync(new ServiceResponse { Result = projectImport.ProjectImportId });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task HandleAsync(DeleteImports message, IMessageHandlerContext context)
        {
            var sql = $"DELETE d FROM {nameof(ProjectImportData)} d INNER JOIN {nameof(ProjectImport)} i ON i.{nameof(ProjectImport.ProjectImportId)} = d.{nameof(ProjectImportData.ProjectImportId)} WHERE i.{nameof(ProjectImport.ProjectDataSourceId)} = @projectDataSourceId;";
            sql += $"DELETE {nameof(ProjectImport)} WHERE {nameof(ProjectImport.ProjectDataSourceId)} = @projectDataSourceId;";
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(sql, new SqlParameter("@projectDataSourceId", message.ProjectDataSourceId));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error deleteing project import data.");
                await context.ResponseAsync(new ServiceResponse { Exception = exception });
                return;
            }
            await context.ResponseAsync(new ServiceResponse());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task HandleAsync(UpdateProjectUpdatedOn message, IMessageHandlerContext context)
        {
            var project = await _dbContext.Projects.FindAsync(message.ProjectId);
            if (project is null)
                return;
            project.UpdatedOn = DateTime.UtcNow;
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error updating project.");
                await context.ResponseAsync(new ServiceResponse { Exception = exception });
                return;
            }
            await context.ResponseAsync(new ServiceResponse());
        }

        public async Task HandleAsync(CopyProject message, IMessageHandlerContext context)
        {
            var tenantId = context.TenantId;
            if (tenantId is null) throw new ArgumentNullException(nameof(context.TenantId));
            var validator = new CopyProjectValidator(_dbContext, tenantId);
            var result = await validator.ValidateAsync(message);

            if (!result.IsValid)
            {
                await context.ResponseAsync(result.ToServiceResponse());
                return;
            }

        //    var sourceProject = _dbContext.Projects.

        //    var projectId = message.ProjectId;

        //    var sql = $"DELETE pjm FROM {nameof(ProjectJobMessage)} pjm";
        //    sql += $" INNER JOIN {nameof(ProjectJob)} pj ON pj.{nameof(ProjectJob.ProjectJobId)} = pjm.{nameof(ProjectJobMessage.ProjectJobId)}";
        //    sql += $" WHERE pj.{nameof(ProjectJob.ProjectId)} = @projectId;";
        //    sql += $" DELETE {nameof(ProjectJob)} WHERE {nameof(ProjectJob.ProjectId)} = @projectId;";
        //    sql += $" DELETE pid FROM {nameof(ProjectImportData)} pid";
        //    sql += $" INNER JOIN {nameof(ProjectImport)} pi ON pi.{nameof(ProjectImport.ProjectImportId)} = pid.{nameof(ProjectImportData.ProjectImportId)}";
        //    sql += $" INNER JOIN {nameof(ProjectDataSource)} pds ON pds.{nameof(ProjectDataSource.ProjectDataSourceId)} = pi.{nameof(ProjectImport.ProjectDataSourceId)}";
        //    sql += $" WHERE pds.{nameof(ProjectDataSource.ProjectId)} = @projectId;";
        //    sql += $" DELETE pi FROM {nameof(ProjectImport)} pi";
        //    sql += $" INNER JOIN {nameof(ProjectDataSource)} pds on pds.{nameof(ProjectDataSource.ProjectDataSourceId)} = pi.{nameof(ProjectImport.ProjectDataSourceId)}";
        //    sql += $" WHERE pds.{nameof(ProjectDataSource.ProjectId)} = @projectId;";
        //    sql += $" DELETE {nameof(ProjectDataSource)} WHERE {nameof(ProjectDataSource.ProjectId)} = @projectId;";
        //    sql += $" DELETE {nameof(Project)} WHERE {nameof(Project.ProjectId)} = @projectId;";
        //    try
        //    {
        //        await _dbContext.Database.ExecuteSqlRawAsync(sql, new SqlParameter("@projectId", projectId));
        //    }
        //    catch (Exception exception)
        //    {
        //        _logger.LogError(exception, "Error deleting a project.");
        //        await context.ResponseAsync(new ServiceResponse { Exception = exception });
        //        return;
        //    }
        //    await context.PublishEventAsync(new ProjectCreated(projectId));
        //    await context.ResponseAsync(new ServiceResponse());
        }

        private class AddProjectDataSourceValidator : AbstractValidator<AddProjectDataSource>
        {
            private readonly ApplicationDbContext _dbContext;
            private readonly IPublishService _processorService;

            public AddProjectDataSourceValidator(ApplicationDbContext dbContext, IPublishService processorService)
            {
                _dbContext = dbContext;
                _processorService = processorService;
                RuleFor(target => target.ProjectId).NotEmpty()
                    .MustAsync(async (id, _) => await _dbContext.Projects.ExistsAsync(id))
                    .WithMessage("ProjectId does not exist.");
                RuleFor(target => target.Name).NotEmpty().MaximumLength(100);
                RuleFor(target => target.DataSourceType).NotEmpty()
                    .MustAsync((id, _) => Task.FromResult(_processorService.Create(id) != null))
                    .WithMessage("Resource type is not defined.");
            }
        }

        private class AddProjectImportDataValidator : AbstractValidator<AddProjectImportData>
        {
            private readonly ApplicationDbContext _dbContext;

            public AddProjectImportDataValidator(ApplicationDbContext dbContext)
            {
                _dbContext = dbContext;
                RuleFor(target => target.ProjectImportId).GreaterThan(0)
                    .MustAsync(async (id, _) => await _dbContext.ProjectImports.ExistsAsync(id))
                    .WithMessage("ProjectImportId does not exist.");
            }
        }

        private class AddProjectImportValidator : AbstractValidator<AddProjectImport>
        {
            private readonly ApplicationDbContext _dbContext;

            public AddProjectImportValidator(ApplicationDbContext dbContext)
            {
                _dbContext = dbContext;
                RuleFor(target => target.ProjectDataSourceId).GreaterThan(0)
                    .MustAsync(async (id, _) => await _dbContext.ProjectDataSources.ExistsAsync(id))
                    .WithMessage("ProjectDataSourceId does not exist.");
                RuleFor(target => target.Name).NotEmpty()
                    .MustAsync(async (c, name, _) => !await _dbContext.ProjectDataSources.AnyAsync(p => p.ProjectDataSourceId == c.ProjectDataSourceId && p.Name == name))
                    .WithMessage("ProjectImport name is not unique.");
            }
        }

        private class CreateProjectValidator : AbstractValidator<CreateProject>
        {
            public CreateProjectValidator()
            {
                RuleFor(target => target.Name).NotEmpty().MaximumLength(100);
            }
        }

        private class CopyProjectValidator : AbstractValidator<CopyProject>
        {
            public CopyProjectValidator(ApplicationDbContext dbContext, string tenantId)
            {
                RuleFor(target => target.SourceProjectId).NotEmpty()
                    .MustAsync(async (id, _) => await dbContext.Projects.ExistsAsync(id))
                    .WithMessage("ProjectId does not exist.");
                RuleFor(target => target.Name).NotEmpty().MaximumLength(100)
                    .MustAsync(async (name, _) => !await dbContext.Projects.AnyAsync(p => p.TenantId == tenantId && p.Name == name))
                    .WithMessage("Project name must be unique");
            }
        }
    }
}
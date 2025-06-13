using Asi.DataMigrationService.Lib.Data;
using Asi.DataMigrationService.Lib.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.Lib.Queries
{
    public interface IProjectQueries
    {
        Task<int> GetImportDataRowCountAsync(int projectDataSourceId, string Name);

        Task<Project> GetProjectAsync(string projectId);

        Task<ProjectDataSource> GetProjectDataSourceAsync(int projectDataSourceId);

        Task<IList<ProjectDataSource>> GetProjectDataSourcesAsync(string projectId);

        Task<ProjectJob> GetProjectJobAsync(int jobId);

        Task<List<ProjectJobMessage>> GetProjectJobMessagesAsync(int jobId);

        Task<List<ProjectJob>> GetProjectJobsAsync(string tenantId);

        Task<IList<Project>> GetProjectsAsync(string tenantId);
    }

    public class ProjectQueries : IProjectQueries
    {
        private readonly IServiceProvider _serviceProvider;

        public ProjectQueries(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<int> GetImportDataRowCountAsync(int projectDataSourceId, string name)
        {
            {
                using var scope = _serviceProvider.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return await dbContext.ProjectImportDatas.AsNoTracking().Where(p => p.ProjectImport.ProjectDataSourceId == projectDataSourceId && p.ProjectImport.Name == name).CountAsync();
            }
        }

        public async Task<Project> GetProjectAsync(string projectId)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Projects.AsNoTracking().Include(p => p.DataSources).FirstOrDefaultAsync(p => p.ProjectId == projectId);
        }

        public async Task<ProjectDataSource> GetProjectDataSourceAsync(int projectDataSourceId)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.ProjectDataSources.AsNoTracking().Include(p => p.Imports).FirstOrDefaultAsync(p => p.ProjectDataSourceId == projectDataSourceId);
        }

        public async Task<IList<ProjectDataSource>> GetProjectDataSourcesAsync(string projectId)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.ProjectDataSources.AsNoTracking().Include(p => p.Imports).Where(p => p.ProjectId == projectId).ToListAsync();
        }

        public async Task<ProjectJob> GetProjectJobAsync(int jobId)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.ProjectJobs.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectJobId == jobId);
        }

        public async Task<List<ProjectJobMessage>> GetProjectJobMessagesAsync(int jobId)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.ProjectJobMessages.AsNoTracking().Where(p => p.ProjectJob.ProjectJobId == jobId).ToListAsync();
        }

        public async Task<List<ProjectJob>> GetProjectJobsAsync(string tenantId)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.ProjectJobs.AsNoTracking().Include(p => p.Project).Where(p => p.Project.TenantId == tenantId).ToListAsync();
        }

        public async Task<IList<Project>> GetProjectsAsync(string tenantId)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Projects.AsNoTracking().Include(p => p.DataSources).Where(p => p.TenantId == tenantId).ToListAsync();
        }
    }
}
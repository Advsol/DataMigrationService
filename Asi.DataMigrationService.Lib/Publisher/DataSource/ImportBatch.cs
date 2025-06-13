using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asi.DataMigrationService.Lib.Data;
using Asi.DataMigrationService.Lib.Data.Models;
using Asi.Soa.Core.DataContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Asi.DataMigrationService.Lib.Publisher.DataSource
{
    public class ImportBatch
    {
        private int? _totalCount;

        public ImportBatch(DataSourceImportInfo importInfo, IServiceProvider serviceProvider)
        {
            DataSourceImportInfo = importInfo;
            ServiceProvider = serviceProvider;
        }

        public DataSourceImportInfo DataSourceImportInfo { get; }
        public DataSourceInfo DataSourceInfo { get; }
        public int ProcessingBatchSize => 100;

        public IServiceProvider ServiceProvider { get; }
        public int SourcePageLimit => 500;
        public string TypeName => DataSourceInfo.TypeName;

        public async Task<IServiceResponsePagedResult<ImportRow>> GetPagedResultAsync(int offset, int limit)
        {
            try
            {
                var totalRows = await GetTotalCountAsync();
                using var scope = ServiceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var query = dbContext.ProjectImportDatas.AsNoTracking().Include(p => p.ProjectImport).Where(p => p.ProjectImport.ProjectImportId == DataSourceImportInfo.ProjectImportId).OrderBy(p => p.ProjectImportDataId);
                if (offset > 0)
                    query = (IOrderedQueryable<ProjectImportData>)query.Skip(offset);
                if (limit > 0)
                    query = (IOrderedQueryable<ProjectImportData>)query.Take(limit);
                var sr = new ServiceResponsePagedResult<ImportRow>();
                var queryRows = query.ToList();
                var resultList = new List<ImportRow>();
                foreach (var item in queryRows)
                {
                    var dsrr = new ImportRowReference
                    {
                        Import = DataSourceImportInfo,
                        ProjectImportDataId = item.ProjectImportDataId,
                        RowNumber = item.RowNumber
                    };
                    resultList.Add(new ImportRow(dsrr, JsonConvert.DeserializeObject<IList<object>>(item.Data, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto })));
                }
                var pagedResult = new PagedResult<ImportRow>(resultList, offset, limit, totalRows);
                return new ServiceResponsePagedResult<ImportRow> { Result = pagedResult };
            }
            catch (Exception exception)
            {
                return new ServiceResponsePagedResult<ImportRow> { Exception = exception};
            }
        }

        private async Task<int> GetTotalCountAsync()
        {
            if (!_totalCount.HasValue)
            {
                using var scope = ServiceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                _totalCount = await dbContext.ProjectImportDatas.AsNoTracking()
                    .Where(p => p.ProjectImport.ProjectImportId == DataSourceImportInfo.ProjectImportId)
                    .CountAsync();
            }
            return _totalCount.Value;
        }
    }
}
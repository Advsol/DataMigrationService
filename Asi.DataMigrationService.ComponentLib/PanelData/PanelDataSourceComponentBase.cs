using Asi.DataMigrationService.Lib.Data.Models;
using Asi.DataMigrationService.Lib.Messages.Commands;
using Asi.Soa.Core.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.MessageQueue.Interfaces;

namespace Asi.DataMigrationService.ComponentLib.PanelData
{
    public class PanelDataSourceComponentBase : DataSourceComponentBase
    {
        protected async Task SaveAllPanelDataAsync(IList<PanelDataSourceDataInfo> panelDatas)
        {
            // remove any data not in select list; replace all data selected
            // in fact, since we are reloading all, might as well clear all
            await DeleteImportDataAsync();

            foreach (var panelData in panelDatas.Where(p => p.IncludeData))
            {
                await SavePanelDataAsync(panelData);
            }
        }

        private async Task SavePanelDataAsync(PanelDataSourceDataInfo panelData)
        {
            var command = new AddProjectImport(ProjectDataSourceId, panelData.Name, panelData.BOEntityDefinition.Properties.Select(p => p.Name));
            var result = await MessageQueueEndpoint.RequestAsync(command, new SendOptions { ServiceContext = await GetServiceContextAsync() });
            if (!result.IsSuccessStatusCode)
            {
                ShowError(result.Message);
                return;
            }
            var service = ClientFactory.Create(panelData.Name, SourceLoginInformation.Uri, SourceLoginInformation.UserCredentials);
            const int offset = 0;
            const int limit = 200;
            var rowNumber = 1;
            var query = new Query<GenericEntityData>().Offset(offset).Limit(limit);
            var response = await service.FindAsync(query);
            while (response.IsSuccessStatusCode)
            {
                var pagedResult = response.Result;
                var data = new List<ProjectImportData>();
                foreach (var item in pagedResult.OfType<GenericEntityData>())
                {
                    var values = new List<object>();
                    foreach (var property in panelData.BOEntityDefinition.Properties)
                    {
                        var value = item.Properties.GetPropertyValue(property.Name);
                        if (value is DateTime date && date == DateTime.MinValue) value = null;
                        values.Add(value);
                    }
                    var importData = new ProjectImportData { RowNumber = ++rowNumber };
                    importData.SetData(values);
                    data.Add(importData);
                }
                var dataCommand = new AddProjectImportData((int)result.Result, data);
                var result2 = await MessageQueueEndpoint.RequestAsync(dataCommand, new SendOptions { ServiceContext = await GetServiceContextAsync() });
                if (!result2.IsSuccessStatusCode)
                {
                    ShowError(result2.Message);
                    return;
                }

                if (!pagedResult.HasNext) break;
                query.Offset = pagedResult.NextOffset;
                response = await service.FindAsync(query);
            }
        }
    }
}
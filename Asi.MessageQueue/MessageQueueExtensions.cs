using Asi.Core.Interfaces.Messaging;
using Asi.Soa.Core.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using IMessageQueueEndpoint = Asi.DataMigrationService.MessageQueue.Interfaces.IMessageQueueEndpoint;
using IMessageQueueProvider = Asi.DataMigrationService.MessageQueue.Interfaces.IMessageQueueProvider;
using IMessageRouting = Asi.DataMigrationService.MessageQueue.Interfaces.IMessageRouting;
using IQueueHandler = Asi.DataMigrationService.MessageQueue.Interfaces.IQueueHandler;
using QueueDefinition = Asi.DataMigrationService.MessageQueue.Interfaces.QueueDefinition;

namespace Asi.DataMigrationService.MessageQueue
{
    public static class MessageQueueExtensions
    {
        public static IServiceCollection AddMessageQueue(this IServiceCollection services)
        {
            services.AddSingleton<IMessageRouting, MessageRouting>();
            services.AddSingleton<MessageRoutingRules>();
            services.AddTransient((provider) =>
            {
                return new Func<QueueDefinition, IQueueHandler>(
                    (queueDefinition) => new QueueHandler(queueDefinition, provider.GetRequiredService<IMessageQueueProvider>(), provider.GetRequiredService<ILogger<QueueHandler>>(),
                    provider)
                    );
            });
            services.AddSingleton<IMessageQueueEndpoint>(c =>
            {
                const string endpointName = "CommandMain";
                var instanceQueue = new QueueDefinition($"{endpointName}_Instance_{ShortGuid.NewGuid()}", false) { MaxDegreeOfParallelism = 10 };
                var endpointQueues = new[] {
                    new QueueDefinition($"{endpointName}", true){MaxDegreeOfParallelism = 10 },                         // Main command endpoint
                    new QueueDefinition($"{endpointName}_Priority", true){MaxDegreeOfParallelism = 10 },                // Priority main command endpoint
                    new QueueDefinition($"{endpointName}_Event", true, new[] { "Event" }){MaxDegreeOfParallelism = 10 } // Endpoint event queue 
                };
                var ep = new MessageQueueEndpoint(instanceQueue, endpointQueues, c.GetRequiredService<Func<QueueDefinition, IQueueHandler>>(), c.GetRequiredService<IMessageRouting>(), c.GetRequiredService<ILogger<MessageQueueEndpoint>>());
                ep.Start();
                return ep;
            });
            services.AddSingleton(c => (Interfaces.IHandleMessages<Reply>)c.GetRequiredService<IMessageQueueEndpoint>());
            services.AddSingleton<IMessageQueueProvider, LocalMessageQueueProvider>();
            return services;
        }
    }
}
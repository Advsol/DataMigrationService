using Asi.Core.Interfaces.Messaging;
using Asi.Soa.Core.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.FormattableString;
using IHandleMessages = Asi.DataMigrationService.MessageQueue.Interfaces.IHandleMessages;
using IMessageHandlerContext = Asi.DataMigrationService.MessageQueue.Interfaces.IMessageHandlerContext;

namespace Asi.DataMigrationService.MessageQueue
{
    /// <summary>   A command runner. </summary>
    public abstract class MessageDispatcher
    {
        #region Public Instance Methods

        /// <summary>   Runs the given handler. </summary>
        ///
        /// <param name="handler">  The handler. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        public abstract Task RunAsync(object handler);

        #endregion Public Instance Methods

        /// <summary>   Executes the command asynchronous operation. </summary>
        ///
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        ///
        /// <param name="lifetimeScope">    The lifetime scope. </param>
        /// <param name="message">          The command. </param>
        /// <param name="context">          The context. </param>
        ///
        /// <returns>   An asynchronous result that yields the run command. </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public static async Task<bool> DispatchMessageAsync(IServiceProvider serviceProvider, IMessage message, IMessageHandlerContext context)
        {
            using var scope = serviceProvider.CreateScope();
            try
            {
                var handlers = new List<IHandleMessages>();
                // event supports multiple subscribers
                if (message is IEvent)
                {
                    var type = typeof(Interfaces.IHandleMessages<>).MakeGenericType(message.GetType());
                    handlers = ((IEnumerable<IHandleMessages>)scope.ServiceProvider.GetServices(type)).ToList();
                }
                else
                {
                    var type = typeof(Interfaces.IHandleMessages<>).MakeGenericType(message.GetType());
                    var handler = scope.ServiceProvider.GetService(type);
                    if (handler != null)
                    {
                        handlers.Add((IHandleMessages)handler);
                    }
                    else
                    {
                        throw new Exception(Invariant($"Not a unique handler defined for -  for message {message.GetType().Name}"));
                    }
                }

                foreach (var handler in handlers)
                {
                    var messageDispatcheType = typeof(MessageDispatcher<>).MakeGenericType(message.GetType());
                    var messageDispatcher = (MessageDispatcher)Activator.CreateInstance(messageDispatcheType, message, context);
                    await messageDispatcher.RunAsync(handler);
                }
                return true;
            }
            catch (Exception e)
            {
                //TODO: log error
                if (context.IsRequestReply)
                    await context.ResponseAsync(new ServiceResponse(StatusCode.ServiceError) { Exception = e });
                return false;
            }
        }
    }

    /// <summary>   A command runner. </summary>
    ///
    /// <typeparam name="T">    Generic type parameter. </typeparam>
    public class MessageDispatcher<T> : MessageDispatcher where T : IMessage
    {
        /// <summary>   Gets the context. </summary>
        ///
        /// <value> The context. </value>
        public IMessageHandlerContext Context { get; }

        #region Constructors

        /// <summary>   Constructor. </summary>
        ///
        /// <param name="message">  The command. </param>
        /// <param name="context">  The context. </param>
        public MessageDispatcher(T message, IMessageHandlerContext context)
        {
            Context = context;
            Message = message;
        }

        #endregion Constructors

        #region Properties

        private T Message { get; }

        #endregion Properties

        #region Public Instance Methods

        /// <summary>   Gets the run. </summary>
        ///
        /// <param name="handler">  The handler. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task RunAsync(Interfaces.IHandleMessages<T> handler)
        {
            try

            {
                await handler.HandleAsync(Message, Context);
            }
            catch (Exception e)
            {
                // TODO: log error
                if (Context.IsRequestReply)
                    await Context.ResponseAsync(new ServiceResponse(StatusCode.ServiceError) { Exception = e });
            }
        }

        /// <summary>   Runs the given handler. </summary>
        ///
        /// <param name="handler">  The handler. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        public override Task RunAsync(object handler)
        {
            return RunAsync((Interfaces.IHandleMessages<T>)handler);
        }

        #endregion Public Instance Methods
    }
}
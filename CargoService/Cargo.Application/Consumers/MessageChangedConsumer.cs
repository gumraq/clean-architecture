using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MassTransit;
using Cargo.Infrastructure.Data;
using Cargo.Infrastructure.Data.Model;
using IDeal.Common.Messaging.Messages;
using AutoMapper;

namespace Cargo.Application.CommandHandlers
{
    public class MessageChangedConsumer : IConsumer<MessageChanged>
    {
        ILogger<MessageChangedConsumer> logger;
        CargoContext dbContext;
        IMapper mapper;

        public MessageChangedConsumer(ILogger<MessageChangedConsumer> logger, CargoContext dbContext, IMapper mapper)
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        public async Task Consume(ConsumeContext<MessageChanged> context)
        {
            if (context.CorrelationId == null)
            {
                throw new ArgumentNullException("Пропущен параметр для определения сообщения");
            }
            Message message = this.dbContext.Messages.Find(context.CorrelationId);

            bool isNew = message == null;

            message = this.mapper.Map(context.Message, message);
            message.Id = context.CorrelationId.Value;

            if (isNew)
            {
                this.dbContext.Messages.Add(message);
            }

            await this.dbContext.SaveChangesAsync();
        }
    }
}

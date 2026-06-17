using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OrderOrchestration.Domain;
using OrderOrchestration.Domain.Data;

namespace OrderOrchestration.Infrastructure.Mongo
{
    /// <summary>
    /// Implementacion de IOrderRepository utilizando MongoDB como almacenamiento de datos.
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly IMongoCollection<Order> _ordersCollection;

        public OrderRepository(IOptions<MongoDbSettings> mongoDbSettings, IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase(mongoDbSettings.Value.DatabaseName);

            _ordersCollection = database.GetCollection<Order>("Orders");

            EnsureIndexes();
        }

        /// <summary>
        /// Garantiza un índice único sobre <c>PaymentToken</c>. Actúa como red de seguridad
        /// de idempotencia: si dos peticiones concurrentes con el mismo token intentaran crear
        /// órdenes distintas, MongoDB rechazará la segunda inserción.
        /// </summary>
        private void EnsureIndexes()
        {
            var indexKeys = Builders<Order>.IndexKeys.Ascending(o => o.PaymentToken);
            var indexModel = new CreateIndexModel<Order>(
                indexKeys,
                new CreateIndexOptions { Unique = true, Name = "ux_orders_payment_token" });

            _ordersCollection.Indexes.CreateOne(indexModel);
        }

        /// <inheritdoc />
        public async Task<Order?> GetByIdAsync(string orderId)
        {
            if (!Guid.TryParse(orderId, out var idGuid)) return null;
            return await _ordersCollection.Find(o => o.OrderId == idGuid).FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<Order?> GetByPaymentTokenAsync(string paymentToken)
        {
            return await _ordersCollection.Find(o => o.PaymentToken == paymentToken).FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<List<Order>> GetOrdersWithUnprocessedEventsAsync()
        {
            var filter = Builders<Order>.Filter.ElemMatch(o => o.OutboxMessages, m => m.Processed == false);

            return await _ordersCollection.Find(filter).ToListAsync();
        }

        /// <inheritdoc />        
        public async Task SaveAsync(Order order)
        {
            var filter = Builders<Order>.Filter.Eq(o => o.OrderId, order.OrderId);

            var options = new ReplaceOptions { IsUpsert = true };

            await _ordersCollection.ReplaceOneAsync(filter, order, options);
        }

        /// <inheritdoc />
        public async Task<List<Order>> GetByStatusAsync(OrderStatus status)
        {
            return await _ordersCollection.Find(o => o.Status == status).ToListAsync();
        }

        /// <inheritdoc />
        public async Task MarkOutboxMessageProcessedAsync(Guid orderId, Guid messageId)
        {
            var filter = Builders<Order>.Filter.And(
                Builders<Order>.Filter.Eq(o => o.OrderId, orderId),
                Builders<Order>.Filter.ElemMatch(o => o.OutboxMessages, m => m.Id == messageId));

            var update = Builders<Order>.Update
                .Set("OutboxMessages.$.Processed", true)
                .Set("OutboxMessages.$.ProcessedOn", DateTime.UtcNow);

            await _ordersCollection.UpdateOneAsync(filter, update);
        }
    }
}

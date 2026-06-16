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
    }
}

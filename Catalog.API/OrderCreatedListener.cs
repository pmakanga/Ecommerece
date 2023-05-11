using Catalog.API.Db;
using Catalog.API.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Plain.RabbitMQ;
using Shared.Models;

namespace Catalog.API
{
    public class OrderCreatedListener : IHostedService
    {
        private readonly ISubscriber subscriber;
        private readonly IPublisher publisher;
        private readonly IServiceScopeFactory scopeFactory;

        public OrderCreatedListener(ISubscriber subscriber, IPublisher publisher, IServiceScopeFactory scopeFactory)
        {
            this.subscriber = subscriber;
            this.publisher = publisher;
            this.scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            subscriber.Subscribe(Subscribe);
            return Task.CompletedTask;
        }

        private bool Subscribe(string message, IDictionary<string, object> header) 
        {
            var response = JsonConvert.DeserializeObject<OrderRequest>(message);

            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
                try
                {
                    CatalogItem catalogItem = context.CatalogItems.Find(response.CatalogId);
                    if (catalogItem == null || catalogItem.AvailableStock < response.Units)
                        throw new Exception();
                    
                    catalogItem.AvailableStock = catalogItem.AvailableStock - response.Units;
                    context.Entry(catalogItem).State = EntityState.Modified;
                    context.SaveChanges();

                    publisher.Publish(JsonConvert.SerializeObject(
                        new CatalogResponse { OrderId = response.OrderId, CatalogId = response.CatalogId, IsSuccess = true}
                        ), "catalog_response_routingkey", null);
                }
                catch (Exception)
                {
                    publisher.Publish(JsonConvert.SerializeObject(
                        new CatalogResponse { OrderId = response.OrderId, CatalogId = response.CatalogId, IsSuccess = false }
                        ), "catalog_response_routingkey", null);
                }
            }

            return true;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

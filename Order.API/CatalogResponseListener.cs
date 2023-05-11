using Newtonsoft.Json;
using Order.API.Db;
using Plain.RabbitMQ;
using Shared.Models;
using System.Text.Json.Serialization;

namespace Order.API
{
    public class CatalogResponseListener : IHostedService
    {
        private ISubscriber subscriber;
        private readonly IServiceScopeFactory scopeFactory;

        public CatalogResponseListener(ISubscriber subscriber, IServiceScopeFactory scopeFactory)
        {
            this.subscriber = subscriber;
            this.scopeFactory = scopeFactory;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            subscriber.Subscribe(Subscribe);
            return Task.CompletedTask;
        }


        private bool Subscribe(string message, IDictionary<string, object> header) 
        {
            var response = JsonConvert.DeserializeObject<CatalogResponse>(message);

            if(!response.IsSuccess)
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var orderingContext = scope.ServiceProvider.GetRequiredService<OrderingContext>();

                    // If transaction is not successful, Remove ordering item
                    var orderItem = orderingContext.OrderItems.Where(o => o.ProductId == response.CatalogId && o.OrderId == response.OrderId).FirstOrDefault();
                    orderingContext.OrderItems.Remove(orderItem);
                    orderingContext.SaveChanges();
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

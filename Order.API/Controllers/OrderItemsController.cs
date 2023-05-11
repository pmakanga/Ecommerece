using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Order.API.Db;
using Order.API.Models;
using Plain.RabbitMQ;
using Shared.Models;

namespace Order.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderItemsController : ControllerBase
    {
        private readonly OrderingContext context;
        private readonly IPublisher publisher;

        public OrderItemsController(OrderingContext context, IPublisher publisher)
        {
            this.context = context;
            this.publisher = publisher;
        }

        // GET: api/OrderItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderItem>>>GetOrderItems()
        {
            return await context.OrderItems.ToListAsync();
        }

        //GET: api/OrderItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderItem>>GetOrderItem(int id)
        {
            var orderItem = await context.OrderItems.FindAsync(id);

            if (orderItem == null) 
            {
                return NotFound();
            }

            return orderItem;
        }

        // POST: api/OrderItems
        [HttpPost]
        public async Task PostOrderItem(OrderItem orderItem)
        {
            context.OrderItems.Add(orderItem);
            await context.SaveChangesAsync();

            // New inserted identity value
            int id = orderItem.Id;

            publisher.Publish(JsonConvert.SerializeObject(new OrderRequest
            {
                OrderId = orderItem.OrderId,
                CatalogId = orderItem.ProductId,
                Units = orderItem.Units,
                Name = orderItem.ProductName
            }),
            "order_created_routingkey", //  Routing key
            null);
        }
    }
}

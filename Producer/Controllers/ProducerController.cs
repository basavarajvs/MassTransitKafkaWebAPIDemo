
using MassTransit;
using Messages;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Producer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProducerController : ControllerBase
    {
        private readonly ITopicProducer<Message> _producer;

        public ProducerController(ITopicProducer<Message> producer)
        {
            _producer = producer;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] string text)
        {
            var message = new Message { Id = Guid.NewGuid(), Text = text };
            await _producer.Produce(message);
            return Ok();
        }
    }
}

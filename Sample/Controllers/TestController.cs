using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Sample.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        public TestController(ILogger<TestController> logger)
        {
            Logger = logger;
            logger.LogDebug("test");
        }

        public ILogger<TestController> Logger { get; }

        [HttpGet("foo")]
        public async Task<IActionResult> Foo()
        {
            using var scope = Logger.BeginScope(new KeyValuePair<string, object>("id", Guid.NewGuid()));
            Logger.LogInformation("Foo called");
            await BeginFoo();

            return Ok("ok!");
        }

        [HttpGet("bar")]
        public IActionResult Bar()
        {
            throw new NotSupportedException("asdf", new Exception("fdsa"));
        }

        public async Task BeginFoo()
        {
            using var scope = Logger.BeginScope(new KeyValuePair<string, object>("method", nameof(BeginFoo)));
            Logger.LogInformation("starting");
            await Task.Delay(2000);
            Logger.LogInformation("completed");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AlterNats;
using System.Security.Cryptography;

namespace NatsSample.Controllers
{
    public class HomeController : Controller
    {
        static ValueTask<IDisposable>? _subscribeTask;
        static int _counter = 1;

        SemaphoreSlim Semaphore { get; set; } = new SemaphoreSlim(1, 1);
        NatsKey NatsKey { get; set; } = new NatsKey("test.subject");
        ILogger _logger;
        INatsCommand _command;

        public HomeController(INatsCommand command, ILogger<HomeController> logger)
        {
            _logger = logger;
            _command = command;
        }

        public IActionResult Index()
        {
            return Content("OK");
        }

        [Route("/subscribe")]
        public async Task<IActionResult> Subscribe()
        {
            Semaphore.Wait();
            try
            {
                if (_subscribeTask is null)
                {
                    _subscribeTask = _command.SubscribeAsync(NatsKey, (string message) => OnReceive(message));
                    _logger.LogDebug("Subscribed");
                }

            }
            finally
            {
                Semaphore.Release();
            }
            await Task.Delay(1);

            return Content("subscribe");
        }

        [Route("/publish")]
        public async Task<IActionResult> Publish([FromQuery] string message = "message")
        {
            Semaphore.Wait();
            try
            {
                await Task.Delay(1);
                var _ = _command.PublishAsync(NatsKey, $"counter:{_counter}, date:{DateTime.Now.ToString("HH:mm:ss")}, {message}");
                _counter++;
            }
            finally
            {
                Semaphore.Release();
            }
            return Content($"publish {message}");
        }

        [Route("/unsubscribe")]
        public async Task<IActionResult> Unsubscribe()
        {
            await Task.Delay(1);
            if (_subscribeTask?.IsCompleted ?? false)
            {
                var task = _subscribeTask?.AsTask();
                if (task is not null)
                {
                    try
                    {
                        var subscribe = await task;
                        subscribe.Dispose();
                    }
                    catch (NatsException e)
                    {
                        _logger.LogError(e, "Failed");
                    }
                    finally
                    {
                        _subscribeTask = null;
                    }
                    _logger.LogDebug("Unsubscribe");
                }
            }
            return Content("unsubscribe");
        }

        private void OnReceive(string message)
        {
            _logger.LogInformation($"Received {message}");
        }
    }
}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mongodb_locks;
using Microsoft.AspNetCore.Mvc;

namespace mongodb_locks.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ResourceController : ControllerBase
  {
    public ILockProvider LockProvider { get; }

    public ResourceController(ILockProvider lockProvider)
    {
      LockProvider = lockProvider;
    }

    public class ResultModel
    {
      public string ResourceId { get; set; }
      public long WorkMs { get; set; }
      public long WaitMs { get; set; }
      public long TotalMs { get; set; }

      public string LockFailedMessage { get; set; }
    }

    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("{id}")]
    public async Task<ResultModel> DoWork(string id)
    {
      var stopwatch = Stopwatch.StartNew();

      //timer.Start();
      var result = new ResultModel { ResourceId = id };

      try
      {
        using (await LockProvider.AcquireLock(id))
        {
          result.WaitMs = stopwatch.ElapsedMilliseconds;

          stopwatch.Restart();
          
          // Do some work (simulated here with a delay)
          await Task.Delay(5000);

          result.WorkMs = stopwatch.ElapsedMilliseconds;
        }
      }
      catch (Exception ex)
      {
        result.WaitMs = stopwatch.ElapsedMilliseconds;
        // Failed
        result.LockFailedMessage = ex.Message;
      }

      result.TotalMs = stopwatch.ElapsedMilliseconds + result.WaitMs;

      //timer.Stop();

      return result;
    }
  }
}


using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace mongodb_locks
{

    public interface ILockProvider
    {
      Task<IDisposable> AcquireLock(string resourceId);
    }

    public class LockProvider : ILockProvider
    {
      private readonly IMongoCollection<LockModel> collection;

      public LockProvider(string mongodbConnString) 
      {
        // Create a lock collection
        var client = new MongoClient(mongodbConnString);

        var database = client.GetDatabase("mydb");

        // Get our collection
        collection = database.GetCollection<LockModel>("resourceLocks");
      }

      public async Task<IDisposable> AcquireLock(string resourceId)
      {
        // Determine the id of the lock
        var lockId = $"lock_{resourceId}";

        var distributedLock = new DistributedLock(collection, lockId);

        var startLockAcquireTime = DateTime.Now;

        // Try and acquire the lock
        while (!await distributedLock.AttemptGetLock())
        {
          // If we failed to acquire the lock after
          await Task.Delay(100);

          // Only try to acquire the lock for 10 seconds
          if ((DateTime.Now - startLockAcquireTime).TotalSeconds > 10)
          {
            throw new ApplicationException($"Could not acquire lock for {resourceId} within the timeout.");
          }
        }

        // This will only return if we have the lock.
        return distributedLock;
      }
    }
}
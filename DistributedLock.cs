using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace mongodb_locks
{
  public class DistributedLock : IDisposable
  {
    private readonly IMongoCollection<LockModel> collection;
    private readonly string lockId;
    private bool lockAcquired;

    public DistributedLock(IMongoCollection<LockModel> collection, string lockId)
    {
      this.collection = collection;
      this.lockId = lockId;
    }

    /// <summary>
    /// Attempts to acquire a lock.
    /// </summary>
    /// <returns>True if the lock was acquired, false otherwise.</returns>
    /// <remarks>
    ///  The way that a lock is acquired is with a FindAndUpdate operation
    ///  with an upsert, so that if a lock does not exist, one is created.
    ///  If the FindAndUpdate output is null, that means the document did not exist,
    ///  but one has been created. This means the lock now belongs to this thread.
    ///  If the FindAndModify output is anything else, that means the document exists already,
    ///  (i.e. the lock has been taken), so this thread must wait.
    ///  If two threads both attempt to upsert at the same time, one will get a duplicate key exception,
    ///  meaning it failed to acquire the lock.
    /// </remarks>
    public async Task<bool> AttemptGetLock()
    {
      if (lockAcquired)
      {
        return true;
      }

      try
      {
        var response = await collection.FindOneAndUpdateAsync<LockModel>(
          // Find a record with the lock ID
          x => x.Id == lockId,
          // If our 'upsert' creates a document, set the ID to the lock ID
          Builders<LockModel>.Update.SetOnInsert(x => x.Id, lockId),
          new FindOneAndUpdateOptions<LockModel>
          {
            // If the record doesn't exist, create it.
            IsUpsert = true,
            // Specifies that the result we want is the record BEFORE it
            // was created (this is important).
            ReturnDocument = ReturnDocument.Before
          });

        // If the result of the FindOneAndUpdateAsync is null, then that means there was no record 
        // before we ran our statement, so we now have the lock.
        // If the result is not null, then it means a document for the lock already existed, so someone else has the lock.
        if (response == null)
        {
          lockAcquired = true;
        }

        return lockAcquired;
      }
      catch (MongoCommandException ex)
      {
        // 11000 == MongoDB Duplicate Key error
        if (ex.Code == 11000)
        {
          // Two threads have tried to acquire a lock at the exact same moment on the same key, 
          // which will cause a duplicate key exception in MongoDB.
          // So this thread failed to acquire the lock.
          return false;
        }

        throw;
      }
    }

    public void Dispose()
    {
      if (lockAcquired)
      {
        // Delete the document with the specified lock ID, effectively releasing the lock.
        collection.DeleteOne(x => x.Id == lockId);
      }
    }
  }
}

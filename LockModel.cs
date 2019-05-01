
using System;

namespace mongodb_locks
{
    public class LockModel
    {        
        public string Id { get; set; }

        /// <summary>
        /// I'm going to set this to the moment in time when the lock should be cleared.
        /// </summary>
        public DateTime ExpireAt { get; set; }
    }
}
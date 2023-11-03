using System;

namespace Nuve.DataStore
{
    public class DataStoreProfileResult
    {
        public string? Method { get; set; }
        public string? Key { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
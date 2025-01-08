namespace tcli {
    public class RefreshAttempt {
        public int attemptId { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public string type { get; set; }
    }

    public class Refresh {
        public string requestId { get; set; }
        public int id { get; set; }
        public string refreshType { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public string status { get; set; }
        public string extendedStatus { get; set; }
        public List<RefreshAttempt> refreshAttempts { get; set; }
    }

    public class PbiRefreshList {
        public string odataContext { get; set; }
        public List<Refresh> value { get; set; }
    }
}
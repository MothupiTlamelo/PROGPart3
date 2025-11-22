using System;
using System.Collections.Generic;

namespace SystemClaim.Models
{
    public class Claims
    {
        public int Id { get; set; }

        // The user who created the claim
        public string WorkerUserId { get; set; }

        // Basic claim details
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Department { get; set; }

        public decimal RatePerJob { get; set; }
        public int NumberOfJobs { get; set; }
        public decimal TotalAmount { get; set; }

        // Workflow status fields
        public string? Status { get; set; }
        public string? RejectReason { get; set; }
        public bool ReasonRequired { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 🔥 Navigation — each claim can have multiple uploaded documents
        public ICollection<UploadDocument>? Documents { get; set; }
    }
}

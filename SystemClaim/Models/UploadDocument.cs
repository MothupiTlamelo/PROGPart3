using System.ComponentModel.DataAnnotations;

namespace SystemClaim.Models
{
    public class UploadDocument
    {
        [Key]
        public int DocumentID { get; set; }   // PK
        public int ClaimID { get; set; }      // FK 
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }

        // Navigation
        public Claims Claim { get; set; }

    }
}

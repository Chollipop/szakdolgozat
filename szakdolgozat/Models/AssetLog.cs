using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using szakdolgozat.Services;

namespace szakdolgozat.Models
{
    [Index(nameof(LogID), IsUnique = true)]
    [Index(nameof(AssetID))]
    public class AssetLog
    {
        [Key]
        public int LogID { get; set; }

        [Required]
        public int AssetID { get; set; }

        [ForeignKey("AssetID")]
        public Asset Asset { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; }

        [Required]
        public DateTime? Timestamp { get; set; }

        private string performedBy;
        [Required]
        [MaxLength(100)]
        public string PerformedBy
        {
            get => performedBy;
            set
            {
                performedBy = value;
                PerformedByFullName = GetFullName();
            }
        }

        [NotMapped]
        public string PerformedByFullName { get; set; }

        public string GetFullName()
        {
            return AuthenticationService.Instance.GetFullNameByGuidAsync(new Guid(PerformedBy)).Result;
        }
    }
}

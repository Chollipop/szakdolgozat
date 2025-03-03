using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using szakdolgozat.Services;

namespace szakdolgozat.Models
{
    [Index(nameof(AssignmentID), IsUnique = true)]
    [Index(nameof(AssetID))]
    public class AssetAssignment
    {
        [Key]
        public int AssignmentID { get; set; }

        [Required]
        public int AssetID { get; set; }

        [ForeignKey("AssetID")]
        public Asset Asset { get; set; }

        private string user;
        [Required]
        [MaxLength(100)]
        public string User
        {
            get => user;
            set
            {
                user = value;
                UserFullName = GetUserFullName();
            }
        }

        [NotMapped]
        public string UserFullName { get; set; }

        [Required]
        public DateTime? AssignmentDate { get; set; }

        [Required]
        public DateTime? ReturnDate { get; set; }

        public string GetUserFullName()
        {
            return AuthenticationService.Instance.GetFullNameByGuidAsync(new Guid(User)).Result;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using szakdolgozat.Services;

namespace szakdolgozat.Models
{
    [Index(nameof(AssetID), IsUnique = true)]
    [Index(nameof(AssetTypeID))]
    public class Asset
    {
        [Key]
        public int AssetID { get; set; }

        [Required]
        [MaxLength(100)]
        public string AssetName { get; set; }

        [Required]
        public int AssetTypeID { get; set; }

        [ForeignKey("AssetTypeID")]
        public AssetType AssetType { get; set; }

        public int? SubtypeID { get; set; }

        [ForeignKey("SubtypeID")]
        public Subtype? Subtype { get; set; }

        private string owner;
        [Required]
        [MaxLength(100)]
        public string Owner
        {
            get => owner;
            set
            {
                owner = value;
                OwnerFullName = GetOwnerFullName();
            }
        }

        [NotMapped]
        public string OwnerFullName { get; set; }

        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public DateTime? PurchaseDate { get; set; }

        [Required]
        public decimal? Value { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public string GetOwnerFullName()
        {
            return AuthenticationService.Instance.GetFullNameByGuidAsync(new Guid(Owner)).Result;
        }
    }
}

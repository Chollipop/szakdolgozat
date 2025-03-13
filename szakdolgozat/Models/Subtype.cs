using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace szakdolgozat.Models
{
    [Index(nameof(TypeID), IsUnique = true)]
    [Index(nameof(AssetTypeID))]
    public class Subtype
    {
        [Key]
        public int TypeID { get; set; }

        [Required]
        public int? AssetTypeID { get; set; }

        [ForeignKey("AssetTypeID")]
        public AssetType AssetType { get; set; }

        [Required]
        [MaxLength(100)]
        public string TypeName { get; set; }
    }
}

﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace szakdolgozat.Models
{
    [Index(nameof(TypeID), IsUnique = true)]
    public class AssetType
    {
        [Key]
        public int TypeID { get; set; }

        [Required]
        [MaxLength(100)]
        public string TypeName { get; set; }
    }
}

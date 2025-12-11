using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GbService.Model.Domain
{
    [Table("Partner")] // Make sure this matches your SQL Table Name
    public class Partner
    {
        [Key]
        public long PartnerId { get; set; }
        public string Name { get; set; }
    }
}
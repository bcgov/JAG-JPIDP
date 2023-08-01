﻿using jumwebapi.Models.Lookups;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace jumwebapi.Models;
[Table("Agency")]
public class AgencyModel : BaseAuditable
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AgencyCode { get; set; } = string.Empty;
}


    



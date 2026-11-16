using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace __PROJECT__.Domain.Entities;

public partial class rf_email_provider : BaseAuditEntity
{
    public int id { get; set; }

    [StringLength(20)]
    public string provider_type { get; set; } = "NONE";

    [StringLength(100)]
    public string? provider_name { get; set; }

    public string? configuration_json { get; set; }

    [StringLength(255)]
    public string? from_email { get; set; }

    [StringLength(100)]
    public string? from_name { get; set; }

    public int? priority { get; set; }

    public DateTime? last_tested_at { get; set; }

    [StringLength(30)]
    public string? last_test_status { get; set; }

    public string? last_test_message { get; set; }
}

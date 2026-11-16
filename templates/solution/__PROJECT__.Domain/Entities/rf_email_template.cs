using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace __PROJECT__.Domain.Entities;

public partial class rf_email_template : BaseAuditEntity
{
    [StringLength(100)]
    public string template_key { get; set; } = string.Empty;

    [StringLength(255)]
    public string subject { get; set; } = string.Empty;

    public string? body_html { get; set; }

    public string? body_text { get; set; }
}

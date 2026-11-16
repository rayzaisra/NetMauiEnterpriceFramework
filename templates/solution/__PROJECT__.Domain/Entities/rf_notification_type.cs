using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace __PROJECT__.Domain.Entities;

public partial class rf_notification_type : BaseEntity
{
    [Column(TypeName = "varchar(50)")]
    [StringLength(50)]
    public string id { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? short_text { get; set; }

    public string? long_text { get; set; }

    public bool? is_send_mail { get; set; }

    public bool? is_send_web { get; set; }

    public string? href { get; set; }
}

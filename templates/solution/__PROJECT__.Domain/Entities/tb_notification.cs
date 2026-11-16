using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace __PROJECT__.Domain.Entities;

public partial class tb_notification : BaseEntity
{
    public long id { get; set; }

    [Column(TypeName = "varchar(50)")]
    [StringLength(50)]
    public string? notification_type_id { get; set; }

    [StringLength(100)]
    public string? receiver_user_id { get; set; }

    [StringLength(100)]
    public string? reff_id { get; set; }

    public bool? is_seen { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace __PROJECT__.Domain.Entities;

[Table("tb_log_activity")]
public partial class tb_log_activity
{
    public long id { get; set; }

    [StringLength(100)]
    public string? user_id { get; set; }

    [StringLength(64)]
    public string? ip_address { get; set; }

    [StringLength(10)]
    public string? http_method { get; set; }

    [StringLength(500)]
    public string? path { get; set; }

    [StringLength(300)]
    public string? route_template { get; set; }

    public int status_code { get; set; }

    public bool success { get; set; }

    public int duration_ms { get; set; }

    [StringLength(64)]
    public string? correlation_id { get; set; }

    [StringLength(500)]
    public string? user_agent { get; set; }

    [StringLength(20)]
    public string? client_type { get; set; }

    [StringLength(20)]
    public string? error_code { get; set; }

    public DateTime created_date { get; set; }
}

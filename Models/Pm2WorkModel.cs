using System.ComponentModel.DataAnnotations.Schema;

namespace stcpui.Models;

[Table("Pm2Work")]
public class Pm2WorkModel
{
    public long Id { get; set; }
    
    public string WorkDir { get; set; } = "/usr/local/bin";

}
using PericoOnFire_2026.Shared.ENUM;
namespace PericoOnFire_2026.Shared.DTOs;
public class HistorialComandasDTO
{
    public int Total { get; set; }
    public List<ComandaHistorialDTO> Comandas { get; set; } = new();
}
public class ComandaHistorialDTO
{
    public int Id { get; set; }
    public int NumeroDiario { get; set; }
    public int? NumeroMesa { get; set; }
    public string? Sala { get; set; }
    public string? Cliente { get; set; }
    public string? Empleado { get; set; }
    public DateTime FechaApertura { get; set; }
    public DateTime FechaCierre { get; set; }
    public EnumEstadoComanda Estado { get; set; }
    public EnumTipoServicio TipoServicio { get; set; }
    public decimal Total { get; set; }
    public List<ItemHistorialDTO> Items { get; set; } = new();
    public List<string> Pagos { get; set; } = new();
}
public class ItemHistorialDTO
{
    public string Producto { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public EnumEstadoPedido Estado { get; set; }
    public string? Observacion { get; set; }
    public string? MotivoCancelacion { get; set; }
}

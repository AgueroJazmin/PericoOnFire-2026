namespace PericoOnFire_2026.Shared.DTOs;
public class FotoPanelDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}
public class CumpleanosEmpleadoDTO
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int? Dia { get; set; }
    public int? Mes { get; set; }
    public DateTime? Proximo { get; set; }
}
public class GuardarCumpleanosDTO
{
    public int? Dia { get; set; }
    public int? Mes { get; set; }
}

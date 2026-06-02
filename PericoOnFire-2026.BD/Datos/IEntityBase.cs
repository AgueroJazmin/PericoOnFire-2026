using  PericoOnFire_2026.Shared.ENUM;

namespace  PericoOnFire_2026.BD.Datos
{
    public interface IEntityBase
    {
        EnumEstadoRegistro EstadoRegistro { get; set; }
        int Id { get; set; }
    }
}
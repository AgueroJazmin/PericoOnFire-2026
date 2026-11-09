using System;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class CancelarPedidoDTO
    {
        // Es obligatorio y queda guardado en la comanda. Si no se especifica, se guarda como "Cancelado sin motivo".
        public string Motivo { get; set; } = "";
    }
}
using System.ComponentModel.DataAnnotations;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class ConfirmarComandaDTO
    {
        [Required]
        public int IdMesa { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Range(1, 50)]
        public int CantidadComensales { get; set; } = 1;

        public string? Observaciones { get; set; }

        [MinLength(1)]
        public List<ItemPedidoDTO> Items { get; set; } = new();
    }

    public class ComandaConfirmadaDTO
    {
        public int IdComanda { get; set; }
        public string NumeroComanda => $"COM-{IdComanda:D6}";
        public List<int> IdsPedidos { get; set; } = new();
    }
}

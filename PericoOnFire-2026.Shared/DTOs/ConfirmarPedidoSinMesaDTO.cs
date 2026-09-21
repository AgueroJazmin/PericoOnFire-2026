using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class ConfirmarPedidoSinMesaDTO
    {
        [Required]
        public int IdUsuario { get; set; }

        [Required]
        public EnumTipoServicio TipoServicio { get; set; }

        [Required(ErrorMessage = "El nombre del cliente es obligatorio")]
        public string NombreCliente { get; set; } = "";
        public string? Telefono { get; set; }

        // Obligatoria solo si TipoServicio == Delivery; se valida en el controller
        // porque acá no sabemos todavía el TipoServicio al momento de bindear.
        //Lo dejo porque considero que es necesario para que el Repartidor pueda ver la direccion de entrega
        public string? Direccion { get; set; } 

        // Horario en el que el cliente quiere el pedido listo (retiro/entrega). Opcional:
        // si no se manda, el pedido entra a cocina/barra apenas se confirma, como siempre.
        public DateTime? HoraDeseada { get; set; }
        public string? Observaciones { get; set; }

        [MinLength(1)]
        public List<ItemPedidoDTO> Items { get; set; } = new();
    }
}

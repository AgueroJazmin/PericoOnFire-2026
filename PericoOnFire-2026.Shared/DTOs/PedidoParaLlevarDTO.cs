using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class PedidoParaLlevarDTO
    {
        public int IdComanda { get; set; }
        public string NumeroComanda => $"COM-{IdComanda:D6}";
        public EnumTipoServicio TipoServicio { get; set; }
        public EnumEstadoComanda EstadoComanda { get; set; }
        public string NombreCliente { get; set; } = "";
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public DateTime FechaApertura { get; set; }
        public List<ItemCuentaDTO> Items { get; set; } = new();
        public List<PedidoSectorEstadoDTO> Pedidos { get; set; } = new();
        public int? IdDelivery { get; set; }
        public string? NombreDelivery { get; set; }

        // Todo lo que no está cancelado tiene que estar ListoParaRetirar para
        // poder repartirlo o cerrarlo ya sea si esta lista está vacía, no hay nada
        // real que entregar. Por eso también se controla en el server.
        public bool TodoListo => Pedidos.Any() && Pedidos.All(p => p.Estado == EnumEstadoPedido.ListoParaRetirar);
        public bool TodoEnCamino => Pedidos.Any() && Pedidos.All(p => p.Estado == EnumEstadoPedido.EnCamino);
        public bool TodoEntregado => Pedidos.Any() && Pedidos.All(p => p.Estado == EnumEstadoPedido.Entregado);

        // Cuando ya está listo el pedido, pasa a: para retirar, en camino, o ya entregado.
        // Se usa para saber si ya se puede pasar la cuenta a caja, sin tener que
        // esperar a que el cliente lo retire o el repartidor lo entregue para poder cobrar.
        public bool TodoListoOMasAlla => Pedidos.Any() && Pedidos.All(p =>p.Estado == EnumEstadoPedido.ListoParaRetirar 
       ||p.Estado == EnumEstadoPedido.EnCamino ||p.Estado == EnumEstadoPedido.Entregado);

    }
}

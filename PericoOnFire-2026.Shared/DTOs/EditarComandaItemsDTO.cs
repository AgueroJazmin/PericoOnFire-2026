using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;

//Este DTO es nuevo y es para la edición de pedidos Take Away y Delivery, los  cuales ya fueron cobrados
//y ahora se quieren cambiar los productos.

namespace PericoOnFire_2026.Shared.DTOs
{
    // Productos que tiene que quedar teniendo el pedido de Take Away/Delivery después de editarlo.
    public class EditarComandaItemsDTO
    {
        public List<ItemPedidoDTO> Items { get; set; } = new();

        // Solo se tiene en cuenta si el pedido ya tenía un cobro, por ejemplo un Take Away pagado al pedir.
        public EnumTipoDescuento TipoDescuento { get; set; } = EnumTipoDescuento.Ninguno;
        public decimal ValorDescuento { get; set; }

        // Delivery sin cobrar con efectivo previsto, tiene que cubrir el nuevo total.
        public decimal? EfectivoPrevistoDelivery { get; set; }
    }

    public class ResultadoEdicionComandaDTO
    {
        public decimal NuevoTotal { get; set; }
        // Lo que falta cobrar en Caja, es decir el pedido ya estaba pagado y ahora cuesta más
        public decimal SaldoACobrar { get; set; }
        // Lo que hay que devolver al cliente, me refiero al pedido ya estaba pagado y ahora cuesta menos
        public decimal DevolucionPendiente { get; set; }
    }
}
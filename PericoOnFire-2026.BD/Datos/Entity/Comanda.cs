using PericoOnFire_2026.Shared.ENUM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    public class Comanda : EntityBase
    {
        public int? IdMesa { get; set; }

        public int? IdCliente { get; set; }

        public int? IdUsuario { get; set; }

        public EnumTipoServicio TipoServicio { get; set; }

        public EnumEstadoComanda Estado { get; set; } = EnumEstadoComanda.Abierta;

        public DateTime FechaApertura { get; set; } = DateTime.Now;

        public DateTime? FechaCierre { get; set; }

        public decimal Total { get; set; } = 0;

        [MaxLength(300)]
        public string? Observaciones { get; set; }

        public Mesa? Mesa { get; set; }

        public Cliente? Cliente { get; set; }

        public Usuario? Usuario { get; set; }

        public List<Pedido> Pedidos { get; set; } = new();

        public List<Pago> Pagos { get; set; } = new();
    }
}

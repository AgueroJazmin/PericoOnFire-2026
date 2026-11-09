using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PericoOnFire_2026.BD.Datos.Entity
{
    // Una fila por cada componente de una promo.
    // La promo en sí es un Producto (con su precio fijo); esta tabla dice qué trae adentro.
    // Ej: Promo "2x Hamburguesas Cheddar" -> IdProductoComponente = Hamburguesa Cheddar, Cantidad = 2
    public class PromocionItem : EntityBase
    {
        public int IdProductoPromo { get; set; }

        public int IdProductoComponente { get; set; }

        [Range(1, 99, ErrorMessage = "La cantidad debe ser mayor a cero")]
        public int Cantidad { get; set; } = 1;

        public Producto ProductoPromo { get; set; } = null!;

        public Producto ProductoComponente { get; set; } = null!;
    }
}
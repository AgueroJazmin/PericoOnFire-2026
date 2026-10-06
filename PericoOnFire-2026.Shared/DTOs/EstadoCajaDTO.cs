using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.DTOs
{
    public class EstadoCajaDTO
    {
        //Todos los importes son del turno abierto.
        //Solo el efectivo mueve el cajón, el resto de metodos de pago se informan aparte.
        public bool Abierta { get; set; }
        public TurnoCajaDTO? Turno { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal CobrosEfectivo { get; set; }
        public decimal CobrosDebito { get; set; }
        public decimal CobrosCredito { get; set; }
        public decimal CobrosTransferencia { get; set; }
        public decimal CobrosQR { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Egresos { get; set; }
        public decimal Retiros { get; set; }
        public int CantidadVentas { get; set; }

        //Con la caja cerrada, el efectivo que se contó en el último cierre para sugerirlo como monto inicial.
        public decimal? MontoInicialSugerido { get; set; }
        public decimal EfectivoEsperado => MontoInicial + CobrosEfectivo + Ingresos - Egresos - Retiros;
        public decimal TotalCobrado => CobrosEfectivo + CobrosDebito + CobrosCredito + CobrosTransferencia + CobrosQR;
    }
}

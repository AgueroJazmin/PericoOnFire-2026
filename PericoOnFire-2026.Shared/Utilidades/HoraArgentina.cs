using System;
using System.Collections.Generic;
using System.Text;

namespace PericoOnFire_2026.Shared.Utilidades
{
    public class HoraArgentina
    {
        //La base guarda todo en UTC y Argentina está en UTC-3 (sin horario de verano), igual que
        //NumeroComandaDiario en el server. Se usa esta regla única para "el día de hoy", los
        //rangos de fechas y las horas que se muestran, tanto en el server como en el cliente
        //(el cliente puede estar prerenderizado en el server, por eso no se usa ToLocalTime).
        private const int DiferenciaConUtc = -3;

        public static DateTime DesdeUtc(DateTime fechaUtc) => fechaUtc.AddHours(DiferenciaConUtc);

        public static DateTime HoyLocal => DesdeUtc(DateTime.UtcNow).Date;

        //Primer instante (en UTC) del día local indicado.
        public static DateTime InicioDiaUtc(DateTime fechaLocal) =>
            DateTime.SpecifyKind(fechaLocal.Date.AddHours(-DiferenciaConUtc), DateTimeKind.Utc);
    }
}

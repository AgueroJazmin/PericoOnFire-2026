using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PericoOnFire_2026.Servicio.ServicioHttp
{
    public class HttpRespuesta<T>
    {
        public T? Respuesta { get; }
        public bool Error { get; }
        public HttpResponseMessage HttpResponseMessage { get; set; }
        public string? MensajeServidor { get; }

        public HttpRespuesta(T? respuesta,
                             bool error,
                             HttpResponseMessage httpResponseMessage, string? mensajeServidor = null)
        {
            Respuesta = respuesta;
            Error = error;
            HttpResponseMessage = httpResponseMessage;
            MensajeServidor = mensajeServidor;
        }

        public string ObtenerError()
        {
            if (!Error)
            {
                return string.Empty;
            }
            else
            {
                var statuscode = HttpResponseMessage.StatusCode;

                switch (statuscode)
                {
                    case HttpStatusCode.NotFound:
                        return "Recurso no encontrado.";
                    case HttpStatusCode.Unauthorized:
                        return "No está logueado.";
                    case HttpStatusCode.Forbidden:
                        return "No tiene autorización a ejecutar este proceso.";
                    case HttpStatusCode.BadRequest:
                        return "No se pudo procesar la información.";
                    case HttpStatusCode.Conflict:
                        return string.IsNullOrWhiteSpace(MensajeServidor)
                            ? "No se pudo completar la operación por un conflicto con datos existentes."
                            : MensajeServidor;
                    default:
                        return string.IsNullOrWhiteSpace(MensajeServidor)
                            ? $"Error en la llamada HTTP. Código de estado: {statuscode}"
                            : MensajeServidor;
                }
            }
        }
    }
}

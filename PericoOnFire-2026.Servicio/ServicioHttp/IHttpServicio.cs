namespace PericoOnFire_2026.Servicio.ServicioHttp
{
    public interface IHttpServicio
    {
        Task<HttpRespuesta<object>> Delete(string url);
        Task<HttpRespuesta<T>> Get<T>(string url);
        Task<string> ObtenerMensajeError(HttpResponseMessage response);
        Task<HttpRespuesta<TResp>> Post<T, TResp>(string url, T entidad);
        Task<HttpRespuesta<object>> Post<T>(string url, T entidad);
        Task<HttpRespuesta<TResp>> Put<T, TResp>(string url, T entidad);
        Task<HttpRespuesta<object>> Put<T>(string url, T entidad);
    }
}
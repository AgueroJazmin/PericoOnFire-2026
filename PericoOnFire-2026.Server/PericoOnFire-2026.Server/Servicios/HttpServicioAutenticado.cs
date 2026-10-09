using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PericoOnFire_2026.Servicio.ServicioHttp;
using System.Security.Claims;

namespace PericoOnFire_2026.Server.Servicios
{
    //Cuando una página corre en el servidor (prerender o circuito de Blazor Server) el HttpClient no tiene
    //las cookies del navegador, y la API (que es esta misma aplicación) respondía 401 "No está logueado"
    //hasta que la página pasaba a correr en WebAssembly (por eso había que recargar una o dos veces).
    //Esta clase arma el IHttpServicio del servidor reenviándole a la API la identidad del usuario logueado.
    public static class HttpServicioAutenticado
    {
        //Un solo handler compartido para todos los circuitos, así no se agotan los sockets.
        private static readonly SocketsHttpHandler handlerCompartido = new()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            UseCookies = false
        };

        public static IHttpServicio Crear(IServiceProvider servicios, Uri direccionBase)
        {
            var handler = new CookieDelUsuarioHandler(
                servicios.GetRequiredService<AuthenticationStateProvider>(),
                servicios.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>())
            {
                InnerHandler = handlerCompartido
            };

            var cliente = new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = direccionBase
            };

            return new HttpServicio(cliente);
        }

        private sealed class CookieDelUsuarioHandler : DelegatingHandler
        {
            private readonly AuthenticationStateProvider authStateProvider;
            private readonly IOptionsMonitor<CookieAuthenticationOptions> opcionesCookie;

            public CookieDelUsuarioHandler(
                AuthenticationStateProvider authStateProvider,
                IOptionsMonitor<CookieAuthenticationOptions> opcionesCookie)
            {
                this.authStateProvider = authStateProvider;
                this.opcionesCookie = opcionesCookie;
            }

            //La cookie se arma en cada request (y no una sola vez) porque un circuito puede durar horas
            //y la cookie que se manda dura solo unos minutos.
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var cookie = await ArmarCookieAsync();
                if (cookie != null)
                {
                    request.Headers.Remove("Cookie");
                    request.Headers.TryAddWithoutValidation("Cookie", cookie);
                }

                return await base.SendAsync(request, cancellationToken);
            }

            private async Task<string?> ArmarCookieAsync()
            {
                ClaimsPrincipal usuario;
                try
                {
                    usuario = (await authStateProvider.GetAuthenticationStateAsync()).User;
                }
                catch (InvalidOperationException)
                {
                    //Todavía no hay estado de autenticación en este scope: se envía sin cookie.
                    return null;
                }

                if (usuario.Identity?.IsAuthenticated != true)
                    return null;

                var opciones = opcionesCookie.Get(IdentityConstants.ApplicationScheme);
                var ahora = DateTimeOffset.UtcNow;

                var propiedades = new AuthenticationProperties
                {
                    IssuedUtc = ahora,
                    ExpiresUtc = ahora.AddMinutes(5)
                };

                var ticket = new AuthenticationTicket(usuario, propiedades, IdentityConstants.ApplicationScheme);

                return $"{opciones.Cookie.Name}={opciones.TicketDataFormat.Protect(ticket)}";
            }
        }
    }
}
using PericoOnFire_2026.BD.Datos;
using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Repositorio.Seguridad
{
    public interface IServicioSeguridad
    {
        Task<ResultadoOperacionSeguridad> ActivarUsuario(string email);
        Task<ResultadoOperacionSeguridad> AsignarRol(AsignarRolDTO dto);
        Task<ServicioSeguridad.ResultadoContrasena> CambiarContrasenaPropia(string idUsuario, CambiarMiContrasenaDTO dto);
        Task<ServicioSeguridad.ResultadoCrearEmpleado> CrearEmpleado(CrearEmpleadoDTO dto);
        Task<ResultadoOperacionSeguridad> DesactivarUsuario(string email);
        Task<ServicioSeguridad.ResultadoContrasena> EstablecerContrasena(EstablecerContrasenaDTO dto);
        Task GuardarContrasenaVisible(ApplicationUser usuario, string contrasena);
        Task<string?> ObtenerContrasenaVisiblePorEmail(string email);
        Task<string?> ObtenerContrasenaVisiblePorId(string idUsuario);
        Task<List<UsuarioRolDTO>> ObtenerUsuarios();
        Task<ResultadoOperacionSeguridad> RemoverRol(AsignarRolDTO dto);
    }
}
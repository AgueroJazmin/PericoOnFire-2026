using PericoOnFire_2026.Shared.DTOs;
using PericoOnFire_2026.Shared.ENUM;

namespace PericoOnFire_2026.Repositorio.Seguridad
{
    public interface IServicioSeguridad
    {
        Task<ResultadoOperacionSeguridad> AsignarRol(AsignarRolDTO dto);
        Task<ServicioSeguridad.ResultadoCrearEmpleado> CrearEmpleado(CrearEmpleadoDTO dto);
        Task<List<UsuarioRolDTO>> ObtenerUsuarios();
        Task<ResultadoOperacionSeguridad> RemoverRol(AsignarRolDTO dto);
    }
}
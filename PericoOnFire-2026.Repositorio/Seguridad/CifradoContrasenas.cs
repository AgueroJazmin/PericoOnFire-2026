using System.Security.Cryptography;
using System.Text;

namespace PericoOnFire_2026.Repositorio.Seguridad
{
    // Las contraseñas de Identity se guardan solo como hash y no se pueden recuperar.
    // Como el administrador necesita poder VERLAS, se guarda además una copia cifrada
    // (AES-GCM) que solo se puede leer con la clave de configuración "Seguridad:ClaveContrasenas".
    // Si la clave no está configurada, la función queda deshabilitada (no se guarda ni se muestra nada)
    // pero el login y el cambio de contraseña siguen funcionando normalmente.
    public interface ICifradoContrasenas
    {
        bool Habilitado { get; }
        string? Cifrar(string textoPlano);
        string? Descifrar(string textoCifrado);
    }

    public class CifradoContrasenas : ICifradoContrasenas
    {
        private const int TamanoNonce = 12;
        private const int TamanoTag = 16;
        private readonly byte[]? clave;

        public CifradoContrasenas(string? claveConfigurada)
        {
            if (!string.IsNullOrWhiteSpace(claveConfigurada))
                clave = SHA256.HashData(Encoding.UTF8.GetBytes(claveConfigurada));
        }

        public bool Habilitado => clave != null;

        public string? Cifrar(string textoPlano)
        {
            if (clave == null || string.IsNullOrEmpty(textoPlano)) return null;

            var datos = Encoding.UTF8.GetBytes(textoPlano);
            var nonce = RandomNumberGenerator.GetBytes(TamanoNonce);
            var cifrado = new byte[datos.Length];
            var tag = new byte[TamanoTag];

            using var aes = new AesGcm(clave, TamanoTag);
            aes.Encrypt(nonce, datos, cifrado, tag);

            // formato: nonce | tag | cifrado, todo en base64
            var resultado = new byte[TamanoNonce + TamanoTag + cifrado.Length];
            Buffer.BlockCopy(nonce, 0, resultado, 0, TamanoNonce);
            Buffer.BlockCopy(tag, 0, resultado, TamanoNonce, TamanoTag);
            Buffer.BlockCopy(cifrado, 0, resultado, TamanoNonce + TamanoTag, cifrado.Length);
            return Convert.ToBase64String(resultado);
        }

        public string? Descifrar(string textoCifrado)
        {
            if (clave == null || string.IsNullOrEmpty(textoCifrado)) return null;

            try
            {
                var todo = Convert.FromBase64String(textoCifrado);
                if (todo.Length < TamanoNonce + TamanoTag) return null;

                var nonce = todo.AsSpan(0, TamanoNonce);
                var tag = todo.AsSpan(TamanoNonce, TamanoTag);
                var cifrado = todo.AsSpan(TamanoNonce + TamanoTag);
                var datos = new byte[cifrado.Length];

                using var aes = new AesGcm(clave, TamanoTag);
                aes.Decrypt(nonce, cifrado, tag, datos);
                return Encoding.UTF8.GetString(datos);
            }
            catch (Exception ex) when (ex is CryptographicException || ex is FormatException)
            {
                // Clave distinta a la que se usó para cifrar, o dato dañado.
                return null;
            }
        }
    }
}
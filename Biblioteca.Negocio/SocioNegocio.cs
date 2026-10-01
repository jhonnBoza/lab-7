using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class SocioNegocio
{
    private readonly SocioDatos _socios = new();

    public Task<List<Socio>> ListarAsync() => _socios.ListarActivosAsync();

    public Task<List<Socio>> BuscarAsync(string criterio)
    {
        criterio = (criterio ?? string.Empty).Trim();
        if (criterio.Length == 0)
        {
            return _socios.ListarActivosAsync();
        }

        return _socios.BuscarAsync(criterio);
    }

    public Task<int> ContarLibrosPendientesAsync(int socioId) => _socios.ContarLibrosPendientesAsync(socioId);

    public async Task<int> InsertarAsync(Socio socio)
    {
        await ValidarAsync(socio);
        return await _socios.InsertarAsync(socio);
    }

    public async Task ActualizarAsync(Socio socio)
    {
        var actual = await _socios.ObtenerPorIdAsync(socio.SocioId)
            ?? throw new ReglaNegocioException("El socio no existe.");

        if (!actual.Activo)
        {
            throw new ReglaNegocioException("No se puede modificar un socio dado de baja.");
        }

        await ValidarAsync(socio);
        await _socios.ActualizarAsync(socio);
    }

    public async Task EliminarAsync(int socioId)
    {
        var actual = await _socios.ObtenerPorIdAsync(socioId)
            ?? throw new ReglaNegocioException("El socio no existe.");

        if (!actual.Activo)
        {
            throw new ReglaNegocioException("El socio ya está dado de baja.");
        }

        if (await _socios.TienePrestamosPendientesAsync(socioId))
        {
            throw new ReglaNegocioException(
                "No se puede dar de baja un socio con préstamos pendientes. Primero registre la devolución.");
        }

        await _socios.EliminarLogicoAsync(socioId);
    }

    private async Task ValidarAsync(Socio socio)
    {
        socio.DNI = (socio.DNI ?? string.Empty).Trim();
        socio.Nombre = (socio.Nombre ?? string.Empty).Trim();
        socio.Email = (socio.Email ?? string.Empty).Trim();

        if (socio.DNI.Length != 8 || !socio.DNI.All(char.IsDigit))
        {
            throw new ReglaNegocioException("El DNI debe tener 8 dígitos.");
        }

        if (socio.Nombre.Length == 0)
        {
            throw new ReglaNegocioException("El nombre del socio es obligatorio.");
        }

        if (socio.Nombre.Length > 120)
        {
            throw new ReglaNegocioException("El nombre no puede superar 120 caracteres.");
        }

        if (!EmailValido(socio.Email) || socio.Email.Length > 120)
        {
            throw new ReglaNegocioException("Ingrese un correo electrónico válido.");
        }

        if (await _socios.ExisteDniAsync(socio.DNI, socio.SocioId))
        {
            throw new ReglaNegocioException("Ya existe un socio con ese DNI.");
        }
    }

    private static bool EmailValido(string email)
    {
        var partes = email.Split('@');
        return partes.Length == 2
            && partes[0].Length > 0
            && partes[1].Contains('.')
            && !partes[1].StartsWith('.')
            && !partes[1].EndsWith('.');
    }
}

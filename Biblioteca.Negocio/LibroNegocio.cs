using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class LibroNegocio
{
    private readonly ILibroRepositorio _libros;
    private readonly AutorDatos _autores = new();

    public LibroNegocio()
        : this(new LibroRepositorio())
    {
    }

    public LibroNegocio(ILibroRepositorio libros)
    {
        _libros = libros;
    }

    public Task<List<Libro>> ListarAsync() => _libros.ListarAsync();

    public Task<List<Autor>> ListarAutoresAsync() => _autores.ListarActivosAsync();

    public Task<List<Libro>> BuscarAsync(string criterio)
    {
        criterio = (criterio ?? string.Empty).Trim();
        if (criterio.Length == 0)
        {
            return _libros.ListarAsync();
        }

        return _libros.BuscarAsync(criterio);
    }

    public async Task<int> InsertarAsync(Libro libro)
    {
        await ValidarAsync(libro);
        return await _libros.InsertarAsync(libro);
    }

    public async Task ActualizarAsync(Libro libro)
    {
        var actual = await _libros.ObtenerPorIdAsync(libro.LibroId)
            ?? throw new ReglaNegocioException("El libro no existe.");

        if (!actual.Activo)
        {
            throw new ReglaNegocioException("No se puede modificar un libro dado de baja.");
        }

        await ValidarAsync(libro);
        await _libros.ActualizarAsync(libro);
    }

    public async Task EliminarAsync(int libroId)
    {
        var actual = await _libros.ObtenerPorIdAsync(libroId)
            ?? throw new ReglaNegocioException("El libro no existe.");

        if (!actual.Activo)
        {
            throw new ReglaNegocioException("El libro ya está dado de baja.");
        }

        if (await _libros.TienePrestamosPendientesAsync(libroId))
        {
            throw new ReglaNegocioException(
                "No se puede dar de baja un libro con préstamos pendientes. Primero registre la devolución.");
        }

        await _libros.EliminarLogicoAsync(libroId);
    }

    private async Task ValidarAsync(Libro libro)
    {
        libro.Titulo = (libro.Titulo ?? string.Empty).Trim();
        libro.ISBN = (libro.ISBN ?? string.Empty).Trim();

        if (libro.Titulo.Length == 0)
        {
            throw new ReglaNegocioException("El título del libro es obligatorio.");
        }

        if (libro.Titulo.Length > 200)
        {
            throw new ReglaNegocioException("El título no puede superar 200 caracteres.");
        }

        if (libro.ISBN.Length == 0)
        {
            throw new ReglaNegocioException("El ISBN es obligatorio.");
        }

        if (libro.ISBN.Length > 20)
        {
            throw new ReglaNegocioException("El ISBN no puede superar 20 caracteres.");
        }

        if (libro.Ejemplares < 0)
        {
            throw new ReglaNegocioException("La cantidad de ejemplares no puede ser negativa.");
        }

        var autor = await _autores.ObtenerPorIdAsync(libro.AutorId);
        if (autor == null || !autor.Activo)
        {
            throw new ReglaNegocioException("Seleccione un autor activo.");
        }

        if (await _libros.ExisteIsbnAsync(libro.ISBN, libro.LibroId))
        {
            throw new ReglaNegocioException("Ya existe un libro con ese ISBN.");
        }
    }
}

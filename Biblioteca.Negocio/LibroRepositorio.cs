using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

// Implementa ILibroRepositorio y delega en LibroDatos.
// Vive en Negocio para no crear una referencia circular: Datos no puede
// referenciar a Negocio porque Negocio ya referencia a Datos.
internal sealed class LibroRepositorio : ILibroRepositorio
{
    private readonly LibroDatos _datos = new();

    public Task<List<Libro>> ListarAsync() => _datos.ListarAsync();

    public Task<List<Libro>> BuscarAsync(string criterio) => _datos.BuscarAsync(criterio);

    public Task<Libro?> ObtenerPorIdAsync(int libroId) => _datos.ObtenerPorIdAsync(libroId);

    public Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId) => _datos.ExisteIsbnAsync(isbn, excluirLibroId);

    public Task<bool> TienePrestamosPendientesAsync(int libroId) => _datos.TienePrestamosPendientesAsync(libroId);

    public Task<int> InsertarAsync(Libro libro) => _datos.InsertarAsync(libro);

    public Task ActualizarAsync(Libro libro) => _datos.ActualizarAsync(libro);

    public Task EliminarLogicoAsync(int libroId) => _datos.EliminarLogicoAsync(libroId);
}

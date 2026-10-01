using System.Data;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class LibroDatos
{
    private const string SelectLibro = @"
            SELECT l.LibroId, l.Titulo, l.ISBN, l.AutorId, a.Nombre AS AutorNombre, l.Ejemplares, l.Activo
            FROM Libros l
            INNER JOIN Autores a ON a.AutorId = l.AutorId";

    public Task<List<Libro>> ListarAsync()
    {
        const string sql = SelectLibro + @"
            WHERE l.Activo = 1
            ORDER BY l.Titulo";

        return ConsultarListaAsync(sql, null);
    }

    public Task<List<Libro>> BuscarAsync(string criterio)
    {
        const string sql = SelectLibro + @"
            WHERE l.Activo = 1
              AND (l.Titulo LIKE @Criterio OR a.Nombre LIKE @Criterio)
            ORDER BY l.Titulo";

        var parametros = new[]
        {
            new SqlParameter("@Criterio", SqlDbType.NVarChar, 200) { Value = "%" + criterio + "%" }
        };

        return ConsultarListaAsync(sql, parametros);
    }

    public Task<List<Libro>> ListarDisponiblesAsync()
    {
        const string sql = SelectLibro + @"
            WHERE l.Activo = 1 AND l.Ejemplares > 0
            ORDER BY l.Titulo";

        return ConsultarListaAsync(sql, null);
    }

    public async Task<Libro?> ObtenerPorIdAsync(int libroId)
    {
        const string sql = SelectLibro + @"
            WHERE l.LibroId = @LibroId";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@LibroId", SqlDbType.Int) { Value = libroId });
        await cn.OpenAsync();
        await using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync())
        {
            return null;
        }

        return Map(rd);
    }

    public async Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Libros
            WHERE ISBN = @ISBN AND LibroId <> @ExcluirLibroId";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@ISBN", SqlDbType.NVarChar, 20) { Value = isbn });
        cmd.Parameters.Add(new SqlParameter("@ExcluirLibroId", SqlDbType.Int) { Value = excluirLibroId });
        await cn.OpenAsync();
        var total = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return total > 0;
    }

    public async Task<bool> TienePrestamosPendientesAsync(int libroId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM DetallePrestamo
            WHERE LibroId = @LibroId AND FechaDevolucion IS NULL";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@LibroId", SqlDbType.Int) { Value = libroId });
        await cn.OpenAsync();
        var total = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return total > 0;
    }

    public async Task<int> InsertarAsync(Libro libro)
    {
        const string sql = @"
            INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares, Activo)
            VALUES (@Titulo, @ISBN, @AutorId, @Ejemplares, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, libro);
        await cn.OpenAsync();
        return (int)(await cmd.ExecuteScalarAsync() ?? 0);
    }

    public async Task ActualizarAsync(Libro libro)
    {
        const string sql = @"
            UPDATE Libros
            SET Titulo = @Titulo,
                ISBN = @ISBN,
                AutorId = @AutorId,
                Ejemplares = @Ejemplares
            WHERE LibroId = @LibroId AND Activo = 1";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, libro);
        cmd.Parameters.Add(new SqlParameter("@LibroId", SqlDbType.Int) { Value = libro.LibroId });
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EliminarLogicoAsync(int libroId)
    {
        const string sql = @"
            UPDATE Libros
            SET Activo = 0
            WHERE LibroId = @LibroId";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@LibroId", SqlDbType.Int) { Value = libroId });
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    internal async Task DescontarEjemplarAsync(int libroId, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"
            UPDATE Libros
            SET Ejemplares = Ejemplares - 1
            WHERE LibroId = @LibroId AND Activo = 1 AND Ejemplares > 0";

        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.Add(new SqlParameter("@LibroId", SqlDbType.Int) { Value = libroId });
        var filas = await cmd.ExecuteNonQueryAsync();
        if (filas == 0)
        {
            throw new InvalidOperationException(
                "No se pudo descontar el ejemplar. El libro no tiene stock o está inactivo.");
        }
    }

    internal async Task DevolverEjemplarAsync(int libroId, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"
            UPDATE Libros
            SET Ejemplares = Ejemplares + 1
            WHERE LibroId = @LibroId";

        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.Add(new SqlParameter("@LibroId", SqlDbType.Int) { Value = libroId });
        await cmd.ExecuteNonQueryAsync();
    }

    private static void AgregarParametros(SqlCommand cmd, Libro libro)
    {
        cmd.Parameters.Add(new SqlParameter("@Titulo", SqlDbType.NVarChar, 200) { Value = libro.Titulo });
        cmd.Parameters.Add(new SqlParameter("@ISBN", SqlDbType.NVarChar, 20) { Value = libro.ISBN });
        cmd.Parameters.Add(new SqlParameter("@AutorId", SqlDbType.Int) { Value = libro.AutorId });
        cmd.Parameters.Add(new SqlParameter("@Ejemplares", SqlDbType.Int) { Value = libro.Ejemplares });
    }

    private static async Task<List<Libro>> ConsultarListaAsync(string sql, SqlParameter[]? parametros)
    {
        var lista = new List<Libro>();
        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        if (parametros != null)
        {
            cmd.Parameters.AddRange(parametros);
        }

        await cn.OpenAsync();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            lista.Add(Map(rd));
        }

        return lista;
    }

    private static Libro Map(SqlDataReader rd)
    {
        return new Libro
        {
            LibroId = rd.GetInt32(rd.GetOrdinal("LibroId")),
            Titulo = rd.GetString(rd.GetOrdinal("Titulo")),
            ISBN = rd.GetString(rd.GetOrdinal("ISBN")),
            AutorId = rd.GetInt32(rd.GetOrdinal("AutorId")),
            AutorNombre = rd.GetString(rd.GetOrdinal("AutorNombre")),
            Ejemplares = rd.GetInt32(rd.GetOrdinal("Ejemplares")),
            Activo = rd.GetBoolean(rd.GetOrdinal("Activo"))
        };
    }
}

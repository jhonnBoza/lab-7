using System.Data;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class DetallePrestamoDatos
{
    public async Task<List<DetallePrestamo>> ListarPorPrestamoAsync(int prestamoId)
    {
        const string sql = @"
            SELECT d.PrestamoId, d.LibroId, l.Titulo AS LibroTitulo, d.FechaDevolucion
            FROM DetallePrestamo d
            INNER JOIN Libros l ON l.LibroId = d.LibroId
            WHERE d.PrestamoId = @PrestamoId
            ORDER BY l.Titulo";

        var lista = new List<DetallePrestamo>();
        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@PrestamoId", SqlDbType.Int) { Value = prestamoId });
        await cn.OpenAsync();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            lista.Add(Map(rd));
        }

        return lista;
    }

    public async Task<List<PrestamoPendiente>> ListarPendientesAsync()
    {
        const string sql = @"
            SELECT p.PrestamoId, d.LibroId, p.SocioId, s.Nombre AS Socio, l.Titulo AS Libro,
                   p.FechaPrestamo, p.FechaLimite, p.Estado
            FROM Prestamos p
            INNER JOIN DetallePrestamo d ON d.PrestamoId = p.PrestamoId
            INNER JOIN Socios s ON s.SocioId = p.SocioId
            INNER JOIN Libros l ON l.LibroId = d.LibroId
            WHERE d.FechaDevolucion IS NULL
            ORDER BY p.FechaLimite, s.Nombre, l.Titulo";

        var lista = new List<PrestamoPendiente>();
        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            lista.Add(new PrestamoPendiente
            {
                PrestamoId = rd.GetInt32(rd.GetOrdinal("PrestamoId")),
                LibroId = rd.GetInt32(rd.GetOrdinal("LibroId")),
                SocioId = rd.GetInt32(rd.GetOrdinal("SocioId")),
                Socio = rd.GetString(rd.GetOrdinal("Socio")),
                Libro = rd.GetString(rd.GetOrdinal("Libro")),
                FechaPrestamo = rd.GetDateTime(rd.GetOrdinal("FechaPrestamo")),
                FechaLimite = rd.GetDateTime(rd.GetOrdinal("FechaLimite")),
                Estado = rd.GetString(rd.GetOrdinal("Estado"))
            });
        }

        return lista;
    }

    internal async Task InsertarAsync(DetallePrestamo detalle, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"
            INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion)
            VALUES (@PrestamoId, @LibroId, NULL)";

        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.Add(new SqlParameter("@PrestamoId", SqlDbType.Int) { Value = detalle.PrestamoId });
        cmd.Parameters.Add(new SqlParameter("@LibroId", SqlDbType.Int) { Value = detalle.LibroId });
        await cmd.ExecuteNonQueryAsync();
    }

    internal async Task<int> MarcarDevueltoAsync(int prestamoId, int libroId, DateTime fechaDevolucion, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"
            UPDATE DetallePrestamo
            SET FechaDevolucion = @FechaDevolucion
            WHERE PrestamoId = @PrestamoId
              AND LibroId = @LibroId
              AND FechaDevolucion IS NULL";

        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.Add(new SqlParameter("@FechaDevolucion", SqlDbType.DateTime) { Value = fechaDevolucion });
        cmd.Parameters.Add(new SqlParameter("@PrestamoId", SqlDbType.Int) { Value = prestamoId });
        cmd.Parameters.Add(new SqlParameter("@LibroId", SqlDbType.Int) { Value = libroId });
        return await cmd.ExecuteNonQueryAsync();
    }

    private static DetallePrestamo Map(SqlDataReader rd)
    {
        var ordinalFecha = rd.GetOrdinal("FechaDevolucion");
        return new DetallePrestamo
        {
            PrestamoId = rd.GetInt32(rd.GetOrdinal("PrestamoId")),
            LibroId = rd.GetInt32(rd.GetOrdinal("LibroId")),
            LibroTitulo = rd.GetString(rd.GetOrdinal("LibroTitulo")),
            FechaDevolucion = rd.IsDBNull(ordinalFecha) ? null : rd.GetDateTime(ordinalFecha)
        };
    }
}

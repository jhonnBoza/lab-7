using System.Data;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class PrestamoDatos
{
    private readonly DetallePrestamoDatos _detalles = new();
    private readonly LibroDatos _libros = new();

    public async Task<int> ContarAsync()
    {
        const string sql = "SELECT COUNT(1) FROM Prestamos";
        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        return (int)(await cmd.ExecuteScalarAsync() ?? 0);
    }

    public async Task<Prestamo?> ObtenerPorIdAsync(int prestamoId)
    {
        const string sql = @"
            SELECT p.PrestamoId, p.SocioId, s.Nombre AS SocioNombre,
                   p.FechaPrestamo, p.FechaLimite, p.Estado
            FROM Prestamos p
            INNER JOIN Socios s ON s.SocioId = p.SocioId
            WHERE p.PrestamoId = @PrestamoId";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@PrestamoId", SqlDbType.Int) { Value = prestamoId });
        await cn.OpenAsync();
        await using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync())
        {
            return null;
        }

        var prestamo = new Prestamo
        {
            PrestamoId = rd.GetInt32(rd.GetOrdinal("PrestamoId")),
            SocioId = rd.GetInt32(rd.GetOrdinal("SocioId")),
            SocioNombre = rd.GetString(rd.GetOrdinal("SocioNombre")),
            FechaPrestamo = rd.GetDateTime(rd.GetOrdinal("FechaPrestamo")),
            FechaLimite = rd.GetDateTime(rd.GetOrdinal("FechaLimite")),
            Estado = rd.GetString(rd.GetOrdinal("Estado"))
        };

        await rd.CloseAsync();
        prestamo.Detalles = await _detalles.ListarPorPrestamoAsync(prestamoId);
        return prestamo;
    }

    public async Task<List<ReportePrestamo>> ListarReporteAsync(DateTime desde, DateTime hasta)
    {
        const string sql = @"
            SELECT p.PrestamoId, s.Nombre AS Socio, l.Titulo AS Libro,
                   p.FechaPrestamo, p.FechaLimite, p.Estado, d.FechaDevolucion
            FROM Prestamos p
            INNER JOIN DetallePrestamo d ON d.PrestamoId = p.PrestamoId
            INNER JOIN Libros l ON l.LibroId = d.LibroId
            INNER JOIN Socios s ON s.SocioId = p.SocioId
            WHERE p.FechaPrestamo >= @Desde
              AND p.FechaPrestamo < DATEADD(DAY, 1, @Hasta)
            ORDER BY p.FechaPrestamo, p.PrestamoId, l.Titulo";

        var lista = new List<ReportePrestamo>();
        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@Desde", SqlDbType.DateTime) { Value = desde.Date });
        cmd.Parameters.Add(new SqlParameter("@Hasta", SqlDbType.DateTime) { Value = hasta.Date });
        await cn.OpenAsync();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            var ordinalFecha = rd.GetOrdinal("FechaDevolucion");
            lista.Add(new ReportePrestamo
            {
                PrestamoId = rd.GetInt32(rd.GetOrdinal("PrestamoId")),
                Socio = rd.GetString(rd.GetOrdinal("Socio")),
                Libro = rd.GetString(rd.GetOrdinal("Libro")),
                FechaPrestamo = rd.GetDateTime(rd.GetOrdinal("FechaPrestamo")),
                FechaLimite = rd.GetDateTime(rd.GetOrdinal("FechaLimite")),
                Estado = rd.GetString(rd.GetOrdinal("Estado")),
                FechaDevolucion = rd.IsDBNull(ordinalFecha) ? null : rd.GetDateTime(ordinalFecha)
            });
        }

        return lista;
    }

    public async Task<int> RegistrarAsync(Prestamo prestamo)
    {
        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await cn.OpenAsync();
        SqlTransaction? tx = null;
        try
        {
            tx = (SqlTransaction)await cn.BeginTransactionAsync();
            var prestamoId = await InsertarCabeceraAsync(prestamo, cn, tx);
            foreach (var detalle in prestamo.Detalles)
            {
                detalle.PrestamoId = prestamoId;
                await _libros.DescontarEjemplarAsync(detalle.LibroId, cn, tx);
                await _detalles.InsertarAsync(detalle, cn, tx);
            }

            await tx.CommitAsync();
            return prestamoId;
        }
        catch
        {
            await RevertirAsync(tx);
            throw;
        }
        finally
        {
            if (tx != null)
            {
                await tx.DisposeAsync();
            }
        }
    }

    public async Task RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion, string nuevoEstado)
    {
        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await cn.OpenAsync();
        SqlTransaction? tx = null;
        try
        {
            tx = (SqlTransaction)await cn.BeginTransactionAsync();
            var filas = await _detalles.MarcarDevueltoAsync(prestamoId, libroId, fechaDevolucion, cn, tx);
            if (filas == 0)
            {
                throw new InvalidOperationException("El libro de este préstamo ya fue devuelto o no existe.");
            }

            await _libros.DevolverEjemplarAsync(libroId, cn, tx);
            await ActualizarEstadoAsync(prestamoId, nuevoEstado, cn, tx);
            await tx.CommitAsync();
        }
        catch
        {
            await RevertirAsync(tx);
            throw;
        }
        finally
        {
            if (tx != null)
            {
                await tx.DisposeAsync();
            }
        }
    }

    private static async Task<int> InsertarCabeceraAsync(Prestamo prestamo, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"
            INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
            VALUES (@SocioId, @FechaPrestamo, @FechaLimite, @Estado);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.Add(new SqlParameter("@SocioId", SqlDbType.Int) { Value = prestamo.SocioId });
        cmd.Parameters.Add(new SqlParameter("@FechaPrestamo", SqlDbType.DateTime) { Value = prestamo.FechaPrestamo });
        cmd.Parameters.Add(new SqlParameter("@FechaLimite", SqlDbType.DateTime) { Value = prestamo.FechaLimite });
        cmd.Parameters.Add(new SqlParameter("@Estado", SqlDbType.NVarChar, 20) { Value = prestamo.Estado });
        return (int)(await cmd.ExecuteScalarAsync() ?? 0);
    }

    private static async Task ActualizarEstadoAsync(int prestamoId, string estado, SqlConnection cn, SqlTransaction tx)
    {
        const string sql = @"
            UPDATE Prestamos
            SET Estado = @Estado
            WHERE PrestamoId = @PrestamoId";

        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.Add(new SqlParameter("@Estado", SqlDbType.NVarChar, 20) { Value = estado });
        cmd.Parameters.Add(new SqlParameter("@PrestamoId", SqlDbType.Int) { Value = prestamoId });
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task RevertirAsync(SqlTransaction? tx)
    {
        if (tx == null)
        {
            return;
        }

        try
        {
            await tx.RollbackAsync();
        }
        catch (InvalidOperationException)
        {
        }
    }
}

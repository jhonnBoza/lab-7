using System.Data;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class SocioDatos
{
    public Task<List<Socio>> ListarActivosAsync()
    {
        const string sql = @"
            SELECT SocioId, DNI, Nombre, Email, Activo
            FROM Socios
            WHERE Activo = 1
            ORDER BY Nombre";

        return ConsultarListaAsync(sql, null);
    }

    public Task<List<Socio>> BuscarAsync(string criterio)
    {
        const string sql = @"
            SELECT SocioId, DNI, Nombre, Email, Activo
            FROM Socios
            WHERE Activo = 1
              AND (Nombre LIKE @Criterio OR DNI LIKE @Criterio)
            ORDER BY Nombre";

        var parametros = new[]
        {
            new SqlParameter("@Criterio", SqlDbType.NVarChar, 120) { Value = "%" + criterio + "%" }
        };

        return ConsultarListaAsync(sql, parametros);
    }

    public async Task<Socio?> ObtenerPorIdAsync(int socioId)
    {
        const string sql = @"
            SELECT SocioId, DNI, Nombre, Email, Activo
            FROM Socios
            WHERE SocioId = @SocioId";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@SocioId", SqlDbType.Int) { Value = socioId });
        await cn.OpenAsync();
        await using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync())
        {
            return null;
        }

        return Map(rd);
    }

    public async Task<bool> ExisteDniAsync(string dni, int excluirSocioId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Socios
            WHERE DNI = @DNI AND SocioId <> @ExcluirSocioId";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@DNI", SqlDbType.NVarChar, 15) { Value = dni });
        cmd.Parameters.Add(new SqlParameter("@ExcluirSocioId", SqlDbType.Int) { Value = excluirSocioId });
        await cn.OpenAsync();
        var total = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return total > 0;
    }

    public async Task<bool> TienePrestamosPendientesAsync(int socioId)
    {
        var total = await ContarLibrosPendientesAsync(socioId);
        return total > 0;
    }

    public async Task<int> ContarLibrosPendientesAsync(int socioId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM DetallePrestamo d
            INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId
            WHERE p.SocioId = @SocioId AND d.FechaDevolucion IS NULL";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@SocioId", SqlDbType.Int) { Value = socioId });
        await cn.OpenAsync();
        return (int)(await cmd.ExecuteScalarAsync() ?? 0);
    }

    public async Task<int> InsertarAsync(Socio socio)
    {
        const string sql = @"
            INSERT INTO Socios (DNI, Nombre, Email, Activo)
            VALUES (@DNI, @Nombre, @Email, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, socio);
        await cn.OpenAsync();
        return (int)(await cmd.ExecuteScalarAsync() ?? 0);
    }

    public async Task ActualizarAsync(Socio socio)
    {
        const string sql = @"
            UPDATE Socios
            SET DNI = @DNI,
                Nombre = @Nombre,
                Email = @Email
            WHERE SocioId = @SocioId AND Activo = 1";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, socio);
        cmd.Parameters.Add(new SqlParameter("@SocioId", SqlDbType.Int) { Value = socio.SocioId });
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EliminarLogicoAsync(int socioId)
    {
        const string sql = @"
            UPDATE Socios
            SET Activo = 0
            WHERE SocioId = @SocioId";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@SocioId", SqlDbType.Int) { Value = socioId });
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    private static void AgregarParametros(SqlCommand cmd, Socio socio)
    {
        cmd.Parameters.Add(new SqlParameter("@DNI", SqlDbType.NVarChar, 15) { Value = socio.DNI });
        cmd.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 120) { Value = socio.Nombre });
        cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 120) { Value = socio.Email });
    }

    private static async Task<List<Socio>> ConsultarListaAsync(string sql, SqlParameter[]? parametros)
    {
        var lista = new List<Socio>();
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

    private static Socio Map(SqlDataReader rd)
    {
        return new Socio
        {
            SocioId = rd.GetInt32(rd.GetOrdinal("SocioId")),
            DNI = rd.GetString(rd.GetOrdinal("DNI")),
            Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
            Email = rd.GetString(rd.GetOrdinal("Email")),
            Activo = rd.GetBoolean(rd.GetOrdinal("Activo"))
        };
    }
}

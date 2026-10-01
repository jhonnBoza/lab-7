using System.Data;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

public class AutorDatos
{
    public async Task<List<Autor>> ListarActivosAsync()
    {
        const string sql = @"
            SELECT AutorId, Nombre, Nacionalidad, Activo
            FROM Autores
            WHERE Activo = 1
            ORDER BY Nombre";

        var lista = new List<Autor>();
        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            lista.Add(Map(rd));
        }

        return lista;
    }

    public async Task<Autor?> ObtenerPorIdAsync(int autorId)
    {
        const string sql = @"
            SELECT AutorId, Nombre, Nacionalidad, Activo
            FROM Autores
            WHERE AutorId = @AutorId";

        await using var cn = new SqlConnection(Conexion.ObtenerCadena());
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add(new SqlParameter("@AutorId", SqlDbType.Int) { Value = autorId });
        await cn.OpenAsync();
        await using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync())
        {
            return null;
        }

        return Map(rd);
    }

    private static Autor Map(SqlDataReader rd)
    {
        return new Autor
        {
            AutorId = rd.GetInt32(rd.GetOrdinal("AutorId")),
            Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
            Nacionalidad = rd.GetString(rd.GetOrdinal("Nacionalidad")),
            Activo = rd.GetBoolean(rd.GetOrdinal("Activo"))
        };
    }
}

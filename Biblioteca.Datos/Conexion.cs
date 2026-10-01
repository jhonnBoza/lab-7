using System.Configuration;

namespace Biblioteca.Datos;

internal static class Conexion
{
    public const string Nombre = "BibliotecaDB";

    public static string ObtenerCadena()
    {
        var cadena = ConfigurationManager.ConnectionStrings[Nombre];
        if (cadena == null || string.IsNullOrWhiteSpace(cadena.ConnectionString))
        {
            throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'BibliotecaDB' en el App.config del proyecto de inicio.");
        }

        return cadena.ConnectionString;
    }
}

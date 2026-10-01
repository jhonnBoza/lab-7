using System.Windows;

namespace Biblioteca.WPF;

internal static class Dialogos
{
    public static void Error(Exception ex)
    {
        var esRegla = ex is Negocio.ReglaNegocioException;
        MessageBox.Show(
            ex.Message,
            esRegla ? "Regla de negocio" : "Error",
            MessageBoxButton.OK,
            esRegla ? MessageBoxImage.Warning : MessageBoxImage.Error);
    }

    public static void Info(string mensaje)
    {
        MessageBox.Show(mensaje, "Biblioteca", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static bool Confirmar(string mensaje)
    {
        return MessageBox.Show(mensaje, "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}

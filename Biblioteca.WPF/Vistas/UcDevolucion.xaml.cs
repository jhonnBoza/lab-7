using System.Windows;
using System.Windows.Controls;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.Vistas;

public class FilaDevolucion
{
    public int PrestamoId { get; init; }

    public int LibroId { get; init; }

    public string Socio { get; init; } = string.Empty;

    public string Libro { get; init; } = string.Empty;

    public DateTime FechaLimite { get; init; }

    public int DiasRetraso { get; init; }

    public decimal Multa { get; init; }
}

public partial class UcDevolucion : UserControl
{
    private readonly PrestamoNegocio _negocio = new();

    public UcDevolucion()
    {
        InitializeComponent();
        txtAyuda.Text = $"La multa es de S/ {PrestamoNegocio.MultaPorDia:N2} por día de retraso y se calcula en la capa de negocio. Si el préstamo no tiene más libros pendientes, pasa a Devuelto.";
    }

    private async void Uc_Loaded(object sender, RoutedEventArgs e) => await CargarAsync();

    private void DgPendientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgPendientes.SelectedItem is not FilaDevolucion fila)
        {
            txtDetalle.Text = "Seleccione un libro pendiente.";
            txtMulta.Text = string.Empty;
            return;
        }

        txtDetalle.Text = $"{fila.Socio} — {fila.Libro}. Fecha límite: {fila.FechaLimite:dd/MM/yyyy}.";
        txtMulta.Text = fila.DiasRetraso == 0
            ? "Sin retraso. Multa: S/ 0.00"
            : $"Retraso: {fila.DiasRetraso} día(s). Multa: S/ {fila.Multa:N2}";
    }

    private async void BtnDevolver_Click(object sender, RoutedEventArgs e)
    {
        if (dgPendientes.SelectedItem is not FilaDevolucion fila)
        {
            Dialogos.Info("Seleccione un libro pendiente.");
            return;
        }

        try
        {
            btnDevolver.IsEnabled = false;
            var resultado = await _negocio.RegistrarDevolucionAsync(fila.PrestamoId, fila.LibroId);
            await CargarAsync();
            var mensaje = resultado.DiasRetraso == 0
                ? "Devolución registrada. No hay multa."
                : $"Devolución registrada. Retraso: {resultado.DiasRetraso} día(s). Multa: S/ {resultado.Multa:N2}.";
            if (resultado.PrestamoCerrado)
            {
                mensaje += " El préstamo quedó en estado Devuelto.";
            }

            txtEstado.Text = mensaje;
            txtDetalle.Text = mensaje;
            txtMulta.Text = $"S/ {resultado.Multa:N2}";
            Dialogos.Info(mensaje);
        }
        catch (Exception ex)
        {
            Dialogos.Error(ex);
        }
        finally
        {
            btnDevolver.IsEnabled = true;
        }
    }

    private async Task CargarAsync()
    {
        try
        {
            var hoy = DateTime.Today;
            var pendientes = await _negocio.ListarPendientesAsync();
            dgPendientes.ItemsSource = pendientes.Select(p => new FilaDevolucion
            {
                PrestamoId = p.PrestamoId,
                LibroId = p.LibroId,
                Socio = p.Socio,
                Libro = p.Libro,
                FechaLimite = p.FechaLimite,
                DiasRetraso = _negocio.CalcularDiasRetraso(p.FechaLimite, hoy),
                Multa = _negocio.CalcularMulta(p.FechaLimite, hoy)
            }).ToList();
            txtEstado.Text = $"{dgPendientes.Items.Count} libro(s) pendiente(s).";
            txtDetalle.Text = "Seleccione un libro pendiente.";
            txtMulta.Text = string.Empty;
        }
        catch (Exception ex)
        {
            Dialogos.Error(ex);
            txtEstado.Text = "No se pudieron cargar las devoluciones.";
        }
    }
}

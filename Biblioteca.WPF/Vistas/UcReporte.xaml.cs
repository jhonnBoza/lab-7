using System.Windows;
using System.Windows.Controls;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.Vistas;

public partial class UcReporte : UserControl
{
    private readonly PrestamoNegocio _negocio = new();

    public UcReporte()
    {
        InitializeComponent();
        dpDesde.SelectedDate = DateTime.Today.AddDays(-30);
        dpHasta.SelectedDate = DateTime.Today;
    }

    private async void Uc_Loaded(object sender, RoutedEventArgs e) => await ConsultarAsync();

    private async void BtnConsultar_Click(object sender, RoutedEventArgs e) => await ConsultarAsync();

    private async Task ConsultarAsync()
    {
        if (dpDesde.SelectedDate is not DateTime desde || dpHasta.SelectedDate is not DateTime hasta)
        {
            Dialogos.Info("Seleccione el intervalo de fechas.");
            return;
        }

        try
        {
            dgReporte.ItemsSource = await _negocio.ReporteAsync(desde, hasta);
            txtEstado.Text = $"{dgReporte.Items.Count} fila(s) en el reporte.";
        }
        catch (Exception ex)
        {
            Dialogos.Error(ex);
            txtEstado.Text = "No se pudo generar el reporte.";
        }
    }
}

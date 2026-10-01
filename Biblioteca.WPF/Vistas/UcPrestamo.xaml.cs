using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.Vistas;

public partial class UcPrestamo : UserControl
{
    private readonly PrestamoNegocio _prestamos = new();
    private readonly SocioNegocio _socios = new();
    private readonly ObservableCollection<Libro> _carrito = new();

    public UcPrestamo()
    {
        InitializeComponent();
        lstLibros.ItemsSource = _carrito;
        txtAyuda.Text = $"Un socio puede tener como máximo {PrestamoNegocio.MaxLibrosPendientes} libros pendientes. El préstamo vence a los {PrestamoNegocio.DiasDePrestamo} días.";
    }

    private async void Uc_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            cboSocio.ItemsSource = await _socios.ListarAsync();
            await CargarLibrosAsync();
            txtEstado.Text = "Seleccione un socio y agregue uno o más libros.";
        }
        catch (Exception ex)
        {
            Dialogos.Error(ex);
        }
    }

    private async void CboSocio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboSocio.SelectedItem is not Socio socio)
        {
            txtCupo.Text = "Seleccione un socio.";
            return;
        }

        try
        {
            var pendientes = await _socios.ContarLibrosPendientesAsync(socio.SocioId);
            txtCupo.Text = $"{socio.Nombre} tiene {pendientes} de {PrestamoNegocio.MaxLibrosPendientes} libros pendientes.";
        }
        catch (Exception ex)
        {
            Dialogos.Error(ex);
        }
    }

    private void BtnAgregar_Click(object sender, RoutedEventArgs e)
    {
        if (cboLibro.SelectedItem is not Libro libro)
        {
            Dialogos.Info("Seleccione un libro.");
            return;
        }

        if (_carrito.Any(x => x.LibroId == libro.LibroId))
        {
            Dialogos.Info("Ese libro ya está en el préstamo.");
            return;
        }

        _carrito.Add(libro);
        txtEstado.Text = $"{_carrito.Count} libro(s) en este préstamo.";
    }

    private void BtnQuitar_Click(object sender, RoutedEventArgs e)
    {
        if (lstLibros.SelectedItem is Libro libro)
        {
            _carrito.Remove(libro);
            txtEstado.Text = $"{_carrito.Count} libro(s) en este préstamo.";
        }
    }

    private async void BtnRegistrar_Click(object sender, RoutedEventArgs e)
    {
        var socioId = cboSocio.SelectedItem is Socio socio ? socio.SocioId : 0;
        try
        {
            btnRegistrar.IsEnabled = false;
            var prestamo = await _prestamos.RegistrarAsync(socioId, _carrito.Select(x => x.LibroId).ToList());
            _carrito.Clear();
            await CargarLibrosAsync();
            if (cboSocio.SelectedItem is Socio seleccionado)
            {
                var pendientes = await _socios.ContarLibrosPendientesAsync(seleccionado.SocioId);
                txtCupo.Text = $"{seleccionado.Nombre} tiene {pendientes} de {PrestamoNegocio.MaxLibrosPendientes} libros pendientes.";
            }

            txtEstado.Text = $"Préstamo {prestamo.PrestamoId} registrado.";
            Dialogos.Info($"Préstamo {prestamo.PrestamoId} registrado. Fecha límite: {prestamo.FechaLimite:dd/MM/yyyy}.");
        }
        catch (Exception ex)
        {
            Dialogos.Error(ex);
        }
        finally
        {
            btnRegistrar.IsEnabled = true;
        }
    }

    private async Task CargarLibrosAsync()
    {
        cboLibro.ItemsSource = await _prestamos.ListarLibrosDisponiblesAsync();
    }
}

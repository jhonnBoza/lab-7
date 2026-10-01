using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.Vistas;

public partial class UcSocios : UserControl
{
    private readonly SocioNegocio _negocio = new();
    private int _socioId;
    private bool _cargando;

    public UcSocios()
    {
        InitializeComponent();
    }

    private async void Uc_Loaded(object sender, RoutedEventArgs e) => await BuscarAsync();

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e) => await BuscarAsync();

    private async void BtnTodos_Click(object sender, RoutedEventArgs e)
    {
        txtBuscar.Text = string.Empty;
        await BuscarAsync();
    }

    private async void TxtBuscar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await BuscarAsync();
        }
    }

    private void DgSocios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando || dgSocios.SelectedItem is not Socio socio)
        {
            return;
        }

        _socioId = socio.SocioId;
        txtDni.Text = socio.DNI;
        txtNombre.Text = socio.Nombre;
        txtEmail.Text = socio.Email;
    }

    private void BtnNuevo_Click(object sender, RoutedEventArgs e) => Limpiar();

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        var socio = new Socio
        {
            SocioId = _socioId,
            DNI = txtDni.Text,
            Nombre = txtNombre.Text,
            Email = txtEmail.Text
        };

        try
        {
            Ocupado(true);
            var nuevo = _socioId == 0;
            if (nuevo)
            {
                await _negocio.InsertarAsync(socio);
            }
            else
            {
                await _negocio.ActualizarAsync(socio);
            }

            await BuscarAsync();
            Limpiar();
            Dialogos.Info(nuevo ? "Socio registrado." : "Socio actualizado.");
        }
        catch (Exception ex)
        {
            Dialogos.Error(ex);
        }
        finally
        {
            Ocupado(false);
        }
    }

    private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
    {
        if (_socioId == 0)
        {
            Dialogos.Info("Seleccione un socio para darlo de baja.");
            return;
        }

        if (!Dialogos.Confirmar("¿Dar de baja este socio? No se elimina de la base de datos."))
        {
            return;
        }

        try
        {
            Ocupado(true);
            await _negocio.EliminarAsync(_socioId);
            await BuscarAsync();
            Limpiar();
            Dialogos.Info("Socio dado de baja.");
        }
        catch (Exception ex)
        {
            Dialogos.Error(ex);
        }
        finally
        {
            Ocupado(false);
        }
    }

    private async Task BuscarAsync()
    {
        try
        {
            Ocupado(true);
            _cargando = true;
            dgSocios.ItemsSource = await _negocio.BuscarAsync(txtBuscar.Text);
            txtEstado.Text = $"{dgSocios.Items.Count} socio(s).";
        }
        catch (Exception ex)
        {
            Dialogos.Error(ex);
            txtEstado.Text = "No se pudieron cargar los socios.";
        }
        finally
        {
            _cargando = false;
            Ocupado(false);
        }
    }

    private void Limpiar()
    {
        _socioId = 0;
        txtDni.Text = string.Empty;
        txtNombre.Text = string.Empty;
        txtEmail.Text = string.Empty;
        dgSocios.SelectedItem = null;
    }

    private void Ocupado(bool ocupado)
    {
        btnGuardar.IsEnabled = !ocupado;
        btnEliminar.IsEnabled = !ocupado;
        btnNuevo.IsEnabled = !ocupado;
    }
}

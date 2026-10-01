using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF.Vistas;

public partial class UcLibros : UserControl
{
    private readonly LibroNegocio _negocio = new();
    private int _libroId;
    private bool _cargando;

    public UcLibros()
    {
        InitializeComponent();
    }

    private async void Uc_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await CargarAutoresAsync();
            await BuscarAsync();
        }
        catch (Exception ex)
        {
            Dialogos.Error(ex);
        }
    }

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

    private void DgLibros_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando || dgLibros.SelectedItem is not Libro libro)
        {
            return;
        }

        _libroId = libro.LibroId;
        txtTitulo.Text = libro.Titulo;
        txtIsbn.Text = libro.ISBN;
        txtEjemplares.Text = libro.Ejemplares.ToString();
        cboAutor.SelectedValue = libro.AutorId;
    }

    private void BtnNuevo_Click(object sender, RoutedEventArgs e) => Limpiar();

    private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtEjemplares.Text.Trim(), out var ejemplares))
        {
            Dialogos.Info("Ingrese una cantidad válida de ejemplares.");
            return;
        }

        var libro = new Libro
        {
            LibroId = _libroId,
            Titulo = txtTitulo.Text,
            ISBN = txtIsbn.Text,
            Ejemplares = ejemplares,
            AutorId = cboAutor.SelectedValue is int autorId ? autorId : 0
        };

        try
        {
            Ocupado(true);
            var nuevo = _libroId == 0;
            if (nuevo)
            {
                await _negocio.InsertarAsync(libro);
            }
            else
            {
                await _negocio.ActualizarAsync(libro);
            }

            await BuscarAsync();
            Limpiar();
            Dialogos.Info(nuevo ? "Libro registrado." : "Libro actualizado.");
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
        if (_libroId == 0)
        {
            Dialogos.Info("Seleccione un libro para darlo de baja.");
            return;
        }

        if (!Dialogos.Confirmar("¿Dar de baja este libro? No se elimina de la base de datos."))
        {
            return;
        }

        try
        {
            Ocupado(true);
            await _negocio.EliminarAsync(_libroId);
            await BuscarAsync();
            Limpiar();
            Dialogos.Info("Libro dado de baja.");
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

    private async Task CargarAutoresAsync()
    {
        cboAutor.ItemsSource = await _negocio.ListarAutoresAsync();
    }

    private async Task BuscarAsync()
    {
        try
        {
            Ocupado(true);
            _cargando = true;
            dgLibros.ItemsSource = await _negocio.BuscarAsync(txtBuscar.Text);
            txtEstado.Text = $"{dgLibros.Items.Count} libro(s).";
        }
        catch (Exception ex)
        {
            Dialogos.Error(ex);
            txtEstado.Text = "No se pudieron cargar los libros.";
        }
        finally
        {
            _cargando = false;
            Ocupado(false);
        }
    }

    private void Limpiar()
    {
        _libroId = 0;
        txtTitulo.Text = string.Empty;
        txtIsbn.Text = string.Empty;
        txtEjemplares.Text = string.Empty;
        cboAutor.SelectedIndex = -1;
        dgLibros.SelectedItem = null;
    }

    private void Ocupado(bool ocupado)
    {
        btnGuardar.IsEnabled = !ocupado;
        btnEliminar.IsEnabled = !ocupado;
        btnNuevo.IsEnabled = !ocupado;
    }
}

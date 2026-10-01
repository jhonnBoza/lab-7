using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

public class ResultadoDevolucion
{
    public decimal Multa { get; set; }

    public int DiasRetraso { get; set; }

    public bool PrestamoCerrado { get; set; }

    public DateTime FechaDevolucion { get; set; }
}

public class PrestamoNegocio
{
    public const int MaxLibrosPendientes = 3;
    public const int DiasDePrestamo = 7;
    public const decimal MultaPorDia = 1.50m;
    public const string EstadoPendiente = "Pendiente";
    public const string EstadoDevuelto = "Devuelto";

    private readonly PrestamoDatos _prestamos = new();
    private readonly DetallePrestamoDatos _detalles = new();
    private readonly SocioDatos _socios = new();
    private readonly LibroDatos _libros = new();

    public Task<List<Libro>> ListarLibrosDisponiblesAsync() => _libros.ListarDisponiblesAsync();

    public Task<List<PrestamoPendiente>> ListarPendientesAsync() => _detalles.ListarPendientesAsync();

    public Task<int> ContarPrestamosAsync() => _prestamos.ContarAsync();

    public int CalcularDiasRetraso(DateTime fechaLimite, DateTime fechaDevolucion)
    {
        var dias = (fechaDevolucion.Date - fechaLimite.Date).Days;
        return dias > 0 ? dias : 0;
    }

    public decimal CalcularMulta(DateTime fechaLimite, DateTime fechaDevolucion)
    {
        return CalcularDiasRetraso(fechaLimite, fechaDevolucion) * MultaPorDia;
    }

    public async Task<Prestamo> RegistrarAsync(int socioId, IReadOnlyCollection<int> libroIds)
    {
        if (socioId <= 0)
        {
            throw new ReglaNegocioException("Seleccione un socio.");
        }

        if (libroIds == null || libroIds.Count == 0)
        {
            throw new ReglaNegocioException("Agregue al menos un libro al préstamo.");
        }

        if (libroIds.Distinct().Count() != libroIds.Count)
        {
            throw new ReglaNegocioException("No puede incluir el mismo libro dos veces en el mismo préstamo.");
        }

        var socio = await _socios.ObtenerPorIdAsync(socioId)
            ?? throw new ReglaNegocioException("El socio no existe.");

        if (!socio.Activo)
        {
            throw new ReglaNegocioException("No se puede prestar libros a un socio inactivo.");
        }

        var pendientes = await _socios.ContarLibrosPendientesAsync(socioId);
        if (pendientes + libroIds.Count > MaxLibrosPendientes)
        {
            var cupo = MaxLibrosPendientes - pendientes;
            throw new ReglaNegocioException(
                cupo <= 0
                    ? $"El socio ya tiene {pendientes} libros pendientes. El máximo es {MaxLibrosPendientes}."
                    : $"El socio tiene {pendientes} libros pendientes y solo puede llevar {cupo} más. El máximo es {MaxLibrosPendientes}.");
        }

        var detalles = new List<DetallePrestamo>();
        foreach (var libroId in libroIds)
        {
            var libro = await _libros.ObtenerPorIdAsync(libroId)
                ?? throw new ReglaNegocioException("Uno de los libros seleccionados no existe.");

            if (!libro.Activo)
            {
                throw new ReglaNegocioException($"No se puede prestar \"{libro.Titulo}\" porque está inactivo.");
            }

            if (libro.Ejemplares <= 0)
            {
                throw new ReglaNegocioException($"No se puede prestar \"{libro.Titulo}\" porque no tiene ejemplares.");
            }

            detalles.Add(new DetallePrestamo { LibroId = libroId, LibroTitulo = libro.Titulo });
        }

        var hoy = DateTime.Today;
        var prestamo = new Prestamo
        {
            SocioId = socioId,
            SocioNombre = socio.Nombre,
            FechaPrestamo = hoy,
            FechaLimite = hoy.AddDays(DiasDePrestamo),
            Estado = EstadoPendiente,
            Detalles = detalles
        };

        prestamo.PrestamoId = await _prestamos.RegistrarAsync(prestamo);
        return prestamo;
    }

    public async Task<ResultadoDevolucion> RegistrarDevolucionAsync(int prestamoId, int libroId)
    {
        var prestamo = await _prestamos.ObtenerPorIdAsync(prestamoId)
            ?? throw new ReglaNegocioException("El préstamo no existe.");

        var detalle = prestamo.Detalles.FirstOrDefault(d => d.LibroId == libroId)
            ?? throw new ReglaNegocioException("Ese libro no pertenece al préstamo.");

        if (detalle.FechaDevolucion != null)
        {
            throw new ReglaNegocioException("Ese libro ya fue devuelto.");
        }

        var fecha = DateTime.Today;
        var dias = CalcularDiasRetraso(prestamo.FechaLimite, fecha);
        var multa = dias * MultaPorDia;
        var pendientes = prestamo.Detalles.Count(d => d.FechaDevolucion == null);
        var nuevoEstado = pendientes <= 1 ? EstadoDevuelto : prestamo.Estado;

        await _prestamos.RegistrarDevolucionAsync(prestamoId, libroId, fecha, nuevoEstado);

        return new ResultadoDevolucion
        {
            Multa = multa,
            DiasRetraso = dias,
            PrestamoCerrado = nuevoEstado == EstadoDevuelto,
            FechaDevolucion = fecha
        };
    }

    public Task<List<ReportePrestamo>> ReporteAsync(DateTime desde, DateTime hasta)
    {
        if (desde.Date > hasta.Date)
        {
            throw new ReglaNegocioException("La fecha inicial no puede ser mayor que la fecha final.");
        }

        return _prestamos.ListarReporteAsync(desde, hasta);
    }
}

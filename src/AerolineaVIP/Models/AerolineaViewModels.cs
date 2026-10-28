using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Progra_03.Models
{
    // ------------------------------------------------------------------------------------
    // Filas que devuelven las vistas de la base de datos (vw_VueloDetalle, vw_DisponibilidadVuelo,
    // vw_EstadoAsientoVuelo, vw_AsientoAeronave, vw_ComprobanteDetalle). Las vistas no estan en el
    // modelo EDMX, por eso se leen con Database.SqlQuery. Cada propiedad coincide con una columna.
    // ------------------------------------------------------------------------------------

    // Un vuelo con su disponibilidad (une vw_VueloDetalle y vw_DisponibilidadVuelo).
    public class TramoRow
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public byte DiaSemana { get; set; }
        public int RutaId { get; set; }
        public decimal TarifaReferencial { get; set; }
        public int AeropuertoOrigenId { get; set; }
        public int AeropuertoDestinoId { get; set; }
        public string Origen { get; set; }
        public string CiudadOrigen { get; set; }
        public string Destino { get; set; }
        public string CiudadDestino { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public int? AeronaveId { get; set; }
        public string EstadoVueloCodigo { get; set; }
        public string Matricula { get; set; }
        public int Capacidad { get; set; }
        public int Ocupados { get; set; }
        public int Disponibles { get; set; }
    }

    // Un tramo que pertenece a un itinerario (para armar las tarjetas de itinerario con escala).
    public class TramoItinerarioRow
    {
        public int ItinerarioId { get; set; }
        public string Referencia { get; set; }
        public string ItinerarioNombre { get; set; }
        public byte Orden { get; set; }
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public byte DiaSemana { get; set; }
        public int RutaId { get; set; }
        public decimal TarifaReferencial { get; set; }
        public int AeropuertoOrigenId { get; set; }
        public int AeropuertoDestinoId { get; set; }
        public string Origen { get; set; }
        public string CiudadOrigen { get; set; }
        public string Destino { get; set; }
        public string CiudadDestino { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public int? AeronaveId { get; set; }
        public string EstadoVueloCodigo { get; set; }
        public string Matricula { get; set; }
        public int Capacidad { get; set; }
        public int Ocupados { get; set; }
        public int Disponibles { get; set; }
    }

    // Estado de cada asiento de un vuelo (vw_EstadoAsientoVuelo).
    public class EstadoAsientoRow
    {
        public int VueloId { get; set; }
        public int AeronaveId { get; set; }
        public byte NumeroAsiento { get; set; }
        public string CodigoAsiento { get; set; }
        public byte Fila { get; set; }
        public string Lado { get; set; }
        public string Ubicacion { get; set; }
        public string EstadoVisual { get; set; }   // DISPONIBLE | RESERVADO | OCUPADO
        public string ColorEstado { get; set; }    // Verde | Morado | Rojo
    }

    // Distribucion de asientos de una aeronave (vw_AsientoAeronave).
    public class AsientoAeronaveRow
    {
        public int AeronaveId { get; set; }
        public string Matricula { get; set; }
        public byte NumeroAsiento { get; set; }
        public string CodigoAsiento { get; set; }
        public byte Fila { get; set; }
        public string Lado { get; set; }
        public string Ubicacion { get; set; }
    }

    // Encabezado de comprobante (vw_ComprobanteDetalle).
    public class ComprobanteRow
    {
        public int ComprobanteId { get; set; }
        public string NumeroComprobante { get; set; }
        public string TipoComprobanteCodigo { get; set; }
        public string TipoComprobante { get; set; }
        public DateTime FechaEmision { get; set; }
        public string EstadoComprobanteCodigo { get; set; }
        public string EmisorNombre { get; set; }
        public string EmisorIdentificacion { get; set; }
        public string EmisorDireccion { get; set; }
        public string LeyendaComprobante { get; set; }
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public string UsuarioId { get; set; }
        public int CompraId { get; set; }
        public DateTime FechaCompra { get; set; }
        public decimal TotalOperacion { get; set; }
        public string MonedaCodigo { get; set; }
        public string EstadoCompraCodigo { get; set; }
        public string CodigoOperacion { get; set; }
        public string MetodoPago { get; set; }
        public string MarcaTarjeta { get; set; }
        public string TarjetaEnmascarada { get; set; }
    }

    // Linea de detalle de una reserva (una asignacion con su vuelo, pasajero y asiento).
    public class LineaReservaRow
    {
        public int AsignacionId { get; set; }
        public int ReservaId { get; set; }
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string CiudadOrigen { get; set; }
        public string CiudadDestino { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public DateTime SalidaProgramada { get; set; }
        public string EstadoVueloCodigo { get; set; }
        public int PasajeroId { get; set; }
        public string TipoDocumentoCodigo { get; set; }
        public string NumeroDocumento { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public byte NumeroAsiento { get; set; }
        public string CodigoAsiento { get; set; }
        public decimal ImporteTramo { get; set; }
        public string EstadoAsignacionCodigo { get; set; }
        public int? CheckInId { get; set; }
    }

    // Resumen de una reserva para listados (pantallas 13 y 20).
    public class ReservaResumenRow
    {
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public string UsuarioId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaExpiracion { get; set; }
        public string EstadoReservaCodigo { get; set; }
        public DateTime? FechaViaje { get; set; }
        public int Pasajeros { get; set; }
        public string EstadoCompra { get; set; }
        public decimal Total { get; set; }
    }

    public class RutaReservaRow
    {
        public int ReservaId { get; set; }
        public int VueloId { get; set; }
        public string CiudadOrigen { get; set; }
        public string CiudadDestino { get; set; }
        public DateTime SalidaProgramada { get; set; }
    }

    public class ConflictoRow
    {
        public string Matricula { get; set; }
        public string CodigoVueloA { get; set; }
        public string CodigoVueloB { get; set; }
        public DateTime FechaA { get; set; }
        public TimeSpan SalidaA { get; set; }
        public TimeSpan LlegadaA { get; set; }
        public TimeSpan SalidaB { get; set; }
        public TimeSpan LlegadaB { get; set; }
    }

    public class ReporteFila
    {
        public string Col1 { get; set; }
        public string Col2 { get; set; }
        public string Col3 { get; set; }
        public string Col4 { get; set; }
        public string Col5 { get; set; }
        public string Col6 { get; set; }
        public string Col7 { get; set; }
        public string Col8 { get; set; }
    }

    // ------------------------------------------------------------------------------------
    // ViewModels de las pantallas 1 a 16
    // ------------------------------------------------------------------------------------

    // Pantallas 1 y 5: criterios de busqueda.
    public class BusquedaVM
    {
        public int? OrigenId { get; set; }
        public int? DestinoId { get; set; }
        public DateTime? Fecha { get; set; }
        public int Pasajeros { get; set; }
        public string Horario { get; set; }          // Todos | Mañana | Tarde | Noche

        public SelectList Aeropuertos { get; set; }
        public List<DiasRutaVM> Rutas { get; set; }

        public BusquedaVM()
        {
            Pasajeros = 1;
            Horario = "Todos";
            Rutas = new List<DiasRutaVM>();
        }
    }

    public class DiasRutaVM
    {
        public string Ruta { get; set; }
        public string Dias { get; set; }
    }

    // Pantalla 6: una tarjeta por vuelo o por itinerario con escala.
    public class TarjetaVueloVM
    {
        public List<TramoRow> Tramos { get; set; }
        public bool EsItinerario { get; set; }
        public string Nombre { get; set; }
        public int Disponibles { get; set; }          // minimo entre los tramos
        public bool Seleccionable { get; set; }
        public string Motivo { get; set; }            // por que no se puede seleccionar
        public string TramosCsv { get; set; }

        public TarjetaVueloVM()
        {
            Tramos = new List<TramoRow>();
        }
    }

    public class ResultadosVM
    {
        public BusquedaVM Busqueda { get; set; }
        public string OrigenCiudad { get; set; }
        public string DestinoCiudad { get; set; }
        public List<TarjetaVueloVM> Tarjetas { get; set; }
        public string Aviso { get; set; }

        public ResultadosVM()
        {
            Tarjetas = new List<TarjetaVueloVM>();
        }
    }

    // Mapa de asientos (parcial _MapaAsientos).
    public class MapaAsientosVM
    {
        public List<EstadoAsientoRow> Asientos { get; set; }
        public bool Seleccionable { get; set; }
        public string Prefijo { get; set; }           // para ids unicos cuando hay varios mapas (tramos)
        public bool Compacto { get; set; }
    }

    // Pantalla 7.
    public class DisponibilidadVM
    {
        public TramoRow Vuelo { get; set; }
        public List<EstadoAsientoRow> Asientos { get; set; }
        public int Reservados { get; set; }
        public int Ocupados { get; set; }
        public DateTime Corte { get; set; }
        public string Mensaje { get; set; }
    }

    // Pantalla 8.
    public class TramoMapaVM
    {
        public TramoRow Vuelo { get; set; }
        public List<EstadoAsientoRow> Asientos { get; set; }
    }

    public class SeleccionAsientoVM
    {
        public List<TramoMapaVM> Tramos { get; set; }
        public int Pasajeros { get; set; }
        public string TramosCsv { get; set; }
        public int VueloId { get; set; }
        public List<int> Previo { get; set; }         // asientos ya elegidos (pasajero * tramos + tramo)
        public string Error { get; set; }

        public SeleccionAsientoVM()
        {
            Tramos = new List<TramoMapaVM>();
            Previo = new List<int>();
        }
    }

    // Pantalla 9.
    public class DatosPasajeroVM
    {
        [Required(ErrorMessage = "Seleccione el tipo de documento.")]
        [Display(Name = "Tipo de documento")]
        public string TipoDocumentoCodigo { get; set; }

        [Required(ErrorMessage = "Ingrese el número de documento.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "El documento debe tener entre 5 y 20 caracteres.")]
        [Display(Name = "Número de documento")]
        public string NumeroDocumento { get; set; }

        [Required(ErrorMessage = "Ingrese los nombres.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingrese los apellidos.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento (opcional)")]
        public DateTime? FechaNacimiento { get; set; }

        [StringLength(256, ErrorMessage = "Máximo 256 caracteres.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [Display(Name = "Correo (opcional)")]
        public string Correo { get; set; }

        [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
        [Display(Name = "Teléfono (opcional)")]
        public string Telefono { get; set; }
    }

    public class PasajerosVM
    {
        public List<DatosPasajeroVM> Pasajeros { get; set; }
        public List<string> CodigosAsiento { get; set; }   // resumen: asientos por pasajero
        public List<TramoRow> Tramos { get; set; }
        public SelectList TiposDocumento { get; set; }

        public PasajerosVM()
        {
            Pasajeros = new List<DatosPasajeroVM>();
            CodigosAsiento = new List<string>();
            Tramos = new List<TramoRow>();
        }
    }

    // Pantalla 10.
    public class ConfirmarReservaVM
    {
        public List<TramoRow> Tramos { get; set; }
        public List<DatosPasajeroVM> Pasajeros { get; set; }
        public List<List<string>> CodigosAsiento { get; set; }  // [pasajero][tramo]
        public List<decimal> ImportePasajero { get; set; }
        public decimal Total { get; set; }
        public int HorasRetencion { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar las condiciones de la reserva.")]
        [Display(Name = "Acepto las condiciones de la reserva")]
        public bool AceptaCondiciones { get; set; }

        public ConfirmarReservaVM()
        {
            Tramos = new List<TramoRow>();
            Pasajeros = new List<DatosPasajeroVM>();
            CodigosAsiento = new List<List<string>>();
            ImportePasajero = new List<decimal>();
        }
    }

    // Pantalla 13.
    public class MisReservasVM
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string Estado { get; set; }
        public int Pagina { get; set; }
        public int TotalPaginas { get; set; }
        public List<ReservaResumenRow> Reservas { get; set; }
        public Dictionary<int, string> Rutas { get; set; }
        public bool EsPersonal { get; set; }

        public MisReservasVM()
        {
            Reservas = new List<ReservaResumenRow>();
            Rutas = new Dictionary<int, string>();
            Pagina = 1;
            TotalPaginas = 1;
        }
    }

    // Pantalla 14 y 12.
    public class DetalleReservaVM
    {
        public Reserva Reserva { get; set; }
        public List<LineaReservaRow> Lineas { get; set; }
        public Compra Compra { get; set; }
        public Pago UltimoPago { get; set; }
        public decimal Total { get; set; }
        public string Moneda { get; set; }
        public string MetodoTexto { get; set; }
        public bool PuedeCancelar { get; set; }
        public string AvisoCancelacion { get; set; }

        [StringLength(300, MinimumLength = 10, ErrorMessage = "El motivo debe tener entre 10 y 300 caracteres.")]
        [Display(Name = "Motivo de cancelación")]
        public string Motivo { get; set; }

        public DetalleReservaVM()
        {
            Lineas = new List<LineaReservaRow>();
            Moneda = Reglas.Moneda;
        }
    }

    // Pantalla 11.
    public class PagoVM
    {
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public string Ruta { get; set; }
        public DateTime? FechaViaje { get; set; }
        public int Pasajeros { get; set; }
        public string Asientos { get; set; }
        public decimal Total { get; set; }
        public string Moneda { get; set; }
        public string EstadoCompra { get; set; }

        public Guid Token { get; set; }

        [Required(ErrorMessage = "Seleccione el método de pago.")]
        public string MetodoPagoCodigo { get; set; }

        [Required(ErrorMessage = "Seleccione la marca de la tarjeta.")]
        public string MarcaTarjetaCodigo { get; set; }

        [Required(ErrorMessage = "Ingrese el número de la tarjeta.")]
        [Display(Name = "Número de tarjeta")]
        public string NumeroTarjeta { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre del titular.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        [Display(Name = "Nombre del titular")]
        public string NombreTitular { get; set; }

        [Required(ErrorMessage = "Ingrese el vencimiento (MM/AA).")]
        [Display(Name = "Vencimiento")]
        public string Vencimiento { get; set; }

        [Required(ErrorMessage = "Ingrese el código de seguridad.")]
        [Display(Name = "Código de seguridad")]
        public string CodigoSeguridad { get; set; }

        public List<MetodoPago> Metodos { get; set; }
        public List<MarcaTarjeta> Marcas { get; set; }

        public PagoVM()
        {
            Metodos = new List<MetodoPago>();
            Marcas = new List<MarcaTarjeta>();
            Moneda = Reglas.Moneda;
        }
    }

    // Pantalla 15.
    public class CheckInVM
    {
        [Required(ErrorMessage = "Ingrese el código de la reserva.")]
        [RegularExpression("^RV[0-9]{8}$", ErrorMessage = "El código debe tener el formato RV00000000.")]
        [Display(Name = "Código de reserva")]
        public string CodigoReserva { get; set; }

        [Required(ErrorMessage = "Ingrese el número de documento del pasajero.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "El documento debe tener entre 5 y 20 caracteres.")]
        [Display(Name = "Número de documento del pasajero")]
        public string NumeroDocumento { get; set; }

        public List<LineaCheckIn> Lineas { get; set; }
        public int[] Seleccionadas { get; set; }
        public string Mensaje { get; set; }
        public string Error { get; set; }

        public CheckInVM()
        {
            Lineas = new List<LineaCheckIn>();
        }
    }

    public class LineaCheckIn
    {
        public LineaReservaRow Linea { get; set; }
        public bool Elegible { get; set; }
        public string Estado { get; set; }
    }

    // Pantalla 16.
    public class ComprobantesVM
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string Tipo { get; set; }
        public List<ComprobanteRow> Comprobantes { get; set; }
        public ComprobanteRow Seleccionado { get; set; }
        public List<LineaReservaRow> Detalle { get; set; }

        public ComprobantesVM()
        {
            Comprobantes = new List<ComprobanteRow>();
            Detalle = new List<LineaReservaRow>();
        }
    }

    // ------------------------------------------------------------------------------------
    // ViewModels de las pantallas 17 a 21
    // ------------------------------------------------------------------------------------

    public class AdminVuelosVM
    {
        public string Pestana { get; set; }           // vuelos | frecuencias | itinerarios
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int? RutaId { get; set; }
        public string Estado { get; set; }
        public List<TramoRow> Vuelos { get; set; }
        public List<ConflictoRow> Conflictos { get; set; }
        public List<Frecuencia> Frecuencias { get; set; }
        public List<Itinerario> Itinerarios { get; set; }
        public SelectList Rutas { get; set; }
        public SelectList Estados { get; set; }
        public bool VerConflictos { get; set; }

        public AdminVuelosVM()
        {
            Pestana = "vuelos";
            Vuelos = new List<TramoRow>();
            Conflictos = new List<ConflictoRow>();
        }
    }

    public class EditarVueloVM
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public string Ruta { get; set; }
        public DateTime Fecha { get; set; }
        public string Horario { get; set; }

        [Display(Name = "Aeronave asignada")]
        public int? AeronaveId { get; set; }

        [Required(ErrorMessage = "Seleccione el estado.")]
        [Display(Name = "Estado")]
        public string EstadoVueloCodigo { get; set; }

        public SelectList Aeronaves { get; set; }
        public SelectList Estados { get; set; }
    }

    public class FilaAeronaveVM
    {
        public Aeronave Aeronave { get; set; }
        public string Modelo { get; set; }
        public int Capacidad { get; set; }
        public int VuelosProximos { get; set; }
    }

    public class AdminAeronavesVM
    {
        public List<FilaAeronaveVM> Flota { get; set; }
        public int? SeleccionadaId { get; set; }
        public string SeleccionadaMatricula { get; set; }
        public List<EstadoAsientoRow> Distribucion { get; set; }
        public List<TramoRow> Asignaciones { get; set; }

        public AdminAeronavesVM()
        {
            Flota = new List<FilaAeronaveVM>();
            Distribucion = new List<EstadoAsientoRow>();
            Asignaciones = new List<TramoRow>();
        }
    }

    public class UsuarioFilaVM
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public bool Bloqueado { get; set; }
    }

    public class AdminGestionVM
    {
        public string Pestana { get; set; }           // usuarios | pasajeros | reservas
        public string Buscar { get; set; }
        public string Rol { get; set; }
        public string Estado { get; set; }
        public bool EsSuperUsuario { get; set; }
        public SelectList RolesLista { get; set; }
        public List<UsuarioFilaVM> Usuarios { get; set; }
        public List<Pasajero> Pasajeros { get; set; }
        public List<ReservaResumenRow> Reservas { get; set; }
        public Dictionary<int, string> RutasReservas { get; set; }

        public AdminGestionVM()
        {
            Pestana = "usuarios";
            Usuarios = new List<UsuarioFilaVM>();
            Pasajeros = new List<Pasajero>();
            Reservas = new List<ReservaResumenRow>();
            RutasReservas = new Dictionary<int, string>();
        }
    }

    public class ReportesVM
    {
        [Display(Name = "Consulta")]
        public string Consulta { get; set; }          // ocupacion | reservas | compras

        [Display(Name = "Fecha desde")]
        public DateTime? Desde { get; set; }

        [Display(Name = "Fecha hasta")]
        public DateTime? Hasta { get; set; }

        public int? RutaId { get; set; }
        public int? AeronaveId { get; set; }
        public string Estado { get; set; }
        public SelectList Rutas { get; set; }
        public SelectList Aeronaves { get; set; }
        public string[] Columnas { get; set; }
        public List<ReporteFila> Filas { get; set; }
        public DateTime? Corte { get; set; }
        public string Mensaje { get; set; }
        public bool Ejecutada { get; set; }

        public ReportesVM()
        {
            Consulta = "ocupacion";
            Columnas = new string[0];
            Filas = new List<ReporteFila>();
        }
    }
}

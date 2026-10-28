using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using Progra_03.Models;

namespace Progra_03
{
    // PRIMERA ENTREGA: nombres de rol y combinaciones para [Authorize(Roles = ...)].
    // La autorizacion real siempre se valida en el servidor (RNP-09); ocultar un enlace no protege nada.
    public static class RolesApp
    {
        public const string SuperUsuario = "SuperUsuario";
        public const string Administrador = "Administrador";
        public const string Operaciones = "Operaciones";
        public const string Comercial = "Comercial";
        public const string Counter = "Counter";
        public const string Pasajero = "Pasajero";
        public const string Cliente = "Cliente";

        // Roles administrativos: panel de administracion (pantallas 17 a 21) y CRUD operativo.
        public const string Administrativos = SuperUsuario + "," + Administrador;

        // CRUD de datos de vuelo: ademas del personal administrativo, el responsable de operaciones.
        public const string Operativos = SuperUsuario + "," + Administrador + "," + Operaciones;

        // CRUD de datos comerciales: ademas del superusuario, el responsable comercial.
        public const string Comerciales = SuperUsuario + "," + Comercial;

        // Quienes pueden reservar, pagar y hacer check-in (matriz 16.1; el counter actua con sus propios permisos, DP-13).
        public const string Compradores = SuperUsuario + "," + Cliente + "," + Pasajero + "," + Counter;

        // Personal que puede consultar reservas, comprobantes y operaciones de otros usuarios.
        public const string Personal = SuperUsuario + "," + Administrador + "," + Counter;
    }

    // Reglas parametrizables. Mientras una decision de 18.1 no se apruebe, su valor se lee de
    // Web.config (appSettings) y no queda fijo en el codigo: asi puede cambiarse sin recompilar.
    public static class Reglas
    {
        // RN-03: maximo siete asientos por vuelo.
        public const int CapacidadMaxima = 7;
        public const string Moneda = "USD";

        // DP-07: horas que una reserva pendiente conserva sus asientos.
        public static int HorasRetencionReserva { get { return Entero("HorasRetencionReserva", 24); } }

        // DP-12: ventana de check-in respecto de la salida programada.
        public static int HorasAperturaCheckIn { get { return Entero("HorasAperturaCheckIn", 24); } }
        public static int HorasCierreCheckIn { get { return Entero("HorasCierreCheckIn", 1); } }

        // DP-09: 1 = los tramos con Galapagos tambien se venden por separado; 0 = solo itinerario completo.
        public static bool VentaTramosGalapagos { get { return Entero("VentaTramosGalapagos", 0) == 1; } }

        // DP-08: 1 = tambien se permite cancelar reservas ya pagadas (sin reembolso automatico).
        public static bool CancelarReservasPagadas { get { return Entero("CancelarReservasPagadas", 0) == 1; } }

        private static int Entero(string clave, int porDefecto)
        {
            string valor = WebConfigurationManager.AppSettings[clave];
            int n;
            if (!string.IsNullOrEmpty(valor) && int.TryParse(valor, out n)) return n;
            return porDefecto;
        }
    }

    // Formato de textos que se repite en varias vistas.
    public static class Fmt
    {
        private static readonly CultureInfo Cultura = new CultureInfo("es-EC");

        public static string Dinero(decimal valor, string moneda)
        {
            return valor.ToString("N2", Cultura) + " " + moneda;
        }

        public static string Dinero(decimal valor)
        {
            return Dinero(valor, Reglas.Moneda);
        }

        public static string Fecha(DateTime f)
        {
            return f.ToString("dd/MM/yyyy");
        }

        public static string FechaHora(DateTime f)
        {
            return f.ToString("dd/MM/yyyy HH:mm");
        }

        public static string Hora(TimeSpan h)
        {
            return h.ToString(@"hh\:mm");
        }

        public static string DiaSemana(int d)
        {
            switch (d)
            {
                case 1: return "Lunes";
                case 2: return "Martes";
                case 3: return "Miércoles";
                case 4: return "Jueves";
                case 5: return "Viernes";
                case 6: return "Sábado";
                case 7: return "Domingo";
                default: return "";
            }
        }

        public static string DiaCorto(int d)
        {
            switch (d)
            {
                case 1: return "Lun";
                case 2: return "Mar";
                case 3: return "Mié";
                case 4: return "Jue";
                case 5: return "Vie";
                case 6: return "Sáb";
                case 7: return "Dom";
                default: return "";
            }
        }

        // Dia de la semana (1 = lunes ... 7 = domingo) de una fecha.
        public static int NumeroDia(DateTime f)
        {
            int d = (int)f.DayOfWeek; // domingo = 0
            return d == 0 ? 7 : d;
        }

        public static string EstadoVuelo(string codigo)
        {
            switch (codigo)
            {
                case "PROGRAMADO": return "Programado";
                case "CONFIRMADO": return "Confirmado";
                case "CANCELADO": return "Cancelado";
                case "COMPLETADO": return "Completado";
                default: return codigo;
            }
        }

        public static string EstadoReserva(string codigo)
        {
            switch (codigo)
            {
                case "PENDIENTE": return "Pendiente de pago";
                case "CONFIRMADA": return "Confirmada";
                case "CANCELADA": return "Cancelada";
                case "EXPIRADA": return "Expirada";
                default: return codigo;
            }
        }

        public static string EstadoCompra(string codigo)
        {
            switch (codigo)
            {
                case "PENDIENTE": return "Pendiente de pago";
                case "PAGADA": return "Pagada";
                case "ANULADA": return "Anulada";
                case null: return "Sin compra";
                default: return codigo;
            }
        }

        public static string EstadoPago(string codigo)
        {
            switch (codigo)
            {
                case "PENDIENTE": return "Pendiente";
                case "APROBADO": return "Aprobado";
                case "RECHAZADO": return "Rechazado";
                case "REVERSADO": return "Reversado";
                case null: return "Sin pago";
                default: return codigo;
            }
        }

        public static string EstadoAeronave(string codigo)
        {
            return codigo == "DISPONIBLE" ? "Disponible" : "No disponible";
        }

        public static string TipoComprobante(string codigo)
        {
            switch (codigo)
            {
                case "CONFIRMACION": return "Confirmación";
                case "FACTURA": return "Comprobante de venta académico";
                case "NOTA_CREDITO": return "Nota de crédito académica";
                default: return codigo;
            }
        }

        // Clase CSS de Bootstrap para una etiqueta de estado (el estado siempre se escribe ademas del color).
        public static string ClaseEstado(string codigo)
        {
            switch (codigo)
            {
                case "CONFIRMADA":
                case "CONFIRMADO":
                case "PAGADA":
                case "APROBADO":
                case "COMPLETADO":
                case "EMITIDO":
                case "DISPONIBLE":
                    return "text-bg-success";
                case "PENDIENTE":
                case "PROGRAMADO":
                    return "text-bg-warning";
                case "CANCELADA":
                case "CANCELADO":
                case "RECHAZADO":
                case "ANULADA":
                case "ANULADO":
                case "NO_DISPONIBLE":
                    return "text-bg-danger";
                default:
                    return "text-bg-secondary";
            }
        }

        // Une los nombres de ciudad de una lista de tramos: Quito -> Guayaquil -> Galapagos.
        public static string Ruta(IEnumerable<TramoRow> tramos)
        {
            var lista = tramos.ToList();
            if (lista.Count == 0) return "";
            var ciudades = new List<string> { lista[0].CiudadOrigen };
            foreach (var t in lista) ciudades.Add(t.CiudadDestino);
            return string.Join(" → ", ciudades);
        }
    }

    // Reglas de venta de un vuelo, compartidas por las pantallas 6 a 11.
    public static class VentaVuelo
    {
        public static bool EstadoPermiteReserva(string estado)
        {
            return estado == "PROGRAMADO" || estado == "CONFIRMADO";
        }

        // RNP-01: sin aeronave asignada el vuelo no se comercializa.
        public static bool Comercializable(TramoRow t)
        {
            return t.AeronaveId.HasValue && EstadoPermiteReserva(t.EstadoVueloCodigo);
        }

        // DP-09: un tramo con Galapagos solo se vende suelto si la politica lo permite.
        public static bool TocaGalapagos(TramoRow t)
        {
            return t.Origen == "GPS" || t.Destino == "GPS";
        }
    }

    // Datos de un pasajero capturados en la pantalla 9 (todavia no se guardan en la base).
    [Serializable]
    public class DatosPasajero
    {
        public string TipoDocumentoCodigo { get; set; }
        public string NumeroDocumento { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
    }

    // Operacion de reserva en curso (pantallas 8 a 10). Se guarda en la sesion del usuario:
    // la seleccion de asientos es TEMPORAL y no bloquea inventario (sin concurrencia avanzada en esta entrega).
    [Serializable]
    public class ReservaEnCurso
    {
        public List<int> Tramos { get; set; }       // VueloId de cada tramo, en orden
        public int Pasajeros { get; set; }
        public List<int> Asientos { get; set; }     // indice = pasajero * numero de tramos + tramo; valor = NumeroAsiento (0 = sin asignar)
        public List<DatosPasajero> Datos { get; set; }

        public ReservaEnCurso()
        {
            Tramos = new List<int>();
            Asientos = new List<int>();
            Datos = new List<DatosPasajero>();
        }

        public int Asiento(int pasajero, int tramo)
        {
            int i = pasajero * Tramos.Count + tramo;
            return (i >= 0 && i < Asientos.Count) ? Asientos[i] : 0;
        }

        public bool MismosTramos(IList<int> otros)
        {
            return otros != null && otros.Count == Tramos.Count && otros.SequenceEqual(Tramos);
        }

        public const string ClaveSesion = "ReservaEnCurso";

        public static ReservaEnCurso Actual(HttpSessionStateBase sesion)
        {
            return sesion[ClaveSesion] as ReservaEnCurso;
        }
    }
}

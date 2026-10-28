using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    // Controlador base de los modulos de vuelos, reservas, compras y administracion.
    // Reune el acceso a la base de dominio (AerolineaVIP) y las consultas a las vistas SQL.
    public abstract class BaseController : Controller
    {
        protected AerolineaVIPEntities db = new AerolineaVIPEntities();

        // Une vw_VueloDetalle y vw_DisponibilidadVuelo: un vuelo con su disponibilidad.
        protected const string SqlTramos =
            "SELECT v.VueloId, v.CodigoVuelo, v.FechaOperacion, v.DiaSemana, v.RutaId, v.TarifaReferencial, " +
            "v.AeropuertoOrigenId, v.AeropuertoDestinoId, v.Origen, v.CiudadOrigen, v.Destino, v.CiudadDestino, " +
            "v.HoraSalida, v.HoraLlegada, v.AeronaveId, v.EstadoVueloCodigo, " +
            "d.Matricula, d.Capacidad, d.Ocupados, d.Disponibles " +
            "FROM dbo.vw_VueloDetalle v JOIN dbo.vw_DisponibilidadVuelo d ON d.VueloId = v.VueloId ";

        protected string UsuarioActualId
        {
            get { return User.Identity.GetUserId(); }
        }

        protected bool EsPersonal
        {
            get
            {
                return User.IsInRole(RolesApp.SuperUsuario) || User.IsInRole(RolesApp.Administrador) || User.IsInRole(RolesApp.Counter);
            }
        }

        // DP-07 (supuesto de implementacion): una reserva pendiente cuya fecha de expiracion ya paso
        // se marca EXPIRADA y sus asientos vuelven a estar disponibles. No hay un proceso programado:
        // la expiracion se aplica al consultar (expiracion perezosa), antes de leer disponibilidad.
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE rpv SET EstadoAsignacionCodigo = 'LIBERADA' " +
                    "FROM dbo.ReservaPasajeroVuelo rpv JOIN dbo.Reserva r ON r.ReservaId = rpv.ReservaId " +
                    "WHERE r.EstadoReservaCodigo = 'PENDIENTE' AND r.FechaExpiracion IS NOT NULL " +
                    "AND r.FechaExpiracion < SYSDATETIME() AND rpv.EstadoAsignacionCodigo = 'PENDIENTE'");
                db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.Reserva SET EstadoReservaCodigo = 'EXPIRADA' " +
                    "WHERE EstadoReservaCodigo = 'PENDIENTE' AND FechaExpiracion IS NOT NULL AND FechaExpiracion < SYSDATETIME()");
            }
            catch (Exception)
            {
                // La expiracion es un apoyo: si falla no debe impedir la consulta.
            }
            base.OnActionExecuting(filterContext);
        }

        protected TramoRow ObtenerTramo(int vueloId)
        {
            return db.Database.SqlQuery<TramoRow>(SqlTramos + "WHERE v.VueloId = @p0", vueloId).FirstOrDefault();
        }

        protected List<TramoRow> ObtenerTramos(IEnumerable<int> vueloIds)
        {
            var resultado = new List<TramoRow>();
            foreach (int id in vueloIds)
            {
                var t = ObtenerTramo(id);
                if (t == null) return null;
                resultado.Add(t);
            }
            return resultado;
        }

        // Estado real de los siete asientos de un vuelo (vw_EstadoAsientoVuelo, solo vuelos con aeronave).
        protected List<EstadoAsientoRow> ObtenerAsientos(int vueloId)
        {
            return db.Database.SqlQuery<EstadoAsientoRow>(
                "SELECT VueloId, AeronaveId, NumeroAsiento, CodigoAsiento, Fila, Lado, Ubicacion, EstadoVisual, ColorEstado " +
                "FROM dbo.vw_EstadoAsientoVuelo WHERE VueloId = @p0 ORDER BY NumeroAsiento", vueloId).ToList();
        }

        // Lineas (asignaciones) de una reserva con vuelo, pasajero y asiento.
        protected List<LineaReservaRow> ObtenerLineas(int reservaId)
        {
            return db.Database.SqlQuery<LineaReservaRow>(
                "SELECT rpv.AsignacionId, rpv.ReservaId, v.VueloId, v.CodigoVuelo, v.FechaOperacion, v.Origen, v.Destino, " +
                "v.CiudadOrigen, v.CiudadDestino, v.HoraSalida, v.HoraLlegada, v.SalidaProgramada, v.EstadoVueloCodigo, " +
                "p.PasajeroId, p.TipoDocumentoCodigo, p.NumeroDocumento, p.Nombres, p.Apellidos, " +
                "rpv.NumeroAsiento, s.Codigo AS CodigoAsiento, rpv.ImporteTramo, rpv.EstadoAsignacionCodigo, " +
                "(SELECT TOP 1 c.CheckInId FROM dbo.CheckIn c WHERE c.AsignacionId = rpv.AsignacionId AND c.EstadoCheckInCodigo = 'COMPLETADO') AS CheckInId " +
                "FROM dbo.ReservaPasajeroVuelo rpv " +
                "JOIN dbo.vw_VueloDetalle v ON v.VueloId = rpv.VueloId " +
                "JOIN dbo.Pasajero p ON p.PasajeroId = rpv.PasajeroId " +
                "JOIN dbo.Vuelo vu ON vu.VueloId = rpv.VueloId " +
                "LEFT JOIN dbo.Aeronave a ON a.AeronaveId = vu.AeronaveId " +
                "LEFT JOIN dbo.Asiento s ON s.ModeloAeronaveId = a.ModeloAeronaveId AND s.NumeroAsiento = rpv.NumeroAsiento " +
                "WHERE rpv.ReservaId = @p0 " +
                "ORDER BY v.SalidaProgramada, p.PasajeroId", reservaId).ToList();
        }

        // Texto de la ruta de cada reserva (Quito -> Guayaquil -> Galapagos), para listados.
        protected Dictionary<int, string> ObtenerRutas(IEnumerable<int> reservaIds)
        {
            var resultado = new Dictionary<int, string>();
            var ids = reservaIds.ToList();
            if (ids.Count == 0) return resultado;
            string lista = string.Join(",", ids.Select(i => i.ToString()));   // enteros: sin riesgo de inyeccion
            var filas = db.Database.SqlQuery<RutaReservaRow>(
                "SELECT DISTINCT rpv.ReservaId, v.VueloId, v.CiudadOrigen, v.CiudadDestino, v.SalidaProgramada " +
                "FROM dbo.ReservaPasajeroVuelo rpv JOIN dbo.vw_VueloDetalle v ON v.VueloId = rpv.VueloId " +
                "WHERE rpv.ReservaId IN (" + lista + ") ORDER BY v.SalidaProgramada").ToList();
            foreach (var g in filas.GroupBy(f => f.ReservaId))
            {
                var ordenadas = g.OrderBy(f => f.SalidaProgramada).ToList();
                var ciudades = new List<string> { ordenadas[0].CiudadOrigen };
                foreach (var f in ordenadas) ciudades.Add(f.CiudadDestino);
                resultado[g.Key] = string.Join(" → ", ciudades);
            }
            return resultado;
        }

        // Reservas con su resumen. usuarioId = null devuelve las de todos (solo para personal autorizado).
        protected List<ReservaResumenRow> ConsultarReservas(string usuarioId, DateTime? desde, DateTime? hasta, string estado, string buscar)
        {
            var sql = new System.Text.StringBuilder();
            var parametros = new List<object>();
            sql.Append("SELECT r.ReservaId, r.CodigoReserva, r.UsuarioId, r.FechaCreacion, r.FechaExpiracion, r.EstadoReservaCodigo, ");
            sql.Append("MIN(v.FechaOperacion) AS FechaViaje, COUNT(DISTINCT rpv.PasajeroId) AS Pasajeros, ");
            sql.Append("(SELECT TOP 1 c.EstadoCompraCodigo FROM dbo.Compra c WHERE c.ReservaId = r.ReservaId ORDER BY c.CompraId DESC) AS EstadoCompra, ");
            sql.Append("ISNULL(SUM(rpv.ImporteTramo), 0) AS Total ");
            sql.Append("FROM dbo.Reserva r ");
            sql.Append("LEFT JOIN dbo.ReservaPasajeroVuelo rpv ON rpv.ReservaId = r.ReservaId ");
            sql.Append("LEFT JOIN dbo.vw_VueloDetalle v ON v.VueloId = rpv.VueloId ");
            sql.Append("WHERE 1 = 1 ");
            if (!string.IsNullOrEmpty(usuarioId))
            {
                sql.Append("AND r.UsuarioId = @p" + parametros.Count + " ");
                parametros.Add(usuarioId);
            }
            if (!string.IsNullOrEmpty(estado))
            {
                sql.Append("AND r.EstadoReservaCodigo = @p" + parametros.Count + " ");
                parametros.Add(estado);
            }
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                sql.Append("AND r.CodigoReserva LIKE @p" + parametros.Count + " ");
                parametros.Add("%" + buscar.Trim() + "%");
            }
            sql.Append("GROUP BY r.ReservaId, r.CodigoReserva, r.UsuarioId, r.FechaCreacion, r.FechaExpiracion, r.EstadoReservaCodigo ");
            var having = new List<string>();
            if (desde.HasValue)
            {
                having.Add("MIN(v.FechaOperacion) >= @p" + parametros.Count);
                parametros.Add(desde.Value.Date);
            }
            if (hasta.HasValue)
            {
                having.Add("MIN(v.FechaOperacion) <= @p" + parametros.Count);
                parametros.Add(hasta.Value.Date);
            }
            if (having.Count > 0) sql.Append("HAVING " + string.Join(" AND ", having) + " ");
            sql.Append("ORDER BY r.ReservaId DESC");
            return db.Database.SqlQuery<ReservaResumenRow>(sql.ToString(), parametros.ToArray()).ToList();
        }

        // Detalle de una reserva con su compra y su ultimo pago (pantallas 12 y 14).
        protected DetalleReservaVM ObtenerDetalle(int reservaId)
        {
            var reserva = db.Reservas.FirstOrDefault(r => r.ReservaId == reservaId);
            if (reserva == null) return null;

            var vm = new DetalleReservaVM { Reserva = reserva, Lineas = ObtenerLineas(reservaId) };
            vm.Compra = db.Compras.Where(c => c.ReservaId == reservaId).OrderByDescending(c => c.CompraId).FirstOrDefault();
            if (vm.Compra != null)
            {
                int compraId = vm.Compra.CompraId;
                vm.UltimoPago = db.Pagoes.Where(p => p.CompraId == compraId).OrderByDescending(p => p.PagoId).FirstOrDefault();
                vm.Moneda = vm.Compra.MonedaCodigo;
            }

            var vigentes = vm.Lineas.Where(l => l.EstadoAsignacionCodigo == "PENDIENTE" || l.EstadoAsignacionCodigo == "CONFIRMADA").ToList();
            vm.Total = (vigentes.Count > 0 ? vigentes : vm.Lineas).Sum(l => l.ImporteTramo);

            if (vm.UltimoPago != null)
            {
                string codigoMarca = vm.UltimoPago.MarcaTarjetaCodigo;
                string codigoMetodo = vm.UltimoPago.MetodoPagoCodigo;
                var marca = db.MarcaTarjetas.FirstOrDefault(x => x.Codigo == codigoMarca);
                var metodo = db.MetodoPagoes.FirstOrDefault(x => x.Codigo == codigoMetodo);
                vm.MetodoTexto = (metodo != null ? metodo.Nombre : codigoMetodo) + " · " + (marca != null ? marca.Nombre : codigoMarca) + " **** " + vm.UltimoPago.UltimosCuatroDigitos;
            }
            return vm;
        }

        // Mensaje legible de un error de base de datos (los triggers de la base usan THROW con texto en espanol).
        protected static string MensajeBase(Exception ex)
        {
            var e = ex;
            while (e.InnerException != null) e = e.InnerException;
            string m = e.Message ?? "";
            if (m.Contains("UX_RPV_AsientoActivo")) return "Uno de los asientos ya no está disponible. Seleccione otro.";
            if (m.Contains("UX_RPV_PasajeroActivo")) return "Este pasajero ya tiene un asiento en ese vuelo.";
            if (m.Contains("UX_Compra_ReservaVigente")) return "La reserva ya tiene una compra vigente.";
            if (m.Contains("UX_Pago_AprobadoPorCompra")) return "La compra ya tiene un pago aprobado.";
            if (m.Contains("UQ_Pasajero_Documento")) return "Ya existe un pasajero con ese documento.";
            if (m.Contains("REFERENCE constraint")) return "No se puede completar la operación: el registro tiene datos asociados.";
            if (m.Contains("UNIQUE") || m.Contains("duplicate")) return "Ya existe un registro con esos datos.";
            return m;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

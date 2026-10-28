using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    // Pantalla 16: comprobantes academicos (sin validez tributaria, DP-05).
    [Authorize]
    public class ComprobantesController : BaseController
    {
        private const string SqlComprobante =
            "SELECT ComprobanteId, NumeroComprobante, TipoComprobanteCodigo, TipoComprobante, FechaEmision, EstadoComprobanteCodigo, " +
            "EmisorNombre, EmisorIdentificacion, EmisorDireccion, LeyendaComprobante, ReservaId, CodigoReserva, UsuarioId, CompraId, " +
            "FechaCompra, TotalOperacion, MonedaCodigo, EstadoCompraCodigo, CodigoOperacion, MetodoPago, MarcaTarjeta, TarjetaEnmascarada " +
            "FROM dbo.vw_ComprobanteDetalle ";

        public ActionResult Index(DateTime? desde, DateTime? hasta, string tipo, int? id)
        {
            var vm = new ComprobantesVM { Desde = desde, Hasta = hasta, Tipo = tipo };
            var sql = SqlComprobante + "WHERE 1 = 1 ";
            var p = new List<object>();
            if (!EsPersonal) { sql += "AND UsuarioId = @p" + p.Count + " "; p.Add(UsuarioActualId); }
            if (desde.HasValue) { sql += "AND FechaEmision >= @p" + p.Count + " "; p.Add(desde.Value.Date); }
            if (hasta.HasValue) { sql += "AND FechaEmision < @p" + p.Count + " "; p.Add(hasta.Value.Date.AddDays(1)); }
            if (!string.IsNullOrEmpty(tipo)) { sql += "AND TipoComprobanteCodigo = @p" + p.Count + " "; p.Add(tipo); }
            sql += "ORDER BY FechaEmision DESC, ComprobanteId DESC";
            vm.Comprobantes = db.Database.SqlQuery<ComprobanteRow>(sql, p.ToArray()).ToList();

            if (id.HasValue) vm.Seleccionado = vm.Comprobantes.FirstOrDefault(c => c.ComprobanteId == id.Value);
            if (vm.Seleccionado == null && vm.Comprobantes.Count > 0) vm.Seleccionado = vm.Comprobantes[0];
            if (vm.Seleccionado != null)
                vm.Detalle = ObtenerLineas(vm.Seleccionado.ReservaId).Where(l => l.EstadoAsignacionCodigo != "LIBERADA").ToList();
            return View(vm);
        }

        // Vista limpia para imprimir o guardar como PDF desde el navegador.
        public ActionResult Ver(int id)
        {
            var c = db.Database.SqlQuery<ComprobanteRow>(SqlComprobante + "WHERE ComprobanteId = @p0", id).FirstOrDefault();
            if (c == null || (c.UsuarioId != UsuarioActualId && !EsPersonal))
            {
                TempData["Error"] = "No se encontró el comprobante solicitado.";
                return RedirectToAction("Index");
            }
            var vm = new ComprobantesVM { Seleccionado = c };
            vm.Detalle = ObtenerLineas(c.ReservaId).Where(l => l.EstadoAsignacionCodigo != "LIBERADA").ToList();
            return View(vm);
        }
    }
}

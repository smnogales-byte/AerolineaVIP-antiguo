using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    // Pantalla 15: check-in. Ventana de apertura y cierre parametrizable (DP-11, supuesto no aprobado).
    [Authorize]
    public class CheckInController : BaseController
    {
        public ActionResult Index()
        {
            return View(new CheckInVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Buscar(CheckInVM modelo)
        {
            if (!ModelState.IsValid) return View("Index", modelo);
            var vm = Cargar(modelo.CodigoReserva, modelo.NumeroDocumento);
            if (vm.Error != null) ModelState.AddModelError("", vm.Error);
            return View("Index", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Confirmar(CheckInVM modelo)
        {
            if (!ModelState.IsValid) return View("Index", modelo);
            var vm = Cargar(modelo.CodigoReserva, modelo.NumeroDocumento);
            if (vm.Error != null)
            {
                ModelState.AddModelError("", vm.Error);
                return View("Index", vm);
            }
            var ids = modelo.Seleccionadas ?? new int[0];
            if (ids.Length == 0)
            {
                ModelState.AddModelError("", "Seleccione al menos un vuelo para registrar el check-in.");
                return View("Index", vm);
            }

            var permitidos = vm.Lineas.Where(l => l.Elegible).Select(l => l.Linea.AsignacionId).ToList();
            int registrados = 0;
            foreach (int id in ids.Distinct())
            {
                if (!permitidos.Contains(id)) continue;   // el servidor revalida: no se confia en el formulario
                try
                {
                    db.CheckIns.Add(new CheckIn { AsignacionId = id, FechaRegistro = DateTime.Now, EstadoCheckInCodigo = "COMPLETADO" });
                    db.SaveChanges();
                    registrados++;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", MensajeBase(ex));
                }
            }
            var recarga = Cargar(modelo.CodigoReserva, modelo.NumeroDocumento);
            recarga.Mensaje = registrados > 0 ? "Check-in registrado en " + registrados + " vuelo(s)." : null;
            if (registrados == 0 && ModelState.IsValid) ModelState.AddModelError("", "Ninguno de los vuelos seleccionados admite check-in.");
            return View("Index", recarga);
        }

        private CheckInVM Cargar(string codigo, string documento)
        {
            var vm = new CheckInVM { CodigoReserva = (codigo ?? "").Trim().ToUpper(), NumeroDocumento = (documento ?? "").Trim() };
            var reserva = db.Reservas.FirstOrDefault(r => r.CodigoReserva == vm.CodigoReserva);
            // Mismo mensaje si no existe o no es del usuario: no se revela la existencia de reservas ajenas.
            if (reserva == null || (reserva.UsuarioId != UsuarioActualId && !EsPersonal))
            {
                vm.Error = "No se encontró una reserva con esos datos.";
                return vm;
            }
            if (reserva.EstadoReservaCodigo != "CONFIRMADA")
            {
                vm.Error = "La reserva debe estar pagada (confirmada) para realizar el check-in.";
                return vm;
            }
            var lineas = ObtenerLineas(reserva.ReservaId)
                .Where(l => l.NumeroDocumento == vm.NumeroDocumento && l.EstadoAsignacionCodigo == "CONFIRMADA").ToList();
            if (lineas.Count == 0)
            {
                vm.Error = "No se encontró una reserva con esos datos.";
                return vm;
            }
            DateTime ahora = DateTime.Now;
            foreach (var l in lineas)
            {
                var item = new LineaCheckIn { Linea = l };
                DateTime abre = l.SalidaProgramada.AddHours(-Reglas.HorasAperturaCheckIn);
                DateTime cierra = l.SalidaProgramada.AddHours(-Reglas.HorasCierreCheckIn);
                if (l.CheckInId.HasValue) item.Estado = "Check-in completado";
                else if (l.EstadoVueloCodigo == "CANCELADO" || l.EstadoVueloCodigo == "CERRADO") item.Estado = "Vuelo no disponible";
                else if (ahora < abre) item.Estado = "Abre el " + Fmt.FechaHora(abre);
                else if (ahora > cierra) item.Estado = "Check-in cerrado";
                else { item.Estado = "Disponible"; item.Elegible = true; }
                vm.Lineas.Add(item);
            }
            return vm;
        }
    }
}

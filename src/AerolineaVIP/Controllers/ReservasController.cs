using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    // Pantallas 8, 9, 10, 13 y 14: seleccion de asientos, datos de pasajeros, reserva, consulta y cancelacion.
    // Hay que iniciar sesion: la reserva queda asociada al UsuarioId del usuario autenticado.
    [Authorize]
    public class ReservasController : BaseController
    {
        // ---------------------------------------------------------------------------
        // Pantalla 8. Seleccion de vuelo y asiento (/Reservas/SeleccionAsiento/{vueloId})
        // La seleccion es TEMPORAL (sesion): la reserva solo se registra al confirmar en la pantalla 10.
        // ---------------------------------------------------------------------------
        [Authorize(Roles = RolesApp.Compradores)]
        public ActionResult SeleccionAsiento(int id, int? pasajeros, string tramos)
        {
            int pax = pasajeros ?? 1;
            var ids = ParseTramos(tramos, id);
            List<TramoRow> filas;
            string error = ValidarTramos(ids, pax, out filas);
            if (error != null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Buscar", "Vuelos");
            }

            var vm = ConstruirSeleccion(filas, pax, id);
            var actual = ReservaEnCurso.Actual(Session);
            if (actual != null && actual.Pasajeros == pax && actual.MismosTramos(ids))
            {
                vm.Previo = new List<int>(actual.Asientos);
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Compradores)]
        public ActionResult SeleccionAsiento(int id, int pasajeros, string tramos, int[] asientos)
        {
            var ids = ParseTramos(tramos, id);
            List<TramoRow> filas;
            string error = ValidarTramos(ids, pasajeros, out filas);
            if (error != null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Buscar", "Vuelos");
            }

            int totalTramos = filas.Count;
            var elegidos = asientos == null ? new List<int>() : asientos.ToList();

            if (elegidos.Count != pasajeros * totalTramos || elegidos.Any(a => a < 0 || a > Reglas.CapacidadMaxima) || elegidos.Any(a => a == 0))
            {
                var faltante = ConstruirSeleccion(filas, pasajeros, id);
                faltante.Previo = elegidos.Count == pasajeros * totalTramos ? elegidos : new List<int>();
                faltante.Error = "Falta asignar asiento a todos los pasajeros en todos los tramos.";
                return View(faltante);
            }

            // RNP-04: el servidor vuelve a comprobar la disponibilidad real antes de continuar.
            var conflictos = new List<string>();
            for (int t = 0; t < totalTramos; t++)
            {
                var estado = ObtenerAsientos(filas[t].VueloId);
                var usados = new HashSet<int>();
                for (int p = 0; p < pasajeros; p++)
                {
                    int numero = elegidos[p * totalTramos + t];
                    var asiento = estado.FirstOrDefault(x => x.NumeroAsiento == numero);
                    if (asiento == null || asiento.EstadoVisual != "DISPONIBLE" || !usados.Add(numero))
                    {
                        string codigo = asiento == null ? numero.ToString() : asiento.CodigoAsiento;
                        conflictos.Add("El asiento " + codigo + " acaba de ser ocupado en el vuelo " + filas[t].CodigoVuelo + ". Seleccione otro.");
                        elegidos[p * totalTramos + t] = 0;
                    }
                }
            }

            if (conflictos.Count > 0)
            {
                var vm = ConstruirSeleccion(filas, pasajeros, id);
                vm.Previo = elegidos;
                vm.Error = string.Join(" ", conflictos);
                return View(vm);
            }

            var enCurso = new ReservaEnCurso { Pasajeros = pasajeros, Tramos = ids, Asientos = elegidos };
            var previa = ReservaEnCurso.Actual(Session);
            if (previa != null && previa.Pasajeros == pasajeros)
            {
                enCurso.Datos = previa.Datos;   // se conservan los datos ya escritos al regresar a esta pantalla
            }
            Session[ReservaEnCurso.ClaveSesion] = enCurso;
            return RedirectToAction("Pasajeros");
        }

        // ---------------------------------------------------------------------------
        // Pantalla 9. Registro de datos del pasajero (/Reservas/Pasajeros)
        // ---------------------------------------------------------------------------
        [Authorize(Roles = RolesApp.Compradores)]
        public ActionResult Pasajeros()
        {
            var en = ReservaEnCurso.Actual(Session);
            if (en == null || en.Asientos.Count == 0)
            {
                TempData["Error"] = "Seleccione primero los asientos.";
                return RedirectToAction("Buscar", "Vuelos");
            }

            var vm = new PasajerosVM();
            for (int i = 0; i < en.Pasajeros; i++)
            {
                var d = i < en.Datos.Count ? en.Datos[i] : null;
                vm.Pasajeros.Add(d == null ? new DatosPasajeroVM { TipoDocumentoCodigo = "CEDULA" } : new DatosPasajeroVM
                {
                    TipoDocumentoCodigo = d.TipoDocumentoCodigo,
                    NumeroDocumento = d.NumeroDocumento,
                    Nombres = d.Nombres,
                    Apellidos = d.Apellidos,
                    FechaNacimiento = d.FechaNacimiento,
                    Correo = d.Correo,
                    Telefono = d.Telefono
                });
            }
            CompletarPasajeros(vm, en);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Compradores)]
        public ActionResult Pasajeros(PasajerosVM modelo)
        {
            var en = ReservaEnCurso.Actual(Session);
            if (en == null || en.Asientos.Count == 0)
            {
                TempData["Error"] = "Seleccione primero los asientos.";
                return RedirectToAction("Buscar", "Vuelos");
            }

            if (modelo.Pasajeros == null || modelo.Pasajeros.Count != en.Pasajeros)
            {
                modelo.Pasajeros = new List<DatosPasajeroVM>();
                for (int i = 0; i < en.Pasajeros; i++) modelo.Pasajeros.Add(new DatosPasajeroVM { TipoDocumentoCodigo = "CEDULA" });
                ModelState.AddModelError("", "Complete los datos de todos los pasajeros.");
            }
            else
            {
                ValidarPasajeros(modelo.Pasajeros);
            }

            if (!ModelState.IsValid)
            {
                CompletarPasajeros(modelo, en);
                return View(modelo);
            }

            en.Datos = modelo.Pasajeros.Select(p => new DatosPasajero
            {
                TipoDocumentoCodigo = p.TipoDocumentoCodigo,
                NumeroDocumento = p.NumeroDocumento,
                Nombres = p.Nombres,
                Apellidos = p.Apellidos,
                FechaNacimiento = p.FechaNacimiento,
                Correo = string.IsNullOrWhiteSpace(p.Correo) ? null : p.Correo.Trim(),
                Telefono = string.IsNullOrWhiteSpace(p.Telefono) ? null : p.Telefono.Trim()
            }).ToList();
            Session[ReservaEnCurso.ClaveSesion] = en;
            return RedirectToAction("Confirmar");
        }

        // ---------------------------------------------------------------------------
        // Pantalla 10. Reserva de pasajes (/Reservas/Confirmar)
        // ---------------------------------------------------------------------------
        [Authorize(Roles = RolesApp.Compradores)]
        public ActionResult Confirmar()
        {
            var en = ReservaEnCurso.Actual(Session);
            if (!OperacionCompleta(en))
            {
                TempData["Error"] = "No hay una operación de reserva en curso. Busque un vuelo para comenzar.";
                return RedirectToAction("Buscar", "Vuelos");
            }
            return View(ConstruirConfirmacion(en));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Compradores)]
        public ActionResult Confirmar(bool aceptaCondiciones, string accion)
        {
            var en = ReservaEnCurso.Actual(Session);
            if (!OperacionCompleta(en))
            {
                TempData["Error"] = "No hay una operación de reserva en curso. Busque un vuelo para comenzar.";
                return RedirectToAction("Buscar", "Vuelos");
            }

            if (!aceptaCondiciones)
            {
                var vm = ConstruirConfirmacion(en);
                ModelState.AddModelError("AceptaCondiciones", "Debe aceptar las condiciones de la reserva.");
                return View(vm);
            }

            int reservaId;
            string error;
            if (!RegistrarReserva(en, out reservaId, out error))
            {
                // Si la revalidacion falla se vuelve a la pantalla 8 indicando el problema (los asientos se redibujan con el estado real).
                TempData["Error"] = error;
                return RedirectToAction("SeleccionAsiento", new { id = en.Tramos[0], pasajeros = en.Pasajeros, tramos = string.Join(",", en.Tramos) });
            }

            // La operacion ya quedo registrada: se limpia la sesion para que un reenvio no duplique la reserva.
            Session.Remove(ReservaEnCurso.ClaveSesion);

            if (accion == "pagar")
            {
                return RedirectToAction("Pagar", "Compras", new { id = reservaId });
            }
            return RedirectToAction("Confirmacion", "Compras", new { id = reservaId });
        }

        // ---------------------------------------------------------------------------
        // Pantalla 13. Consulta de reservas (/Reservas/MisReservas): solo las propias.
        // ---------------------------------------------------------------------------
        public ActionResult MisReservas(DateTime? desde, DateTime? hasta, string estado, int? pagina)
        {
            var vm = new MisReservasVM { Desde = desde, Hasta = hasta, Estado = estado, Pagina = pagina ?? 1 };

            if (desde.HasValue && hasta.HasValue && hasta.Value < desde.Value)
            {
                ModelState.AddModelError("Hasta", "La fecha final no puede ser anterior a la inicial.");
                return View(vm);
            }

            // Se filtra siempre por el UsuarioId autenticado (no por un parametro de la URL).
            var todas = ConsultarReservas(UsuarioActualId, desde, hasta, estado, null);
            const int porPagina = 10;
            vm.TotalPaginas = Math.Max(1, (int)Math.Ceiling(todas.Count / (double)porPagina));
            if (vm.Pagina < 1) vm.Pagina = 1;
            if (vm.Pagina > vm.TotalPaginas) vm.Pagina = vm.TotalPaginas;
            vm.Reservas = todas.Skip((vm.Pagina - 1) * porPagina).Take(porPagina).ToList();
            vm.Rutas = ObtenerRutas(vm.Reservas.Select(r => r.ReservaId));
            return View(vm);
        }

        // ---------------------------------------------------------------------------
        // Pantalla 14. Gestion de reservas y cancelaciones (/Reservas/Gestionar/{reservaId})
        // ---------------------------------------------------------------------------
        public ActionResult Gestionar(int id)
        {
            var vm = ObtenerDetalle(id);
            if (vm == null)
            {
                TempData["Error"] = "No se encontró la operación solicitada.";
                return RedirectToAction("MisReservas");
            }
            if (!TieneAcceso(vm.Reserva)) return View("AccesoDenegado");
            PrepararCancelacion(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancelar(int id, string motivo)
        {
            var vm = ObtenerDetalle(id);
            if (vm == null)
            {
                TempData["Error"] = "No se encontró la operación solicitada.";
                return RedirectToAction("MisReservas");
            }
            if (!TieneAcceso(vm.Reserva)) return View("AccesoDenegado");

            PrepararCancelacion(vm);
            vm.Motivo = motivo;

            // El estado puede haber cambiado mientras se revisaba: se recarga con el estado actual.
            if (!vm.PuedeCancelar)
            {
                TempData["Error"] = vm.AvisoCancelacion ?? "La reserva no se puede cancelar en su estado actual.";
                return RedirectToAction("Gestionar", new { id = id });
            }

            if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 10 || motivo.Trim().Length > 300)
            {
                ModelState.AddModelError("Motivo", "El motivo debe tener entre 10 y 300 caracteres.");
                return View("Gestionar", vm);
            }

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // Los asientos vuelven a estar disponibles (verde) y la reserva queda CANCELADA.
                    db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.ReservaPasajeroVuelo SET EstadoAsignacionCodigo = 'CANCELADA' " +
                        "WHERE ReservaId = @p0 AND EstadoAsignacionCodigo IN ('PENDIENTE', 'CONFIRMADA')", id);
                    db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.Reserva SET EstadoReservaCodigo = 'CANCELADA' WHERE ReservaId = @p0", id);
                    // Compra vigente: se anula. Sin politica de reembolso aprobada (DP-08) no se genera ningun reembolso.
                    db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.Compra SET EstadoCompraCodigo = 'ANULADA' " +
                        "WHERE ReservaId = @p0 AND EstadoCompraCodigo IN ('PENDIENTE', 'PAGADA')", id);
                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    TempData["Error"] = MensajeBase(ex);
                    return RedirectToAction("Gestionar", new { id = id });
                }
            }

            TempData["Mensaje"] = "Reserva " + vm.Reserva.CodigoReserva + " cancelada.";
            return RedirectToAction("Gestionar", new { id = id });
        }

        // ---------------------------------------------------------------------------
        // Apoyo
        // ---------------------------------------------------------------------------

        private bool TieneAcceso(Reserva reserva)
        {
            return reserva.UsuarioId == UsuarioActualId || EsPersonal;
        }

        private void PrepararCancelacion(DetalleReservaVM vm)
        {
            string estado = vm.Reserva.EstadoReservaCodigo;
            if (estado == "PENDIENTE")
            {
                vm.PuedeCancelar = true;
                vm.AvisoCancelacion = "La cancelación liberará los asientos de la reserva. Esta acción no se puede deshacer.";
            }
            else if (estado == "CONFIRMADA")
            {
                if (Reglas.CancelarReservasPagadas)
                {
                    vm.PuedeCancelar = true;
                    vm.AvisoCancelacion = "La cancelación liberará los asientos. Las condiciones de reembolso están pendientes de aprobación: no se genera ningún reembolso.";
                }
                else
                {
                    vm.PuedeCancelar = false;
                    vm.AvisoCancelacion = "Las condiciones de reembolso están pendientes de aprobación: por ahora no se pueden cancelar reservas ya pagadas.";
                }
            }
            else
            {
                vm.PuedeCancelar = false;
                vm.AvisoCancelacion = "La reserva está " + Fmt.EstadoReserva(estado).ToLower() + " y no admite cancelación.";
            }
        }

        private List<int> ParseTramos(string tramos, int id)
        {
            var lista = new List<int>();
            if (!string.IsNullOrWhiteSpace(tramos))
            {
                foreach (var parte in tramos.Split(','))
                {
                    int n;
                    if (!int.TryParse(parte.Trim(), out n)) return null;
                    lista.Add(n);
                }
            }
            if (lista.Count == 0) lista.Add(id);
            if (lista.Count > 6 || lista.Distinct().Count() != lista.Count) return null;
            return lista;
        }

        // Comprueba las reglas de venta de los tramos elegidos (RNP-01, RNP-05, DP-09).
        private string ValidarTramos(List<int> ids, int pasajeros, out List<TramoRow> filas)
        {
            filas = null;
            if (ids == null || ids.Count == 0) return "No se pudo identificar el vuelo seleccionado.";
            if (pasajeros < 1 || pasajeros > Reglas.CapacidadMaxima)
                return "La cantidad de pasajeros debe estar entre 1 y " + Reglas.CapacidadMaxima + ".";

            var lista = ObtenerTramos(ids);
            if (lista == null) return "El vuelo seleccionado no existe.";

            for (int i = 0; i < lista.Count; i++)
            {
                var t = lista[i];
                if (t.FechaOperacion.Date + t.HoraSalida <= DateTime.Now)
                    return "El vuelo " + t.CodigoVuelo + " ya salió o está por salir.";
                if (!VentaVuelo.Comercializable(t))
                    return "El vuelo " + t.CodigoVuelo + " no está disponible para reservas.";
                if (t.Disponibles < pasajeros)
                    return "El vuelo " + t.CodigoVuelo + " no tiene asientos suficientes para " + pasajeros + " pasajero(s).";
                if (i > 0)
                {
                    var anterior = lista[i - 1];
                    bool continuo = anterior.AeropuertoDestinoId == t.AeropuertoOrigenId
                                    && anterior.FechaOperacion.Date + anterior.HoraLlegada <= t.FechaOperacion.Date + t.HoraSalida;
                    if (!continuo) return "Los tramos del itinerario no son continuos.";
                }
            }

            if (lista.Count == 1 && VentaVuelo.TocaGalapagos(lista[0]) && !Reglas.VentaTramosGalapagos)
                return "Los tramos con Galápagos se comercializan únicamente como itinerario completo.";

            filas = lista;
            return null;
        }

        private SeleccionAsientoVM ConstruirSeleccion(List<TramoRow> filas, int pasajeros, int vueloId)
        {
            var vm = new SeleccionAsientoVM
            {
                Pasajeros = pasajeros,
                VueloId = vueloId,
                TramosCsv = string.Join(",", filas.Select(f => f.VueloId.ToString()))
            };
            foreach (var f in filas)
            {
                vm.Tramos.Add(new TramoMapaVM { Vuelo = f, Asientos = ObtenerAsientos(f.VueloId) });
            }
            return vm;
        }

        private Dictionary<int, string> MapaCodigos(int aeronaveId)
        {
            int modeloId = db.Aeronaves.Where(a => a.AeronaveId == aeronaveId).Select(a => a.ModeloAeronaveId).FirstOrDefault();
            return db.Asientoes.Where(s => s.ModeloAeronaveId == modeloId).ToList().ToDictionary(s => (int)s.NumeroAsiento, s => s.Codigo);
        }

        // Codigo de asiento (1D, 2I...) de cada pasajero y tramo.
        private List<List<string>> CodigosPorPasajero(ReservaEnCurso en, List<TramoRow> tramos)
        {
            var mapas = new List<Dictionary<int, string>>();
            foreach (var t in tramos) mapas.Add(t.AeronaveId.HasValue ? MapaCodigos(t.AeronaveId.Value) : new Dictionary<int, string>());

            var resultado = new List<List<string>>();
            for (int p = 0; p < en.Pasajeros; p++)
            {
                var fila = new List<string>();
                for (int t = 0; t < tramos.Count; t++)
                {
                    int numero = en.Asiento(p, t);
                    string codigo;
                    fila.Add(mapas[t].TryGetValue(numero, out codigo) ? codigo : numero.ToString());
                }
                resultado.Add(fila);
            }
            return resultado;
        }

        private void CompletarPasajeros(PasajerosVM vm, ReservaEnCurso en)
        {
            vm.Tramos = ObtenerTramos(en.Tramos) ?? new List<TramoRow>();
            vm.CodigosAsiento = CodigosPorPasajero(en, vm.Tramos).Select(f => string.Join(" / ", f)).ToList();
            var tipos = db.TipoDocumentoIdentidads.OrderBy(t => t.Codigo).ToList();
            vm.TiposDocumento = new SelectList(tipos, "Codigo", "Descripcion");
        }

        // Validaciones del servidor de la pantalla 9 (se repiten las del cliente).
        private void ValidarPasajeros(List<DatosPasajeroVM> lista)
        {
            var documentos = new HashSet<string>();
            for (int i = 0; i < lista.Count; i++)
            {
                var p = lista[i];
                string prefijo = "Pasajeros[" + i + "].";
                p.NumeroDocumento = (p.NumeroDocumento ?? "").Trim().ToUpperInvariant();
                p.Nombres = (p.Nombres ?? "").Trim();
                p.Apellidos = (p.Apellidos ?? "").Trim();

                if (p.TipoDocumentoCodigo != "CEDULA" && p.TipoDocumentoCodigo != "PASAPORTE")
                {
                    ModelState.AddModelError(prefijo + "TipoDocumentoCodigo", "Seleccione el tipo de documento.");
                }
                else if (p.TipoDocumentoCodigo == "CEDULA" && !Regex.IsMatch(p.NumeroDocumento, "^[0-9]{10}$"))
                {
                    ModelState.AddModelError(prefijo + "NumeroDocumento", "La cédula debe tener 10 dígitos.");
                }
                else if (p.TipoDocumentoCodigo == "PASAPORTE" && !Regex.IsMatch(p.NumeroDocumento, "^[A-Z0-9]{5,20}$"))
                {
                    ModelState.AddModelError(prefijo + "NumeroDocumento", "El pasaporte debe tener entre 5 y 20 letras o números.");
                }
                else if (!documentos.Add(p.TipoDocumentoCodigo + ":" + p.NumeroDocumento))
                {
                    ModelState.AddModelError(prefijo + "NumeroDocumento", "Este documento ya está en la reserva.");
                }

                if (p.Nombres.Length > 0 && !Regex.IsMatch(p.Nombres, @"^[\p{L} .'\-]+$"))
                    ModelState.AddModelError(prefijo + "Nombres", "Use solo letras y espacios.");
                if (p.Apellidos.Length > 0 && !Regex.IsMatch(p.Apellidos, @"^[\p{L} .'\-]+$"))
                    ModelState.AddModelError(prefijo + "Apellidos", "Use solo letras y espacios.");

                if (p.FechaNacimiento.HasValue && (p.FechaNacimiento.Value.Date <= new DateTime(1900, 1, 1) || p.FechaNacimiento.Value.Date > DateTime.Today))
                    ModelState.AddModelError(prefijo + "FechaNacimiento", "La fecha de nacimiento debe ser posterior a 01/01/1900 y no futura.");

                if (!string.IsNullOrWhiteSpace(p.Telefono) && !Regex.IsMatch(p.Telefono.Trim(), @"^[0-9+ ]{1,20}$"))
                    ModelState.AddModelError(prefijo + "Telefono", "Use solo dígitos, espacios y «+».");
            }
        }

        private bool OperacionCompleta(ReservaEnCurso en)
        {
            if (en == null || en.Tramos.Count == 0 || en.Pasajeros < 1) return false;
            if (en.Asientos.Count != en.Pasajeros * en.Tramos.Count || en.Asientos.Any(a => a < 1)) return false;
            if (en.Datos.Count != en.Pasajeros) return false;
            return true;
        }

        private ConfirmarReservaVM ConstruirConfirmacion(ReservaEnCurso en)
        {
            var vm = new ConfirmarReservaVM();
            vm.Tramos = ObtenerTramos(en.Tramos) ?? new List<TramoRow>();
            vm.CodigosAsiento = CodigosPorPasajero(en, vm.Tramos);
            vm.HorasRetencion = Reglas.HorasRetencionReserva;
            foreach (var d in en.Datos)
            {
                vm.Pasajeros.Add(new DatosPasajeroVM
                {
                    TipoDocumentoCodigo = d.TipoDocumentoCodigo,
                    NumeroDocumento = d.NumeroDocumento,
                    Nombres = d.Nombres,
                    Apellidos = d.Apellidos
                });
                // El importe de cada tramo es la tarifa referencial de la ruta (valor de ejemplo, DP-04).
                vm.ImportePasajero.Add(vm.Tramos.Sum(t => t.TarifaReferencial));
            }
            vm.Total = vm.ImportePasajero.Sum();
            return vm;
        }

        // Registra la reserva en una sola transaccion: si un asiento falla no se guarda nada.
        private bool RegistrarReserva(ReservaEnCurso en, out int reservaId, out string error)
        {
            reservaId = 0;
            error = null;
            int pax = en.Pasajeros;
            int totalTramos = en.Tramos.Count;

            List<TramoRow> filas;
            string problema = ValidarTramos(en.Tramos, pax, out filas);
            if (problema != null)
            {
                error = problema;
                return false;
            }

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // RNP-04: revalidar la disponibilidad de cada asiento elegido.
                    for (int t = 0; t < totalTramos; t++)
                    {
                        var estado = ObtenerAsientos(filas[t].VueloId);
                        for (int p = 0; p < pax; p++)
                        {
                            int numero = en.Asiento(p, t);
                            var asiento = estado.FirstOrDefault(x => x.NumeroAsiento == numero);
                            if (asiento == null || asiento.EstadoVisual != "DISPONIBLE")
                            {
                                tx.Rollback();
                                error = "El asiento " + (asiento == null ? numero.ToString() : asiento.CodigoAsiento) + " del vuelo " + filas[t].CodigoVuelo + " ya no está disponible. Seleccione otro.";
                                return false;
                            }
                        }
                    }

                    // Pasajeros: si el documento ya existe se reutiliza el registro (unicidad TipoDocumento + Numero).
                    var pasajeroIds = new List<int>();
                    foreach (var d in en.Datos)
                    {
                        string tipo = d.TipoDocumentoCodigo;
                        string numero = d.NumeroDocumento;
                        var existente = db.Pasajeroes.FirstOrDefault(x => x.TipoDocumentoCodigo == tipo && x.NumeroDocumento == numero);
                        if (existente == null)
                        {
                            existente = new Pasajero
                            {
                                TipoDocumentoCodigo = tipo,
                                NumeroDocumento = numero,
                                Nombres = d.Nombres,
                                Apellidos = d.Apellidos,
                                FechaNacimiento = d.FechaNacimiento,
                                Correo = d.Correo,
                                Telefono = d.Telefono,
                                FechaRegistro = DateTime.Now
                            };
                            db.Pasajeroes.Add(existente);
                            db.SaveChanges();
                        }
                        pasajeroIds.Add(existente.PasajeroId);
                    }

                    // Reserva PENDIENTE con fecha de expiracion segun la politica de retencion (DP-07, parametrizable).
                    var ahora = DateTime.Now;
                    var reserva = new Reserva
                    {
                        UsuarioId = UsuarioActualId,
                        FechaCreacion = ahora,
                        FechaExpiracion = ahora.AddHours(Reglas.HorasRetencionReserva),
                        EstadoReservaCodigo = "PENDIENTE"
                    };
                    db.Reservas.Add(reserva);
                    db.SaveChanges();

                    // Asignaciones PENDIENTE (morado) por pasajero y tramo; el importe se copia de la tarifa vigente.
                    for (int p = 0; p < pax; p++)
                    {
                        for (int t = 0; t < totalTramos; t++)
                        {
                            db.ReservaPasajeroVueloes.Add(new ReservaPasajeroVuelo
                            {
                                ReservaId = reserva.ReservaId,
                                PasajeroId = pasajeroIds[p],
                                VueloId = filas[t].VueloId,
                                NumeroAsiento = (byte)en.Asiento(p, t),
                                ImporteTramo = filas[t].TarifaReferencial,
                                EstadoAsignacionCodigo = "PENDIENTE",
                                FechaAsignacion = ahora
                            });
                        }
                    }
                    db.SaveChanges();
                    tx.Commit();
                    reservaId = reserva.ReservaId;
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    error = MensajeBase(ex);
                    return false;
                }
            }
        }
    }
}

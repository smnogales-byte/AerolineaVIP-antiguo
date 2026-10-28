using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    // Pantallas 17 a 21: area administrativa. La autorizacion se valida en el servidor (RNP-09).
    [Authorize(Roles = RolesApp.Operativos + "," + RolesApp.Comercial + "," + RolesApp.Counter)]
    public class AdminController : BaseController
    {
        // ---------------------------------------------------------------------------
        // Pantalla 17. Panel del administrador (solo accesos a los modulos, sin dashboard)
        // ---------------------------------------------------------------------------
        public ActionResult Index()
        {
            ViewBag.Vuelos = db.Vueloes.Count();
            ViewBag.VuelosSinAeronave = db.Vueloes.Count(v => v.AeronaveId == null && v.EstadoVueloCodigo != "CANCELADO");
            ViewBag.Aeronaves = db.Aeronaves.Count();
            ViewBag.ReservasPendientes = db.Reservas.Count(r => r.EstadoReservaCodigo == "PENDIENTE");
            ViewBag.ReservasConfirmadas = db.Reservas.Count(r => r.EstadoReservaCodigo == "CONFIRMADA");
            using (var idb = new ApplicationDbContext()) { ViewBag.Usuarios = idb.Users.Count(); }
            return View();
        }

        // ---------------------------------------------------------------------------
        // Pantalla 18. Administracion de vuelos e itinerarios
        // ---------------------------------------------------------------------------
        [Authorize(Roles = RolesApp.Operativos)]
        public ActionResult Vuelos(string pestana, DateTime? desde, DateTime? hasta, int? rutaId, string estado, bool verConflictos = false)
        {
            var vm = new AdminVuelosVM
            {
                Pestana = string.IsNullOrEmpty(pestana) ? "vuelos" : pestana,
                Desde = desde ?? DateTime.Today,
                Hasta = hasta,
                RutaId = rutaId,
                Estado = estado,
                VerConflictos = verConflictos
            };
            CargarCombosVuelos(vm);

            if (vm.Pestana == "frecuencias")
            {
                vm.Frecuencias = db.Frecuencias.Include("Ruta").OrderBy(f => f.RutaId).ThenBy(f => f.DiaSemana).ThenBy(f => f.HoraSalida).ToList();
            }
            else if (vm.Pestana == "itinerarios")
            {
                vm.Itinerarios = db.Itinerarios.Include("TramoItinerarios").OrderBy(i => i.Referencia).ToList();
            }
            else
            {
                var sql = SqlTramos + "WHERE v.FechaOperacion >= @p0 ";
                var p = new List<object> { vm.Desde.Value.Date };
                if (hasta.HasValue) { sql += "AND v.FechaOperacion <= @p" + p.Count + " "; p.Add(hasta.Value.Date); }
                if (rutaId.HasValue) { sql += "AND v.RutaId = @p" + p.Count + " "; p.Add(rutaId.Value); }
                if (!string.IsNullOrEmpty(estado)) { sql += "AND v.EstadoVueloCodigo = @p" + p.Count + " "; p.Add(estado); }
                sql += "ORDER BY v.FechaOperacion, v.HoraSalida, v.VueloId";
                vm.Vuelos = db.Database.SqlQuery<TramoRow>(sql, p.ToArray()).Take(300).ToList();
                if (verConflictos) vm.Conflictos = BuscarConflictos();
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Operativos)]
        public ActionResult GenerarVuelos(DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (!fechaInicio.HasValue || !fechaFin.HasValue)
            {
                TempData["Error"] = "Indique la fecha inicial y la fecha final.";
            }
            else if (fechaFin.Value < fechaInicio.Value)
            {
                TempData["Error"] = "La fecha final no puede ser anterior a la inicial.";
            }
            else if ((fechaFin.Value - fechaInicio.Value).TotalDays > 365)
            {
                TempData["Error"] = "El rango no puede superar 366 días.";
            }
            else
            {
                try
                {
                    int antes = db.Vueloes.Count();
                    db.Database.ExecuteSqlCommand("EXEC dbo.usp_GenerarVuelos @p0, @p1", fechaInicio.Value.Date, fechaFin.Value.Date);
                    int despues = db.Vueloes.Count();
                    TempData["Mensaje"] = "Se generaron " + (despues - antes) + " vuelo(s) nuevo(s). Los vuelos existentes no se duplican.";
                }
                catch (Exception ex) { TempData["Error"] = MensajeBase(ex); }
            }
            return RedirectToAction("Vuelos");
        }

        [Authorize(Roles = RolesApp.Operativos)]
        public ActionResult EditarVuelo(int id)
        {
            var vm = ConstruirEdicion(id);
            if (vm == null) return HttpNotFound();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Operativos)]
        public ActionResult EditarVuelo(EditarVueloVM modelo)
        {
            var vuelo = db.Vueloes.Find(modelo.VueloId);
            if (vuelo == null) return HttpNotFound();
            if (!db.EstadoVueloes.Any(e => e.Codigo == modelo.EstadoVueloCodigo))
                ModelState.AddModelError("EstadoVueloCodigo", "Seleccione un estado válido.");
            if (modelo.AeronaveId.HasValue)
            {
                var aeronave = db.Aeronaves.Find(modelo.AeronaveId.Value);
                if (aeronave == null) ModelState.AddModelError("AeronaveId", "La aeronave no existe.");
                else if (aeronave.EstadoAeronaveCodigo != "DISPONIBLE") ModelState.AddModelError("AeronaveId", "La aeronave no está disponible para asignaciones.");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    // La base valida con triggers que la aeronave no se solape ni cambie con reservas existentes.
                    vuelo.AeronaveId = modelo.AeronaveId;
                    vuelo.EstadoVueloCodigo = modelo.EstadoVueloCodigo;
                    db.SaveChanges();
                    TempData["Mensaje"] = "Vuelo actualizado.";
                    return RedirectToAction("Vuelos");
                }
                catch (Exception ex) { ModelState.AddModelError("", MensajeBase(ex)); }
            }
            var vm = ConstruirEdicion(modelo.VueloId);
            vm.AeronaveId = modelo.AeronaveId;
            vm.EstadoVueloCodigo = modelo.EstadoVueloCodigo;
            vm.Aeronaves = new SelectList(db.Aeronaves.OrderBy(a => a.Matricula).ToList(), "AeronaveId", "Matricula", modelo.AeronaveId);
            vm.Estados = new SelectList(db.EstadoVueloes.OrderBy(e => e.Codigo).ToList(), "Codigo", "Descripcion", modelo.EstadoVueloCodigo);
            return View(vm);
        }

        // ---------------------------------------------------------------------------
        // Pantalla 19. Administracion de aeronaves
        // ---------------------------------------------------------------------------
        [Authorize(Roles = RolesApp.Operativos)]
        public ActionResult Aeronaves(int? id)
        {
            var vm = new AdminAeronavesVM();
            var naves = db.Aeronaves.Include("ModeloAeronave").OrderBy(a => a.Matricula).ToList();
            foreach (var a in naves)
            {
                int aid = a.AeronaveId;
                DateTime hoy = DateTime.Today;
                vm.Flota.Add(new FilaAeronaveVM
                {
                    Aeronave = a,
                    Modelo = a.ModeloAeronave.Fabricante + " " + a.ModeloAeronave.Nombre,
                    Capacidad = a.ModeloAeronave.CapacidadAsientos,
                    VuelosProximos = db.Vueloes.Count(v => v.AeronaveId == aid && v.FechaOperacion >= hoy && v.EstadoVueloCodigo != "CANCELADO")
                });
            }
            var seleccion = id.HasValue ? naves.FirstOrDefault(a => a.AeronaveId == id.Value) : naves.FirstOrDefault();
            if (seleccion != null)
            {
                vm.SeleccionadaId = seleccion.AeronaveId;
                vm.SeleccionadaMatricula = seleccion.Matricula;
                var filas = db.Database.SqlQuery<AsientoAeronaveRow>(
                    "SELECT AeronaveId, Matricula, NumeroAsiento, CodigoAsiento, Fila, Lado, Ubicacion FROM dbo.vw_AsientoAeronave WHERE AeronaveId = @p0 ORDER BY NumeroAsiento",
                    seleccion.AeronaveId).ToList();
                vm.Distribucion = filas.Select(f => new EstadoAsientoRow
                {
                    AeronaveId = f.AeronaveId, NumeroAsiento = f.NumeroAsiento, CodigoAsiento = f.CodigoAsiento,
                    Fila = f.Fila, Lado = f.Lado, Ubicacion = f.Ubicacion, EstadoVisual = "DISPONIBLE", ColorEstado = "Verde"
                }).ToList();
                vm.Asignaciones = db.Database.SqlQuery<TramoRow>(
                    SqlTramos + "WHERE v.AeronaveId = @p0 AND v.FechaOperacion >= @p1 ORDER BY v.FechaOperacion, v.HoraSalida",
                    seleccion.AeronaveId, DateTime.Today).Take(40).ToList();
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Administrativos)]
        public ActionResult CambiarEstadoAeronave(int id)
        {
            var a = db.Aeronaves.Find(id);
            if (a == null) return HttpNotFound();
            string nuevo = a.EstadoAeronaveCodigo == "DISPONIBLE" ? "NO_DISPONIBLE" : "DISPONIBLE";
            try
            {
                a.EstadoAeronaveCodigo = nuevo;
                db.SaveChanges();
                TempData["Mensaje"] = "La aeronave " + a.Matricula + " ahora está " + Fmt.EstadoAeronave(nuevo).ToLower() + ".";
            }
            catch (Exception ex) { TempData["Error"] = MensajeBase(ex); }
            return RedirectToAction("Aeronaves", new { id = id });
        }

        // ---------------------------------------------------------------------------
        // Pantalla 20. Gestion de usuarios, pasajeros y reservas
        // ---------------------------------------------------------------------------
        [Authorize(Roles = RolesApp.Personal)]
        public ActionResult Gestion(string pestana, string buscar, string rol, string estado)
        {
            var vm = new AdminGestionVM
            {
                Pestana = string.IsNullOrEmpty(pestana) ? "usuarios" : pestana,
                Buscar = buscar,
                Rol = rol,
                Estado = estado,
                EsSuperUsuario = User.IsInRole(RolesApp.SuperUsuario)
            };
            using (var idb = new ApplicationDbContext())
            {
                var roles = idb.Roles.OrderBy(r => r.Name).ToList();
                vm.RolesLista = new SelectList(roles, "Name", "Name", rol);
                if (vm.Pestana == "usuarios")
                {
                    var nombres = roles.ToDictionary(r => r.Id, r => r.Name);
                    var usuarios = idb.Users.ToList();
                    if (!string.IsNullOrWhiteSpace(buscar))
                    {
                        string b = buscar.Trim().ToLower();
                        usuarios = usuarios.Where(u => (u.Email ?? "").ToLower().Contains(b) || (u.UserName ?? "").ToLower().Contains(b)).ToList();
                    }
                    vm.Usuarios = usuarios.Select(u => new UsuarioFilaVM
                    {
                        Id = u.Id, UserName = u.UserName, Email = u.Email,
                        Roles = u.Roles.Select(r => nombres[r.RoleId]).OrderBy(n => n).ToList(),
                        Bloqueado = u.LockoutEnabled && u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc.Value > DateTime.UtcNow
                    }).Where(u => string.IsNullOrEmpty(rol) || u.Roles.Contains(rol)).OrderBy(u => u.Email).ToList();
                }
            }
            if (vm.Pestana == "pasajeros")
            {
                var q = db.Pasajeroes.AsQueryable();
                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    string b = buscar.Trim();
                    q = q.Where(p => p.NumeroDocumento.Contains(b) || p.Nombres.Contains(b) || p.Apellidos.Contains(b));
                }
                vm.Pasajeros = q.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).Take(200).ToList();
            }
            else if (vm.Pestana == "reservas")
            {
                vm.Reservas = ConsultarReservas(null, null, null, estado, buscar).Take(200).ToList();
                vm.RutasReservas = ObtenerRutas(vm.Reservas.Select(r => r.ReservaId));
            }
            return View(vm);
        }

        // Asignacion de roles: solo el SuperUsuario (matriz 16.1). Un visitante nunca llega aqui.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult AsignarRoles(string id, string[] roles)
        {
            roles = roles ?? new string[0];
            using (var idb = new ApplicationDbContext())
            {
                var um = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(idb));
                var rm = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(idb));
                var usuario = um.FindById(id);
                if (usuario == null) return HttpNotFound();

                var validos = rm.Roles.Select(r => r.Name).ToList();
                var nuevos = roles.Where(r => validos.Contains(r)).Distinct().ToList();
                if (nuevos.Count == 0)
                {
                    TempData["Error"] = "El usuario debe conservar al menos un rol.";
                    return RedirectToAction("Gestion");
                }
                // El superusuario no puede quitarse a si mismo ese rol (evita dejar el sistema sin superusuario).
                if (id == UsuarioActualId && !nuevos.Contains(RolesApp.SuperUsuario))
                {
                    TempData["Error"] = "No puede quitarse a sí mismo el rol SuperUsuario.";
                    return RedirectToAction("Gestion");
                }
                var actuales = um.GetRoles(id).ToList();
                foreach (var r in actuales.Where(r => !nuevos.Contains(r))) um.RemoveFromRole(id, r);
                foreach (var r in nuevos.Where(r => !actuales.Contains(r))) um.AddToRole(id, r);
                TempData["Mensaje"] = "Roles actualizados para " + usuario.Email + ".";
            }
            return RedirectToAction("Gestion");
        }

        // ---------------------------------------------------------------------------
        // Pantalla 21. Consultas y reportes administrativos
        // ---------------------------------------------------------------------------
        [Authorize(Roles = RolesApp.Administrativos + "," + RolesApp.Comercial + "," + RolesApp.Operaciones)]
        public ActionResult Reportes(ReportesVM filtros, bool ejecutar = false)
        {
            var vm = filtros ?? new ReportesVM();
            if (string.IsNullOrEmpty(vm.Consulta)) vm.Consulta = "ocupacion";
            CargarCombosReporte(vm);
            if (ejecutar) Ejecutar(vm);
            return View(vm);
        }

        [Authorize(Roles = RolesApp.Administrativos + "," + RolesApp.Comercial + "," + RolesApp.Operaciones)]
        public ActionResult ExportarCsv(ReportesVM filtros)
        {
            var vm = filtros ?? new ReportesVM();
            if (string.IsNullOrEmpty(vm.Consulta)) vm.Consulta = "ocupacion";
            Ejecutar(vm);
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", vm.Columnas.Select(Csv)));
            foreach (var f in vm.Filas)
            {
                var celdas = new[] { f.Col1, f.Col2, f.Col3, f.Col4, f.Col5, f.Col6, f.Col7, f.Col8 }.Take(vm.Columnas.Length);
                sb.AppendLine(string.Join(",", celdas.Select(Csv)));
            }
            var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", "reporte_" + vm.Consulta + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv");
        }

        // ---------------------------------------------------------------------------
        // Apoyo
        // ---------------------------------------------------------------------------
        private static string Csv(string valor)
        {
            valor = valor ?? "";
            // Evita que una celda se interprete como formula al abrir el CSV en una hoja de calculo.
            if (valor.Length > 0 && "=+-@".IndexOf(valor[0]) >= 0) valor = "'" + valor;
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }

        private void CargarCombosVuelos(AdminVuelosVM vm)
        {
            var rutas = db.Database.SqlQuery<RutaCombo>(
                "SELECT r.RutaId, ao.Localidad + ' → ' + ad.Localidad AS Texto FROM dbo.Ruta r " +
                "JOIN dbo.Aeropuerto ao ON ao.AeropuertoId = r.AeropuertoOrigenId " +
                "JOIN dbo.Aeropuerto ad ON ad.AeropuertoId = r.AeropuertoDestinoId ORDER BY Texto").ToList();
            vm.Rutas = new SelectList(rutas, "RutaId", "Texto", vm.RutaId);
            vm.Estados = new SelectList(db.EstadoVueloes.OrderBy(e => e.Codigo).ToList(), "Codigo", "Descripcion", vm.Estado);
        }

        private void CargarCombosReporte(ReportesVM vm)
        {
            var rutas = db.Database.SqlQuery<RutaCombo>(
                "SELECT r.RutaId, ao.Localidad + ' → ' + ad.Localidad AS Texto FROM dbo.Ruta r " +
                "JOIN dbo.Aeropuerto ao ON ao.AeropuertoId = r.AeropuertoOrigenId " +
                "JOIN dbo.Aeropuerto ad ON ad.AeropuertoId = r.AeropuertoDestinoId ORDER BY Texto").ToList();
            vm.Rutas = new SelectList(rutas, "RutaId", "Texto", vm.RutaId);
            vm.Aeronaves = new SelectList(db.Aeronaves.OrderBy(a => a.Matricula).ToList(), "AeronaveId", "Matricula", vm.AeronaveId);
        }

        private EditarVueloVM ConstruirEdicion(int id)
        {
            var t = ObtenerTramo(id);
            if (t == null) return null;
            return new EditarVueloVM
            {
                VueloId = t.VueloId,
                CodigoVuelo = t.CodigoVuelo,
                Ruta = t.CiudadOrigen + " → " + t.CiudadDestino,
                Fecha = t.FechaOperacion,
                Horario = Fmt.Hora(t.HoraSalida) + " - " + Fmt.Hora(t.HoraLlegada),
                AeronaveId = t.AeronaveId,
                EstadoVueloCodigo = t.EstadoVueloCodigo,
                Aeronaves = new SelectList(db.Aeronaves.OrderBy(a => a.Matricula).ToList(), "AeronaveId", "Matricula", t.AeronaveId),
                Estados = new SelectList(db.EstadoVueloes.OrderBy(e => e.Codigo).ToList(), "Codigo", "Descripcion", t.EstadoVueloCodigo)
            };
        }

        // Vuelos de una misma aeronave que se solapan el mismo dia (DP-02: sin tiempo de preparacion definido).
        private List<ConflictoRow> BuscarConflictos()
        {
            return db.Database.SqlQuery<ConflictoRow>(
                "SELECT a.Matricula, va.CodigoVuelo AS CodigoVueloA, vb.CodigoVuelo AS CodigoVueloB, va.FechaOperacion AS FechaA, " +
                "va.HoraSalida AS SalidaA, va.HoraLlegada AS LlegadaA, vb.HoraSalida AS SalidaB, vb.HoraLlegada AS LlegadaB " +
                "FROM dbo.vw_VueloDetalle va " +
                "JOIN dbo.vw_VueloDetalle vb ON vb.AeronaveId = va.AeronaveId AND vb.FechaOperacion = va.FechaOperacion AND vb.VueloId > va.VueloId " +
                "JOIN dbo.Aeronave a ON a.AeronaveId = va.AeronaveId " +
                "WHERE va.EstadoVueloCodigo <> 'CANCELADO' AND vb.EstadoVueloCodigo <> 'CANCELADO' " +
                "AND va.HoraSalida < vb.HoraLlegada AND vb.HoraSalida < va.HoraLlegada " +
                "ORDER BY va.FechaOperacion, a.Matricula").ToList();
        }

        private void Ejecutar(ReportesVM vm)
        {
            vm.Ejecutada = true;
            vm.Corte = DateTime.Now;
            if (vm.Desde.HasValue && vm.Hasta.HasValue && vm.Hasta.Value < vm.Desde.Value)
            {
                vm.Mensaje = "La fecha hasta no puede ser anterior a la fecha desde.";
                vm.Ejecutada = false;
                return;
            }
            var es = new CultureInfo("es-EC");
            if (vm.Consulta == "reservas")
            {
                vm.Columnas = new[] { "Código", "Creada", "Fecha de viaje", "Pasajeros", "Estado reserva", "Estado compra", "Total (USD)" };
                var lista = ConsultarReservas(null, vm.Desde, vm.Hasta, vm.Estado, null);
                vm.Filas = lista.Select(r => new ReporteFila
                {
                    Col1 = r.CodigoReserva,
                    Col2 = Fmt.FechaHora(r.FechaCreacion),
                    Col3 = r.FechaViaje.HasValue ? Fmt.Fecha(r.FechaViaje.Value) : "",
                    Col4 = r.Pasajeros.ToString(),
                    Col5 = Fmt.EstadoReserva(r.EstadoReservaCodigo),
                    Col6 = string.IsNullOrEmpty(r.EstadoCompra) ? "Sin compra" : Fmt.EstadoCompra(r.EstadoCompra),
                    Col7 = r.Total.ToString("0.00", CultureInfo.InvariantCulture)
                }).ToList();
            }
            else if (vm.Consulta == "compras")
            {
                vm.Columnas = new[] { "Compra", "Reserva", "Fecha", "Importe", "Moneda", "Estado compra", "Operación de pago" };
                var sql = "SELECT c.CompraId, r.CodigoReserva, c.FechaCompra, c.Importe, c.MonedaCodigo, c.EstadoCompraCodigo, " +
                          "(SELECT TOP 1 p.CodigoOperacion FROM dbo.Pago p WHERE p.CompraId = c.CompraId ORDER BY p.PagoId DESC) AS CodigoOperacion " +
                          "FROM dbo.Compra c JOIN dbo.Reserva r ON r.ReservaId = c.ReservaId WHERE 1 = 1 ";
                var p = new List<object>();
                if (vm.Desde.HasValue) { sql += "AND c.FechaCompra >= @p" + p.Count + " "; p.Add(vm.Desde.Value.Date); }
                if (vm.Hasta.HasValue) { sql += "AND c.FechaCompra < @p" + p.Count + " "; p.Add(vm.Hasta.Value.Date.AddDays(1)); }
                if (!string.IsNullOrEmpty(vm.Estado)) { sql += "AND c.EstadoCompraCodigo = @p" + p.Count + " "; p.Add(vm.Estado); }
                sql += "ORDER BY c.CompraId DESC";
                var filas = db.Database.SqlQuery<CompraReporteRow>(sql, p.ToArray()).ToList();
                vm.Filas = filas.Select(c => new ReporteFila
                {
                    Col1 = c.CompraId.ToString(),
                    Col2 = c.CodigoReserva,
                    Col3 = Fmt.FechaHora(c.FechaCompra),
                    Col4 = c.Importe.ToString("0.00", CultureInfo.InvariantCulture),
                    Col5 = c.MonedaCodigo,
                    Col6 = Fmt.EstadoCompra(c.EstadoCompraCodigo),
                    Col7 = c.CodigoOperacion ?? ""
                }).ToList();
            }
            else
            {
                vm.Consulta = "ocupacion";
                vm.Columnas = new[] { "Vuelo", "Fecha", "Ruta", "Horario", "Aeronave", "Capacidad", "Ocupados", "Ocupación" };
                var sql = SqlTramos + "WHERE 1 = 1 ";
                var p = new List<object>();
                if (vm.Desde.HasValue) { sql += "AND v.FechaOperacion >= @p" + p.Count + " "; p.Add(vm.Desde.Value.Date); }
                if (vm.Hasta.HasValue) { sql += "AND v.FechaOperacion <= @p" + p.Count + " "; p.Add(vm.Hasta.Value.Date); }
                if (vm.RutaId.HasValue) { sql += "AND v.RutaId = @p" + p.Count + " "; p.Add(vm.RutaId.Value); }
                if (vm.AeronaveId.HasValue) { sql += "AND v.AeronaveId = @p" + p.Count + " "; p.Add(vm.AeronaveId.Value); }
                sql += "ORDER BY v.FechaOperacion, v.HoraSalida";
                var vuelos = db.Database.SqlQuery<TramoRow>(sql, p.ToArray()).Take(500).ToList();
                vm.Filas = vuelos.Select(v => new ReporteFila
                {
                    Col1 = v.CodigoVuelo,
                    Col2 = Fmt.Fecha(v.FechaOperacion),
                    Col3 = v.CiudadOrigen + " → " + v.CiudadDestino,
                    Col4 = Fmt.Hora(v.HoraSalida) + " - " + Fmt.Hora(v.HoraLlegada),
                    Col5 = v.Matricula ?? "Sin asignar",
                    Col6 = v.Capacidad.ToString(),
                    Col7 = v.Ocupados.ToString(),
                    Col8 = v.Capacidad > 0 ? Math.Round(100.0 * v.Ocupados / v.Capacidad, 0).ToString(CultureInfo.InvariantCulture) + " %" : "—"
                }).ToList();
            }
            if (vm.Filas.Count == 0) vm.Mensaje = "No hay datos para los filtros seleccionados.";
        }
    }

    public class RutaCombo
    {
        public int RutaId { get; set; }
        public string Texto { get; set; }
    }

    public class CompraReporteRow
    {
        public int CompraId { get; set; }
        public string CodigoReserva { get; set; }
        public DateTime FechaCompra { get; set; }
        public decimal Importe { get; set; }
        public string MonedaCodigo { get; set; }
        public string EstadoCompraCodigo { get; set; }
        public string CodigoOperacion { get; set; }
    }
}

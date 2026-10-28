using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    // Pantallas 5, 6 y 7: busqueda de vuelos, resultados y disponibilidad de asientos.
    // Acceso publico (visitante anonimo): solo se consulta, nunca se reserva desde aqui.
    [AllowAnonymous]
    public class VuelosController : BaseController
    {
        private const string SqlItinerarios =
            "SELECT t.ItinerarioId, i.Referencia, i.Nombre AS ItinerarioNombre, t.Orden, " +
            "v.VueloId, v.CodigoVuelo, v.FechaOperacion, v.DiaSemana, v.RutaId, v.TarifaReferencial, " +
            "v.AeropuertoOrigenId, v.AeropuertoDestinoId, v.Origen, v.CiudadOrigen, v.Destino, v.CiudadDestino, " +
            "v.HoraSalida, v.HoraLlegada, v.AeronaveId, v.EstadoVueloCodigo, " +
            "d.Matricula, d.Capacidad, d.Ocupados, d.Disponibles " +
            "FROM dbo.TramoItinerario t " +
            "JOIN dbo.Itinerario i ON i.ItinerarioId = t.ItinerarioId AND i.EstadoItinerarioCodigo = 'ACTIVO' " +
            "JOIN dbo.vw_VueloDetalle v ON v.VueloId = t.VueloId " +
            "JOIN dbo.vw_DisponibilidadVuelo d ON d.VueloId = v.VueloId ";

        // ---------------------------------------------------------------------------
        // Pantalla 5. Busqueda de vuelos (/Vuelos/Buscar)
        // ---------------------------------------------------------------------------
        public ActionResult Buscar(int? origenId, int? destinoId, DateTime? fecha, int? pasajeros)
        {
            var vm = new BusquedaVM
            {
                OrigenId = origenId,
                DestinoId = destinoId,
                Fecha = fecha,
                Pasajeros = pasajeros ?? 1
            };
            Preparar(vm);
            return View(vm);
        }

        // Destinos que se pueden alcanzar desde un origen (para el selector dependiente, jQuery).
        public JsonResult Destinos(int origenId)
        {
            var ids = DestinosAlcanzables(origenId);
            var lista = db.Aeropuertoes
                .Where(a => a.Activo && ids.Contains(a.AeropuertoId))
                .OrderBy(a => a.Localidad)
                .ToList()
                .Select(a => new { id = a.AeropuertoId, texto = a.Localidad + " (" + a.CodigoIATA + ")" })
                .ToList();
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        // ---------------------------------------------------------------------------
        // Pantalla 6. Resultados (/Vuelos/Resultados)
        // ---------------------------------------------------------------------------
        public ActionResult Resultados(int? origenId, int? destinoId, DateTime? fecha, int? pasajeros, string horario)
        {
            string error = ValidarBusqueda(origenId, destinoId, fecha, pasajeros);
            if (error != null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Buscar", new
                {
                    origenId = origenId,
                    destinoId = destinoId,
                    fecha = fecha.HasValue ? fecha.Value.ToString("yyyy-MM-dd") : null,
                    pasajeros = pasajeros
                });
            }

            int o = origenId.Value;
            int d = destinoId.Value;
            DateTime dia = fecha.Value.Date;
            int pax = pasajeros.Value;

            var vm = new ResultadosVM();
            vm.Busqueda = new BusquedaVM { OrigenId = o, DestinoId = d, Fecha = dia, Pasajeros = pax, Horario = string.IsNullOrEmpty(horario) ? "Todos" : horario };
            Preparar(vm.Busqueda);
            vm.OrigenCiudad = db.Aeropuertoes.Where(a => a.AeropuertoId == o).Select(a => a.Localidad).FirstOrDefault();
            vm.DestinoCiudad = db.Aeropuertoes.Where(a => a.AeropuertoId == d).Select(a => a.Localidad).FirstOrDefault();

            // 1) Vuelos directos de la ruta en esa fecha.
            var directos = db.Database.SqlQuery<TramoRow>(
                SqlTramos + "WHERE v.AeropuertoOrigenId = @p0 AND v.AeropuertoDestinoId = @p1 AND v.FechaOperacion = @p2 ORDER BY v.HoraSalida",
                o, d, dia).ToList();

            foreach (var t in directos)
            {
                // DP-09: los tramos con Galapagos no se venden sueltos salvo que la politica lo permita.
                if (VentaVuelo.TocaGalapagos(t) && !Reglas.VentaTramosGalapagos)
                {
                    vm.Aviso = "Los tramos con Galápagos se comercializan únicamente como itinerario completo desde Quito.";
                    continue;
                }
                vm.Tarjetas.Add(CrearTarjeta(new List<TramoRow> { t }, false, pax));
            }

            // 2) Itinerarios con escala (por ejemplo Quito - Guayaquil - Galapagos) que cubren origen y destino.
            var filas = db.Database.SqlQuery<TramoItinerarioRow>(
                SqlItinerarios + "WHERE v.FechaOperacion = @p0 ORDER BY t.ItinerarioId, t.Orden", dia).ToList();

            foreach (var g in filas.GroupBy(f => f.ItinerarioId))
            {
                var lista = g.OrderBy(f => f.Orden).ToList();
                bool encontrado = false;
                for (int a = 0; a < lista.Count && !encontrado; a++)
                {
                    if (lista[a].AeropuertoOrigenId != o) continue;
                    for (int b = a + 1; b < lista.Count; b++)
                    {
                        if (lista[b].AeropuertoDestinoId == d)
                        {
                            var tramos = lista.Skip(a).Take(b - a + 1).Select(ConvertirTramo).ToList();
                            vm.Tarjetas.Add(CrearTarjeta(tramos, true, pax));
                            encontrado = true;
                            break;
                        }
                    }
                }
            }

            // Filtro de horario (opcional) segun la hora de salida del primer tramo.
            string h = vm.Busqueda.Horario;
            if (h == "Mañana") vm.Tarjetas = vm.Tarjetas.Where(t => t.Tramos[0].HoraSalida < new TimeSpan(12, 0, 0)).ToList();
            else if (h == "Tarde") vm.Tarjetas = vm.Tarjetas.Where(t => t.Tramos[0].HoraSalida >= new TimeSpan(12, 0, 0) && t.Tramos[0].HoraSalida < new TimeSpan(18, 0, 0)).ToList();
            else if (h == "Noche") vm.Tarjetas = vm.Tarjetas.Where(t => t.Tramos[0].HoraSalida >= new TimeSpan(18, 0, 0)).ToList();

            vm.Tarjetas = vm.Tarjetas.OrderBy(t => t.Tramos[0].HoraSalida).ToList();

            if (vm.Tarjetas.Count == 0 && vm.Aviso == null)
            {
                vm.Aviso = MensajeSinVuelos(o, d, dia);
            }

            return View(vm);
        }

        // ---------------------------------------------------------------------------
        // Pantalla 7. Disponibilidad de asientos (/Vuelos/Disponibilidad/{vueloId})
        // ---------------------------------------------------------------------------
        public ActionResult Disponibilidad(int id)
        {
            var vuelo = ObtenerTramo(id);
            if (vuelo == null || vuelo.EstadoVueloCodigo == "CANCELADO")
            {
                TempData["Error"] = "El vuelo seleccionado no está disponible.";
                return RedirectToAction("Buscar");
            }

            var vm = new DisponibilidadVM { Vuelo = vuelo, Corte = DateTime.Now };
            if (!vuelo.AeronaveId.HasValue)
            {
                vm.Asientos = new List<EstadoAsientoRow>();
                vm.Mensaje = "Este vuelo aún no tiene aeronave asignada; sus asientos no pueden reservarse.";
            }
            else
            {
                vm.Asientos = ObtenerAsientos(id);
                vm.Reservados = vm.Asientos.Count(a => a.EstadoVisual == "RESERVADO");
                vm.Ocupados = vm.Asientos.Count(a => a.EstadoVisual == "OCUPADO");
            }
            return View(vm);
        }

        // ---------------------------------------------------------------------------
        // Apoyo
        // ---------------------------------------------------------------------------

        private void Preparar(BusquedaVM vm)
        {
            var aeropuertos = db.Aeropuertoes.Where(a => a.Activo).OrderBy(a => a.Localidad).ToList();
            if (!vm.OrigenId.HasValue)
            {
                // Valor inicial: Quito (wireframes 1 y 5).
                var quito = aeropuertos.FirstOrDefault(a => a.CodigoIATA == "UIO");
                if (quito != null) vm.OrigenId = quito.AeropuertoId;
            }
            vm.Aeropuertos = new SelectList(
                aeropuertos.Select(a => new { a.AeropuertoId, Texto = a.Localidad + " (" + a.CodigoIATA + ")" }).ToList(),
                "AeropuertoId", "Texto", vm.OrigenId);

            // Dias de operacion de cada ruta (panel informativo de la pantalla 5).
            var porId = aeropuertos.ToDictionary(a => a.AeropuertoId);
            var rutas = db.Rutas.Where(r => r.Activa).ToList();
            var frecuencias = db.Frecuencias.Where(f => f.Activa).ToList();
            foreach (var r in rutas.OrderBy(r => r.AeropuertoOrigenId).ThenBy(r => r.AeropuertoDestinoId))
            {
                if (!porId.ContainsKey(r.AeropuertoOrigenId) || !porId.ContainsKey(r.AeropuertoDestinoId)) continue;
                var dias = frecuencias.Where(f => f.RutaId == r.RutaId).Select(f => (int)f.DiaSemana).Distinct().OrderBy(x => x).ToList();
                if (dias.Count == 0) continue;
                vm.Rutas.Add(new DiasRutaVM
                {
                    Ruta = porId[r.AeropuertoOrigenId].Localidad + " → " + porId[r.AeropuertoDestinoId].Localidad,
                    Dias = string.Join(" · ", dias.Select(Fmt.DiaCorto))
                });
            }
        }

        // Valida los criterios en el servidor (no se confia en lo que valido el navegador).
        private string ValidarBusqueda(int? origenId, int? destinoId, DateTime? fecha, int? pasajeros)
        {
            if (!origenId.HasValue || !destinoId.HasValue || !fecha.HasValue || !pasajeros.HasValue)
                return "Complete los campos marcados con *.";
            if (origenId.Value == destinoId.Value)
                return "El origen y el destino deben ser distintos.";
            if (pasajeros.Value < 1 || pasajeros.Value > Reglas.CapacidadMaxima)
                return "La cantidad de pasajeros debe estar entre 1 y " + Reglas.CapacidadMaxima + ".";
            if (fecha.Value.Date < DateTime.Today)
                return "La fecha de salida no puede ser anterior a hoy.";
            int o = origenId.Value;
            int d = destinoId.Value;
            int n = db.Aeropuertoes.Count(a => a.Activo && (a.AeropuertoId == o || a.AeropuertoId == d));
            if (n != 2)
                return "Seleccione un origen y un destino válidos.";
            return null;
        }

        private TarjetaVueloVM CrearTarjeta(List<TramoRow> tramos, bool esItinerario, int pasajeros)
        {
            var t = new TarjetaVueloVM
            {
                Tramos = tramos,
                EsItinerario = esItinerario,
                Nombre = Fmt.Ruta(tramos),
                TramosCsv = string.Join(",", tramos.Select(x => x.VueloId.ToString())),
                Disponibles = tramos.Min(x => x.Disponibles)
            };

            t.Seleccionable = true;
            if (tramos.Any(x => x.EstadoVueloCodigo == "CANCELADO"))
            {
                t.Seleccionable = false;
                t.Motivo = "Vuelo cancelado";
            }
            else if (tramos.Any(x => !x.AeronaveId.HasValue))
            {
                t.Seleccionable = false;
                t.Motivo = "Sin aeronave asignada: aún no se comercializa";
            }
            else if (tramos.Any(x => !VentaVuelo.EstadoPermiteReserva(x.EstadoVueloCodigo)))
            {
                t.Seleccionable = false;
                t.Motivo = "Vuelo no disponible para reservas";
            }
            else if (t.Disponibles <= 0)
            {
                t.Seleccionable = false;
                t.Motivo = "Vuelo sin asientos disponibles";
            }
            else if (t.Disponibles < pasajeros)
            {
                t.Seleccionable = false;
                t.Motivo = "Solo quedan " + t.Disponibles + " asientos disponibles";
            }
            return t;
        }

        private static TramoRow ConvertirTramo(TramoItinerarioRow r)
        {
            return new TramoRow
            {
                VueloId = r.VueloId,
                CodigoVuelo = r.CodigoVuelo,
                FechaOperacion = r.FechaOperacion,
                DiaSemana = r.DiaSemana,
                RutaId = r.RutaId,
                TarifaReferencial = r.TarifaReferencial,
                AeropuertoOrigenId = r.AeropuertoOrigenId,
                AeropuertoDestinoId = r.AeropuertoDestinoId,
                Origen = r.Origen,
                CiudadOrigen = r.CiudadOrigen,
                Destino = r.Destino,
                CiudadDestino = r.CiudadDestino,
                HoraSalida = r.HoraSalida,
                HoraLlegada = r.HoraLlegada,
                AeronaveId = r.AeronaveId,
                EstadoVueloCodigo = r.EstadoVueloCodigo,
                Matricula = r.Matricula,
                Capacidad = r.Capacidad,
                Ocupados = r.Ocupados,
                Disponibles = r.Disponibles
            };
        }

        // Mensaje cuando no hay resultados: explica el dia de operacion en vez de inventar vuelos.
        private string MensajeSinVuelos(int origenId, int destinoId, DateTime dia)
        {
            var ruta = db.Rutas.FirstOrDefault(r => r.Activa && r.AeropuertoOrigenId == origenId && r.AeropuertoDestinoId == destinoId);
            if (ruta != null)
            {
                var dias = db.Frecuencias.Where(f => f.Activa && f.RutaId == ruta.RutaId)
                    .Select(f => (int)f.DiaSemana).Distinct().OrderBy(x => x).ToList();
                if (dias.Count > 0 && !dias.Contains(Fmt.NumeroDia(dia)))
                {
                    var o = db.Aeropuertoes.First(a => a.AeropuertoId == origenId).Localidad;
                    var de = db.Aeropuertoes.First(a => a.AeropuertoId == destinoId).Localidad;
                    return "La ruta " + o + "–" + de + " opera " + string.Join(", ", dias.Select(x => Fmt.DiaSemana(x).ToLower())) + ". Elija otra fecha.";
                }
            }
            return "No hay vuelos para los criterios seleccionados.";
        }

        // Destinos alcanzables desde un origen: rutas directas mas conexiones de un salto el mismo dia
        // (por ejemplo Quito - Guayaquil - Galapagos). DP-09: sin venta de tramos sueltos con Galapagos,
        // un destino con Galapagos solo aparece si se alcanza con escala.
        private List<int> DestinosAlcanzables(int origenId)
        {
            var aeropuertos = db.Aeropuertoes.ToList();
            var gps = aeropuertos.FirstOrDefault(a => a.CodigoIATA == "GPS");
            int gpsId = gps == null ? -1 : gps.AeropuertoId;
            var rutas = db.Rutas.Where(r => r.Activa).ToList();
            var frecuencias = db.Frecuencias.Where(f => f.Activa).ToList();
            var destinos = new HashSet<int>();

            foreach (var r in rutas.Where(x => x.AeropuertoOrigenId == origenId))
            {
                bool tocaGalapagos = r.AeropuertoOrigenId == gpsId || r.AeropuertoDestinoId == gpsId;
                if (tocaGalapagos && !Reglas.VentaTramosGalapagos) continue;
                destinos.Add(r.AeropuertoDestinoId);
            }

            foreach (var f1 in frecuencias)
            {
                var r1 = rutas.FirstOrDefault(r => r.RutaId == f1.RutaId);
                if (r1 == null || r1.AeropuertoOrigenId != origenId) continue;
                foreach (var f2 in frecuencias)
                {
                    if (f2.DiaSemana != f1.DiaSemana || f2.HoraSalida < f1.HoraLlegada) continue;
                    var r2 = rutas.FirstOrDefault(r => r.RutaId == f2.RutaId);
                    if (r2 == null || r2.AeropuertoOrigenId != r1.AeropuertoDestinoId) continue;
                    if (r2.AeropuertoDestinoId != origenId) destinos.Add(r2.AeropuertoDestinoId);
                }
            }
            return destinos.ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    // Pantallas 11 y 12: pago simulado y confirmacion. No se realizan cobros reales (DP-06).
    [Authorize]
    public class ComprasController : BaseController
    {
        // ---------------------------------------------------------------------------
        // Pantalla 11. Proceso de compra y pago (/Compras/Pagar/{reservaId})
        // ---------------------------------------------------------------------------
        [Authorize(Roles = RolesApp.Compradores)]
        public ActionResult Pagar(int id)
        {
            var detalle = ObtenerDetalle(id);
            if (detalle == null)
            {
                TempData["Error"] = "No se encontró la operación solicitada.";
                return RedirectToAction("MisReservas", "Reservas");
            }
            if (detalle.Reserva.UsuarioId != UsuarioActualId && !EsPersonal) return View("~/Views/Shared/AccesoDenegado.cshtml");

            string problema = ElegibilidadPago(detalle);
            if (problema != null)
            {
                TempData["Error"] = problema;
                return RedirectToAction("MisReservas", "Reservas");
            }

            var vm = ConstruirPago(detalle);
            vm.Token = Guid.NewGuid();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.Compradores)]
        public ActionResult Pagar(int id, PagoVM modelo)
        {
            var detalle = ObtenerDetalle(id);
            if (detalle == null)
            {
                TempData["Error"] = "No se encontró la operación solicitada.";
                return RedirectToAction("MisReservas", "Reservas");
            }
            if (detalle.Reserva.UsuarioId != UsuarioActualId && !EsPersonal) return View("~/Views/Shared/AccesoDenegado.cshtml");

            // RNP-10: si la misma solicitud (misma clave) ya se proceso, no se duplica nada.
            if (modelo.Token != Guid.Empty && db.Compras.Any(c => c.ClaveIdempotencia == modelo.Token))
            {
                return RedirectToAction("Confirmacion", new { id = id });
            }

            string problema = ElegibilidadPago(detalle);
            if (problema != null)
            {
                TempData["Error"] = problema;
                return RedirectToAction("MisReservas", "Reservas");
            }

            var marca = db.MarcaTarjetas.FirstOrDefault(m => m.Codigo == modelo.MarcaTarjetaCodigo && m.Activa);
            var metodo = db.MetodoPagoes.FirstOrDefault(m => m.Codigo == modelo.MetodoPagoCodigo && m.Activo);
            string numero = Regex.Replace(modelo.NumeroTarjeta ?? "", @"[\s-]", "");

            // Validaciones de formato (no se verifica la existencia de la tarjeta ni se autoriza con un banco).
            if (metodo == null) ModelState.AddModelError("MetodoPagoCodigo", "Seleccione un método de pago válido.");
            if (marca == null)
            {
                ModelState.AddModelError("MarcaTarjetaCodigo", "Seleccione una marca de tarjeta válida.");
            }
            else
            {
                if (!Regex.IsMatch(numero, "^[0-9]+$") || numero.Length != marca.LongitudNumero)
                    ModelState.AddModelError("NumeroTarjeta", "El número debe tener " + marca.LongitudNumero + " dígitos para " + marca.Nombre + ".");
                string codigo = modelo.CodigoSeguridad ?? "";
                if (!Regex.IsMatch(codigo, "^[0-9]+$") || codigo.Length != marca.LongitudCodigoSeguridad)
                    ModelState.AddModelError("CodigoSeguridad", "El código de seguridad debe tener " + marca.LongitudCodigoSeguridad + " dígitos.");
            }
            if (!VencimientoValido(modelo.Vencimiento))
                ModelState.AddModelError("Vencimiento", "Ingrese un vencimiento válido (MM/AA) que no esté vencido.");
            if (!string.IsNullOrWhiteSpace(modelo.NombreTitular) && !Regex.IsMatch(modelo.NombreTitular.Trim(), @"^[\p{L} .'\-]{2,100}$"))
                ModelState.AddModelError("NombreTitular", "Use solo letras y espacios (2 a 100 caracteres).");

            if (!ModelState.IsValid)
            {
                var vm = ConstruirPago(detalle);
                vm.Token = modelo.Token == Guid.Empty ? Guid.NewGuid() : modelo.Token;
                vm.MetodoPagoCodigo = modelo.MetodoPagoCodigo;
                vm.MarcaTarjetaCodigo = modelo.MarcaTarjetaCodigo;
                vm.NombreTitular = modelo.NombreTitular;
                vm.Vencimiento = modelo.Vencimiento;
                // Por seguridad, el numero de tarjeta y el codigo no se devuelven al formulario.
                return View(vm);
            }

            // ---- Registro de la compra y del pago simulado, en una sola transaccion ----
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // El importe lo calcula el servidor (no se acepta el que envia el navegador).
                    decimal importe = detalle.Lineas
                        .Where(l => l.EstadoAsignacionCodigo == "PENDIENTE")
                        .Sum(l => l.ImporteTramo);
                    if (importe <= 0) throw new InvalidOperationException("La reserva no tiene asientos pendientes de pago.");

                    int reservaId = detalle.Reserva.ReservaId;
                    var compra = db.Compras.Where(c => c.ReservaId == reservaId && c.EstadoCompraCodigo == "PENDIENTE")
                                           .OrderByDescending(c => c.CompraId).FirstOrDefault();
                    if (compra == null)
                    {
                        compra = new Compra
                        {
                            ReservaId = reservaId,
                            FechaCompra = DateTime.Now,
                            Importe = importe,
                            MonedaCodigo = Reglas.Moneda,
                            EstadoCompraCodigo = "PENDIENTE",
                            ClaveIdempotencia = modelo.Token == Guid.Empty ? Guid.NewGuid() : modelo.Token
                        };
                        db.Compras.Add(compra);
                        db.SaveChanges();
                    }

                    // Regla de la simulacion (documentada): un numero de tarjeta terminado en 0000 se RECHAZA;
                    // cualquier otro numero con formato valido se APRUEBA.
                    bool aprobado = !numero.EndsWith("0000");
                    int secuencia = db.Pagoes.Count(p => p.Compra.ReservaId == reservaId) + 1;
                    string codigoOperacion = "SIM-" + detalle.Reserva.CodigoReserva + "-" + secuencia.ToString("00");

                    var pago = new Pago
                    {
                        CompraId = compra.CompraId,
                        FechaPago = DateTime.Now,
                        Importe = compra.Importe,
                        EstadoPagoCodigo = aprobado ? "APROBADO" : "RECHAZADO",
                        MetodoPagoCodigo = metodo.Codigo,
                        MarcaTarjetaCodigo = marca.Codigo,
                        UltimosCuatroDigitos = numero.Substring(numero.Length - 4),   // nunca se guarda el numero completo
                        NombreTitular = modelo.NombreTitular.Trim(),
                        CodigoOperacion = codigoOperacion,
                        EsSimulado = true,
                        MensajeResultado = aprobado ? "Pago simulado aprobado." : "Pago simulado rechazado (tarjeta de prueba terminada en 0000)."
                    };
                    db.Pagoes.Add(pago);
                    db.SaveChanges();

                    if (aprobado)
                    {
                        // La compra solo pasa a PAGADA despues de registrar el pago aprobado; la reserva queda CONFIRMADA
                        // y sus asientos pasan a OCUPADO (rojo).
                        db.Database.ExecuteSqlCommand("UPDATE dbo.Compra SET EstadoCompraCodigo = 'PAGADA' WHERE CompraId = @p0", compra.CompraId);
                        db.Database.ExecuteSqlCommand("UPDATE dbo.Reserva SET EstadoReservaCodigo = 'CONFIRMADA', FechaExpiracion = NULL WHERE ReservaId = @p0", reservaId);
                        db.Database.ExecuteSqlCommand(
                            "UPDATE dbo.ReservaPasajeroVuelo SET EstadoAsignacionCodigo = 'CONFIRMADA' " +
                            "WHERE ReservaId = @p0 AND EstadoAsignacionCodigo = 'PENDIENTE'", reservaId);

                        // Comprobantes academicos (DP-05): la confirmacion y el comprobante de venta de ejemplo.
                        db.Comprobantes.Add(NuevoComprobante(compra.CompraId, "CONFIRMACION", "CNF-"));
                        db.SaveChanges();
                        db.Comprobantes.Add(NuevoComprobante(compra.CompraId, "FACTURA", "ACAD-"));
                        db.SaveChanges();
                    }

                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    ModelState.AddModelError("", MensajeBase(ex));
                    var vm = ConstruirPago(ObtenerDetalle(id));
                    vm.Token = modelo.Token == Guid.Empty ? Guid.NewGuid() : modelo.Token;
                    return View(vm);
                }
            }

            return RedirectToAction("Confirmacion", new { id = id });
        }

        // ---------------------------------------------------------------------------
        // Pantalla 12. Confirmacion de compra o reserva (/Compras/Confirmacion/{reservaId})
        // Solo muestra datos persistidos y estados confirmados.
        // ---------------------------------------------------------------------------
        public ActionResult Confirmacion(int id)
        {
            var vm = ObtenerDetalle(id);
            if (vm == null)
            {
                TempData["Error"] = "No se encontró la operación solicitada.";
                return RedirectToAction("MisReservas", "Reservas");
            }
            if (vm.Reserva.UsuarioId != UsuarioActualId && !EsPersonal) return View("~/Views/Shared/AccesoDenegado.cshtml");
            return View(vm);
        }

        // ---------------------------------------------------------------------------
        // Apoyo
        // ---------------------------------------------------------------------------

        // Una reserva es pagable si esta PENDIENTE y no vencio.
        private string ElegibilidadPago(DetalleReservaVM d)
        {
            if (d.Reserva.EstadoReservaCodigo == "CONFIRMADA") return "La reserva " + d.Reserva.CodigoReserva + " ya está pagada.";
            if (d.Reserva.EstadoReservaCodigo == "CANCELADA") return "La reserva " + d.Reserva.CodigoReserva + " está cancelada.";
            if (d.Reserva.EstadoReservaCodigo == "EXPIRADA") return "La reserva " + d.Reserva.CodigoReserva + " expiró.";
            if (d.Reserva.EstadoReservaCodigo != "PENDIENTE") return "La reserva no admite pago.";
            if (!d.Lineas.Any(l => l.EstadoAsignacionCodigo == "PENDIENTE")) return "La reserva no tiene asientos pendientes de pago.";
            return null;
        }

        private PagoVM ConstruirPago(DetalleReservaVM d)
        {
            var vigentes = d.Lineas.Where(l => l.EstadoAsignacionCodigo == "PENDIENTE").ToList();
            var vm = new PagoVM
            {
                ReservaId = d.Reserva.ReservaId,
                CodigoReserva = d.Reserva.CodigoReserva,
                Pasajeros = vigentes.Select(l => l.PasajeroId).Distinct().Count(),
                Asientos = string.Join(", ", vigentes.Select(l => l.CodigoAsiento).Distinct()),
                Total = vigentes.Sum(l => l.ImporteTramo),
                FechaViaje = vigentes.Count > 0 ? (DateTime?)vigentes.Min(l => l.FechaOperacion) : null,
                EstadoCompra = d.Compra == null ? "Sin compra" : Fmt.EstadoCompra(d.Compra.EstadoCompraCodigo),
                Metodos = db.MetodoPagoes.Where(m => m.Activo).OrderBy(m => m.Codigo).ToList(),
                Marcas = db.MarcaTarjetas.Where(m => m.Activa).OrderBy(m => m.Nombre).ToList()
            };
            var ciudades = new List<string>();
            var orden = vigentes.GroupBy(l => l.VueloId).Select(g => g.First()).OrderBy(l => l.SalidaProgramada).ToList();
            if (orden.Count > 0)
            {
                ciudades.Add(orden[0].CiudadOrigen);
                foreach (var l in orden) ciudades.Add(l.CiudadDestino);
            }
            vm.Ruta = string.Join(" → ", ciudades);
            if (vm.Metodos.Count > 0) vm.MetodoPagoCodigo = vm.Metodos[0].Codigo;
            return vm;
        }

        // Vencimiento MM/AA: mes 01 a 12 y no vencido.
        private static bool VencimientoValido(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return false;
            var m = Regex.Match(valor.Trim(), @"^(0[1-9]|1[0-2])/?([0-9]{2})$");
            if (!m.Success) return false;
            int mes = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            int anio = 2000 + int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            var ultimoDia = new DateTime(anio, mes, 1).AddMonths(1).AddDays(-1);
            return ultimoDia >= DateTime.Today;
        }

        // Numeracion academica consecutiva por tipo (ACAD-000001, CNF-000001...). DP-05: formato de ejemplo.
        private Comprobante NuevoComprobante(int compraId, string tipo, string prefijo)
        {
            int ultimo = 0;
            var numeros = db.Comprobantes.Where(c => c.TipoComprobanteCodigo == tipo).Select(c => c.NumeroComprobante).ToList();
            foreach (var n in numeros)
            {
                int valor;
                if (n != null && n.StartsWith(prefijo) && int.TryParse(n.Substring(prefijo.Length), out valor) && valor > ultimo) ultimo = valor;
            }
            return new Comprobante
            {
                CompraId = compraId,
                TipoComprobanteCodigo = tipo,
                NumeroComprobante = prefijo + (ultimo + 1).ToString("000000"),
                FechaEmision = DateTime.Now,
                EstadoComprobanteCodigo = "EMITIDO"
            };
        }
    }
}

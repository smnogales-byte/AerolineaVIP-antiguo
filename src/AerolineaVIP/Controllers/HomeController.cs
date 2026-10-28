using System;
using System.Linq;
using System.Web.Mvc;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    // Pantalla 1. Pagina de inicio: presenta la aerolinea y da acceso a la busqueda de vuelos.
    [AllowAnonymous]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var vm = new BusquedaVM();
            using (var db = new AerolineaVIPEntities())
            {
                var aeropuertos = db.Aeropuertoes.Where(a => a.Activo).OrderBy(a => a.Localidad).ToList();
                var quito = aeropuertos.FirstOrDefault(a => a.CodigoIATA == "UIO");
                if (quito != null) vm.OrigenId = quito.AeropuertoId;
                vm.Aeropuertos = new SelectList(
                    aeropuertos.Select(a => new { a.AeropuertoId, Texto = a.Localidad + " (" + a.CodigoIATA + ")" }).ToList(),
                    "AeropuertoId", "Texto", vm.OrigenId);
            }
            return View(vm);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Aerolínea VIP: proyecto académico de reserva y administración de pasajes.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Contacto del proyecto académico.";
            return View();
        }
    }
}

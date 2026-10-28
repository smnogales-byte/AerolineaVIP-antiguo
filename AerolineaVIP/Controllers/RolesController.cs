using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    public class UsuarioRolesVM
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
    }

    public class EditarRolesVM
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public List<string> RolesActuales { get; set; }
        public List<string> TodosLosRoles { get; set; }
    }

    // RNP-09: la autorización se valida en el servidor, no solo ocultando el menú.
    [Authorize(Roles = "SuperUsuario,Administrador")]
    public class RolesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> um;

        public RolesController()
        {
            db = new ApplicationDbContext();
            um = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        }

        // Lista de usuarios con sus roles.
        public ActionResult Index()
        {
            var nombres = db.Roles.ToDictionary(r => r.Id, r => r.Name);
            var lista = db.Users.ToList().Select(u => new UsuarioRolesVM
            {
                Id = u.Id,
                Email = u.Email,
                Roles = u.Roles.Select(r => nombres[r.RoleId]).OrderBy(n => n).ToList()
            }).OrderBy(u => u.Email).ToList();
            return View(lista);
        }

        [Authorize(Roles = "SuperUsuario")]

        public ActionResult Editar(string id)
        {
            var usuario = um.FindById(id);
            if (usuario == null) return HttpNotFound();
            return View(Construir(usuario));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario")]
        public ActionResult Editar(string id, string[] rolesSeleccionados)
        {
            var usuario = um.FindById(id);
            if (usuario == null) return HttpNotFound();

            // Solo se aceptan roles que existen en la lista oficial.
            var existentes = db.Roles.Select(r => r.Name).ToList();
            var elegidos = (rolesSeleccionados ?? new string[0]).Intersect(existentes).ToList();

            // Evita que el administrador se quite su propio rol y deje el sistema sin administrador.
            if (id == User.Identity.GetUserId() && !elegidos.Contains("Administrador"))
            {
                ModelState.AddModelError("", "No puedes quitarte a ti mismo el rol Administrador.");
                return View(Construir(usuario));
            }

            var actuales = um.GetRoles(id);
            um.RemoveFromRoles(id, actuales.Except(elegidos).ToArray());
            um.AddToRoles(id, elegidos.Except(actuales).ToArray());
            return RedirectToAction("Index");
        }

        private EditarRolesVM Construir(ApplicationUser usuario)
        {
            return new EditarRolesVM
            {
                Id = usuario.Id,
                Email = usuario.Email,
                RolesActuales = um.GetRoles(usuario.Id).ToList(),
                TodosLosRoles = db.Roles.Select(r => r.Name).OrderBy(n => n).ToList()
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { um.Dispose(); db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    // CRUD de Métodos de pago generado con el estilo del scaffolding de MVC 5 con Entity Framework.
    // Acceso por rol validado en el servidor; el borrado queda reservado al SuperUsuario.
    [Authorize(Roles = RolesApp.Comerciales)]
    public class MetodosPagoController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: MetodosPago
        public ActionResult Index()
        {
            return View(db.MetodoPagoes.ToList());
        }

        // GET: MetodosPago/Details/5
        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago entidad = db.MetodoPagoes.Find(id);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            return View(entidad);
        }

        // GET: MetodosPago/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: MetodosPago/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Codigo,Nombre,Activo")] MetodoPago entidad)
        {
            if (db.MetodoPagoes.Any(x => x.Codigo == entidad.Codigo))
            {
                ModelState.AddModelError("Codigo", "Ya existe un registro con ese código.");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    db.MetodoPagoes.Add(entidad);
                    db.SaveChanges();
                    TempData["Mensaje"] = "Registro creado.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", Mensaje(ex));
                }
            }

            return View(entidad);
        }

        // GET: MetodosPago/Edit/5
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago entidad = db.MetodoPagoes.Find(id);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            return View(entidad);
        }

        // POST: MetodosPago/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Codigo,Nombre,Activo")] MetodoPago entidad)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(entidad).State = EntityState.Modified;
                    db.SaveChanges();
                    TempData["Mensaje"] = "Registro actualizado.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", Mensaje(ex));
                }
            }
            return View(entidad);
        }

        // GET: MetodosPago/Delete/5
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago entidad = db.MetodoPagoes.Find(id);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            return View(entidad);
        }

        // POST: MetodosPago/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(string id)
        {
            MetodoPago entidad = db.MetodoPagoes.Find(id);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            try
            {
                db.MetodoPagoes.Remove(entidad);
                db.SaveChanges();
                TempData["Mensaje"] = "Registro eliminado.";
            }
            catch (DbUpdateException)
            {
                // Integridad referencial: no se puede borrar un registro con datos asociados (Pagos).
                TempData["Error"] = "No se puede eliminar: el registro tiene datos asociados (Pagos).";
            }
            return RedirectToAction("Index");
        }

        private static string Mensaje(Exception ex)
        {
            var e = ex;
            while (e.InnerException != null) e = e.InnerException;
            string m = e.Message ?? "";
            if (m.Contains("UNIQUE") || m.Contains("duplicate")) return "Ya existe un registro con esos datos.";
            if (m.Contains("CHECK")) return "Algún valor no cumple las reglas de la base de datos (por ejemplo, capacidad de 1 a 7 asientos o un horario de llegada posterior a la salida).";
            if (m.Contains("FOREIGN KEY")) return "Seleccione valores válidos en las listas.";
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

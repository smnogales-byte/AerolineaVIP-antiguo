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
    // CRUD de Rutas generado con el estilo del scaffolding de MVC 5 con Entity Framework.
    // Acceso por rol validado en el servidor; el borrado queda reservado al SuperUsuario.
    [Authorize(Roles = RolesApp.Operativos)]
    public class RutasController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Rutas
        public ActionResult Index()
        {
            return View(db.Rutas.ToList());
        }

        // GET: Rutas/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta entidad = db.Rutas.Find(id.Value);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            return View(entidad);
        }

        // GET: Rutas/Create
        public ActionResult Create()
        {
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuertoes.ToList(), "AeropuertoId", "Localidad");
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuertoes.ToList(), "AeropuertoId", "Localidad");
            return View();
        }

        // POST: Rutas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AeropuertoOrigenId,AeropuertoDestinoId,TarifaReferencial,Activa")] Ruta entidad)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Rutas.Add(entidad);
                    db.SaveChanges();
                    TempData["Mensaje"] = "Registro creado.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", Mensaje(ex));
                }
            }

            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuertoes.ToList(), "AeropuertoId", "Localidad", entidad.AeropuertoOrigenId);
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuertoes.ToList(), "AeropuertoId", "Localidad", entidad.AeropuertoDestinoId);
            return View(entidad);
        }

        // GET: Rutas/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta entidad = db.Rutas.Find(id.Value);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuertoes.ToList(), "AeropuertoId", "Localidad", entidad.AeropuertoOrigenId);
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuertoes.ToList(), "AeropuertoId", "Localidad", entidad.AeropuertoDestinoId);
            return View(entidad);
        }

        // POST: Rutas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,TarifaReferencial,Activa")] Ruta entidad)
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
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuertoes.ToList(), "AeropuertoId", "Localidad", entidad.AeropuertoOrigenId);
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuertoes.ToList(), "AeropuertoId", "Localidad", entidad.AeropuertoDestinoId);
            return View(entidad);
        }

        // GET: Rutas/Delete/5
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta entidad = db.Rutas.Find(id.Value);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            return View(entidad);
        }

        // POST: Rutas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(int id)
        {
            Ruta entidad = db.Rutas.Find(id);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            try
            {
                db.Rutas.Remove(entidad);
                db.SaveChanges();
                TempData["Mensaje"] = "Registro eliminado.";
            }
            catch (DbUpdateException)
            {
                // Integridad referencial: no se puede borrar un registro con datos asociados (Frecuencias).
                TempData["Error"] = "No se puede eliminar: el registro tiene datos asociados (Frecuencias).";
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

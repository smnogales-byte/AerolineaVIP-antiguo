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
    // CRUD de Frecuencias generado con el estilo del scaffolding de MVC 5 con Entity Framework.
    // Acceso por rol validado en el servidor; el borrado queda reservado al SuperUsuario.
    [Authorize(Roles = RolesApp.Operativos)]
    public class FrecuenciasController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Frecuencias
        public ActionResult Index()
        {
            return View(db.Frecuencias.ToList());
        }

        // GET: Frecuencias/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia entidad = db.Frecuencias.Find(id.Value);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            return View(entidad);
        }

        // GET: Frecuencias/Create
        public ActionResult Create()
        {
            ViewBag.RutaId = ListaRutas(null);
            return View();
        }

        // POST: Frecuencias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RutaId,DiaSemana,HoraSalida,HoraLlegada,Activa")] Frecuencia entidad)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Frecuencias.Add(entidad);
                    db.SaveChanges();
                    TempData["Mensaje"] = "Registro creado.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", Mensaje(ex));
                }
            }

            ViewBag.RutaId = ListaRutas(entidad.RutaId);
            return View(entidad);
        }

        // GET: Frecuencias/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia entidad = db.Frecuencias.Find(id.Value);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            ViewBag.RutaId = ListaRutas(entidad.RutaId);
            return View(entidad);
        }

        // POST: Frecuencias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FrecuenciaId,RutaId,DiaSemana,HoraSalida,HoraLlegada,Activa")] Frecuencia entidad)
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
            ViewBag.RutaId = ListaRutas(entidad.RutaId);
            return View(entidad);
        }

        // GET: Frecuencias/Delete/5
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia entidad = db.Frecuencias.Find(id.Value);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            return View(entidad);
        }

        // POST: Frecuencias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesApp.SuperUsuario)]
        public ActionResult DeleteConfirmed(int id)
        {
            Frecuencia entidad = db.Frecuencias.Find(id);
            if (entidad == null)
            {
                return HttpNotFound();
            }
            try
            {
                db.Frecuencias.Remove(entidad);
                db.SaveChanges();
                TempData["Mensaje"] = "Registro eliminado.";
            }
            catch (DbUpdateException)
            {
                // Integridad referencial: no se puede borrar un registro con datos asociados (Vuelos).
                TempData["Error"] = "No se puede eliminar: el registro tiene datos asociados (Vuelos).";
            }
            return RedirectToAction("Index");
        }

        // Texto legible para cada ruta: Quito -> Guayaquil.
        private SelectList ListaRutas(object seleccionada)
        {
            var aeropuertos = db.Aeropuertoes.ToDictionary(a => a.AeropuertoId, a => a.Localidad);
            var items = db.Rutas.ToList()
                .Select(r => new { r.RutaId, Texto = aeropuertos[r.AeropuertoOrigenId] + " → " + aeropuertos[r.AeropuertoDestinoId] })
                .ToList();
            return new SelectList(items, "RutaId", "Texto", seleccionada);
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

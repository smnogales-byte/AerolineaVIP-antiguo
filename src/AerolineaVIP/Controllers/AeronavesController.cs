using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Progra_03.Models;

namespace Progra_03.Controllers
{
    // HU-006: Administración de aeronaves de la flota.
    // Solicitado en: HU-006 (Como administrador, quiero registrar las tres aeronaves),
    //                RF-016 (flota inicial de tres registros del Embraer Phenom 300E),
    //                RF-041 (solo el personal autorizado crea o modifica la información operativa).
    // Origen: explícito en el enunciado. El rol Administrador viene de la matriz 16.1 (propuesta).
    //
    // Alcance de este controlador:
    //  - Listar, crear y editar la matrícula y el modelo de una aeronave.
    //  - NO se elimina: los vuelos referencian a las aeronaves.
    //  - NO se cambia el estado (DISPONIBLE / NO_DISPONIBLE) aquí: eso es HU-024 (Operaciones).
    //
    // Las tres aeronaves iniciales (AV-01, AV-02, AV-03) ya las inserta el script de la base de datos.
    // La capacidad (máx. 7 asientos, RN-03) pertenece al modelo y no se edita desde esta pantalla.
    [Authorize(Roles = "SuperUsuario,Administrador,Operaciones")]
    public class AeronavesController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Aeronaves
        public ActionResult Index()
        {
            var lista = (from a in db.Set<Aeronave>()
                         join m in db.Set<ModeloAeronave>() on a.ModeloAeronaveId equals m.ModeloAeronaveId
                         orderby a.Matricula
                         select new AeronaveFilaVM
                         {
                             AeronaveId = a.AeronaveId,
                             Matricula = a.Matricula,
                             Modelo = m.Fabricante + " " + m.Nombre,
                             Capacidad = m.CapacidadAsientos,
                             Estado = a.EstadoAeronaveCodigo
                         }).ToList();

            return View(lista);
        }

        // GET: Aeronaves/Crear
        public ActionResult Crear()
        {
            var vm = new AeronaveFormVM();
            CargarModelos(null);
            return View(vm);
        }

        // POST: Aeronaves/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(AeronaveFormVM vm)
        {
            // La matrícula se normaliza (sin espacios, en mayúsculas) antes de validar.
            ModelState.Remove("Matricula");
            vm.Matricula = (vm.Matricula ?? "").Trim().ToUpperInvariant();
            ValidarFormulario(vm, null);

            if (ModelState.IsValid)
            {
                var aeronave = new Aeronave
                {
                    Matricula = vm.Matricula,
                    ModeloAeronaveId = vm.ModeloAeronaveId,
                    // Toda aeronave nueva inicia disponible; cambiarlo es HU-024.
                    EstadoAeronaveCodigo = "DISPONIBLE"
                };

                try
                {
                    db.Set<Aeronave>().Add(aeronave);
                    db.SaveChanges();
                    TempData["Mensaje"] = "Aeronave " + aeronave.Matricula + " registrada.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException)
                {
                    // Respaldo: la base también protege la unicidad (UQ_Aeronave_Matricula).
                    ModelState.AddModelError("Matricula", "No se pudo guardar. Verifique que la matrícula no esté repetida.");
                }
            }

            CargarModelos(vm.ModeloAeronaveId);
            return View(vm);
        }

        // GET: Aeronaves/Editar/5
        public ActionResult Editar(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var aeronave = db.Set<Aeronave>().Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }

            var vm = new AeronaveFormVM
            {
                AeronaveId = aeronave.AeronaveId,
                Matricula = aeronave.Matricula,
                ModeloAeronaveId = aeronave.ModeloAeronaveId,
                Estado = aeronave.EstadoAeronaveCodigo
            };
            CargarModelos(vm.ModeloAeronaveId);
            return View(vm);
        }

        // POST: Aeronaves/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(AeronaveFormVM vm)
        {
            var aeronave = db.Set<Aeronave>().Find(vm.AeronaveId);
            if (aeronave == null)
            {
                return HttpNotFound();
            }

            ModelState.Remove("Matricula");
            vm.Matricula = (vm.Matricula ?? "").Trim().ToUpperInvariant();
            vm.Estado = aeronave.EstadoAeronaveCodigo; // solo lectura en esta pantalla
            ValidarFormulario(vm, aeronave.AeronaveId);

            if (ModelState.IsValid)
            {
                aeronave.Matricula = vm.Matricula;
                aeronave.ModeloAeronaveId = vm.ModeloAeronaveId;

                try
                {
                    db.SaveChanges();
                    TempData["Mensaje"] = "Aeronave " + aeronave.Matricula + " actualizada.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("Matricula", "No se pudo guardar. Verifique que la matrícula no esté repetida.");
                }
            }

            CargarModelos(vm.ModeloAeronaveId);
            return View(vm);
        }

        // Validaciones del formulario (se repiten en el servidor, RNF-007).
        private void ValidarFormulario(AeronaveFormVM vm, int? aeronaveIdActual)
        {
            if (string.IsNullOrEmpty(vm.Matricula))
            {
                ModelState.AddModelError("Matricula", "La matrícula es obligatoria.");
            }
            else if (vm.Matricula.Length > 15)
            {
                ModelState.AddModelError("Matricula", "La matrícula no puede superar 15 caracteres.");
            }
            else
            {
                string matricula = vm.Matricula;
                bool repetida = db.Set<Aeronave>().Any(a => a.Matricula == matricula && a.AeronaveId != (aeronaveIdActual ?? 0));
                if (repetida)
                {
                    ModelState.AddModelError("Matricula", "Ya existe una aeronave con esa matrícula.");
                }
            }

            int modeloId = vm.ModeloAeronaveId;
            if (!db.Set<ModeloAeronave>().Any(m => m.ModeloAeronaveId == modeloId))
            {
                ModelState.AddModelError("ModeloAeronaveId", "Seleccione un modelo válido.");
            }
        }

        private void CargarModelos(int? seleccionado)
        {
            var modelos = db.Set<ModeloAeronave>()
                            .OrderBy(m => m.Nombre)
                            .Select(m => new { m.ModeloAeronaveId, Texto = m.Fabricante + " " + m.Nombre + " (" + m.CapacidadAsientos + " asientos)" })
                            .ToList();
            ViewBag.Modelos = new SelectList(modelos, "ModeloAeronaveId", "Texto", seleccionado);
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

    // Fila de la lista de aeronaves.
    public class AeronaveFilaVM
    {
        public int AeronaveId { get; set; }
        public string Matricula { get; set; }
        public string Modelo { get; set; }
        public int Capacidad { get; set; }
        public string Estado { get; set; }
    }

    // Formulario de crear / editar aeronave. Estado es solo de lectura (se cambia esn HU-024).
    public class AeronaveFormVM
    {
        public int AeronaveId { get; set; }
        public string Matricula { get; set; }
        public int ModeloAeronaveId { get; set; }
        public string Estado { get; set; }
    }
}
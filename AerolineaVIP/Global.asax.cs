using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.AspNet.Identity;
using Progra_03.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Progra_03
{
    public class MvcApplication : System.Web.HttpApplication
    {

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);


            // CODIGO INSERTADO (HU-003): roles, usuarios iniciales y permisos
            ApplicationDbContext db = new ApplicationDbContext();

            CrearRoles(db);
            CrearSuperUsuario(db);
            CrearAdministrador(db);
            CrearOperaciones(db);
            CrearComercial(db);
            CrearCounter(db);
            CrearPasajero(db);
            CrearCliente(db);
            AsignarPermisos(db);
            db.Dispose();
        }





        // Crea los 6 roles de la matriz de autorización 16.1 del enunciado (ENUNCIADO_PROYECTO.pdf),
        // en AerolineaVIP_Identity (tabla AspNetRoles). Cada rol se crea solo si no existe, así que
        // reiniciar la aplicación no duplica nada.
        // Origen general: Sprint 1 (HU-003, roles preliminares), RF-005 (el sistema administrará roles
        // y permisos) y RF-044 (gestionar usuarios y roles).
        // Leyenda de la matriz: P = permitido, R = restringido a condiciones específicas.
        // Nombre en el código -> perfil en el enunciado: Counter = Empleado de counter,
        // Operaciones = Responsable de operaciones, Comercial = Responsable comercial.
        // La matriz es una propuesta inicial: la autorización real debe comprobarse en el servidor
        // y basarse en permisos específicos, no solo en el nombre del rol.
        private void CrearRoles(ApplicationDbContext db)
        {
            var rolemanager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));

            // 1) Cliente: Perfil "Cliente" del enunciado. Rol por defecto de quien se registra.
            //    Solicitado en: 4.1 (usuarios principales), 9.2 INT-02, 9.8 (perfil Cliente), 16.1 (columna Cliente), RF-001, HU-001, CP-004.
            //    Origen: El documento original lo identifica explícitamente.
            //    P: buscar vuelos, gestionar su cuenta, consultar sus reservas, crear reserva, iniciar compra, check-in si es elegible.
            //    R: consultar pagos autorizados.
            //    No administra vuelos ni accede a operaciones de terceros (9.8).
            if (!rolemanager.RoleExists("Cliente"))
            {
                rolemanager.Create(new IdentityRole("Cliente"));
            }

            // 2) Pasajero: Perfil "Pasajero" del enunciado. Persona que viaja y hace su check-in.
            //    Solicitado en: 4.1, 9.2 INT-03, 9.8 (perfil Pasajero), 16.1 (columna Pasajero), RF-035, RF-036, HU-016.
            //    Origen: El documento original lo identifica explícitamente.
            //    P: buscar vuelos, gestionar su cuenta, consultar sus reservas, check-in si es elegible.
            //    R: crear reserva, iniciar compra y consultar pagos autorizados.
            //    No obtiene permisos administrativos por ser pasajero (9.8).
            if (!rolemanager.RoleExists("Pasajero"))
            {
                rolemanager.Create(new IdentityRole("Pasajero"));
            }

            // 3) Counter: Perfil "Empleado de counter" del enunciado. Atiende en Quito, Guayaquil, Manta y Galápagos con la misma aplicación (RN-07).
            //    Solicitado en: 4.1, 9.2 INT-04, 9.8 (perfil Empleado de counter), 16.1 (columna Counter), RF-038, RF-039, HU-018, CP-023.
            //    Origen: El documento original lo identifica explícitamente; sus funciones exactas siguen pendientes (DP-13).
            //    P: buscar vuelos.
            //    R: consultar reservas, crear reserva, iniciar compra, pagos autorizados, check-in y reportes.
            //    No puede modificar reglas críticas ni otorgarse permisos (9.8).
            if (!rolemanager.RoleExists("Counter"))
            {
                rolemanager.Create(new IdentityRole("Counter"));
            }

            // 4) Administrador: Perfil "Administrador del sistema" del enunciado.
            //    Solicitado en: 4.1 (administradores propuestos), 9.2 INT-05, 9.8 (perfil Administrador), 16.1 (columna Administrador), RF-005, RF-044, HU-003, HU-006, HU-017, HU-021, HU-025, CP-024.
            //    Origen: Es una propuesta del enunciado (9.2 y 9.8 lo indican); HU-003 pide que gestione los roles.
            //    P: buscar vuelos, consultar reservas, gestionar usuarios y roles y consultar reportes.
            //    R: el resto de funciones, solo bajo condiciones específicas.
            //    Es el único rol que gestiona usuarios y roles (16.1). Sus operaciones sensibles deben quedar auditadas (9.8).
            if (!rolemanager.RoleExists("Administrador"))
            {
                rolemanager.Create(new IdentityRole("Administrador"));
            }

            // 5) Operaciones: Perfil "Responsable de operaciones" (responsable operativo de vuelos) del enunciado.
            //    Solicitado en: 9.2 INT-06, 9.8 (perfil Responsable de operaciones), 16.1 (columna Operaciones), RF-041, RF-047, HU-007, HU-024, 18.2.
            //    Origen: Es una propuesta de diseño del enunciado (9.2 y 9.8), para separar la operación de vuelos (mínimo privilegio).
            //    P: buscar vuelos, administrar aeronaves, administrar asignaciones de vuelos y consultar reportes.
            //    R: consultar reservas, check-in y auditoría.
            //    No modifica tarifas o políticas comerciales sin permiso específico (9.8).
            if (!rolemanager.RoleExists("Operaciones"))
            {
                rolemanager.Create(new IdentityRole("Operaciones"));
            }

            // 6) Comercial: Perfil "Responsable comercial" (responsable comercial y financiero) del enunciado.
            //    Solicitado en: 9.2 INT-07, 9.8 (perfil Responsable comercial), 16.1 (columna Comercial), decisiones DP-04 a DP-08 (tarifas, impuestos, pagos, reservas y cancelaciones).
            //    Origen: Es una propuesta de diseño del enunciado (9.2 y 9.8), para separar la gestión comercial (mínimo privilegio).
            //    P: buscar vuelos, consultar pagos autorizados, gestionar tarifas y reglas comerciales y consultar reportes.
            //    R: consultar reservas, crear reserva, iniciar compra y auditoría.
            //    No altera datos operativos sin autorización (9.8).
            if (!rolemanager.RoleExists("Comercial"))
            {
                rolemanager.Create(new IdentityRole("Comercial"));
            }

        }




        // UserName = Email: la pantalla de login de la plantilla autentica con el correo.
        // Las claves cumplen la política de Identity (mayúscula, minúscula, número y símbolo);
        // si no la cumplen, Create falla en silencio y el usuario no se crea.
        // Son cuentas de prueba, una por rol de la matriz 16.1, para verificar los permisos por rol
        // (CP-004: acceso administrativo indebido; CP-024: gestión administrativa). No subir estas
        // claves a GitHub.
        private void CrearSuperUsuario(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("superadmin@aerolineavip.com");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "superadmin@aerolineavip.com",
                    Email = "superadmin@aerolineavip.com"
                };
                usermanager.Create(user, "SuperAdmin#2026");
            }
        }




        private void CrearAdministrador(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("administrador@aerolineavip.com");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "administrador@aerolineavip.com",
                    Email = "administrador@aerolineavip.com"
                };
                usermanager.Create(user, "Administrador#2026");
            }
        }




        private void CrearOperaciones(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("operaciones@aerolineavip.com");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "operaciones@aerolineavip.com",
                    Email = "operaciones@aerolineavip.com"
                };
                usermanager.Create(user, "Operaciones#2026");
            }
        }




        private void CrearComercial(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("comercial@aerolineavip.com");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "comercial@aerolineavip.com",
                    Email = "comercial@aerolineavip.com"
                };
                usermanager.Create(user, "Comercial#2026");
            }
        }




        private void CrearCounter(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("counter@aerolineavip.com");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "counter@aerolineavip.com",
                    Email = "counter@aerolineavip.com"
                };
                usermanager.Create(user, "Counter#2026");
            }
        }




        private void CrearPasajero(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("pasajero@aerolineavip.com");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "pasajero@aerolineavip.com",
                    Email = "pasajero@aerolineavip.com"
                };
                usermanager.Create(user, "Pasajero#2026");
            }
        }




        private void CrearCliente(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("cliente@aerolineavip.com");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "cliente@aerolineavip.com",
                    Email = "cliente@aerolineavip.com"
                };
                usermanager.Create(user, "Cliente#2026");
            }
        }




        private void AsignarPermisos(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));


            // 1) superadmin: cuenta de respaldo con rol Administrador, para poder entrar a /Roles
            var superAdmin = usermanager.FindByName("superadmin@aerolineavip.com");
            if (superAdmin != null)
            {
                if (!usermanager.IsInRole(superAdmin.Id, "Administrador"))
                {
                    usermanager.AddToRole(superAdmin.Id, "Administrador");
                }
            }


            // 2) administrador
            var administrador = usermanager.FindByName("administrador@aerolineavip.com");
            if (administrador != null)
            {
                if (!usermanager.IsInRole(administrador.Id, "Administrador"))
                {
                    usermanager.AddToRole(administrador.Id, "Administrador");
                }
            }


            // 3) operaciones
            var operaciones = usermanager.FindByName("operaciones@aerolineavip.com");
            if (operaciones != null)
            {
                if (!usermanager.IsInRole(operaciones.Id, "Operaciones"))
                {
                    usermanager.AddToRole(operaciones.Id, "Operaciones");
                }
            }


            // 4) comercial
            var comercial = usermanager.FindByName("comercial@aerolineavip.com");
            if (comercial != null)
            {
                if (!usermanager.IsInRole(comercial.Id, "Comercial"))
                {
                    usermanager.AddToRole(comercial.Id, "Comercial");
                }
            }


            // 5) counter
            var counter = usermanager.FindByName("counter@aerolineavip.com");
            if (counter != null)
            {
                if (!usermanager.IsInRole(counter.Id, "Counter"))
                {
                    usermanager.AddToRole(counter.Id, "Counter");
                }
            }


            // 6) pasajero
            var pasajero = usermanager.FindByName("pasajero@aerolineavip.com");
            if (pasajero != null)
            {
                if (!usermanager.IsInRole(pasajero.Id, "Pasajero"))
                {
                    usermanager.AddToRole(pasajero.Id, "Pasajero");
                }
            }


            // 7) cliente
            var cliente = usermanager.FindByName("cliente@aerolineavip.com");
            if (cliente != null)
            {
                if (!usermanager.IsInRole(cliente.Id, "Cliente"))
                {
                    usermanager.AddToRole(cliente.Id, "Cliente");
                }
            }


        }
    }
}
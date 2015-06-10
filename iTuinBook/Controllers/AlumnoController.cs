using iTuinBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace iTuinBook.Controllers
{
    public class AlumnoController : Controller
    {
        Contexto db = new Contexto();

        //
        // GET: /Alumno/

        public ActionResult Index()
        {
            return View(getGrupos());
        }

        public ActionResult Secuencias()
        {
            var user = getCurrentUser();

            var datosUsuario = from d in db.DatosUsuario
                               where d.UserProfileID == user.UserId
                               select d;

            var datosSimples = from ds in db.DatosSimples
                               from du in datosUsuario
                               where ds.DatosUsuarioID == du.DatosUsuarioID
                               select ds;

            ViewBag.DatosUsuario = datosUsuario.ToList();
            ViewBag.DatosSimples = datosSimples.ToList();                               

            return View();
        }

        #region Funciones de Tareas
        public ActionResult Tareas()
        {
            var user = getCurrentUser();

            var grupos = from i in db.Inscripciones
                         from g in db.Grupos
                         where i.UserId == user.UserId &&
                         g.GrupoID == i.GrupoID &&
                         i.Aceptado == true
                         select g;

            var datosUsuario = from d in db.DatosUsuario
                               where d.UserProfileID == user.UserId
                               select d;

            ViewBag.DatosUsuario = datosUsuario.ToList();

            return View(grupos);
        }
        #endregion

        #region Funciones de Grupo
        public ActionResult Grupos()
        {
            UserProfile user = getCurrentUser();

            var misGrupos = from i in db.Inscripciones
                            from g in db.Grupos
                            where i.UserId == user.UserId &&
                            i.GrupoID == g.GrupoID &&
                            i.Aceptado == true
                            select g;

            ViewBag.MisGrupos = misGrupos;

            var gruposSolicitados = from i in db.Inscripciones
                                    from g in db.Grupos
                                    where i.UserId == user.UserId &&
                                    i.GrupoID == g.GrupoID &&
                                    i.Aceptado == false
                                    select g;

            ViewBag.GruposSolicitados = gruposSolicitados;

            var grupos = from g in db.Grupos
                         where g.Publico == true
                         select g;

            return View(grupos);
        }

        [HttpPost]
        public ActionResult Grupos(string searchString)
        {
            UserProfile user = getCurrentUser();

            var misGrupos = from i in db.Inscripciones
                            from g in db.Grupos
                            where i.UserId == user.UserId &&
                            i.GrupoID == g.GrupoID &&
                            i.Aceptado == true
                            select g;

            ViewBag.MisGrupos = misGrupos;

            var gruposSolicitados = from i in db.Inscripciones
                                    from g in db.Grupos
                                    where i.UserId == user.UserId &&
                                    i.GrupoID == g.GrupoID &&
                                    i.Aceptado == false
                                    select g;

            ViewBag.GruposSolicitados = gruposSolicitados;

            var grupos = from g in db.Grupos
                         select g;

            if (!String.IsNullOrEmpty(searchString))
            {
                grupos = grupos.Where(g => g.Nombre.ToUpper().Contains(searchString.ToUpper()));
            }
            else
            {
                grupos = grupos.Where(g => g.Publico == true);
            }

            return View(grupos);        
        }

        public ActionResult Solicitud(int id)
        {
            UserProfile user = getCurrentUser();

            var inscripciones = from i in db.Inscripciones
                                where i.UserId == user.UserId && i.GrupoID == id
                                select i;

            if (!(inscripciones.Count() > 0))
            {
                db.Inscripciones.Add(new Inscripcion { UserId = user.UserId, GrupoID = id, Aceptado = false });
                db.SaveChanges();
            }

            return RedirectToAction("Grupos");
        }

        public ActionResult Unirse(int id)
        {
            UserProfile user = getCurrentUser();

            var inscripciones = from i in db.Inscripciones
                                where i.UserId == user.UserId && i.GrupoID == id
                                select i;

            if (!(inscripciones.Count() > 0))
            {
                db.Inscripciones.Add(new Inscripcion { UserId = user.UserId, GrupoID = id, Grupo = db.Grupos.Find(id), Aceptado = true });
                user.Grupos.Add(db.Grupos.Find(id));
                db.SaveChanges();
            }

            return RedirectToAction("Grupos");
        }

        public ActionResult Abandonar(int id)
        {
            UserProfile user = getCurrentUser();

            var inscripcion = (from i in db.Inscripciones
                               where i.UserId == user.UserId && i.GrupoID == id
                               select i).Single();

            db.Inscripciones.Remove(inscripcion);

            db.SaveChanges();

            return RedirectToAction("Grupos");
        }    

        #endregion

        #region Funciones Extras
        // Devuelve el UserProfile del usuario actual
        private UserProfile getCurrentUser()
        {
            UserProfile user = (from u in db.UserProfiles
                                where u.UserName == User.Identity.Name
                                select u).FirstOrDefault();

            return user;
        }

        private IEnumerable<Grupo> getGrupos()
        {
            UserProfile user = getCurrentUser();

            var misGrupos = from i in db.Inscripciones
                            from g in db.Grupos
                            where i.UserId == user.UserId &&
                            i.GrupoID == g.GrupoID &&
                            i.Aceptado == true
                            select g;

            return misGrupos;
        }
        #endregion
    }
}

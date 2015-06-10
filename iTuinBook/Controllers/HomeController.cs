using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using iTuinBook.Models;


namespace iTuinBook.Controllers
{
    public class HomeController : Controller
    {
        Contexto db = new Contexto();

        public ActionResult Index()
        {
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            if (User.Identity.IsAuthenticated)
            {
                UserProfile user = (from u in db.UserProfiles
                                    where u.UserName == User.Identity.Name
                                    select u).FirstOrDefault();

                if (user != null)
                {
                    switch (user.Type)
                    {
                        case "Alumno":
                            return RedirectToAction("Index", "Alumno");
                        case "Docente":
                            return RedirectToAction("Index", "Docente");
                        default:
                            return RedirectToAction("Index", "Invitado");
                    }
                }
                else
                    return View();
            }
            else
            {
                return View();
            }     
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}

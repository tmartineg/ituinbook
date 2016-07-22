using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ReadAndLearn.Models;
using System.Text.RegularExpressions;
using NLog;

namespace ReadAndLearn.Controllers
{
    public class ReadAndLearnController : Controller
    {
        
        private static Logger logger = LogManager.GetCurrentClassLogger();
        
        Contexto db = new Contexto();
        
        //Generamos ext para poder usar métodos como ext.GetUsuarioID()
        ExternalMethods ext = new ExternalMethods();
      

        // GET: /ReadAndLearn/
        
        // Indica el inicio o la continuación de una tarea por parte del alumno.
        public ActionResult Iniciar(int GrupoID, int ModuloID, int tmpActual, int accActual,string moment = "")        
        {
            //guirisan/secuencias
            int numAccion = 1;
            DateTime datetimeclient;
            if (moment != "")
            {
                datetimeclient = DateTime.Parse(moment);
            }
            else
            {
                //DateTimeOffset dto = new DateTimeOffset(DateTime.Now.Date);
                //datetimeclient = dto.DateTime;
                datetimeclient = DateTime.Now;
            }
            

            logger.Debug("ReadAndLearnController/Iniciar");
            var user = (from u in db.UserProfiles
                        where User.Identity.Name == u.UserName
                        select u).Single();

            // intentamos recuperar los DatosUsuario referidos a esta tarea. pueden ser 0 (si se está empezando el test)
            var datosUser = (from dat in db.DatosUsuario
                             where dat.ModuloID == ModuloID &&
                             dat.GrupoID == GrupoID &&
                             dat.UserProfileID == user.UserId
                             select dat).ToList();

            
            if (datosUser.Count == 0) 
            {
                // No hay secuencia, primer inicio de la tarea
                // Creamos el DatoUsuario con los detalles de inicio de tarea
                DatosUsuario du = new DatosUsuario() { GrupoID = GrupoID, ModuloID = ModuloID, Cerrada = false, EscenaActual = 0, PreguntaActual = 0, TextoActual = 0, AccionActual = 0, IndAcierto = 0, IndSelec = 0, IndUsoAyud = 0, Puntos = 0, Inicio = datetimeclient, UserProfileID = user.UserId, UserProfile = user, DatoSimple = new List<DatoSimple> (),
                                                       SeleccionNeg = 0, SeleccionPos = 0, RespuestaPos = 0, RespuestaNeg = 0, AyudaPos = 0, AyudaNeg = 0, BuscaNeg = 0, BuscaPos = 0, RevisaNeg = 0, RevisaPos = 0, RevisaStatus = 1};
                // creamos el datosimple de inicio de la tarea (codeop=1)
                DatoSimple ds = new DatoSimple() { CodeOP = 1, Momento = datetimeclient, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, NumAccion = 1 };

                du.DatoSimple.Add(ds);

                db.DatosUsuario.Add(du);

                db.SaveChanges();

                // añadimos el datosimple del momento de inicio
                // mientras CodeOP = 1 indica el primer inicio, CodeOP = 2 puede marcar una continuación, por eso es necesario añadir dos datos simples.
                du.DatoSimple.Add(new DatoSimple() { CodeOP = 2, Momento = datetimeclient, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, NumAccion = 1});
                
                db.SaveChanges();

                Session.Timeout = 120;
                // añadimos a la sesion la referencia al ID del DatosUsuario creado
                Session["DatosUsuarioID"] = du.DatosUsuarioID;

                //guirisan/secuencias
                
                //print datosRow
                string datosRow = numAccion + "__url:[/ReadAndLearnController/Iniciar]__data:[" + "GrupoID=" + GrupoID + "_ModuloID=" + ModuloID + "_tmpActual=" + tmpActual
                + "_accActual=" + accActual + "_moment=" + moment + "]";
                //print to file datosRow
                ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name),GrupoID,ModuloID,datosRow);

                //Al iniciar un test por vez primera, se generan dos datos simples (codeop=1 y codeop=2),
                //a partir de una única acción del usuario. Por tanto, devolveremos al navegador numAccion=2
                //para saber que la siguiente, será la segunda acción que realice el usuario
                //numAccion = 2;
                //NOTA: Se ha comentado ya que el cliente suma +1 a numAccion antes de generar los datosRaw
                //así que le pasamos el número de la última acción realizada, no de la próxima a realizar
                
            }
            else 
            {
                // Hay Secuencia, continuación de la tarea

                //guirisan/secuencias
                //capturamos último DatoSimple y obtenemos el valor del último numAccion
                numAccion = datosUser.First().DatoSimple.Last().NumAccion + 1;
                
                //añadimos el datosimple del momento inicial (se refiere al instante en que se retoma la actividad)
                DatoSimple ds = new DatoSimple() { CodeOP = 2, Momento = datetimeclient, DatosUsuario = datosUser.First(), DatosUsuarioID = datosUser.First().DatosUsuarioID, Dato01 = datosUser.First().AccionActual, Dato02 = 1, NumAccion = numAccion };
                datosUser.First().DatoSimple.Add(ds);
                datosUser.First().RevisaStatus = 1;
                db.SaveChanges();

                Session.Timeout = 120;
                Session["DatosUsuarioID"] = datosUser.First().DatosUsuarioID;

                //guirisan/secuencias
                string datosRow = numAccion + "__url:[/ReadAndLearnController/Iniciar]__data:[" + "GrupoID=" + GrupoID + "_ModuloID=" + ModuloID + "_tmpActual=" + tmpActual
                + "_accActual=" + accActual + "_moment=" + moment + "]";
                //print to file datosRow
                ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, datosRow);


            }
            
            // cargamos el módulo que está realizando el usuario
            var modulo = db.Modulos.Find(ModuloID);

            // intentamos cargar su configuración. si no existe una en la BD, lo dejamos en null
            ConfigModulo configModulo = new ConfigModulo();
            try
            {
                configModulo = (from c in db.ConfigModulo
                                    where c.ModuloID == modulo.ModuloID
                                    select c).First();
            }
            catch (Exception)
            {
                configModulo = null;
            }

            DatosUsuario datUser;

            switch(modulo.Condicion)
            {
                case 1: // iTextBook
                    datUser = db.DatosUsuario.Find(Session["DatosUsuarioID"]);

                    switch (configModulo.Plantilla)
                    { 
                        case 0:
                            logger.Debug("modulo.condicion: 1 (iTextBook) , configModulo.Plantilla : 0, redirect to PL0_experimentos/PL0_Texto");
                            //guirisan/secuencias: añadido numAccion al RedirectToAction como parámetro
                            return RedirectToAction("PL0_Texto", "PL0_Experimentos", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = datUser.TextoActual, db, NumAccion = numAccion, inicioTexto = true });
                        case 1:
                            logger.Debug("modulo.condicion: 1 (iTextBook) , configModulo.Plantilla : 1, redirect to PL2_experimentos/PL2_Texto");
                            //guirisan/secuencias: añadido numAccion al RedirectToAction como parámetro
                            return RedirectToAction("PL2_Texto", "PL2_Experimentos", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = datUser.TextoActual, NumAccion = numAccion });
                        case 2:
                            //return RedirectToAction("PL2_Texto", "PL2_Experimentos", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = datUser.TextoActual });                            
                        case 3:
                            return RedirectToAction("PL3_Texto", "PL3_Experimentos", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = datUser.TextoActual, inicioTexto = true });
                        //guirisan/issues https://github.com/guirisan/ituinbook/issues/83
                        //ruta añadida para la nueva plantilla de fdbk auditivo
                        case 4:
                            return RedirectToAction("PL4_Texto", "PL4_Experimentos", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = datUser.TextoActual, inicioTexto = true });

                        default:
                            break;
                    }
                    
                    return RedirectToAction("Texto", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = datUser.TextoActual, moment = datetimeclient, numAccion = numAccion });                           
                case 2://tuinlec
                case 3://tuinlec
                    datUser = db.DatosUsuario.Find(Session["DatosUsuarioID"]);
                    if (modulo.Escenas.Count() > 0)
                    {
                        return RedirectToAction("Area", new { GrupoID = datUser.GrupoID, ModuloID = datUser.ModuloID, escActual = datUser.EscenaActual, accActual = datUser.AccionActual, numAccion = numAccion });
                    }
                    else
                    {
                        return RedirectToAction("Texto", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = tmpActual, moment = datetimeclient });
                    }                    
                case 4://sin uso
                        datUser = db.DatosUsuario.Find(Session["DatosUsuarioID"]);
                        return RedirectToAction("PL2_Texto", "PL2_Experimentos", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = datUser.TextoActual });
                           
                        
                default://sin uso?
                    datUser = db.DatosUsuario.Find(Session["DatosUsuarioID"]);

                    switch (configModulo.Plantilla)
                    { 
                        case 0:
                            //return RedirectToAction("PL0_Texto", "PL0_Experimentos", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = datUser.TextoActual, db });
                        case 1:
                            //return RedirectToAction("PL1_Texto", "PL1_Experimentos", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = datUser.TextoActual});                            
                        case 2:
                            return RedirectToAction("PL2_Texto", "PL2_Experimentos", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = datUser.TextoActual });                            
                        case 3:
                            break;
                        default:
                            break;
                    }
                    
                    return RedirectToAction("Texto", new { GrupoID = GrupoID, ModuloID = ModuloID, textoActual = datUser.TextoActual, moment = datetimeclient });                       
            };
             
            //Texto texto = db.Modulos.Find(ModuloID).Textos.First();
            
            
            //codigo inaccesible, eliminar?
            //return RedirectToAction("Escena", new { GrupoID = GrupoID, ModuloID = ModuloID, escActual = 0 });
        }

        public ActionResult Area(int GrupoID, int ModuloID, int accActual, int escActual, int numAccion)
        {
            logger.Debug("ReadAndLearnController/Area");
            DatosUsuario du = db.DatosUsuario.Find(Session["DatosUsuarioID"]);

            var user = (from u in db.UserProfiles
                        where User.Identity.Name == u.UserName
                        select u).Single();

            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            foreach (Escena acc in db.Escenas)
            {
                continue;
            }

            Modulo modulo = db.Modulos.Find(ModuloID);

            Escena escena = db.Escenas.Find(modulo.Escenas.ElementAt(escActual).EscenaID);

            Accion accion = escena.Acciones.OrderBy(acc => acc.Orden).ElementAt(Convert.ToInt32(accActual));

            //Accion accion = escena.Acciones.ElementAt(accActual);

            ViewBag.Nombre = user.Nombre;
            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;
            ViewBag.escActual = escActual;
            ViewBag.accActual = Convert.ToInt32(accActual);
            ViewBag.TextoID = accion.TextoID;
            ViewBag.PreguntaID = accion.PreguntaID;
            ViewBag.Accion = accion;
            ViewBag.numAccion = numAccion;

            return View();
        }

        public ActionResult CambiarAccion(int GrupoID, int ModuloID, int accActual, int escActual, string moment, string dataRow = "", int numAccion = -1)
        {
            //guirisan/secuencias
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            logger.Debug("ReadAndLearnController/CambiarAccion");
            string mensaje = "";

            var user = (from u in db.UserProfiles
                        where User.Identity.Name == u.UserName
                        select u).Single();

            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            foreach (Escena acc in db.Escenas)
            {
                continue;
            }

            Modulo modulo = db.Modulos.Find(ModuloID);

            Escena escena = db.Escenas.Find(modulo.Escenas.ElementAt(escActual).EscenaID);
            DateTime datetimeclient = DateTime.Parse(moment);
            if (accActual < escena.Acciones.Count)
            {
                Accion accion = escena.Acciones.OrderBy(acc => acc.Orden).ElementAt(Convert.ToInt32(accActual));

                DatosUsuario du = db.DatosUsuario.Find(Session["DatosUsuarioID"]);
                
                DatoSimple ds = new DatoSimple() { CodeOP = 3, Momento = datetimeclient, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Dato01 = accActual, Dato02 = accion.AccionID, NumAccion = numAccion };
                
                db.DatosSimples.Add(ds);

                if (accActual > du.AccionActual)
                {
                    du.AccionActual = accActual;

                    db.SaveChanges();
                }

                db.SaveChanges();

                if (accion.Mensaje != null)
                    mensaje = accion.Mensaje.Replace("USUARIO", user.Nombre);

                return Json(new { Puntos = du.Puntos, Mensaje = mensaje, CodeOP = accion.CodeOP, TextoID = accion.TextoID, PaginaID = accion.PaginaID, PreguntaID = accion.PreguntaID, EscenaID = accion.EscenaID, JsonRequestBehavior.AllowGet });
            }
            else
            {
                //if (!accActual < escena.Acciones.Count) significa que ha acabado. codeOP=100 -> siguiente pregunta
                DatosUsuario du = db.DatosUsuario.Find(Session["DatosUsuarioID"]);

                DatoSimple ds = new DatoSimple() { CodeOP = 100, Momento = datetimeclient, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Dato01 = accActual, NumAccion = numAccion };

                //guirisan/secuencias
                //FIN DE ESCENA
                ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow + "__fin-de-area");

                du.Cerrada = true;

                db.DatosSimples.Add(ds);

                if (accActual > du.AccionActual)
                {
                    du.AccionActual = accActual;

                    db.SaveChanges();
                }

                db.SaveChanges();

                return Json(new { redirect = Url.Action("Tareas", "Alumno"), Puntos = du.Puntos, Mensaje = mensaje, CodeOP = 100, JsonRequestBehavior.AllowGet });
            }
        }

        string RemoveBetween(string s, char begin, char end)
        {
            logger.Debug("ReadAndLearnController/RemoveBetween");
            Regex regex = new Regex(string.Format("\\{0}.*?\\{1}", begin, end));
            return regex.Replace(s, string.Empty);
        }

        public ActionResult SeleccionPertinente(int PreguntaID, string Seleccion)
        {
            logger.Debug("ReadAndLearnController/SeleccionPertinente");
            Pregunta pregunta = db.Preguntas.Find(PreguntaID);
            Pagina pagina = pregunta.Texto.Paginas.First();
            string respuesta = Seleccion;

            // SELECCION PERTINENTE //
            int CharPertPregunta = 0;
            string contenido = "";

            List<List<List<int>>> rangos = new List<List<List<int>>>();

            rangos.Add(new List<List<int>>());

            contenido = pagina.Contenido;

            contenido = Server.UrlDecode(contenido);
            contenido = RemoveBetween(contenido, '<', '>');
            contenido = HttpUtility.HtmlDecode(contenido);
            contenido = contenido.Replace("&nbsp;", " ");
            contenido = contenido.Replace(System.Environment.NewLine, " ");

            contenido = contenido.Replace((char)160, (char)32);

            string[] frases = pregunta.Pertinente.Split('/');

            CharPertPregunta = pregunta.Pertinente.Length - frases.Count();

            foreach (string frase in frases)
            {
                string strTmp = frase.Replace((char)160, (char)32);

                //strTmp = strTmp.Replace(

                if (contenido.IndexOf(strTmp) != -1)
                {
                    rangos[rangos.Count - 1].Add(new List<int>());

                    int ini = contenido.IndexOf(strTmp);
                    int fin = ini + frase.Length;

                    rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(ini);
                    rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(fin);
                }
            }

            string[] selecciones = respuesta.Split('\\');

            int ragInf;
            int ragSup;

            string pertCorr = "";
            string pertTmp = "";

            foreach (string select in selecciones)
            {
                pertCorr = select.Replace("\n", "");

                pertCorr = pertCorr.Replace((char)160, (char)32);

                string seleccion = select.Replace("\n", "");

                contenido = pagina.Contenido;

                contenido = Server.UrlDecode(contenido);

                contenido = RemoveBetween(contenido, '<', '>');

                contenido = HttpUtility.HtmlDecode(contenido);
                contenido = contenido.Replace("&nbsp;", " ");

                contenido = contenido.Replace(System.Environment.NewLine, " ");

                contenido = contenido.Replace((char)160, (char)32);
                seleccion = seleccion.Replace((char)160, (char)32);

                if (contenido.IndexOf(seleccion) != -1)
                {
                    ragInf = contenido.IndexOf(seleccion);
                    ragSup = ragInf + seleccion.Length;

                    foreach (List<int> posible in rangos[0].Reverse<List<int>>())
                    {
                        if (ragInf > posible[1] || ragSup < posible[0])
                        {
                            continue;
                        }
                        else
                        {
                            if (ragInf >= posible[0])
                            {
                                if (ragSup <= posible[1]) // Esta todo dentro
                                {
                                    pertCorr = "<span style=\"color:green\">" + pertCorr + "</span>";
                                    continue;
                                }
                                else // Sobresale por la derecha
                                {
                                    pertCorr = "<span style=\"color:green\">" + pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                    continue;
                                }
                            }
                            else
                            {
                                if (ragSup <= posible[1]) // Sobresale por la izquierda
                                {
                                    pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">") + "</span>";
                                    continue;
                                }
                                else // Sobresale por ambos lados
                                {
                                    pertCorr = pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                    pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">");
                                    continue;
                                }
                            }
                        }
                    }

                    pertTmp += pertCorr + "\\";
                }
            }

            return Json(new { result = Server.HtmlEncode(pertTmp) });            
        }

        public string AlgoritmoSeleccion(string Contenido, string respuesta, int PreguntaID)
        {
            logger.Debug("ReadAndLearnController/AlgoritmoSeleccion");
            // SELECCION PERTINENTE //
            int CharPertPregunta = 0;
            string contenido = "";

            Pregunta pregunta = db.Preguntas.Find(PreguntaID);

            List<List<List<int>>> rangos = new List<List<List<int>>>();

            rangos.Add(new List<List<int>>());

            contenido = Contenido;
            
            contenido = Server.UrlDecode(contenido);
            contenido = RemoveBetween(contenido, '<', '>');
            contenido = HttpUtility.HtmlDecode(contenido);
            contenido = contenido.Replace("&nbsp;", " ");
            contenido = contenido.Replace(System.Environment.NewLine, " ");

            contenido = contenido.Replace((char)160, (char)32);

            string[] frases = pregunta.Pertinente.Split('/');

            CharPertPregunta = pregunta.Pertinente.Length - frases.Count();

            foreach (string frase in frases)
            {
                string strTmp = frase.Replace((char)160, (char)32);

                //strTmp = strTmp.Replace(

                if (contenido.IndexOf(strTmp) != -1)
                {
                    rangos[rangos.Count - 1].Add(new List<int>());

                    int ini = contenido.IndexOf(strTmp);
                    int fin = ini + frase.Length;

                    rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(ini);
                    rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(fin);
                }
            }

            string[] selecciones = respuesta.Split('\\');

            int ragInf;
            int ragSup;

            int tmp1 = 0; // Número de caracteres pertinentes            

            int CharPertinentes = 0;
            int totalCharSelect = 0;

            string pertCorr = "";
            string pertTmp = "";

            foreach (string select in selecciones)
            {
                pertCorr = select.Replace("\n", "");

                pertCorr = pertCorr.Replace((char)160, (char)32);

                string seleccion = select.Replace("\n", "");

                contenido = Contenido;

                contenido = Server.UrlDecode(contenido);

                contenido = RemoveBetween(contenido, '<', '>');

                contenido = HttpUtility.HtmlDecode(contenido);
                contenido = contenido.Replace("&nbsp;", " ");

                contenido = contenido.Replace(System.Environment.NewLine, " ");

                contenido = contenido.Replace((char)160, (char)32);
                seleccion = seleccion.Replace((char)160, (char)32);

                if (contenido.IndexOf(seleccion) != -1)
                {
                    ragInf = contenido.IndexOf(seleccion);
                    ragSup = ragInf + seleccion.Length;

                    foreach (List<int> posible in rangos[0].Reverse<List<int>>())
                    {
                        if (ragInf > posible[1] || ragSup < posible[0])
                        {
                            continue;
                        }
                        else
                        {
                            if (ragInf >= posible[0])
                            {
                                if (ragSup <= posible[1]) // Esta todo dentro
                                {
                                    tmp1 += seleccion.Length;
                                    pertCorr = "<span style=\"color:green\">" + pertCorr + "</span>";
                                    continue;
                                }
                                else // Sobresale por la derecha
                                {
                                    tmp1 += seleccion.Length - (ragSup - posible[1]);
                                    pertCorr = "<span style=\"color:green\">" + pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                    continue;
                                }
                            }
                            else
                            {
                                if (ragSup <= posible[1]) // Sobresale por la izquierda
                                {
                                    tmp1 += seleccion.Length - (posible[0] - ragInf);
                                    pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">") + "</span>";
                                    continue;
                                }
                                else // Sobresale por ambos lados
                                {
                                    tmp1 += seleccion.Length - (ragSup - posible[1]) - (posible[0] - ragInf);
                                    pertCorr = pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                    pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">");
                                    continue;
                                }
                            }
                        }
                    }

                    pertTmp += pertCorr + "\\";
                }

                CharPertinentes += tmp1;

                totalCharSelect += select.Length;

                tmp1 = 0;
            }

            respuesta = Server.HtmlEncode(pertTmp);
            //respuesta = Server.HtmlEncode(pertTmp).Replace('&', '?');

            // Porcentaje de texto pertinente seleccionado frente al texto pertinente de la pregunta.
            double porcPert = ((double)CharPertinentes * 100.0) / (double)CharPertPregunta;
            // Porcentaje de texto no pertinente seleccionado frente a texto pertinente seleccionado.
            double porcNoPert = 100.0 - ((double)CharPertinentes * 100.0) / (double)totalCharSelect;

            return respuesta;
        }

        [HttpPost]
        public ActionResult ValidarPreguntaSeleccion(int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {
            logger.Debug("ReadAndLearnController/ValidarPreguntaSeleccion");
            Pregunta pregunta = db.Preguntas.Find(PreguntaID);
            Pagina pagina = pregunta.Texto.Paginas.First();
            string respOriginal = respuesta;

            DatosUsuario du = db.DatosUsuario.Find(Session["DatosUsuarioID"]);

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), du.GrupoID, du.ModuloID, dataRow);

            // SELECCION PERTINENTE //
            int CharPertPregunta = 0;
            string contenido = "";
            
            List<List<List<int>>> rangos = new List<List<List<int>>>();

            rangos.Add(new List<List<int>>());

            contenido = pagina.Contenido;

            contenido = Server.UrlDecode(contenido);
            contenido = RemoveBetween(contenido, '<', '>');
            contenido = HttpUtility.HtmlDecode(contenido);
            contenido = contenido.Replace("&nbsp;", " ");
            contenido = contenido.Replace(System.Environment.NewLine, " ");

            contenido = contenido.Replace((char)160, (char)32);

            string[] frases = pregunta.Pertinente.Split('/');

            CharPertPregunta = pregunta.Pertinente.Length - frases.Count();

            foreach (string frase in frases)
            {
                string strTmp = frase.Replace((char)160, (char)32);

                //strTmp = strTmp.Replace(

                if (contenido.IndexOf(strTmp) != -1)
                {
                    rangos[rangos.Count - 1].Add(new List<int>());

                    int ini = contenido.IndexOf(strTmp);
                    int fin = ini + frase.Length;

                    rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(ini);
                    rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(fin);
                }
            }

            string[] selecciones = respuesta.Split('\\');
            
            int ragInf;
            int ragSup;

            int tmp1 = 0; // Número de caracteres pertinentes            

            int CharPertinentes = 0;
            int totalCharSelect = 0;

            string pertCorr = "";
            string pertTmp = "";

            foreach (string select in selecciones)
            {
                pertCorr = select.Replace("\n", "");

                pertCorr = pertCorr.Replace((char)160, (char)32);
                
                string seleccion = select.Replace("\n", "");

                contenido = pagina.Contenido;

                contenido = Server.UrlDecode(contenido);

                contenido = RemoveBetween(contenido, '<', '>');

                contenido = HttpUtility.HtmlDecode(contenido);
                contenido = contenido.Replace("&nbsp;", " ");

                contenido = contenido.Replace(System.Environment.NewLine, " ");

                contenido = contenido.Replace((char)160, (char)32);
                seleccion = seleccion.Replace((char)160, (char)32);

                if (contenido.IndexOf(seleccion) != -1)
                {
                    ragInf = contenido.IndexOf(seleccion);
                    ragSup = ragInf + seleccion.Length;

                    foreach (List<int> posible in rangos[0].Reverse<List<int>>())
                    {
                        if (ragInf > posible[1] || ragSup < posible[0])
                        {
                            continue;
                        }
                        else
                        {
                            if (ragInf >= posible[0])
                            {
                                if (ragSup <= posible[1]) // Esta todo dentro
                                {
                                    tmp1 += seleccion.Length;
                                    pertCorr = "<span style=\"color:green\">" + pertCorr + "</span>";
                                    continue;
                                }
                                else // Sobresale por la derecha
                                {
                                    tmp1 += seleccion.Length - (ragSup - posible[1]);
                                    pertCorr = "<span style=\"color:green\">" + pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                    continue;
                                }
                            }
                            else
                            {
                                if (ragSup <= posible[1]) // Sobresale por la izquierda
                                {
                                    tmp1 += seleccion.Length - (posible[0] - ragInf);
                                    pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">") + "</span>";
                                    continue;
                                }
                                else // Sobresale por ambos lados
                                {
                                    tmp1 += seleccion.Length - (ragSup - posible[1]) - (posible[0] - ragInf);
                                    pertCorr = pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                    pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">");
                                    continue;
                                }
                            }
                        }
                    }

                    pertTmp += pertCorr + "\\";
                }
                
                CharPertinentes += tmp1;

                totalCharSelect += select.Length;

                tmp1 = 0;
            }

            respuesta = Server.HtmlEncode(pertTmp);
            //respuesta = Server.HtmlEncode(pertTmp).Replace('&', '?');

            // Porcentaje de texto pertinente seleccionado frente al texto pertinente de la pregunta.
            double porcPert = ((double)CharPertinentes * 100.0) / (double)CharPertPregunta;
            // Porcentaje de texto no pertinente seleccionado frente a texto pertinente seleccionado.
            double porcNoPert = 100.0 - ((double)CharPertinentes * 100.0) / (double)totalCharSelect;
            string mensaje = "";

            if (totalCharSelect == 0)
                
            {
                porcNoPert = 100;
            }

            DatoSimple ds = new DatoSimple() { CodeOP = 6, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Info = respOriginal, PreguntaID = PreguntaID, Dato01 = (float)porcPert, Dato02 = (float)porcNoPert, NumAccion = numAccion };

            db.DatosSimples.Add(ds);

            db.SaveChanges();


            if (porcNoPert > 25) // SI IRRELEVANTE
            {
                if (porcPert < 15) //NADA
                {
                    du.Puntos += 0;
                    mensaje += "La selección no es adecuada. Ahora es importantísimo que revises en el texto cuál es la información que necesitas para responder bien. Si tienes dudas avisa al profesor.";
                }
                else // PARTE o TODO
                {   
                    if (porcPert > 65) // TODO
                    {
                        du.Puntos += 70;

                        mensaje += "Tu selección es bastante buena aunque sobra alguna información. Ahora revisa en el texto la selección correcta, te ayudará a responder bien.";
                    }
                    else // PARTE
                    {
                        du.Puntos += 20;

                        mensaje += "Revisa la selección correcta en el texto y compárala con tu selección para ver cómo podría mejorarse. Fíjate en la selección correcta para responder.";
                    }
                }
            }
            else // NO IRRELEVANTE
            {
                if (porcPert < 15) //NADA
                {
                    du.Puntos += 15;

                    mensaje += "Solo has seleccionado UNA PEQUEÑA PARTE de la información necesaria. Es muy importante que revises en el texto la selección correcta, fíjate en ella para responder. Si tienes dudas avisa al profesor.";
                }
                else // PARTE o TODO
                {
                    if (porcPert > 65) // TODO
                    {
                        du.Puntos += 100;

                        mensaje += "¡Excelente selección! Fíjate en ella para responder a la pregunta. ¡Sigue así!";
                    }
                    else // PARTE
                    {
                        du.Puntos += 60;

                        mensaje += "Has seleccionado bien pero te falta alguna información. Revisa en el texto la selección correcta completa y fíjate en ella para responder.";
                    }
                }
            }

            db.SaveChanges();

            return Json(new { Mensaje = mensaje, Correccion = respuesta });
        }
        
        //ValidarPreguntaParejas
        [HttpPost]
        public ActionResult ValidarPreguntaParejas(int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {
            DatosUsuario du = db.DatosUsuario.Find(Session["DatosUsuarioID"]);

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), du.GrupoID, du.ModuloID, dataRow);
            
            logger.Debug("ReadAndLearnController/ValidarPreguntaParejas");
            Pregunta preg = db.Preguntas.Find(PreguntaID);
            ConfigPregunta configPreg = db.ConfigPregunta.Single(c => c.PreguntaID == PreguntaID);

            ViewBag.Pagina = preg.Texto.Paginas.First();
            
            DatoSimple ds = new DatoSimple() { CodeOP = 5, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Info = respuesta, PreguntaID = PreguntaID, NumAccion = numAccion };

            db.DatosSimples.Add(ds);

            db.SaveChanges();

            string[] param = respuesta.Split('/');
            int cont = 0;
            int acierto = 0;
            int total = 0;

            foreach(string opcion in param)
            {
                foreach (string str in opcion.Split('-'))
                {
                    if (str != param[cont].Split('-')[0] && str != param[cont].Split('-')[param[cont].Split('-').Length - 1])
                    {
                        if (preg.Emparejados.ToList()[cont].ColDer != null && preg.Emparejados.ToList()[cont].ColDer.IndexOf(str) > -1)
                        {
                            acierto++;
                        }
                    }
                }

                cont++;
            }

            foreach (Emparejado par in preg.Emparejados)
            {
                if (par.ColIzq == null)
                {
                    foreach (string str in par.ColDer.Split('/'))
                    {
                        if (!(respuesta.IndexOf(str) > -1))
                        {
                            acierto++;
                        }

                        total++;
                    }
                }
                else
                {
                    if (par.ColDer != null)
                    {
                        foreach (string str in par.ColDer.Split('/'))
                        {
                            total++;
                        }
                    }
                }            
            }


            double porc = Convert.ToDouble(acierto) * 100.0 / Convert.ToDouble(total);
            string mensaje = "";

            if (configPreg.CorregirSeleccion)
            {
                if (porc < 30)
                {
                    du.Puntos += 0;

                    mensaje = "USUARIO, ahora es muy importante que revises atentamente la Solución Correcta. Después, compárala con tu solución y piensa en los errores. Esto te ayudará a mejorar. Si tienes dudas, avisa a tu profesor.";
                }
                else
                {
                    if (30 <= porc && porc < 65)
                    {
                        du.Puntos += 50;

                        mensaje = "USUARIO, revisa la Solución Correcta y compárala con la tuya. Pensar sobre los errores te ayudará a mejorar.";
                    }
                    else
                    {
                        if (65 <= porc && porc < 99)
                        {
                            du.Puntos += 80;

                            mensaje = "Hay algún error en tu tarea. Consulta la Solución Correcta y compárala con la tuya. Te ayudará.";
                        }
                        else // Perfecto
                        {
                            du.Puntos += 100;

                            mensaje = "¡Perfecto!";
                        }
                    }
                }
            }
            else
            {
                du.Puntos += 100;
                mensaje = "Sigamos.";
            }
            db.SaveChanges();

            return Json(new { redirect = Url.Action("PreguntasPajeraCorregida", new { PreguntaID = PreguntaID, Respuesta = respuesta }), Mensaje = mensaje, PreguntaID = preg.PreguntaID, Respuesta = respuesta });
        }

        public ActionResult PreguntasPajeraCorregida(int PreguntaID, string Respuesta)
        {
            logger.Debug("ReadAndLearnController/PreguntasPajeraCorregida");
            Pregunta preg = db.Preguntas.Find(PreguntaID);
            ViewBag.Pagina = preg.Texto.Paginas.First();
            ConfigPregunta configPreg = db.ConfigPregunta.Single(c => c.PreguntaID == PreguntaID);

            string seleccion = "";
            string[] param = Respuesta.Split('/');
            List<string> derecha = new List<string>();
            string[] parejas;
            int cont = 0;
            var rnd = new Random();
            string tinterio_bien = "";
            string tinterio_mal = "";
            string[] resp = Respuesta.Split('/');
            bool flag = false;
            /*
            foreach (string str in param[cont].Split('-'))
            {
                if (str != param[cont].Split('-')[0] && str != param[cont].Split('-')[param[cont].Split('-').Length - 1])
                {
                    foreach (Emparejado emp in preg.Emparejados)
                    {
                        if (emp.ColDer != null)
                        {
                            parejas = emp.ColDer.Split('/');

                            foreach (string str2 in parejas)
                            {
                                if (str2 == str)
                                {
                                    flag = true;
                                    break;
                                }                                
                            }
                        }
                    }

                    if (flag)
                    {
                        tinterio += str + "/";
                    }
                }
            }    
            */
            ViewBag.Config = configPreg;
            ViewBag.Respuesta = Respuesta;

            string tinterio_sistema = "";

            foreach (Emparejado emp in preg.Emparejados)
            {
                if (emp.ColIzq == null)
                {
                    tinterio_sistema += emp.ColDer + "/";
                }

                if (emp.ColDer != null)
                {
                    parejas = emp.ColDer.Split('/');

                    foreach (string str in parejas)
                    {
                        if (Respuesta.IndexOf(str) == -1)
                        {
                            if(emp.ColIzq == null)
                                tinterio_bien += str + "/";
                            else
                                tinterio_mal += str + "/";
                        }

                        derecha.Add(str);
                    }
                }
            }

            var result = derecha.OrderBy(item => rnd.Next());

            ViewBag.TinterioSistema = tinterio_sistema;
            ViewBag.TinterioBien = tinterio_bien;
            ViewBag.TinterioMal = tinterio_mal;

            ViewBag.Lista = result.ToList();
            
            return View(db.Preguntas.Find(PreguntaID));
        }



        [HttpPost]
        public ActionResult ValidarPreguntaClaves(int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {
            DatosUsuario du = GetDatosUsuario();
            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), du.GrupoID, du.ModuloID, dataRow); 
            
            logger.Debug("ReadAndLearnController/ValidarPreguntaClaves");
            Pregunta preg = db.Preguntas.Find(PreguntaID);
            string Mensaje = "";
            string results = "";
            string errores = "";

            if (preg.Claves != null)
            {
                string[] param = Regex.Split(preg.Claves, "//");

                string[] clavesM = param[0].Split('/');
                string[] clavesO = param[1].Split('/');

                string[] palabras = respuesta.Split('/');

                palabras = palabras.Where(p => p != palabras[palabras.Length - 1]).ToArray();

                foreach (string str in palabras)
                {
                    bool flag_correcta = false;

                    foreach (string find in clavesO)
                    {
                        foreach (string find2 in find.Split(' '))
                        {
                            string espacio = str.Split(':')[0].Substring(0, str.Split(':')[0].Length - 1);
                            if (find2 == espacio)
                            {
                                flag_correcta = true;
                                if (results.IndexOf(str) == -1)
                                    results += str + "/";

                                break;
                            }
                        }
                    }

                    if (flag_correcta == false)
                    {
                        errores += str + "/";
                    }
                }

                if (results.Length != 0)
                    results = results.Remove(results.Length - 1);

                if (errores.Length != 0)
                    errores = errores.Remove(errores.Length - 1);

                int palOcultas = 0;

                foreach (string str in clavesO)
                {
                    palOcultas += str.Split(' ').Length;
                }

                int palAcertadas = results.Split('/').Length;
                int palErradas = 0;
                
                if(errores != "")
                    palErradas = errores.Split('/').Length;

                float porcAcierto = (float)palAcertadas / (float)palOcultas;
                float porcErrores = (float)palErradas / ((float)palAcertadas + (float)palErradas);

                string feedback = "";

                

                DatoSimple ds = new DatoSimple() { CodeOP = 7, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Info = respuesta, PreguntaID = PreguntaID, Dato01 = porcAcierto, Dato02 = porcErrores,  NumAccion = numAccion};
                db.DatosSimples.Add(ds);

                db.SaveChanges();

                if (porcAcierto < 0.1) // 0%
                {
                    if (porcErrores < 0.1)
                    {
                        du.Puntos += 0;

                        feedback = "USUARIO, fíjate en las pistas que tendrías que haber seleccionado.";
                    }
                    else
                    {
                        du.Puntos += 0;

                        feedback = "USUARIO, revisa la tarea y fíjate en las pistas que debías seleccionar. Si tienes dudas, avisa a tu profesor.";
                    }
                }
                else
                {
                    if (0.1 <= porcAcierto && porcAcierto < 0.6) // 1% a 60%
                    {
                        if (porcErrores < 0.4)
                        {
                            du.Puntos += 40;

                            feedback = "USUARIO, revisa la tarea corregida y fíjate en las pistas correctas.";
                        }
                        else
                        {
                            du.Puntos += 20;

                            feedback = "USUARIO, revisa la tarea corregida y fíjate en las pistas correctas. Si tienes dudas, avisa a tu profesor.";
                        }
                    }
                    else
                    {
                        if (0.6 <= porcAcierto && porcAcierto < 0.99) // 60% a 99%
                        {
                            if (porcErrores < 0.4)
                            {
                                du.Puntos += 70;

                                feedback = "Bien USUARIO, revisa la tarea corregida y fíjate en las pistas correctas.";
                            }
                            else
                            {
                                du.Puntos += 60;

                                feedback = "USUARIO, has seleccionado demasiadas palabras. Revisa la tarea corregida y fíjate en las pistas correctas.";
                            }
                        }
                        else // 100%
                        {
                            if (porcErrores < 0.1)
                            {
                                du.Puntos += 100;

                                feedback = "¡Perfecto USUARIO!";
                            }
                            else
                            {
                                du.Puntos += 85;

                                feedback = "USUARIO, has encontrado todas las pistas, pero has elegido de más. Revisa tu tarea corregida.";
                            }
                        }
                    }
                }

                db.SaveChanges();

                Mensaje = feedback;
            }

            return Json(new { Mensaje = Mensaje, Results = results, Errores = errores });
        }

        [HttpPost]
        public ActionResult ValidarPreguntaTest(int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow ="")
        {
            logger.Debug("ReadAndLearnController/ValidarPreguntaTest");
            DatosUsuario du = db.DatosUsuario.Find(Session["DatosUsuarioID"]);

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), du.GrupoID, du.ModuloID, dataRow);

            DatoSimple ds = new DatoSimple() { CodeOP = 4, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Info = respuesta, PreguntaID = PreguntaID, NumAccion = numAccion};
            db.DatosSimples.Add(ds);

            db.SaveChanges();

            Pregunta preg = db.Preguntas.Find(PreguntaID);
            ConfigPregunta configPreg = db.ConfigPregunta.Single(c => c.PreguntaID == PreguntaID);

            if (preg.TipoPreguntaID == 1)
            {
                foreach (Alternativa alt in preg.Alternativas)
                {
                    if (alt.Opcion == respuesta)
                    {
                        if (alt.Valor) // Acierto
                        {
                            ds.Valor = 1;
                            du.Puntos += 100;
                            db.SaveChanges();

                            if (alt.FeedbackContenido == null)
                            {
                                return Json(new { redirect = Url.Action("PreguntaSimpleCorregida", new { PreguntaID = PreguntaID }), Puntos = du.Puntos, codeOP = GetCodeOP_Feedback(configPreg), mensaje = preg.FDBK_Correcto, PreguntaID = preg.PreguntaID });
                            }
                            else
                            {
                                return Json(new { redirect = Url.Action("PreguntaSimpleCorregida", new { PreguntaID = PreguntaID }), Puntos = du.Puntos, codeOP = GetCodeOP_Feedback(configPreg), mensaje = alt.FeedbackContenido, PreguntaID = preg.PreguntaID });
                            }
                        }
                        else // Fallo
                        {
                            ds.Valor = 0;

                            db.SaveChanges();

                            if (alt.FeedbackContenido == null)
                            {
                                return Json(new { redirect = Url.Action("PreguntaSimpleCorregida", new { PreguntaID = PreguntaID }), Puntos = du.Puntos, codeOP = GetCodeOP_Feedback(configPreg), mensaje = preg.FDBK_Incorrecto, PreguntaID = preg.PreguntaID });
                            }
                            else
                            {
                                return Json(new { redirect = Url.Action("PreguntaSimpleCorregida", new { PreguntaID = PreguntaID }), Puntos = du.Puntos, codeOP = GetCodeOP_Feedback(configPreg), mensaje = alt.FeedbackContenido, PreguntaID = preg.PreguntaID });
                            }
                        }
                    }
                }
            }
            else
            {
                string[,] Criterios = new string[preg.Criterios.Count(), 2];

                int i = 0;
                foreach (Criterio cri in preg.Criterios)
                {
                    Criterios[i, 0] = cri.Opcion;
                    Criterios[i, 1] = cri.Valor.ToString();
                    i++;
                }

                double sdfsdf = Corrector(Criterios, respuesta);

            }
            return Json(new { redirect = Url.Action("Pregunta", new { PreguntaID = PreguntaID }) });        
        }

        public ActionResult PreguntaSimpleCorregida(int PreguntaID, bool? Repetido)
        {
            logger.Debug("ReadAndLearnController/PreguntaSimpleCorregida");
            Pregunta preg = db.Preguntas.Find(PreguntaID);

            var user = (from u in db.UserProfiles
                        where User.Identity.Name == u.UserName
                        select u).Single();

            ViewBag.Nombre = user.Nombre;

            ViewBag.Pagina = preg.Texto.Paginas.First();
                        
            ViewBag.Repetido = Repetido;

            switch (preg.TipoPreguntaID)
            {
                case 1: // Tipo Test
                    ViewBag.config = (from c in db.ConfigPregunta
                                      where c.PreguntaID == PreguntaID
                                      select c).Single();

                    DatosUsuario du = GetDatosUsuario();

                    foreach (DatoSimple ds in du.DatoSimple.Reverse())
                    {
                        if (ds.CodeOP == 4 && ds.PreguntaID == PreguntaID)
                        {
                            ViewBag.Respuesta = ds.Info;

                            foreach (Alternativa resp in preg.Alternativas)
                            {
                                if (resp.Opcion == ds.Info)
                                {
                                    if (resp.FeedbackContenido != null)
                                    {
                                        ViewBag.Mensaje = resp.FeedbackContenido;
                                    }
                                    else
                                    {
                                        if (resp.Valor)
                                        {
                                            ViewBag.Mensaje = preg.FDBK_Correcto;
                                        }
                                        else
                                        {
                                            ViewBag.Mensaje = preg.FDBK_Incorrecto;
                                        }
                                    }
                                    break;
                                }
                            }
                        }                    
                    }
                    string selecc = "";

                    foreach (DatoSimple ds in du.DatoSimple.Reverse())
                    {
                        if (ds.CodeOP == 6 && ds.PreguntaID == PreguntaID)
                        {
                            selecc = ds.Info;
                            
                            break;
                        }
                    }
                    
                    ViewBag.Seleccion = selecc;
                    break;
                case 2:
                    break;
                case 3:
                    List<string> derecha = new List<string>();
                    string[] parejas;
                    var rnd = new Random();
                    ConfigPregunta config = new ConfigPregunta();

                    var listaConfig = from c in db.ConfigPregunta
                                      where c.PreguntaID == PreguntaID
                                      select c;

                    if (listaConfig.Count() == 1)
                    {
                        config = listaConfig.First();
                    }

                    foreach (Emparejado emp in preg.Emparejados)
                    {
                        if (emp.ColDer != null)
                        {
                            parejas = emp.ColDer.Split('/');

                            foreach (string str in parejas)
                            {
                                derecha.Add(str);
                            }
                        }
                    }

                    var result = derecha.OrderBy(item => rnd.Next());

                    ViewBag.config = config;
                    ViewBag.Lista = result.ToList();
                    break;
                default:
                    break;
            }



            return View(db.Preguntas.Find(PreguntaID));
        }

        public ActionResult PreguntaSimpleSimulada(int PreguntaID)
        {
            logger.Debug("ReadAndLearnController/PreguntaSimpleSimulada");
            Pregunta preg = db.Preguntas.Find(PreguntaID);
            ViewBag.Pagina = preg.Texto.Paginas.First();
            ConfigPregunta config = new ConfigPregunta();
            string opcion = "";

            try
            {
                opcion = (from a in preg.Alternativas
                                 where a.Valor == true
                                 select a.Opcion).Single();
            }
            catch (Exception e)
            {
                opcion = "";
            }

            ViewBag.Opcion = opcion;

            switch (preg.TipoPreguntaID)
            {
                case 1: // Tipo Test
                    try
                    {
                        config = (from c in db.ConfigPregunta
                                  where c.PreguntaID == PreguntaID
                                  select c).Single();

                        ViewBag.config = config;
                    }
                    catch { 
                    
                    }
                    break;
                case 2:
                    break;
                case 3:
                    List<string> derecha = new List<string>();
                    string[] parejas;
                    var rnd = new Random();
                    config = new ConfigPregunta();

                    var listaConfig = from c in db.ConfigPregunta
                                      where c.PreguntaID == PreguntaID
                                      select c;

                    if (listaConfig.Count() == 1)
                    {
                        config = listaConfig.First();
                    }

                    foreach (Emparejado emp in preg.Emparejados)
                    {
                        if (emp.ColDer != null)
                        {
                            parejas = emp.ColDer.Split('/');

                            foreach (string str in parejas)
                            {
                                derecha.Add(str);
                            }
                        }
                    }

                    var result = derecha.OrderBy(item => rnd.Next());

                    ViewBag.config = config;
                    ViewBag.Lista = result.ToList();
                    break;
                default:
                    break;
            }

            return View(db.Preguntas.Find(PreguntaID));
        }

        public ActionResult PreguntaSimple(int PreguntaID)
        {
            logger.Debug("ReadAndLearnController/PreguntaSimple");
            DatosUsuario du = db.DatosUsuario.Find(Session["DatosUsuarioID"]);
            DatoSimple ds = new DatoSimple();
            Boolean flag_repetido = false;

            Pregunta preg = db.Preguntas.Find(PreguntaID);
            ViewBag.Pagina = preg.Texto.Paginas.First();
            ConfigPregunta config = new ConfigPregunta();
            
            var user = (from u in db.UserProfiles
                        where User.Identity.Name == u.UserName
                        select u).Single();

            ViewBag.Nombre = user.Nombre;

            switch (preg.TipoPreguntaID)
            { 
                case 1: // Tipo Test
                case 3:
                    if (du != null)
                    {
                        var DatosSimples = (from d in db.DatosSimples
                                            where d.DatosUsuarioID == du.DatosUsuarioID &&
                                                  d.CodeOP == 3
                                            select d).ToList();

                        ds = DatosSimples.Last();

                        if (ds != null)
                        {
                            if (ds.Dato01 < du.AccionActual)
                            {
                                flag_repetido = true;
                            }
                        }
                    }

                    try
                    {
                        config = (from c in db.ConfigPregunta
                                  where c.PreguntaID == PreguntaID
                                  select c).Single();

                        ViewBag.config = config;
                    }
                    catch (Exception e)
                    { 
                        
                    }

                    if (config.Responder == false)
                    {
                        return RedirectToAction("PreguntaSimpleSimulada", new { PreguntaID = PreguntaID });
                    }
                    else
                    {
                        if (flag_repetido)
                        {
                            var DatosSimples = (from d in db.DatosSimples
                                               where d.DatosUsuarioID == du.DatosUsuarioID &&
                                                     d.CodeOP == 4 &&
                                                     d.PreguntaID == PreguntaID
                                               select d).ToList();

                            if (DatosSimples.Count != 0)
                            {
                                DatoSimple ds2 = DatosSimples.Last();
                                Pregunta tmp = new Pregunta();
                                string FDBK = "";

                                if (ds2 != null)
                                {
                                    tmp = db.Preguntas.Find(ds2.PreguntaID);

                                    foreach (Alternativa alt in tmp.Alternativas)
                                    {
                                        if (alt.Opcion == ds2.Info)
                                        {
                                            FDBK = alt.FeedbackContenido;
                                        }
                                    }
                                }

                                return RedirectToAction("PreguntaSimpleCorregida", new { PreguntaID = PreguntaID, Repetido = flag_repetido });
                            }
                            else
                            {
                                var DatosSimples2 = (from d in db.DatosSimples
                                                     where d.DatosUsuarioID == du.DatosUsuarioID &&
                                                           d.CodeOP == 7 &&
                                                           d.PreguntaID == PreguntaID
                                                     select d).ToList();

                                DatoSimple ds2 = DatosSimples2.Last();

                                ViewBag.SolucionClaves = ds2.Info;
                            }
                        }
                    }
                    break;
                case 2:
                    break;
                case 4:
                    if (du != null)
                    {
                        var DatosSimples = (from d in db.DatosSimples
                                            where d.DatosUsuarioID == du.DatosUsuarioID &&
                                                  d.CodeOP == 3
                                            select d).ToList();

                        ds = DatosSimples.Last();

                        if (ds != null)
                        {
                            if (ds.Dato01 < du.AccionActual)
                            {
                                var DatosSimples2 = (from d in db.DatosSimples
                                                    where d.DatosUsuarioID == du.DatosUsuarioID &&
                                                          d.CodeOP == 5 &&
                                                          d.PreguntaID == PreguntaID
                                                    select d).ToList();

                                if (DatosSimples2.Count > 0)
                                {
                                    DatoSimple ds2 = DatosSimples2.Last();
                                    Pregunta tmp = new Pregunta();

                                    return RedirectToAction("PreguntasPajeraCorregida", new { PreguntaID = PreguntaID, Respuesta = ds2.Info });
                                }
                                else
                                {
                                    return RedirectToAction("PreguntasPajeraCorregida", new { PreguntaID = PreguntaID, Respuesta = "" });
                                }
                            }
                        }
                    }

                    List<string> derecha = new List<string>();
                    string[] parejas;
                    var rnd = new Random();
                    config = new ConfigPregunta();

                    var listaConfig = from c in db.ConfigPregunta
                                      where c.PreguntaID == PreguntaID
                                      select c;

                    if (listaConfig.Count() == 1)
                    {
                        config = listaConfig.First();
                    }

                    foreach (Emparejado emp in preg.Emparejados)
                    {
                        if (emp.ColDer != null)
                        {
                            parejas = emp.ColDer.Split('/');

                            foreach (string str in parejas)
                            {
                                derecha.Add(str);
                            }
                        }
                    }

                    var result = derecha.OrderBy(item => rnd.Next());

                    ViewBag.config = config;
                    ViewBag.Lista = result.ToList();
                    break;
                default:
                    break;
            }

            

            return View(db.Preguntas.Find(PreguntaID));
        }

        public ActionResult Texto(int GrupoID, int ModuloID, int textoActual, string moment, int numAccion = -1 )
        {
            logger.Debug("ReadAndLearnController/Texto");
            
            try
            {
                DatosUsuario du = GetDatosUsuario();

                Texto texto = db.Modulos.Find(ModuloID).Textos.ToList()[textoActual];
                Pregunta pregunta = texto.Preguntas.ToList()[du.PreguntaActual];


                ViewBag.PregActual = du.PreguntaActual;

                return View(db.Textos.Find(texto.TextoID));
            }
            catch (Exception e)
            {
                DatosUsuario du = GetDatosUsuario();
                DatoSimple ds;
                //guirisan/secuencias
                DateTime datetimeclient = DateTime.Parse(moment);

                du.TextoActual++;
                du.PreguntaActual = 0;
                du.AyudaStatus = 0;
                du.BuscaStatus = 0;

                db.SaveChanges();

                if (du.TextoActual == 1)
                {
                    ds = new DatoSimple() { CodeOP = 50, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.AyudaStatus, Dato02 = du.BuscaStatus, Dato03 = du.RevisaStatus, NumAccion = numAccion };
                    db.DatosSimples.Add(ds);
                    db.SaveChanges();

                    return RedirectToAction("Texto", new { du.GrupoID, ModuloID = du.ModuloID, textoActual = 1 });
                }
                else // Fin módulo
                {
                    ds = new DatoSimple() { CodeOP = 60, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.AyudaStatus, Dato02 = du.BuscaStatus, Dato03 = du.RevisaStatus, NumAccion = numAccion };
                    db.DatosSimples.Add(ds);
                    du.Cerrada = true;

                    db.SaveChanges();

                    return RedirectToAction("Tareas", "Alumno");
                }
            }
        }

        public ActionResult TextoCompleto(int TextoID, string moment, int numAccion = -1)
        {
            logger.Debug("ReadAndLearnController/TextoCompleto");
            DatosUsuario du = GetDatosUsuario();
            DateTime datetimeclient = DateTime.Parse(moment);

            // Inicio Practica Guiada
            DatoSimple ds = new DatoSimple() { CodeOP = 200, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, TextoID = TextoID, NumAccion = numAccion};
            db.DatosSimples.Add(ds);

            return View(db.Textos.Find(TextoID));
        }


        public ActionResult PreguntaCompleta(int pregActual, int pregTotal, int TextoID)
        {
            logger.Debug("ReadAndLearnController/PreguntaCompleta");
            Pregunta preg = db.Textos.Find(TextoID).Preguntas.ElementAt(pregActual);

            ViewBag.pregActual = pregActual;
            ViewBag.pregTotal = pregTotal;
            ViewBag.TextoID = TextoID;

            return View(preg);
        }

        [HttpPost]
        public ActionResult ValidarPreguntaCompleta(int PreguntaID, string respuesta, int pregActual, int pregTotal, int TextoID, string moment, int numAccion = -1, string dataRow = "")
        {
            logger.Debug("ReadAndLearnController/ValidarPreguntaCompleta");
            DatosUsuario du = GetDatosUsuario();

            //guirisan/secuencias/pending
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), du.GrupoID, du.ModuloID, dataRow);

            Pregunta pregunta = db.Preguntas.Find(PreguntaID);
            DateTime datetimeclient = DateTime.Parse(moment);
            if (CorregirRespuesta(PreguntaID, respuesta)) // Acierto
            {
                
                string Mensaje = "";

                if (PreguntaID == 240 || PreguntaID == 243 || PreguntaID == 244)
                {
                    switch (du.BuscaStatus)
                    {
                        case 0:
                            Mensaje = "Muy bien. No olvides que en otras preguntas buscar en el texto te puede ayudar a asegurar tu respuesta.";
                            break;
                        case 1:
                            Mensaje = "¡Muy bien! Buscar en el texto te ha sido útil. ¡Sigue así!";
                            break;
                        case 2:
                            Mensaje = "¡Muy bien! Has buscado en el texto y has encontrado la información importante para la pregunta. Esto te ha ayudado a responder bien. ¡Sigue así!";
                            break;
                    }
                }
                else
                {
                    switch (du.BuscaStatus)
                    {
                        case 0:
                            Mensaje = "Muy bien. No olvides que en otras preguntas buscar en el texto te puede ayudar a asegurar tu respuesta.";
                            break;
                        case 1:
                            Mensaje = "Muy bien, está genial que busques en el texto. Aunque has acertado la pregunta, esta vez no has encontrado la información necesaria. Aún puedes mejorar tu estrategia de búsqueda.";
                            break;
                        case 2:
                            Mensaje = "¡Muy bien! Has buscado en el texto y has encontrado la información importante para la pregunta. Esto te ha ayudado a responder bien. ¡Sigue así!";
                            break;
                    }
                }

                // 201 - Validar Pregunta
                DatoSimple ds = new DatoSimple() { CodeOP = 201, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, TextoID = TextoID, PreguntaID = PreguntaID, Valor = 1, Info = respuesta, Dato01 = du.BuscaStatus, NumAccion = numAccion};
                db.DatosSimples.Add(ds);
                
                du.BuscaStatus = 0;
                du.Puntos += 100;
                du.RespuestaPos++;

                db.SaveChanges();

                return Json(new { redirect = Url.Action("PreguntaCompletaCorregida", new { PreguntaID = PreguntaID, pregActual = pregActual, pregTotal = pregTotal, TextoID = TextoID, Respuesta = respuesta }), Mensaje = Mensaje, PreguntaID = pregunta.PreguntaID, Respuesta = respuesta });
            }
            else // Fallo
            {
                
                string Mensaje = "";


                if (PreguntaID == 240 || PreguntaID == 243 || PreguntaID == 244)
                {
                    switch (du.BuscaStatus)
                    {
                        case 0:
                            Mensaje = "Recuerda que para responder bien es importante que busques la información necesaria para la pregunta. Ahora revisa las alternativas y el texto para entender el fallo.";
                            break;
                        case 1:
                            Mensaje = "Está muy bien que busques en el texto. Aunque ahora no has acertado, buscar puede ayudarte en otras preguntas. Sigue usando esta estrategia para asegurar tu respuesta. Ahora revisa las alternativas y el texto para entender el fallo.";
                            break;
                        case 2:
                            Mensaje = "Has estado muy bien buscando en el texto. Has encontrado la información importante para esta pregunta aunque esta vez no te ha ayudado a responder bien. Revisa las alternativas y el texto para entender el fallo y poder mejorar.";
                            break;
                    }
                }
                else
                {
                    switch (du.BuscaStatus)
                    {
                        case 0:
                            Mensaje = "Para responder bien es importante que busques en el texto la información necesaria para la pregunta. Ahora revisa el texto y las alternativas para entender el fallo.";
                            break;
                        case 1:
                            Mensaje = "Está muy bien que busques en el texto, aunque esta vez no has encontrado la información necesaria para la pregunta. Revisa las alternativas y relee el texto para entender el fallo. Esto te ayudará a mejorar tu estrategia de búsqueda.";
                            break;
                        case 2:
                            Mensaje = "Has estado muy bien buscando en el texto. Has encontrado la información importante para esta pregunta aunque esta vez no te ha ayudado a responder bien. Revisa las alternativas y el texto para entender el fallo y poder mejorar.";
                            break;
                    }
                }

                // 201 - Validar Pregunta
                DatoSimple ds = new DatoSimple() { CodeOP = 201, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, TextoID = TextoID, PreguntaID = PreguntaID, Valor = 1, Info = respuesta, Dato01 = du.BuscaStatus, NumAccion = numAccion };
                db.DatosSimples.Add(ds);

                du.BuscaStatus = 0;
                du.Puntos += 0;
                du.RespuestaNeg++;

                db.SaveChanges();
               
                return Json(new { redirect = Url.Action("PreguntaCompletaCorregida", new { PreguntaID = PreguntaID, pregActual = pregActual, pregTotal = pregTotal, TextoID = TextoID, Respuesta = respuesta }), Mensaje = Mensaje, PreguntaID = pregunta.PreguntaID, Respuesta = respuesta });
            }
        }

        public ActionResult PreguntaCompletaCorregida(int PreguntaID, int pregActual, int pregTotal, int TextoID, string Respuesta)
        {
            logger.Debug("ReadAndLearnController/PreguntaCompletaCorregida");
            Pregunta pregunta = db.Preguntas.Find(PreguntaID);

            ConfigPregunta config = new ConfigPregunta();

            var listaConfig = from c in db.ConfigPregunta
                              where c.PreguntaID == pregunta.PreguntaID
                              select c;

            if (listaConfig.Count() == 1)
            {
                config = listaConfig.First();
            }

            ViewBag.config = config;
            ViewBag.Respuesta = Respuesta;
            ViewBag.pregActual = pregActual;
            ViewBag.pregTotal = pregTotal;
            ViewBag.TextoID = TextoID;

            return View(pregunta);
        }

        public ActionResult PreguntaIndependienteCorregida(int PreguntaID, int pregActual, int pregTotal, int TextoID, string Respuesta, string Seleccion)
        {
            logger.Debug("ReadAndLearnController/PreguntaIndependienteCorregida");
            Pregunta pregunta = db.Preguntas.Find(PreguntaID);
            DatosUsuario du = new DatosUsuario();

            du.RevisaStatus = 0;

            du = GetDatosUsuario();

            if (Respuesta == null)
            {
                foreach (DatoSimple ds in du.DatoSimple.Reverse())
                {
                    if (ds.CodeOP == 5 && ds.PreguntaID == PreguntaID)
                    {
                        ViewBag.Respuesta = ds.Info;
                        break;
                    }
                }
            }
            else
            {
                ViewBag.Respuesta = Respuesta;
            }

            ConfigPregunta config = new ConfigPregunta();

            var listaConfig = from c in db.ConfigPregunta
                              where c.PreguntaID == pregunta.PreguntaID
                              select c;

            if (listaConfig.Count() == 1)
            {
                config = listaConfig.First();
            }

            if ((double)du.RespuestaPos / (double)(pregActual + (7 * du.TextoActual)) < 0.45)
            {
                if (config.ForzarNoTarea)
                {
                    ViewBag.TareaSeleccion = false;
                }
                else
                {
                    Seleccion = Server.HtmlDecode(Seleccion.Replace('?', '&'));

                    ViewBag.TareaSeleccion = true;
                }                
            }
            else
            {
                if (Seleccion != null && Seleccion != "")
                {
                    if (config.ForzarNoTarea)
                    {
                        ViewBag.TareaSeleccion = false;
                    }
                    else
                    {
                        Seleccion = Server.HtmlDecode(Seleccion.Replace('?', '&'));

                        ViewBag.TareaSeleccion = true;
                    }  
                }
                else
                {
                    ViewBag.TareaSeleccion = false;
                }
            }

            ViewBag.config = config;
            ViewBag.Puntos = du.Puntos;
            ViewBag.pregActual = pregActual;
            ViewBag.pregTotal = pregTotal;
            ViewBag.TextoID = TextoID;
            ViewBag.Seleccion = Seleccion;

            return View(pregunta);
        }

        public ActionResult SiguientePregunta(int PreguntaID, int pregActual, int pregTotal, int TextoID, string moment, string dataRow ="", int numAccion = -1)
        {
            logger.Debug("ReadAndLearnController/SiguientePregunta");

            
            
            DatosUsuario du = GetDatosUsuario();
            Texto text = db.Textos.Find(TextoID);
            DatoSimple ds;

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), du.GrupoID, du.ModuloID, dataRow);

            if (pregActual + 2 > text.Preguntas.Count()) // Cambio de texto
            {
                du.TextoActual++;
                du.PreguntaActual = 0;
                du.AyudaStatus = 0;
                du.BuscaStatus = 0;

                db.SaveChanges();

                if (du.TextoActual == 1)
                {
                    ds = new DatoSimple() { CodeOP = 50, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.AyudaStatus, Dato02 = du.BuscaStatus, Dato03 = du.RevisaStatus, NumAccion = numAccion};
                    db.DatosSimples.Add(ds);
                    db.SaveChanges();
                    return Json(new { redirect = Url.Action("Texto", new { du.GrupoID, ModuloID = du.ModuloID, textoActual = 1, moment = datetimeclient }) });
                }
                else // Fin módulo
                {
                    ds = new DatoSimple() { CodeOP = 60, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.AyudaStatus, Dato02 = du.BuscaStatus, Dato03 = du.RevisaStatus, NumAccion = numAccion };
                    db.DatosSimples.Add(ds);

                    du.Cerrada = true;

                    db.SaveChanges();

                    return Json(new { redirect = Url.Action("Tareas", "Alumno") });
                }
            }

            ds = new DatoSimple() { CodeOP = 10, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.AyudaStatus, Dato02 = du.BuscaStatus, Dato03 = du.RevisaStatus, NumAccion = numAccion };
            db.DatosSimples.Add(ds);

            du.AyudaStatus = 0;
            du.BuscaStatus = 0;
            du.PreguntaActual++;

            db.SaveChanges();

            pregActual = du.PreguntaActual;

            try
            {
                Pregunta preguntaTest = db.Textos.Find(TextoID).Preguntas.ToList()[pregActual];

                return Json(new { redirect = Url.Action("PreguntaIndependiente", new { pregActual = pregActual + 1, pregTotal = pregTotal, TextoID = TextoID }) });
            }
            catch (Exception e)
            {
                du.TextoActual++;
                du.PreguntaActual = 0;
                du.AyudaStatus = 0;
                du.BuscaStatus = 0;



                db.SaveChanges();

                if (du.TextoActual == 1)
                {
                    ds = new DatoSimple() { CodeOP = 50, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.AyudaStatus, Dato02 = du.BuscaStatus, Dato03 = du.RevisaStatus, NumAccion = numAccion };
                    db.DatosSimples.Add(ds);
                    db.SaveChanges();
                    return Json(new { redirect = Url.Action("Texto", new { du.GrupoID, ModuloID = du.ModuloID, textoActual = 1, moment = datetimeclient, NumAccion = numAccion }) });
                }
                else // Fin módulo
                {
                    ds = new DatoSimple() { CodeOP = 60, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.AyudaStatus, Dato02 = du.BuscaStatus, Dato03 = du.RevisaStatus, NumAccion = numAccion };
                    db.DatosSimples.Add(ds);

                    du.Cerrada = true;

                    db.SaveChanges();

                    return Json(new { redirect = Url.Action("Tareas", "Alumno") });
                }            
            }
        }

        public ActionResult SiguientePreguntaCompleta(int PreguntaID, int pregActual, int pregTotal, int TextoID, string moment, int numAccion = -1, string dataRow = "")
        {
            logger.Debug("ReadAndLearnController/SiguientePreguntaCompleta");

            DatosUsuario du = GetDatosUsuario();

            Texto texto = db.Textos.Find(TextoID);

            //guirisan/secuencias/pending
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), du.GrupoID, du.ModuloID, dataRow);

            //parse datetime measured in client
            DateTime datetimeclient = DateTime.Parse(moment);
	


            if (texto.Preguntas.Count > pregActual + 1)
            {
                // 202 - Validar Pregunta
                DatoSimple ds = new DatoSimple() { CodeOP = 202, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, TextoID = TextoID, Info = du.PreguntaActual.ToString(), Dato01 = du.BuscaStatus, NumAccion = numAccion};
                db.DatosSimples.Add(ds);

                du.PreguntaActual++;
                db.SaveChanges();

                return Json(new { redirect = Url.Action("PreguntaCompleta", new { pregActual = pregActual + 1, pregTotal = pregTotal, TextoID = TextoID }) });
            }
            else
            {
                // Fin Practica Guiada
                DatoSimple ds = new DatoSimple() { CodeOP = 299, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, TextoID = TextoID, NumAccion = numAccion};

                db.DatosSimples.Add(ds);

                // Crear una HttpPost que permita cambiar de acción ya que no puedo llamar directamente a CambiarAccion por JsonRequestBehavior.AllowGet
                return Json(new { redirect = Url.Action("CambiarAccion", new { GrupoID = du.GrupoID, ModuloID = du.ModuloID, accActual = du.AccionActual, escActual = du.EscenaActual, moment = datetimeclient, numAccion = numAccion, dataRow = dataRow  }) }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult PreguntaIndependiente(int pregActual, int pregTotal, int TextoID, int? EstadoPregunta)
        {
            logger.Debug("ReadAndLearnController/PreguntaIndependiente");
            DatosUsuario du = GetDatosUsuario();

            pregActual = du.PreguntaActual;

            Pregunta pregunta = db.Textos.Find(TextoID).Preguntas.ToList()[pregActual];


            List<string> derecha = new List<string>();
            string[] parejas;
            var rnd = new Random();
            ConfigPregunta config = new ConfigPregunta();

            var listaConfig = from c in db.ConfigPregunta
                                where c.PreguntaID == pregunta.PreguntaID
                                select c;

            if (listaConfig.Count() == 1)
            {
                config = listaConfig.First();
            }

            if ((double)du.RespuestaPos / (double)(pregActual + (7 * du.TextoActual)) < 0.45)
            {
                if (config.ForzarNoTarea)
                {
                    ViewBag.TareaSeleccion = false;
                }
                else
                {
                    ViewBag.TareaSeleccion = true;
                }
            }
            else
            {
                ViewBag.TareaSeleccion = false;
            }

            var result = derecha.OrderBy(item => rnd.Next());

            ViewBag.Puntos = du.Puntos;

            ViewBag.config = config;
            ViewBag.Lista = result.ToList();
            ViewBag.pregActual = pregActual;
            ViewBag.pregTotal = pregTotal;
            ViewBag.TextoID = TextoID;
            ViewBag.EstadoPregunta = EstadoPregunta;
            ViewBag.Texto = db.Textos.Find(TextoID);

            return View(pregunta);            
            
        }

        public ActionResult Pregunta(int PreguntaID, int ModuloID, int escActual, int accActual, int GrupoID)
        {
            logger.Debug("ReadAndLearnController/Pregunta");
            Pregunta preg = db.Preguntas.Find(PreguntaID);
            List<string> derecha = new List<string>();
            string[] parejas;
            var rnd = new Random();
            ConfigPregunta config = new ConfigPregunta();

            var listaConfig = from c in db.ConfigPregunta
                              where c.PreguntaID == PreguntaID
                              select c;

            if (listaConfig.Count() == 1)
            {
                config = listaConfig.First();
            }

            foreach (Emparejado emp in preg.Emparejados)
            {
                if (emp.ColDer != null)
                {
                    parejas = emp.ColDer.Split('/');

                    foreach (string str in parejas)
                    {
                        derecha.Add(str);
                    }
                }
            }

            var result = derecha.OrderBy(item => rnd.Next());

            ViewBag.config = config;
            ViewBag.Lista = result.ToList();
            ViewBag.ModuloID = ModuloID;
            ViewBag.escActual = escActual;
            ViewBag.accActual = accActual;
            ViewBag.GrupoID = GrupoID;

            return View(db.Preguntas.Find(PreguntaID));
        }

        public int GetCodeOP_Feedback(ConfigPregunta config)
        {
            logger.Debug("ReadAndLearnController/GetCodeOP_Feedback");
            if (config.FeedbackProfesor) 
            {
                if (config.FeedbackAlumno) // 1 1 - Conversación profesor y alumno
                {
                    return 103;
                }
                else // 1 0 - Profesor
                {
                    return 102;
                }
            }
            else 
            {
                if (config.FeedbackAlumno) // 0 1 - Alumno
                {
                    return 101;
                }
                else // 0 0 - Programa
                {
                    return 100;
                }
            }
        }

        

        [HttpPost]
        public ActionResult ValidarPrimeraSeleccion(int PreguntaID, int pregActual, int pregTotal, int TextoID, string respuesta)
        {
            logger.Debug("ReadAndLearnController/ValidarPrimeraSeleccion");
            int cont = 0;
            int inicio = 0;

            var texto = db.Textos.Find(TextoID);

            var user = (from u in db.UserProfiles
                        where User.Identity.Name == u.UserName
                        select u).Single();

            Pregunta pregunta = db.Preguntas.Find(PreguntaID);

            string Pertinente = respuesta;
            string tmpPertinente = "";

            // SELECCION PERTINENTE //
            int CharPertPregunta = 0;
            int numPag = 0;
            string contenido;

            List<List<List<int>>> rangos = new List<List<List<int>>>();

            foreach (Pagina pagina in texto.Paginas)
            {
                rangos.Add(new List<List<int>>());

                contenido = pagina.Contenido;

                contenido = Server.UrlDecode(contenido);

                contenido = HttpUtility.HtmlDecode(contenido);

                contenido = RemoveBetween(contenido, '<', '>');

                //contenido = contenido.Replace("&nbsp;", " ");

                contenido = contenido.Replace((char)160, (char)32);
                contenido = contenido.Replace(System.Environment.NewLine, " ");

                string[] frases = pregunta.Pertinente.Split('\\');

                CharPertPregunta = pregunta.Pertinente.Length - frases.Count();

                foreach (string frase in frases)
                {
                    string strTmp = frase.Replace((char)160, (char)32);

                    //strTmp = strTmp.Replace(

                    if (contenido.IndexOf(strTmp) != -1)
                    {
                        rangos[rangos.Count - 1].Add(new List<int>());

                        int ini = contenido.IndexOf(strTmp);
                        int fin = ini + frase.Length;

                        rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(ini);
                        rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(fin);
                    }
                }
            }

            string[] selecciones = Pertinente.Split('\\');
            numPag = 0;
            int ragInf;
            int ragSup;

            int tmp1 = 0; // Número de caracteres pertinentes            

            int CharPertinentes = 0;
            int totalCharSelect = 0;

            string pertCorr = "";
            string pertTmp = "";
            string mensaje = "";

            foreach (string select in selecciones)
            {
                if (select == null || select == "")
                    continue;

                if (Pertinente[0] == ' ')
                {
                    Pertinente = Pertinente.Remove(0, 1);
                }

                pertCorr = select.Replace("\n", "");

                pertCorr = pertCorr.Replace((char)160, (char)32);

                foreach (Pagina pagina in texto.Paginas)
                {
                    string seleccion = select.Replace("\n", "");

                    contenido = pagina.Contenido;

                    contenido = Server.UrlDecode(contenido);

                    contenido = HttpUtility.HtmlDecode(contenido);

                    contenido = RemoveBetween(contenido, '<', '>');

                    //contenido = contenido.Replace("&nbsp;", " ");

                    contenido = contenido.Replace((char)160, (char)32);
                    contenido = contenido.Replace(System.Environment.NewLine, " ");

                    seleccion = seleccion.Replace((char)160, (char)32);

                    if (seleccion != "")
                        while (seleccion[0] == ' ')
                            seleccion = seleccion.Remove(0, 1);

                    if (contenido.IndexOf(seleccion) != -1)
                    {
                        ragInf = contenido.IndexOf(seleccion);
                        ragSup = ragInf + seleccion.Length;

                        foreach (List<int> posible in rangos[numPag].Reverse<List<int>>())
                        {
                            if (ragInf > posible[1] || ragSup < posible[0])
                            {
                                continue;
                            }
                            else
                            {
                                if (ragInf >= posible[0])
                                {
                                    if (ragSup <= posible[1]) // Esta todo dentro
                                    {
                                        tmp1 += seleccion.Length;
                                        pertCorr = "<span style=\"color:green\">" + pertCorr + "</span>";
                                        continue;
                                    }
                                    else // Sobresale por la derecha
                                    {
                                        tmp1 += seleccion.Length - (ragSup - posible[1]);
                                        pertCorr = "<span style=\"color:green\">" + pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (ragSup <= posible[1]) // Sobresale por la izquierda
                                    {
                                        tmp1 += seleccion.Length - (posible[0] - ragInf);
                                        pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">") + "</span>";
                                        continue;
                                    }
                                    else // Sobresale por ambos lados
                                    {
                                        tmp1 += seleccion.Length - (ragSup - posible[1]) - (posible[0] - ragInf);
                                        pertCorr = pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                        pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">");
                                        continue;
                                    }
                                }
                            }
                        }

                        pertTmp += pertCorr + "\\";
                    }

                    numPag++;
                }

                CharPertinentes += tmp1;

                totalCharSelect += select.Length;

                tmp1 = 0;
                numPag = 0;
            }

            Pertinente = Server.HtmlEncode(pertTmp).Replace('&', '?');



            // Porcentaje de texto pertinente seleccionado frente al texto pertinente de la pregunta.
            double porcPert = ((double)CharPertinentes * 100.0) / (double)CharPertPregunta;
            // Porcentaje de texto no pertinente seleccionado frente a texto pertinente seleccionado.
            double porcNoPert = 100.0 - ((double)CharPertinentes * 100.0) / (double)totalCharSelect;

            if (totalCharSelect == 0)
            {
                porcNoPert = 100;
            }


            db.SaveChanges();

            if (porcNoPert > 35) // SI IRRELEVANTE
            {
                if (porcPert < 15) //NADA
                {
                    Pertinente = tmpPertinente;
                    mensaje += "Has seleccionado información NO necesaria. /2º INTENTO: Revisa la pregunta. Selecciona SOLO la información necesaria.";
                }
                else // PARTE o TODO
                {
                    if (porcPert > 65) // TODO
                    {
                        Pertinente = tmpPertinente;
                        mensaje += "Has seleccionado MÁS de lo necesario. /2º INTENTO: Revisa la pregunta. Elimina información no necesaria de tu selección.";
                    }
                    else // PARTE
                    {
                        Pertinente = tmpPertinente;
                        mensaje += "Has seleccionado SOLO PARTE de lo necesario y mucho NO necesario. /2º INTENTO: Revisa la pregunta. Elimina información no necesaria de tu selección y asegúrate de añadir TODA la necesaria.";
                    }
                }
            }
            else // NO IRRELEVANTE
            {
                if (porcPert < 15) //NADA
                {
                    Pertinente = tmpPertinente;
                    mensaje += "Has seleccionado SOLO PARTE de lo necesario. /2º INTENTO: Revisa la pregunta. Asegúrate de añadir TODA la información necesaria.";
                }
                else // PARTE o TODO
                {
                    if (porcPert > 65) // TODO
                    {
                        mensaje += "Has seleccionado TODO lo necesario. /Sigue así. Ahora responde a la pregunta.";
                        return Json(new { redirect = Url.Action("Pregunta", new { Pertinente = Pertinente, Respuesta = respuesta, Feedback = mensaje, TextoID = TextoID, Sigue = "Sigue" }) });
                    }
                    else // PARTE
                    {
                        Pertinente = tmpPertinente;
                        mensaje += "Has seleccionado SOLO PARTE de lo necesario. /2º INTENTO: Revisa la pregunta. Asegúrate de añadir TODA la información necesaria.";
                    }
                }
            }

            //return Json(new { redirect = Url.Action("Pregunta", new { Pertinente = Pertinente, Respuesta = respuesta, Feedback = mensaje, TextoID = TextoID, Sigue = "Repite" }) });


            //return Json(new { redirect = Url.Action("Pregunta", new { Pertinente = Pertinente, Respuesta = respuesta, Feedback = mensaje, TextoID = TextoID, Sigue = "Sigue" }) });

            return Json(new { Respuesta = respuesta, Feedback = mensaje });
        }

        [HttpPost]
        public ActionResult ValidarSegundaSeleccion(int PreguntaID, int pregActual, int pregTotal, int TextoID, string respuesta)
        {
            logger.Debug("ReadAndLearnController/ValidarSegundaSeleccion");
            int cont = 0;
            int inicio = 0;

            var texto = db.Textos.Find(TextoID);

            var user = (from u in db.UserProfiles
                        where User.Identity.Name == u.UserName
                        select u).Single();

            Pregunta pregunta = db.Preguntas.Find(PreguntaID);

            string Pertinente = respuesta;
            string tmpPertinente = "";

            // SELECCION PERTINENTE //
            int CharPertPregunta = 0;
            int numPag = 0;
            string contenido;

            List<List<List<int>>> rangos = new List<List<List<int>>>();

            foreach (Pagina pagina in texto.Paginas)
            {
                rangos.Add(new List<List<int>>());

                contenido = pagina.Contenido;

                contenido = Server.UrlDecode(contenido);

                contenido = HttpUtility.HtmlDecode(contenido);

                contenido = RemoveBetween(contenido, '<', '>');

                //contenido = contenido.Replace("&nbsp;", " ");

                contenido = contenido.Replace((char)160, (char)32);
                contenido = contenido.Replace(System.Environment.NewLine, " ");

                string[] frases = pregunta.Pertinente.Split('\\');

                CharPertPregunta = pregunta.Pertinente.Length - frases.Count();

                foreach (string frase in frases)
                {
                    string strTmp = frase.Replace((char)160, (char)32);

                    //strTmp = strTmp.Replace(

                    if (contenido.IndexOf(strTmp) != -1)
                    {
                        rangos[rangos.Count - 1].Add(new List<int>());

                        int ini = contenido.IndexOf(strTmp);
                        int fin = ini + frase.Length;

                        rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(ini);
                        rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(fin);
                    }
                }
            }

            string[] selecciones = Pertinente.Split('\\');
            numPag = 0;
            int ragInf;
            int ragSup;

            int tmp1 = 0; // Número de caracteres pertinentes            

            int CharPertinentes = 0;
            int totalCharSelect = 0;

            string pertCorr = "";
            string pertTmp = "";
            string mensaje = "";

            foreach (string select in selecciones)
            {
                if (select == null || select == "")
                    continue;

                if (Pertinente[0] == ' ')
                {
                    Pertinente = Pertinente.Remove(0, 1);
                }

                pertCorr = select.Replace("\n", "");

                pertCorr = pertCorr.Replace((char)160, (char)32);

                foreach (Pagina pagina in texto.Paginas)
                {
                    string seleccion = select.Replace("\n", "");

                    contenido = pagina.Contenido;

                    contenido = Server.UrlDecode(contenido);

                    contenido = HttpUtility.HtmlDecode(contenido);

                    contenido = RemoveBetween(contenido, '<', '>');

                    //contenido = contenido.Replace("&nbsp;", " ");

                    contenido = contenido.Replace((char)160, (char)32);
                    contenido = contenido.Replace(System.Environment.NewLine, " ");

                    seleccion = seleccion.Replace((char)160, (char)32);

                    if (seleccion != "")
                        while (seleccion[0] == ' ')
                            seleccion = seleccion.Remove(0, 1);

                    if (contenido.IndexOf(seleccion) != -1)
                    {
                        ragInf = contenido.IndexOf(seleccion);
                        ragSup = ragInf + seleccion.Length;

                        foreach (List<int> posible in rangos[numPag].Reverse<List<int>>())
                        {
                            if (ragInf > posible[1] || ragSup < posible[0])
                            {
                                continue;
                            }
                            else
                            {
                                if (ragInf >= posible[0])
                                {
                                    if (ragSup <= posible[1]) // Esta todo dentro
                                    {
                                        tmp1 += seleccion.Length;
                                        pertCorr = "<span style=\"color:green\">" + pertCorr + "</span>";
                                        continue;
                                    }
                                    else // Sobresale por la derecha
                                    {
                                        tmp1 += seleccion.Length - (ragSup - posible[1]);
                                        pertCorr = "<span style=\"color:green\">" + pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (ragSup <= posible[1]) // Sobresale por la izquierda
                                    {
                                        tmp1 += seleccion.Length - (posible[0] - ragInf);
                                        pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">") + "</span>";
                                        continue;
                                    }
                                    else // Sobresale por ambos lados
                                    {
                                        tmp1 += seleccion.Length - (ragSup - posible[1]) - (posible[0] - ragInf);
                                        pertCorr = pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                        pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">");
                                        continue;
                                    }
                                }
                            }
                        }

                        pertTmp += pertCorr + "\\";
                    }

                    numPag++;
                }

                CharPertinentes += tmp1;

                totalCharSelect += select.Length;

                tmp1 = 0;
                numPag = 0;
            }

            Pertinente = Server.HtmlEncode(pertTmp).Replace('&', '?');

            // Porcentaje de texto pertinente seleccionado frente al texto pertinente de la pregunta.
            double porcPert = ((double)CharPertinentes * 100.0) / (double)CharPertPregunta;
            // Porcentaje de texto no pertinente seleccionado frente a texto pertinente seleccionado.
            double porcNoPert = 100.0 - ((double)CharPertinentes * 100.0) / (double)totalCharSelect;

            if (totalCharSelect == 0)
            {
                porcNoPert = 100;
            }
            
            db.SaveChanges();

            if (porcNoPert > 35) // SI IRRELEVANTE
            {
                if (porcPert < 15) //NADA
                {
                    Pertinente = tmpPertinente;
                    mensaje += "Has seleccionado información NO necesaria.";
                }
                else // PARTE o TODO
                {
                    if (porcPert > 65) // TODO
                    {
                        Pertinente = tmpPertinente;
                        mensaje += "Has seleccionado MÁS de lo necesario.";
                    }
                    else // PARTE
                    {
                        Pertinente = tmpPertinente;
                        mensaje += "Has seleccionado SOLO PARTE de lo necesario y mucho NO necesario.";
                    }
                }
            }
            else // NO IRRELEVANTE
            {
                if (porcPert < 15) //NADA
                {
                    Pertinente = tmpPertinente;
                    mensaje += "Has seleccionado SOLO PARTE de lo necesario.";
                }
                else // PARTE o TODO
                {
                    if (porcPert > 65) // TODO
                    {
                        mensaje += "Has seleccionado TODO lo necesario.";
                        return Json(new { redirect = Url.Action("Pregunta", new { Pertinente = Pertinente, Respuesta = respuesta, Feedback = mensaje, TextoID = TextoID, Sigue = "Sigue" }) });
                    }
                    else // PARTE
                    {
                        Pertinente = tmpPertinente;
                        mensaje += "Has seleccionado SOLO PARTE de lo necesario.";
                    }
                }
            }

            return Json(new { Respuesta = respuesta, Feedback = mensaje });
        }

        [HttpPost]
        public void UsoAyudasIndependiente(int Ayuda, string moment, int numAccion = -1, string dataRow = "")
        {
            logger.Debug("ReadAndLearnController/UsoAyudasIndependiente");
            DatosUsuario du = GetDatosUsuario();

            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), du.GrupoID, du.ModuloID, dataRow);

            DateTime datetimeclient = DateTime.Parse(moment);
            #region Codificación
            switch (du.IndUsoAyud)
            {
                case 0:
                    if (Ayuda == 1) // Flota
                    {
                        du.IndUsoAyud = 4;
                    }
                    else
                    {
                        if (Ayuda == 2) // Prisma
                        {
                            du.IndUsoAyud = 2;
                        }
                        else // Lupa
                        {
                            du.IndUsoAyud = 1;
                        }
                    }
                    break;
                case 1:
                    if (Ayuda == 1) // Flota
                    {
                        du.IndUsoAyud = 5;
                    }
                    else
                    {
                        if (Ayuda == 2) // Prisma
                        {
                            du.IndUsoAyud = 3;
                        }
                        else // Lupa
                        {
                            du.IndUsoAyud = 1;
                        }
                    }
                    break;
                case 2:
                    if (Ayuda == 1) // Flota
                    {
                        du.IndUsoAyud = 6;
                    }
                    else
                    {
                        if (Ayuda == 2) // Prisma
                        {
                            du.IndUsoAyud = 2;
                        }
                        else // Lupa
                        {
                            du.IndUsoAyud = 3;
                        }
                    }
                    break;
                case 3:
                    if (Ayuda == 1) // Flota
                    {
                        du.IndUsoAyud = 7;
                    }
                    else
                    {
                        if (Ayuda == 2) // Prisma
                        {
                            du.IndUsoAyud = 3;
                        }
                        else // Lupa
                        {
                            du.IndUsoAyud = 3;
                        }
                    }
                    break;
                case 4:
                    if (Ayuda == 1) // Flota
                    {
                        du.IndUsoAyud = 4;
                    }
                    else
                    {
                        if (Ayuda == 2) // Prisma
                        {
                            du.IndUsoAyud = 6;
                        }
                        else // Lupa
                        {
                            du.IndUsoAyud = 5;
                        }
                    }
                    break;
                case 5:
                    if (Ayuda == 1) // Flota
                    {
                        du.IndUsoAyud = 5;
                    }
                    else
                    {
                        if (Ayuda == 2) // Prisma
                        {
                            du.IndUsoAyud = 7;
                        }
                        else // Lupa
                        {
                            du.IndUsoAyud = 5;
                        }
                    }
                    break;
                case 6:
                    if (Ayuda == 1) // Flota
                    {
                        du.IndUsoAyud = 6;
                    }
                    else
                    {
                        if (Ayuda == 2) // Prisma
                        {
                            du.IndUsoAyud = 6;
                        }
                        else // Lupa
                        {
                            du.IndUsoAyud = 7;
                        }
                    }
                    break;
                case 7:
                    if (Ayuda == 1) // Flota
                    {
                        du.IndUsoAyud = 7;
                    }
                    else
                    {
                        if (Ayuda == 2) // Prisma
                        {
                            du.IndUsoAyud = 7;
                        }
                        else // Lupa
                        {
                            du.IndUsoAyud = 7;
                        }
                    }
                    break;
            }
            #endregion

            DatoSimple ds = new DatoSimple() { CodeOP = 15, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.TextoActual, Dato02 = du.PreguntaActual, Dato03 = Ayuda, NumAccion = numAccion };
            db.DatosSimples.Add(ds);

            db.SaveChanges();

            if ((du.PreguntaActual + du.TextoActual * 7) >= (du.AyudaPos + du.AyudaNeg))
            {
                du.AyudaPos++;
                du.AyudaStatus = 1;
                db.SaveChanges();
            }
        }

        [HttpPost]
        public void RevisaIndependiente(string moment, int numAccion = -1, string dataRow = "" )
        {

            logger.Debug("ReadAndLearnController/RevisaIndependiente");
            DatosUsuario du = GetDatosUsuario();

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), du.GrupoID, du.ModuloID, dataRow);


            if ((du.PreguntaActual + du.TextoActual * 7) >= (du.RevisaPos + du.RevisaNeg))
            {
                DatoSimple ds = new DatoSimple() { CodeOP = 17, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.TextoActual, Dato02 = du.PreguntaActual, NumAccion = numAccion };
                db.DatosSimples.Add(ds);

                du.RevisaPos++;
                du.RevisaStatus = 1;                
                db.SaveChanges();
            }
        }

        [HttpPost]
        public void UsoAyudas()
        {
            logger.Debug("ReadAndLearnController/UsoAyudas");
            DatosUsuario du = GetDatosUsuario();

            if (du.PreguntaActual >= (du.AyudaPos + du.AyudaNeg))
            {
                du.AyudaPos++;

                db.SaveChanges();
            }
        }

        [HttpPost]
        public ActionResult GetPertinente(int pregActual, string TextoID)
        {
            logger.Debug("ReadAndLearnController/GetPertinente");
            int tmp = Convert.ToInt32(TextoID);

            var pregunta = from p in db.Preguntas
                           where p.Texto.TextoID == tmp
                           select p;

            String Pertinente = pregunta.ToList()[pregActual].Pertinente;
            
            return Json(new { Pertinente = Pertinente });
        }


        [HttpPost]
        public void BuscaIndependiente(string moment, int numAccion = -1, string dataRow = "")
        {
            logger.Debug("ReadAndLearnController/BuscaIndependiente");

            DatosUsuario du = GetDatosUsuario();

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), du.GrupoID, du.ModuloID, dataRow);

            if ((du.PreguntaActual + du.TextoActual * 7) >= (du.BuscaPos + du.BuscaNeg))
            {
                DatoSimple ds = new DatoSimple() { CodeOP = 16, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };
                db.DatosSimples.Add(ds);

                du.BuscaPos++;
                du.BuscaStatus = 1;
                db.SaveChanges();
            }
        }



        [HttpPost]
        public void Busca()
        {
            logger.Debug("ReadAndLearnController/Busca");
            DatosUsuario du = GetDatosUsuario();

            if (du.PreguntaActual >= (du.BuscaPos + du.BuscaNeg))
            {
                du.BuscaPos++;
                
                if (du.BuscaStatus != 2)
                {
                    du.BuscaStatus = 1;
                }

                db.SaveChanges();
            }
        }

        [HttpPost]
        public void PertinenteEncontrado()
        {
            logger.Debug("ReadAndLearnController/PertinenteEncontrado");
            //guirisan: create datoSimple ?
            DatosUsuario du = GetDatosUsuario();

            du.BuscaStatus = 2;
            db.SaveChanges();            
        }

        private String CalcularFDBK()
        {
            logger.Debug("ReadAndLearnController/CalcularFDBK - TODO: RETURN ''");
            DatosUsuario du = GetDatosUsuario();



            return "";
        }


        [HttpPost]
        public ActionResult ValidarPreguntaIndependiente(int PreguntaID, int pregActual, int pregTotal, int TextoID, string respuesta, string Seleccion, string moment, int numAccion = -1, string dataRow ="")
        {
            logger.Debug("ReadAndLearnController/ValidarPreguntaIndependiente");
            Pregunta pregunta = db.Preguntas.Find(PreguntaID);
            DatosUsuario du = db.DatosUsuario.Find(Session["DatosUsuarioID"]);

            //guirisan/secuencias
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), du.GrupoID, du.ModuloID, dataRow);
            DateTime datetimeclient = DateTime.Parse(moment);

            if (du.AyudaStatus != 1)
            {
                du.AyudaStatus = -1;            
                du.AyudaNeg++;
            }

            if (du.BuscaStatus != 1)
            {
                du.BuscaStatus = -1;
                du.BuscaNeg++;                
            }

            ConfigPregunta config = new ConfigPregunta();

            var listaConfig = from c in db.ConfigPregunta
                              where c.PreguntaID == pregunta.PreguntaID
                              select c;

            if (listaConfig.Count() == 1)
            {
                config = listaConfig.First();
            }

            if ((double)du.RespuestaPos / (double)(pregActual + (7 * du.TextoActual)) < 0.45)
            {
                if (!config.ForzarNoTarea)
                {
                    #region Algoritmo

                    Pagina pagina = pregunta.Texto.Paginas.First();
                    string respOriginal = Seleccion;



                    // SELECCION PERTINENTE //
                    int CharPertPregunta = 0;
                    string contenido = "";

                    List<List<List<int>>> rangos = new List<List<List<int>>>();

                    rangos.Add(new List<List<int>>());

                    contenido = pagina.Contenido;

                    contenido = Server.UrlDecode(contenido);
                    contenido = RemoveBetween(contenido, '<', '>');
                    contenido = HttpUtility.HtmlDecode(contenido);
                    contenido = contenido.Replace("&nbsp;", " ");
                    contenido = contenido.Replace(System.Environment.NewLine, " ");

                    contenido = contenido.Replace((char)160, (char)32);

                    string[] frases = pregunta.Pertinente.Split('/');

                    CharPertPregunta = pregunta.Pertinente.Length - frases.Count();

                    foreach (string frase in frases)
                    {
                        string strTmp = frase.Replace((char)160, (char)32);

                        //strTmp = strTmp.Replace(

                        if (contenido.IndexOf(strTmp) != -1)
                        {
                            rangos[rangos.Count - 1].Add(new List<int>());

                            int ini = contenido.IndexOf(strTmp);
                            int fin = ini + frase.Length;

                            rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(ini);
                            rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(fin);
                        }
                    }

                    string[] selecciones = Seleccion.Split('\\');

                    int ragInf;
                    int ragSup;

                    int tmp1 = 0; // Número de caracteres pertinentes            

                    int CharPertinentes = 0;
                    int totalCharSelect = 0;

                    
                    string pertCorr = "";
                    string pertTmp = "";

                    foreach (string select in selecciones)
                    {
                        pertCorr = select.Replace("\n", "");

                        pertCorr = pertCorr.Replace((char)160, (char)32);

                        string seleccion = select.Replace("\n", "");

                        contenido = pagina.Contenido;

                        contenido = Server.UrlDecode(contenido);

                        contenido = RemoveBetween(contenido, '<', '>');

                        contenido = HttpUtility.HtmlDecode(contenido);
                        contenido = contenido.Replace("&nbsp;", " ");

                        contenido = contenido.Replace(System.Environment.NewLine, " ");

                        contenido = contenido.Replace((char)160, (char)32);
                        seleccion = seleccion.Replace((char)160, (char)32);

                        if (contenido.IndexOf(seleccion) != -1)
                        {
                            ragInf = contenido.IndexOf(seleccion);
                            ragSup = ragInf + seleccion.Length;

                            foreach (List<int> posible in rangos[0].Reverse<List<int>>())
                            {
                                if (ragInf > posible[1] || ragSup < posible[0])
                                {
                                    continue;
                                }
                                else
                                {
                                    if (ragInf >= posible[0])
                                    {
                                        if (ragSup <= posible[1]) // Esta todo dentro
                                        {
                                            tmp1 += seleccion.Length;
                                            pertCorr = "<span style=\"color:green\">" + pertCorr + "</span>";
                                            continue;
                                        }
                                        else // Sobresale por la derecha
                                        {
                                            tmp1 += seleccion.Length - (ragSup - posible[1]);
                                            pertCorr = "<span style=\"color:green\">" + pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        if (ragSup <= posible[1]) // Sobresale por la izquierda
                                        {
                                            tmp1 += seleccion.Length - (posible[0] - ragInf);
                                            pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">") + "</span>";
                                            continue;
                                        }
                                        else // Sobresale por ambos lados
                                        {
                                            tmp1 += seleccion.Length - (ragSup - posible[1]) - (posible[0] - ragInf);
                                            pertCorr = pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                            pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">");
                                            continue;
                                        }
                                    }
                                }
                            }

                            pertTmp += pertCorr + "\\";
                        }

                        CharPertinentes += tmp1;

                        totalCharSelect += select.Length;

                        tmp1 = 0;
                    }

                    Seleccion = Server.HtmlEncode(pertTmp).Replace('&', '?');
                    //respuesta = Server.HtmlEncode(pertTmp).Replace('&', '?');

                    // Porcentaje de texto pertinente seleccionado frente al texto pertinente de la pregunta.
                    double porcPert = ((double)CharPertinentes * 100.0) / (double)CharPertPregunta;
                    // Porcentaje de texto no pertinente seleccionado frente a texto pertinente seleccionado.
                    double porcNoPert = 100.0 - ((double)CharPertinentes * 100.0) / (double)totalCharSelect;
                    string mensaje = "";

                    if (totalCharSelect == 0)
                    {
                        porcNoPert = 100;
                    }

                    DatoSimple ds = new DatoSimple() { CodeOP = 25, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Info = respOriginal, PreguntaID = PreguntaID, Dato01 = (float)porcPert, Dato02 = (float)porcNoPert, NumAccion = numAccion};

                    db.DatosSimples.Add(ds);

                    db.SaveChanges();
                    
                    #endregion
                }
                //Boolean tmp = CorregirSeleccion(PreguntaID, Seleccion);
            }

            if (CorregirRespuesta(PreguntaID, respuesta)) // Acierto
            {   
                DatoSimple ds = new DatoSimple() { CodeOP = 5, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Info = respuesta, PreguntaID = PreguntaID, Valor = 1, NumAccion = numAccion};
                db.DatosSimples.Add(ds);

                if (config.ForzarNoTarea)
                {
                    du.Puntos = du.Puntos + 100;
                }
                else
                {
                    #region Puntuaciones
                    switch (du.IndUsoAyud)
                    {
                        case 0:
                            du.Puntos = du.Puntos + 100;
                            break;
                        case 1:
                            du.Puntos = du.Puntos + 50;
                            break;
                        case 2:
                            du.Puntos = du.Puntos + 60;
                            break;
                        case 3:
                            du.Puntos = du.Puntos + 40;
                            break;
                        case 4:
                            du.Puntos = du.Puntos + 80;
                            break;
                        case 5:
                            du.Puntos = du.Puntos + 45;
                            break;
                        case 6:
                            du.Puntos = du.Puntos + 55;
                            break;
                        case 7:
                            du.Puntos = du.Puntos + 35;
                            break;
                        case 8:
                            break;
                    }
                    #endregion
                }

                du.RespuestaPos++;
                du.IndUsoAyud = 0;

                db.SaveChanges();

                return Json(new { redirect = Url.Action("PreguntaIndependienteCorregida", new { PreguntaID = PreguntaID, pregActual = pregActual, pregTotal = pregTotal, TextoID = TextoID, Respuesta = respuesta, Seleccion = Seleccion }), Mensaje = GeneradorFeedback(true), PreguntaID = pregunta.PreguntaID, Respuesta = respuesta, Puntos = du.Puntos });
            }
            else // Fallo
            {
                DatoSimple ds = new DatoSimple() { CodeOP = 5, DatosUsuario = du, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Info = respuesta, PreguntaID = PreguntaID, Valor = 0, NumAccion = numAccion };
                db.DatosSimples.Add(ds);

                du.RespuestaNeg++;
                du.IndUsoAyud = 0;

                db.SaveChanges();

                return Json(new { redirect = Url.Action("PreguntaIndependienteCorregida", new { PreguntaID = PreguntaID, pregActual = pregActual, pregTotal = pregTotal, TextoID = TextoID, Respuesta = respuesta, Seleccion = Seleccion }), Mensaje = GeneradorFeedback(false), PreguntaID = pregunta.PreguntaID, Respuesta = respuesta, Puntos = du.Puntos });
            }
        }

        private String GeneradorFeedback(bool Acierto) // Version 2014_04_03:13_19
        {
            logger.Debug("ReadAndLearnController/GeneradorFeedback");
            DatosUsuario du = db.DatosUsuario.Find(Session["DatosUsuarioID"]);

            string mensaje = "";

            if (Acierto)
            {
                #region BUSCAR
                switch (du.BuscaStatus)
                {
                    case 1: // Busca
                        #region BUSCARPOS
                        switch (du.BuscaPos) // REVISA
                        {
                            case 1: // timing 1 (sin coletilla)
                                #region AYUDAR
                                switch(du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        { 
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                { 
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Estupendo USUARIO! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Estupendo USUARIO! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Estupendo USUARIO! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto, buscar te ha ayudado a responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;                                            
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;
                                                            default:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto, buscar te ha ayudado a responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;

                            case 3: // timing 3-5 ("Recuerda")
                            case 5:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Estupendo USUARIO! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Estupendo USUARIO! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Estupendo USUARIO! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto, buscar te ha ayudado a responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;
                                                            default:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto, buscar te ha ayudado a responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;
                            case 8: // timing 8-11-13 ("Insisto")
                            case 11:
                            case 13:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Estupendo USUARIO! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Estupendo USUARIO! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Muy bien! Buscar y usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "¡Estupendo USUARIO! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Genial! Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto, buscar te ha ayudado a responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;
                                                            default:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO, buscar te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien USUARIO, la estrategia de búsqueda te ha servido.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto USUARIO. Se nota que sabes usar las estrategias para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, buscar te ha ayudado a responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Perfecto, buscar te ha ayudado a responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "¡Excelente! Buscar en el texto te ha ayudado a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;
                            default:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, USUARIO.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;
                        }
                        #endregion
                        break;
                    case -1: // No Busca
                        #region BUSCARNEG
                        switch (du.BuscaNeg) // REVISA
                        {
                            case 1: // timing 1 (sin coletilla)
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Parece que usar ayudas te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien. Sigue así. ";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Parece que usar ayudas te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien. Sigue así. ";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Parece que usar ayudas te ha servido.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien. Sigue así. ";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, pero no olvides que buscar puede ser de ayuda en otras ocasiones.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, pero no olvides que buscar puede ser de ayuda en otras ocasiones.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien. Sigue así. ";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, USUARIO.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, pero no olvides que buscar puede ser de ayuda en otras ocasiones.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, pero no olvides que buscar puede ser de ayuda en otras ocasiones.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;

                            case 3: // timing 3-5 ("Recuerda")
                            case 5:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, usar ayudas te ha sido útil.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo. Parece que usar ayudas te ha servido. Recuerda que buscar también nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido. Recuerda que buscar también nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil. No olvides que buscar también sirve para asegurar la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda. Recuerda que buscando también nos aseguramos de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo. Parece que usar ayudas te ha servido. Recuerda que buscar también nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, usar ayudas te ha sido útil.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo. Parece que usar ayudas te ha servido. Recuerda que buscar también nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido. Recuerda que buscar también nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil. No olvides que buscar también sirve para asegurar la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda. Recuerda que buscando también nos aseguramos de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo. Parece que usar ayudas te ha servido. Recuerda que buscar también nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, usar ayudas te ha sido útil.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo. Parece que usar ayudas te ha servido. Recuerda que buscar también nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido. Recuerda que buscar también nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil. No olvides que buscar también sirve para asegurar la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda. Recuerda que buscando también nos aseguramos de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo. Parece que usar ayudas te ha servido. Recuerda que buscar también nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, ten en cuenta que si dudas, buscar es una buena estrategia.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo USUARIO, pero no olvides que buscar sirve para asegurar la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial USUARIO, aunque recuerda que buscando nos aseguramos de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, ten en cuenta que si dudas, buscar es una buena estrategia.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, pero no olvides que buscar sirve para asegurar la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, aunque recuerda que buscando nos aseguramos de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Estupendo, pero no olvides que buscar sirve para asegurar la respuesta.";
                                                                break;
                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, pero no olvides que buscar sirve para asegurar la respuesta.";
                                                                break;
                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo, pero no olvides que buscar sirve para asegurar la respuesta.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, pero no olvides que buscar sirve para asegurar la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, pero no olvides que buscar sirve para asegurar la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, aunque recuerda que buscando nos aseguramos de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, pero no olvides que buscar sirve para asegurar la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, aunque recuerda que buscando nos aseguramos de la respuesta correcta.";
                                                                break;
                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial, aunque recuerda que buscando nos aseguramos de la respuesta correcta.";
                                                                break;
                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, aunque recuerda que buscando nos aseguramos de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, aunque recuerda que buscando nos aseguramos de la respuesta correcta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, pero no olvides que buscar sirve para asegurar la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, aunque recuerda que buscando nos aseguramos de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, aunque recuerda que buscando nos aseguramos de la respuesta correcta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, ten en cuenta que si dudas, buscar es una buena estrategia.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar nos ayuda a estar seguros de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo USUARIO, pero no olvides que buscar sirve para asegurar la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial USUARIO, aunque recuerda que buscando nos aseguramos de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, ten en cuenta que si dudas, buscar es una buena estrategia.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;

                            case 8: // timing 8-11-13 ("Insisto")
                            case 11:
                            case 13:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, usar ayudas te ayuda.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, usar ayudas te ayuda.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo USUARIO, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial USUARIO, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, usar ayudas te ayuda.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, usar ayudas te ayuda.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo USUARIO, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial USUARIO, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, usar ayudas te ayuda.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, usar ayudas te ayuda.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo USUARIO, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial USUARIO, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, parece que sabes tomar buenas decisiones sobre cuándo buscar.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo USUARIO, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial USUARIO, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, parece que sabes tomar buenas decisiones sobre cuándo buscar.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG 
                                        switch (du.AyudaNeg) // VOY POR AQUI
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Estupendo, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, parece que sabes tomar buenas decisiones sobre cuándo buscar.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, pero recuerda que buscar te puede ayudar a estar seguro de la respuesta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo USUARIO, pero no olvides que buscar te sirve para asegurar tu respuesta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial USUARIO, aunque recuerda que si buscas te aseguras de la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, parece que sabes tomar buenas decisiones sobre cuándo buscar.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;
                            default:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Parece que usar ayudas te ha servido.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial USUARIO, usar ayudas te ha sido útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo USUARIO, usar ayudas te ayuda.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien, usar ayudas te ha servido para responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Genial, USUARIO.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Muy bien. Sigue así.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Estupendo, USUARIO.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Genial, USUARIO.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;
                        }
                        #endregion
                        break;
                }
                #endregion
            }
            else
            {
                #region BUSCAR
                switch (du.BuscaStatus)
                {
                    case 1: // Busca
                        #region BUSCARPOS
                        switch (du.BuscaPos) // REVISA
                        {
                            case 1: // timing 1 (sin coletilla)
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, seguro que la próxima vez buscar y usar las ayudas te servirá. Ahora es importante que revises texto y alternativas para mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar y usar las ayudas son buenas estrategias, pero recuerda que es muy importante que revises texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, seguro que la próxima vez buscar y usar las ayudas te servirá. Ahora es importante que revises texto y alternativas para mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar y usar las ayudas son buenas estrategias, pero recuerda que es muy importante que revises texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, seguro que la próxima vez buscar y usar las ayudas te servirá. Ahora es importante que revises texto y alternativas para mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar y usar las ayudas son buenas estrategias, pero recuerda que es muy importante que revises texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Ahora revisa texto y alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. Ahora es importante que revises texto y alternativas para comprender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta, seguro que a la próxima te ayuda. Recuerda que ahora es muy importate que revises las alternativas y el texto para aprender de tus fallos.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Si dudas puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Si dudas puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Si dudas puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a reponder bien. Si dudas puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que hayas buscado. Aún así, si dudas, usar las ayudas te será útil. Ahora revisa el texto y las alternativas para entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia. En caso de dudas, usar las ayudas también puede servirte. Ahora es importate que revises el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar en el texto. Y si tienes dudas, usar las ayudas también puede serte útil. Recuerda que es importante revisar texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a reponder bien. Si dudas puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que hayas buscado. Usar las ayudas también es importante si tienes dudas. Ahora revisa el texto y las alternativas para entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, aunque si tienes dudas es importante que uses las ayudas para responder bien. También es importante que revises el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar en el texto. Y si tienes dudas, es importante que uses las ayudas para responder bien. No olvides revisar texto y alternativas para entender los fallos.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que también puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que hayas buscado. Recuerda que en caso de dudas, es importante usar las ayudas. Ahora revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia. Recuerda que usar las ayudas te será útil en caso de dudas. También es importate revisar texto y alternativas para entender por qué esa alternativa es la correcta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar en el texto. Si tienes dudas, recuerda que es importante usar las ayudas. No olvides que es importante revisar texto y alternativas para mejorar en las siguientes preguntas.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que también puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Ahora revisa texto y alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. Ahora es importante que revises texto y alternativas para comprender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta, seguro que a la próxima te ayuda. Recuerda que ahora es muy importate que revises las alternativas y el texto para aprender de tus fallos.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;

                            case 3: // timing 3-5 ("Recuerda")
                            case 5:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, seguro que la próxima vez buscar y usar las ayudas te servirá. Ahora es importante que revises texto y alternativas para mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar y usar las ayudas son buenas estrategias, pero recuerda que es muy importante que revises texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, seguro que la próxima vez buscar y usar las ayudas te servirá. Ahora es importante que revises texto y alternativas para mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar y usar las ayudas son buenas estrategias, pero recuerda que es muy importante que revises texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, seguro que la próxima vez buscar y usar las ayudas te servirá. Ahora es importante que revises texto y alternativas para mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar y usar las ayudas son buenas estrategias, pero recuerda que es muy importante que revises texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Ahora revisa texto y alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. Ahora es importante que revises texto y alternativas para comprender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta, seguro que a la próxima te ayuda. Recuerda que ahora es muy importate que revises las alternativas y el texto para aprender de tus fallos.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Si dudas puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Si dudas puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Si dudas puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a reponder bien. Si dudas puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que hayas buscado. Aún así, si dudas, usar las ayudas te será útil. Ahora revisa el texto y las alternativas para entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia. En caso de dudas, usar las ayudas también puede servirte. Ahora es importate que revises el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar en el texto. Y si tienes dudas, usar las ayudas también puede serte útil. Recuerda que es importante revisar texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a reponder bien. Si dudas puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que hayas buscado. Usar las ayudas también es importante si tienes dudas. Ahora revisa el texto y las alternativas para entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, aunque si tienes dudas es importante que uses las ayudas para responder bien. También es importante que revises el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar en el texto. Y si tienes dudas, es importante que uses las ayudas para responder bien. No olvides revisar texto y alternativas para entender los fallos.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que también puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que hayas buscado. Recuerda que en caso de dudas, es importante usar las ayudas. Ahora revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia. Recuerda que usar las ayudas te será útil en caso de dudas. También es importate revisar texto y alternativas para entender por qué esa alternativa es la correcta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar en el texto. Si tienes dudas, recuerda que es importante usar las ayudas. No olvides que es importante revisar texto y alternativas para mejorar en las siguientes preguntas.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que también puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Ahora revisa texto y alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. Ahora es importante que revises texto y alternativas para comprender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta, seguro que a la próxima te ayuda. Recuerda que ahora es muy importate que revises las alternativas y el texto para aprender de tus fallos.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;

                            case 8: // timing 8-11-13 ("Insisto")
                            case 11:
                            case 13:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, seguro que la próxima vez buscar y usar las ayudas te servirá. Ahora es importante que revises texto y alternativas para mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar y usar las ayudas son buenas estrategias, pero recuerda que es muy importante que revises texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, seguro que la próxima vez buscar y usar las ayudas te servirá. Ahora es importante que revises texto y alternativas para mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar y usar las ayudas son buenas estrategias, pero recuerda que es muy importante que revises texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez te será más útil. Para saber por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, seguro que la próxima vez buscar y usar las ayudas te servirá. Ahora es importante que revises texto y alternativas para mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar y usar las ayudas son buenas estrategias, pero recuerda que es muy importante que revises texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está muy bien que busques y uses las ayudas. Aunque ahora no has acertado, seguro que la próxima vez  estas estrategias te ayudarán.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Ahora revisa texto y alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. Ahora es importante que revises texto y alternativas para comprender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta, seguro que a la próxima te ayuda. Recuerda que ahora es muy importate que revises las alternativas y el texto para aprender de tus fallos.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Si dudas puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Si dudas puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Si dudas puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a reponder bien. Si dudas puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que hayas buscado. Aún así, si dudas, usar las ayudas te será útil. Ahora revisa el texto y las alternativas para entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia. En caso de dudas, usar las ayudas también puede servirte. Ahora es importate que revises el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar en el texto. Y si tienes dudas, usar las ayudas también puede serte útil. Recuerda que es importante revisar texto y alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a reponder bien. Si dudas puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que hayas buscado. Usar las ayudas también es importante si tienes dudas. Ahora revisa el texto y las alternativas para entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, aunque si tienes dudas es importante que uses las ayudas para responder bien. También es importante que revises el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar en el texto. Y si tienes dudas, es importante que uses las ayudas para responder bien. No olvides revisar texto y alternativas para entender los fallos.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. También puedes usar las ayudas si tienes dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que puedes usar las ayudas. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que también puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está muy bien que hayas buscado. Recuerda que en caso de dudas, es importante usar las ayudas. Ahora revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia. Recuerda que usar las ayudas te será útil en caso de dudas. También es importate revisar texto y alternativas para entender por qué esa alternativa es la correcta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar en el texto. Si tienes dudas, recuerda que es importante usar las ayudas. No olvides que es importante revisar texto y alternativas para mejorar en las siguientes preguntas.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta. Recuerda que también puedes usar las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Sigue usando la estrategias de buscar, aunque esta vez no has acertado seguro que a la próxima te ayudará. Ahora revisa el texto y las alternativas para entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si sigues practicando la estrategia de buscar, pronto te ayudará a responder bien. Ahora revisa texto y alternativas para entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar es una buena estrategia, sigue usándola para asegurar tu respuesta. Ahora es importante que revises texto y alternativas para comprender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, sigue usando la estrategia de buscar para asegurar tu respuesta, seguro que a la próxima te ayuda. Recuerda que ahora es muy importate que revises las alternativas y el texto para aprender de tus fallos.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas buscado. Con la práctica cada vez buscarás mejor.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;
                            default:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si revisas el texto y las alternativas, podrás entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Es importante que revises el texto y las alternativas para poder comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Recuerda que es importante que revises el texto y las alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si revisas el texto y las alternativas, podrás entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Es importante que revises el texto y las alternativas para poder comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Recuerda que es importante que revises el texto y las alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si revisas el texto y las alternativas, podrás entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Es importante que revises el texto y las alternativas para poder comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Recuerda que es importante que revises el texto y las alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Revisar la información destacada en el texto puede ayudarte a entender tu fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Recuerda que revisar la información destacada en el texto puede ser muy útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Ojo! Es muy importante que revises la información destacada en el texto.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Usar las ayudas puede servirte para contestar bien en próximas preguntas. Ahora revisar texto y alternativas te ayudará a entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Si tienes dudas es imp que uses las ayudas para responder. Si revisas el texto y las alternativas entenderás por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Recuerda que en caso de dudas, usar las ayudas será útil para contestar bien. Revisa el texto y las alternativas para comprender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Usar las ayudas te puede ayudar a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "En caso de dudas, usar las ayudas te será útil para responder bien. Ahora revisa el texto y las alternativas para entender por qué has fallado, te ayudará en próximas preguntas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Usar las ayudas te será útil para responder bien en caso de dudas. También es importate que revises el texto y las alternativas para comprender el fallo y poder mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Si tienes dudas, usar las ayudas te será útil para responder bien. Y recuerda que es importante revisar texto y alternativas para entender los fallos y poder mejorar en otras preguntas.";
                                                                break;
                                                            default:
                                                                mensaje = "Usar las ayudas te puede ayudar a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si tienes dudas es imp que uses las ayudas para responder. Si revisas el texto y las alternativas entenderás por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Si tienes dudas es imp que uses las ayudas para responder. Si revisas el texto y las alternativas entenderás por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Si tienes dudas es imp que uses las ayudas para responder. Si revisas el texto y las alternativas entenderás por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Recuerda que las ayudas te pueden ser útiles para contestar bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Es importante que uses las ayudas para responder bien en caso de dudas. Ahora revisa el texto y las alternativas para entender por qué has fallado, te ayudará en próximas preguntas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Si tienes dudas es importante que uses las ayudas para responder bien. También es importante que revises el texto y las alternativas para comprender el fallo y poder mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "En caso de dudas, es importante que uses las ayudas para responder bien. Y recuerda que es importante revisar texto y alternativas para entender los fallos y poder mejorar en otras preguntas.";
                                                                break;
                                                            default:
                                                                mensaje = "Recuerda que las ayudas te pueden ser útiles para contestar bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Recuerda que en caso de dudas, usar las ayudas será útil para contestar bien. Revisa el texto y las alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Recuerda que en caso de dudas, usar las ayudas será útil para contestar bien. Revisa el texto y las alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Recuerda que en caso de dudas, usar las ayudas será útil para contestar bien. Revisa el texto y las alternativas para comprender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Siempre que tengas dudas, piensa que las ayudas pueden facilitar tu tarea.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Recuerda que en caso de dudas, es importante usar las ayudas para responder bien. Ahora revisa el texto y las alternativas para entender por qué has fallado, te ayudará en próximas preguntas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Recuerda que usar las ayudas te será útil para responder bien en caso de dudas. También es importante que revises el texto y las alternativas para comprender el fallo y poder mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Si tienes dudas, recuerda que es importante usar las ayudas para responder bien. Y recuerda que es importante revisar texto y alternativas para entender los fallos y poder mejorar en otras preguntas.";
                                                                break;
                                                            default:
                                                                mensaje = "Siempre que tengas dudas, piensa que las ayudas pueden facilitar tu tarea.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Revisar la información destacada en el texto puede ayudarte a entender tu fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Recuerda que revisar la información destacada en el texto puede ser muy útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Ojo! Es muy importante que revises la información destacada en el texto.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;
                        }
                        #endregion
                        break;
                    case -1: // No Busca
                        #region BUSCARNEG
                        switch (du.BuscaNeg) // REVISA
                        {
                            case 1: // timing 1 (sin coletilla)
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para acertar es importante que busques en el texto. Revisar texto y alternativas te ayudará a entender por qué esa alternativa no es la correcta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para acertar es importante que busques en el texto. Revisar texto y alternativas te ayudará a entender por qué esa alternativa no es la correcta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para acertar es importante que busques en el texto. Revisar texto y alternativas te ayudará a entender por qué esa alternativa no es la correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para asegurar el acierto tienes que buscar en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para asegurar el acierto tienes que buscar en el texto. Para saber por qué has fallado revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, si tienes dudas está bien que uses las ayudas, pero ten en cuenta que buscar también es una estrategia muy útil. Ahora es importante que revises el texto y las alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, usar las ayudas es útil, aunque buscar en el texto también puede ser de gran ayuda. Recuerda que es muy importante revisar las alternativas y el texto para entender el fallo y poder mejorar.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para asegurar el acierto tienes que buscar en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para acertar es importante que busques en el texto. Revisar texto y alternativas te ayudará a entender por qué esa alternativa no es la correcta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para acertar es importante que busques en el texto. Revisar texto y alternativas te ayudará a entender por qué esa alternativa no es la correcta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para acertar es importante que busques en el texto. Revisar texto y alternativas te ayudará a entender por qué esa alternativa no es la correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para asegurar el acierto tienes que buscar en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para asegurar el acierto tienes que buscar en el texto. Para saber por qué has fallado revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, si tienes dudas está bien que uses las ayudas, pero ten en cuenta que buscar también es una estrategia muy útil. Ahora es importante que revises el texto y las alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, usar las ayudas es útil, aunque buscar en el texto también puede ser de gran ayuda. Recuerda que es muy importante revisar las alternativas y el texto para entender el fallo y poder mejorar.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para asegurar el acierto tienes que buscar en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para acertar es importante que busques en el texto. Revisar texto y alternativas te ayudará a entender por qué esa alternativa no es la correcta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para acertar es importante que busques en el texto. Revisar texto y alternativas te ayudará a entender por qué esa alternativa no es la correcta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para acertar es importante que busques en el texto. Revisar texto y alternativas te ayudará a entender por qué esa alternativa no es la correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para asegurar el acierto tienes que buscar en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para asegurar el acierto tienes que buscar en el texto. Para saber por qué has fallado revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, si tienes dudas está bien que uses las ayudas, pero ten en cuenta que buscar también es una estrategia muy útil. Ahora es importante que revises el texto y las alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, usar las ayudas es útil, aunque buscar en el texto también puede ser de gran ayuda. Recuerda que es muy importante revisar las alternativas y el texto para entender el fallo y poder mejorar.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero para asegurar el acierto tienes que buscar en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Buscar en el texto puede ser útil para responder bien. Revisa texto y alternativas, te ayudará a entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Buscar en el texto puede ser útil para responder bien. Revisa texto y alternativas, te ayudará a entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Buscar en el texto puede ser útil para responder bien. Revisa texto y alternativas, te ayudará a entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "No olvides que buscar en el texto te ayudará a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Buscar en el texto es una buena estrategia para responder bien. Para saber por qué has fallado revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar en el texto te puede ser útil. Ahora es importante que revises el texto y las alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar en el texto puede ayudarte a responder bien. Recuerda que es muy importante revisar las alternativas y el texto para entender el fallo y poder mejorar. ";
                                                                break;
                                                            default:
                                                                mensaje = "No olvides que buscar en el texto te ayudará a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG 
                                        switch (du.AyudaNeg) // VOY POR AQUI
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Buscar en el texto y usar las ayudas si tienes dudas son estrategias que te ayudarán a responder bien. Revisa texto y alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Buscar en el texto y usar las ayudas si tienes dudas son estrategias que te ayudarán a responder bien. Revisa texto y alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Buscar en el texto y usar las ayudas si tienes dudas son estrategias que te ayudarán a responder bien. Revisa texto y alternativas para comprender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Buscar en el texto y usar las ayudas si tienes dudas son estrategias que te ayudarán a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Buscar en el texto y usar las ayudas son estrategias que te ayudarán a responder bien. Revisa texto y alternativas para saber por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar en el texto y, ante la duda, usar las ayudas son buenas estrategias para responder bien. Ahora es importante que revises el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Buscar en el texto y usar las ayudas en caso de dudas son estrategias útiles para responder bien. Recuerda que es importante revisar el texto y las alternativas para comprender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Buscar en el texto y usar las ayudas si tienes dudas son estrategias que te ayudarán a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, buscar en el texto y emplear las ayudas te será útil. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar en el texto y emplear las ayudas te será útil. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar en el texto y emplear las ayudas te será útil. Si revisas texto y alternativas comprenderás el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, buscar en texto y emplear las ayudas puede serte útil cuando respondes a preguntas relacionadas con el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Buscar en el texto te ayudarán a responder bien. En caso de dudas, también es importante usar las ayudas. Ahora revisa texto y alternativas para saber por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar en el texto te ayudarán a responder bien. Si tienes dudas, también es importante usar las ayudas. Es esencial que revises el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar en el texto te ayudarán a responder bien. Usar las ayudas en caso de dudas también es importante. Pero recuerda que es esencial revisar texto y alternativas para comprender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, buscar en texto y emplear las ayudas puede serte útil cuando respondes a preguntas relacionadas con el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, buscar en el texto te ayudará a responder bien. Recuerda que usar las ayudas también puede serte útil. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar en el texto te ayudará a responder bien. Recuerda que usar las ayudas también puede serte útil. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar en el texto te ayudará a responder bien. Recuerda que usar las ayudas también puede serte útil. Ahora revisar texto y alternativas te ayudará a entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, buscar en el texto te puede ayudar a responder. Recuerda que también es importante usar las ayudas si tienes dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Buscar en el texto te ayudará a responder bien, y recuerda que también es importante usar las ayudas si dudas. Revisa texto y alternativas para saber por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar en el texto te ayudará a responder bien, y recuerda que también es importante usar las ayudas si dudas. Ahora es esencial que revises el texto y las alternativas para entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar en el texto te ayudará a responder bien, y recuerda que también es importante usar las ayudas si dudas. No olvides lo importante que es revisar el texto y las alternativas para saber por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, buscar en el texto te puede ayudar a responder. Recuerda que también es importante usar las ayudas si tienes dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Buscar en el texto puede ser útil para responder bien. Revisa texto y alternativas, te ayudará a entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Buscar en el texto puede ser útil para responder bien. Revisa texto y alternativas, te ayudará a entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Buscar en el texto puede ser útil para responder bien. Revisa texto y alternativas, te ayudará a entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "No olvides que buscar en el texto te ayudará a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Buscar en el texto es una buena estrategia para responder bien. Para saber por qué has fallado revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar en el texto te puede ser útil. Ahora es importante que revises el texto y las alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar en el texto puede ayudarte a responder bien. Recuerda que es muy importante revisar las alternativas y el texto para entender el fallo y poder mejorar.";
                                                                break;
                                                            default:
                                                                mensaje = "No olvides que buscar en el texto te ayudará a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;

                            case 3: // timing 3-5 ("Recuerda")
                            case 5:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Usar las ayudas puede ser útil en caso de duda, pero es muy importante que busques en el texto para poder acertar. Revisar texto y alternativas te ayudará a comprender tu error.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Usar las ayudas puede ser útil en caso de duda, pero es muy importante que busques en el texto para poder acertar. Revisar texto y alternativas te ayudará a comprender tu error.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Usar las ayudas puede ser útil en caso de duda, pero es muy importante que busques en el texto para poder acertar. Revisar texto y alternativas te ayudará a comprender tu error.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, usar las ayudas es una buena estrategia si tienes dudas, pero para asegurar el acierto también es importante buscar información en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Aunque uses las ayudas, es importante que busques en el texto para asegurarte de responder bien. Revisa alternativas y texto para comprender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, a veces no es suficiente usar las ayudas ya que para acertar también es importante que busques en el texto. Ahora es fundamental que revises texto y alternativas. ";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, además de usar las ayudas, es importante que asegures tu respuesta buscando en el texto. Ahora no olvides que es muy importante revisar las alternativas y el texto.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, usar las ayudas es una buena estrategia si tienes dudas, pero para asegurar el acierto también es importante buscar información en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Usar las ayudas puede ser útil en caso de duda, pero es muy importante que busques en el texto para poder acertar. Revisar texto y alternativas te ayudará a comprender tu error.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Usar las ayudas puede ser útil en caso de duda, pero es muy importante que busques en el texto para poder acertar. Revisar texto y alternativas te ayudará a comprender tu error.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Usar las ayudas puede ser útil en caso de duda, pero es muy importante que busques en el texto para poder acertar. Revisar texto y alternativas te ayudará a comprender tu error.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, usar las ayudas es una buena estrategia si tienes dudas, pero para asegurar el acierto también es importante buscar información en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Aunque uses las ayudas, es importante que busques en el texto para asegurarte de responder bien. Revisa alternativas y texto para comprender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, a veces no es suficiente usar las ayudas ya que para acertar también es importante que busques en el texto. Ahora es fundamental que revises texto y alternativas. ";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, además de usar las ayudas, es importante que asegures tu respuesta buscando en el texto. Ahora no olvides que es muy importante revisar las alternativas y el texto.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, usar las ayudas es una buena estrategia si tienes dudas, pero para asegurar el acierto también es importante buscar información en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Usar las ayudas puede ser útil en caso de duda, pero es muy importante que busques en el texto para poder acertar. Revisar texto y alternativas te ayudará a comprender tu error.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Usar las ayudas puede ser útil en caso de duda, pero es muy importante que busques en el texto para poder acertar. Revisar texto y alternativas te ayudará a comprender tu error.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Usar las ayudas puede ser útil en caso de duda, pero es muy importante que busques en el texto para poder acertar. Revisar texto y alternativas te ayudará a comprender tu error.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, usar las ayudas es una buena estrategia si tienes dudas, pero para asegurar el acierto también es importante buscar información en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Aunque uses las ayudas, es importante que busques en el texto para asegurarte de responder bien. Revisa alternativas y texto para comprender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, a veces no es suficiente usar las ayudas ya que para acertar también es importante que busques en el texto. Ahora es fundamental que revises texto y alternativas. ";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, además de usar las ayudas, es importante que asegures tu respuesta buscando en el texto. Ahora no olvides que es muy importante revisar las alternativas y el texto.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, usar las ayudas es una buena estrategia si tienes dudas, pero para asegurar el acierto también es importante buscar información en el texto.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, es importante que busques en el texto para asegurar tu respuesta. Si revisas el texto y las alternativas podrás entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, es importante que busques en el texto para asegurar tu respuesta. Si revisas el texto y las alternativas podrás entender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, es importante que busques en el texto para asegurar tu respuesta. Si revisas el texto y las alternativas podrás entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Ten cuidado, ya sabes que si buscas es más probable que aciertes.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Es importante que busques en el texto para asegurarte de responder bien. Revisa alternativas y texto para comprender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, para acertar es importante que busques en el texto. Ahora recuerda revisar texto y alternativas para poder mejorar en otras preguntas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, es importante que asegures tu respuesta buscando en el texto. Ahora no olvides revisar las alternativas y el texto, es muy importante para que aprendas de tus fallos.";
                                                                break;
                                                            default:
                                                                mensaje = "Ten cuidado, ya sabes que si buscas es más probable que aciertes.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Es importante que busques en el texto antes de responder. Usar las ayudas también puede servirte. Revisa texto y alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Es importante que busques en el texto antes de responder. Usar las ayudas también puede servirte. Revisa texto y alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Es importante que busques en el texto antes de responder. Usar las ayudas también puede servirte. Revisa texto y alternativas para comprender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Es importante que busques en el texto para responder bien. Usar las ayudas también puede serte útil si dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Es importante que busques en el texto para asegurarte de responder bien. Puedes usar las ayudas si lo necesitas. Ahora revisa alternativas y texto para comprender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, es importante que busques en el texto para responder bien. Si lo necesitas, puedes usar las ayudas. Ahora no olvides revisar el texto y las alternativas para comprender el fallo y poder mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, buscar en el texto es importante para responder bien. Puedes usar las ayudas si lo necesitas. Ahora es esencial que revises el texto y las alternativas para comprender tu fallo y poder mejorar.";
                                                                break;
                                                            default:
                                                                mensaje = "Es importante que busques en el texto para responder bien. Usar las ayudas también puede serte útil si dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, para acertar es importante que busques en el texto y uses las ayudas si las necesitas. Revisa texto y alternativas para entender por qué esa respuesta es la correcta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, para acertar es importante que busques en el texto y uses las ayudas si las necesitas. Revisa texto y alternativas para entender por qué esa respuesta es la correcta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, para acertar es importante que busques en el texto y uses las ayudas si las necesitas. Revisa texto y alternativas para entender por qué esa respuesta es la correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, es importante que busques en el texto y uses las ayudas para responder.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Es importante que busques en el texto para asegurarte de responder bien. Usar las ayudas en caso de dudas también puede ser muy útil. Ahora revisa alternativas y texto para comprender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, buscar la respuesta en el texto es importante para responder bien. Las ayudas también pueden ser muy útiles si tienes dudas. Ahora es importanque que revises alternativas y texto para comprender el fallo y poder hacerlo mejor a la próxima.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, es importante que busques en el texto para responder bien. En caso de dudas, también puede ser útil usar las ayudas. Ahora es esencial que revises el texto y las alternativas para comprender tu fallo y poder mejorar.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, es importante que busques en el texto y uses las ayudas para responder.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, es importante que asegures tu respuesta buscando en el texto. Recuerda que usar las ayudas también puede serte útil. Ahora revisar las alternativas y releer el texto te ayudará a entender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, es importante que asegures tu respuesta buscando en el texto. Recuerda que usar las ayudas también puede serte útil. Ahora revisar las alternativas y releer el texto te ayudará a entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, es importante que asegures tu respuesta buscando en el texto. Recuerda que usar las ayudas también puede serte útil. Ahora revisar las alternativas y releer el texto te ayudará a entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, para acertar es importante que busques en el texto y uses las ayudas si las necesitas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Es importante que busques en el texto y uses las ayudas para asegurarte de responder bien. Ahora revisa el texto y las alternativas para entender por qué has fallado. ";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, es importante que asegures tu respuesta buscando en el texto y que pienses si necesitas las ayudas. Ahora no olvides revisar el texto y las alternativas para entender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, recuerda que buscar información en el texto y usar las ayudas son dos estrategias fundamentales cuando contestamos a preguntas. Ahora es muy importante que revises el texto y las alternativas para poder mejorar.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, para acertar es importante que busques en el texto y uses las ayudas si las necesitas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, es importante que busques en el texto para asegurar tu respuesta. Si revisas el texto y las alternativas podrás entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, es importante que busques en el texto para asegurar tu respuesta. Si revisas el texto y las alternativas podrás entender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, es importante que busques en el texto para asegurar tu respuesta. Si revisas el texto y las alternativas podrás entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Ten cuidado, ya sabes que si buscas es más probable que aciertes.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Es importante que busques en el texto para asegurarte de responder bien. Revisa alternativas y texto para comprender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, para acertar es importante que busques en el texto. Ahora recuerda revisar texto y alternativas para poder mejorar en otras preguntas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, es importante que asegures tu respuesta buscando en el texto. Ahora no olvides revisar las alternativas y el texto, es muy importante para que aprendas de tus fallos.";
                                                                break;
                                                            default:
                                                                mensaje = "Ten cuidado, ya sabes que si buscas es más probable que aciertes.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;

                            case 8: // timing 8-11-13 ("Insisto")
                            case 11:
                            case 13:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Has usado las ayudas, pero recuerda que es fundamental que busques en el texto para responder bien. Revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Has usado las ayudas, pero recuerda que es fundamental que busques en el texto para responder bien. Revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Has usado las ayudas, pero recuerda que es fundamental que busques en el texto para responder bien. Revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, está bien que uses las ayudas si dudas, pero recuerda que buscar en el texto también es importante para acertar.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero recuerda que es importante que busques en el texto antes de responder. Para entender el fallo, revisa texto y alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, aunque has usado las ayudas, te recuerdo que un pequeño esfuerzo buscando en el texto te ayudará a responder bien. Ahora es importante revisar texto y alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, bien por usar las ayudas, pero recuerda que buscar en el texto es fundamental para asegurar tu respuesta. No olvides que es muy importante revisar texto y alternativas para mejorar.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, está bien que uses las ayudas si dudas, pero recuerda que buscar en el texto también es importante para acertar.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Has usado las ayudas, pero recuerda que es fundamental que busques en el texto para responder bien. Revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Has usado las ayudas, pero recuerda que es fundamental que busques en el texto para responder bien. Revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Has usado las ayudas, pero recuerda que es fundamental que busques en el texto para responder bien. Revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, está bien que uses las ayudas si dudas, pero recuerda que buscar en el texto también es importante para acertar.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero recuerda que es importante que busques en el texto antes de responder. Para entender el fallo, revisa texto y alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, aunque has usado las ayudas, te recuerdo que un pequeño esfuerzo buscando en el texto te ayudará a responder bien. Ahora es importante revisar texto y alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, bien por usar las ayudas, pero recuerda que buscar en el texto es fundamental para asegurar tu respuesta. No olvides que es muy importante revisar texto y alternativas para mejorar.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, está bien que uses las ayudas si dudas, pero recuerda que buscar en el texto también es importante para acertar.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Has usado las ayudas, pero recuerda que es fundamental que busques en el texto para responder bien. Revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Has usado las ayudas, pero recuerda que es fundamental que busques en el texto para responder bien. Revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Has usado las ayudas, pero recuerda que es fundamental que busques en el texto para responder bien. Revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, está bien que uses las ayudas si dudas, pero recuerda que buscar en el texto también es importante para acertar.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Está bien que uses las ayudas si tienes dudas, pero recuerda que es importante que busques en el texto antes de responder. Para entender el fallo, revisa texto y alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, aunque has usado las ayudas, te recuerdo que un pequeño esfuerzo buscando en el texto te ayudará a responder bien. Ahora es importante revisar texto y alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, bien por usar las ayudas, pero recuerda que buscar en el texto es fundamental para asegurar tu respuesta. No olvides que es muy importante revisar texto y alternativas para mejorar.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, está bien que uses las ayudas si dudas, pero recuerda que buscar en el texto también es importante para acertar.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, no olvides que para acertar es muy importante buscar. Revisa el texto y las alternativas, te ayudará a entender por qué esa opción no es correcta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, no olvides que para acertar es muy importante buscar. Revisa el texto y las alternativas, te ayudará a entender por qué esa opción no es correcta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, no olvides que para acertar es muy importante buscar. Revisa el texto y las alternativas, te ayudará a entender por qué esa opción no es correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Insisto, un pequeño esfuerzo buscando en el texto te ayudará a contestar bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Recuerda que es importante que busques en el texto antes de responder. Para entender el fallo, revisa texto y alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, te recuerdo que un pequeño esfuerzo buscando en el texto te ayudará a responder bien. Ahora es importante revisar texto y alternativas para entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, recuerda que buscar en el texto es fundamental para asegurar tu respuesta. Recuerda que ahora es muy importante que revises texto y alternativas para entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Insisto, un pequeño esfuerzo buscando en el texto te ayudará a contestar bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Recuerda que es importante que busques en el texto antes de responder, además usar las ayudas puede ser útil si tienes dudas. Si revisas texto y alternativas entenderás por qué esa es la respuesta correcta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Recuerda que es importante que busques en el texto antes de responder, además usar las ayudas puede ser útil si tienes dudas. Si revisas texto y alternativas entenderás por qué esa es la respuesta correcta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Recuerda que es importante que busques en el texto antes de responder, además usar las ayudas puede ser útil si tienes dudas. Si revisas texto y alternativas entenderás por qué esa es la respuesta correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Recuerda que es importante que busques en el texto antes de responder, además usar las ayudas puede ser útil si tienes dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Recuerda que es importante buscar en texto la respuesta a la pregunta. Usar las ayudas también puede ayudarte a encontrar la respuesta. Revisa el texto y las alternativas para saber por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, debes recordar que es importante buscar en el texto para responder bien. Usa las ayudas si las necesitas. Ahora revisa el texto y las alternativas para comprender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, recuerda que es importante usar la estrategia de buscar para acertar. Usar las ayudas también puede ayudarte. No olvides revisar texto y alternativas para poder mejorar.";
                                                                break;
                                                            default:
                                                                mensaje = "Recuerda que es importante que busques en el texto antes de responder, además usar las ayudas puede ser útil si tienes dudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, te recuerdo que un pequeño esfuerzo buscando en el texto te ayudará a responder bien. Si dudas, también es importante usar las ayudas. Ahora revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, te recuerdo que un pequeño esfuerzo buscando en el texto te ayudará a responder bien. Si dudas, también es importante usar las ayudas. Ahora revisa el texto y las alternativas.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, te recuerdo que un pequeño esfuerzo buscando en el texto te ayudará a responder bien. Si dudas, también es importante usar las ayudas. Ahora revisa el texto y las alternativas.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, recuerda que es importante buscar información en el texto para responder bien.  También es importante que consideres si necesitas las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Recuerda que buscar y usar las ayudas es importante para acertar.  Para entender por qué has fallado, revisa el texto y las alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, recuerda que es fundamental buscar en el texto para acertar. En caso de duda, también es importante que uses las ayudas. Ahora revisa el texto y las aternativas para mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, recuerda que es importante que busques en el texto la información antes de responder y que uses las ayudas. No olvides revisar el texto y las alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, recuerda que es importante buscar información en el texto para responder bien.  También es importante que consideres si necesitas las ayudas.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, recuerda que buscar en el texto es fundamental para asegurar tu respuesta. Además no olvides que usar las ayudas puede aclarar tus dudas. Ahora revisa el texto y  las alternativas para entender tu fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, recuerda que buscar en el texto es fundamental para asegurar tu respuesta. Además no olvides que usar las ayudas puede aclarar tus dudas. Ahora revisa el texto y  las alternativas para entender tu fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, recuerda que buscar en el texto es fundamental para asegurar tu respuesta. Además no olvides que usar las ayudas puede aclarar tus dudas. Ahora revisa el texto y  las alternativas para entender tu fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, recuerda que es importante buscar información en el texto y usar las ayudas, siempre que tengas dudas sobre cuál es la respuesta correcta.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Recuerda que buscar en el texto y usar las ayudas son dos estrategias fundamentales para responder bien.  Ahora revisa el texto y las alternativas para comprender el fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, recuerda que buscar en el texto es fundamental para asegurar tu respuesta. Además no olvides que usar las ayudas puede aclarar tus dudas. Ahora es importante que revises el texto y  las alternativas para entender tu fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, recuerda que hemos aprendido que la búsqueda de información en el texto y emplear las ayudas son dos estrategias fundamentales cuando contestamos a preguntas.  Ahora no olvides que es importante revisar el texto y las alternativas para hacerlo mejor la próxima vez.";
                                                                break;
                                                            default:
                                                                mensaje = "USUARIO, recuerda que es importante buscar información en el texto y usar las ayudas, siempre que tengas dudas sobre cuál es la respuesta correcta. ";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "USUARIO, no olvides que para acertar es muy importante buscar. Revisa el texto y las alternativas, te ayudará a entender por qué esa opción no es correcta.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, no olvides que para acertar es muy importante buscar. Revisa el texto y las alternativas, te ayudará a entender por qué esa opción no es correcta.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, no olvides que para acertar es muy importante buscar. Revisa el texto y las alternativas, te ayudará a entender por qué esa opción no es correcta.";
                                                                break;
                                                            default:
                                                                mensaje = "Insisto, un pequeño esfuerzo buscando en el texto te ayudará a contestar bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Recuerda que es importante que busques en el texto antes de responder. Para entender el fallo, revisa texto y alternativas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "USUARIO, te recuerdo que un pequeño esfuerzo buscando en el texto te ayudará a responder bien. Ahora es importante revisar texto y alternativas para entender el fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "USUARIO, recuerda que buscar en el texto es fundamental para asegurar tu respuesta. Recuerda que ahora es muy importante que revises texto y alternativas para entender el fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Insisto, un pequeño esfuerzo buscando en el texto te ayudará a contestar bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;
                            default:
                                #region AYUDAR
                                switch (du.AyudaStatus)
                                {
                                    case 1:
                                        #region AYUDAPOS
                                        switch (du.AyudaPos)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si revisas el texto y las alternativas, podrás entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Es importante que revises el texto y las alternativas para poder comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Recuerda que es importante que revises el texto y las alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si revisas el texto y las alternativas, podrás entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Es importante que revises el texto y las alternativas para poder comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Recuerda que es importante que revises el texto y las alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Esta bien que uses las ayudas, seguro que pronto te ayudarán a responder. Ahora revisa texto y alternativas para enteder tu fallo.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si revisas el texto y las alternativas, podrás entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Es importante que revises el texto y las alternativas para poder comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Recuerda que es importante que revises el texto y las alternativas para entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Está bien que hayas usado las ayudas. La próxima vez te será más útil.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Revisar la información destacada en el texto puede ayudarte a entender tu fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Recuerda que revisar la información destacada en el texto puede ser muy útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Ojo! Es muy importante que revises la información destacada en el texto.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                    case -1:
                                        #region AYUDANEG
                                        switch (du.AyudaNeg)
                                        {
                                            case 1:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Usar las ayudas puede servirte para contestar bien en próximas preguntas. Ahora revisar texto y alternativas te ayudará a entender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Usar las ayudas puede servirte para contestar bien en próximas preguntas. Ahora revisar texto y alternativas te ayudará a entender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Usar las ayudas puede servirte para contestar bien en próximas preguntas. Ahora revisar texto y alternativas te ayudará a entender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Usar las ayudas te puede ayudar a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "En caso de dudas, usar las ayudas te será útil para responder bien. Ahora revisa el texto y las alternativas para entender por qué has fallado, te ayudará en próximas preguntas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Usar las ayudas te será útil para responder bien en caso de dudas. También es importate que revises el texto y las alternativas para comprender el fallo y poder mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Si tienes dudas, usar las ayudas te será útil para responder bien. Y recuerda que es importante revisar texto y alternativas para entender los fallos y poder mejorar en otras preguntas.";
                                                                break;
                                                            default:
                                                                mensaje = "Usar las ayudas te puede ayudar a responder bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 3:
                                            case 5:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Si tienes dudas es imp que uses las ayudas para responder. Si revisas el texto y las alternativas entenderás por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Si tienes dudas es imp que uses las ayudas para responder. Si revisas el texto y las alternativas entenderás por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Si tienes dudas es imp que uses las ayudas para responder. Si revisas el texto y las alternativas entenderás por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Recuerda que las ayudas te pueden ser útiles para contestar bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Es importante que uses las ayudas para responder bien en caso de dudas. Ahora revisa el texto y las alternativas para entender por qué has fallado, te ayudará en próximas preguntas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Si tienes dudas es importante que uses las ayudas para responder bien. También es importante que revises el texto y las alternativas para comprender el fallo y poder mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "En caso de dudas, es importante que uses las ayudas para responder bien. Y recuerda que es importante revisar texto y alternativas para entender los fallos y poder mejorar en otras preguntas.";
                                                                break;
                                                            default:
                                                                mensaje = "Recuerda que las ayudas te pueden ser útiles para contestar bien.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                            case 8:
                                            case 11:
                                            case 13:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Recuerda que en caso de dudas, usar las ayudas será útil para contestar bien. Revisa el texto y las alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Recuerda que en caso de dudas, usar las ayudas será útil para contestar bien. Revisa el texto y las alternativas para comprender por qué has fallado.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Recuerda que en caso de dudas, usar las ayudas será útil para contestar bien. Revisa el texto y las alternativas para comprender por qué has fallado.";
                                                                break;
                                                            default:
                                                                mensaje = "Siempre que tengas dudas, piensa que las ayudas pueden facilitar tu tarea.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Recuerda que en caso de dudas, es importante usar las ayudas para responder bien. Ahora revisa el texto y las alternativas para entender por qué has fallado, te ayudará en próximas preguntas.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Recuerda que usar las ayudas te será útil para responder bien en caso de dudas. También es importante que revises el texto y las alternativas para comprender el fallo y poder mejorar.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "Si tienes dudas, recuerda que es importante usar las ayudas para responder bien. Y recuerda que es importante revisar texto y alternativas para entender los fallos y poder mejorar en otras preguntas.";
                                                                break;
                                                            default:
                                                                mensaje = "Siempre que tengas dudas, piensa que las ayudas pueden facilitar tu tarea.";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                }
                                                #endregion
                                                break;
                                            default:
                                                #region REVISAR
                                                switch (du.RevisaStatus)
                                                {
                                                    case 1:
                                                        #region REVISAPOS
                                                        switch (du.RevisaPos) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;
                                                    case -1:
                                                        #region REVISANEG
                                                        switch (du.RevisaNeg) // FINAL
                                                        {
                                                            case 1: // timing 1 (sin coletilla)
                                                                mensaje = "Revisar la información destacada en el texto puede ayudarte a entender tu fallo.";
                                                                break;

                                                            case 3: // timing 3-5 ("Recuerda")
                                                            case 5:
                                                                mensaje = "Recuerda que revisar la información destacada en el texto puede ser muy útil.";
                                                                break;

                                                            case 8: // timing 8-11-13 ("Insisto")
                                                            case 11:
                                                            case 13:
                                                                mensaje = "¡Ojo! Es muy importante que revises la información destacada en el texto.";
                                                                break;
                                                            default:
                                                                mensaje = "";
                                                                break;
                                                        }
                                                        #endregion
                                                        break;

                                                }
                                                #endregion
                                                break;
                                        }
                                        #endregion
                                        break;
                                }
                                #endregion
                                break;
                        }
                        #endregion
                        break;
                }
                #endregion
            }

            mensaje = mensaje.Replace("USUARIO", du.UserProfile.Nombre);

            return mensaje;
        }

        

        private Boolean CorregirRespuesta(int PreguntaID, string Respuesta)
        {
            logger.Debug("ReadAndLearnController/CorregirRespuesta");
            Pregunta pregunta = db.Preguntas.Find(PreguntaID);
           
            foreach (Alternativa alt in pregunta.Alternativas) {
                if (alt.Opcion == Respuesta)
                    return alt.Valor;
            }

            return false;
        }

        private Boolean CorregirSeleccion(int PreguntaID, string Seleccion)
        {
            logger.Debug("ReadAndLearnController/CorregirSeleccion");
            Pregunta pregunta = db.Preguntas.Find(PreguntaID);
            string Pertinente = "";
            Double Porcentaje = 0.0;
            bool flag_status = false;
            int caracPerts = 0;
            int caracNoPerts = 0;

            foreach (string sel in Seleccion.Split('/'))
            {
                flag_status = false;

                foreach (string per in Pertinente.Split('/'))
                {
                    LevenshteinDistance(sel, per, out Porcentaje);

                    if (Porcentaje < 0.2) // Bien
                    {
                        caracPerts += sel.Length;
                        flag_status = true;
                        break;
                    }
                }

                if (!flag_status)
                {
                    caracNoPerts += sel.Length;
                }
            }

            return (caracPerts - caracNoPerts) > (Pertinente.Length / 2);
        }

        [HttpPost]
        public ActionResult ValidarPregunta(int PreguntaID, string respuesta)
        {
            logger.Debug("ReadAndLearnController/ValidarPregunta");
            Pregunta preg = db.Preguntas.Find(PreguntaID);
            ConfigPregunta configPreg = db.ConfigPregunta.Find(5);

            if (preg.TipoPreguntaID == 1)
            {
                foreach(Alternativa alt in preg.Alternativas)
                {
                    if (alt.Opcion == respuesta)
                    {
                        if (alt.Valor) // Acierto
                        {
                            return Json(new { codeOP = GetCodeOP_Feedback(configPreg), mensaje = alt.FeedbackContenido, PreguntaID = preg.PreguntaID });
                        }
                        else // Fallo
                        {
                            return Json(new { codeOP = GetCodeOP_Feedback(configPreg), mensaje = alt.FeedbackContenido, PreguntaID = preg.PreguntaID });
                        }
                    }
                }
            }
            else
            {
                string[,] Criterios = new string[preg.Criterios.Count(), 2];

                int i = 0;
                foreach (Criterio cri in preg.Criterios)
                {
                    Criterios[i, 0] = cri.Opcion;
                    Criterios[i, 1] = cri.Valor.ToString();
                    i++;
                }

                double sdfsdf = Corrector(Criterios, respuesta);

            }
            return Json(new { redirect = Url.Action("Pregunta", new { PreguntaID = PreguntaID }) });             
        }

        public ActionResult Pagina(int TextoID)
        {
            logger.Debug("ReadAndLearnController/Pagina");
            return View(db.Textos.Find(TextoID).Paginas.First());
        }

        public ActionResult Escena(int GrupoID, int ModuloID, int escActual, int? accActual)
        {
            logger.Debug("ReadAndLearnController/Escena");
            /*
            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            foreach (Escena acc in db.Escenas)
            {
                continue;
            }

            Modulo modulo = db.Modulos.Find(ModuloID);

            Escena escena = db.Escenas.Find(modulo.Escenas.ElementAt(escActual).EscenaID);

            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;
            ViewBag.escActual = escActual;
            ViewBag.accActual = Convert.ToInt32(accActual);

             */
            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            foreach (Escena acc in db.Escenas)
            {
                continue;
            }

            Modulo modulo = db.Modulos.Find(ModuloID);

            Escena escena = db.Escenas.Find(modulo.Escenas.ElementAt(escActual).EscenaID);

            Accion accion = escena.Acciones.ElementAt(Convert.ToInt32(accActual));

            //Accion accion = escena.Acciones.ElementAt(accActual);

            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;
            ViewBag.escActual = escActual;
            ViewBag.accActual = Convert.ToInt32(accActual);
            ViewBag.TextoID = accion.TextoID;
            ViewBag.PreguntaID = accion.PreguntaID;
            ViewBag.mensaje = accion.Mensaje;

            return View(db.Escenas.Find(escena.EscenaID).Acciones.ElementAt(Convert.ToInt32(accActual)));
        }


        public ActionResult EscenaFeedbackCompleta(int PreguntaID, int codeOP, string mensaje, int accTotalFeedback, int ModuloID, int escActual, int accActual, int GrupoID)
        {
            logger.Debug("ReadAndLearnController/EscenaFeedbackCompleta");
            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            foreach (Escena acc in db.Escenas)
            {
                continue;
            }
            ViewBag.mensaje = mensaje;
            ViewBag.codeOP = codeOP;
            ViewBag.PreguntaID = PreguntaID;
            ViewBag.ModuloID = ModuloID;
            ViewBag.escActual = escActual;
            ViewBag.accActual = accActual;
            ViewBag.GrupoID = GrupoID;

            return View();
        }

        public ActionResult EscenaFeedback(int PreguntaID, int codeOP, string mensaje, int accTotalFeedback, int ModuloID, int escActual, int accActual, int GrupoID)
        {
            logger.Debug("ReadAndLearnController/EscenaFeedback");
            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            foreach (Escena acc in db.Escenas)
            {
                continue;
            }
            ViewBag.mensaje = mensaje;
            ViewBag.codeOP = codeOP;
            ViewBag.PreguntaID = PreguntaID;
            ViewBag.ModuloID = ModuloID;
            ViewBag.escActual = escActual;
            ViewBag.accActual = accActual;
            ViewBag.GrupoID = GrupoID;        

            return View();
        }

        public ActionResult FinFeedbackContenido(int GrupoID, int ModuloID, int escActual, int accActual)
        {
            logger.Debug("ReadAndLearnController/FinFeedbackContenido");
            int nuevaAccion = accActual + 1;
            
            return Json(new { redirect = Url.Action("Escena", new { GrupoID = GrupoID, ModuloID = ModuloID, escActual = escActual, accActual = nuevaAccion }) });             
        }

        
        public ActionResult SiguienteAccionFeedback(int GrupoID, int ModuloID, int escActual, int? accActual)
        {
            logger.Debug("ReadAndLearnController/SiguienteAccionFeedback");
            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            foreach (Escena acc in db.Escenas)
            {
                continue;
            }

            Modulo modulo = db.Modulos.Find(ModuloID);

            Escena escena = db.Escenas.Find(modulo.Escenas.ElementAt(escActual).EscenaID);

            Accion accion = escena.Acciones.ElementAt(Convert.ToInt32(accActual));

            //Accion accion = escena.Acciones.ElementAt(accActual);

            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;
            ViewBag.escActual = escActual;
            ViewBag.accActual = Convert.ToInt32(accActual);
            ViewBag.TextoID = accion.TextoID;
            ViewBag.PreguntaID = accion.PreguntaID;

            return Json(new { codeOP = accion.CodeOP, mensaje = accion.Mensaje, PreguntaID = accion.PreguntaID, TextoID = accion.TextoID });
        }

        public ActionResult SiguienteAccion(int GrupoID, int ModuloID, int escActual, int? accActual)
        {
            logger.Debug("ReadAndLearnController/SiguienteAccion");
            string mensaje = "";

            var user = (from u in db.UserProfiles
                        where User.Identity.Name == u.UserName
                        select u).Single();

            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            foreach (Escena acc in db.Escenas)
            {
                continue;
            }
            
            Modulo modulo = db.Modulos.Find(ModuloID);

            Escena escena = db.Escenas.Find(modulo.Escenas.ElementAt(escActual).EscenaID);

            Accion accion = escena.Acciones.ElementAt(Convert.ToInt32(accActual));

            if (accion.Mensaje != null)
                mensaje = accion.Mensaje.Replace("USUARIO", user.Nombre);

            //Accion accion = escena.Acciones.ElementAt(accActual);

            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;
            ViewBag.escActual = escActual;
            ViewBag.accActual = Convert.ToInt32(accActual);
            ViewBag.TextoID = accion.TextoID;
            ViewBag.PreguntaID = accion.PreguntaID;

            return Json(new { codeOP = accion.CodeOP, mensaje = mensaje, PreguntaID = accion.PreguntaID, TextoID = accion.TextoID });            
        }

        public ActionResult Workspace(int GrupoID, int ModuloID, int escActual)
        {
            logger.Debug("ReadAndLearnController/Workspace");
            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;
            ViewBag.esActual = escActual;

            return View();
        }

        public ActionResult Escritorio()
        {
            logger.Debug("ReadAndLearnController/Escritorio");
            return View();
        }

        #region Levenshtein
        public double Corrector(string[,] CriterioCorrecion, string respuesta)
        {
            logger.Debug("ReadAndLearnController/Corrector");
            double MaximoPositivo = 0;
            double MaximoNegativo = 0;
            double[] Correccion = new double[CriterioCorrecion.GetLength(0)];
            // 'nCriteriosMax, es un entero que le informa de cuantos criterios de corrección hemos utilizado como máximo.

            // 'Si el alumno no responde 0
            respuesta = respuesta.Trim();

            if (respuesta == "")
                return 0;

            // 'Si hay respuesta la analizamos en un bucle, pasando por todos los criterios de corrección introducidos.
            for (int i = 0; i < CriterioCorrecion.GetLength(0); i++)
            {
                // 'Los criterios pueden estar vacios, ya que ese máximo es compartido por todas las preguntas.
                if (CriterioCorrecion[i,0] != null)
                {
                    Correccion[i] = FilterStrings(respuesta, CriterioCorrecion[i,0]);
                    Correccion[i] = Correccion[i] * Convert.ToDouble(CriterioCorrecion[i,1]);
                }
            }

            for (int j = 0; j < CriterioCorrecion.GetLength(0); j++)
            {
                if (Correccion[j] > MaximoPositivo)
                {
                    MaximoPositivo = Correccion[j];
                }
                if (Correccion[j] < MaximoNegativo)
                {
                    MaximoNegativo = Correccion[j];
                }
            }

            double Puntuacion;
            Puntuacion = MaximoPositivo + MaximoNegativo;

            // Impide valores negativos. ¿Es lo que queremos? Lanzar feedback en caso de hacerlo muy mal.
            /*if (Puntuacion < 0) 
                Puntuacion = 0;*/

            return Puntuacion;
        }

        static int FilterStrings(string str1, string str2)
        {
            logger.Debug("ReadAndLearnController/FilterStrings");
            List<char> FCar = new List<char>() { '.', ',', ';', ':', '-', '_', '¿', '?', '!', '¡', '*', '+', '\'', '\"', '&', '(', ')', '=', '$', '/', '#', '%', 'º', 'ª' };
            List<string> FWords = new List<string>() { "a", "acá", "ahí", "algo", "alguien", "algún", "alguna", "parte", "allí", "allá", "aquí", "bastante", "cerca", "de", "demasiado", "demasiado", "demasiada", "demasiados", 
                                                       "demasiadas", "él", "el", "la", "ella", "ellos", "ellas", "entonces", "eso", "es", "está", "este", "esta", "estos", "estas", "esto", "hay", "lejos", "los", "las", "más", 
                                                       "menos", "mi", "mis", "mucho", "mucha", "muchos", "muchas", "muy", "nada", "nadie", "ninguna", "no", "nunca", "otro", "otra", "otros", "otra", "poco", "poco", "poca", 
                                                       "pocos", "pocas", "porque", "que", "siempre", "si", "su", "sus", "tan", "tanto", "tanta", "tantos", "tantas", "tengo", "todavía", "todo", "todos", "todos", "tú", "tu", 
                                                       "tus", "uno", "una", "unos", "unas", "usted", "ustedes", "vosotros", "yo", "cómo", "cuál", "cuándo", "cuánto", "dónde", "qué", "quién", "a", "al", "lado", "partir", 
                                                       "alrededor", "antes", "través", "bajo", "cerca", "como", "con", "contra", "de", "del", "desde", "después", "detrás", "durante", "en", "lugar", "medio", "entre", "vez", 
                                                       "fuera", "hacia", "hasta", "para", "para", "por", "y", "según", "sin", "sobre", "tras" };

            /*
            ' Nwords1 = número de palabras en la respuesta
            ' Nwords2 = número de palabras en la alternativa correcta
            ' Nfunct1 = número de palabras función en la respuesta -estas no se tienen en cuenta
            ' Nfunct2 = número de palabras función en la alternativa correcta -estas no se tienen en cuenta
            */
            int NWords1 = 0, NWords2 = 0, NFunct1, NFunct2;
            List<string> RealWords1;
            List<string> RealWords2;

            // 'FILTRAR LA CADENA PARA ELIMINAR SIMBOLOS Y ESPACIOS REPETIDOS O ESPACIOS FINALES.
            char PreviousFCar = ' ';
            char NewCar, Car;
            string InputFAlumno = ""; // 'input filtrado del alumno
            string InputFCorrect = "";

            // '-> a) filtra el input del alumno
            for (int i = 0; i < str1.Length; i++)
            {
                Car = str1[i];

                foreach (char caracter in FCar)
                {
                    if (Car == caracter)
                        Car = ' ';
                }

                // Excepción 1
                if (PreviousFCar == ' ' && Car == ' ')
                    NewCar = ' ';
                else
                    NewCar = Car;

                // Excepción 2
                if ((i - 1) == str1.Length && Car == ' ')
                    NewCar = ' ';

                PreviousFCar = Car;
                InputFAlumno = InputFAlumno + NewCar;
            }

            RealWords1 = new List<string>(InputFAlumno.Split(' '));

            PreviousFCar = ' ';

            for (int i = 0; i < str2.Length; i++)
            {
                Car = str2[i];

                foreach (char caracter in FCar)
                {
                    if (Car == caracter)
                        Car = ' ';
                }

                // Excepción 1
                if (PreviousFCar == ' ' && Car == ' ')
                    NewCar = ' ';
                else
                    NewCar = Car;

                // Excepción 2
                if ((i - 1) == str1.Length && Car == ' ')
                    NewCar = ' ';

                PreviousFCar = Car;
                InputFCorrect = InputFCorrect + NewCar;
            }

            RealWords2 = new List<string>(InputFCorrect.Split(' '));

            // 'HALLA el número de palabras de tipo funcion en la respuesta y  en la alternativa correcta
            NFunct1 = 0;
            NFunct2 = 0;

            int nPalabras;

            foreach (string PalabraF in FWords)
            {
                nPalabras = 0;

                for (int i = 0; i < RealWords1.Count; i++)                    
                {
                    string PalabraR = RealWords1[i];

                    if (PalabraF.Trim() == PalabraR.Trim() || PalabraR == "")
                    {
                        RealWords1[nPalabras] = "FW";
                        NFunct1++;
                    }
                    else
                    {
                        RealWords1[nPalabras] = PalabraR.Trim();
                    }

                    nPalabras++;
                }

                NWords1 = RealWords1.Count;

                nPalabras = 0;

                for (int i = 0; i < RealWords2.Count; i++) 
                {
                    string PalabraR = RealWords2[i];

                    if (PalabraF.Trim() == PalabraR.Trim() || PalabraR == "")
                    {
                        RealWords2[nPalabras] = "FW";
                        NFunct2++;
                    }
                    else
                    {
                        RealWords2[nPalabras] = PalabraR.Trim();
                    }

                    nPalabras++;
                }

                NWords2 = RealWords2.Count;
            }

            /* 
             * 'IMPRIME una matriz con las "x"s (y=1) = las palabras clave de la alternativa correcta
             * 'las "y" todas y cada una de las palabras de la respuesta. EJ:
             * ' CORRECTA_1 ALUMNO_1 ALUMNO_2 ALUMNO_3 ALUMNO_4 ALUMNO_5
             * ' CORRECTA_2 ALUMNO_1 ALUMNO_2 ALUMNO_3 ALUMNO_4 ALUMNO_5
             * ' CORRECTA_3 ALUMNO_1 ALUMNO_2 ALUMNO_3 ALUMNO_4 ALUMNO_5
             */
            string[,] CompareRW = new string[(NWords2 - NFunct2), (1 + NWords1 - NFunct1)];
            int[,] Distancias = new int[(NWords2 - NFunct2), (1 + NWords1 - NFunct1)];
            double[,] Porcentaje = new double[(NWords2 - NFunct2), (1 + NWords1 - NFunct1)];
            int y = 0;
            int a = 0;

            foreach (string PalabraR in RealWords2)
            {
                if (PalabraR != "FW")
                {
                    int nPalabrasF = 0;

                    CompareRW[y, 0] = PalabraR;
                    foreach (string PalabraF in RealWords1)
                    {
                        if (PalabraF != "FW")
                        {
                            a++;
                            CompareRW[y, a] = PalabraF;
                        }
                    }
                }
            }

            // 'Halla las distancias de levenshetein
            for (int xs = 0; xs < (NWords2 - NFunct2); xs++)
            {
                for (int ys = 0; ys < (1 + NWords1 - NFunct1); ys++)
                {
                    Distancias[xs, ys] = LevenshteinDistance(CompareRW[xs, 0], CompareRW[xs, ys], out Porcentaje[xs, ys]);
                }
            }

            // 'decide si la respuesta equivale a la alternativa correcta
            int[] CorrectWord = new int[(NWords2 - NFunct2)];
            bool Match = false;
            int CorrectPerc = 0;

            if ((NWords1 - NFunct1) == 0) // 'siempre que la respuesta incluya al menos 1 palabra válida...
            {
                return 0;
            }
            else
            {
                bool[] WordUsed = new bool[NWords1 - NFunct1]; // 'palabras en la respuesta (la 1 la ocupa la palabra de la alternativa)

                for (int xs = 0; xs < (NWords2 - NFunct2); xs++)
                {
                    for (int ys = 0; ys < (NWords1 - NFunct1); ys++)
                    {
                        if (CompareRW[xs, 0].Count() >= 6)
                        {
                            if (Distancias[xs, ys + 1] <= 2)
                            {
                                Match = true; // 'si la palabra clave tiene más de 6 letras se admiten 2 errores tipográficos
                            }
                        }

                        if (CompareRW[xs, 0].Count() == 5)
                        {
                            if (Distancias[xs, ys + 1] <= 1)
                            {
                                Match = true; // 'si la palabra clave tiene más de 6 letras se admiten 2 errores tipográficos
                            }
                        }

                        if (CompareRW[xs, 0].Count() <= 4)
                        {
                            if (Distancias[xs, ys + 1] == 0)
                            {
                                Match = true; // 'si la palabra clave tiene más de 6 letras se admiten 2 errores tipográficos
                            }
                        }

                        if (Match)
                        {
                            if (WordUsed[ys] == false)
                            {
                                CorrectWord[xs] = 1;
                                WordUsed[ys] = true;
                            }

                            Match = false;
                        }
                    }
                }

                for (int xs = 0; xs < (NWords2 - NFunct2); xs++)
                {
                    CorrectPerc = CorrectPerc + CorrectWord[xs];
                }

                // 'Correccion(Alternativa) = Abs(CorrectPerc / (NWords2 - NFunct2))

                if ((CorrectPerc / (NWords2 - NFunct2)) == 1)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        static int LevenshteinDistance(string s, string t, out double porcentaje)
        {
            logger.Debug("ReadAndLearnController/LevenshteinDistance");
            porcentaje = 0;

            // d es una tabla con m+1 renglones y n+1 columnas
            int costo = 0;
            int m = s.Length;
            int n = t.Length;
            int[,] d = new int[m + 1, n + 1];

            // Verifica que exista algo que comparar
            if (n == 0) return m;
            if (m == 0) return n;

            // Llena la primera columna y la primera fila.
            for (int i = 0; i <= m; d[i, 0] = i++) ;
            for (int j = 0; j <= n; d[0, j] = j++) ;

            /// recorre la matriz llenando cada unos de los pesos.
            /// i columnas, j renglones
            for (int i = 1; i <= m; i++)
            {
                // recorre para j
                for (int j = 1; j <= n; j++)
                {
                    /// si son iguales en posiciones equidistantes el peso es 0
                    /// de lo contrario el peso suma a uno.
                    costo = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = System.Math.Min(System.Math.Min(d[i - 1, j] + 1,  //Eliminacion
                        d[i, j - 1] + 1),  //Inserccion 
                        d[i - 1, j - 1] + costo);  //Sustitucion
                }
            }
            /// Calculamos el porcentaje de cambios en la palabra.
            if (s.Length > t.Length)
                porcentaje = ((double)d[m, n] / (double)s.Length);
            else
                porcentaje = ((double)d[m, n] / (double)t.Length);
            return d[m, n];
        }


        [HttpPost]
        public ActionResult Algoritmo(string Pertinente, int TextoID, string NuevaSeleccion, int? PreguntaID)
        {
            logger.Debug("ReadAndLearnController/Algoritmo");
            if (NuevaSeleccion.Length > 15)
            {
                ConfigPregunta config = new ConfigPregunta();

                if (PreguntaID != null)
                {
                    config = db.ConfigPregunta.Single(c => c.PreguntaID == PreguntaID);
                }

                if (config != null && config.AutoSeleccion)
                {
                    Pertinente = Regex.Replace(Pertinente, "\\/+", "/");

                    if (Pertinente != "" && Pertinente.First() == ' ')
                        Pertinente = Pertinente.Substring(1);

                    if (Pertinente != "" && Pertinente.Last() == ' ')
                        Pertinente = Pertinente.Remove(Pertinente.Length - 1);

                    if (Pertinente != "" && Pertinente.First() == '/')
                        Pertinente = Pertinente.Substring(1);

                    if (Pertinente != "" && Pertinente.Last() == '/')
                        Pertinente = Pertinente.Remove(Pertinente.Length - 1);

                    Pertinente = Pertinente.Replace("/", "\\");

                    if (NuevaSeleccion != "" && NuevaSeleccion.First() == ' ')
                        NuevaSeleccion = NuevaSeleccion.Substring(1);

                    if (NuevaSeleccion != "" && NuevaSeleccion.Last() == ' ')
                        NuevaSeleccion = NuevaSeleccion.Remove(NuevaSeleccion.Length - 2);

                    // SELECCION PERTINENTE //
                    string contenido;
                    char[] delimiterChars = { '\\', '/' };

                    List<List<List<int>>> nuevoRangos = new List<List<List<int>>>();

                    List<int> unicoRango = new List<int>();
                    int num = 0;

                    foreach (Pagina pagina in db.Textos.Find(TextoID).Paginas)
                    {
                        nuevoRangos.Add(new List<List<int>>());

                        contenido = pagina.Contenido;

                        contenido = HttpUtility.HtmlDecode(contenido);

                        contenido = RemoveBetween(contenido, '<', '>');

                        //contenido = contenido.Replace("&nbsp;", " ");
                        contenido = contenido.Replace(System.Environment.NewLine, " ");
                        contenido = contenido.Replace((char)160, (char)32);

                        string selTmp;

                        selTmp = NuevaSeleccion.Replace((char)160, (char)32);

                        if (contenido.IndexOf(selTmp) != -1)
                        {
                            foreach (string strFR in selTmp.Split('.'))
                            {
                                if (strFR != "")
                                {
                                    string frase;

                                    int pos = contenido.IndexOf(strFR);
                                    int ini = 0, fin = 0;

                                    string anterior = contenido.Substring(0, pos);

                                    for (int i = anterior.Length - 1; i > 0; i--)
                                    {
                                        if (anterior[i] == '.')
                                        {
                                            ini = i + 1;
                                            break;
                                        }
                                    }

                                    string posterior = contenido.Substring(pos);

                                    for (int i = 0; i < posterior.Length; i++)
                                    {
                                        if (posterior[i] == '.')
                                        {
                                            fin = (pos - ini) + i + 1;
                                            break;
                                        }

                                    }

                                    frase = contenido.Substring(ini, fin);

                                    if (Pertinente.Length > 3)
                                    {
                                        Boolean flag = false;

                                        foreach (string str in Pertinente.Split('/'))
                                        {
                                            if (str == frase)
                                            {
                                                flag = true;
                                                break;
                                            }
                                        }

                                        if (!flag)
                                        {
                                            Pertinente += "\\" + frase;
                                        }
                                    }
                                    else
                                    {
                                        Pertinente = frase;
                                    }
                                }
                            }
                        }
                    }

                    return Json(new { result = Pertinente });
                }
                else
                {
                    if (Pertinente.Length > 5)
                    {
                        Pertinente = Regex.Replace(Pertinente, "\\/+", "/");

                        if (Pertinente != "" && Pertinente.First() == ' ')
                            Pertinente = Pertinente.Substring(1);

                        if (Pertinente != "" && Pertinente.Last() == ' ')
                            Pertinente = Pertinente.Remove(Pertinente.Length - 1);

                        if (Pertinente != "" && Pertinente.First() == '/')
                            Pertinente = Pertinente.Substring(1);

                        if (Pertinente != "" && Pertinente.Last() == '/')
                            Pertinente = Pertinente.Remove(Pertinente.Length - 1);

                        Pertinente = Pertinente.Replace("/", "\\");

                        if (NuevaSeleccion != "" && NuevaSeleccion.First() == ' ')
                            NuevaSeleccion = NuevaSeleccion.Substring(1);

                        if (NuevaSeleccion != "" && NuevaSeleccion.Last() == ' ')
                            NuevaSeleccion = NuevaSeleccion.Remove(NuevaSeleccion.Length - 2);

                        // SELECCION PERTINENTE //                    
                        string contenido;
                        char[] delimiterChars = { '\\', '/' };
                        List<List<List<int>>> rangos = new List<List<List<int>>>();

                        foreach (Pagina pagina in db.Textos.Find(TextoID).Paginas)
                        {
                            rangos.Add(new List<List<int>>());
                            
                            contenido = pagina.Contenido;

                            contenido = HttpUtility.HtmlDecode(contenido);

                            contenido = RemoveBetween(contenido, '<', '>');

                            //contenido = contenido.Replace("&nbsp;", " ");
                            contenido = contenido.Replace(System.Environment.NewLine, " ");
                            contenido = contenido.Replace((char)160, (char)32);

                            string[] frases = Pertinente.Split(delimiterChars);

                            foreach (string frase in frases)
                            {
                                string selTmp;

                                selTmp = frase.Replace((char)160, (char)32);

                                if (contenido.IndexOf(selTmp) != -1)
                                {
                                    rangos[rangos.Count - 1].Add(new List<int>());

                                    int ini = contenido.IndexOf(selTmp);
                                    int fin = ini + selTmp.Length;

                                    rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(ini);
                                    rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(fin);
                                }
                            }
                        }

                        List<List<List<int>>> nuevoRangos = new List<List<List<int>>>();

                        List<int> unicoRango = new List<int>();
                        int num = 0;

                        foreach (Pagina pagina in db.Textos.Find(TextoID).Paginas)
                        {
                            nuevoRangos.Add(new List<List<int>>());

                            contenido = pagina.Contenido;

                            contenido = HttpUtility.HtmlDecode(contenido);

                            contenido = RemoveBetween(contenido, '<', '>');

                            //contenido = contenido.Replace("&nbsp;", " ");
                            contenido = contenido.Replace(System.Environment.NewLine, " ");
                            contenido = contenido.Replace((char)160, (char)32);

                            string selTmp;

                            selTmp = NuevaSeleccion.Replace((char)160, (char)32);

                            if (contenido.IndexOf(selTmp) != -1)
                            {
                                num = nuevoRangos.Count - 1;
                                nuevoRangos[nuevoRangos.Count - 1].Add(new List<int>());

                                int ini = contenido.IndexOf(selTmp);
                                int fin = ini + selTmp.Length;

                                nuevoRangos[nuevoRangos.Count - 1][nuevoRangos[nuevoRangos.Count - 1].Count - 1].Add(ini);
                                nuevoRangos[nuevoRangos.Count - 1][nuevoRangos[nuevoRangos.Count - 1].Count - 1].Add(fin);
                                unicoRango.Add(ini);
                                unicoRango.Add(fin);
                            }
                        }

                        List<List<int>> CopiaRangos = new List<List<int>>();

                        List<int> filaRangos = new List<int>();
                        List<int> finRangos = new List<int>();

                        foreach (List<int> rang in rangos[num])
                        {
                            filaRangos.Add(rang[0]);
                            filaRangos.Add(rang[1]);
                        }

                        bool flag_ini = true;
                        bool flag_agregado = false;
                        int inicio = unicoRango[0];
                        int final = unicoRango[1];
                        int count = 0;

                        foreach (int n in filaRangos)
                        {
                            count++;

                            if (flag_agregado)
                            {
                                finRangos.Add(n);
                                continue;
                            }

                            if (flag_ini)
                            {
                                if (inicio < n)
                                {
                                    flag_ini = false;

                                    if (finRangos.Count % 2 == 0) // Par
                                    {
                                        finRangos.Add(inicio);

                                        if (final < n)
                                        {
                                            finRangos.Add(final);
                                            finRangos.Add(n);
                                            flag_agregado = true;
                                            continue; // Agregado
                                        }
                                    }
                                    else
                                    {
                                        if (final < n)
                                        {
                                            finRangos.Add(n);
                                            flag_agregado = true;
                                            continue; // Agregado
                                        }
                                    }
                                }
                                else
                                {
                                    finRangos.Add(n);
                                }
                            }
                            else
                            {
                                if (final < n)
                                {
                                    if ((count - 1) % 2 == 0) // Par
                                    {
                                        finRangos.Add(final);
                                        finRangos.Add(n);
                                        flag_agregado = true;
                                        continue; // Agregado
                                    }
                                    else // Impar
                                    {
                                        finRangos.Add(n);
                                        flag_agregado = true;
                                        continue; // Agregado
                                    }

                                }
                            }
                        }

                        if (!flag_agregado)
                        {
                            if (finRangos.Count % 2 == 0) // Par
                            {
                                finRangos.Add(inicio);
                                finRangos.Add(final);
                            }
                            else
                            {
                                finRangos.Add(final);
                            }

                            flag_agregado = true;
                        }

                        string nuevaSeleccion = "";
                        int numRang = 0;

                        foreach (Pagina pagina in db.Textos.Find(TextoID).Paginas)
                        {
                            contenido = pagina.Contenido;

                            contenido = HttpUtility.HtmlDecode(contenido);

                            contenido = RemoveBetween(contenido, '<', '>');

                            //contenido = contenido.Replace("&nbsp;", " ");
                            contenido = contenido.Replace(System.Environment.NewLine, " ");
                            contenido = contenido.Replace((char)160, (char)32);

                            if (numRang == num)
                            {
                                for (int z = 0; z < finRangos.Count; z++)
                                {
                                    nuevaSeleccion += contenido.Substring(finRangos[z], finRangos[z + 1] - finRangos[z]) + "/";
                                    z++;
                                }
                            }
                            else
                            {
                                foreach (List<int> r in rangos[numRang])
                                {
                                    nuevaSeleccion += contenido.Substring(r[0], r[1] - r[0]) + "/";
                                }
                            }

                            numRang++;
                        }

                        nuevaSeleccion = nuevaSeleccion.Remove(nuevaSeleccion.Length - 1);

                        NuevaSeleccion = nuevaSeleccion;

                        return Json(new { result = NuevaSeleccion });
                    }
                    else
                    {
                        return Json(new { result = NuevaSeleccion });
                    }
                }
            }
            else
            {
                return Json(new { result = Pertinente });
            }
        }
        
        /*
        [HttpPost]
        public ActionResult ValidarSeleccion(int ModuloID, int TextoID, string Answer, string Secuencia)
        {
            int cont = 0;
            int inicio = 0;

            var texto = db.Textos.Find(TextoID);

            var user = (from u in db.Alumnos
                        where User.Identity.Name == u.Usuario
                        select u).Single();

            // Coge la última secuencia del usuario.
            var secuencia = (from s in db.Secuencias
                             where s.AlumnoID == user.PersonID &&
                                   s.Abierta == true
                             orderby s.SecuenciaID
                             select s).FirstOrDefault();

            Pregunta_Ce pregunta = (Pregunta_Ce)texto.Preguntas.ToList()[secuencia.Pregunta];

            foreach (Accion acc in secuencia.Acciones.Reverse())
            {
                if (acc.Operacion.IndexOf("CPLT") != -1) //Encontrado
                {
                    inicio = acc.Orden;

                    break;
                }

                if (acc.Operacion.IndexOf("INT") != -1) //Encontrado
                {
                    inicio = acc.Orden;

                    break;
                }
            }

            string Pertinente = "";
            string tmpPertinente = "";

            foreach (Accion acc in secuencia.Acciones)
            {
                if (cont >= inicio)
                {
                    if (acc.Operacion.IndexOf("PTNT") != -1) //Encontrado
                    {
                        int ini = acc.Operacion.IndexOf("PTNT") + 5;
                        int fin = acc.Operacion.Count();

                        Pertinente = acc.Operacion.Substring(ini, fin - ini);
                        tmpPertinente = Pertinente;
                    }
                }

                cont++;
            }

            // SELECCION PERTINENTE //
            int CharPertPregunta = 0;
            int numPag = 0;
            string contenido;

            List<List<List<int>>> rangos = new List<List<List<int>>>();

            foreach (Pagina pagina in texto.Paginas)
            {
                rangos.Add(new List<List<int>>());

                contenido = pagina.Contenido;

                contenido = Server.UrlDecode(contenido);

                contenido = RemoveBetween(contenido, '<', '>');
                contenido = contenido.Replace("&nbsp;", " ");

                contenido = contenido.Replace((char)160, (char)32);


                string[] frases = pregunta.Pertinente.Split('\\');

                CharPertPregunta = pregunta.Pertinente.Length - frases.Count();

                foreach (string frase in frases)
                {
                    string strTmp = frase.Replace((char)160, (char)32);

                    //strTmp = strTmp.Replace(

                    if (contenido.IndexOf(strTmp) != -1)
                    {
                        rangos[rangos.Count - 1].Add(new List<int>());

                        int ini = contenido.IndexOf(strTmp);
                        int fin = ini + frase.Length;

                        rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(ini);
                        rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(fin);
                    }
                }
            }

            string[] selecciones = Pertinente.Split('\\');
            numPag = 0;
            int ragInf;
            int ragSup;

            int tmp1 = 0; // Número de caracteres pertinentes            

            int CharPertinentes = 0;
            int totalCharSelect = 0;

            string pertCorr = "";
            string pertTmp = "";
            string mensaje = "";

            foreach (string select in selecciones)
            {
                if (select == null || select == "")
                    continue;

                if (Pertinente[0] == ' ')
                {
                    Pertinente = Pertinente.Remove(0, 1);
                }

                pertCorr = select.Replace("\n", "");

                pertCorr = pertCorr.Replace((char)160, (char)32);

                foreach (Pagina pagina in texto.Paginas)
                {
                    string seleccion = select.Replace("\n", "");

                    contenido = pagina.Contenido;

                    contenido = Server.UrlDecode(contenido);

                    contenido = RemoveBetween(contenido, '<', '>');

                    contenido = contenido.Replace("&nbsp;", " ");

                    contenido = contenido.Replace((char)160, (char)32);
                    seleccion = seleccion.Replace((char)160, (char)32);

                    if (seleccion != "")
                        while (seleccion[0] == ' ')
                            seleccion = seleccion.Remove(0, 1);

                    if (contenido.IndexOf(seleccion) != -1)
                    {
                        ragInf = contenido.IndexOf(seleccion);
                        ragSup = ragInf + seleccion.Length;

                        foreach (List<int> posible in rangos[numPag].Reverse<List<int>>())
                        {
                            if (ragInf > posible[1] || ragSup < posible[0])
                            {
                                continue;
                            }
                            else
                            {
                                if (ragInf >= posible[0])
                                {
                                    if (ragSup <= posible[1]) // Esta todo dentro
                                    {
                                        tmp1 += seleccion.Length;
                                        pertCorr = "<span style=\"color:green\">" + pertCorr + "</span>";
                                        continue;
                                    }
                                    else // Sobresale por la derecha
                                    {
                                        tmp1 += seleccion.Length - (ragSup - posible[1]);
                                        pertCorr = "<span style=\"color:green\">" + pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (ragSup <= posible[1]) // Sobresale por la izquierda
                                    {
                                        tmp1 += seleccion.Length - (posible[0] - ragInf);
                                        pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">") + "</span>";
                                        continue;
                                    }
                                    else // Sobresale por ambos lados
                                    {
                                        tmp1 += seleccion.Length - (ragSup - posible[1]) - (posible[0] - ragInf);
                                        pertCorr = pertCorr.Insert(select.Length - (ragSup - posible[1]), "</span>");
                                        pertCorr = pertCorr.Insert(posible[0] - ragInf, "<span style=\"color:green\">");
                                        continue;
                                    }
                                }
                            }
                        }

                        pertTmp += pertCorr + "\\";
                    }

                    numPag++;
                }

                CharPertinentes += tmp1;

                totalCharSelect += select.Length;

                tmp1 = 0;
                numPag = 0;
            }

            Pertinente = Server.HtmlEncode(pertTmp).Replace('&', '?');



            // Porcentaje de texto pertinente seleccionado frente al texto pertinente de la pregunta.
            double porcPert = ((double)CharPertinentes * 100.0) / (double)CharPertPregunta;
            // Porcentaje de texto no pertinente seleccionado frente a texto pertinente seleccionado.
            double porcNoPert = 100.0 - ((double)CharPertinentes * 100.0) / (double)totalCharSelect;

            if (totalCharSelect == 0)
            {
                porcNoPert = 100;
            }

            if (secuencia.i < 1) // Primer Intento
            {

                secuencia.i = 1;

                db.SaveChanges();

                if (porcNoPert > 35) // SI IRRELEVANTE
                {
                    if (porcPert < 15) //NADA
                    {
                        Pertinente = tmpPertinente;
                        mensaje += "Has seleccionado información NO necesaria. /2º INTENTO: Revisa la pregunta. Selecciona SOLO la información necesaria.";
                    }
                    else // PARTE o TODO
                    {
                        if (porcPert > 65) // TODO
                        {
                            Pertinente = tmpPertinente;
                            mensaje += "Has seleccionado MÁS de lo necesario. /2º INTENTO: Revisa la pregunta. Elimina información no necesaria de tu selección.";
                        }
                        else // PARTE
                        {
                            Pertinente = tmpPertinente;
                            mensaje += "Has seleccionado SOLO PARTE de lo necesario y mucho NO necesario. /2º INTENTO: Revisa la pregunta. Elimina información no necesaria de tu selección y asegúrate de añadir TODA la necesaria.";
                        }
                    }
                }
                else // NO IRRELEVANTE
                {
                    if (porcPert < 15) //NADA
                    {
                        Pertinente = tmpPertinente;
                        mensaje += "Has seleccionado SOLO PARTE de lo necesario. /2º INTENTO: Revisa la pregunta. Asegúrate de añadir TODA la información necesaria.";
                    }
                    else // PARTE o TODO
                    {
                        if (porcPert > 65) // TODO
                        {
                            mensaje += "Has seleccionado TODO lo necesario. /Sigue así. Ahora responde a la pregunta.";
                            return Json(new { redirect = Url.Action("Pregunta", new { Pertinente = Pertinente, Respuesta = Answer, Feedback = mensaje, ModuloID = ModuloID, TextoID = TextoID, Sigue = "Sigue" }) });
                        }
                        else // PARTE
                        {
                            Pertinente = tmpPertinente;
                            mensaje += "Has seleccionado SOLO PARTE de lo necesario. /2º INTENTO: Revisa la pregunta. Asegúrate de añadir TODA la información necesaria.";
                        }
                    }
                }

                return Json(new { redirect = Url.Action("Pregunta", new { Pertinente = Pertinente, Respuesta = Answer, Feedback = mensaje, ModuloID = ModuloID, TextoID = TextoID, Sigue = "Repite" }) });
            }
            else // Segundo intento            
            {
                if (porcNoPert > 35) // SI IRRELEVANTE
                {
                    if (porcPert < 15) //NADA
                    {
                        mensaje += "Has seleccionado información NO necesaria. /ANTES DE RESPONDER, revisa en el texto la información que necesitas para responder.";
                    }
                    else // PARTE o TODO
                    {
                        if (porcPert > 65) // TODO
                        {
                            mensaje += "Has seleccionado MÁS de lo necesario. /ANTES DE RESPONDER, revisa tu selección.";
                        }
                        else // PARTE
                        {
                            mensaje += "Has seleccionado SOLO PARTE de lo necesario y mucho NO necesario. /ANTES DE RESPONDER, revisa en el texto la información que necesitas para responder.";
                        }
                    }
                }
                else // NO IRRELEVANTE
                {
                    if (porcPert < 15) //NADA
                    {
                        mensaje += "Has seleccionado SOLO PARTE de lo necesario. /ANTES DE RESPONDER, revisa en el texto la información que necesitas para responder.";
                    }
                    else // PARTE o TODO
                    {
                        if (porcPert > 65) // TODO
                        {
                            mensaje += "Has seleccionado TODO lo necesario. /Sigue así. Ahora responde a la pregunta.";
                        }
                        else // PARTE
                        {
                            mensaje += "Has seleccionado SOLO PARTE de lo necesario. /ANTES DE RESPONDER, revisa en el texto la información que necesitas para responder.";
                        }
                    }
                }
            }

            return Json(new { redirect = Url.Action("Pregunta", new { Pertinente = Pertinente, Respuesta = Answer, Feedback = mensaje, ModuloID = ModuloID, TextoID = TextoID, Sigue = "Sigue" }) });
        }
        */
        [HttpPost]
        public ActionResult Algoritmo_Original(string Pertinente, int TextoID, string NuevaSeleccion)
        {
            logger.Debug("ReadAndLearnController/Algoritmo_Original TODO: must not be here!");
            Pertinente = Regex.Replace(Pertinente, "\\/+", "/");

            if (Pertinente != "" && Pertinente.First() == ' ')
                Pertinente = Pertinente.Substring(1);

            if (Pertinente != "" && Pertinente.Last() == ' ')
                Pertinente = Pertinente.Remove(Pertinente.Length - 1);

            if (Pertinente != "" && Pertinente.First() == '/')
                Pertinente = Pertinente.Substring(1);

            if (Pertinente != "" && Pertinente.Last() == '/')
                Pertinente = Pertinente.Remove(Pertinente.Length - 1);

            Pertinente = Pertinente.Replace("/", "\\");

            if (Pertinente.Length > 5)
            {
                // SELECCION PERTINENTE //
                int CharPertPregunta = 0;
                int numPag = 0;
                string contenido;
                char[] delimiterChars = { '\\', '/' };
                List<List<List<int>>> rangos = new List<List<List<int>>>();

                foreach (Pagina pagina in db.Textos.Find(TextoID).Paginas)
                {
                    rangos.Add(new List<List<int>>());

                    contenido = pagina.Contenido;

                    contenido = HttpUtility.HtmlDecode(contenido);

                    contenido = RemoveBetween(contenido, '<', '>');

                    //contenido = contenido.Replace("&nbsp;", " ");

                    contenido = contenido.Replace((char)160, (char)32);
                    contenido = contenido.Replace(System.Environment.NewLine, " ");

                    string[] frases = Pertinente.Split(delimiterChars);

                    foreach (string frase in frases)
                    {
                        string selTmp;

                        selTmp = frase.Replace((char)160, (char)32);

                        if (contenido.IndexOf(selTmp) != -1)
                        {
                            rangos[rangos.Count - 1].Add(new List<int>());

                            int ini = contenido.IndexOf(selTmp);
                            int fin = ini + selTmp.Length;

                            rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(ini);
                            rangos[rangos.Count - 1][rangos[rangos.Count - 1].Count - 1].Add(fin);
                        }
                    }
                }

                List<List<List<int>>> nuevoRangos = new List<List<List<int>>>();

                List<int> unicoRango = new List<int>();
                int num = 0;

                foreach (Pagina pagina in db.Textos.Find(TextoID).Paginas)
                {
                    nuevoRangos.Add(new List<List<int>>());

                    contenido = pagina.Contenido;

                    contenido = HttpUtility.HtmlDecode(contenido);

                    contenido = RemoveBetween(contenido, '<', '>');

                    //contenido = contenido.Replace("&nbsp;", " ");
                    contenido = contenido.Replace(System.Environment.NewLine, " ");
                    contenido = contenido.Replace((char)160, (char)32);

                    string selTmp;

                    selTmp = NuevaSeleccion.Replace((char)160, (char)32);

                    if (contenido.IndexOf(selTmp) != -1)
                    {
                        num = nuevoRangos.Count - 1;
                        nuevoRangos[nuevoRangos.Count - 1].Add(new List<int>());

                        int ini = contenido.IndexOf(selTmp);
                        int fin = ini + selTmp.Length;

                        nuevoRangos[nuevoRangos.Count - 1][nuevoRangos[nuevoRangos.Count - 1].Count - 1].Add(ini);
                        nuevoRangos[nuevoRangos.Count - 1][nuevoRangos[nuevoRangos.Count - 1].Count - 1].Add(fin);
                        unicoRango.Add(ini);
                        unicoRango.Add(fin);
                    }
                }

                List<List<int>> CopiaRangos = new List<List<int>>();

                List<int> filaRangos = new List<int>();
                List<int> finRangos = new List<int>();

                foreach (List<int> rang in rangos[num])
                {
                    filaRangos.Add(rang[0]);
                    filaRangos.Add(rang[1]);
                }

                bool flag_ini = true;
                bool flag_agregado = false;
                int inicio = unicoRango[0];
                int final = unicoRango[1];
                int count = 0;

                foreach (int n in filaRangos)
                {
                    count++;

                    if (flag_agregado)
                    {
                        finRangos.Add(n);
                        continue;
                    }

                    if (flag_ini)
                    {
                        if (inicio < n)
                        {
                            flag_ini = false;

                            if (finRangos.Count % 2 == 0) // Par
                            {
                                finRangos.Add(inicio);

                                if (final < n)
                                {
                                    finRangos.Add(final);
                                    finRangos.Add(n);
                                    flag_agregado = true;
                                    continue; // Agregado
                                }
                            }
                            else
                            {
                                if (final < n)
                                {
                                    finRangos.Add(n);
                                    flag_agregado = true;
                                    continue; // Agregado
                                }
                            }
                        }
                        else
                        {
                            finRangos.Add(n);
                        }
                    }
                    else
                    {
                        if (final < n)
                        {
                            if ((count - 1) % 2 == 0) // Par
                            {
                                finRangos.Add(final);
                                finRangos.Add(n);
                                flag_agregado = true;
                                continue; // Agregado
                            }
                            else // Impar
                            {
                                finRangos.Add(n);
                                flag_agregado = true;
                                continue; // Agregado
                            }

                        }
                    }
                }

                if (!flag_agregado)
                {
                    if (finRangos.Count % 2 == 0) // Par
                    {
                        finRangos.Add(inicio);
                        finRangos.Add(final);
                    }
                    else
                    {
                        finRangos.Add(final);
                    }

                    flag_agregado = true;
                }

                string nuevaSeleccion = "";
                int numRang = 0;

                foreach (Pagina pagina in db.Textos.Find(TextoID).Paginas)
                {
                    contenido = pagina.Contenido;

                    contenido = HttpUtility.HtmlDecode(contenido);

                    contenido = RemoveBetween(contenido, '<', '>');

                    //contenido = contenido.Replace("&nbsp;", " ");
                    contenido = contenido.Replace(System.Environment.NewLine, " ");

                    contenido = contenido.Replace((char)160, (char)32);


                    if (numRang == num)
                    {
                        for (int z = 0; z < finRangos.Count; z++)
                        {
                            nuevaSeleccion += contenido.Substring(finRangos[z], finRangos[z + 1] - finRangos[z]) + "/";
                            z++;
                        }
                    }
                    else
                    {
                        foreach (List<int> r in rangos[numRang])
                        {
                            nuevaSeleccion += contenido.Substring(r[0], r[1] - r[0]) + "/";
                        }
                    }

                    numRang++;
                }

                nuevaSeleccion = nuevaSeleccion.Remove(nuevaSeleccion.Length - 1);

                NuevaSeleccion = nuevaSeleccion;

                return Json(new { result = NuevaSeleccion });
            }
            else
            {
                return Json(new { result = NuevaSeleccion });
            }



        }
         
        #endregion


        private DatosUsuario GetDatosUsuario()
        {
            logger.Debug("ReadAndLearnController/GetDatosUsuario");
            return db.DatosUsuario.Find(Session["DatosUsuarioID"]);
        }
        
        #region Reglas de Feedback
        public ReglaSimple getReglaS(int ReglaID)
        {
            return db.ReglasSimples.Find(ReglaID);
        }

        public ReglaCompleja getReglaC(int ReglaID)
        {
            return db.ReglasComplejas.Find(ReglaID);
        }

        public bool analisisReglaCompleja(int ReglaCID)
        {
            var Regla = db.ReglasComplejas.Find(ReglaCID);

            switch (Regla.OpCode)
            {
                case 1: // RS vs null
                    var Regla1 = getReglaS(Regla.Regla_1);

                    return analisisReglaSimple(Regla1.Variable, Regla1.Operador, Regla1.Parametro);
                case 2: // null vs RS
                    var Regla2 = getReglaS(Regla.Regla_2);

                    return analisisReglaSimple(Regla2.Variable, Regla2.Operador, Regla2.Parametro);
                case 3: // RC vs null
                    return analisisReglaCompleja(Regla.Regla_1);
                case 4: // null vs RC
                    return analisisReglaCompleja(Regla.Regla_2);
                case 5: // RS vs RS
                    var Regla5a = getReglaS(Regla.Regla_1);
                    var Regla5b = getReglaS(Regla.Regla_2);

                    return operadorLogico(analisisReglaSimple(Regla5a.Variable, Regla5a.Operador, Regla5a.Parametro), analisisReglaSimple(Regla5b.Variable, Regla5b.Operador, Regla5b.Parametro), Regla.Operador);
                case 6: // RS vs RC
                    var Regla6 = getReglaS(Regla.Regla_1);

                    return operadorLogico(analisisReglaSimple(Regla6.Variable, Regla6.Operador, Regla6.Parametro), analisisReglaCompleja(Regla.Regla_2), Regla.Operador);
                case 7: // RC vs RS
                    var Regla7 = getReglaS(Regla.Regla_2);

                    return operadorLogico(analisisReglaCompleja(Regla.Regla_1), analisisReglaSimple(Regla7.Variable, Regla7.Operador, Regla7.Parametro), Regla.Operador);
                case 8: // RC vs RC
                    return operadorLogico(analisisReglaCompleja(Regla.Regla_1), analisisReglaCompleja(Regla.Regla_2), Regla.Operador);
                default: // Others
                    return false;
            }
        }

        public bool analisisReglaSimple(int VarID, int OpS, double Param)
        {
            var regla = db.ReglasSimples.Find(VarID);
            var dato = getDato(VarID); // Para sacar el dato que queremos
                

            switch (OpS)
            {
                case 1: // ==
                    return dato == Param;
                case 2: // <=
                    return dato <= Param;
                case 3: // >=
                    return dato >= Param;
                case 4: // !=
                    return dato != Param;
                case 5: // <
                    return dato < Param;
                case 6: // >
                    return dato > Param;
                default:
                    return false;
            }
        }

        public int getDato(int i)
        {
            return 2;
        }

        private bool operadorLogico(bool V1, bool V2, int Op)
        {
            switch (Op)
            {
                case 1:
                    return V1 && V2;
                case 2:
                    return V1 || V2;
                default:
                    return false;
            }
        }
        #endregion


        
    }
}

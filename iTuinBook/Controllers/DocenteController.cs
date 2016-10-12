using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ReadAndLearn.Models;
using System.Data;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Globalization;
using NLog;

namespace ReadAndLearn.Controllers
{
    public class DocenteController : Controller
    {
        Contexto db = new Contexto();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        ExternalMethods ext = new ExternalMethods();

        //
        // GET: /Docente/

        public ActionResult Index()
        {
            List<Solicitud> Solicitudes = new List<Solicitud>();

            UserProfile user = getCurrentUser();

            var inscripciones = (from u in db.UserProfiles
                              from g in db.Grupos
                              from p in g.Propietarios                              
                              from i in db.Inscripciones
                              where p.UserId == user.UserId &&
                              i.Aceptado == false &&
                              i.GrupoID == g.GrupoID
                              select i).Distinct();
                      
            foreach (Inscripcion ins in inscripciones)
            {   
                Solicitudes.Add(new Solicitud(ins.InscripcionID, db.UserProfiles.Find(ins.UserId), db.Grupos.Find(ins.GrupoID)));
            }
      
            return View(Solicitudes);
        }
        #region Funciones Alumnos
        public ActionResult Alumnos()
        {
            var user = getCurrentUser();
            
            var grupos = from g in db.Grupos
                         from d in g.Propietarios
                         where d.UserId == user.UserId
                         select g;

            var alumnos = (from a in db.UserProfiles
                            from i in db.Inscripciones
                            from g in grupos                            
                            where a.UserId == i.UserId &&
                                i.GrupoID == g.GrupoID
                            select a).Distinct();

            return View(alumnos);
        }

        public ActionResult AceptarSolicitud(int InscripcionID)
        {
            db.Inscripciones.Find(InscripcionID).Aceptado = true;

            db.SaveChanges();       

            return RedirectToAction("Index");
        }

        public ActionResult RechazarSolicitud(int InscripcionID)
        {
            db.Inscripciones.Remove(db.Inscripciones.Find(InscripcionID));
            
            db.SaveChanges();

            return RedirectToAction("Index");
        }
        #endregion

        #region Funciones Módulos
        public ActionResult Modulos()
        {  
            var user = getCurrentUser();

            var modulos = (from m in db.Modulos
                           where m.Propiedad == user.UserId
                           select m).Distinct();

            var modAdquiridos = from m in db.Modulos
                                from p in m.Propietarios
                                where p.UserId == user.UserId &&
                                m.Propiedad != user.UserId
                                select m;
   
            var todos = (from m in db.Modulos
                        from p in m.Propietarios
                        where p.UserId != user.UserId &&
                        m.Publico == true
                        select m).Distinct();

            ViewBag.TodosModulos = todos;
            ViewBag.Adquiridos = modAdquiridos;

            return View(modulos);
        }

        public ActionResult AdquirirModulo(int ModuloID)
        {
            UserProfile user = getCurrentUser();
            Modulo modulo = db.Modulos.Find(ModuloID);

            user.Modulos.Add(modulo);
            modulo.Propietarios.Add(user);

            db.SaveChanges();

            return RedirectToAction("Modulos");
        }

        public ActionResult CrearModulo(int? GrupoID)
        {
            ViewBag.Tipos = GetTipos();

            if (GrupoID != null)
            {
                ViewBag.Grupo = db.Grupos.Find(GrupoID);
            }

            return View();
        }

        [HttpPost]
        public ActionResult CrearModulo(Modulo modulo, int? GrupoID)
        {
            ViewBag.Tipos = GetTipos();
            UserProfile user = getCurrentUser();
            modulo.Propietarios = new List<UserProfile>();
            ConfigModulo config = new ConfigModulo();

            //guirisan/secuencias
            //ver1.0: los grupos de un modulo se referencian desde una icollection en la clase modulo
            
            // Si viene de la creación de un grupo, se agrega automáticamente al grupo
            if (GrupoID != null)
            {
                modulo.Grupos = new List<Grupo>();
                modulo.Grupos.Add(db.Grupos.Find(GrupoID));               
            }
            
            //ver2.0: ahora se referenciana además a través de la clase intermedia GrupoModulo
            GrupoModulo gm = new GrupoModulo();

            if (GrupoID != null)
            {
                gm.Grupo = db.Grupos.Find(GrupoID);
                gm.GrupoID = (int)GrupoID;
            
                gm.Modulo = modulo;
                gm.ModuloID = modulo.ModuloID;
                gm.Orden = db.Grupos.Find(GrupoID).GrupoModulo.Count() + 1;
                db.GrupoModulo.Add(gm);
            }
            //end guirisan/secuencias


            modulo.Propietarios.Add(user);
            modulo.Propiedad = user.UserId;
            user.Modulos.Add(modulo);
            db.Modulos.Add(modulo);
            
            db.SaveChanges();
            
            config.ModuloID = modulo.ModuloID;
            config.Modulo = modulo;
            config.Plantilla = 1;

            db.ConfigModulo.Add(config);

            db.Modulos.Find(modulo.ModuloID).ConfigModulo = config;

            db.SaveChanges();

            return RedirectToAction("AdministrarModulo", new { GrupoID = GrupoID, ModuloID = modulo.ModuloID });
        }

        public ActionResult AdministrarModulo(int ModuloID, int? GrupoID)
        {
            var user = getCurrentUser();

            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;
            List<SelectListItem> TipoTimings = new List<SelectListItem>();

            TipoTimings.Add(new SelectListItem { Text = "Simple / Acumulativo", Value = "0" });
            TipoTimings.Add(new SelectListItem { Text = "Complejo / Desarrollado", Value = "1" });

            ViewBag.TipoTimings = TipoTimings;

            return View(db.Modulos.Find(ModuloID));
        }

        public ActionResult EditarModulo(int ModuloID, int? GrupoID)
        {
            ViewBag.GrupoID = GrupoID;

            return View(db.Modulos.Find(ModuloID));
        }

        [HttpPost]
        public ActionResult EditarModulo(Modulo updateModulo)
        {
            if (ModelState.IsValid)
            {
                UserProfile user = getCurrentUser();
                updateModulo.Propietarios = new List<UserProfile>();

                updateModulo.Propietarios.Add(user);
                updateModulo.Propiedad = user.UserId;

                this.db.Entry(updateModulo).State = EntityState.Modified;
                this.db.SaveChanges();

                return this.RedirectToAction("AdministrarModulo", new { ModuloID = updateModulo.ModuloID });
            }

            return View(updateModulo);
        }

        public ActionResult ConfigurarModulo(int ModuloID)
        {
            if (db.Modulos.Find(ModuloID).ConfigModulo == null)
            {
                ConfigModulo config = new ConfigModulo();

                config.ModuloID = ModuloID;
                config.Modulo = db.Modulos.Find(ModuloID);

                db.ConfigModulo.Add(config);

                db.Modulos.Find(ModuloID).ConfigModulo = config;

                db.SaveChanges();
            }

            return View(db.Modulos.Find(ModuloID).ConfigModulo);
        }

        [HttpPost]
        public ActionResult ConfigurarModulo(ConfigModulo Config, int ModuloID)
        {
            if (ModelState.IsValid)
            {
                db.Entry(Config).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("AdministrarModulo", new { ModuloID = ModuloID });
            }

            return View(Config);            
        }
        
        public ActionResult ConfigurarGrupo(int GrupoID)
        {
            if (db.Grupos.Find(GrupoID).ConfigGrupo == null)
            {
                ConfigGrupo config = new ConfigGrupo();

                config.GrupoID = GrupoID;
                config.Grupo = db.Grupos.Find(GrupoID);

                db.ConfigGrupo.Add(config);

                db.Grupos.Find(GrupoID).ConfigGrupo = config;

                db.SaveChanges();
            }

            return View(db.Grupos.Find(GrupoID).ConfigGrupo);
        }

        [HttpPost]
        public ActionResult ConfigurarGrupo(ConfigGrupo Config, int GrupoID)
        {
            if (ModelState.IsValid)
            {
                db.Entry(Config).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("AdministrarGrupo", new { GrupoID = GrupoID });
            }

            return View(Config);
        }
        
        #endregion

        #region Funciones Texto - Páginas - Preguntas - Ayudas
        public ActionResult AgregarImagen()
        {
            var user = getCurrentUser();
            
            ViewBag.Imagenes = user.Imagenes;            
            
            return View();
        }

        struct prueba
        {
            public string ID;
            public string Name;
            public string file;            
        };
        
        [HttpPost]
        public ActionResult Imagenes()
        {
            var user = getCurrentUser();
            List<prueba> listImagen = new List<prueba>();

            foreach (Imagenes img in user.Imagenes)
            {
                listImagen.Add(new prueba { ID = img.ImagenesID.ToString(), Name = img.Nombre, file = Convert.ToBase64String(img.file) });
            }
            
            return Json(listImagen);//, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EditarTexto(int TextoID)
        {
            return View(db.Textos.Find(TextoID));
        }

        [HttpPost]
        public ActionResult EditarTexto(Texto texto, int TextoID)
        {
            if (ModelState.IsValid)
            {
                this.db.Entry(texto).State = EntityState.Modified;
                this.db.SaveChanges();

                return RedirectToAction("AdministrarTexto", new { TextoID = TextoID }); 
            }

            return View();
        }

        public ActionResult AgregarRegionImagen(string file)
        {
            ViewBag.file = file;

            return View();
        }

        [HttpPost]
        public ActionResult AgregarImagen(Fichero imagen, EventArgs e)
        {
            var user = getCurrentUser();

            //Check server side validation using data annotation
            if (ModelState.IsValid)
            {
                Imagenes nueva = new Imagenes();
                
                MemoryStream target = new MemoryStream();                
                imagen.file.InputStream.CopyTo(target);
                byte[] data = target.ToArray();

                nueva.file = data;
                nueva.UserProfile = user;                
                nueva.Nombre = imagen.Nombre;
                db.Imagenes.Add(nueva);
                user.Imagenes.Add(nueva);

                db.SaveChanges();

                ViewBag.Message = "La imagen ha sido cargada correctamente.";
                ModelState.Clear();
            }

            ViewBag.Imagenes = user.Imagenes;

            return View();
        }

        public ActionResult AdministrarTexto(int TextoID, int? GrupoID, int? ModuloID)
        {
            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;

            return View(db.Textos.Find(TextoID));
        }

        public ActionResult CrearTexto(int? GrupoID, int? ModuloID)
        {
            if(GrupoID != null)
            {
                ViewBag.Grupo = db.Grupos.Find(GrupoID);
            }

            if (ModuloID != null)
            {
                ViewBag.Modulo = db.Modulos.Find(ModuloID);
            }

            return View();
        }

        [HttpPost]
        public ActionResult CrearTexto(Texto nuevoTexto, int? GrupoID, int? ModuloID)
        {
            if(ModuloID != null)
            {
                nuevoTexto.Modulos = new List<Modulo>();
                nuevoTexto.Modulos.Add(db.Modulos.Find(ModuloID));

                //guirisan/issue https://github.com/guirisan/ituinbook/issues/20
                //Se ha comentado el siguiente bloque de código porque al exigir valor para Orden en el formulario
                //y ser este numérico no hay que contemplar los casos en que no se declare el orden
                /*
                if (nuevoTexto.Orden == null)
                {
                    int length = db.Modulos.Find(ModuloID).Textos.Count();
                    nuevoTexto.Orden = length + 1;
                }
                 */
            }

            db.Textos.Add(nuevoTexto);

            db.SaveChanges();

            ConfigTexto config = new ConfigTexto();

            config.TextoID = nuevoTexto.TextoID;
            config.Texto = db.Textos.Find(nuevoTexto.TextoID);

            db.ConfigTexto.Add(config);

            db.Textos.Find(nuevoTexto.TextoID).ConfigTexto = config;

            db.SaveChanges();

            return RedirectToAction("CrearPagina", new { GrupoID = GrupoID, ModuloID = ModuloID, TextoID = nuevoTexto.TextoID });
        }
        
        public ActionResult EliminarTexto(int TextoID, int ModuloID)
        {
            Texto text = db.Textos.Find(TextoID);

            text.Preguntas.Clear();

            db.Textos.Remove(text);
            
            db.SaveChanges();

            return RedirectToAction("AdministrarModulo", new { ModuloID = ModuloID });            
        }

        public ActionResult EliminarModulo(int ModuloID)
        {
            db.Modulos.Remove(db.Modulos.Find(ModuloID));

            db.SaveChanges();
            
            return RedirectToAction("Modulos");            
        }

      

        public ActionResult ConfigurarTexto(int TextoID)
        {
            if (db.Textos.Find(TextoID).ConfigTexto == null)
            {
                ConfigTexto config = new ConfigTexto();
                
                config.TextoID = TextoID;
                config.Texto = db.Textos.Find(TextoID);

                db.ConfigTexto.Add(config);

                db.Textos.Find(TextoID).ConfigTexto = config;

                db.SaveChanges();
            }

            return View(db.Textos.Find(TextoID).ConfigTexto);
        }

        [HttpPost]
        public ActionResult ConfigurarTexto(ConfigTexto Config, int TextoID)
        {
            if (ModelState.IsValid)
            {
                db.Entry(Config).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("AdministrarTexto", new { TextoID = TextoID });
            }            

            return View(Config);
        }

        public ActionResult ConfigurarPregunta(int PreguntaID)
        {
            var pregunta = db.Preguntas.Find(PreguntaID);

            IEnumerable<ConfigPregunta> listaConfig = from c in db.ConfigPregunta
                         where c.PreguntaID == PreguntaID
                         select c;

            ConfigPregunta config = null;

            if(listaConfig.Count() > 0)
                config = listaConfig.First();

            if (config == null)
            {
                config = new ConfigPregunta();

                config.PreguntaID = PreguntaID;
                config.Pregunta = db.Preguntas.Find(PreguntaID);

                pregunta.ConfigPregunta = config;
                db.ConfigPregunta.Add(config);

                db.SaveChanges();
            }

            return View(db.Preguntas.Find(PreguntaID).ConfigPregunta);
        }

        [HttpPost]
        public ActionResult ConfigurarPregunta(ConfigPregunta Config, int PreguntaID)
        {
            if (ModelState.IsValid)
            {
                Config.PreguntaID = PreguntaID;

                db.Entry(Config).State = EntityState.Modified;                
                db.Preguntas.Find(PreguntaID).ConfigPregunta = Config;                
                db.SaveChanges();

                return RedirectToAction("AdministrarPregunta", new { PreguntaID = PreguntaID });
            }

            return View(Config);
        }


        public ActionResult CrearPagina(int? GrupoID, int? ModuloID, int TextoID)
        {
            if (GrupoID != null)
            {
                ViewBag.Grupo = db.Grupos.Find(GrupoID);
            }

            if (ModuloID != null)
            {
                ViewBag.Modulo = db.Modulos.Find(ModuloID);
            }

            ViewBag.Texto = db.Textos.Find(TextoID);

            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult CrearPagina(Pagina nuevaPagina, int TextoID, string txt)
        {
            if (ModelState.IsValid)
            {
                nuevaPagina.TextoID = TextoID;
                nuevaPagina.Texto = db.Textos.Find(TextoID);

                //guirisan/issue https://github.com/guirisan/ituinbook/issues/20
                /* Comentado por la misma razón que el orden del texto, ya no es necesario al requerir
                 * valor para el campo Orden en el formulario*/
                //int length = db.Textos.Find(TextoID).Paginas.Count();
                //nuevaPagina.Orden = length + 1;

                nuevaPagina.Contenido = HttpUtility.HtmlDecode(nuevaPagina.Contenido);
                
                
                //guirisan/issue https://github.com/guirisan/ituinbook/issues/79
                //indexación de las palabras en el texto para el correcto funcionamiento
                //de la tarea de selección
                nuevaPagina.Contenido = textIndexation(nuevaPagina.Contenido);
                
                
                db.Paginas.Add(nuevaPagina);

                db.SaveChanges();

                ViewBag.Texto = db.Textos.Find(TextoID);

                return RedirectToAction("AdministrarTexto", new { TextoID = TextoID });
            }

           
            return View(nuevaPagina);
        }

        /** textIndexation(string source)
         *  Dado un texto con etiquetas html (el contenido de la página a insertar / modificar), esta función asigna
         *  un índice a cada palabra o número de 1 a n, encerrando cada palabra a indizar entre etiquetas span del tipo
         *  <span data-windex="1">palabraX</span>  <span data-windex="2">palabraY</span> 
         *  - los números se consideran palabras y tienen su propio índice
         *  - una palabra compuesta (l'atmosfera) se considera una única palabra
         *  - los signos de puntuació no computan para la indexación. El punto, coma, punto y coma, dos puntos, etc., 
         *    
         * 
         */
        public string textIndexation(string source)
        {
            /***************************************************
             *           DOTNETFIDDLE.NET                      *
             ***************************************************/

            /*****variables del algoritmo*/
            int pos = 0;            //posición del texto actual (de 0 a text.Length);
            string result = "";     //variable para almacenar el resultado a devolver
            string aux = "";        //variable auxiliar para almacenar caracteres de una misma etiqueta html, palabra, etc
            bool endhtmlflag = false;   //variable para indicar si fin o no de la etiqueta html
            bool endwordflag = false;   //variable para indicar el fin de la palabra (a la que asignamos índice)
            int windex = 1;         //variable para asignar índices a las palabras del texto como atributos de tag span (usar atributo data-windex)
            Regex alphanumericregexp = new Regex(@"[a-zA-Z0-9áéíóúñ]");     //regexp para ver si un caracter es alfanumérico o no
            Regex endwordregexp = new Regex(@"[\s.:,;&\(\)\[\]\\\/\-_¿\?¡\!]");  //regexp para ver si un caracter es un signo de puntuación que indique el fin de palabra


            //preparación de source para su parseo
            source = source.Trim(); //eliminación de espacios en blanco al principio y final

            //replace &nbsp; with " " (blankspace)
            source = source.Replace("&nbsp;", " ");

            //source = source.RemoveFuckingWordLabels(); //pendiente, buscar función que elimine información estupida de word?
            
            char[] text = source.ToCharArray(); //¿arraycialización? del string recibido como parámetro en char[] array

            while (pos < text.Length)
            {
                //System.Diagnostics.Debug.Write("MAIN while -> pos=" + pos + " - text(pos)=" + text[pos]);

                /************** START HTML TAG****************/
                if (text[pos].ToString().CompareTo("<") == 0)  
                {
                    System.Diagnostics.Debug.Write("start html -> pos=" + pos + " - text(pos)=" + text[pos]);
                    endhtmlflag = false;

                    aux = "";
                    aux += text[pos];
                    pos++;
                    while (!endhtmlflag)
                    {
                        if (text[pos].ToString().CompareTo(">") == 0)
                        {
                            endhtmlflag = true;
                        }
                        aux += text[pos];
                        pos++;
                    }
                    //System.Diagnostics.Debug.Write("end html -> pos=" + pos + " - text(pos)=" + text[pos]);
                    result += aux;                              //end html tag ">"
                }

                /************** BLANK SPACE **************/ 
                else if (text[pos].ToString().CompareTo(" ") == 0)                 //blankspace
                {
                    System.Diagnostics.Debug.Write("blankspace -> pos=" + pos);
                    result += " ";
                    pos++;
                }

                /************** SALTO DE LÍNEA *************/
                else if (text[pos].ToString().CompareTo("\\") == 0)               //breakline \r o \n o \r\n
                {
                    System.Diagnostics.Debug.Write("breaklline -> pos=" + pos + " - text(pos)=" + text[pos]);
                    pos++;
                    result += "\\" + text[pos];
                    pos++;
                }

                /************** PALABRA (ALFANUMÉRICO) *******/
                else if (alphanumericregexp.IsMatch(text[pos].ToString())) //alphanumeric start (palabra, o número)
                {
                    System.Diagnostics.Debug.Write("start alphanumeric -> pos=" + pos + " - text(pos)=" + text[pos]);
                    endwordflag = false;
                    aux = "<span data-windex='" + windex++ + "'>";
                    aux += text[pos++];
                    bool endtextflag = false; //fix this shit. declaracions dins del bucle???
                    while (!endwordflag && !endtextflag)
                    {
                        //System.Diagnostics.Debug.Write("CONTINUE alphanumeric -> pos=" + pos + " - text(pos)=" + text[pos]);
                        if (pos >= text.Length)
                        {
                            endtextflag = true;
                        }
                        else if (text[pos].ToString().CompareTo("&") == 0)
                        {
                            //el caracter es un & de acento
                            while (text[pos].ToString().CompareTo(";") != 0)
                            {
                                aux += text[pos++];
                            }
                            aux += text[pos++];
                        }
                        else if (!alphanumericregexp.IsMatch(text[pos].ToString()))
                        {
                            //el caracter ya no es alphanumeric
                            endwordflag = true;
                            aux += "</span>";
                            //hay que incluir text[pos] en el resultado o se pierde.
                            ///////////////////   o no?    /////////////////////////
                            //si borramos el pos++...
                        }                        
                        else
                        {
                            //el caracter es alfanumérico
                            aux += text[pos++];
                        }

                    }
                    //System.Diagnostics.Debug.Write("********************end alphanumeric -> pos=" + pos + " - text(pos)=" + text[pos]);
                    result += aux;

                }
                else if (endwordregexp.IsMatch(text[pos].ToString()))		//signo de puntuación
                {

                    result += text[pos++];

                }
                else
                {
                    //ERROR
                    System.Diagnostics.Debug.Write("else NOT CAPTURED -> pos=" + pos + " - text(pos)=[ " + text[pos] + " ]");

                }
            }

            return result;
        }


        public ActionResult EliminarPregunta(int TextoID, int PreguntaID)
        {
            Pregunta preg = db.Preguntas.Find(PreguntaID);
            Texto texto = db.Textos.Find(TextoID);

            texto.Preguntas.Remove(preg);
            db.Preguntas.Remove(preg);

            db.SaveChanges();

            return RedirectToAction("AdministrarTexto", new { TextoID = TextoID });
        }

        public ActionResult EditarPagina(int? GrupoID, int? ModuloID, int TextoID, int PaginaID)
        {
            if (GrupoID != null)
            {
                ViewBag.Grupo = db.Grupos.Find(GrupoID);
            }

            if (ModuloID != null)
            {
                ViewBag.Modulo = db.Modulos.Find(ModuloID);
            }

            ViewBag.Texto = db.Textos.Find(TextoID);

            return View(db.Paginas.Find(PaginaID));
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditarPagina(Pagina nuevaPagina, int TextoID, int PaginaID)
        {
            if (ModelState.IsValid)
            {
                nuevaPagina.Contenido = HttpUtility.HtmlDecode(nuevaPagina.Contenido);

                //guirisan/issue https://github.com/guirisan/ituinbook/issues/79
                //indexación de las palabras en el texto para el correcto funcionamiento
                //de la tarea de selección
                //*****************************************************
                //********************IMPOSIBLE**********************
                //REASIGNAR los valores data-windex después de eliminar o añadir palabras, es básicamente imposible.
                //opción de eliminar la opción de editar las páginas a cambio de eliminar la vieja y crear una nueva.
                //nuevaPagina.Contenido = textIndexation(nuevaPagina.Contenido);

                this.db.Entry(nuevaPagina).State = EntityState.Modified;
                this.db.SaveChanges();
            }

            ViewBag.Texto = db.Textos.Find(TextoID);

            return RedirectToAction("EditarPagina", new { TextoID = TextoID, PaginaID = PaginaID });            
        }
        public ActionResult VerPagina(int TextoID, int PaginaID)
        {
            Texto texto = db.Textos.Find(TextoID);
            Match cierre;

            Regex rx = new Regex(@"<h\d{1}>");
            Regex rxf = new Regex(@"</h\d{1}>");
            IList<Indice> indeces = new List<Indice>();
            string contenido = "";

            foreach(Pagina pag in texto.Paginas)
            {
                foreach (Match apertura in rx.Matches(pag.Contenido))
                {
                    contenido = "";
                           
                    cierre = rxf.Match(pag.Contenido, apertura.Index);

                    contenido = pag.Contenido.Substring(apertura.Index + 4 , cierre.Index - apertura.Index - 4);

                    indeces.Add(new Indice() { Contenido = contenido, PaginaID = pag.PaginaID, Nivel = Convert.ToInt32(apertura.Value[2].ToString()) });
                }
            }

            ViewBag.Indices = indeces;
            ViewBag.TextoID = TextoID;

            return View(db.Paginas.Find(PaginaID));        
        }

        public ActionResult EliminarPagina(int TextoID, int PaginaID)
        {
            db.Paginas.Remove(db.Paginas.Find(PaginaID));

            db.SaveChanges();

            return RedirectToAction("AdministrarTexto", new { TextoID = TextoID });            
        }

        public ActionResult VisualizarPagina(int? TextoID, int? PaginaID)
        {   
            ViewBag.Texto = db.Textos.Find(TextoID);

            if (PaginaID == null)
            {
                if (db.Textos.Find(TextoID).Paginas.Count() > 0)
                    ViewBag.Pagina = db.Textos.Find(TextoID).Paginas.First();
            }
            else
            {
                ViewBag.Pagina = db.Paginas.Find(PaginaID);
            }

            return View();
        }

        public ActionResult CrearTarea(int TextoID)
        {
            ViewBag.Texto = db.Textos.Find(TextoID);

            return View();
        }

        [HttpPost]
        public ActionResult CrearTarea(TareaOrdenar TareaOrdenar, int TextoID)
        {
            TareaOrdenar.Texto = db.Textos.Find(TextoID);
            TareaOrdenar.TextoID = TextoID;
            
            TareaOrdenar.Ordenados = new string[TareaOrdenar.Num];

            

            db.TareasOrdenar.Add(TareaOrdenar);

            db.SaveChanges();

            return RedirectToAction("AgregarItemsOrdenar", new { TareaOrdenarID = TareaOrdenar.TareaOrdenarID });
        }

        public ActionResult EditarTareaOrdenar(int TextoID, int TareaOrdenarID)
        {
            return View(db.TareasOrdenar.Find(TareaOrdenarID));
        }

        [HttpPost]
        public ActionResult EditarTareaOrdenar(TareaOrdenar tareaOrdenar, int TextoID, int TareaOrdenarID)
        {
            if (ModelState.IsValid)
            {
                this.db.Entry(tareaOrdenar).State = EntityState.Modified;
                this.db.SaveChanges();

                return RedirectToAction("AdministrarTexto", new { TextoID = TextoID });
            }

            return View(tareaOrdenar);
        }

        public ViewResult VerTareaOrdenar(int TareaOrdenarID)
        {
            TareaOrdenar tareaOrdenar = db.TareasOrdenar.Find(TareaOrdenarID);

            return View(tareaOrdenar);
        }

        public ActionResult EliminarTareaOrdenar(int TareaOrdenarID)
        {
            TareaOrdenar tarea = db.TareasOrdenar.Find(TareaOrdenarID);

            db.TareasOrdenar.Remove(tarea);

            db.SaveChanges();

            int TextoID = tarea.TextoID;

            return RedirectToAction("AdministrarTexto", new { TextoID = TextoID });
        }

        public ActionResult AgregarItemsOrdenar(int TareaOrdenarID)
        {
            TareaOrdenar tareaOrdenar = db.TareasOrdenar.Find(TareaOrdenarID);

            return View(tareaOrdenar);
        }

        [HttpPost]
        public ActionResult AgregarItemsOrdenar(TareaOrdenar tareaOrdenar)
        {
            if (ModelState.IsValid)
            {
                
                Items item = new Items();
                int i = 0;

                foreach (string str in tareaOrdenar.Ordenados)
                {
                    i = 1;

                    foreach (string str2 in tareaOrdenar.Orden)
                    {
                        if (str2 == str)
                        {
                            item = new Items();

                            item.Item = str;

                            item.TareaOrdenar = tareaOrdenar;
                            item.TareaOrdenarID = tareaOrdenar.TareaOrdenarID;
                            item.Order = i;

                            db.Items.Add(item);

                            db.SaveChanges();
                            tareaOrdenar.ItemsOrdenados.Add(item);
                        }

                        i++;
                    }
                }

                tareaOrdenar.Texto = db.Textos.Find(tareaOrdenar.TextoID);

                this.db.Entry(tareaOrdenar).State = EntityState.Modified;
                this.db.SaveChanges();

                return RedirectToAction("AdministrarTexto", new { TextoID = tareaOrdenar.TextoID });
            }

            return View(tareaOrdenar);
        }


        public ActionResult CrearPregunta(int TextoID, int? PaginaID)
        {
            ViewBag.Tipos = GetTiposPreguntas();
            ViewBag.Dificultad = new int[] { 1, 2, 3, 4, 5 };

            if (PaginaID != null)
            {
                ViewBag.Pagina = db.Paginas.Find(PaginaID);
                ViewBag.Texto = db.Textos.Find(TextoID);
            }
            else
            {
                ViewBag.Texto = db.Textos.Find(TextoID);
            }

            return View();
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult CrearPregunta(Pregunta nuevaPregunta, int TextoID, int? PaginaID)
        {
            ConfigPregunta config = new ConfigPregunta();

            nuevaPregunta.Texto = db.Textos.Find(TextoID);
            nuevaPregunta.SubPreguntas = new List<SubPregunta>();

            //guirisan/secuencias https://github.com/guirisan/ituinbook/issues/20
            /* Comentado por la obligatoriedad del campo Orden en el formulario de creación de pregunta*/
            //int length = db.Textos.Find(TextoID).Preguntas.Count();
            //nuevaPregunta.Orden = length + 1;

            db.Preguntas.Add(nuevaPregunta);

            db.SaveChanges();

            config.PreguntaID = nuevaPregunta.PreguntaID;
            config.Pregunta = db.Preguntas.Find(nuevaPregunta.PreguntaID);

            nuevaPregunta.ConfigPregunta = config;
            db.ConfigPregunta.Add(config);

            db.SaveChanges();

            switch (nuevaPregunta.TipoPreguntaID)
            { 
                case 1:
                    return RedirectToAction("AgregarAlternativa", new { TextoID = TextoID, PreguntaID = nuevaPregunta.PreguntaID });                   
                case 2:
                    return RedirectToAction("AgregarCriterioCorreccion", new { TextoID = TextoID, PreguntaID = nuevaPregunta.PreguntaID });                   
                case 3:
                    return RedirectToAction("AdministrarPregunta", new { TextoID = TextoID, PreguntaID = nuevaPregunta.PreguntaID });                                      
                case 4:
                    return RedirectToAction("AgregarEmparejados", new { TextoID = TextoID, PreguntaID = nuevaPregunta.PreguntaID });                                      
                default:
                    return RedirectToAction("AdministrarTexto", new { TextoID = TextoID });
            }
            
        }

        public ActionResult EditarPregunta(int PreguntaID, int TextoID)
        {
            ViewBag.Tipos = GetTiposPreguntas();
            ViewBag.Dificultad = new int[] { 1, 2, 3, 4, 5 };

            ViewBag.Texto = db.Textos.Find(TextoID);

            return View(db.Preguntas.Find(PreguntaID));
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult EditarPregunta(Pregunta pregunta, int PreguntaID, int TextoID)
        {
            ViewBag.Tipos = GetTiposPreguntas();
            ViewBag.Dificultad = new int[] { 1, 2, 3, 4, 5 };

            ViewBag.Texto = db.Textos.Find(TextoID);

            if (ModelState.IsValid)
            {
                this.db.Entry(pregunta).State = EntityState.Modified;
                this.db.SaveChanges();

                return RedirectToAction("AdministrarTexto", new { TextoID = TextoID });
            }

            return View(pregunta);
        }

        public ActionResult AgregarEmparejados(int PreguntaID)
        {
            ViewBag.Pregunta = db.Preguntas.Find(PreguntaID);

            return View();
        }

        [HttpPost]
        public ActionResult AgregarEmparejados(Emparejado emparejado, int PreguntaID)
        {
            Pregunta preg = db.Preguntas.Find(PreguntaID);

            emparejado.Pregunta = preg;
            emparejado.PreguntaID = preg.PreguntaID;

            db.Emparejados.Add(emparejado);

            preg.Emparejados.Add(emparejado);

            db.SaveChanges();

            ViewBag.Pregunta = preg;

            return View();
        }

        public ActionResult EliminarEmparejado(int EmparejadoID, int PreguntaID)
        {
            db.Preguntas.Find(PreguntaID).Emparejados.Remove(db.Emparejados.Find(EmparejadoID));

            db.Emparejados.Remove(db.Emparejados.Find(EmparejadoID));

            db.SaveChanges();

            return RedirectToAction("AgregarEmparejados", new { PreguntaID = PreguntaID });                                                  
        }

        public ActionResult AgregarAyudas(int PreguntaID)
        {
            ViewBag.Texto = db.Preguntas.Find(PreguntaID).Texto;                          

            return View(db.Preguntas.Find(PreguntaID).Ayuda);
        }

        [HttpPost]
        public ActionResult AgregarAyudas(Ayuda ayudas, int PreguntaID)
        {
            if (db.Preguntas.Find(PreguntaID).Ayuda != null)
            {
                db.Ayudas.Remove(db.Ayudas.Find(db.Preguntas.Find(PreguntaID).Ayuda.AyudaID));    
            }            

            db.Preguntas.Find(PreguntaID).Ayuda = ayudas;
            db.Ayudas.Add(ayudas);
            
            db.SaveChanges();

            return RedirectToAction("AdministrarPregunta", new { PreguntaID = PreguntaID });                                      
        }

        public ActionResult AgregarSubpregunta(int PreguntaID)
        {
            ViewBag.PreguntaID = PreguntaID;

            return View();
        }

        [HttpPost]
        public ActionResult AgregarSubpregunta(SubPregunta subpregunta, int PreguntaID)
        {
            Pregunta preg = db.Preguntas.Find(PreguntaID);

            preg.SubPreguntas.Add(subpregunta);
            db.SubPreguntas.Add(subpregunta);

            db.SaveChanges();
           
            return RedirectToAction("AgregarSubAlternativa", new { PreguntaID = PreguntaID, SubPreguntaID = subpregunta.SubPreguntaID });                                                  
        }

        public ActionResult EliminarSubPregunta(int PreguntaID, int SubPreguntaID)
        {
            SubPregunta subpreg = db.SubPreguntas.Find(SubPreguntaID);
            Pregunta preg = db.Preguntas.Find(PreguntaID);

            preg.SubPreguntas.Remove(subpreg);
            db.SubPreguntas.Remove(subpreg);

            db.SaveChanges();

            return RedirectToAction("AdministrarPregunta", new { PreguntaID = PreguntaID });
        }

        public ActionResult AgregarSubAlternativa(int PreguntaID, int SubPreguntaID)
        {
            SubPregunta subpreg = db.SubPreguntas.Find(SubPreguntaID);

            ViewBag.SubPregunta = subpreg;
            ViewBag.PreguntaID = PreguntaID;

            return View();
        }

        [HttpPost]
        public ActionResult AgregarSubAlternativa(SubAlternativa subalternativa, int PreguntaID, int SubPreguntaID)
        {
            SubPregunta subpreg = db.SubPreguntas.Find(SubPreguntaID);

            subpreg.SubAlternativas.Add(subalternativa);

            db.SubAlternativas.Add(subalternativa);

            db.SaveChanges();

            return RedirectToAction("AgregarSubAlternativa", new { SubPreguntaID = SubPreguntaID });
        }

        public ActionResult EliminarSubAlternativa(int SubPreguntaID, int SubAlternativaID)
        {
            db.SubPreguntas.Find(SubPreguntaID).SubAlternativas.Remove(db.SubAlternativas.Find(SubAlternativaID));

            db.SubAlternativas.Remove(db.SubAlternativas.Find(SubAlternativaID));

            db.SaveChanges();

            return RedirectToAction("AgregarSubAlternativa", new { SubPreguntaID = SubPreguntaID });
        }       

        public ActionResult SimularPregunta(int PreguntaID)
        {
            ViewBag.Pregunta = db.Preguntas.Find(PreguntaID);
            return View();
        }

        public ActionResult AdministrarPregunta(int PreguntaID)
        {
            return View(db.Preguntas.Find(PreguntaID));
        }

        public ActionResult AgregarAlternativa(int? TextoID, int? PaginaID, int PreguntaID)
        {
            ViewBag.Pregunta = db.Preguntas.Find(PreguntaID);

            return View();
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult AgregarAlternativa(Alternativa nuevaAltenativa, int? TextoID, int? PaginaID, int PreguntaID)
        {
            ViewBag.Pregunta = db.Preguntas.Find(PreguntaID);

            db.Preguntas.Find(PreguntaID).Alternativas.Add(nuevaAltenativa);

            db.SaveChanges();

            return RedirectToAction("AgregarAlternativa", new { TextoID = TextoID, PaginaID = PaginaID, PreguntaID = PreguntaID });              
        }

        public ActionResult EditarAlternativa(int PreguntaID, int AlternativaID)
        {
            ViewBag.PreguntaID = PreguntaID;

            return View(db.Alternativas.Find(AlternativaID));
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult EditarAlternativa(Alternativa update, int PreguntaID, int AlternativaID)
        {
            ViewBag.PreguntaID = PreguntaID;

            if (ModelState.IsValid)
            {
                this.db.Entry(update).State = EntityState.Modified;
                this.db.SaveChanges();

                return RedirectToAction("AdministrarPregunta", new { PreguntaID = PreguntaID });
            }

            return View(update);
        }

        public ActionResult EliminarAlternativa(int AlternativaID, int PreguntaID)
        {
            Alternativa alt = db.Alternativas.Find(AlternativaID);

            db.Preguntas.Find(PreguntaID).Alternativas.Remove(alt);

            db.Alternativas.Remove(alt);
            db.SaveChanges();

            return RedirectToAction("AdministrarPregunta", new { PreguntaID = PreguntaID });
        }


        public ActionResult AgregarCriterioCorreccion(int TextoID, int? PaginaID, int PreguntaID)
        {
            ViewBag.Pregunta = db.Preguntas.Find(PreguntaID);
            ViewBag.TextoID = TextoID;

            return View();
        }

        [HttpPost]
        public ActionResult AgregarCriterioCorreccion(string Opcion, string Valor, string FeedbackCriterio, int TextoID, int? PaginaID, int PreguntaID)
        {
            Pregunta preg = db.Preguntas.Find(PreguntaID);
            Criterio criterio = new Criterio();
            criterio.Opcion = (string) Opcion;
            criterio.Valor = Double.Parse(Valor);
            criterio.FeedbackCriterio = FeedbackCriterio;
            criterio.PreguntaID = PreguntaID;
            
            criterio.Pregunta = preg;

            db.Preguntas.Find(PreguntaID).Criterios.Add(criterio);
            db.Criterio.Add(criterio);

            db.SaveChanges();

            return RedirectToAction("AgregarCriterioCorreccion", new { TextoID = TextoID, PaginaID = PaginaID, PreguntaID = PreguntaID });
        }
           
        /* MÉTODO ANTIGUO
         * FALLA Al parsear el atributo Valor de criterio, aunque llegue 0.5 lo parsea como 5.0
         * No deja enviar "5,0", el atributo llega correcto en la Request, asi que para solucionarlo parseamos manualmente los atributos del criterio
         * */
        /*
        [HttpPost]
        public ActionResult AgregarCriterioCorreccion(Criterio criterio, int TextoID, int? PaginaID, int PreguntaID)
        {
            Pregunta preg = db.Preguntas.Find(PreguntaID);

            criterio.Pregunta = preg;

            db.Preguntas.Find(PreguntaID).Criterios.Add(criterio);
            db.Criterio.Add(criterio);

            db.SaveChanges();

            return RedirectToAction("AgregarCriterioCorreccion", new { TextoID = TextoID, PaginaID = PaginaID, PreguntaID = PreguntaID });
        }
        */
        public ActionResult EliminarCriterioCorreccion(int TextoID, int CriterioID, int PreguntaID)
        {
            Pregunta preg = db.Preguntas.Find(PreguntaID);
            Criterio cri = db.Criterio.Find(CriterioID);

            preg.Criterios.Remove(cri);
            db.Criterio.Remove(cri);

            db.SaveChanges();

            return RedirectToAction("AgregarCriterioCorreccion", new { TextoID = TextoID, PreguntaID = PreguntaID });
        }

        #endregion

        public ActionResult GetSecuencia()
        {

            return View();
        }

        [HttpPost]
        public ActionResult GetSecuencia(string UserName)
        {
            List<DatosSecuencia> datosSecuencia = new List<DatosSecuencia>();
            List<List<DatosSecuencia>> Todos = new List<List<DatosSecuencia>>();
            StringBuilder str = new StringBuilder();

            try
            {
                var user = (from u in db.UserProfiles
                            where u.UserName == UserName
                            select u).First();

                var datosUsuarios = from du in db.DatosUsuario
                                    where du.UserProfileID == user.UserId
                                    select du;

                foreach (DatosUsuario du in datosUsuarios)
                {
                    datosSecuencia = new List<DatosSecuencia>();

                    var datosSimples = from ds in db.DatosSimples
                                       where ds.DatosUsuarioID == du.DatosUsuarioID
                                       select ds;


                    foreach (DatoSimple ds in datosSimples)
                    {
                        str.AppendFormat("CodeOP: {0}, PreguntaID: {1}, TextoID: {2}, Momento: {3}, Info: {4}, Tiempo: {5}, Valor: {6}, Dato01: {7}, Dato02: {8}, Dato03: {9}", ds.CodeOP, ds.PreguntaID, ds.TextoID, ds.Momento, ds.Info, ds.Tiempo, ds.Valor, ds.Dato01, ds.Dato02, ds.Dato03);
                        datosSecuencia.Add(new DatosSecuencia { Cadena = str.ToString() });
                    }

                    Todos.Add(datosSecuencia);
                }
            }
            catch(Exception)
            {
            
            }

            return View("Secuencia", Todos);
        }


        #region Funciones Modelado
        public ActionResult AdministrarEscena(int? GrupoID, int? ModuloID, int EscenaID)
        {
            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            if (GrupoID != null)
            {
                ViewBag.Grupo = db.Grupos.Find(GrupoID);
            }

            if (ModuloID != null)
            {
                ViewBag.Modulo = db.Modulos.Find(ModuloID);
            }

            return View(db.Escenas.Find(EscenaID));
        }

        public ActionResult CrearEscena(int? GrupoID, int? ModuloID)
        {
            if (GrupoID != null)
            {
                ViewBag.Grupo = db.Grupos.Find(GrupoID);
            }

            if (ModuloID != null)
            {
                ViewBag.Modulo = db.Modulos.Find(ModuloID);
            }

            return View();
        }

        [HttpPost]
        public ActionResult CrearEscena(Escena escena, int? GrupoID, int? ModuloID)
        {
            if (ModuloID != null)
            {   
                escena.Modulo = db.Modulos.Find(ModuloID);
                escena.ModuloID = db.Modulos.Find(ModuloID).ModuloID;
            }

            escena.Acciones = new List<Accion>();
            db.Escenas.Add(escena);           
            db.SaveChanges();

            return RedirectToAction("CrearAccion", new { GrupoID = GrupoID, ModuloID = ModuloID, EscenaID = escena.EscenaID });            
        }

        public ActionResult EliminarEscena(int EscenaID, int ModuloID)
        {
            Escena esc = db.Escenas.Find(EscenaID);

            db.Escenas.Remove(esc);

            db.SaveChanges();

            return RedirectToAction("AdministrarModulo", new { ModuloID = ModuloID });                        
        }

        public ActionResult ConfigurarEscena(int EscenaID)
        {
            return View();
        }
      
        [HttpPost]
        public ActionResult ConfigurarEscena(ConfigEscena config, int EscenaID)
        {
            return View();
        }

        public ActionResult SimularEscena(int EscenaID)
        {
            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            return View(db.Escenas.Find(EscenaID));
        }

        public ActionResult EditarAccion(int AccionID, int EscenaID, int Origen, int? ModuloID)
        {
            return View(db.Acciones.Find(AccionID));
        }

        [HttpPost]        
        public ActionResult EditarAccion(Accion acc, int AccionID, int EscenaID, int Origen, int? ModuloID)
        {
            if (ModelState.IsValid)
            {
                this.db.Entry(acc).State = EntityState.Modified;
                this.db.SaveChanges();

                switch (Origen)
                {   
                    case 1:
                        return RedirectToAction("CrearAccion", new { EscenaID = EscenaID });
                    case 2:
                        return RedirectToAction("AdministrarEscena", new { ModuloID = ModuloID, EscenaID = EscenaID });
                    default:
                        return RedirectToAction("AdministrarEscena", new { ModuloID = ModuloID, EscenaID = EscenaID });
                }                  
            }

            return View(acc);            
        }

        [HttpPost]
        public ActionResult ActualizarAccion(int AccionID, string mensaje)
        {
            db.Acciones.Find(AccionID).Mensaje = mensaje;
            db.SaveChanges();

            return Json(new { redirect = Url.Action("SimularEscena", new { EscenaID = db.Acciones.Find(AccionID).EscenaID }) });            
        }


        public ActionResult SubirAccion(int EscenaID, int AccionID)
        {
            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            Accion accion = db.Acciones.Find(AccionID);

            if (accion.Orden > 1)
            {
                Accion acc = (from a in db.Acciones
                              where a.Orden == accion.Orden - 1 && a.EscenaID == EscenaID
                              select a).Single();

                accion.Orden--;
                acc.Orden++;

                db.SaveChanges();
            }

            return RedirectToAction("AdministrarEscena", new { EscenaID = EscenaID });
        }

        public ActionResult BajarAccion(int EscenaID, int AccionID)
        {
            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            Accion accion = db.Acciones.Find(AccionID);
            Escena escena = db.Escenas.Find(EscenaID);

            if (accion.Orden < escena.Acciones.Count)
            {
                Accion acc = (from a in db.Acciones
                              where a.Orden == accion.Orden + 1 && a.EscenaID == EscenaID
                              select a).Single();

                accion.Orden++;
                acc.Orden--;

                db.SaveChanges();
            }

            db.SaveChanges();

            return RedirectToAction("AdministrarEscena", new { EscenaID = EscenaID });
        }

        public ActionResult EliminarAccion(int? GrupoID, int? ModuloID, int EscenaID, int AccionID, int Origen)
        {
            Accion accion = db.Acciones.Find(AccionID);
            int num = accion.Orden;

            db.Acciones.Remove(db.Acciones.Find(AccionID));

            var acciones = from a in db.Acciones
                           where a.EscenaID == EscenaID
                           select a;


            foreach (Accion acc in acciones)
            {
                if (acc.Orden > num)
                {
                    acc.Orden--;
                }
            }

            db.SaveChanges();

            switch (Origen)
            { 
                case 1:
                    return RedirectToAction("CrearAccion", new { GrupoID = GrupoID, ModuloID = ModuloID, EscenaID = EscenaID });                                        
                case 2:
                    return RedirectToAction("AdministrarEscena", new { ModuloID = ModuloID, EscenaID = EscenaID });                        
                default:
                    return RedirectToAction("AdministrarEscena", new { ModuloID = ModuloID, EscenaID = EscenaID });                                            
            }            
        }

        public ActionResult ReordenarAcciones(int? GrupoID, int? ModuloID, int EscenaID)
        {
            var acciones = from acc in db.Acciones
                           where acc.EscenaID == EscenaID
                           orderby acc.Orden
                           select acc;

            int i = 1;

            foreach (Accion acc in acciones)
            {
                acc.Orden = i++;
            }

            db.SaveChanges();
            
            return RedirectToAction("CrearAccion", new { GrupoID = GrupoID, ModuloID = ModuloID, EscenaID = EscenaID });                                        
        }

        public ActionResult CrearAccion(int? GrupoID, int? ModuloID, int EscenaID)
        {
            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            if (GrupoID != null)
            {
                ViewBag.Grupo = db.Grupos.Find(GrupoID);
            }

            if (ModuloID != null)
            {
                ViewBag.Modulo = db.Modulos.Find(ModuloID);
            }

            ViewBag.Escena = db.Escenas.Find(EscenaID);

            //var ids = db.Modulos.Find(db.Escenas.Find(EscenaID).ModuloID).Textos;

            ViewBag.Textos = db.Modulos.Find(db.Escenas.Find(EscenaID).ModuloID).Textos;

            ViewBag.Paginas = db.Modulos.Find(db.Escenas.Find(EscenaID).ModuloID).Textos.First().Paginas;

            var preguntas = from p in db.Modulos.Find(db.Escenas.Find(EscenaID).ModuloID).Textos
                            from pr in p.Preguntas
                            select pr;

            ViewBag.Preguntas = preguntas;

            ViewBag.Tipos = GetCodigos();

            return View();
        }

        [HttpPost]
        public ActionResult CrearAccion(Accion accion, int? GrupoID, int? ModuloID, int EscenaID)
        {
            foreach (Accion acc in db.Acciones)
            {
                continue;
            }

            if (accion.CodeOP >= 1 && accion.CodeOP <= 6)
            {
                
            }

            if (ModelState.IsValid)
            {
                Escena escena = db.Escenas.Find(EscenaID);

                if (escena.Acciones == null)
                    escena.Acciones = new List<Accion>();

                escena.Acciones.Add(accion);
                accion.Escena = db.Escenas.Find(EscenaID);
                accion.EscenaID = EscenaID;

                if (accion.Orden == db.Escenas.Find(EscenaID).Acciones.Count)
                {
                    accion.Orden = db.Escenas.Find(EscenaID).Acciones.Count;
                    db.Acciones.Add(accion);

                    db.SaveChanges();
                }
                else
                {
                    int pos = accion.Orden;

                    var acciones = from a in db.Acciones
                                   where a.EscenaID == EscenaID &&
                                   a.Orden >= pos
                                   select a;

                    foreach (Accion acc in acciones)
                    {
                        acc.Orden++;
                    }

                    db.Acciones.Add(accion);

                    db.SaveChanges();                
                }
            }

            ViewBag.Textos = db.Modulos.Find(db.Escenas.Find(EscenaID).ModuloID).Textos;

            ViewBag.Paginas = db.Modulos.Find(db.Escenas.Find(EscenaID).ModuloID).Textos.First().Paginas;

            var preguntas = from p in db.Modulos.Find(db.Escenas.Find(EscenaID).ModuloID).Textos
                            from pr in p.Preguntas
                            select pr;

            ViewBag.Preguntas = preguntas;
            
            ViewBag.Escena = db.Escenas.Find(EscenaID);
            ViewBag.Tipos = GetCodigos();

            return View();  
        }

        [HttpPost]
        public ActionResult GetPaginas(int TextoID)
        {
            Texto texto = db.Textos.Find(TextoID);

            IEnumerable<int> listPaginas = from p in texto.Paginas
                                           select p.PaginaID;

            return Json(new { redirect = listPaginas.ToList() });            
        }
        #endregion

        #region Funciones Grupos
        public ActionResult Grupo()
        {
            var user = getCurrentUser();

            var grupos = from g in db.Grupos
                            from d in g.Propietarios
                            where d.UserId == user.UserId
                            select g;

            ViewBag.Grupos = grupos.ToList();

            return View();
        }

        public ActionResult AdministrarGrupo(int GrupoID)
        {
            Grupo grupo = db.Grupos.Find(GrupoID);

            ViewBag.User = getCurrentUser().UserId;

            return View(grupo);
        }

        public ActionResult CrearGrupo()
        {
            return View();
        }

        public ActionResult AgregarModulo(int GrupoID)
        {
            Grupo grupo = db.Grupos.Find(GrupoID);
            
            var user = getCurrentUser();

            var modulos = from g in db.Modulos
                          from d in g.Propietarios
                          where d.UserId == user.UserId
                          select g;

            ViewBag.Modulos = modulos.ToList();

            return View(grupo);
        }

        public ActionResult AgregarModuloExistente(int GrupoID, int ModuloID)
        {
            Modulo modulo = db.Modulos.Find(ModuloID);
            Grupo grupo = db.Grupos.Find(GrupoID);

            //guirisan/secuencias
            //var count = contar modulos del grupo grupo
            //añadir a la entidad GrupoModulo la relación, asignando al orden count +1
            
            GrupoModulo gm = new GrupoModulo();
            gm.GrupoID = GrupoID;
            gm.Grupo = grupo;
            gm.ModuloID = ModuloID;
            gm.Modulo = modulo;
            gm.Orden = grupo.Modulos.Count() + 1;
            db.GrupoModulo.Add(gm);

            db.Grupos.Find(GrupoID).Modulos.Add(modulo);

            if (grupo.Orden == null || grupo.Orden == "")
            {
                grupo.Orden = modulo.ModuloID.ToString();
            }
            else
            {
                grupo.Orden += ":" + modulo.ModuloID.ToString();            
            }

            modulo.Grupos.Add(grupo);

            db.SaveChanges();

            return RedirectToAction("AdministrarGrupo", new { GrupoID = GrupoID });
        }

        public ActionResult DesvincularmeDeModulo(int ModuloID)
        {
            UserProfile user = getCurrentUser();
            Modulo modulo = db.Modulos.Find(ModuloID);

            user.Modulos.Remove(modulo);
            modulo.Propietarios.Remove(user);

            db.SaveChanges();

            return RedirectToAction("Modulos");
        }

        public ActionResult DesvincularModulo(int GrupoID, int ModuloID)
        {
            //TO DO
            //guirisan/pending
            //eliminar en la tabla relacion GrupoModulo la fila correspondiente

            Modulo modulo = db.Modulos.Find(ModuloID);
            Grupo grupo = db.Grupos.Find(GrupoID);

            grupo.Modulos.Remove(modulo);

            var param = grupo.Orden.Split(':');
            string nuevoOrden = "";

            foreach (string str in param)
            {
                if (str == ModuloID.ToString())
                    continue;
                
                if(nuevoOrden == "")
                {
                    nuevoOrden = str;
                }
                else
                {
                    nuevoOrden += ":" + str;
                }
            }

            grupo.Orden = nuevoOrden;

            db.SaveChanges();

            return RedirectToAction("AdministrarGrupo", new { GrupoID = GrupoID });
        }
        
        [HttpPost]
        public ActionResult CrearGrupo(Grupo nuevoGrupo)
        {
            ConfigGrupo config = new ConfigGrupo();

            nuevoGrupo.Propietarios = new List<UserProfile>();

            nuevoGrupo.Propietarios.Add(getCurrentUser());
            
            db.Grupos.Add(nuevoGrupo);

            db.SaveChanges();

            config.GrupoID = nuevoGrupo.GrupoID;
            config.Grupo = db.Grupos.Find(nuevoGrupo.GrupoID);

            nuevoGrupo.ConfigGrupo = config;
            db.ConfigGrupo.Add(config);

            db.SaveChanges();

            return RedirectToAction("AdministrarGrupo", new { GrupoID = nuevoGrupo.GrupoID });
        }

        public ActionResult EditarGrupo(int? GrupoID)
        {
            ViewBag.Tipos = GetTipos();

            return View(db.Grupos.Find(GrupoID));
        }

        [HttpPost]
        public ActionResult EditarGrupo(Grupo updateGrupo)
        {
            if (ModelState.IsValid)
            {
                Grupo grupo = db.Grupos.Find(updateGrupo.GrupoID);

                grupo.Nombre = updateGrupo.Nombre;
                grupo.Descripcion = updateGrupo.Descripcion;
                grupo.Publico = updateGrupo.Publico;

                this.db.Entry(grupo).State = EntityState.Modified;
                this.db.SaveChanges();

                return RedirectToAction("AdministrarGrupo", new { GrupoID = updateGrupo.GrupoID });
            }

            return View(updateGrupo);
        }

        public ActionResult EliminarGrupo(int GrupoID)
        {            
            var mod = db.Modulos.ToList();

            mod.ForEach(m => db.Grupos.Find(GrupoID).Modulos.Remove(m));

            db.Grupos.Remove(db.Grupos.Find(GrupoID));

            db.SaveChanges();

            return this.RedirectToAction("Grupo");
        }

        #endregion

        #region Funciones Feedback
        public ActionResult Feedback()
        {  
            ViewBag.Variables = GetVariables();
            

            #region Operadores
            List<SelectListItem> Operadores = new List<SelectListItem>();

            Operadores.Add(new SelectListItem { Text = "Igual (==)", Value = "1" });
            Operadores.Add(new SelectListItem { Text = "Menor o igual (<=)", Value = "2" });
            Operadores.Add(new SelectListItem { Text = "Mayor o igual (>=)", Value = "3" });
            Operadores.Add(new SelectListItem { Text = "Desigual (!=)", Value = "4" });
            Operadores.Add(new SelectListItem { Text = "Menor que... (<)", Value = "5" });
            Operadores.Add(new SelectListItem { Text = "Mayor que... (>)", Value = "6" });

            ViewBag.Operadores = Operadores;
            #endregion
            
            var user = getCurrentUser();

            var reglas = from r in db.ReglasComplejas
                         where r.UserProfileID == user.UserId
                         select r;

            var reglasSimples = from r in db.ReglasSimples
                                where r.UserProfileID == user.UserId
                                select r;

            var timings = from t in db.Timings
                          where t.UserProfileID == user.UserId
                          select t;

            ViewBag.Timings = timings.ToList();

            ViewBag.Reglas = reglasSimples.ToList();

            return View(reglas.ToList());
        }

        public ActionResult GetSimples()
        {
            var user = getCurrentUser();

            var reglasSimples = from r in db.ReglasSimples
                                where r.UserProfileID == user.UserId
                                select r.ReglaSimpleID;

            return Json(new { Reglas = reglasSimples });
        }

        public ActionResult GetComplejas()
        {
            var user = getCurrentUser();

            var reglasComplejas = from r in db.ReglasComplejas
                                  where r.UserProfileID == user.UserId
                                  select r.ReglaComplejaID;

            return Json(new { Reglas = reglasComplejas });
        }

        public ActionResult AgregarTiming()
        {
            var user = getCurrentUser();

            var timings = from t in db.Timings
                          where t.UserProfileID == user.UserId
                          select t;

            ViewBag.Timings = timings.ToList();

            List<SelectListItem> TipoTimings = new List<SelectListItem>();

            TipoTimings.Add(new SelectListItem { Text = "Simple / Acumulativo", Value = "0" });
            TipoTimings.Add(new SelectListItem { Text = "Complejo / Desarrollado", Value = "1" });

            ViewBag.TipoTimings = TipoTimings;

            return View();
        }

        [HttpPost]
        public ActionResult AgregarTiming(Timing Timing)
        {
            var user = getCurrentUser();

            Timing.UserProfile = user;
            Timing.UserProfileID = user.UserId;

            db.Timings.Add(Timing);

            db.SaveChanges();

            List<SelectListItem> TipoTimings = new List<SelectListItem>();

            TipoTimings.Add(new SelectListItem { Text = "Simple / Acumulativo", Value = "0" });
            TipoTimings.Add(new SelectListItem { Text = "Complejo / Desarrollado", Value = "1" });

            var timings = from t in db.Timings
                          where t.UserProfileID == user.UserId
                          select t;

            ViewBag.Timings = timings.ToList();

            ViewBag.TipoTimings = TipoTimings;

            return View();
        }

        public ActionResult EditarTiming(int TimingID)
        {
            List<SelectListItem> TipoTimings = new List<SelectListItem>();

            TipoTimings.Add(new SelectListItem { Text = "Simple / Acumulativo", Value = "0" });
            TipoTimings.Add(new SelectListItem { Text = "Complejo / Desarrollado", Value = "1" });

            ViewBag.TipoTimings = TipoTimings;

            return View(db.Timings.Find(TimingID));
        }

        [HttpPost]
        public ActionResult EditarTiming(Timing updateTiminig, int TimingID)
        {
            if (ModelState.IsValid)
            {
                this.db.Entry(updateTiminig).State = EntityState.Modified;
                this.db.SaveChanges();

                return RedirectToAction("Feedback");
            }

            List<SelectListItem> TipoTimings = new List<SelectListItem>();

            TipoTimings.Add(new SelectListItem { Text = "Simple / Acumulativo", Value = "0" });
            TipoTimings.Add(new SelectListItem { Text = "Complejo / Desarrollado", Value = "1" });

            ViewBag.TipoTimings = TipoTimings;

            return View(updateTiminig);
        }

        public ActionResult EliminarTiming(int TimingID)
        {
            Timing timing = db.Timings.Find(TimingID);

            var modulos = from m in db.Modulos
                          from r in m.Timings
                          where r.TimingID == TimingID
                          select m;

            foreach (Modulo mod in modulos)
            {
                mod.Timings.Remove(timing);
            }

            db.Timings.Remove(timing);

            db.SaveChanges();

            return RedirectToAction("Feedback");
        }

        public ActionResult AgregarRegla(int ModuloID)
        {
            Modulo modulo = db.Modulos.Find(ModuloID);
            
            var user = getCurrentUser();

            var reglas = from r in db.ReglasComplejas                          
                          where r.UserProfileID == user.UserId
                          select r;

            ViewBag.Reglas = reglas.ToList();

            return View(modulo);        
        }


        

        public ActionResult DesvincularTiming(int ModuloID, int TimingID)
        {
            var timing = db.Timings.Find(TimingID);

            Modulo modulo = db.Modulos.Find(ModuloID);

            modulo.Timings.Remove(timing);

            db.SaveChanges();

            return RedirectToAction("AdministrarModulo", new { ModuloID = ModuloID });
        }

        public ActionResult DesvincularRegla(int ModuloID, int ReglaComplejaID)
        {
            var regla = db.ReglasComplejas.Find(ReglaComplejaID);

            Modulo modulo = db.Modulos.Find(ModuloID);

            modulo.ReglasComplejas.Remove(regla);

            db.SaveChanges();

            return RedirectToAction("AdministrarModulo", new { ModuloID = ModuloID });
        }

        public ActionResult AgregarTimingaModulo(int ModuloID)
        {
            Modulo modulo = db.Modulos.Find(ModuloID);

            var user = getCurrentUser();

            var timing = from t in db.Timings
                         where t.UserProfileID == user.UserId
                         select t;

            ViewBag.Timings = timing.ToList();

            return View(modulo);
        }

        public ActionResult AgregarTimingExistente(int ModuloID, int TimingID)
        {
            Modulo modulo = db.Modulos.Find(ModuloID);
            Timing timing = db.Timings.Find(TimingID);

            db.Modulos.Find(ModuloID).Timings.Add(timing);

            timing.Modulos.Add(modulo);

            db.SaveChanges();

            return RedirectToAction("AdministrarModulo", new { ModuloID = ModuloID });
        }

        public ActionResult AgregarReglaExistente(int ModuloID, int ReglaID)
        {
            Modulo modulo = db.Modulos.Find(ModuloID);
            ReglaCompleja regla = db.ReglasComplejas.Find(ReglaID);

            db.Modulos.Find(ModuloID).ReglasComplejas.Add(regla);
            
            regla.Modulos.Add(modulo);
            
            db.SaveChanges();

            return RedirectToAction("AdministrarModulo", new { ModuloID = ModuloID });
        }

        public ActionResult AgregarReglasSeleccionadas(int ModuloID, string Lista)
        {
            Modulo modulo = db.Modulos.Find(ModuloID);

            string tmp = Lista.Substring(0, Lista.Length - 1);
            string[] param = tmp.Split('-');

            foreach (string str2 in param)
            {
                ReglaCompleja regla = db.ReglasComplejas.Find(Convert.ToInt32(str2));

                db.Modulos.Find(ModuloID).ReglasComplejas.Add(regla);

                regla.Modulos.Add(modulo);

                db.SaveChanges();
            }

            return Json(new { redirect = Url.Action("AdministrarModulo", new { ModuloID = ModuloID }) });          
        }

        

        public ActionResult AgregarReglaCompleja()
        {
            var user = getCurrentUser();

            var reglasComplejas = from r in db.ReglasComplejas
                                  where r.UserProfileID == user.UserId
                                  select r;

            var reglasSimples = from r in db.ReglasSimples
                                where r.UserProfileID == user.UserId
                                select r;

            ViewBag.ReglasSimples = reglasSimples.ToList();
            ViewBag.ReglasComplejas = reglasComplejas.ToList();
            
            #region Variables
            List<SelectListItem> Operadores = new List<SelectListItem>();

            Operadores.Add(new SelectListItem { Text = "Y (&&)", Value = "1" });
            Operadores.Add(new SelectListItem { Text = "O (||)", Value = "2" });

            ViewBag.Operadores = Operadores;

            List<SelectListItem> Reglas = new List<SelectListItem>();

            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Simple | Regla 2: Vacío", Value = "1" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Vacío | Regla 2: Regla Simple", Value = "2" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Compleja | Regla 2: Vacío", Value = "3" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Vacío | Regla 2: Regla Compleja", Value = "4" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Simple | Regla 2: Regla Simple", Value = "5" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Simple | Regla 2: Regla Compleja", Value = "6" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Compleja | Regla 2: Regla Simple", Value = "7" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Compleja | Regla 2: Regla Compleja", Value = "8" });

            ViewBag.Reglas = Reglas;

            #endregion

            ViewBag.VariablesSimples = GetVariables();            

            #region Operadores
            List<SelectListItem> OperadoresSimples = new List<SelectListItem>();

            OperadoresSimples.Add(new SelectListItem { Text = "Igual (==)", Value = "1" });
            OperadoresSimples.Add(new SelectListItem { Text = "Menor o igual (<=)", Value = "2" });
            OperadoresSimples.Add(new SelectListItem { Text = "Mayor o igual (>=)", Value = "3" });
            OperadoresSimples.Add(new SelectListItem { Text = "Desigual (!=)", Value = "4" });
            OperadoresSimples.Add(new SelectListItem { Text = "Menor que... (<)", Value = "5" });
            OperadoresSimples.Add(new SelectListItem { Text = "Mayor que... (>)", Value = "6" });

            ViewBag.OperadoresSimples = OperadoresSimples;
            #endregion
            

            return View();
        }

        [HttpPost]
        public ActionResult AgregarReglaCompleja(ReglaCompleja RC) //, HttpPostedFile file = null
        {
            var user = getCurrentUser();

            //guirisan/issue https://github.com/guirisan/ituinbook/issues/99

            /*if (file != null && file.ContentLength > 0)
            {
                // extract only the fielname
                var fileName = Path.GetFileName(file.FileName);
                // store the file inside ~/App_Data/uploads folder
                var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName);
                file.SaveAs(path);
                RC.FeedbackAudio = new byte[file.ContentLength];
                file.InputStream.Read(RC.FeedbackAudio, 0, file.ContentLength);
            }
             * */

            
           

            RC.UserProfile = user;
            RC.UserProfileID = user.UserId;

            db.ReglasComplejas.Add(RC);

            db.SaveChanges();

            return RedirectToAction("Feedback");
        }

        public ActionResult EditarReglaCompleja(int ReglaComplejaID)
        {
            var user = getCurrentUser();

            var reglasComplejas = from r in db.ReglasComplejas
                                  where r.UserProfileID == user.UserId
                                  select r;

            var reglasSimples = from r in db.ReglasSimples
                                where r.UserProfileID == user.UserId
                                select r;

            ViewBag.ReglasSimples = reglasSimples.ToList();
            ViewBag.ReglasComplejas = reglasComplejas.ToList();

            #region Reglas Complejas
            List<SelectListItem> Operadores = new List<SelectListItem>();

            Operadores.Add(new SelectListItem { Text = "Y (&&)", Value = "1" });
            Operadores.Add(new SelectListItem { Text = "O (||)", Value = "2" });

            ViewBag.Operadores = Operadores;

            List<SelectListItem> Reglas = new List<SelectListItem>();

            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Simple | Regla 2: Vacío", Value = "1" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Vacío | Regla 2: Regla Simple", Value = "2" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Compleja | Regla 2: Vacío", Value = "3" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Vacío | Regla 2: Regla Compleja", Value = "4" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Simple | Regla 2: Regla Simple", Value = "5" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Simple | Regla 2: Regla Compleja", Value = "6" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Compleja | Regla 2: Regla Simple", Value = "7" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Compleja | Regla 2: Regla Compleja", Value = "8" });

            ViewBag.Reglas = Reglas;

            #endregion

            ViewBag.VariablesSimples = GetVariables();
          

            #region Operadores
            List<SelectListItem> OperadoresSimples = new List<SelectListItem>();

            OperadoresSimples.Add(new SelectListItem { Text = "Igual (==)", Value = "1" });
            OperadoresSimples.Add(new SelectListItem { Text = "Menor o igual (<=)", Value = "2" });
            OperadoresSimples.Add(new SelectListItem { Text = "Mayor o igual (>=)", Value = "3" });
            OperadoresSimples.Add(new SelectListItem { Text = "Desigual (!=)", Value = "4" });
            OperadoresSimples.Add(new SelectListItem { Text = "Menor que... (<)", Value = "5" });
            OperadoresSimples.Add(new SelectListItem { Text = "Mayor que... (>)", Value = "6" });

            ViewBag.OperadoresSimples = OperadoresSimples;
            #endregion

            return View(db.ReglasComplejas.Find(ReglaComplejaID));
        }

        [HttpPost]
        public ActionResult EditarReglaCompleja(ReglaCompleja updateReglaCompleja)
        {
            UserProfile user = getCurrentUser();

            var reglasComplejas = from r in db.ReglasComplejas
                                  where r.UserProfileID == user.UserId
                                  select r;

            var reglasSimples = from r in db.ReglasSimples
                                where r.UserProfileID == user.UserId
                                select r;
            
            #region Reglas Complejas
            List<SelectListItem> Operadores = new List<SelectListItem>();

            Operadores.Add(new SelectListItem { Text = "Y (&&)", Value = "1" });
            Operadores.Add(new SelectListItem { Text = "O (||)", Value = "2" });

            ViewBag.Operadores = Operadores;

            List<SelectListItem> Reglas = new List<SelectListItem>();

            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Simple | Regla 2: Vacío", Value = "1" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Vacío | Regla 2: Regla Simple", Value = "2" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Compleja | Regla 2: Vacío", Value = "3" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Vacío | Regla 2: Regla Compleja", Value = "4" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Simple | Regla 2: Regla Simple", Value = "5" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Simple | Regla 2: Regla Compleja", Value = "6" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Compleja | Regla 2: Regla Simple", Value = "7" });
            Reglas.Add(new SelectListItem { Text = "Regla 1: Regla Compleja | Regla 2: Regla Compleja", Value = "8" });

            ViewBag.Reglas = Reglas;

            #endregion

            ViewBag.VariablesSimples = GetVariables();
            

            #region Operadores
            List<SelectListItem> OperadoresSimples = new List<SelectListItem>();

            OperadoresSimples.Add(new SelectListItem { Text = "Igual (==)", Value = "1" });
            OperadoresSimples.Add(new SelectListItem { Text = "Menor o igual (<=)", Value = "2" });
            OperadoresSimples.Add(new SelectListItem { Text = "Mayor o igual (>=)", Value = "3" });
            OperadoresSimples.Add(new SelectListItem { Text = "Desigual (!=)", Value = "4" });
            OperadoresSimples.Add(new SelectListItem { Text = "Menor que... (<)", Value = "5" });
            OperadoresSimples.Add(new SelectListItem { Text = "Mayor que... (>)", Value = "6" });

            ViewBag.OperadoresSimples = OperadoresSimples;
            #endregion

            ViewBag.ReglasSimples = reglasSimples.ToList();
            ViewBag.ReglasComplejas = reglasComplejas.ToList();

            if (ModelState.IsValid)
            {
                this.db.Entry(updateReglaCompleja).State = EntityState.Modified;
                this.db.SaveChanges();

                return this.RedirectToAction("Feedback");
            }

            return View(updateReglaCompleja);
        }

        public ActionResult EliminarReglaCompleja(int ReglaComplejaID)
        {
            ReglaCompleja regla = db.ReglasComplejas.Find(ReglaComplejaID);

            var modulos = from m in db.Modulos
                          from r in m.ReglasComplejas
                          where r.ReglaComplejaID == ReglaComplejaID
                          select m;

            foreach (Modulo mod in modulos)
            {
                mod.ReglasComplejas.Remove(regla);
            }

            db.ReglasComplejas.Remove(regla);

            db.SaveChanges();

            return RedirectToAction("Feedback");
        }

       
        public ActionResult AgregarReglaSimple()
        {  
            ViewBag.Variables = GetVariables();
         
            #region Operadores
            List<SelectListItem> Operadores = new List<SelectListItem>();

            Operadores.Add(new SelectListItem { Text = "Igual (==)", Value = "1" });
            Operadores.Add(new SelectListItem { Text = "Menor o igual (<=)", Value = "2" });
            Operadores.Add(new SelectListItem { Text = "Mayor o igual (>=)", Value = "3" });
            Operadores.Add(new SelectListItem { Text = "Desigual (!=)", Value = "4" });
            Operadores.Add(new SelectListItem { Text = "Menor que... (<)", Value = "5" });
            Operadores.Add(new SelectListItem { Text = "Mayor que... (>)", Value = "6" });

            ViewBag.Operadores = Operadores;
            #endregion

            return View();
        }

        [HttpPost]
        public ActionResult AgregarReglaSimple(ReglaSimple RS)
        {
            var user = getCurrentUser();

            RS.UserProfile = user;
            RS.UserProfileID = user.UserId;

            db.ReglasSimples.Add(RS);

            db.SaveChanges();

            return RedirectToAction("Feedback");           
        }

        public ActionResult EditarReglaSimple(int ReglaSimpleID)
        {   
            ViewBag.Variables = GetVariables();
         
            #region Operadores
            List<SelectListItem> Operadores = new List<SelectListItem>();

            Operadores.Add(new SelectListItem { Text = "Igual (==)", Value = "1" });
            Operadores.Add(new SelectListItem { Text = "Menor o igual (<=)", Value = "2" });
            Operadores.Add(new SelectListItem { Text = "Mayor o igual (>=)", Value = "3" });
            Operadores.Add(new SelectListItem { Text = "Desigual (!=)", Value = "4" });
            Operadores.Add(new SelectListItem { Text = "Menor que... (<)", Value = "5" });
            Operadores.Add(new SelectListItem { Text = "Mayor que... (>)", Value = "6" });

            ViewBag.Operadores = Operadores;
            #endregion

            return View(db.ReglasSimples.Find(ReglaSimpleID));
        }

        [HttpPost]
        public ActionResult EditarReglaSimple(ReglaSimple updateReglaSimple)
        {   
            ViewBag.Variables = GetVariables();
         
            #region Operadores
            List<SelectListItem> Operadores = new List<SelectListItem>();

            Operadores.Add(new SelectListItem { Text = "Igual (==)", Value = "1" });
            Operadores.Add(new SelectListItem { Text = "Menor o igual (<=)", Value = "2" });
            Operadores.Add(new SelectListItem { Text = "Mayor o igual (>=)", Value = "3" });
            Operadores.Add(new SelectListItem { Text = "Desigual (!=)", Value = "4" });
            Operadores.Add(new SelectListItem { Text = "Menor que... (<)", Value = "5" });
            Operadores.Add(new SelectListItem { Text = "Mayor que... (>)", Value = "6" });

            ViewBag.Operadores = Operadores;
            #endregion

            if (ModelState.IsValid)
            {
                UserProfile user = getCurrentUser();

                this.db.Entry(updateReglaSimple).State = EntityState.Modified;
                this.db.SaveChanges();

                return this.RedirectToAction("Feedback");
            }

            return View(updateReglaSimple);
        }

        public ActionResult EliminarReglaSimple(int ReglaSimpleID)
        {
            ReglaSimple regla = db.ReglasSimples.Find(ReglaSimpleID);

            var complejas = from r in db.ReglasComplejas
                            where r.Regla_1 == ReglaSimpleID || r.Regla_2 == ReglaSimpleID
                            select r;

            foreach (ReglaCompleja reg in complejas)
            {
                var modulos = from m in db.Modulos
                              from r in m.ReglasComplejas
                              where r.ReglaComplejaID == reg.ReglaComplejaID
                              select m;

                foreach (Modulo mod in modulos)
                {
                    mod.ReglasComplejas.Remove(reg);
                }

                db.ReglasComplejas.Remove(reg);
            }

            db.ReglasSimples.Remove(regla);

            db.SaveChanges();

            return RedirectToAction("Feedback");
        }

        #endregion

        public ActionResult Seguimientos()
        {
            var user = getCurrentUser();

            var grupos = from g in db.Grupos
                         from p in g.Propietarios
                         where p.UserId == user.UserId
                         select g;

            return View(grupos.ToList());
        }

        public ActionResult SeguimientosGrupos(int GrupoID)
        {
            var user = getCurrentUser();

            var grupo = db.Grupos.Find(GrupoID);
            
            ViewBag.GrupoID = GrupoID;

            return View(grupo.Modulos.ToList());
        }
        public ActionResult SeguimientosModulos(int GrupoID, int ModuloID)
        {
            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;

            ViewBag.Modulo = db.Modulos.Find(ModuloID);

            return View();
        }

        public ActionResult DetallesTodos(int GrupoID, int ModuloID)
        {
            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;
            ViewBag.Modulo = db.Modulos.Find(ModuloID);
            Modulo mod = db.Modulos.Find(ModuloID);

            string datos = "";

            var user = getCurrentUser();

            var alumnos = from a in db.UserProfiles
                          from g in db.Grupos
                          from p in g.Propietarios
                          from i in db.Inscripciones
                          from du in db.DatosUsuario
                          where a.Type == "Alumno" &&
                          p.UserId == user.UserId &&
                          i.GrupoID == g.GrupoID &&
                          g.GrupoID == GrupoID &&
                          du.ModuloID == ModuloID &&
                          du.Cerrada == true &&
                          i.Alumno.UserId == a.UserId &&
                          du.UserProfileID == a.UserId
                          select a;

            if (mod.Escenas != null && mod.Escenas.Count() > 0)
            {
                #region Modelado
                foreach (UserProfile id in alumnos)
                {
                    List<DatosDetalladoMod> lista = new List<DatosDetalladoMod>();
                    DatosDetalladoMod ddi = new DatosDetalladoMod();
                    DateTime ini = new DateTime();
                    DateTime fin = new DateTime();
                    int pregActual = 1;
                    Double tiempo = 0;
                    Double tmpPreg = 0;
                    int contador = 0;
                    bool primer = true;

                    ddi.Nombre = id.Nombre;
                    ddi.Usuario = id.UserName;

                    ViewBag.Modulo = db.Modulos.Find(ModuloID);

                    bool tick = false;

                    var listDs = (from d in db.DatosSimples
                                  from u in db.UserProfiles
                                  from du in db.DatosUsuario
                                  where d.DatosUsuarioID == du.DatosUsuarioID &&
                                  du.UserProfileID == id.UserId &&
                                  du.ModuloID == ModuloID &&
                                  du.GrupoID == GrupoID
                                  select d).Distinct().ToList();

                    foreach (DatoSimple ds in listDs)
                    {
                        if (tick)
                        {
                            tmpPreg = 0;

                            fin = Convert.ToDateTime(ds.Momento);

                            Double tmp = (fin - ini).TotalSeconds;

                            if (tmp < 3600)
                            {
                                tmpPreg = tmp;
                                tiempo += tmp;
                            }

                            ini = Convert.ToDateTime(ds.Momento);
                        }
                        else
                        {
                            ini = Convert.ToDateTime(ds.Momento);
                            tick = true;
                        }

                        if (ds.CodeOP == 4) // Tipo Test
                        {
                            ddi.Acierto = ds.Valor.ToString();
                            ddi.Seleccion = ds.Info;
                            ddi.PregActual = pregActual.ToString();
                            ddi.PregId = ds.PreguntaID.ToString();
                            ddi.TmpPreg = tmpPreg.ToString();

                            ddi.TipoPreg = "Test";

                            lista.Add(ddi);

                            pregActual++;
                            contador++;
                            ddi = new DatosDetalladoMod();
                        }

                        if (ds.CodeOP == 7) // Palabras Claves
                        {
                            ddi.PorcBien = ds.Dato01.ToString();
                            ddi.PorcMal = ds.Dato02.ToString();

                            ddi.Seleccion = ds.Info;

                            ddi.PregActual = pregActual.ToString();
                            ddi.PregId = ds.PreguntaID.ToString();
                            ddi.TmpPreg = tmpPreg.ToString();

                            ddi.TipoPreg = "Claves";

                            lista.Add(ddi);

                            pregActual++;
                            contador++;
                            ddi = new DatosDetalladoMod();
                        }

                        if (ds.CodeOP == 5) // Emparejar
                        {
                            ddi.Seleccion = ds.Info;

                            ddi.PregActual = pregActual.ToString();
                            ddi.PregId = ds.PreguntaID.ToString();

                            ddi.TipoPreg = "Parejas";
                            ddi.TmpPreg = tmpPreg.ToString();

                            lista.Add(ddi);

                            pregActual++;
                            contador++;
                            ddi = new DatosDetalladoMod();
                        }
                    }

                    foreach (DatosDetalladoMod dat in lista)
                    {
                        if (primer)
                        {
                            datos += dat.Nombre + "+";
                            datos += dat.Usuario + "+";
                            primer = false;
                        }

                        datos += dat.PregId + "+";
                        datos += dat.PregActual + "+";
                        datos += dat.TipoPreg + "+";
                        datos += dat.Acierto + "+";
                        datos += dat.PorcBien + "+";
                        datos += dat.PorcMal + "+";
                        datos += dat.Seleccion + "+";
                        datos += dat.TmpPreg + "#";
                    }

                    datos = datos.Remove(datos.Length - 1);

                    ViewBag.Tiempo = tiempo;

                    datos = datos.Remove(datos.Length - 1);

                    datos += "$";

                    ViewBag.Contador = contador;

                }

                datos = datos.Remove(datos.Length - 1);

                ViewBag.Datos = datos;
                #endregion
            }
            else
            {
                #region Independiente
                foreach (UserProfile id in alumnos)
                {
                    List<DatosDetalladoIndep> lista = new List<DatosDetalladoIndep>();
                    DatosDetalladoIndep ddi = new DatosDetalladoIndep();
                    DateTime ini = new DateTime();
                    DateTime fin = new DateTime();
                    bool lectIni = false;
                    int pregActual = 1;
                    int textActual = 1;
                    string lecIni = "";
                    int tAciertos = 0;
                    int tBusca = 0;
                    int tAyudas = 0;
                    int tRevisa = 0;
                    bool usaAyuda = false;
                    int contador = 0;
                    bool primer = true;


                    ddi.Nombre = id.Nombre;
                    ddi.Usuario = id.UserName;

                    var listDs = (from d in db.DatosSimples
                                  from u in db.UserProfiles
                                  from du in db.DatosUsuario
                                  where d.DatosUsuarioID == du.DatosUsuarioID &&
                                  du.UserProfileID == id.UserId &&
                                  du.ModuloID == ModuloID &&
                                  du.GrupoID == GrupoID
                                  select d).Distinct().ToList();


                    foreach (DatoSimple ds in listDs)
                    {
                        if (ds.PreguntaID != 0)
                        {
                            ddi.PreguntaID = ds.PreguntaID.ToString();
                        }

                        if (lectIni)
                        {
                            if (ds.CodeOP != 2)
                            {
                                fin = Convert.ToDateTime(ds.Momento);

                                Double tmp = (fin - ini).TotalSeconds;

                                ini = Convert.ToDateTime(ds.Momento);

                                ddi.LectInic = tmp.ToString();
                                lecIni = (Math.Round(tmp, 2)).ToString();

                                lectIni = false;
                            }
                        }

                        if (ds.CodeOP == 5)
                        {
                            ddi.Acierto = ds.Valor.ToString();
                            ddi.PregActual = ds.PreguntaID.ToString();
                            tAciertos += ds.Valor;
                            contador++;
                            continue;
                        }

                        if (ds.CodeOP == 16)
                        {
                            ddi.Busca = "1";
                            tBusca++;
                            continue;
                        }

                        if (ds.CodeOP == 17)
                        {
                            ddi.Revisa = "1";
                            tRevisa++;
                            continue;
                        }

                        if (ds.CodeOP == 15)
                        {
                            if (!usaAyuda)
                            {
                                tAyudas++;
                                usaAyuda = true;
                            }

                            switch (Convert.ToInt32(ds.Dato03))
                            {
                                case 1:
                                    ddi.Ayuda1 = "1";
                                    break;
                                case 2:
                                    ddi.Ayuda2 = "1";
                                    break;
                                case 3:
                                    ddi.Ayuda3 = "1";
                                    break;
                                default:
                                    break;
                            }

                            continue;
                        }

                        if (ds.CodeOP == 10) // Siguiente Pregunta
                        {
                            fin = Convert.ToDateTime(ds.Momento);

                            Double tmp = (fin - ini).TotalSeconds;

                            ini = Convert.ToDateTime(ds.Momento);

                            ddi.TmpPreg = (Math.Round(tmp, 2)).ToString();
                            ddi.TextActual = textActual.ToString();
                            ddi.PregActual = pregActual.ToString();
                            ddi.LectInic = lecIni;

                            lista.Add(ddi);

                            pregActual++;
                            ddi = new DatosDetalladoIndep();
                            usaAyuda = false;
                            continue;
                        }

                        if (ds.CodeOP == 25) // Tarea Selección
                        {
                            ddi.TareaSel = "1";
                            ddi.PorcPert = (ds.Dato01 > 100) ? "100" : ds.Dato01.ToString();
                            ddi.PorcNoPert = (ds.Dato02 > 100) ? "100" : ds.Dato02.ToString();

                            continue;
                        }

                        if (ds.CodeOP == 50) // Cambio de Texto
                        {
                            fin = Convert.ToDateTime(ds.Momento);

                            Double tmp = (fin - ini).TotalSeconds;

                            ddi.TmpPreg = (Math.Round(tmp, 2)).ToString();
                            ddi.PregActual = pregActual.ToString();
                            ddi.TextActual = textActual.ToString();
                            ddi.LectInic = lecIni;

                            lista.Add(ddi);

                            ddi = new DatosDetalladoIndep();

                            pregActual = 1;
                            textActual++;

                            ddi.TextActual = textActual.ToString();
                            ddi.PregActual = "1";

                            ini = Convert.ToDateTime(ds.Momento);

                            lectIni = true;
                            usaAyuda = false;
                            continue;
                        }

                        if (ds.CodeOP == 60)
                        {
                            fin = Convert.ToDateTime(ds.Momento);

                            Double tmp = (fin - ini).TotalSeconds;

                            ddi.TmpPreg = (Math.Round(tmp, 2)).ToString();
                            ddi.PregActual = pregActual.ToString();
                            ddi.TextActual = textActual.ToString();
                            ddi.LectInic = lecIni;

                            lista.Add(ddi);

                            ddi = new DatosDetalladoIndep();
                            usaAyuda = false;
                            continue;
                        }

                        if (ds.CodeOP == 1)
                        {
                            ddi.TextActual = textActual.ToString();
                            ddi.PregActual = pregActual.ToString();

                            ini = Convert.ToDateTime(ds.Momento);
                            lectIni = true;

                            continue;
                        }
                    }

                    foreach (DatosDetalladoIndep dat in lista)
                    {
                        if (primer)
                        {
                            datos += dat.Nombre + "+";
                            datos += dat.Usuario + "+";
                            primer = false;
                        }
                        datos += dat.TextActual + "+";
                        datos += dat.LectInic + "+";
                        datos += dat.PregActual + "+";
                        datos += dat.PreguntaID + "+";
                        datos += dat.Acierto + "+";
                        datos += dat.Busca + "+";
                        datos += dat.Ayuda1 + "+";
                        datos += dat.Ayuda2 + "+";
                        datos += dat.Ayuda3 + "+";
                        datos += dat.Revisa + "+";
                        datos += dat.TareaSel + "+";
                        datos += dat.PorcPert + "+";
                        datos += dat.PorcNoPert + "+";
                        datos += dat.TmpPreg + "#";
                    }

                    datos = datos.Remove(datos.Length - 1);

                    datos += "$";

                    ViewBag.Contador = contador;
                }

                ViewBag.Datos = datos.Remove(datos.Length - 1);
                #endregion
            }
            return View();
        }

        public ActionResult DetallesAlumno(int GrupoID, int ModuloID, int UserId)
        {
            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;

            string datos = "";

            var listDs = (from d in db.DatosSimples
                         from u in db.UserProfiles
                         from du in db.DatosUsuario
                         where d.DatosUsuarioID == du.DatosUsuarioID &&
                         du.UserProfileID == UserId &&
                         du.ModuloID == ModuloID &&
                         du.GrupoID == GrupoID
                          select d).Distinct().ToList();

            Modulo mod = db.Modulos.Find(ModuloID);

            ViewBag.Nombre = db.UserProfiles.Find(UserId).Nombre;

            if (mod.Escenas != null && mod.Escenas.Count() > 0) // Modelado
            {
                List<DatosDetalladoMod> lista = new List<DatosDetalladoMod>();
                DatosDetalladoMod ddi = new DatosDetalladoMod();
                DateTime ini = new DateTime();
                DateTime fin = new DateTime();
                int pregActual = 1;
                Double tiempo = 0;
                Double tmpPreg = 0;
                
                ViewBag.Modulo = db.Modulos.Find(ModuloID);

                bool tick = false;

                foreach (DatoSimple ds in listDs)
                {   
                    if (tick) {
                        tmpPreg = 0;

                        fin = Convert.ToDateTime(ds.Momento);

                        Double tmp = (fin - ini).TotalSeconds;

                        if (tmp < 3600)
                        {
                            tmpPreg = tmp;
                            tiempo += tmp;
                        }

                        ini = Convert.ToDateTime(ds.Momento);
                    }
                    else {
                        ini = Convert.ToDateTime(ds.Momento);
                        tick = true;
                    }

                    if (ds.CodeOP == 4) // Tipo Test
                    {
                        ddi.Acierto = ds.Valor.ToString();
                        ddi.Seleccion = ds.Info;
                        ddi.PregActual = pregActual.ToString();
                        ddi.PregId = ds.PreguntaID.ToString();
                        ddi.TmpPreg = tmpPreg.ToString();

                        ddi.TipoPreg = "Test";

                        lista.Add(ddi);

                        pregActual++;

                        ddi = new DatosDetalladoMod();
                    }

                    if (ds.CodeOP == 7) // Palabras Claves
                    {
                        ddi.PorcBien = ds.Dato01.ToString();
                        ddi.PorcMal = ds.Dato02.ToString();

                        ddi.Seleccion = ds.Info;

                        ddi.PregActual = pregActual.ToString();
                        ddi.PregId = ds.PreguntaID.ToString();
                        ddi.TmpPreg = tmpPreg.ToString();

                        ddi.TipoPreg = "Claves";

                        lista.Add(ddi);

                        pregActual++;

                        ddi = new DatosDetalladoMod();                    
                    }

                    if (ds.CodeOP == 5) // Emparejar
                    {
                        ddi.Seleccion = ds.Info;

                        ddi.PregActual = pregActual.ToString();
                        ddi.PregId = ds.PreguntaID.ToString();
                        
                        ddi.TipoPreg = "Parejas";
                        ddi.TmpPreg = tmpPreg.ToString();

                        lista.Add(ddi);

                        pregActual++;

                        ddi = new DatosDetalladoMod(); 
                    }
                }

                foreach (DatosDetalladoMod dat in lista)
                {
                    datos += dat.PregId + "+";
                    datos += dat.PregActual + "+";
                    datos += dat.TipoPreg + "+";
                    datos += dat.Acierto + "+";
                    datos += dat.PorcBien + "+";
                    datos += dat.PorcMal + "+";
                    datos += dat.Seleccion + "+";
                    datos += dat.TmpPreg + "#";
                }

                datos = datos.Remove(datos.Length - 1);

                ViewBag.Tiempo = tiempo;
                ViewBag.Datos = datos;                
            }
            else // Independiente
            {
                #region Analisis Individual
                List<DatosDetalladoIndep> lista = new List<DatosDetalladoIndep>();
                DatosDetalladoIndep ddi = new DatosDetalladoIndep();
                DateTime ini = new DateTime();
                DateTime fin = new DateTime();
                bool lectIni = false;
                int pregActual = 1;
                int textActual = 1;
                string lecIni = "";
                int tAciertos = 0;
                int tBusca = 0;
                int tAyudas = 0;
                int tRevisa = 0;
                bool usaAyuda = false;

                ViewBag.Modulo = db.Modulos.Find(ModuloID);

                foreach (DatoSimple ds in listDs)
                {
                    if (lectIni)
                    {
                        if (ds.CodeOP != 2)
                        {
                            fin = Convert.ToDateTime(ds.Momento);

                            Double tmp = (fin - ini).TotalSeconds;

                            ini = Convert.ToDateTime(ds.Momento);

                            ddi.LectInic = tmp.ToString();
                            lecIni = tmp.ToString();

                            lectIni = false;
                        }
                    }

                    if (ds.CodeOP == 5)
                    {
                        ddi.Acierto = ds.Valor.ToString();
                        ddi.PregActual = ds.PreguntaID.ToString();
                        tAciertos += ds.Valor;
                        continue;
                    }
                    
                    if (ds.CodeOP == 16)
                    {
                        ddi.Busca = "1";
                        tBusca++;
                        continue;
                    }

                    if (ds.CodeOP == 17)
                    {
                        ddi.Revisa = "1";
                        tRevisa++;
                        continue;
                    }

                    if (ds.CodeOP == 15)
                    {
                        if (!usaAyuda)
                        {
                            tAyudas++;
                            usaAyuda = true;
                        }

                        switch (Convert.ToInt32(ds.Dato03))
                        {   
                            case 1:
                                ddi.Ayuda1 = "1";
                                break;
                            case 2:
                                ddi.Ayuda2 = "1";
                                break;
                            case 3:
                                ddi.Ayuda3 = "1";
                                break;
                            default:
                                break;                        
                        }

                        continue;
                    }

                    if (ds.CodeOP == 10) // Siguiente Pregunta
                    {
                        fin = Convert.ToDateTime(ds.Momento);

                        Double tmp = (fin - ini).TotalSeconds;

                        ini = Convert.ToDateTime(ds.Momento);

                        ddi.TmpPreg = tmp.ToString();
                        ddi.TextActual = textActual.ToString();
                        ddi.PregActual = pregActual.ToString();
                        ddi.LectInic = lecIni;

                        lista.Add(ddi);

                        pregActual++;
                        ddi = new DatosDetalladoIndep();
                        usaAyuda = false;
                        continue;
                    }

                    if (ds.CodeOP == 25) // Tarea Selección
                    {
                        ddi.TareaSel = "1";
                        ddi.PorcPert = (ds.Dato01 > 100) ? "100" : ds.Dato01.ToString();
                        ddi.PorcNoPert = (ds.Dato02 > 100) ? "100" : ds.Dato02.ToString();

                        continue;
                    }

                    if (ds.CodeOP == 50) // Cambio de Texto
                    {
                        fin = Convert.ToDateTime(ds.Momento);

                        Double tmp = (fin - ini).TotalSeconds;

                        ddi.TmpPreg = tmp.ToString();
                        ddi.PregActual = pregActual.ToString();
                        ddi.TextActual = textActual.ToString();
                        ddi.LectInic = lecIni;

                        lista.Add(ddi);

                        ddi = new DatosDetalladoIndep();

                        pregActual = 1;
                        textActual++;

                        ddi.TextActual = textActual.ToString();
                        ddi.PregActual = "1";

                        ini = Convert.ToDateTime(ds.Momento);
                        
                        lectIni = true;
                        usaAyuda = false;
                        continue;
                    }

                    if (ds.CodeOP == 60)
                    {
                        fin = Convert.ToDateTime(ds.Momento);

                        Double tmp = (fin - ini).TotalSeconds;

                        ddi.TmpPreg = tmp.ToString();
                        ddi.PregActual = pregActual.ToString();
                        ddi.TextActual = textActual.ToString();
                        ddi.LectInic = lecIni;

                        lista.Add(ddi);

                        ddi = new DatosDetalladoIndep();
                        usaAyuda = false;
                        continue;
                    }

                    if (ds.CodeOP == 1)
                    {   
                        ddi.TextActual = textActual.ToString();
                        ddi.PregActual = pregActual.ToString();

                        ini = Convert.ToDateTime(ds.Momento);
                        lectIni = true;

                        continue;
                    }
                }

                ViewBag.tAciertos = tAciertos;
                ViewBag.tBusca = tBusca;
                ViewBag.tAyudas = tAyudas;
                ViewBag.tRevisa = tRevisa;
                
                #endregion

                foreach (DatosDetalladoIndep dat in lista)
                {
                    datos += dat.TextActual + "+";
                    datos += dat.LectInic + "+";
                    datos += dat.PregActual + "+";
                    datos += dat.Acierto + "+";
                    datos += dat.Busca + "+";
                    datos += dat.Ayuda1 + "+";
                    datos += dat.Ayuda2 + "+";
                    datos += dat.Ayuda3 + "+";
                    datos += dat.Revisa + "+";
                    datos += dat.TareaSel + "+";
                    datos += dat.PorcPert + "+";
                    datos += dat.PorcNoPert + "+";
                    datos += dat.TmpPreg + "#";
                }

                datos = datos.Remove(datos.Length - 1);
                
                #region Análisis Medias

                int totalAciertos = 0;
                int totalBusca = 0;
                int totalAyuda = 0;
                int totalRevisa = 0;

                double medAciertos = 0;
                double medBusca = 0;
                double medAyuda = 0;
                double medRevisa = 0;

                var user = getCurrentUser();

                var alumnos = from a in db.UserProfiles
                               from g in db.Grupos
                               from p in g.Propietarios
                               from i in db.Inscripciones
                               from du in db.DatosUsuario
                               where a.Type == "Alumno" &&
                               p.UserId == user.UserId &&
                               i.GrupoID == g.GrupoID &&
                               g.GrupoID == GrupoID &&
                               du.ModuloID == ModuloID &&
                               du.Cerrada == true &&
                               i.Alumno.UserId == a.UserId &&
                               du.UserProfileID == a.UserId
                               select a;

                foreach (UserProfile id in alumnos)
                {
                    usaAyuda = false;

                    var listMed = (from d in db.DatosSimples
                                  from u in db.UserProfiles
                                  from du in db.DatosUsuario
                                  where d.DatosUsuarioID == du.DatosUsuarioID &&
                                  du.UserProfileID == id.UserId &&
                                  du.ModuloID == ModuloID &&
                                  du.GrupoID == GrupoID
                                  select d).Distinct().ToList();

                    foreach (DatoSimple ds in listMed)
                    {
                        if (ds.CodeOP == 5)
                        {
                            totalAciertos += ds.Valor;                            
                            continue;
                        }

                        if (ds.CodeOP == 16)
                        {
                            totalBusca++;
                            continue;
                        }

                        if (ds.CodeOP == 17)
                        {
                            totalRevisa++;
                            continue;
                        }

                        if (ds.CodeOP == 15 && !usaAyuda)
                        {   
                            totalAyuda++;

                            usaAyuda = true;
                            continue;
                        }

                        if (ds.CodeOP == 10) // Siguiente Pregunta
                        {
                            usaAyuda = false;                            
                            continue;
                        }

                        if (ds.CodeOP == 50) // Cambio de Texto
                        {
                            usaAyuda = false;
                            continue;
                        }

                        if (ds.CodeOP == 60)
                        {
                            usaAyuda = false;
                            continue;
                        }
                    }

                }

                ViewBag.medAciertos = totalAciertos / alumnos.Count();
                ViewBag.medAyuda = totalAyuda / alumnos.Count();
                ViewBag.medBusca = totalBusca / alumnos.Count();
                ViewBag.medRevisa = totalRevisa / alumnos.Count();

                #endregion

            }

            ViewBag.Datos = datos;

            return View();
        }

        private class DatosDetalladoMod
        {
            public string Nombre { get; set; }
            public string Usuario { get; set; }

            public string PregId { get; set; }
            public string PregActual { get; set; }
            public string TipoPreg { get; set; }
            

            public string Acierto { get; set; }
            public string PorcBien { get; set; }
            public string PorcMal { get; set; }
            public string Seleccion { get; set; }

            public string Tiempo { get; set; }
            public string TmpPreg { get; set; }
        }


        private class DatosDetalladoIndep
        {
            public string Nombre { get; set; }
            public string Usuario { get; set; }

            public string TextActual { get; set; }
            public string PregActual { get; set; }

            public string PreguntaID { get; set; }

            public string LectInic { get; set; }
            public string Acierto { get; set; }
            public string Busca { get; set; }
            public string Ayuda1 { get; set; }
            public string Ayuda2 { get; set; }
            public string Ayuda3 { get; set; }
            public string Revisa { get; set; }

            public string TareaSel { get; set; }
            public string PorcPert { get; set; }
            public string PorcNoPert { get; set; }

            public string TmpPreg { get; set; }
        }

        public List<DatosPregunta> AnalizaIndependient(List<DatoSimple> lista, int GrupoID, int ModuloID, int UserId)
        {
            List<DatosPregunta> listaPreg = new List<DatosPregunta>();

            DateTime ini = new DateTime();
            DateTime fin = new DateTime();
            int pregActual = 0;
            int textActual = 0;

            int PregID = 0;

            bool lecIni = false;
            bool busca = false;
            bool ayuda = false;
            bool revisa = false;
            bool acierto = false;

            foreach (DatoSimple ds in lista)
            {
                if (lecIni)
                {
                    fin = Convert.ToDateTime(ds.Momento);
                    ini = Convert.ToDateTime(ds.Momento);
                    lecIni = false;
                }

                if (ds.CodeOP == 1)
                {
                    ini = Convert.ToDateTime(ds.Momento);
                    lecIni = true;
                    continue;
                }

                if (ds.CodeOP == 15)
                {
                    ayuda = true;
                    continue;
                }

                if (ds.CodeOP == 16)
                {
                    busca = true;
                    continue;
                }

                if (ds.CodeOP == 17)
                {
                    revisa = true;
                    continue;
                }

                if (ds.CodeOP == 10) // Siguiente Pregunta
                {
                    fin = Convert.ToDateTime(ds.Momento);

                    Double tmp = (fin - ini).TotalMilliseconds;

                    listaPreg.Add(new DatosPregunta() { PreguntaID = PregID, Acierto = Convert.ToInt32(acierto), Ayuda = Convert.ToInt32(ayuda), Busca = Convert.ToInt32(busca), Revisa = Convert.ToInt32(revisa), Texto = textActual, Pregunta = pregActual, Tiempo = tmp });

                    revisa = false;
                    busca = false;
                    ayuda = false;

                    ini = Convert.ToDateTime(ds.Momento);
                    pregActual++;
                    continue;
                }

                if (ds.CodeOP == 5)
                {
                    acierto = Convert.ToBoolean(ds.Valor);
                    PregID = ds.PreguntaID;
                    continue;
                }

                if (ds.CodeOP == 50) // Cambio de Texto
                {
                    fin = Convert.ToDateTime(ds.Momento);

                    Double tmp = (fin - ini).TotalMilliseconds;

                    listaPreg.Add(new DatosPregunta() { PreguntaID = PregID, Acierto = Convert.ToInt32(acierto), Ayuda = Convert.ToInt32(ayuda), Busca = Convert.ToInt32(busca), Revisa = Convert.ToInt32(revisa), Texto = textActual, Pregunta = pregActual, Tiempo = tmp });

                    revisa = false;
                    busca = false;
                    ayuda = false;

                    ini = Convert.ToDateTime(ds.Momento);

                    textActual++;
                    pregActual = 0;

                    revisa = false;
                    busca = false;
                    ayuda = false;
                    acierto = false;
                    continue;
                }

                if (ds.CodeOP == 60)
                {
                    fin = Convert.ToDateTime(ds.Momento);

                    Double tmp = (fin - ini).TotalMilliseconds;

                    listaPreg.Add(new DatosPregunta() { PreguntaID = PregID, Acierto = Convert.ToInt32(acierto), Ayuda = Convert.ToInt32(ayuda), Busca = Convert.ToInt32(busca), Revisa = Convert.ToInt32(revisa), Texto = textActual, Pregunta = pregActual, Tiempo = tmp });
                    continue;
                }

            }

            return listaPreg;
        }

        public ActionResult SeguimientosAlumnosAcabado(int GrupoID, int ModuloID)
        {
            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;

            ViewBag.Modulo = db.Modulos.Find(ModuloID);

            var user = getCurrentUser();

            var alumnos = (from a in db.UserProfiles
                           from g in db.Grupos
                           from p in g.Propietarios
                           from i in db.Inscripciones
                           from du in db.DatosUsuario
                           where a.Type == "Alumno" &&
                           p.UserId == user.UserId &&
                           i.GrupoID == g.GrupoID &&
                           g.GrupoID == GrupoID &&
                           du.ModuloID == ModuloID &&
                           du.Cerrada == true &&
                           i.Alumno.UserId == a.UserId &&
                           du.UserProfileID == a.UserId
                           select new { a.UserName, a.Nombre, du.AccionActual, g.GrupoID, du.ModuloID, a.UserId }).OrderBy(m => m.ModuloID);

            List<Datos> Lista = new List<Datos>();

            foreach (var tmp in alumnos)
            {
                var listDu = from d in db.DatosUsuario
                         where d.UserProfileID == tmp.UserId &&
                         d.ModuloID == ModuloID && d.GrupoID == GrupoID
                         select d;

                foreach (DatosUsuario du in listDu)
                {
                    Datos newDat = new Datos();
                    DateTime dt = new DateTime();

                    var sec = from ds in db.DatosSimples
                              where ds.DatosUsuarioID == du.DatosUsuarioID
                              select ds;

                    if (sec.Count() > 0)
                    {
                        string tiempo = (from ds in db.DatosSimples
                                         where ds.DatosUsuarioID == du.DatosUsuarioID
                                         select ds).ToList().Last().Momento.ToString();

                        dt = Convert.ToDateTime(tiempo);
                    }

                    newDat.Fecha = du.Inicio.ToString();
                    newDat.Nombre = du.UserProfile.Nombre;
                    newDat.Usuario = du.UserProfile.UserName;

                    newDat.Puntos = du.Puntos.ToString();
                    newDat.Acciones = du.AccionActual.ToString();
                    newDat.AyudaNeg = du.AyudaNeg.ToString();
                    newDat.AyudaPos = du.AyudaPos.ToString();
                    newDat.BuscaNeg = du.BuscaNeg.ToString();
                    newDat.BuscaPos = du.BuscaPos.ToString();
                    newDat.RespuestaNeg = du.RespuestaNeg.ToString();
                    newDat.RespuestaPos = du.RespuestaPos.ToString();
                    newDat.RevisaNeg = du.RevisaNeg.ToString();
                    newDat.RevisaPos = du.RevisaPos.ToString();
                    newDat.UserId = du.UserProfileID;

                    TimeSpan ts = dt - Convert.ToDateTime(du.Inicio);

                    newDat.TiempoTotal = ts.TotalSeconds.ToString();

                    Lista.Add(newDat);
                }
            }

            return View(Lista.ToList());
        }

        public ActionResult SeguimientosAlumnosIndependiente(int GrupoID, int ModuloID)
        {
            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;

            var user = getCurrentUser();

            var alumnos = (from a in db.UserProfiles
                           from g in db.Grupos
                           from p in g.Propietarios
                           from i in db.Inscripciones
                           from du in db.DatosUsuario
                           where a.Type == "Alumno" &&
                           p.UserId == user.UserId &&
                           i.GrupoID == g.GrupoID &&
                           g.GrupoID == GrupoID &&
                           du.ModuloID == ModuloID &&
                           du.Cerrada == false &&
                           i.Alumno.UserId == a.UserId &&
                           du.UserProfileID == a.UserId
                           select new { a.UserName, a.Nombre, du.AccionActual, g.GrupoID, du.ModuloID }).OrderBy(m => m.ModuloID);

            List<SeguimientosIndependiente> Lista = new List<SeguimientosIndependiente>();

            int preg = 0;

            foreach (Texto txt in db.Modulos.Find(ModuloID).Textos)
            {
                preg += txt.Preguntas.Count;
            }

            foreach (var tmp in alumnos)
            {
                var userA = (from u in db.UserProfiles
                             where u.UserName == tmp.UserName
                             select u).Single();

                var listDu = (from du in db.DatosUsuario
                             where du.UserProfileID == userA.UserId &&
                             du.ModuloID == ModuloID &&
                             du.GrupoID == GrupoID
                             select du).Single();

                Lista.Add(new SeguimientosIndependiente()
                {
                    UserName = tmp.UserName,
                    Nombre = tmp.Nombre,
                    EstadoActual = listDu.PreguntaActual + listDu.TextoActual * listDu.PreguntaActual,
                    EstadoMaximo = preg,
                    GrupoID = tmp.GrupoID,
                    ModuloID = tmp.ModuloID,
                    Aciertos = listDu.RespuestaPos,
                    Errores = listDu.RespuestaNeg
                });
            }

            return View(Lista.ToList());
        }

        public ActionResult SeguimientosAlumnos(int GrupoID, int ModuloID)
        {
            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;
            var user = getCurrentUser();

            var alumnos = (from a in db.UserProfiles
                           from g in db.Grupos
                           from p in g.Propietarios
                           from i in db.Inscripciones
                           from du in db.DatosUsuario
                           where a.Type == "Alumno" &&
                           p.UserId == user.UserId &&
                           i.GrupoID == g.GrupoID &&
                           g.GrupoID == GrupoID &&
                           du.ModuloID == ModuloID &&
                           du.Cerrada == false &&
                           i.Alumno.UserId == a.UserId &&
                           du.UserProfileID == a.UserId
                           select new { a.UserName, a.Nombre, du.AccionActual, g.GrupoID, du.ModuloID }).OrderBy(m => m.ModuloID);

            List<Seguimientos> Lista = new List<Seguimientos>();

            foreach (var tmp in alumnos)
            {
                var flag_rapido = false;
                var flag_error = false;

                var acciones = from a in db.Acciones
                               from e in db.Escenas
                               where a.EscenaID == e.EscenaID &&
                               e.ModuloID == ModuloID
                               select a;

                // Este rango se cambiará por la media cuando la muestra de usuarios sea lo suficientemente grande.
                int rngAcc = 15;
                Int64 tiempo = 5; // 5 minutos en hacer 15 acciones
                int etapa = tmp.AccionActual / rngAcc;

                if (etapa == 1)
                {
                    try
                    {
                        var alumno = db.UserProfiles.ToList().Find(a => a.UserName == tmp.UserName);

                        var inicio = (from du in db.DatosUsuario
                                      where du.ModuloID == ModuloID &&
                                      du.GrupoID == GrupoID &&
                                      du.Cerrada == false &&
                                      alumno.UserId == du.UserProfileID
                                      select du.Inicio).First();

                        var tmpEtapa = (from ds in db.DatosSimples
                                        from du in db.DatosUsuario
                                        where ds.DatosUsuarioID == du.DatosUsuarioID &&
                                        ds.CodeOP == 3 && ds.Dato01 > ((etapa) * rngAcc - 1) &&
                                        du.ModuloID == ModuloID &&
                                        du.GrupoID == GrupoID &&
                                        du.UserProfileID == alumno.UserId
                                        select ds).OrderBy(ds => ds.DatoSimpleID);

                        DatoSimple dts = tmpEtapa.First();

                        var fin = dts.Momento;

                        var time = fin.Subtract(inicio);

                        if (time.Ticks < (tiempo * 600000000)) // Si se cumnple, va demasiado rápido.
                        {
                            flag_rapido = true;
                        }
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                }
                else
                {
                    try
                    {
                        var alumno = db.UserProfiles.ToList().Find(a => a.UserName == tmp.UserName);

                        if (etapa > 1)
                        {
                            var tmpEtapa = (from ds in db.DatosSimples
                                            from du in db.DatosUsuario
                                            where ds.DatosUsuarioID == du.DatosUsuarioID &&
                                            ds.CodeOP == 3 && ds.Dato01 > ((etapa - 1) * rngAcc) &&
                                            du.UserProfileID == alumno.UserId
                                            select ds).OrderBy(ds => ds.DatoSimpleID);

                            var tmpEtapa2 = (from ds in db.DatosSimples
                                             from du in db.DatosUsuario
                                             where ds.DatosUsuarioID == du.DatosUsuarioID &&
                                             ds.CodeOP == 3 && ds.Dato01 > (etapa * rngAcc - 1) &&
                                             du.ModuloID == ModuloID &&
                                             du.GrupoID == GrupoID &&
                                             du.UserProfileID == alumno.UserId
                                             select ds).OrderBy(ds => ds.DatoSimpleID);

                            DatoSimple dts = tmpEtapa.First();
                            DatoSimple dts2 = tmpEtapa2.First();

                            var inicio = dts.Momento;
                            var fin = dts2.Momento;

                            var time = fin.Subtract(inicio);

                            if (Convert.ToInt64(time.Ticks) < Convert.ToInt64(tiempo * 600000000)) // Si se cumnple, va demasiado rápido.
                            {
                                flag_rapido = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                }

                float numAcc = acciones.Count();

                float num = tmp.AccionActual;
                float porc = (num * 100) / numAcc;

                var alumn = db.UserProfiles.ToList().Find(a => a.UserName == tmp.UserName);

                var respuest = (from ds in db.DatosSimples
                                from du in db.DatosUsuario
                                where ds.DatosUsuarioID == du.DatosUsuarioID &&
                                ds.CodeOP == 4 &&
                                du.ModuloID == ModuloID &&
                                du.GrupoID == GrupoID &&
                                du.UserProfileID == alumn.UserId
                                select ds).OrderBy(ds => ds.DatoSimpleID);

                var aciertos = from ds in db.DatosSimples
                               from du in db.DatosUsuario
                               where ds.DatosUsuarioID == du.DatosUsuarioID &&
                               ds.CodeOP == 4 &&
                               du.ModuloID == ModuloID &&
                               du.GrupoID == GrupoID &&
                               ds.Valor == 1 &&
                               du.UserProfileID == alumn.UserId
                               select ds;

                try
                {
                    if (respuest.Count() > 3)
                    {
                        float total = respuest.Count();
                        float ac = aciertos.Count();

                        if ((ac / total) < 0.45)
                        {
                            flag_error = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    flag_error = false;
                }

                Lista.Add(new Seguimientos()
                {
                    UserName = tmp.UserName,
                    Nombre = tmp.Nombre,
                    AccionActual = porc,
                    GrupoID = tmp.GrupoID,
                    ModuloID = tmp.ModuloID,
                    AvisoVel = flag_rapido
                });
            }

            return View(Lista.ToList());
        }

        /*
         *                         
                <td>
                    <div class="container"><div id="@id" class="progressbar"></div></div>
                </td>
         * */

        [HttpPost]
        public ActionResult ActualizarSeguimiento(int GrupoID, int ModuloID)
        {
            var user = getCurrentUser();

            var alumnos = (from a in db.UserProfiles
                           from g in db.Grupos
                           from p in g.Propietarios
                           from i in db.Inscripciones
                           from du in db.DatosUsuario
                           where a.Type == "Alumno" &&
                           p.UserId == user.UserId &&
                           i.GrupoID == g.GrupoID &&
                           g.GrupoID == GrupoID &&
                           du.ModuloID == ModuloID &&
                           du.Cerrada == false &&
                           i.Alumno.UserId == a.UserId &&
                           du.UserProfileID == a.UserId
                           select new { a.UserName, a.Nombre, du.AccionActual, g.GrupoID, du.ModuloID }).OrderBy(m => m.ModuloID);

            List<Seguimientos> Lista = new List<Seguimientos>();
            
            foreach (var tmp in alumnos)
            {
                try
                {
                    var flag_rapido = false;
                    var flag_error = false;

                    var acciones = from a in db.Acciones
                                   from e in db.Escenas
                                   where a.EscenaID == e.EscenaID &&
                                   e.ModuloID == ModuloID
                                   select a;

                    // Este rango se cambiará por la media cuando la muestra de usuarios sea lo suficientemente grande.
                    int rngAcc = 15;
                    Int64 tiempo = 3; // 3 minutos en hacer 15 acciones
                    int etapa = tmp.AccionActual / rngAcc;

                    if (etapa == 1)
                    {
                        var alumno = db.UserProfiles.ToList().Find(a => a.UserName == tmp.UserName);

                        var inicio = (from du in db.DatosUsuario
                                      where du.ModuloID == ModuloID &&
                                      du.GrupoID == GrupoID &&
                                      du.Cerrada == false &&
                                      alumno.UserId == du.UserProfileID
                                      select du.Inicio).First();

                        var tmpEtapa = (from ds in db.DatosSimples
                                        from du in db.DatosUsuario
                                        where ds.DatosUsuarioID == du.DatosUsuarioID &&
                                        ds.CodeOP == 3 && ds.Dato01 > ((etapa) * rngAcc - 1) &&
                                        du.ModuloID == ModuloID &&
                                        du.GrupoID == GrupoID &&
                                        du.UserProfileID == alumno.UserId
                                        select ds).OrderBy(ds => ds.DatoSimpleID);

                        DatoSimple dts = tmpEtapa.First();

                        var fin = dts.Momento;

                        var time = fin.Subtract(inicio);

                        if (time.Ticks < (tiempo * 600000000)) // Si se cumnple, va demasiado rápido.
                        {
                            flag_rapido = true;
                        }
                    }
                    else
                    {
                        var alumno = db.UserProfiles.ToList().Find(a => a.UserName == tmp.UserName);

                        if (etapa > 1)
                        {
                            var tmpEtapa = (from ds in db.DatosSimples
                                            from du in db.DatosUsuario
                                            where ds.DatosUsuarioID == du.DatosUsuarioID &&
                                            ds.CodeOP == 3 && ds.Dato01 > ((etapa - 1) * rngAcc) &&
                                            du.UserProfileID == alumno.UserId
                                            select ds).OrderBy(ds => ds.DatoSimpleID);

                            var tmpEtapa2 = (from ds in db.DatosSimples
                                             from du in db.DatosUsuario
                                             where ds.DatosUsuarioID == du.DatosUsuarioID &&
                                             ds.CodeOP == 3 && ds.Dato01 > (etapa * rngAcc - 1) &&
                                             du.ModuloID == ModuloID &&
                                             du.GrupoID == GrupoID &&
                                             du.UserProfileID == alumno.UserId
                                             select ds).OrderBy(ds => ds.DatoSimpleID);

                            DatoSimple dts = tmpEtapa.First();
                            DatoSimple dts2 = tmpEtapa2.First();

                            var inicio = dts.Momento;
                            var fin = dts2.Momento;

                            var time = fin.Subtract(inicio);

                            if (Convert.ToInt64(time.Ticks) < Convert.ToInt64(tiempo * 600000000)) // Si se cumnple, va demasiado rápido.
                            {
                                flag_rapido = true;
                            }
                        }
                    }

                    float numAcc = acciones.Count();

                    float num = tmp.AccionActual;
                    float porc = (num * 100) / numAcc;

                    var alumn = db.UserProfiles.ToList().Find(a => a.UserName == tmp.UserName);

                    var respuest = (from ds in db.DatosSimples
                                    from du in db.DatosUsuario
                                    where ds.DatosUsuarioID == du.DatosUsuarioID &&
                                    ds.CodeOP == 4 &&
                                    du.ModuloID == ModuloID &&
                                    du.GrupoID == GrupoID &&
                                    du.UserProfileID == alumn.UserId
                                    select ds).OrderBy(ds => ds.DatoSimpleID);

                    var aciertos =  from ds in db.DatosSimples
                                    from du in db.DatosUsuario
                                    where ds.DatosUsuarioID == du.DatosUsuarioID &&
                                    ds.CodeOP == 4 &&
                                    du.ModuloID == ModuloID &&
                                    du.GrupoID == GrupoID &&
                                    ds.Valor == 1 &&
                                    du.UserProfileID == alumn.UserId                                    
                                    select ds;

                    try
                    {
                        if (respuest.Count() > 3)
                        {
                            float total = respuest.Count();
                            float ac = aciertos.Count();

                            if ((ac / total) < 0.45)
                            {
                                flag_error = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        flag_error = false;
                    }

                    Lista.Add(new Seguimientos()
                    {
                        UserName = tmp.UserName,
                        Nombre = tmp.Nombre,
                        AccionActual = porc,
                        GrupoID = tmp.GrupoID,
                        ModuloID = tmp.ModuloID,
                        AvisoVel = flag_rapido,
                        AvisoErr = flag_error
                    });
                }
                catch (Exception e)
                {
                    continue;
                }
            }

            return Json(new { datos = Lista.ToList(), redirect = Url.Action("SeguimientosAlumnos", "Docente", new { GrupoID = GrupoID, ModuloID = ModuloID }) });            
        }

        public ActionResult SacarSecuencias()
        {
            var users = from u in db.UserProfiles
                        select u;

            int numUser = 0;

            foreach (UserProfile user in users)
            {

                var datos = from du in db.DatosUsuario
                            where user.UserId == du.UserProfileID
                            select du;

                foreach (DatosUsuario datosUsuario in datos)
                {
                    

                    
                    
                    //guirisan code to fix slash problems in path
                    string username = user.UserName.Replace("/", "_");
                    
                    //esta lína no hace falta salvo que haya barras invertidas \ en el nombre de usuario. Ya serian ganas de tocar la moral...
                    //ATENCIÓN: no funciona tal cual! hay que ver como inclur una barra invertida como string sin que la tome como un 
                    //carácter de control para anular el siguiente carácter (puede que dos barras invertidas?)
                    //string username = user.UserName.Replace("'\'", "_");

                    string fileLoc = @"C:\inetpub\wwwroot\Secuencias\" + username + "_" + user.UserId + "_G" + datosUsuario.GrupoID + "_M" + datosUsuario.ModuloID + ".txt";

                    if (!System.IO.File.Exists(fileLoc))
                    {
                        var secuencia = from s in db.DatosSimples
                                        where s.DatosUsuarioID == datosUsuario.DatosUsuarioID
                                        orderby s.DatoSimpleID ascending
                                        select s;

                        numUser++;
                        FileStream fs = null;
                        if (!System.IO.File.Exists(fileLoc))
                        {
                            using (fs = System.IO.File.Create(fileLoc))
                            {
                                int cont = 1;
                                StreamWriter m_streamWriter = new StreamWriter(fs);

                                foreach (DatoSimple acc in secuencia)
                                {
                                    //guirisan//issues https://github.com/guirisan/ituinbook/issues/47
                                    //string linea = (cont++).ToString() + "_" + acc.NumAccion +"_" + acc.DatoSimpleID + "_" + acc.PreguntaID + "_" + acc.Momento + "_" + acc.CodeOP + "_" + acc.Valor + "_" + acc.Dato01 + "_" + acc.Dato02 + "_" + acc.Dato03 + "_" + acc.Info + "\n";
                                    string linea = "";
                                    if (acc.PreguntaID > 0)
                                    {
                                        //para los datos que tienen una pregunta a partir de la cual saber el texto, la página...
                                        Pregunta preg = db.Preguntas.Find(acc.PreguntaID);
                                        //linea = (cont++).ToString() + "_" + acc.NumAccion + "_" + acc.DatoSimpleID + "_" + preg.Texto.TextoID + "_" + preg.Texto.Orden + "_" + preg.Pagina.PaginaID + "_" + preg.Pagina.Orden + "_" + acc.PreguntaID + "_" + preg.Orden + "_" + acc.Momento + "_" + acc.CodeOP + "_" + acc.Valor + "_" + acc.Dato01 + "_" + acc.Dato02 + "_" + acc.Dato03 + "_" + acc.Info + "\n";
                                        linea = (cont++).ToString() + "_" ;
                                        linea += acc.NumAccion + "_";
                                        linea += acc.DatoSimpleID + "_" ;
                                        linea += preg.Texto.TextoID + "_" ;
                                        linea += "TX" + preg.Texto.Orden + "_" ;
                                        //comentem la pàgina perque CAP pregunta de la BD té una pàgina assignada
                                        //linea += preg.Pagina.PaginaID + "_" ;
                                        //linea += preg.Pagina.Orden + "_" ;
                                        linea += acc.PreguntaID + "_" ;
                                        linea += "PR" + preg.Orden + "_" ;
                                        //issue 
                                        //imprimir milisegons en el moment
                                        linea += acc.Momento.ToString("dd/MM/yyy hh:mm:ss.fff") + "_";

                                        linea += acc.CodeOP + "_" ;
                                        linea += acc.Valor + "_" ;
                                        linea += acc.Dato01 + "_" ;
                                        linea += acc.Dato02 + "_" ;
                                        linea += acc.Dato03 + "_";
                                        linea += acc.Info2 + "_";
                                        linea += acc.Info + "\n";

                                    }
                                    else
                                    {
                                        //para los datos que NO tienen una pregunta
                                        linea = (cont++).ToString() + "_";
                                        linea += acc.NumAccion + "_";
                                        linea += acc.DatoSimpleID + "_";
                                        linea += "NULL" + "_";
                                        linea += "NULL" + "_";
                                        linea += "NULL" + "_";
                                        linea += "NULL" + "_";
                                        linea += acc.Momento.ToString("dd/MM/yyy hh:mm:ss.fff") + "_";
                                        linea += acc.CodeOP + "_";
                                        linea += acc.Valor + "_";
                                        linea += acc.Dato01 + "_";
                                        linea += acc.Dato02 + "_";
                                        linea += acc.Dato03 + "_";
                                        linea += acc.Info2 + "_";
                                        linea += acc.Info + "\n";

                                    }
                                    
                                    
                                    /*
                                    string linea = (cont++).ToString() + "_" +
                                    acc.NumAccion +"_" +
                                    acc.DatoSimpleID + "_" +

                                    //identificador absoluto del texto
                                    preg.Texto.TextoID+ "_" +
                                    //identificador del texto respecto al módulo
                                    preg.Texto.Orden + "_" + 

                                    //identificador absoluto de la pagina
                                    preg.Pagina.PaginaID + "_" +
                                    //identificador de la página respecto al texto
                                    preg.Pagina.Orden + "_" + 

                                    //identificador absoluto de pregunta
                                    acc.PreguntaID + "_" +
                                    //identificador relativo de pregunta
                                    preg.Orden + "_" +

                                    acc.Momento + "_" +
                                    acc.CodeOP + "_" +
                                    acc.Valor + "_" +
                                    acc.Dato01 + "_" +
                                    acc.Dato02 + "_" +
                                    acc.Dato03 + "_" +
                                    acc.Info + "\n";
                                    */

                                    
                                    m_streamWriter.WriteLine(linea);
                                }

                                m_streamWriter.Flush();
                            }

                            fs.Close();
                        }
                    }
                
                
                }
                
            }

            ViewBag.Numero = numUser;

            return View();
        }

        public ActionResult Analizador()
        {
            UserProfile user = getCurrentUser();

            var grupos = from g in db.Grupos
                         from d in g.Propietarios
                         where d.UserId == user.UserId
                         select g;

            var modulos = from m in db.Modulos
                          where user.UserId == m.Propiedad
                          select m;

            List<SelectListItem> MisGrupos = new List<SelectListItem>();

            foreach (Grupo grp in grupos)
            {
                MisGrupos.Add(new SelectListItem { Text = grp.Nombre, Value = grp.GrupoID.ToString() });
            }

            ViewBag.Grupos = MisGrupos;

            ViewBag.Variables = GetVariables();

            return View();
        }

        [HttpPost]
        public ActionResult GetModulosDeGrupo(int GrupoID)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Analisis(string varSelected,string dateIni, string dateFin, int? GrupodID)
        {
            UserProfile user = getCurrentUser();

            DateTime inicio = new DateTime(1900, 1, 1);
            DateTime final = DateTime.Now;

            if (dateIni != "")
            {
                string[] param = dateIni.Split('/');

                inicio = new DateTime(Convert.ToInt32(param[2]), Convert.ToInt32(param[0]), Convert.ToInt32(param[1]));
            }

            if (dateFin != "")
            {
                string[] param = dateFin.Split('/');

                final = new DateTime(Convert.ToInt32(param[2]), Convert.ToInt32(param[0]), Convert.ToInt32(param[1]));
            }

            string[] variables = varSelected.Split('/');

            var sujetos = from sec in db.DatosUsuario
                          from gru in db.Grupos
                          from pro in gru.Propietarios
                          where sec.GrupoID == gru.GrupoID &&
                          sec.Inicio <= final &&
                          sec.Inicio >= inicio &&
                          pro.UserId == user.UserId/* &&
                          sec.UserProfileID == 2491*/
                          select sec;

            if (GrupodID != null)
            {
                sujetos = from d in sujetos
                          where d.GrupoID == GrupodID
                          select d;                
            }

            //var filtro1 = todos.Select(grp => grp.GrupoID == GrupodID);
            

            /*
            if (GrupodID != null)
            {

                var sujetos = from sec in db.DatosUsuario
                              from gru in db.Grupos
                              from pro in gru.Propietarios
                              where sec.GrupoID == GrupodID &&
                              sec.Inicio <= final &&
                              sec.Inicio >= inicio &&
                              sec.GrupoID == gru.GrupoID &&
                              pro.UserId == user.UserId
                              select sec;
            }
            else
            {
                var sujetos = from sec in db.DatosUsuario
                              from gru in db.Grupos
                              from pro in gru.Propietarios
                              where sec.GrupoID == gru.GrupoID &&
                              sec.Inicio <= final &&
                              sec.Inicio >= inicio &&                              
                              pro.UserId == user.UserId
                              select sec;
            }
            */

            List<DatoSujeto> Matriz = ExtraerDatos(sujetos.ToList());
            return Json(JsonConvert.SerializeObject(Matriz));             
        }

        private List<DatoSujeto> ExtraerDatos(List<DatosUsuario> list)
        {
            List<DatoSujeto> arrayDatos = new List<DatoSujeto>();
            bool flag_segIntento = false;

            foreach(DatosUsuario du in list)
            {
                /*if (du.DatosUsuarioID != 4473)
                    continue;*/

                DatoSujeto suj = new DatoSujeto();
                logger.Debug("EXTRAER DATOS: {0}", suj.Nombre);
                try
                {
                    List<DatoUnitario> datos = new List<DatoUnitario>();
                    DateTime IniMod = new DateTime();

                    suj.UserID = du.UserProfileID;
                    suj.Nombre = db.UserProfiles.Find(du.UserProfileID).Nombre;

                    DatoUnitario datoUnit = new DatoUnitario();
                    DatoUnitario datoUnitTexto = new DatoUnitario();
                    datoUnit.Intento = 1;

                    foreach (DatoSimple ds in du.DatoSimple)
                    {
                        if (datoUnit.PreguntaID == 0)
                            datoUnit.PreguntaID = ds.PreguntaID;

                        switch (ds.CodeOP) // En pregunta, las variables empiezan en el 8
                        {
                            case 1: // Empieza el módulo
                                IniMod = ds.Momento;
                                break;
                            case 2:
                                int e = 3;
                                break;
                            case 3:
                                int ed = 3;
                                break;
                            case 5:
                                datoUnitTexto.TiempoLecIni = Convert.ToDouble(ds.Info, CultureInfo.InvariantCulture) / 1000.0;
                                break;
                            case 6:
                                datoUnitTexto.VelLecIni = Convert.ToDouble(ds.Info, CultureInfo.InvariantCulture);
                                break;
                            case 8:
                                datoUnit.TiempoTotalPregunta = Convert.ToDouble(ds.Info, CultureInfo.InvariantCulture) / 1000.0;
                                break;
                            case 9: // Segundo intento (almacenar datos hasta ahora y empezar a recoger nuevos)
                                datos.Add(datoUnit);

                                DatoUnitario datoTmp = new DatoUnitario();

                                datoTmp.PreguntaID = datoUnit.PreguntaID;
                                datoTmp.Intento = 2;

                                datoUnit = new DatoUnitario();

                                datoUnit = datoTmp;

                                flag_segIntento = true;
                                break;
                            case 10:
                                datoUnit.Inicio = ds.Momento;
                                break;
                            case 11:
                                datoUnit.Final = ds.Momento;
                                break;
                            case 13: // Porcentje de acierto
                                datoUnit.RespDada = ds.Info;
                                datoUnit.PorcAcierto = ds.Dato01;
                                break;
                            case 14:
                                datoUnit.NumCambiosResp++;
                                break;
                            case 16: // Número de veces que lee el enunciado
                                datoUnit.NumEnun++;
                                break;
                            case 17: // Número de veces que lee el enunciado
                                datoUnit.NumAlte++;
                                break;
                            case 18:
                                datoUnit.TiempoEnum += ds.Dato01;
                                break;
                            case 19: // Tiempo leyendo alternativas
                                datoUnit.TiempoAlte += ds.Dato01;
                                break;
                            case 24:
                                datoUnit.NumBusq++;
                                break;
                            case 28: // UltPertinente
                                datoUnit.UltPert = 1;
                                break;
                            case 42: // PenUltimoPerti
                                datoUnit.PenUltPert = 1;
                                break;
                            case 29:
                                datoUnit.PorcPertEncTotal = ds.Dato01;
                                break;
                            case 30:
                                datoUnit.PorcPertEncBusqueda = ds.Dato01;
                                break;
                            case 40:
                                datoUnit.OrdenBusqueda += ds.Info2 + ":";
                                break;
                            case 41:
                                datoUnit.Continuidad += ds.Info2 + ":";
                                break;
                            case 44:
                                if (flag_segIntento)
                                {
                                    datos.Last().TiempoLecFDBK = Convert.ToDouble(ds.Info, CultureInfo.InvariantCulture) / 1000.0;
                                    flag_segIntento = false;
                                }
                                else
                                {
                                    datoUnit.TiempoLecFDBK = Convert.ToDouble(ds.Info, CultureInfo.InvariantCulture) / 1000.0;
                                }
                                break;
                            case 45:
                                if (flag_segIntento)
                                {
                                    datos.Last().VelLecFDBK = Convert.ToDouble(ds.Info, CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    datoUnit.VelLecFDBK = Convert.ToDouble(ds.Info, CultureInfo.InvariantCulture);
                                }
                                break;
                            case 48:
                                datoUnit.PorcNoPertSubTarea = ds.Dato01;
                                break;
                            case 49:
                                datoUnit.PorcPertSubTarea = ds.Dato01;
                                break;
                            case 50:
                                datoUnit.PorcNoPertTarea = ds.Dato01;
                                break;
                            case 51:
                                datoUnit.PorcPertTarea = ds.Dato01;
                                break;
                            case 52:

                                break;
                            case 100:
                            case 101:
                            case 102:
                                // Pasar datosUnitarios de texto a pregunta
                                if (datoUnit.TiempoTotalPregunta == 0)
                                {
                                    DateTime tag = new DateTime();

                                    if (datoUnit.Final != tag && datoUnit.Inicio != tag)
                                    {
                                        datoUnit.TiempoTotalPregunta = (datoUnit.Final - datoUnit.Inicio).Seconds;
                                    }
                                }
                                
                                datoUnit.InicioTexto = datoUnitTexto.InicioTexto;
                                datoUnit.FinalTexto = datoUnitTexto.FinalTexto;
                                datoUnit.PorcLectIni = datoUnitTexto.PorcLectIni;
                                datoUnit.TiempoLecIni = datoUnitTexto.TiempoLecIni;
                                datoUnit.VelLecIni = datoUnitTexto.VelLecIni;
                                datoUnit.Continuidad = datoUnitTexto.Continuidad;
                                
                                datos.Add(datoUnit);

                                datoUnit = new DatoUnitario();
                                datoUnit.Intento = 1;
                                break;
                        }
                    }

                    suj.datos = datos;

                    arrayDatos.Add(suj);
                }
                catch (Exception e)
                {
                    logger.Debug("ERROR EXTRAYENDO DATOS: {0}", suj.Nombre);

                    suj.Nombre += "::ERROR";
                    arrayDatos.Add(suj);

                    continue;
                }
            }

            return arrayDatos;
        }

        #region Funciones Extras
        private List<SelectListItem> GetVariables()
        {
            #region Variables
            List<SelectListItem> Variables = new List<SelectListItem>();

            // Variables Texto
            Variables.Add(new SelectListItem { Text = "Tiempo total en el texto (+ preguntas)", Value = "1" });
            Variables.Add(new SelectListItem { Text = "Momento (fecha) en el que inició el texto", Value = "2" });
            Variables.Add(new SelectListItem { Text = "Momento (fecha) en el que finalizó el texto", Value = "3" });
            Variables.Add(new SelectListItem { Text = "Porcentaje de lectura inicial", Value = "4" });
            Variables.Add(new SelectListItem { Text = "Tiempo de lectura inicial", Value = "5" });
            Variables.Add(new SelectListItem { Text = "Velocidad de lectura inicial", Value = "6" });
            Variables.Add(new SelectListItem { Text = "Orden de lectura de regiones", Value = "7" });
            // Variables Pregunta
            Variables.Add(new SelectListItem { Text = "Tiempo total en la pregunta", Value = "8" });
            Variables.Add(new SelectListItem { Text = "Intento de pregunta", Value = "9" });
            Variables.Add(new SelectListItem { Text = "Momento (fecha) en el que inició la pregunta", Value = "10" });
            Variables.Add(new SelectListItem { Text = "Momento (fecha) en el que finalizó la pregunta", Value = "11" });
            Variables.Add(new SelectListItem { Text = "Tipo de pregunta (Abierta, test, ect)", Value = "12" });
            //guirisan/issues https://github.com/guirisan/ituinbook/issues/103
            //cambio a variable 13. Antes registraba "porcentaje de acierto". ahora 0 (fallo) o 1 (acierto)
            Variables.Add(new SelectListItem { Text = "Acierto (1) o fallo (0) en eleccion multiple", Value = "13" });
            Variables.Add(new SelectListItem { Text = "Número de veces modificada la respuesta", Value = "14" });
            Variables.Add(new SelectListItem { Text = "Respuesta dada", Value = "15" });
            Variables.Add(new SelectListItem { Text = "Número de veces abierto el enunciado", Value = "16" });
            Variables.Add(new SelectListItem { Text = "Número de veces abiertas las alternativas", Value = "17" });
            Variables.Add(new SelectListItem { Text = "Tiempo leyendo enunciado", Value = "18" });
            Variables.Add(new SelectListItem { Text = "Tiempo leyendo alternativas", Value = "19" });
            Variables.Add(new SelectListItem { Text = "Velocidad de lectura del enunciado", Value = "20" });
            Variables.Add(new SelectListItem { Text = "Velocidad de lectura de alternativas", Value = "21" });
            Variables.Add(new SelectListItem { Text = "Porcentaje de tiempo en la primera lectura sobre el total", Value = "22" });
            Variables.Add(new SelectListItem { Text = "Porcentaje de tiempo en la primera lectura sobre el total", Value = "23" });
            Variables.Add(new SelectListItem { Text = "Número de búsquedas", Value = "24" });
            Variables.Add(new SelectListItem { Text = "Tiempo de búsqueda total", Value = "25" });
            Variables.Add(new SelectListItem { Text = "Tiempo de búsqueda de pertinente", Value = "26" });
            Variables.Add(new SelectListItem { Text = "Tiempo de búsqueda de no pertinente", Value = "27" });
            Variables.Add(new SelectListItem { Text = "Último Pertinente", Value = "28" });
            Variables.Add(new SelectListItem { Text = "Porcentaje de pertinente encontrado sobre el total", Value = "29" });
            Variables.Add(new SelectListItem { Text = "Porcentaje de pertinente encontrado sobre regiones búsquedas", Value = "30" });
            Variables.Add(new SelectListItem { Text = "Velocidad de búsqueda", Value = "31" });
            Variables.Add(new SelectListItem { Text = "Número total de ayudas abiertas", Value = "32" });
            Variables.Add(new SelectListItem { Text = "Tiempo total en ayudas", Value = "33" });
            Variables.Add(new SelectListItem { Text = "Número de parafraseo", Value = "34" });
            Variables.Add(new SelectListItem { Text = "Número de prisma", Value = "35" });
            Variables.Add(new SelectListItem { Text = "Número de Lupa", Value = "36" });
            Variables.Add(new SelectListItem { Text = "Tiempo de parafraseo", Value = "37" });
            Variables.Add(new SelectListItem { Text = "Tiempo de prisma", Value = "38" });
            Variables.Add(new SelectListItem { Text = "Tiempo de lupa", Value = "39" });

            Variables.Add(new SelectListItem { Text = "Región Leída (Búsqueda)", Value = "40" });
            Variables.Add(new SelectListItem { Text = "Región Leída (Lectura Inicial)", Value = "41" });
            Variables.Add(new SelectListItem { Text = "Penúltimo Pertinente (Búsqueda)", Value = "42" });
            Variables.Add(new SelectListItem { Text = "Porcentaje de acierto total", Value = "43" });
            Variables.Add(new SelectListItem { Text = "Tiempo de Lectura del Feedback de la pregunta anterior", Value = "44" });
            Variables.Add(new SelectListItem { Text = "Velocidad de Lectura del Feedback de la pregunta anterior", Value = "45" });
            Variables.Add(new SelectListItem { Text = "Tiempo de Lectura de revisión en la pregunta anterior", Value = "46" });
            Variables.Add(new SelectListItem { Text = "Velocidad de Lectura de revisión en la pregunta anterior", Value = "47" });

            //guirisan/issues https://github.com/guirisan/ituinbook/issues/96
            //variables 48 y 49 comentadas por ser de subtarea
            //edit del anterior: si eliminamos esta, los conteos luego salen mal por no corresponder el campo Value con el numero de variables posibles
            Variables.Add(new SelectListItem { Text = "Porcentaje de NO pertinente en la selección (SubTarea) - deprecated", Value = "48" });
            Variables.Add(new SelectListItem { Text = "Porcentaje de pertinente en la selección (SubTarea) - deprecated", Value = "49" });
            
            Variables.Add(new SelectListItem { Text = "Porcentaje de pertinente en la selección", Value = "50" });
            Variables.Add(new SelectListItem { Text = "Porcentaje de distractor en la selección", Value = "51" });

            Variables.Add(new SelectListItem { Text = "SIN USO", Value = "52" }); //ponia "Feedback en tarea de responder"

            //guirisan/issues https://github.com/guirisan/ituinbook/issues/101
            Variables.Add(new SelectListItem { Text = "Porcentaje de pertinente en la selección sobre el total", Value = "53" });
            Variables.Add(new SelectListItem { Text = "Porcentaje de distractor en la selección sobre el total", Value = "54" });

            //guirisan/issues https://github.com/guirisan/ituinbook/issues/103
            Variables.Add(new SelectListItem { Text = "Porcentaje de neutro en la selección", Value = "55"});
            Variables.Add(new SelectListItem { Text = "Porcentaje de no pertinente (dist + neut) en seleccion", Value = "56"});
            
            #endregion

            return Variables;
        }


        // Devuelve el UserProfile del usuario actual
        private UserProfile getCurrentUser()
        {
            UserProfile user = (from u in db.UserProfiles
                                where u.UserName == User.Identity.Name
                                select u).FirstOrDefault();
            
            return user;
        }
           
        public class TipoCondicion
        {
            public int TipoCondicionID { get; set; }
            public string Tipo { get; set; }
        }

        public class TipoPregunta
        {
            public int TipoPreguntaID { get; set; }
            public string Tipo { get; set; }
        }

        public class TipoOperacion
        {
            public int TipoOperacionID { get; set; }
            public string Operacion { get; set; }
        }


        // Devuelve los posibles tipos de grupo
        private IList<TipoCondicion> GetTipos()
        {
            IList<TipoCondicion> tipos = new List<TipoCondicion>();

            tipos.Add(new TipoCondicion() { TipoCondicionID = 1, Tipo = "iTextBook" });
            tipos.Add(new TipoCondicion() { TipoCondicionID = 2, Tipo = "TuinLEC" });
            /* guirisan (https://github.com/guirisan/ituinbook/issues/5)
             * Dado el código del método iniciar, que comprueba la condición del módulo para 
             * ver que cargar exactamente, las condiciones de la 3 a la 7 no se usan nunca,
             * asi que las eliminamos de los tipos disponibles. ESTO ESTÁ EN PRUEBAS A 19-1-2016
             * */
            /*
            tipos.Add(new TipoCondicion() { TipoCondicionID = 3, Tipo = "TuinLEC" });
            tipos.Add(new TipoCondicion() { TipoCondicionID = 4, Tipo = "Sin uso" });
            tipos.Add(new TipoCondicion() { TipoCondicionID = 5, Tipo = "Sin uso" });
            tipos.Add(new TipoCondicion() { TipoCondicionID = 6, Tipo = "Sin uso" });
            tipos.Add(new TipoCondicion() { TipoCondicionID = 7, Tipo = "Sin uso" });
            */
            return tipos;
        }

        private IList<TipoOperacion> GetCodigos()
        {
            IList<TipoOperacion> tipos = new List<TipoOperacion>();

            tipos.Add(new TipoOperacion() { TipoOperacionID = 1, Operacion = "Hablar Profesor"});
            tipos.Add(new TipoOperacion() { TipoOperacionID = 2, Operacion = "Pensar Profesor" });
            tipos.Add(new TipoOperacion() { TipoOperacionID = 3, Operacion = "Hablar Alumna" });
            tipos.Add(new TipoOperacion() { TipoOperacionID = 4, Operacion = "Pensar Alumna" });
            tipos.Add(new TipoOperacion() { TipoOperacionID = 5, Operacion = "Hablar Alumno" });
            tipos.Add(new TipoOperacion() { TipoOperacionID = 6, Operacion = "Pensar Alumno" });
            tipos.Add(new TipoOperacion() { TipoOperacionID = 7, Operacion = "Mostrar Texto" });
            tipos.Add(new TipoOperacion() { TipoOperacionID = 8, Operacion = "Quitar Texto" });
            tipos.Add(new TipoOperacion() { TipoOperacionID = 9, Operacion = "Mostrar Página" });
            tipos.Add(new TipoOperacion() { TipoOperacionID = 10, Operacion = "Quitar Página" });
            tipos.Add(new TipoOperacion() { TipoOperacionID = 11, Operacion = "Mostrar Pregunta" });
            tipos.Add(new TipoOperacion() { TipoOperacionID = 12, Operacion = "Quitar Pregunta" });

            return tipos;
        }

        private IList<TipoPregunta> GetTiposPreguntas()
        {
            IList<TipoPregunta> tipos = new List<TipoPregunta>();

            tipos.Add(new TipoPregunta() { TipoPreguntaID = 1, Tipo = "Test" });
            tipos.Add(new TipoPregunta() { TipoPreguntaID = 2, Tipo = "Abierta" });
            tipos.Add(new TipoPregunta() { TipoPreguntaID = 3, Tipo = "Seleccionar" });
            tipos.Add(new TipoPregunta() { TipoPreguntaID = 4, Tipo = "Emparejar" });
            tipos.Add(new TipoPregunta() { TipoPreguntaID = 5, Tipo = "Ordenar" });
            
            return tipos;
        }

        #endregion
    }
}

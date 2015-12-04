using ReadAndLearn.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace ReadAndLearn.Controllers
{
    public class PL0_ExperimentosController : Controller
    {
        Contexto db = new Contexto();
        ExternalMethods ext = new ExternalMethods();

        #region FUNCIONES COMUNES
        private void SaveChanges()
        {
            ext.SaveChanges();
            db.SaveChanges();        
        }

        protected RedirectToRouteResult RouterPregunta(int GrupoID, int ModuloID, Pregunta pregunta, DatosUsuario datosUsuario, int textoID)
        {           
            switch (pregunta.TipoPreguntaID)
            { 
                case 0: // Test //guirisan ERROR! no hay TipoPregunta 0, empiezan desde 1
                    return RedirectToAction("PL0_Pregunta_Test", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });                    
                case 1:
                    return RedirectToAction("PL0_Pregunta_Test", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });                    
                case 2:
                    return RedirectToAction("PL0_Pregunta_Abierta", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });                    
                case 3:
                    return RedirectToAction("PL0_Pregunta_Seleccion", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });
                case 4:
                    return RedirectToAction("PL0_Pregunta_Relacionar", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });
                default:
                    return RedirectToAction("PL0_Pregunta_Test", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });                    
            }

            return RedirectToAction("PL0_Pregunta_Test", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });
        }

        [HttpPut]
        public void RegistrarAccion(int DatosUsuarioID, int GrupoID, int TextoID, int ModuloID, int PreguntaID, int CodeOP, string Param)
        {   
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            du.PreguntaID = PreguntaID;
            du.TextoID = TextoID;

            SaveChanges();

            DatoSimple ds = new DatoSimple() { CodeOP = CodeOP, DatosUsuarioID = DatosUsuarioID, TextoID = TextoID, PreguntaID = PreguntaID, Info = Param, Momento = DateTime.Now };

            db.DatosSimples.Add(ds);

            SaveChanges();            
        }

        [HttpPost]
        public ActionResult Algoritmo(string Pertinente, int TextoID, string NuevaSeleccion)
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

                    contenido = Server.UrlDecode(contenido);

                    contenido = RemoveBetween(contenido, '<', '>');

                    contenido = contenido.Replace("&nbsp;", " ");

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

                    contenido = Server.UrlDecode(contenido);

                    contenido = RemoveBetween(contenido, '<', '>');

                    contenido = contenido.Replace("&nbsp;", " ");

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

                    contenido = Server.UrlDecode(contenido);

                    contenido = RemoveBetween(contenido, '<', '>');

                    contenido = contenido.Replace("&nbsp;", " ");

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

        protected string RemoveBetween(string s, char begin, char end)
        {
            Regex regex = new Regex(string.Format("\\{0}.*?\\{1}", begin, end));
            return regex.Replace(s, string.Empty);
        }
        #endregion
        //
        // GET: /PL0_Experimentos/

        public ActionResult PL0_Texto(int GrupoID, int ModuloID, int textoActual, Contexto _db, int NumAccion)
        {
            if (_db != null)
            {
                db = (Contexto)_db;

                db.SaveChanges();
            }

            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto text = ext.GetTextoActual(ModuloID, textoActual);
            Pregunta preg = ext.GetPreguntaActual(text, du.PreguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.numAccion = NumAccion;

            return View(ext.GetTexto(text.TextoID));
        }

        public ActionResult PL0_Texto_Busca(int GrupoID, int ModuloID, int textoActual)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto text = ext.GetTextoActual(ModuloID, textoActual);
            Pregunta preg = ext.GetPreguntaActual(text, du.PreguntaActual);

            ViewBag.DatosUsuario = du;

            return View(ext.GetTexto(text.TextoID));
        }

        public ActionResult PL0_Texto_Revisa(int GrupoID, int ModuloID, int textoActual)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto text = ext.GetTextoActual(ModuloID, textoActual);
            Pregunta preg = ext.GetPreguntaActual(text, du.PreguntaActual);
            
            ViewBag.DatosUsuario = du;

            return View(ext.GetTexto(text.TextoID));
        }

        public ActionResult PL0_Texto_Seleccion(int GrupoID, int ModuloID, int textoActual)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto text = ext.GetTextoActual(ModuloID, textoActual);
            Pregunta preg = ext.GetPreguntaActual(text, du.PreguntaActual);

            ViewBag.DatosUsuario = du;

            return View(ext.GetTexto(text.TextoID));
        }

        public ActionResult PL0_Pregunta(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            du.PreguntaID = pregunta.PreguntaID;
            du.TextoID = textoID;

            SaveChanges();            

            return RouterPregunta(GrupoID, ModuloID, pregunta, du, texto.TextoID);            
        }        

        #region Pregunta TEST
        public ActionResult PL0_Pregunta_Test(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);
            
            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);
            
            config.EnmascararAlternativas = true;
            db.SaveChanges();
            
            ViewBag.ConfigPregunta = config;


            return View(ext.GetPreguntaActual(texto, preguntaActual));
        }

        [HttpPost]
        public ActionResult PL0_Pregunta_Test_Validar(int GrupoID, int ModuloID, int PreguntaID, string respuesta)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            

            Pregunta pregunta = ext.GetPregunta(PreguntaID);

            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            string mensaje = "";

            foreach (Alternativa alt in pregunta.Alternativas)
            {
                if (alt.Opcion == respuesta)
                {
                    if (alt.Valor) // Acierto
                    {
                        DatoSimple ds = new DatoSimple();

                        // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                        mensaje = (alt.FeedbackContenido == null ? (pregunta.FDBK_Correcto == null ? null : pregunta.FDBK_Correcto) : alt.FeedbackContenido);

                        du.Puntos += 100;
                        
                        // Registrar respuesta
                        ds.CodeOP = 15;
                        ds.Info = respuesta;
                        ds.Momento = DateTime.Now;
                        ds.PreguntaID = PreguntaID;
                        ds.TextoID = pregunta.Texto.TextoID;                        
                        ds.DatosUsuarioID = du.DatosUsuarioID;

                        db.DatosSimples.Add(ds);
                        du.DatoSimple.Add(ds);

                        SaveChanges();                        
                    }
                    else // Fallo
                    {
                        DatoSimple ds = new DatoSimple();

                        // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                        mensaje = (alt.FeedbackContenido == null ? (pregunta.FDBK_Incorrecto == null ? null : pregunta.FDBK_Incorrecto) : alt.FeedbackContenido);
                                                
                        // Registrar respuesta
                        ds.CodeOP = 15;
                        ds.Info = respuesta;
                        ds.Momento = DateTime.Now;
                        ds.PreguntaID = PreguntaID;
                        ds.TextoID = pregunta.Texto.TextoID;
                        ds.DatosUsuarioID = du.DatosUsuarioID;                        

                        db.DatosSimples.Add(ds);
                        du.DatoSimple.Add(ds);

                        SaveChanges();
                        // Registrar respuesta CodeOP = 15
                    }

                    break;
                }
            }

            // Genera un feedback si no hay feedback de contenido ni de pregunta.
            mensaje = mensaje == null ? ext.GetFeedback(du) : mensaje;

            return Json(new { redirect = Url.Action("PL0_Pregunta_Test_Resuelta", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = PreguntaID }), Puntos = du.Puntos, mensaje = mensaje, PreguntaID = pregunta.PreguntaID });            
        }

        public ActionResult PL0_Pregunta_Test_Resuelta(int GrupoID, int ModuloID, int preguntaID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Pregunta pregunta = new Pregunta();

            DatoSimple ds = du.DatoSimple.Last(p => p.CodeOP == 15);

            pregunta = ext.GetPregunta((int)preguntaID);

            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);
            ViewBag.ConfigPregunta = ext.GetConfigPregunta((int)preguntaID);
            
            ViewBag.DatosUsuario = du;
            ViewBag.DatoSimple = ds;

            // Buscar en el registro la respuesta dada a esta pregunta

            return View(pregunta);
        }
        #endregion

        #region Pregunta ABIERTA
        public ActionResult PL0_Pregunta_Abierta(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);            
            ViewBag.ConfigPregunta = ext.GetConfigPregunta(pregunta.PreguntaID);
            ViewBag.TextoID = textoID;

            // REGISTRAR INICIO

            return View(ext.GetPreguntaActual(texto, preguntaActual));
        }

        public ActionResult PL0_Pregunta_Abierta_Validar(int GrupoID, int ModuloID, int TextoID, int PreguntaID, string respuesta)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(TextoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, du.PreguntaActual);

            string mensaje = "";

            // GENERAR FEEDBACK

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);
            ViewBag.ConfigPregunta = ext.GetConfigPregunta(pregunta.PreguntaID);

            // REGISTRAR VALIDAR

            return Json(new { redirect = Url.Action("PL0_Pregunta_Abierta_Resuelta", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = PreguntaID }), Puntos = du.Puntos, mensaje = mensaje, PreguntaID = pregunta.PreguntaID });             
        }

        public ActionResult PL0_Pregunta_Abierta_Resuelta(int GrupoID, int ModuloID, int preguntaID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Pregunta pregunta = ext.GetPregunta(preguntaID);

            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);
            ViewBag.ConfigPregunta = ext.GetConfigPregunta(preguntaID);

            ViewBag.DatosUsuario = du;

            return View(pregunta);
        }

        #endregion

        #region Pregunta Seleccion
        public ActionResult PL0_Pregunta_Seleccion(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);
            ViewBag.ConfigPregunta = ext.GetConfigPregunta(pregunta.PreguntaID);
            ViewBag.TextoID = textoID;

            // REGISTRAR INICIO

            return View(ext.GetPreguntaActual(texto, preguntaActual));
        }
        #endregion

    }
}

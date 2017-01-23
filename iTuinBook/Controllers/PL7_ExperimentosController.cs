
using System;
using System.Collections.Generic;
using System.Linq;
using ReadAndLearn.Models;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using NLog;


namespace ReadAndLearn.Controllers
{
    public class PL7_ExperimentosController : Controller
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        Contexto db = new Contexto();
        ExternalMethods ext = new ExternalMethods();

        #region FUNCIONES COMUNES
        [HttpPost]
        public ActionResult AjaxConfigPregunta(int PreguntaID)
        {
            logger.Debug("PL7_Experimentos/AjaxConfigPRegunta");
            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            if (configPreg != null)
                return Json(new { busqueda = configPreg.EnmascararTexto, revision = configPreg.EnmascararTextoRevisa });
            else
                return Json(new { busqueda = "False", revision = "False" });
        }


        public void ValidarSeleccion(int ModuloID, int GrupoID, int PreguntaID, int TextoID, string respuesta, out double pert, out double noPert, bool subtarea, string moment, int numAccion = -1)
        {
            //guirisan
            logger.Debug("PL7_Experimentos/ValidarSeleccion");
            DateTime datetimeclient = DateTime.Parse(moment);


            Pregunta pregunta = db.Preguntas.Find(PreguntaID);
            Pagina pagina = pregunta.Texto.Paginas.First();
            string respOriginal = respuesta;


            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

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

            int auto = respuesta.Split('\\').Length;
            int manual = respuesta.Split('/').Length;

            string[] selecciones;

            if (auto > manual)
            {
                selecciones = respuesta.Split('\\');
            }
            else
            {
                selecciones = respuesta.Split('/');
            }


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

            if (totalCharSelect == 0)
            {
                porcNoPert = 100;
            }

            if (porcPert > 100)
            {
                porcPert = 100;
            }



            if (subtarea)
            {
                DatoSimple ds = new DatoSimple() { CodeOP = 123, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Info = respOriginal, Info2 = pertTmp, TextoID = TextoID, PreguntaID = PreguntaID, Dato01 = (float)porcPert, Dato02 = (float)porcNoPert, NumAccion = numAccion };
                db.DatosSimples.Add(ds);
            }
            else
            {
                DatoSimple ds = new DatoSimple() { CodeOP = 124, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Info = respOriginal, Info2 = pertTmp, TextoID = TextoID, PreguntaID = PreguntaID, Dato01 = (float)porcPert, Dato02 = (float)porcNoPert, NumAccion = numAccion };
                db.DatosSimples.Add(ds);
            }

            pert = porcPert;
            noPert = porcNoPert;

            db.SaveChanges();
        }

        public string GetFeedbackSeleccion(DatosUsuario du, double porcPert, double porcNoPert)
        {
            logger.Debug("PL7_Experimentos/GetFeedbackSeleccion");
            string mensaje = "";

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

            return mensaje;
        }

        private void SaveChanges()
        {
            logger.Debug("PL7_Experimentos/SaveChanges");
            try
            {
                ext.SaveChanges();
                db.SaveChanges();
            }
            catch (Exception e)
            {

            }
        }

        protected RedirectToRouteResult RouterPregunta(int GrupoID, int ModuloID, Pregunta pregunta, DatosUsuario datosUsuario, int textoID, string moment, int numAccion = -1, bool segundoIntento = false, bool preguntaResuelta = false, bool primerIntento = false)
        {
            logger.Debug("PL7_Experimentos/RouterPregunta");
            DateTime datetimeclient = DateTime.Parse(moment);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);

            //guirisan/issues https://github.com/guirisan/ituinbook/issues/38#issuecomment-181284162
            //el primer DS-codeop=10 generado al volver al segundo intento de pregunta se genera desde aquí.
            //comentamos la línea por duplicidad de información
            if (!segundoIntento && !preguntaResuelta && !primerIntento)
            {
                db.DatosSimples.Add(new DatoSimple() { CodeOP = 10, DatosUsuarioID = datosUsuario.DatosUsuarioID, Momento = datetimeclient, PreguntaID = pregunta.PreguntaID, NumAccion = numAccion });
            }
            else if (segundoIntento)
            {
                //issue https://github.com/guirisan/ituinbook/issues/50
                //si es segundoIntento, indicamos que se está yendo a una pregunta y que és un segundo intento
                db.DatosSimples.Add(new DatoSimple() { CodeOP = 131, DatosUsuarioID = datosUsuario.DatosUsuarioID, Momento = datetimeclient, PreguntaID = pregunta.PreguntaID, NumAccion = numAccion });
            }
            else if (preguntaResuelta)
            {
                //issue https://github.com/guirisan/ituinbook/issues/52
                //si es preguntaResuelta, indicamos que se está yendo a una pregunta ya respondida (consulta el texto después de acertar a la primera o acertar/fallar a la segunda
                db.DatosSimples.Add(new DatoSimple() { CodeOP = 132, DatosUsuarioID = datosUsuario.DatosUsuarioID, Momento = datetimeclient, PreguntaID = pregunta.PreguntaID, NumAccion = numAccion });
            }
            else if (primerIntento)
            {
                //issue https://github.com/guirisan/ituinbook/issues/67
                //si es primerIntento, indicamos que está volviendo a la pregunta desde la que llegó al texto 
                //y que es un primer intento de repuesta
                db.DatosSimples.Add(new DatoSimple() { CodeOP = 135, DatosUsuarioID = datosUsuario.DatosUsuarioID, Momento = datetimeclient, PreguntaID = pregunta.PreguntaID, NumAccion = numAccion });

            }    

            db.SaveChanges();

            switch (pregunta.TipoPreguntaID)
            {
                case 0:
                    if (segundoIntento)
                    {
                        return RedirectToAction("PL7_Pregunta_Test_2", new {GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = datosUsuario.PreguntaID});
                    }
                    
                    else
                    {
                        return RedirectToAction("PL7_Pregunta_Test", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID, moment = moment, numAccion = numAccion });
                    }
                    
                case 1: // Test
                    if (config != null && config.SeleccionarPertinente)
                    {
                        if (config.SimultanearTareas)
                        {
                            return RedirectToAction("PL7_Pregunta_Test_Seleccion_Simultaneo", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });
                        }
                        else
                        {
                            return RedirectToAction("PL7_Pregunta_Test_Seleccion", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });
                        }
                    }
                    else
                    {
                        if (segundoIntento)
                        {
                            return RedirectToAction("PL7_Pregunta_Test_2", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = datosUsuario.PreguntaID });
                        }
                        else if (preguntaResuelta)
                        {
                            return RedirectToAction("PL7_Pregunta_Test_Resuelta", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaID = datosUsuario.PreguntaID }); //int GrupoID, int ModuloID, int preguntaID, string feedbackText
                        }

                            //guirisan/issue https://github.com/guirisan/ituinbook/issues/67
                            //las siguiente tres lineas son por si hubiera que discernir entre cargar la pregunta de test por primera vez
                            //y cargarla al volver de consultar el texto en la condición en que pueden (por ejemplo para deshabilitar el botón
                            //volver al texto y no permitir volver una segunda vez.
                        else if (primerIntento)
                        {
                            return RedirectToAction("PL7_Pregunta_Test", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID, moment = moment, numAccion = numAccion });
                        }


                        else
                        {
                            return RedirectToAction("PL7_Pregunta_Test", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID, moment = moment, numAccion = numAccion });
                        }
                    }
                case 2: // Abierta
                    if (config != null && config.SeleccionarPertinente)
                    {
                        return RedirectToAction("PL7_Pregunta_Abierta_Seleccion", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });
                    }
                    else
                    {
                        return RedirectToAction("PL7_Pregunta_Abierta", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });
                    }
                case 3: // Selección
                    return RedirectToAction("PL7_Pregunta_Seleccion", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });
                case 5: // Ordenar
                    return RedirectToAction("PL7_Pregunta_Ordenar", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID });
                default:
                    return RedirectToAction("PL7_Pregunta_Test", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = datosUsuario.PreguntaActual, textoID = textoID, moment = moment, numAccion = numAccion });
            }
        }

        public class Pertinente
        {
            public int Region { get; set; }
            public double Porc { get; set; }
            public int Chars { get; set; }
        }

        private double CalculoPertinente(string str)
        {
            logger.Debug("PL7_Experimentos/CalculoPertinente");
            string[] param = str.Substring(0, str.Length - 1).Split('/');
            List<Pertinente> lista = new List<Pertinente>();

            foreach (string item in param)
            {
                string[] par = item.Split('-');

                lista.Add(new Pertinente() { Region = Convert.ToInt32(par[0]), Porc = Convert.ToDouble(par[1]), Chars = Convert.ToInt32(par[2]) });
            }

            var orden = lista.GroupBy(p => p.Region)
                             .Select(grp => grp.First())
                             .ToList();

            return orden.Sum(p => p.Porc);
        }

        private double CalculoPertinenteSobreBusqueda(string str)
        {
            logger.Debug("PL7_Experimentos/CalculoPertinenteSobreBusqueda");
            string[] param = str.Substring(0, str.Length - 1).Split('/');
            List<Pertinente> lista = new List<Pertinente>();

            foreach (string item in param)
            {
                string[] par = item.Split('-');

                lista.Add(new Pertinente() { Region = Convert.ToInt32(par[0]), Porc = Convert.ToDouble(par[1]), Chars = Convert.ToInt32(par[2]) });
            }

            var orden = lista.GroupBy(p => p.Region)
                             .Select(grp => grp.First())
                             .ToList();

            var parcial = from p in orden
                          where p.Porc != 0
                          select p;

            int sumaTotal = orden.Sum(p => p.Chars);
            int sumaParcial = parcial.Sum(p => p.Chars);

            double porc = ((double)sumaParcial * 100.0) / (double)sumaTotal;

            return porc;
        }

        [HttpPost]
        public ActionResult GetPreguntaID(int ModuloID, int GrupoID)
        {
            logger.Debug("PL7_Experimentos/GetPreguntaID");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            return Json(new { result = du.PreguntaID });
        }

        [HttpPost]
        public void RegistrarAccion(int DatosUsuarioID, int GrupoID, int TextoID, int ModuloID, int PreguntaID, int CodeOP, string Param, string moment, int numAccion = -1, string dataRow = "")
        {
            logger.Debug("PL7_Experimentos/RegistrarAccion");

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Pregunta pregunta = db.Preguntas.Find(PreguntaID);
            Texto texto = db.Textos.Find(TextoID);

            double velocidad = 0.0;

            if (PreguntaID != 0)
                du.PreguntaID = PreguntaID;

            du.TextoID = TextoID;

            SaveChanges();

            DatoSimple ds = new DatoSimple();

            if (CodeOP == 40 || CodeOP == 41)
            {
                ds = new DatoSimple() { CodeOP = CodeOP, DatosUsuarioID = DatosUsuarioID, TextoID = TextoID, PreguntaID = PreguntaID, Info = Param.Split('/').First(), Info2 = Param.Split('/').Last(), Momento = datetimeclient, NumAccion = numAccion };
            }
            else
            {
                DatoSimple ds3 = new DatoSimple();
                DatoSimple ds4 = new DatoSimple();

                switch (CodeOP)
                {
                    case 13:
                        // ver si es ult o penultimo pertinente (PertinenteStatus)
                        // ver porcentaje de pertinente encontrado (PertinenteOrden)
                        if (du.PertinenteStatus == 2) // Último
                        {
                            ds3 = new DatoSimple() { CodeOP = 28, DatosUsuarioID = DatosUsuarioID, TextoID = TextoID, PreguntaID = PreguntaID, Momento = datetimeclient, NumAccion = numAccion };
                        }
                        else
                        {
                            if (du.PertinenteStatus == 1)
                            {
                                ds3 = new DatoSimple() { CodeOP = 42, DatosUsuarioID = DatosUsuarioID, TextoID = TextoID, PreguntaID = PreguntaID, Momento = datetimeclient, NumAccion = numAccion };
                            }
                        }

                        if (du.PertinenteOrden != "")
                        {
                            double porc = CalculoPertinente(du.PertinenteOrden);

                            db.DatosSimples.Add(new DatoSimple() { CodeOP = 29, DatosUsuarioID = DatosUsuarioID, TextoID = TextoID, PreguntaID = PreguntaID, Dato01 = (float)porc, Momento = datetimeclient, NumAccion = numAccion });
                        }

                        db.DatosSimples.Add(ds3);

                        ds = new DatoSimple() { CodeOP = CodeOP, DatosUsuarioID = DatosUsuarioID, TextoID = TextoID, PreguntaID = PreguntaID, Info = Param, Momento = datetimeclient, NumAccion = numAccion };
                        break;
                    default:
                        ds = new DatoSimple() { CodeOP = CodeOP, DatosUsuarioID = DatosUsuarioID, TextoID = TextoID, PreguntaID = PreguntaID, Info = Param, Momento = datetimeclient, NumAccion = numAccion };
                        break;
                }



            }

            db.DatosSimples.Add(ds);

            SaveChanges();

            DatoSimple ds2 = new DatoSimple();

            switch (CodeOP)
            {
                case 5:
                    du.PreguntaID = texto.Preguntas.ElementAt(du.PreguntaActual).PreguntaID;

                    db.SaveChanges();
                    break;
                case 18:
                    velocidad = (double)pregunta.Enunciado.Split(' ').Count() / (Convert.ToDouble(Param) / 1000.0);

                    ds2 = new DatoSimple() { CodeOP = 20, DatosUsuarioID = DatosUsuarioID, TextoID = TextoID, PreguntaID = PreguntaID, Info = velocidad.ToString(), Momento = datetimeclient, NumAccion = numAccion };

                    break;
                case 19:
                    double totalPalabras = 0.0;

                    foreach (Alternativa alt in pregunta.Alternativas)
                    {
                        totalPalabras += alt.Opcion.Split(' ').Count();
                    }

                    velocidad = (double)totalPalabras / (Convert.ToDouble(Param) / 1000.0);

                    ds2 = new DatoSimple() { CodeOP = 21, DatosUsuarioID = DatosUsuarioID, TextoID = TextoID, PreguntaID = PreguntaID, Info = velocidad.ToString(), Momento = datetimeclient, NumAccion = numAccion };
                    break;
                case 40:
                    Pregunta preg = db.Preguntas.Find(texto.Preguntas.ElementAt(du.PreguntaActual).PreguntaID);

                    if (preg.Pertinente != null && preg.Pertinente != "")
                    {
                        string str = "<reg pagina=\"" + Param.Split('/').First() + "\" region=\"" + Param.Split('/').Last() + "\"";
                        int numPag = Convert.ToInt32(Param.Split('/').First()) - 1;
                        int ini = texto.Paginas.ElementAt(numPag).Contenido.IndexOf(str);
                        string substr = texto.Paginas.ElementAt(numPag).Contenido.Substring(ini);
                        int fin = substr.IndexOf("</reg>");
                        string substr2 = substr.Substring(0, fin);

                        string str2 = RemoveBetween(substr2, '<', '>');

                        double porc = AlgoritmoPertinente(HttpUtility.HtmlDecode(str2), preg.Pertinente, preg.PreguntaID);

                        if (porc != 0) // Ha encontrado pertinente en el texto
                        {
                            du.PertinenteStatus = 2; // Último pertinente                            
                        }
                        else
                        {
                            if (du.PertinenteStatus == 2)
                            {
                                du.PertinenteStatus = 1;
                            }
                            else
                            {
                                du.PertinenteStatus = 0;
                            }
                        }

                        du.PertinenteOrden += Param.Split('/').Last() + "-" + porc + "-" + HttpUtility.HtmlDecode(str2).Length + "/";

                        SaveChanges();
                    }
                    break;
            }
        }

        public bool BuscarAccion(int CodeOP, int GrupoID, int ModuloID, int TextoID, int PreguntaID)
        {
            logger.Debug("PL7_Experimentos/BuscarAccion");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            var acciones = from d in db.DatosSimples
                           where d.DatosUsuarioID == du.DatosUsuarioID &&
                           d.TextoID == TextoID &&
                           d.PreguntaID == PreguntaID &&
                           d.CodeOP == CodeOP
                           select d;

            return acciones.Count() > 0;
        }

        [HttpPost]
        public ActionResult Algoritmo(string Pertinente, int TextoID, string NuevaSeleccion)
        {
            logger.Debug("PL7_Experimentos/Algoritmo");
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

        public ActionResult Error(string error, string excepcion, int GrupoID, int ModuloID)
        {
            ViewBag.Error = error;
            ViewBag.Excepcion = excepcion;
            ViewBag.GrupoID = GrupoID;
            ViewBag.ModuloID = ModuloID;

            return View();
        }


        //guirisan/issues https://github.com/guirisan/ituinbook/issues/46
        //public ActionResult Agradecimiento(int GrupoID, int ModuloID)
        public ActionResult Agradecimiento(int duID)
        {
            DatosUsuario du = db.DatosUsuario.Find(duID);
            ViewBag.du = du;
            return View();
        }

        public double AlgoritmoPertinente(string Contenido, string respuesta, int PreguntaID)
        {
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

            string[] selecciones = respuesta.Split('/');

            int ragInf;
            int ragSup;

            int tmp1 = 0; // Número de caracteres pertinentes            

            int CharPertinentes = 0;

            foreach (string select in selecciones)
            {
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
                                    continue;
                                }
                                else // Sobresale por la derecha
                                {
                                    tmp1 += seleccion.Length - (ragSup - posible[1]);
                                    continue;
                                }
                            }
                            else
                            {
                                if (ragSup <= posible[1]) // Sobresale por la izquierda
                                {
                                    tmp1 += seleccion.Length - (posible[0] - ragInf);
                                    continue;
                                }
                                else // Sobresale por ambos lados
                                {
                                    tmp1 += seleccion.Length - (ragSup - posible[1]) - (posible[0] - ragInf);
                                    continue;
                                }
                            }
                        }
                    }
                }

                CharPertinentes += tmp1;

                tmp1 = 0;
            }

            // Porcentaje de texto pertinente seleccionado frente al texto pertinente de la pregunta.
            double porcPert = ((double)CharPertinentes * 100.0) / (double)CharPertPregunta;

            return porcPert;
        }
        #endregion

        public ActionResult PL7_Texto(int GrupoID, int ModuloID, int textoActual, string moment = "", int numAccion = -1, bool SegundoIntento = false, bool preguntaResuelta = false, bool inicioTexto = false, bool primerIntento = false)
        {
            logger.Debug("PL7_Experimentos/PL7_Texto");
            try
            {
                DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

                //guirisan/issues https://github.com/guirisan/ituinbook/issues/38
                if (moment != "")
                {
                    //crear datosimple para registrar momento de volver al texto
                    DateTime datetimeclient = DateTime.Parse(moment);
                    DatoSimple ds = new DatoSimple() { CodeOP = 127, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };
                    db.DatosSimples.Add(ds);
                    SaveChanges();
                }
                
                

                Texto text = ext.GetTextoActual(ModuloID, textoActual);
                Pregunta preg = ext.GetPreguntaActual(text, du.PreguntaActual);

                ViewBag.DatosUsuario = du;
                ViewBag.Pregunta = preg;
                ViewBag.ModuloID = ModuloID;
                //guirisan/secuencias/developing
                ViewBag.numAccion = numAccion;
                ViewBag.SegundoIntento = SegundoIntento;
                ViewBag.PreguntaResuelta = preguntaResuelta;
                ViewBag.InicioTexto = inicioTexto;
                ViewBag.PrimerIntento = primerIntento;
                return View(ext.GetTexto(text.TextoID));
            }
            catch (Exception e)
            {
                return RedirectToAction("Error", new { error = "Error al intentar mostrar el texto.", excepcion = e.InnerException, GrupoID = GrupoID, ModuloID = ModuloID });
            }
        }


        [HttpPost]
        public ActionResult PL7_Texto_Cambiar(int GrupoID, int ModuloID, int TextoID, string moment, int numAccion = -1, string dataRow = "")
        {
            logger.Debug("PL7_Experimentos/PL7_Texto_Cambiar");

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);


            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Modulo mod = db.Modulos.Find(ModuloID);
            Texto text = db.Textos.Find(TextoID);
            Grupo gr = db.Grupos.Find(GrupoID);

            DatoSimple ds;

            du.TextoActual++;
            du.PreguntaActual = 0;
            du.AyudaStatus = 0;
            du.BuscaStatus = 0;

            ds = new DatoSimple() { CodeOP = 3, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };

            db.DatosSimples.Add(ds);

            SaveChanges();

            if (du.TextoActual < mod.Textos.Count) // Cambiar de texto
            {
                ds = new DatoSimple() { CodeOP = 101, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.AyudaStatus, Dato02 = du.BuscaStatus, Dato03 = du.RevisaStatus, NumAccion = numAccion };
                db.DatosSimples.Add(ds);

                SaveChanges();

                return Json(new { redirect = Url.Action("PL7_Texto", new { du.GrupoID, ModuloID = du.ModuloID, textoActual = du.TextoActual }), Parent = true });
            }
            else  // Fin módulo
            {
                ConfigGrupo config = new ConfigGrupo();

                config = gr.ConfigGrupo;

                if (config != null && config.AutoChange)
                {
                    string[] param = gr.Orden.Split(':');
                    int pos = Array.FindIndex(param, item => item == ModuloID.ToString());

                    if (pos < param.Length - 1) // Hay más módulos
                    {
                        ds = new DatoSimple() { CodeOP = 102, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };
                        db.DatosSimples.Add(ds);

                        du.Cerrada = true;

                        SaveChanges();

                        int sigModuloID = Convert.ToInt32(param[pos + 1]);

                        return Json(new { redirect = Url.Action("Iniciar", "ReadAndLearn", new { du.GrupoID, ModuloID = sigModuloID, tmpActual = 0, accActual = 0, moment = datetimeclient, NumAccion = numAccion }), Parent = true });
                    }
                    else // No quedan módulos
                    {
                        ds = new DatoSimple() { CodeOP = 102, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };
                        db.DatosSimples.Add(ds);

                        du.Cerrada = true;

                        SaveChanges();

                        return Json(new { redirect = Url.Action("Tareas", "Alumno"), Parent = true });
                    }
                }
                else
                {
                    ds = new DatoSimple() { CodeOP = 102, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.AyudaStatus, Dato02 = du.BuscaStatus, Dato03 = du.RevisaStatus, NumAccion = numAccion };
                    db.DatosSimples.Add(ds);

                    du.Cerrada = true;

                    SaveChanges();

                    return Json(new { redirect = Url.Action("Tareas", "Alumno"), Parent = true });
                }
            }
        }

        public ActionResult PL7_Pregunta(int GrupoID, int ModuloID, int preguntaActual, int textoID, string moment, int numAccion = -1, bool segundoIntento = false, bool preguntaResuelta = false, bool primerIntento = false)
        {

            logger.Debug("PL7_Experimentos/PL7_Pregunta");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            du.PreguntaID = pregunta.PreguntaID;
            du.TextoID = textoID;

            SaveChanges();

            return RouterPregunta(GrupoID, ModuloID, pregunta, du, texto.TextoID, moment, numAccion, segundoIntento, preguntaResuelta, primerIntento);
        }

        public ActionResult PL7_Siguiente_Pregunta(int GrupoID, int ModuloID, int TextoID, string moment, int PreguntaID = 0, int numAccion = -1, string dataRow = "", bool greetingsPage = false, bool preguntaResuelta = false)
        {
            logger.Debug("PL7_Siguiente_Pregunta");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            Modulo mod = db.Modulos.Find(ModuloID);
            Texto text = db.Textos.Find(TextoID);
            DatoSimple ds;
            Grupo gr = db.Grupos.Find(GrupoID);

            
            
            if (greetingsPage)
            {
                ds = new DatoSimple() { CodeOP = 130, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };
                db.DatosSimples.Add(ds);
                du.Cerrada = true;
                SaveChanges();

                return Json(new { redirect = Url.Action("Tareas", "Alumno"), Parent = true });
            }



            if (du.PreguntaActual + 2 > text.Preguntas.Count()) // Cambio de texto
            {
                du.TextoActual++;
                du.PreguntaActual = 0;

                //issue https://github.com/guirisan/ituinbook/issues/69#issuecomment-212217670
                //aquí insertamos un 11 para indicar el final de la última pregunta que de otra manera no aparecería, está unas cuantas líneas más abajo
                db.DatosSimples.Add(new DatoSimple() { CodeOP = 11, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, PreguntaID = PreguntaID, NumAccion = numAccion });


                ds = new DatoSimple() { CodeOP = 3, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };

                db.DatosSimples.Add(ds);

                SaveChanges();
                if (du.TextoActual < mod.Textos.Count) // Cambiar de texto
                {
                    ds = new DatoSimple() { CodeOP = 101, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };

                    db.DatosSimples.Add(ds);

                    SaveChanges();

                    return Json(new { redirect = Url.Action("PL7_Texto", new { du.GrupoID, ModuloID = du.ModuloID, textoActual = du.TextoActual }), Parent = true });
                }
                else  // Fin módulo
                {

                    ConfigGrupo config = new ConfigGrupo();

                    config = gr.ConfigGrupo;

                    if (config != null && config.AutoChange)
                    {
                        string[] param = gr.Orden.Split(':');
                        int pos = Array.FindIndex(param, item => item == ModuloID.ToString());

                        if (pos < param.Length - 1) // Hay más módulos
                        {
                            ds = new DatoSimple() { CodeOP = 102, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };
                            db.DatosSimples.Add(ds);

                            du.Cerrada = true;

                            SaveChanges();
                            //guirisan/secuencias/development
                            //fin de módulo (y comienzo de uno nuevo), guardar datos!!
                            //paso al siguiente módulo, reiniciar numAccion!!
                            int sigModuloID = Convert.ToInt32(param[pos + 1]);

                            return Json(new { redirect = Url.Action("Iniciar", "ReadAndLearn", new { du.GrupoID, ModuloID = sigModuloID, tmpActual = 0, accActual = 0, moment = moment }), Parent = true });
                        }
                        else // No quedan módulos
                        {
                            //guirisan/issues https://github.com/guirisan/ituinbook/issues/46
                            //**************************************************************************/
                            //********************************TO-DO*************************************/
                            //********************************TO-DO*************************************/
                            //**************************************************************************/
                            //**************************************************************************/
                            //**************************************************************************/
                            //**************************************************************************/
                            //**************************************************************************/
                            //**************************************************************************/
                            ds = new DatoSimple() { CodeOP = 102, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };
                            db.DatosSimples.Add(ds);

                            du.Cerrada = true;

                            SaveChanges();

                            return Json(new { redirect = Url.Action("Tareas", "Alumno"), Parent = true });
                        }
                    }
                    else
                    {
                        //guirisan/issues https://github.com/guirisan/ituinbook/issues/46
                        //distinguir si viene o no de la página de agradecimiento
                        if (greetingsPage)
                        {
                            ds = new DatoSimple() { CodeOP = 130, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };
                            db.DatosSimples.Add(ds);
                            du.Cerrada = true;
                            SaveChanges();

                            return Json(new { redirect = Url.Action("Tareas", "Alumno"), Parent = true });
                        }
                        else
                        {
                            //no ha visto la página de agradecimiento. le mandamos allí

                            ds = new DatoSimple() { CodeOP = 102, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };
                            db.DatosSimples.Add(ds);
                            SaveChanges();
                            //return Json(new { redirect = Url.Action("PL7_Texto", new { du.GrupoID, ModuloID = du.ModuloID, textoActual = du.TextoActual }), Parent = true });
                            return Json(new { redirect = Url.Action("Agradecimiento", new { duID = du.DatosUsuarioID }) });
                        }
                    }
                }
            }

            //eliminada por estar duplicada con la línea de antes del if, remover si faltara un dato al pasar por PL7_Siguiente_Pregunta
            //db.DatosSimples.Add(new DatoSimple() { CodeOP = 11, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, PreguntaID = PreguntaID, NumAccion = numAccion });

            //issue https://github.com/guirisan/ituinbook/issues/69#issuecomment-212217670
            //
            if (!preguntaResuelta)
            {
                db.DatosSimples.Add(new DatoSimple() { CodeOP = 11, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, PreguntaID = PreguntaID, NumAccion = numAccion });
                SaveChanges();

            }


            ds = new DatoSimple() { CodeOP = 100, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, NumAccion = numAccion };
            db.DatosSimples.Add(ds);

            du.AyudaStatus = 0;
            du.BuscaStatus = 0;
            du.PreguntaActual++;

            SaveChanges();

            /* 
            * to-do: mirar porque hace un try catch a pregunta() y texto()
            */
            try
            {
                //
                
                Pregunta preguntaTest = db.Textos.Find(TextoID).Preguntas.ToList()[db.SaveChanges()];

                return Json(new { redirect = Url.Action("PL7_Pregunta", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = du.PreguntaActual, textoID = TextoID, moment = moment, numAccion = numAccion }), Parent = false });
            }
            catch (Exception e)
            {
                du.TextoActual++;
                du.PreguntaActual = 0;
                du.AyudaStatus = 0;
                du.BuscaStatus = 0;

                db.SaveChanges();

                ds = new DatoSimple() { CodeOP = 50, DatosUsuarioID = du.DatosUsuarioID, Momento = datetimeclient, Dato01 = du.AyudaStatus, Dato02 = du.BuscaStatus, Dato03 = du.RevisaStatus, NumAccion = numAccion };
                db.DatosSimples.Add(ds);
                db.SaveChanges();

                return Json(new { redirect = Url.Action("PL7_Texto", new { du.GrupoID, ModuloID = du.ModuloID, textoActual = du.TextoActual }) });
            }
        }

        #region Pregunta TEST
        public ActionResult PL7_Pregunta_Test(int GrupoID, int ModuloID, int preguntaActual, int textoID, string moment, int numAccion = -1)
        {
            logger.Debug("PL7_Experimentos/PL7_Pregunta_Test");
            DateTime datetimeclient = DateTime.Parse(moment);

            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);

            db.SaveChanges();

            ViewBag.ConfigPregunta = config;

            DatoSimple dsR = new DatoSimple() { CodeOP = 52, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = pregunta.PreguntaID, Momento = datetimeclient, NumAccion = numAccion };

            db.DatosSimples.Add(dsR);

            SaveChanges();

            var seleccion = from ds in db.DatosSimples
                            where ds.PreguntaID == pregunta.PreguntaID &&
                            ds.TextoID == textoID &&
                            ds.CodeOP == 123 &&
                            du.DatosUsuarioID == du.DatosUsuarioID
                            select ds;


            if (seleccion != null && seleccion.Count() > 0)
            {
                ViewBag.TareaSel = true;
                ViewBag.Seleccion = seleccion.AsEnumerable().Last();
            }
            else
            {
                ViewBag.TareaSel = false;
            }

            return View(ext.GetPreguntaActual(texto, preguntaActual));
        }

        public ActionResult PL7_Pregunta_Test_2(int GrupoID, int ModuloID, int PreguntaID, string feedbackText)
        {
            logger.Debug("PL7_Experimentos/PL7_pregunta_test_2");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Pregunta pregunta = ext.GetPregunta(PreguntaID);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);

            db.SaveChanges();

            ViewBag.ConfigPregunta = config;

            var seleccion = from ds in db.DatosSimples
                            where ds.PreguntaID == pregunta.PreguntaID &&
                            ds.TextoID == pregunta.Texto.TextoID &&
                            ds.CodeOP == 123 &&
                            ds.DatosUsuarioID == du.DatosUsuarioID
                            select ds;

            var respuesta = from ds in db.DatosSimples
                            where ds.PreguntaID == pregunta.PreguntaID &&
                            ds.TextoID == pregunta.Texto.TextoID &&
                            ds.CodeOP == 13 &&
                            ds.DatosUsuarioID == du.DatosUsuarioID
                            select ds;

            if (respuesta != null && respuesta.Count() > 0)
            {
                ViewBag.Respuesta = respuesta.AsEnumerable().Last();
            }

            if (seleccion != null && seleccion.Count() > 0)
            {
                ViewBag.TareaSel = true;
                ViewBag.Seleccion = seleccion.AsEnumerable().Last();
            }
            else
            {
                ViewBag.TareaSel = false;
            }

            /*guirisan*/
            ViewBag.feedbackText = feedbackText;
            return View(pregunta);
        }

        public ActionResult PL7_Pregunta_Test_Seleccion(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            logger.Debug("PL7_Experimentos/PL7_pregunta_test_seleccion");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);


            db.SaveChanges();

            ViewBag.ConfigPregunta = config;

            return View(ext.GetPreguntaActual(texto, preguntaActual));
        }
        
        [HttpPost]
        public ActionResult PL7_Pregunta_Test_Seleccion_Validar(int GrupoID, int ModuloID, int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {
            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Double pert = 0, noPert = 0;
            Pregunta pregunta = ext.GetPregunta(PreguntaID);

            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            ValidarSeleccion(ModuloID, GrupoID, PreguntaID, pregunta.Texto.TextoID, respuesta, out pert, out noPert, true, moment);

            DatoSimple dsP = new DatoSimple() { CodeOP = 49, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Momento = datetimeclient, Dato01 = (float)pert, NumAccion = numAccion };
            DatoSimple dsNP = new DatoSimple() { CodeOP = 48, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Momento = datetimeclient, Dato01 = (float)noPert, NumAccion = numAccion };

            db.DatosSimples.Add(dsP);
            db.DatosSimples.Add(dsNP);

            SaveChanges();

            if (configPreg != null && configPreg.DosSeleccionarPertinente)
            {
                return Json(new { redirect = Url.Action("PL7_Pregunta_Test_Seleccion_2", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = du.PreguntaActual, textoID = du.TextoID }), Puntos = du.Puntos, mensaje = ext.GetFeedback(du), PreguntaID = pregunta.PreguntaID });
            }
            else
            {
                return Json(new { redirect = Url.Action("PL7_Pregunta_Test", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = du.PreguntaActual, TextoID = pregunta.Texto.TextoID, moment = moment, numAccion = numAccion }), Puntos = du.Puntos, mensaje = ext.GetFeedback(du), PreguntaID = pregunta.PreguntaID });
            }
        }

        [HttpPost]
        public ActionResult PL7_Pregunta_Test_Seleccion_2_Validar(int GrupoID, int ModuloID, int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {
            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Double pert = 0, noPert = 0;
            Pregunta pregunta = ext.GetPregunta(PreguntaID);

            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            ValidarSeleccion(ModuloID, GrupoID, PreguntaID, pregunta.Texto.TextoID, respuesta, out pert, out noPert, true, moment);

            DatoSimple dsP = new DatoSimple() { CodeOP = 49, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Momento = datetimeclient, Dato01 = (float)pert, NumAccion = numAccion };
            DatoSimple dsNP = new DatoSimple() { CodeOP = 48, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Momento = datetimeclient, Dato01 = (float)noPert, NumAccion = numAccion };

            db.DatosSimples.Add(dsP);
            db.DatosSimples.Add(dsNP);

            SaveChanges();

            return Json(new { redirect = Url.Action("PL7_Pregunta_Test", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = du.PreguntaActual, TextoID = pregunta.Texto.TextoID, moment = moment, numAccion = numAccion }), Puntos = du.Puntos, mensaje = ext.GetFeedback(du), PreguntaID = pregunta.PreguntaID });
        }

        public ActionResult PL7_Pregunta_Test_Seleccion_2(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);

            var seleccion = from dsa in db.DatosSimples
                            where dsa.PreguntaID == pregunta.PreguntaID &&
                            dsa.TextoID == pregunta.Texto.TextoID &&
                            dsa.CodeOP == 123 &&
                            du.DatosUsuarioID == du.DatosUsuarioID
                            select dsa;


            if (seleccion != null && seleccion.Count() > 0)
            {
                ViewBag.TareaSel = true;
                ViewBag.Seleccion = seleccion.AsEnumerable().Last();
            }
            else
            {
                ViewBag.TareaSel = false;
            }

            //guirisan: que cambios está guardando, si únicamente utiliza seleccion para consultarla?
            db.SaveChanges();

            ViewBag.ConfigPregunta = config;

            return View(ext.GetPreguntaActual(texto, preguntaActual));
        }

        public ActionResult PL7_Pregunta_Test_Seleccion_Simultaneo(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);


            db.SaveChanges();

            ViewBag.ConfigPregunta = config;

            return View(ext.GetPreguntaActual(texto, preguntaActual));
        }

        [HttpPost]
        public ActionResult PL7_Pregunta_Test_Seleccion_Simultaneo_Validar(int GrupoID, int ModuloID, int PreguntaID, string respuestaTest, string respuestaSel, string moment, int numAccion = -1, string dataRow = "")
        {
            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Double pert = 0, noPert = 0;
            Pregunta pregunta = ext.GetPregunta(PreguntaID);
            bool flag_fallo = false;
            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);
            float valor = 0;
            string mensaje = "";

            ValidarSeleccion(ModuloID, GrupoID, PreguntaID, pregunta.Texto.TextoID, respuestaSel, out pert, out noPert, true, moment);

            DatoSimple dsP = new DatoSimple() { CodeOP = 49, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Momento = datetimeclient, Dato01 = (float)pert, NumAccion = numAccion };
            DatoSimple dsNP = new DatoSimple() { CodeOP = 48, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Momento = datetimeclient, Dato01 = (float)noPert, NumAccion = numAccion };

            db.DatosSimples.Add(dsP);
            db.DatosSimples.Add(dsNP);

            SaveChanges();

            // ver si es ult o penultimo pertinente (PertinenteStatus)
            // ver porcentaje de pertinente encontrado (PertinenteOrden)
            if (du.PertinenteStatus == 2) // Último
            {
                db.DatosSimples.Add(new DatoSimple() { CodeOP = 28, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = du.PreguntaID, Momento = datetimeclient, NumAccion = numAccion });
            }
            else
            {
                if (du.PertinenteStatus == 1)
                {
                    db.DatosSimples.Add(new DatoSimple() { CodeOP = 42, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = du.PreguntaID, Momento = datetimeclient, NumAccion = numAccion });
                }
            }

            if (du.PertinenteOrden != null && du.PertinenteOrden != "")
            {
                double porc = CalculoPertinente(du.PertinenteOrden);

                db.DatosSimples.Add(new DatoSimple() { CodeOP = 29, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Dato01 = (float)porc, Momento = datetimeclient, NumAccion = numAccion });

                double porc2 = CalculoPertinenteSobreBusqueda(du.PertinenteOrden);

                db.DatosSimples.Add(new DatoSimple() { CodeOP = 30, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Dato01 = (float)porc2, Momento = datetimeclient, NumAccion = numAccion });
            }

            foreach (Alternativa alt in pregunta.Alternativas)
            {
                if (alt.Opcion == respuestaTest)
                {
                    if (alt.Valor) // Acierto
                    {
                        DatoSimple ds = new DatoSimple();

                        // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                        mensaje = (alt.FeedbackContenido == null ? (pregunta.FDBK_Correcto == null ? null : pregunta.FDBK_Correcto) : alt.FeedbackContenido);

                        du.Puntos += 100;

                        // Registrar respuesta
                        ds.CodeOP = 13;
                        ds.Info = respuestaTest;
                        //guirisan/secuencias
                        ds.Momento = datetimeclient;
                        ds.NumAccion = numAccion;

                        ds.PreguntaID = PreguntaID;
                        ds.TextoID = pregunta.Texto.TextoID;
                        ds.DatosUsuarioID = du.DatosUsuarioID;
                        ds.Dato01 = 100;
                        ds.Dato03 = 1; // Indica intento
                        db.DatosSimples.Add(ds);
                        du.DatoSimple.Add(ds);
                        du.PertinenteOrden = "";
                        valor = 1;

                        SaveChanges();

                        flag_fallo = false;
                    }
                    else // Fallo
                    {
                        DatoSimple ds = new DatoSimple();

                        // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                        mensaje = (alt.FeedbackContenido == null ? (pregunta.FDBK_Incorrecto == null ? null : pregunta.FDBK_Incorrecto) : alt.FeedbackContenido);

                        // Registrar respuesta
                        ds.CodeOP = 13;
                        ds.Info = respuestaTest;
                        //guirisan/secuencias
                        ds.Momento = datetimeclient;
                        ds.NumAccion = numAccion;

                        ds.PreguntaID = PreguntaID;
                        ds.TextoID = pregunta.Texto.TextoID;
                        ds.DatosUsuarioID = du.DatosUsuarioID;
                        ds.Dato01 = 0;
                        ds.Dato03 = 1; // Indica intento
                        db.DatosSimples.Add(ds);
                        du.DatoSimple.Add(ds);
                        du.PertinenteOrden = "";
                        valor = 0;

                        SaveChanges();
                        // Registrar respuesta CodeOP = 15

                        flag_fallo = true;
                    }

                    break;
                }
            }

            // Genera un feedback si no hay feedback de contenido ni de pregunta.
            mensaje = mensaje == null ? ext.GetFeedback(du) : mensaje;

            if (ext.GetModulo(ModuloID).Timings != null && ext.GetModulo(ModuloID).Timings.Count > 0)
            {
                mensaje = ProcesarTimings(mensaje, du, valor);
            }

            if (configPreg != null && (configPreg.DosIntentosTest && flag_fallo))
            {
                return Json(new { redirect = Url.Action("PL7_Pregunta_Test_2", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = PreguntaID }), Puntos = du.Puntos, mensaje = mensaje, PreguntaID = pregunta.PreguntaID });
            }
            else
            {
                return Json(new { redirect = Url.Action("PL7_Pregunta_Test_Resuelta", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = PreguntaID }), Puntos = du.Puntos, mensaje = mensaje, PreguntaID = pregunta.PreguntaID });
            }
        }

        [HttpPost]
        public ActionResult PL7_Pregunta_Test_Validar(int GrupoID, int ModuloID, int PreguntaID, string respuesta, string moment, int numAccion, string dataRow)
        {
            logger.Debug("PL7_Pregunta_Test_Validar");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            bool flag_fallo = false;
            float valor = 0;

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);

            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            /*
            //add dataRow to user file
            string path = @"C:\inetpub\wwwroot\datosRawReadAndLearn\" + User.Identity.Name + "_U" + ext.GetUsuarioID(User.Identity.Name) + "_G" + du.GrupoID + "_M" + du.ModuloID + ".txt";

            if (!System.IO.File.Exists(path))
            {
                System.IO.File.Create(path);
                System.IO.TextWriter tw = new System.IO.StreamWriter(path);
                tw.WriteLine(dataRow);
                tw.Close();
            }
            else if (System.IO.File.Exists(path))
            {
                System.IO.TextWriter tw = new System.IO.StreamWriter(path,true);
                tw.WriteLine(dataRow);
                tw.Close();
            }
            //end guirisan/secuencias
            */

            Pregunta pregunta = ext.GetPregunta(PreguntaID);

            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            string mensaje = "", explicacion = "";

            // ver si es ult o penultimo pertinente (PertinenteStatus)
            // ver porcentaje de pertinente encontrado (PertinenteOrden)
            if (du.PertinenteStatus == 2) // Último
            {
                db.DatosSimples.Add(new DatoSimple() { CodeOP = 28, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = du.PreguntaID, Momento = datetimeclient, NumAccion = numAccion });
            }
            else
            {
                if (du.PertinenteStatus == 1)
                {
                    db.DatosSimples.Add(new DatoSimple() { CodeOP = 42, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = du.PreguntaID, Momento = datetimeclient, NumAccion = numAccion });
                }
            }

            if (du.PertinenteOrden != null && du.PertinenteOrden != "")
            {
                double porc = CalculoPertinente(du.PertinenteOrden);

                db.DatosSimples.Add(new DatoSimple() { CodeOP = 29, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Dato01 = (float)porc, Momento = datetimeclient, NumAccion = numAccion });

                double porc2 = CalculoPertinenteSobreBusqueda(du.PertinenteOrden);

                db.DatosSimples.Add(new DatoSimple() { CodeOP = 30, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Dato01 = (float)porc2, Momento = datetimeclient, NumAccion = numAccion });
            }



            foreach (Alternativa alt in pregunta.Alternativas)
            {
                if (alt.Opcion == respuesta)
                {
                    explicacion = pregunta.Explicacion;
                    if (alt.Valor) // Acierto
                    {
                        DatoSimple ds = new DatoSimple();

                        // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                        mensaje = pregunta.FDBK_Correcto;

                        du.Puntos += 100;

                        // Registrar respuesta
                        ds.CodeOP = 13;
                        ds.Info = respuesta;
                        ds.Momento = datetimeclient;
                        ds.PreguntaID = PreguntaID;
                        ds.TextoID = pregunta.Texto.TextoID;
                        ds.DatosUsuarioID = du.DatosUsuarioID;
                        ds.Dato01 = 100;
                        ds.Dato03 = 1; // Indica intento
                        ds.NumAccion = numAccion;
                        db.DatosSimples.Add(ds);
                        du.DatoSimple.Add(ds);
                        du.PertinenteOrden = "";
                        valor = 1;

                        SaveChanges();

                        flag_fallo = false;
                    }
                    else // Fallo
                    {
                        DatoSimple ds = new DatoSimple();

                        // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                        mensaje = pregunta.FDBK_Incorrecto;

                        // Registrar respuesta
                        ds.CodeOP = 13;
                        ds.Info = respuesta;
                        ds.Momento = datetimeclient;
                        ds.PreguntaID = PreguntaID;
                        ds.TextoID = pregunta.Texto.TextoID;
                        ds.DatosUsuarioID = du.DatosUsuarioID;
                        ds.Dato01 = 0;
                        ds.Dato03 = 1; // Indica intento
                        ds.NumAccion = numAccion;
                        db.DatosSimples.Add(ds);
                        du.DatoSimple.Add(ds);
                        du.PertinenteOrden = "";
                        valor = 0;

                        SaveChanges();
                        // Registrar respuesta CodeOP = 15

                        flag_fallo = true;
                    }

                    break;
                }
            }

            

            if (ext.GetModulo(ModuloID).Timings != null && ext.GetModulo(ModuloID).Timings.Count > 0)
            {
                mensaje = ProcesarTimings(mensaje, du, valor);

                //guirisan/issues https://github.com/guirisan/ituinbook/issues/145
                //Si mensaje es = "", 
                int n = ext.GetModulo(ModuloID).Timings.First().PregLanzada;
                if (du.PreguntaActual > 0 && (((du.PreguntaActual + 1) % n) == 0))
                {
                    //pregunta múltiplo de N
                    return Json(new { redirect = Url.Action("PL7_Pregunta_Test_Resueltas", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = PreguntaID, feedbackText = mensaje, explicacionText = explicacion, preguntaActual = du.PreguntaActual, nPreg = n }), Puntos = du.Puntos, mensaje = mensaje, PreguntaID = pregunta.PreguntaID });
                }
                else
                {
                    return Json(new { redirect = Url.Action("PL7_Pregunta_Test_Resuelta_Directa", new { GrupoID = GrupoID, ModuloID = ModuloID, TextoID = du.TextoID, preguntaID = PreguntaID }) });
                }
            }

            if (configPreg != null && (configPreg.DosIntentosTest && flag_fallo))
            {
                return Json(new { redirect = Url.Action("PL7_Pregunta_Test_2", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = PreguntaID, feedbackText = mensaje }), Puntos = du.Puntos, mensaje = mensaje, PreguntaID = pregunta.PreguntaID });
            }
            else
            {
                return Json(new { redirect = Url.Action("PL7_Pregunta_Test_Resuelta", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = PreguntaID, feedbackText = mensaje, explicacionText = explicacion }), Puntos = du.Puntos, mensaje = mensaje, PreguntaID = pregunta.PreguntaID });
            }
        }

        public string ProcesarTimings(string mensaje, DatosUsuario du, float valor)
        {
            Modulo modulo = ext.GetModulo(du.ModuloID);
            string sms = "";

            var Timings = from t in modulo.Timings
                          select t;

            du.SumaAciertos += valor;

            if (du.SecuenciaAciertos == null || du.SecuenciaAciertos == "")
            {
                du.SecuenciaAciertos += valor.ToString();
            }
            else
            {
                du.SecuenciaAciertos += "/" + valor.ToString();
            }

            foreach (Timing tim in Timings)
            {
                if (tim.Tipo == 0) // Simple / Acumulativo
                {
                    if (du.ContadorFDBCAcum == (tim.PregLanzada - 1))
                    {
                        sms += du.FeedbackAcumulado;
                        sms +=  mensaje;
                        du.FeedbackAcumulado = "";
                        du.ContadorFDBCAcum = 0;
                    }
                    else
                    {
                        du.FeedbackAcumulado += mensaje;
                        du.ContadorFDBCAcum++;
                    }

                    SaveChanges();
                }
                else
                {
                    if ((du.PreguntaActual + 1) % tim.PregLanzada == 0)
                    {
                        string str = du.SecuenciaAciertos;
                        double total = 0.0;
                        string[] param = str.Split('/');

                        for (int i = 1; i <= tim.PregLanzada; i++)
                        {
                            total += Convert.ToDouble(param[param.Length - i]);
                        }

                        double porc = (100.0 * total) / (double)tim.PregLanzada;

                        string fdbk = tim.Feedback;

                        string[] items = fdbk.Split('/');

                        foreach (string tmp in items)
                        {
                            int posInf = tmp.IndexOf(")");
                            string inf = tmp.Substring(1, posInf - 1);

                            string strTmp = tmp.Substring(posInf);

                            int posSup = strTmp.IndexOf("(");

                            string sup = strTmp.Substring(posSup + 1, strTmp.Count() - posSup - 2);

                            if (Convert.ToDouble(inf) <= porc && Convert.ToDouble(sup) >= porc)
                            {
                                if (sms == null || sms == "")
                                    //sms += "<br />";

                                sms += tmp.Substring(posInf + 1, posSup - 1);

                                break;
                            }
                        }
                    }
                }
            }

            SaveChanges();

            return sms;
        }

        [HttpPost]
        public ActionResult PL7_Pregunta_Test_2_Validar(int GrupoID, int ModuloID, int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {
            logger.Debug("PL7_Pregunta_Test_2_Validar");
            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);


            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Pregunta pregunta = ext.GetPregunta(PreguntaID);

            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            string mensaje = "", explicacion = "";  

            if (du.PertinenteOrden != null && du.PertinenteOrden != "")
            {
                double porc = CalculoPertinente(du.PertinenteOrden);

                db.DatosSimples.Add(new DatoSimple() { CodeOP = 49, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Dato01 = (float)porc, Momento = datetimeclient, NumAccion = numAccion });

                double porc2 = CalculoPertinenteSobreBusqueda(du.PertinenteOrden);

                db.DatosSimples.Add(new DatoSimple() { CodeOP = 50, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Dato01 = (float)porc2, Momento = datetimeclient, NumAccion = numAccion });
            }

            DatoSimple ds = new DatoSimple();
            foreach (Alternativa alt in pregunta.Alternativas)
            {
                if (alt.Opcion == respuesta)

                    explicacion = pregunta.Explicacion;
                if (alt.Valor)// Acierto
                {
                    //comentado para que el feedback tras el segundo intento sea correctivo y no elaborativo
                    // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                    //mensaje = (alt.FeedbackContenido == null ? (pregunta.FDBK_Correcto == null ? null : pregunta.FDBK_Correcto) : alt.FeedbackContenido);
                    mensaje = pregunta.FDBK_Correcto;
                    du.Puntos += 100;

                    // Registrar respuesta
                    ds.CodeOP = 13;
                    ds.Info = respuesta;
                    //guirisan/secuencias
                    ds.Momento = datetimeclient;
                    ds.NumAccion = numAccion;

                    ds.PreguntaID = PreguntaID;
                    ds.TextoID = pregunta.Texto.TextoID;
                    ds.DatosUsuarioID = du.DatosUsuarioID;
                    ds.Dato01 = 100;
                    ds.Dato03 = 2; // Indica intento
                    db.DatosSimples.Add(ds);
                    du.DatoSimple.Add(ds);
                    du.PertinenteOrden = "";
                    SaveChanges();
                }
                else // Fallo
                {
                    //comentado para que el feedback tras el segundo intento sea correctivo y no elaborativo
                    // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                    //mensaje = (alt.FeedbackContenido == null ? (pregunta.FDBK_Incorrecto == null ? null : pregunta.FDBK_Incorrecto) : alt.FeedbackContenido);
                    mensaje = pregunta.FDBK_Incorrecto;
                    // Registrar respuesta
                    ds.CodeOP = 13;
                    ds.Info = respuesta;
                    //guirisan/secuencias
                    ds.Momento = datetimeclient;
                    ds.NumAccion = numAccion;

                    ds.PreguntaID = PreguntaID;
                    ds.TextoID = pregunta.Texto.TextoID;
                    ds.DatosUsuarioID = du.DatosUsuarioID;
                    ds.Dato01 = 0;
                    ds.Dato03 = 2; // Indica intento
                    db.DatosSimples.Add(ds);
                    du.DatoSimple.Add(ds);
                    du.PertinenteOrden = "";

                    SaveChanges();
                }
            }

            // Genera un feedback si no hay feedback de contenido ni de pregunta.   
            //comentado porque borraba el feedback
            //mensaje = ext.GetFeedback(du);

            return Json(new { redirect = Url.Action("PL7_Pregunta_Test_Resuelta", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = PreguntaID, feedbackText = mensaje, explicacionText = explicacion }), Puntos = du.Puntos, PreguntaID = pregunta.PreguntaID });
        }

        public ActionResult PL7_Pregunta_Test_Resuelta(int GrupoID, int ModuloID, int preguntaID, string feedbackText, string explicacionText = "")
        {
            logger.Debug("PL7_Pregunta_Test_Resuelta");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Pregunta pregunta = new Pregunta();

            DatoSimple ds = du.DatoSimple.Last(p => p.CodeOP == 13);

            pregunta = ext.GetPregunta((int)preguntaID);

            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);
            ViewBag.ConfigPregunta = ext.GetConfigPregunta((int)preguntaID);

            ViewBag.DatosUsuario = du;
            ViewBag.DatoSimple = ds;
            //guirisan
            ViewBag.feedbackText = feedbackText;
            //guirisan/issues https://github.com/guirisan/ituinbook/issues/60
            //añadido texto de explicacion al viewbag
            ViewBag.explicacionText = explicacionText;
            ViewBag.AyudaFlota = BuscarAccion(120, GrupoID, ModuloID, pregunta.Texto.TextoID, pregunta.PreguntaID);
            // Buscar en el registro la respuesta dada a esta pregunta

            return View(pregunta);
        }

        public ActionResult PL7_Pregunta_Test_Resuelta_Directa(int GrupoID, int ModuloID, int preguntaID)
        {
            logger.Debug("PL7_Pregunta_Test_Resuelta");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Pregunta pregunta = new Pregunta();

            DatoSimple ds = du.DatoSimple.Last(p => p.CodeOP == 13);

            pregunta = ext.GetPregunta((int)preguntaID);

            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);
            ViewBag.ConfigPregunta = ext.GetConfigPregunta((int)preguntaID);

            ViewBag.DatosUsuario = du;
            ViewBag.DatoSimple = ds;
            
            return View(pregunta);
        }

        public ActionResult PL7_Pregunta_Test_Resueltas(int GrupoID, int ModuloID, int preguntaID, string feedbackText, string explicacionText = "", int preguntaActual = 0, int nPreg = 0)
        {
            logger.Debug("PL7_Pregunta_Test_Resueltas");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Pregunta[] preguntas = new Pregunta[nPreg]; //variable para cebar el modelo
            ViewBag.Preguntas = new Pregunta[nPreg]; //array para preguntas en ViewBag
            ViewBag.DatosSimples = new DatoSimple[nPreg]; //array para los datossimples correspondientes
            preguntaActual = preguntaActual - (nPreg - 1);     //ajustamos preguntaActual a la primera de las que ha realizado
                                                         //para mostrar 5 6 7 8, en lugar de 8 7 6 5 (poder hacer el for sumando)

            ICollection<DatoSimple> ds = du.DatoSimple.Where(x => x.CodeOP == 13).ToList();
            //guirisan/issues
            //cargar y asociar a cada pregunta el datosimple para saber que se ha respondido. si, esto tenía que pasar...

            for (int i = 0; i < nPreg; i++)
            {
                Pregunta p = ext.GetPreguntaActual(ext.GetTexto(du.TextoID),preguntaActual);
                p.ConfigPregunta = ext.GetConfigPregunta(p.PreguntaID);

                ViewBag.Preguntas[i] = p;
                preguntas[i] = p;
                
                preguntaActual++;

                //datossimples
                ViewBag.DatosSimples[i] = ds.FirstOrDefault(x => x.PreguntaID == p.PreguntaID);
            }

            if (explicacionText != "")
            {
                ViewBag.explicacionIntegrada = explicacionText;
            }
            
            


            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ViewBag.DatosUsuario = du;
            
            ViewBag.feedbackText = feedbackText;
            //guirisan/issues https://github.com/guirisan/ituinbook/issues/60
            //añadido texto de explicacion al viewbag
            ViewBag.explicacionText = explicacionText;

            return View(preguntas.ToList());
        }
        
        #endregion //region pregunta_test

        #region Pregunta Seleccion
        public ActionResult PL7_Pregunta_Seleccion(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);


            db.SaveChanges();

            ViewBag.ConfigPregunta = config;

            var seleccion = from ds in db.DatosSimples
                            where ds.PreguntaID == pregunta.PreguntaID &&
                            ds.TextoID == textoID &&
                            ds.CodeOP == 123 &&
                            du.DatosUsuarioID == du.DatosUsuarioID
                            select ds;


            if (seleccion != null && seleccion.Count() > 0)
            {
                ViewBag.TareaSel = true;
                ViewBag.Seleccion = seleccion.AsEnumerable().Last();
            }
            else
            {
                ViewBag.TareaSel = false;
            }

            return View(ext.GetPreguntaActual(texto, preguntaActual));
        }

        [HttpPost]
        public ActionResult PL7_Pregunta_Seleccion_Validar(int GrupoID, int ModuloID, int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {
            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Double pert = 0, noPert = 0;
            Pregunta pregunta = ext.GetPregunta(PreguntaID);
            string mensaje = "";

            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            ValidarSeleccion(ModuloID, GrupoID, PreguntaID, pregunta.Texto.TextoID, respuesta, out pert, out noPert, false, moment);

            DatoSimple dsP = new DatoSimple() { CodeOP = 51, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Momento = datetimeclient, Dato01 = (float)pert, NumAccion = numAccion };
            DatoSimple dsNP = new DatoSimple() { CodeOP = 50, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Momento = datetimeclient, Dato01 = (float)noPert, NumAccion = numAccion };

            db.DatosSimples.Add(dsP);
            db.DatosSimples.Add(dsNP);

            SaveChanges();

            DatoSimple ds = new DatoSimple();

            // Registrar respuesta
            ds.CodeOP = 13;
            ds.Info = respuesta;
            //guirisan/secuencias
            ds.Momento = datetimeclient;
            ds.NumAccion = numAccion;

            ds.PreguntaID = PreguntaID;
            ds.TextoID = pregunta.Texto.TextoID;
            ds.DatosUsuarioID = du.DatosUsuarioID;
            ds.Dato01 = (float)pert;
            ds.Dato02 = (float)noPert;

            db.DatosSimples.Add(ds);
            du.DatoSimple.Add(ds);

            mensaje = ext.GetFeedback(du);

            SaveChanges();

            return Json(new { redirect = Url.Action("PL7_Pregunta_Seleccion_Resuelta", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaID = pregunta.PreguntaID }), Puntos = du.Puntos, mensaje = mensaje, PreguntaID = pregunta.PreguntaID });
        }

        public ActionResult PL7_Pregunta_Seleccion_Resuelta(int GrupoID, int ModuloID, int preguntaID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Pregunta pregunta = new Pregunta();

            DatoSimple ds = du.DatoSimple.Last(p => p.CodeOP == 13);

            pregunta = ext.GetPregunta((int)preguntaID);

            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);
            ViewBag.ConfigPregunta = ext.GetConfigPregunta((int)preguntaID);

            ViewBag.DatosUsuario = du;
            ViewBag.DatoSimple = ds;
            ViewBag.AyudaFlota = BuscarAccion(34, GrupoID, ModuloID, pregunta.Texto.TextoID, pregunta.PreguntaID);
            // Buscar en el registro la respuesta dada a esta pregunta

            var seleccion = from dsa in db.DatosSimples
                            where dsa.PreguntaID == pregunta.PreguntaID &&
                            dsa.TextoID == pregunta.Texto.TextoID &&
                            dsa.CodeOP == 124 &&
                            du.DatosUsuarioID == du.DatosUsuarioID
                            select dsa;


            if (seleccion != null && seleccion.Count() > 0)
            {
                ViewBag.TareaSel = true;
                ViewBag.Seleccion = seleccion.AsEnumerable().Last();
            }
            else
            {
                ViewBag.TareaSel = false;
            }

            return View(pregunta);
        }
        
        #endregion

        #region PreguntaAbierta
        public ActionResult PL7_Pregunta_Abierta(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            logger.Debug("PL7_Pregunta_Abierta");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);


            db.SaveChanges();

            ViewBag.ConfigPregunta = config;

            var seleccion = from ds in db.DatosSimples
                            where ds.PreguntaID == pregunta.PreguntaID &&
                            ds.TextoID == textoID &&
                            ds.CodeOP == 123 &&
                            du.DatosUsuarioID == du.DatosUsuarioID
                            select ds;


            if (seleccion != null && seleccion.Count() > 0)
            {
                ViewBag.TareaSel = true;
                ViewBag.Seleccion = seleccion.AsEnumerable().Last();
            }
            else
            {
                ViewBag.TareaSel = false;
            }

            return View(ext.GetPreguntaActual(texto, preguntaActual));
        }

        public ActionResult PL7_Pregunta_Abierta_2(int GrupoID, int ModuloID, int PreguntaID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Pregunta pregunta = ext.GetPregunta(PreguntaID);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);

            db.SaveChanges();

            ViewBag.ConfigPregunta = config;

            var seleccion = from ds in db.DatosSimples
                            where ds.PreguntaID == pregunta.PreguntaID &&
                            ds.TextoID == pregunta.Texto.TextoID &&
                            ds.CodeOP == 123 &&
                            ds.DatosUsuarioID == du.DatosUsuarioID
                            select ds;

            var respuesta = from ds in db.DatosSimples
                            where ds.PreguntaID == pregunta.PreguntaID &&
                            ds.TextoID == pregunta.Texto.TextoID &&
                            ds.CodeOP == 13 &&
                            ds.DatosUsuarioID == du.DatosUsuarioID
                            select ds;

            if (respuesta != null && respuesta.Count() > 0)
            {
                ViewBag.Respuesta = respuesta.AsEnumerable().Last();
            }

            if (seleccion != null && seleccion.Count() > 0)
            {
                ViewBag.TareaSel = true;
                ViewBag.Seleccion = seleccion.AsEnumerable().Last();
            }
            else
            {
                ViewBag.TareaSel = false;
            }

            return View(pregunta);
        }

        public ActionResult PL7_Pregunta_Abierta_Seleccion(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);

            db.SaveChanges();

            ViewBag.ConfigPregunta = config;

            return View(ext.GetPreguntaActual(texto, preguntaActual));
        }

        public ActionResult PL7_Pregunta_Abierta_Seleccion_2(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);

            var seleccion = from ds in db.DatosSimples
                            where ds.PreguntaID == pregunta.PreguntaID &&
                            ds.TextoID == textoID &&
                            ds.CodeOP == 123 &&
                            du.DatosUsuarioID == du.DatosUsuarioID
                            select ds;


            if (seleccion != null && seleccion.Count() > 0)
            {
                ViewBag.TareaSel = true;
                ViewBag.Seleccion = seleccion.AsEnumerable().Last();
            }
            else
            {
                ViewBag.TareaSel = false;
            }

            db.SaveChanges();

            ViewBag.ConfigPregunta = config;

            return View(ext.GetPreguntaActual(texto, preguntaActual));
        }

        [HttpPost]
        public ActionResult PL7_Pregunta_Abierta_Seleccion_Validar(int GrupoID, int ModuloID, int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            Double pert = 0, noPert = 0;
            Pregunta pregunta = ext.GetPregunta(PreguntaID);

            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            ValidarSeleccion(ModuloID, GrupoID, PreguntaID, pregunta.Texto.TextoID, respuesta, out pert, out noPert, true, moment, numAccion);

            if (pert > 70 && noPert < 35)
            {
                return Json(new { redirect = Url.Action("PL7_Pregunta_Abierta", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = du.PreguntaActual, TextoID = pregunta.Texto.TextoID }), Puntos = du.Puntos, mensaje = GetFeedbackSeleccion(du = du, pert, noPert), PreguntaID = pregunta.PreguntaID });
            }
            else
            {
                return Json(new { redirect = Url.Action("PL7_Pregunta_Abierta_Seleccion_2", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = du.PreguntaActual, TextoID = pregunta.Texto.TextoID }), Puntos = du.Puntos, mensaje = GetFeedbackSeleccion(du = du, pert, noPert), PreguntaID = pregunta.PreguntaID });
            }
        }



        [HttpPost]
        public ActionResult PL7_Pregunta_Abierta_Seleccion_2_Validar(int GrupoID, int ModuloID, int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            Double pert = 0, noPert = 0;
            Pregunta pregunta = ext.GetPregunta(PreguntaID);

            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            ValidarSeleccion(ModuloID, GrupoID, PreguntaID, pregunta.Texto.TextoID, respuesta, out pert, out noPert, true, moment, numAccion);

            return Json(new { redirect = Url.Action("PL7_Pregunta_Abierta", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaActual = du.PreguntaActual, TextoID = pregunta.Texto.TextoID }), Puntos = du.Puntos, mensaje = GetFeedbackSeleccion(du = du, pert, noPert), PreguntaID = pregunta.PreguntaID });
        }

        public ActionResult PL7_Pregunta_Abierta_Validar(int GrupoID, int ModuloID, int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {
            logger.Debug("PL7_Pregunta_Abierta_Validar");
            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Double pert = 0, noPert = 0;
            Pregunta pregunta = ext.GetPregunta(PreguntaID);
            string mensaje = "";

            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            string[,] Criterios = new string[pregunta.Criterios.Count(), 2];

            int i = 0;
            foreach (Criterio cri in pregunta.Criterios)
            {
                Criterios[i, 0] = cri.Opcion.ToLower();
                Criterios[i, 1] = cri.Valor.ToString();
                i++;
            }

            DatoSimple ds = new DatoSimple();

            if (respuesta != null && respuesta != "")
            {
                ds.Dato01 = (float)ext.Corrector(Criterios, respuesta.ToLower());

                if (ds.Dato01 == 0)
                {
                    // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                    mensaje = (pregunta.FDBK_Correcto == null ? null : pregunta.FDBK_Incorrecto);
                }
                else
                {
                    du.Puntos += 100;
                    // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                    mensaje = (pregunta.FDBK_Correcto == null ? null : pregunta.FDBK_Correcto);
                }
            }
            else
            {
                // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                mensaje = (pregunta.FDBK_Incorrecto == null ? null : pregunta.FDBK_Incorrecto);
            }

            // Registrar respuesta
            ds.CodeOP = 13;
            ds.Info = respuesta;
            //guirisan/secuencias
            ds.Momento = datetimeclient;
            ds.NumAccion = numAccion;

            ds.PreguntaID = PreguntaID;
            ds.TextoID = pregunta.Texto.TextoID;
            ds.DatosUsuarioID = du.DatosUsuarioID;
            ds.Info = respuesta;
            ds.Dato03 = 1; // Indica intento
            db.DatosSimples.Add(ds);
            du.DatoSimple.Add(ds);

            SaveChanges();


            // Genera un feedback si no hay feedback de contenido ni de pregunta.
            mensaje = mensaje == null ? ext.GetFeedback(du) : mensaje;


            if (configPreg != null && (configPreg.DosIntentosAbierta && ds.Dato01 == 0))
            {
                return Json(new { redirect = Url.Action("PL7_Pregunta_Abierta_2", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = PreguntaID }), Puntos = du.Puntos, mensaje = mensaje, PreguntaID = pregunta.PreguntaID });
            }
            else
            {
                return Json(new { redirect = Url.Action("PL7_Pregunta_Abierta_Resuelta", new { GrupoID = GrupoID, ModuloID = ModuloID, preguntaID = pregunta.PreguntaID }), Puntos = du.Puntos, mensaje = GetFeedbackSeleccion(du, pert, noPert), PreguntaID = pregunta.PreguntaID });

            }

        }

        [HttpPost]
        public ActionResult PL7_Pregunta_Abierta_2_Validar(int GrupoID, int ModuloID, int PreguntaID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {

            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);

            Pregunta pregunta = ext.GetPregunta(PreguntaID);

            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            string mensaje = "";

            if (du.PertinenteOrden != null && du.PertinenteOrden != "")
            {
                double porc = CalculoPertinente(du.PertinenteOrden);

                db.DatosSimples.Add(new DatoSimple() { CodeOP = 49, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Dato01 = (float)porc, Momento = datetimeclient, NumAccion = numAccion });

                double porc2 = CalculoPertinenteSobreBusqueda(du.PertinenteOrden);

                db.DatosSimples.Add(new DatoSimple() { CodeOP = 50, DatosUsuarioID = du.DatosUsuarioID, TextoID = du.TextoID, PreguntaID = PreguntaID, Dato01 = (float)porc2, Momento = datetimeclient, NumAccion = numAccion });
            }

            string[,] Criterios = new string[pregunta.Criterios.Count(), 2];

            int i = 0;
            foreach (Criterio cri in pregunta.Criterios)
            {
                Criterios[i, 0] = cri.Opcion.ToLower();
                Criterios[i, 1] = cri.Valor.ToString();
                i++;
            }

            DatoSimple ds = new DatoSimple();

            if (respuesta != null && respuesta != "")
            {
                ds.Dato01 = (float)ext.Corrector(Criterios, respuesta.ToLower());
                du.Puntos += 100;
                // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                mensaje = (pregunta.FDBK_Correcto == null ? null : pregunta.FDBK_Correcto);
            }
            else
            {
                // Comprueba si hay Feedback de Contenido o de pregunta en esa prioridad.
                mensaje = (pregunta.FDBK_Incorrecto == null ? null : pregunta.FDBK_Incorrecto);
            }

            // Registrar respuesta
            ds.CodeOP = 13;
            ds.Info = respuesta;
            //guirisan/secuencias
            ds.Momento = datetimeclient;
            ds.NumAccion = numAccion;

            ds.PreguntaID = PreguntaID;
            ds.TextoID = pregunta.Texto.TextoID;
            ds.DatosUsuarioID = du.DatosUsuarioID;
            ds.Info = respuesta;
            ds.Dato03 = 2; // Indica intento
            db.DatosSimples.Add(ds);
            du.DatoSimple.Add(ds);

            SaveChanges();

            // Genera un feedback si no hay feedback de contenido ni de pregunta.            
            mensaje = ext.GetFeedback(du);

            return Json(new { redirect = Url.Action("PL7_Pregunta_Abierta_Resuelta", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = PreguntaID }), Puntos = du.Puntos, mensaje = mensaje, PreguntaID = pregunta.PreguntaID });
        }

        public ActionResult PL7_Pregunta_Abierta_Resuelta(int GrupoID, int ModuloID, int preguntaID)
        {
            logger.Debug("PL7_Pregunta_Abierta_Resuelta");
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Pregunta pregunta = new Pregunta();

            DatoSimple ds1 = du.DatoSimple.Last(p => p.CodeOP == 13 && p.Dato03 == 1);

            var dato = from d in du.DatoSimple
                       where d.CodeOP == 13 && d.Dato03 == 2
                       select d;

            /*
             (from ds in db.DatosSimples
            where ds.DatosUsuarioID == du.DatosUsuarioID
            select ds).ToList().Last().Momento.ToString();*/

            pregunta = ext.GetPregunta((int)preguntaID);

            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);
            ViewBag.ConfigPregunta = ext.GetConfigPregunta((int)preguntaID);

            ViewBag.DatosUsuario = du;

            if (ds1 != null)
            {
                ViewBag.DatoSimple = ds1;
            }

            if (dato != null)
            {
                ViewBag.DatoSimple2 = dato.ToList().LastOrDefault();
            }

            ViewBag.AyudaFlota = BuscarAccion(120, GrupoID, ModuloID, pregunta.Texto.TextoID, pregunta.PreguntaID);
            // Buscar en el registro la respuesta dada a esta pregunta

            var seleccion = from dsa in db.DatosSimples
                            where dsa.PreguntaID == pregunta.PreguntaID &&
                            dsa.TextoID == pregunta.Texto.TextoID &&
                            dsa.CodeOP == 123 &&
                            du.DatosUsuarioID == du.DatosUsuarioID
                            select dsa;


            if (seleccion != null && seleccion.Count() > 0)
            {
                ViewBag.TareaSel = true;
                ViewBag.Seleccion = seleccion.AsEnumerable().Last();
            }
            else
            {
                ViewBag.TareaSel = false;
            }

            return View(pregunta);
        }

        #endregion

        #region Pregunta Ordenar
        public ActionResult PL7_Pregunta_Ordenar(int GrupoID, int ModuloID, int preguntaActual, int textoID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            Texto texto = ext.GetTexto(textoID);
            Pregunta pregunta = ext.GetPreguntaActual(texto, preguntaActual);

            ViewBag.DatosUsuario = du;
            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);

            ConfigPregunta config = ext.GetConfigPregunta(pregunta.PreguntaID);

            if (config == null)
            {
                config = new ConfigPregunta { PreguntaID = pregunta.PreguntaID };
                db.ConfigPregunta.Add(config);
                pregunta.ConfigPregunta = config;
                db.SaveChanges();
            }

            db.SaveChanges();

            ViewBag.ConfigPregunta = config;

            TareaOrdenar tareaOrdenar = db.TareasOrdenar.Find(pregunta.TareaID);//13/*pregunta.TareaID*/);

            return View(tareaOrdenar);
        }

        [HttpPost]
        public ActionResult PL7_Pregunta_Ordenar_Validar(int GrupoID, int ModuloID, int PreguntaID, int TareaOrdenarID, string respuesta, string moment, int numAccion = -1, string dataRow = "")
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));

            //guirisan/secuencias
            DateTime datetimeclient = DateTime.Parse(moment);
            ext.AddDataRow(User.Identity.Name, ext.GetUsuarioID(User.Identity.Name), GrupoID, ModuloID, dataRow);


            TareaOrdenar tareaOrdenar = db.TareasOrdenar.Find(TareaOrdenarID);
            Pregunta pregunta = ext.GetPregunta(PreguntaID);

            ConfigPregunta configPreg = ext.GetConfigPregunta(PreguntaID);

            string[] param = respuesta.Split(new string[] { "s3p4r40r" }, StringSplitOptions.None);

            string mensaje = "";
            int i = 1, coincidencia = 0;

            foreach (string str in param)
            {
                if (str != "undefined")
                    coincidencia += tareaOrdenar.ItemsOrdenados.First(s => s.Item == str).Order == i ? 1 : 0;

                i++;
            }

            float porcAcierto = (float)coincidencia / (float)param.Length;

            if (porcAcierto > 0.75) // Acierto
            {
                mensaje = "Has ordenado bien.";
            }
            else
            {
                mensaje = "Has ordenado mal.";
            }

            /*DatoSimple ds = new DatoSimple();

            // Registrar respuesta
            ds.CodeOP = 15;
            ds.Info = respuesta;
            ds.Momento = datetimeclient;
            ds.PreguntaID = PreguntaID;
            ds.TextoID = pregunta.Texto.TextoID;
            ds.DatosUsuarioID = du.DatosUsuarioID;
            ds.Dato01 = (float)porcAcierto;            

            db.DatosSimples.Add(ds);
            du.DatoSimple.Add(ds);

            SaveChanges();*/

            DatoSimple dso = new DatoSimple();

            // Registrar respuesta
            dso.CodeOP = 13;
            dso.Info = respuesta;
            //guirisan/secuencias
            dso.Momento = datetimeclient;
            dso.NumAccion = numAccion;

            dso.PreguntaID = PreguntaID;
            dso.TextoID = pregunta.Texto.TextoID;
            dso.DatosUsuarioID = du.DatosUsuarioID;
            dso.Dato01 = (float)porcAcierto;
            dso.Info2 = mensaje;
            db.DatosSimples.Add(dso);
            du.DatoSimple.Add(dso);

            SaveChanges();

            // Genera un feedback si no hay feedback de contenido ni de pregunta.
            //mensaje = mensaje == null ? ext.GetFeedback() : mensaje;

            return Json(new { redirect = Url.Action("PL7_Pregunta_Ordenar_Resuelta", new { GrupoID = GrupoID, ModuloID = ModuloID, PreguntaID = PreguntaID, TareaOrdenarID = TareaOrdenarID }), Puntos = du.Puntos, mensaje = mensaje, PreguntaID = PreguntaID });
        }

        public ActionResult PL7_Pregunta_Ordenar_Resuelta(int GrupoID, int ModuloID, int preguntaID, int TareaOrdenarID)
        {
            DatosUsuario du = ext.GetDatosUsuarios(ModuloID, GrupoID, ext.GetUsuarioID(User.Identity.Name));
            Pregunta pregunta = new Pregunta();

            DatoSimple ds = du.DatoSimple.Last(p => p.CodeOP == 13);

            pregunta = ext.GetPregunta((int)preguntaID);

            ViewBag.ConfigModulo = ext.GetConfigModulo(ModuloID);
            ViewBag.ConfigPregunta = ext.GetConfigPregunta((int)preguntaID);

            ViewBag.Pregunta = pregunta;

            ViewBag.DatosUsuario = du;
            ViewBag.DatoSimple = ds;
            ViewBag.AyudaFlota = BuscarAccion(120, GrupoID, ModuloID, pregunta.Texto.TextoID, pregunta.PreguntaID);
            // Buscar en el registro la respuesta dada a esta pregunta

            var seleccion = from dsa in db.DatosSimples
                            where dsa.PreguntaID == pregunta.PreguntaID &&
                            dsa.TextoID == pregunta.Texto.TextoID &&
                            dsa.CodeOP == 126 &&
                            du.DatosUsuarioID == du.DatosUsuarioID
                            select dsa;


            if (seleccion != null && seleccion.Count() > 0)
            {
                ViewBag.TareaSel = true;
                ViewBag.Seleccion = seleccion.AsEnumerable().Last();
            }
            else
            {
                ViewBag.TareaSel = false;
            }

            TareaOrdenar tareaOrdenar = db.TareasOrdenar.Find(pregunta.TareaID);//13/*pregunta.TareaID*/);

            return View(tareaOrdenar);
        }
        #endregion

        
        

    }
}

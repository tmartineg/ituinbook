using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace ReadAndLearn.Models
{
    public class ClientErrorHandler : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            var response = filterContext.RequestContext.HttpContext.Response;
            response.Write(filterContext.Exception.Message);
            response.ContentType = MediaTypeNames.Text.Plain;
            filterContext.ExceptionHandled = true;
        }
    }

    public class ExternalMethods
    {
        Contexto db = new Contexto();

        /**
         * Escribe en el fichero de datosRaw (C:\inetpub\wwwroot\datosRawReadAndLearn) del usuario una nueva línea. Si el fichero no existe, lo crea.
         * El nombre de fichero tiene el siguiente formato: UserName + "_U" + UserID + "_G" + GrupoID + "_M" + ModuloID + ".txt"
         * p.ej. alu12_U12_G1_M1.txt
         * Recibe el nombre e ID de usuario, GrupoID, ModuloID, y los datos a imprimir en el fichero.
         */
        public bool AddDataRow(string UserName, int UserID, int GrupoID, int ModuloID, string dataRow)
        {   
            string path = @"C:\inetpub\wwwroot\datosRawReadAndLearn\" + UserName + "_U" + UserID + "_G" + GrupoID + "_M" + ModuloID + ".txt";
            /*
             * da fallo tanto al crear un nuevo archivo como al intentar añadir líneas.
             * para más inri, solo con algunos usuarios (con user alu1 funciona OK, con alu21 falla en el if y en el else
             * diciendo "El archivo ... está siendo utilizado por otro proceso"
             */
            /*  
            try
            {
                if (!System.IO.File.Exists(path))
                {
                    System.IO.File.Create(path);
                    System.IO.TextWriter tw = new System.IO.StreamWriter(path);
                    tw.WriteLine(dataRow);
                    tw.Close();
                }
                else if (System.IO.File.Exists(path))
                {
                    System.IO.TextWriter tw = new System.IO.StreamWriter(path, true);
                    tw.WriteLine(dataRow);
                    tw.Close();
                }
            }
              */
            try
            {
                System.IO.File.AppendAllLines(path, new[] { dataRow });
            }
            catch (Exception e)
            {
                //error writing datosRow
                return false;
            }
            return true;
        }
        
        public void SaveChanges()
        {
            db.SaveChanges();
        }

        public int GetUsuarioID(string Usuario)
        {
            try
            {
                var user = (from u in db.UserProfiles
                            where Usuario == u.UserName
                            select u).Single();
                return user.UserId;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public DatosUsuario GetDatosUsuarios(int moduloID, int grupoID, int usuario)
        {
            try
            {
                var datosUser = (from dat in db.DatosUsuario
                                 where dat.ModuloID == moduloID &&
                                 dat.GrupoID == grupoID &&
                                 dat.UserProfileID == usuario
                                 select dat).ToList().First();

                return datosUser;
            }
            catch (Exception)
            {
                return null;
            }

        }

        public Modulo GetModulo(int ModuloID)
        {
            try { 
                return db.Modulos.Find(ModuloID);
            }
            catch(Exception)
            {
                return null;
            }
        }

        public Texto GetTextoActual(int moduloID, int textoActual)
        {
            try
            {
                return db.Modulos.Find(moduloID).Textos.ToList()[textoActual];
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Texto GetTexto(int textoID)
        {
            try
            {
                return db.Textos.Find(textoID);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Pregunta GetPreguntaActual(Texto texto, int preguntaActual)
        {
            try
            {
                return texto.Preguntas.ToList()[preguntaActual];
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Pregunta GetPregunta(int preguntaID)
        {
            try
            {
                return db.Preguntas.Find(preguntaID);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public ConfigPregunta GetConfigPregunta(int preguntaID)
        {
            try
            {
                return db.ConfigPregunta.Single(c => c.PreguntaID == preguntaID);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public ConfigModulo GetConfigModulo(int ModuloID)
        {
            try
            {
                return db.ConfigModulo.Single(c => c.ModuloID == ModuloID);
            }
            catch (Exception)
            {
                return null;
            }
        }

        //guirisan/secuencias
        //los siguientes métodos permiten conocer el árbol de pertenencia de una pregunta - texto - modulo
        //end guirisan/secuencias


        public string GetFeedback(DatosUsuario du)
        {
            Modulo modulo = db.Modulos.Find(du.ModuloID);
            
            foreach (ReglaCompleja regla in modulo.ReglasComplejas.Reverse())
            {
                if (analisisReglaCompleja(regla.ReglaComplejaID, du))
                    return regla.Feedback;
            }

            return "";
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

        public bool analisisReglaCompleja(int ReglaCID, DatosUsuario du)
        {
            var Regla = db.ReglasComplejas.Find(ReglaCID);

            switch (Regla.OpCode)
            {
                case 1: // RS vs null
                    var Regla1 = getReglaS(Regla.Regla_1);

                    return analisisReglaSimple(Regla1.Variable, Regla1.Operador, Convert.ToDouble(Regla1.Param), du);
                case 2: // null vs RS
                    var Regla2 = getReglaS(Regla.Regla_2);

                    return analisisReglaSimple(Regla2.Variable, Regla2.Operador, Convert.ToDouble(Regla2.Param), du);
                case 3: // RC vs null
                    return analisisReglaCompleja(Regla.Regla_1, du);
                case 4: // null vs RC
                    return analisisReglaCompleja(Regla.Regla_2, du);
                case 5: // RS vs RS
                    var Regla5a = getReglaS(Regla.Regla_1);
                    var Regla5b = getReglaS(Regla.Regla_2);

                    return operadorLogico(analisisReglaSimple(Regla5a.Variable, Regla5a.Operador, Convert.ToDouble(Regla5a.Param), du), analisisReglaSimple(Regla5b.Variable, Regla5b.Operador, Convert.ToDouble(Regla5b.Param), du), Regla.Operador);
                case 6: // RS vs RC
                    var Regla6 = getReglaS(Regla.Regla_1);

                    return operadorLogico(analisisReglaSimple(Regla6.Variable, Regla6.Operador, Convert.ToDouble(Regla6.Param), du), analisisReglaCompleja(Regla.Regla_2, du), Regla.Operador);
                case 7: // RC vs RS
                    var Regla7 = getReglaS(Regla.Regla_2);

                    return operadorLogico(analisisReglaCompleja(Regla.Regla_1, du), analisisReglaSimple(Regla7.Variable, Regla7.Operador, Convert.ToDouble(Regla7.Param), du), Regla.Operador);
                case 8: // RC vs RC
                    return operadorLogico(analisisReglaCompleja(Regla.Regla_1, du), analisisReglaCompleja(Regla.Regla_2, du), Regla.Operador);
                default: // Others
                    return false;
            }
        }

        public bool analisisReglaSimple(int VarID, int OpS, double Param, DatosUsuario du)
        {
            var regla = db.ReglasSimples.Find(VarID);
            var dato = getDato(VarID, du); // Para sacar el dato que queremos


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


        //param i: variable de regla simple 
        public double getDato(int i, DatosUsuario du)
        {
            IQueryable<DatoSimple> ds;

            if (i < 8) // Variables de Texto
            {
                ds = (from d in db.DatosSimples
                      where d.DatosUsuarioID == du.DatosUsuarioID &&
                      d.CodeOP == i &&
                      d.TextoID == du.TextoID
                      select d);
            }
            else // Variables de Pregunta
            { 
                ds = (from d in db.DatosSimples
                      where d.DatosUsuarioID == du.DatosUsuarioID &&
                      d.CodeOP == i &&
                      d.PreguntaID == du.PreguntaID &&
                      d.TextoID == du.TextoID
                      select d);
            }

            

            switch (i) // Todo se calcula a nivel de pregunta y no como computo global de todas las preguntas. Las variables de texto van a parte...
            {
                /******************************************************************************************
                 * ****************************************************************************************
                 * ****** I = codeop en https://docs.google.com/spreadsheets/d/1YLiGMUn1XTluUPOgGNVYMQW2JUUyGxm-euywYARER6Y/
                 * ****************************************************************************************
                 * ****************************************************************************************/
                case 5: // Lectura inicial
                    return ds.ToList().LastOrDefault() != null ? (Convert.ToDouble(ds.ToList().LastOrDefault().Info) / 1000.0) : -1.0;        
                case 6:
                    return ds.ToList().LastOrDefault() != null ? Convert.ToDouble(ds.ToList().LastOrDefault().Info) : -1.0;        
                case 9: // Intento de pregunta
                    if (ds.ToList().Count > 0)
                        return 2.0;
                    else
                        return -1.0;
                case 13:
                    //CODIGO ORIGNAL DE CASE13
                    //return ds.ToList().LastOrDefault() != null ? Convert.ToDouble(ds.ToList().LastOrDefault().Dato01) : -1.0;
                    //DEVUELVE ALGO ASÍ COMO EL PORCENTAJE DE ACIERTO EN PREGUNTAS DE TEST SOBRE EL MÓDULO ACTUAL. PARECE INUTILIZABLE

                    //return 0 -> error
                    //return 1 -> acierto
                    return Convert.ToDouble(ds.ToList().LastOrDefault().Info2);
                case 16: // Número de lecturas de enunciado
                    return Convert.ToDouble(ds.ToList().Count);
                case 17: // Número de lecturas de alternativas
                    return Convert.ToDouble(ds.ToList().Count); 
                case 24: // Número de búsquedas
                    return Convert.ToDouble(ds.ToList().Count);  
                case 34: // Número de Ayudas Parafraseo
                    return Convert.ToDouble(ds.ToList().Count);  
                case 48:
                    return 0;
                case 49:
                    return 0;
                case 50:
                    //porcentaje de acierto en la selección
                    return ds.ToList().Last().Dato01;
                case 51:
                    //porcentaje de distractoras en la selección
                    return ds.ToList().Last().Dato01;
                case 52:
                    if (ds.ToList().Count > 0)
                        return 1.0;
                    else
                        return 0.0;
                case 53:
                    return ds.ToList().Last().Dato01;
                case 54:
                    return ds.ToList().Last().Dato01;
                case 55:
                    return ds.ToList().Last().Dato01;
                case 56:
                    return ds.ToList().Last().Dato01;
                case 58:
                    return ds.ToList().Last().Dato01;


                default:
                    return 0;
            }
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

        #region Preguntas Abiertas

        public double Corrector(string[,] CriterioCorrecion, string respuesta)
        {
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
                if (CriterioCorrecion[i, 0] != null)
                {
                    Correccion[i] = FilterStrings(respuesta, CriterioCorrecion[i, 0]);
                    Correccion[i] = Correccion[i] * Convert.ToDouble(CriterioCorrecion[i, 1]);
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
            if (Puntuacion < 0)
                Puntuacion = 0;

            return Puntuacion;
        }

        static int FilterStrings(string str1, string str2)
        {
            List<char> FCar = new List<char>() { '.', ',', ';', ':', '-', '_', '¿', '?', '!', '¡', '*', '+', '\'', '\"', '&', '(', ')', '=', '$', '/', '#', '%', 'º', 'ª' };
            List<string> FWords = new List<string>() { "a", "acá", "ahí", "algo", "alguien", "algún", "alguna", "parte", "allí", "allá", "aquí", "bastante", "cerca", "de", "demasiado", "demasiado", "demasiada", "demasiados", 
                                                       "demasiadas", "él", "el", "la", "ella", "ellos", "ellas", "entonces", "eso", "es", "está", "este", "esta", "estos", "estas", "esto", "hay", "lejos", "los", "las", "más", 
                                                       "menos", "mi", "mis", "mucho", "mucha", "muchos", "muchas", "muy", "nada", "nadie", "ninguna", "nunca", "otro", "otra", "otros", "otra", "poco", "poco", "poca", 
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


            foreach (string PalabraR in RealWords2)
            {
                if (PalabraR != "FW")
                {
                    int nPalabrasF = 0;
                    int a = 0;

                    CompareRW[y, 0] = PalabraR;
                    foreach (string PalabraF in RealWords1)
                    {
                        if (PalabraF != "FW")
                        {
                            a++;
                            CompareRW[y, a] = PalabraF;
                        }
                    }
                    y++;
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

                try
                {

                    if ((CorrectPerc / (NWords2 - NFunct2)) == 1)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (Exception e)
                {
                    return 0;
                }
            }
        }

        static int LevenshteinDistance(string s, string t, out double porcentaje)
        {
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
        #endregion
    }
}
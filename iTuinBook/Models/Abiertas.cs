using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace ReadAndLearn.Models
{
    public class Abiertas
    {
        public int Corrector(string[,] CriterioCorrecion, string respuesta, int nCriteriosMax)
        {   
            int MaximoPositivo = 0;
            int MaximoNegativo = 0;
            int[] Correccion = new int[CriterioCorrecion.Length];
            // 'nCriteriosMax, es un entero que le informa de cuantos criterios de corrección hemos utilizado como máximo.

            // 'Si el alumno no responde 0
            respuesta = respuesta.Trim();

            if (respuesta == "")
                return 0;

            // 'Si hay respuesta la analizamos en un bucle, pasando por todos los criterios de corrección introducidos.
            for (int i = 0; i < CriterioCorrecion.Length; i++)
            { 
                // 'Los criterios pueden estar vacios, ya que ese máximo es compartido por todas las preguntas.
                if (CriterioCorrecion[i,0] == null)
                {
                    Correccion[i] = FilterStrings(respuesta, CriterioCorrecion[i,0]);
                    Correccion[i] = Correccion[i] * Convert.ToInt32(CriterioCorrecion[i,1]);
                }
            }

            for (int j = 0; j < CriterioCorrecion.Length; j++)
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

            int Puntuacion;
            Puntuacion = MaximoPositivo + MaximoNegativo;

            if (Puntuacion < 0)
                Puntuacion = 0;

            return Puntuacion;
        }

        #region Funcione Externas

        static int FilterStrings(string str1, string str2)
        {
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

                foreach (string PalabraR in RealWords1)
                {
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

                foreach (string PalabraR in RealWords2)
                {
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
            double[,] Distancias = new double[2, 3]; 
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
                    LevenshteinDistance(CompareRW[xs, 1], CompareRW[xs, ys], out Distancias[xs, ys]);
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
                        if (CompareRW[xs, 1].Count() >= 6)
                        {
                            if (Distancias[xs, ys] <= 2)
                            {
                                Match = true; // 'si la palabra clave tiene más de 6 letras se admiten 2 errores tipográficos
                            }
                        }
                        
                        if (CompareRW[xs, 1].Count() >= 5)
                        {
                            if (Distancias[xs, ys] <= 1)
                            {
                                Match = true; // 'si la palabra clave tiene más de 6 letras se admiten 2 errores tipográficos
                            }
                        }

                        if (CompareRW[xs, 1].Count() >= 4)
                        {
                            if (Distancias[xs, ys] == 0)
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
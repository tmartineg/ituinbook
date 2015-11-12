using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ReadAndLearn.Controllers
{
    /*public class PruebaController : Controller
    {
        //
        // GET: /Prueba/

        public ActionResult Index()
        {
          

            return View();
        }
        
        public bool analisisReglaCompleja(int ReglaCID)
        { 
            var Regla = db.ReglasComplejas.Find(ReglaCID);
            
            switch(Regla.OpCode)
            {
                case 1: // RS vs null
                    var Regla1 = getReglaS(Regla.Regla_1);

                    return analisisReglaSimple(Regla1.Variable, Regla1.OperadorS, Regla1.Parametro);
                case 2: // null vs RS
                    var Regla2 = getReglaS(Regla.Regla_2);

                    return analisisReglaSimple(Regla2.Variable, Regla2.OperadorS, Regla2.Parametro);
                case 3: // RC vs null
                    return analisisReglaCompleja(Regla_1);
                case 4: // null vs RC
                    return analisisReglaCompleja(Regla_2);
                case 5: // RS vs RS
                    var Regla1 = getReglaS(Regla.Regla_1);
                    var Regla2 = getReglaS(Regla.Regla_2);

                    return operadorLogico(analisisReglaSimple(Regla1.Variable, Regla1.OperadorS, Regla1.Parametro), analisisReglaSimple(Regla2.Variable, Regla2.OperadorS, Regla2.Parametro), Regla.OperadorC);
                case 6: // RS vs RC
                    var Regla1 = getReglaS(Regla.Regla_1);

                    return operadorLogico(analisisReglaSimple(Regla1.Variable, Regla1.OperadorS, Regla1.Parametro), analisisReglaCompleja(Regla_2), Regla.OperadorC);                    
                case 7: // RC vs RS
                    var Regla2 = getReglaS(Regla.Regla_2);

                    return operadorLogico(analisisReglaCompleja(Regla_1), analisisReglaSimple(Regla2.Variable, Regla2.OperadorS, Regla2.Parametro), Regla.OperadorC);                    
                case 8: // RC vs RC
                    return operadorLogico(aanalisisReglaCompleja(Regla_1), analisisReglaCompleja(Regla_2), Regla.OperadorC);                                    
                default: // Others
                    return false;                
            }
        }

        public bool analisisReglaSimple(int VarID, int OpS, int Param)
        {
            var dato = db.ReglasSimples(VarID);

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

        
    }*/
}

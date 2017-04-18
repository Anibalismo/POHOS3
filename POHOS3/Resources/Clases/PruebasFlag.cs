using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TfhkaNet.IF.VE;

namespace POHOS3.Resources
{
    class PruebasFlag
    {
        Tfhka impresora;

        public PruebasFlag( Tfhka _impresora )
        {
            impresora = _impresora;

            descripcionFlag[00] = @"Impresión de errores en factura
                                    00 = No imprime los mensajes de error, solo se muestran en el display.
                                    01 = Imprime los mensajes de error";
            descripcionFlag[01] = "";
            descripcionFlag[02] = "";
            descripcionFlag[03] = "";
        }

        public void textFlag00()
        {
            impresora.SendCmd("PJ0001");
            impresora.SendCmd("!000000010000001000Test");
            impresora.SendCmd("W000000010000001000Test");
            impresora.SendCmd("!101");
            impresora.SendCmd("PJ0000");
        }

        private string[] descripcionFlag = new string[64];
         
    }
}

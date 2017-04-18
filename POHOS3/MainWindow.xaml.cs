using System;
using System.Windows;
using TfhkaNet.IF.VE;
using System.Threading;
using System.ComponentModel;
using System.Windows.Input;
using System.Reflection;
using System.Globalization;
using System.Windows.Forms;

using System.IO;
using MessageBox = System.Windows.Forms.MessageBox;

using System.Text.RegularExpressions;

namespace POHOS3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region VARIABLES
        // El objeto de la impresora
        Tfhka impresora = new Tfhka();

        // Para enviar los comandos en otro thread
        private System.ComponentModel.BackgroundWorker backgroundWorker1;

        // Almacena la dirección del archivo donde está siendo almacenado el textbox
        string archivoActivo;

        // Primera prueba de traer archivos como parte del proyecto
        //        string archivoPrueba = POHOS3.Properties.Resources.Pruebas_de_Caracteres;

        // Un archivo temporal para almacenar el textbox mientras el usuario le asigna un sitio permanente
        string archivoTemporal = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";

        // Variable auxiliar para enviar continuamente el contenido del textbox
        bool CicloEnviar = false;

        // Para precargar un archivo.
        // string archivoPruebaString = POHOS3.Properties.Resources.Pruebas_de_Caracteres;



        // Para saber por cuál línea de text voy
        int LineaTextoEnviando = 0;

        // Este menú no estoy muy seguro si debería ser global en este contexto
        System.Windows.Controls.MenuItem Menu_Comunicaciones = new System.Windows.Controls.MenuItem();

        // Para verificar si la impresora está abierta o no
        bool ImpresoraAbierta = false;

        // Para las lecturas de archivos
        Assembly _assembly;
        StreamReader _textStreamReader;

        #endregion


        public MainWindow()
        {
            InitializeComponent();

            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            backgroundWorker1.WorkerSupportsCancellation = true;

            //TODO: Esto debe ser dinámico. Que el programa determine qué está conectado y lo seleccione auto

            impresora.SendCmdRetryAttempts = 1;
            impresora.SendCmdRetryInterval = 1000;
            InitializeBackgroundWorker();
            impresora.ReadFpStatus();
            
            Assembly _assembly;
            StreamReader _textStreamReader;
            _assembly = Assembly.GetExecutingAssembly();
            _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.TextFile1.txt"));
            TextBox1.Text = _textStreamReader.ReadToEnd();
            //TextBox1.Text = "FACTURA";
            
            //Excel
            ExcelData exceldata = new ExcelData();
            DataGrid1.DataContext = exceldata;


            //string crearPLU(string cmd, int _tasa, float _precio, float _cantidad, string descripcion);
            string comando = crearPLU("credito", 1, 123.123f, 123.123f, "descripcion");


            //A veces quito los botones que me sobran
            Button_Abrir.Visibility = System.Windows.Visibility.Hidden;
            Button_Guardar.Visibility = System.Windows.Visibility.Hidden;
            //Button_Guess.Visibility = System.Windows.Visibility.Hidden;
            //Button_Stop.Visibility = System.Windows.Visibility.Hidden;
            Button_Repetir.Content = "Repetir";
            Menu();

            //TextBox1.FontFamily = SystemFonts.CaptionFontFamily;
            controlUsuario1.Button1Click += controlUsuario1_Button1Click;


        }

        private void controlUsuario1_Button1Click(object sender, RoutedEventArgs e )
        {
            MessageBox.Show("hola Hugo");
        }

    private void btnPruebas_Click(object sender, RoutedEventArgs e)
        {
            var botonInicio = sender as System.Windows.Controls.Button;
            if (botonInicio != null)
            {

                if (botonInicio.Background == System.Windows.Media.Brushes.Green)
                {
                    // TODO: generar textbox según parametros especificados por el usuario
                    EnviarArchivo(TextBox1.Text);
                    botonInicio.Background = System.Windows.Media.Brushes.Red;
                    botonInicio.Content = "Detener";
                }
                else {
                    // TODO: cancelar el envío
                    botonInicio.Background = System.Windows.Media.Brushes.Green;
                    botonInicio.Content = "Iniciar";
                }

                parserTextoHKA(TextBox1.Text);
                
            }
        }

        private void GenerarComandosCicloDePruebas()
        {
            int FacturasPorCiclo = Convert.ToInt16(textBox_Facturas.Text);
            int NDDPorCiclo = Convert.ToInt16(textBox_NDD.Text);
            int NDCPorCiclo = Convert.ToInt16(textBox_NDC.Text);




        }


        public void Menu()
        {
            Menu_Comunicaciones.Header = "Comunicaciones";

            MainMenu1.Items.Add(Menu_Comunicaciones);
            CargarPuertos();
        }

        private void CargarPuertos() {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            
            Menu_Comunicaciones.Items.Clear();

            foreach (string puerto in ports)
            {
                System.Windows.Controls.MenuItem Menu_Parcial = new System.Windows.Controls.MenuItem();
                Menu_Parcial.Header = puerto;
                Menu_Parcial.Click += SeleccionPuertoComunicaciones;
                Menu_Comunicaciones.Items.Add(Menu_Parcial);
            }
        }

        #region EVENTOS_DE_VENTANA
        private void SeleccionPuertoComunicaciones(object sender, System.EventArgs e)
        {
            string Puerto = ((System.Windows.Controls.MenuItem)sender).Header.ToString();

            try
            {
                impresora.OpenFpCtrl(Puerto);
                ImpresoraAbierta = true;
                Menu_Comunicaciones.Header = "Cerrar puerto " + Puerto;
                Menu_Comunicaciones.Click -= SeleccionPuertoComunicaciones;
                Menu_Comunicaciones.Click += CerrarPuertoSeleccionado;
                Menu_Comunicaciones.Items.Clear();

            }
            catch
            {
                MessageBox.Show("Error al intentar abrir el puerto de comunicaciones");
            }
        }

        private void CerrarPuertoSeleccionado(object sender, System.EventArgs e) {
            try
            {
                impresora.CloseFpCtrl();

                Menu_Comunicaciones.Header = "Comunicaciones";
                Menu_Comunicaciones.Click -= CerrarPuertoSeleccionado;
                CargarPuertos();
                ImpresoraAbierta = false;


            }
            catch {
                MessageBox.Show("Hubo un error al intentar cerrar el puerto com");
            }
        }
        #endregion

        // Se inicializan algunos parametros de la tarea en segundo plano
        private void InitializeBackgroundWorker()
        {
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            backgroundWorker1.WorkerReportsProgress = true;
        }

        // Se pregunta al usuario qué archivo abrir y se carga en el textbox
        private void Button_Abrir_Click(object sender, RoutedEventArgs e)
        {
            if (backgroundWorker1.IsBusy != true)
            {

                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Archivos de Texto |*.txt";

                dlg.InitialDirectory = System.IO.Directory.GetCurrentDirectory() + "\\Ejemplos";
                if (dlg.ShowDialog() == true)
                {
                    // Start the asynchronous operation.
                    archivoActivo = dlg.FileName;
                }

                try
                {
                    using (StreamReader sr = new StreamReader(dlg.FileName))
                    {
                        String line = sr.ReadToEnd();
                        TextBox1.Text = line;
                        nombreArchivoStatusBar.Text = dlg.FileName;
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }


        // El manejador de eventos que "maneja" el resultado de la operación en segundo plano
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Primer, si hay una excepción, se maneja desde acá
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                // Si el usuario cancela la operación (con el boton de "Detener")
                habilitarBotones();

            }
            else
            {
                // Una tercera condición, donde la tarea termina correctamente
                habilitarBotones();
            }
        }

        private void habilitarBotones()
        {
            //            Boton_Abrir.IsEnabled = true;
            //            Boton_Enviar.IsEnabled = true;
        }

        private void deshabilitarBotones()
        {

            //            Boton_Abrir.IsEnabled = false;
            //            Boton_Enviar.IsEnabled = false;
        }

        // Manejador del evento de enviar el contenido del textbox
        private void Button_Enviar_Click(object sender, RoutedEventArgs e)
        {
            EnviarArchivo(TextBox1.Text);
        }

        private void EnviarArchivo(string fileText)
        {
            if (ImpresoraAbierta == true)
            {
                // Si el textbox no está vacio, se envia el texto del textbox
                if (fileText != "")
                {
                    File.WriteAllText(archivoTemporal, fileText);
                    archivoActivo = archivoTemporal;

                    // Si no se está enviando un archivo, se envia
                    if (backgroundWorker1.IsBusy != true)
                    {
                        backgroundWorker1.RunWorkerAsync(archivoActivo);
                    }
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("No se ha conectado una impresora");
            }
        }

        public void parserTextoHKA( string texto )
        {
            string[] palabrasClave = { "factura", "credito", "debito" };

            // Si el textbox no está vacio, se envia el texto del textbox
            if (texto != "")
            {
                string ArchivoTemporal = System.Environment.CurrentDirectory + Path.DirectorySeparatorChar + "ArchivoTemporal.txt";
                File.Create(ArchivoTemporal).Close();

                string[] lineasArchivo;//= File.ReadAllLines(archivoTemporal);

                lineasArchivo = texto.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                foreach (var linea in lineasArchivo)
                {
                    bool contains =false;
                    string comando = "vacio";

                    foreach (var _comando in palabrasClave)
                    {
                        contains |= linea.IndexOf( _comando, StringComparison.OrdinalIgnoreCase) >= 0;

                        if (contains)
                        {
                            comando = _comando;
                            break;
                        }
                    }

                    if (contains)
                    {
                        string cantidadCiclos = getBetween(linea, "(", ")");
                        int ciclos = 0;
                        if (cantidadCiclos == "")
                            ciclos = 1;
                        else
                            ciclos = Convert.ToInt16(cantidadCiclos);


                        /////////
                        ///////// Acá poner la parte de que se genere un comando random, y que se inserte
                        string line;
                        _assembly = Assembly.GetExecutingAssembly();
                        _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources."+ comando + ".txt"));

                        bool insertarComandosS2 = true;

                        do
                        {
                            while ((line = _textStreamReader.ReadLine()) != null)
                            {
                                // inserta la linea en el archivo nuevo
                                File.AppendAllText(ArchivoTemporal, line + Environment.NewLine);

                                if (insertarComandosS2)
                                {
                                    File.AppendAllText(ArchivoTemporal, "S2" + Environment.NewLine);
                                }

                            }
                        } while (--ciclos > 0);
                        /////////
                        /////////


                    }
                    else
                    {
                        File.AppendAllText("POHOS3.Resources.temporal.txt", linea);
                    }


                }
            }
        }


        string crearPLU(string _cmd, int _tasa, float _precio, float _cantidad, string descripcion)
        {
            string cmd = "";
            string tasa = _tasa.ToString();

            if (_tasa >= 1 && _tasa <= 4)
            {
                if (_cmd == "factura") { cmd = (0x30 + _tasa).ToString(); }
                else if (_cmd == "credito") { cmd = "d" + tasa; }
                else if (_cmd == "debito") { cmd = "`" + tasa; }
            }

            else
                throw new ArgumentNullException("value");

            string precio = Math.Round(_precio, 2).ToString();
            precio = precio.Replace(",", "");
            while (precio.Length < 10)
                precio = "0" + precio;

            string cantidad = Math.Round(_cantidad, 3).ToString();
            cantidad = cantidad.Replace(",", "");
            while (cantidad.Length < 8)
                cantidad = "0" + cantidad;

            return cmd + precio + cantidad + descripcion;
        }

        Random rnd1 = new Random();

        public string crearPLUAleatorio( string tipoDocumento , int montoMaximo , int itemsMaximos)
        {
            string[] palabrasClave = { "factura", "credito", "debito" };

            string RunningPath = AppDomain.CurrentDomain.BaseDirectory;
            string Archivo = string.Format("{0}Resources\\Diccionario.txt", Path.GetFullPath(Path.Combine(RunningPath, @"..\..\")));
            string[] lineasArchivo = File.ReadAllLines(Archivo);

            int indice = rnd1.Next(lineasArchivo.Length);

            string cmd = palabrasClave[rnd1.Next(2)];
            int tasa = rnd1.Next(1, 4 + 1);
            float precio = (float) (rnd1.Next(1, montoMaximo));
            float cantidad = (float)(rnd1.Next(1, itemsMaximos));
            string descripciónPLU = lineasArchivo[indice];

            return crearPLU(cmd,tasa,precio,cantidad,descripciónPLU);
        }

        public string crearDocumentoAleatorio(string tipoDocumento , int CantidadPLUMaxima)
        {
            string[] palabrasClave = { "factura", "credito", "debito" };
            string ArchivoTemporal = System.Environment.CurrentDirectory + Path.DirectorySeparatorChar + "DocumentoTemporal.txt";
            File.Create(ArchivoTemporal).Close();

            Random rnd = new Random();
            int CantidadPLU = rnd.Next(CantidadPLUMaxima);

            do
            {
                string PLU = crearPLUAleatorio(tipoDocumento, 300, 5);
                File.AppendAllText(ArchivoTemporal, PLU + Environment.NewLine);

            } while (--CantidadPLU>0);

            return ArchivoTemporal;
        }

        #region IDEAS_PENDIENTES
        private struct _MontosRestantes
        {
            public double Tasa0;
            public double Tasa1;
            public double Tasa2;
            public double Tasa3;
        } 

        //TODO terminar
        private _MontosRestantes calculaMontoDesbordeZ()
        {
            _MontosRestantes MontosRestantesZ;
            //SV para saber qué impresora es, S1 también.
            //S3 Para leer el flag del monto máximo
            //U0Z: y extraer el monto de las facturas del día

            //esto puede tirar error por el tema de que no todas las impresoras responden al SV
            TfhkaNet.IF.SVPrinterData sv = impresora.GetSVPrinterData();

            S1PrinterData s1 = impresora.GetS1PrinterData();
            string modeloImpresora = s1.RegisteredMachineNumber;

            modeloImpresora = modeloImpresora.Substring(0, 3);

            int flagMontoRestante=15;

            switch ( modeloImpresora )
            {
                case "Z6B":
                    flagMontoRestante = 15;
                    //seleccionar acá el flag que debo consultar
                    break;

                default:
                    break;
            }

            TfhkaNet.IF.VE.S3PrinterData s3 = impresora.GetS3PrinterData();

            //byte _flagMontoRestante = s3.AllSystemFlags.GetValue(flagMontoRestante);

            // comparar el flag leido contra la lista de flags, y los valores de máximo dinero que puedo leer.

            double montoMaximo = 9999999f;
            switch (flagMontoRestante)
            {
                case 0:
                    //el monto máximo por z es 9.999.999
                    break;
                case 1:
                    //el monto máximo por z es 999.999
                    break;
                case 2:
                    //el monto máximo por z es 99.999
                    break;
                default:
                    break;
            }

            TfhkaNet.IF.VE.ReportData _acumuladoX = impresora.GetXReport();

            MontosRestantesZ.Tasa3 = montoMaximo - _acumuladoX.AdditionalRate3Sale;
            MontosRestantesZ.Tasa1 = montoMaximo - _acumuladoX.GeneralRate1Sale;
            MontosRestantesZ.Tasa2 = montoMaximo - _acumuladoX.ReducedRate2Sale;
            MontosRestantesZ.Tasa0 = montoMaximo - _acumuladoX.FreeSalesTax;
        
            return MontosRestantesZ;

        }

        private void btnGenerarScript_Click(object sender, RoutedEventArgs e)
        {
            bool generarZMitadDia = (bool)checkBox_ZMitadDia.IsChecked;
            int facturas = Convert.ToInt16(textBox_Facturas.Text.ToString());
            int notaCredito = Convert.ToInt16(textBox_NDC.Text.ToString());
            int notaDebito = Convert.ToInt16(textBox_NDD.Text.ToString());

            crearDocumentoAleatorio("factura" , 5);



        }



        #endregion


        #region ACTUALIZAR_GUI
        String TiempoDeEnvio;
        String TiempoDeRecepcion;
        string lineaEnviada, LineaRecibida;

        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }

        private string incrementarDia( )
        {

            impresora.SendCmd("I0Z");

            Thread.Sleep(15000); // :/

            S1PrinterData reporteS1 = impresora.GetS1PrinterData();
            DateTime dateTime = reporteS1.CurrentPrinterDateTime;
            DateTime dateTime2;

            dateTime2 = dateTime.AddDays(1);

            string diaIncrementado = dateTime2.ToString("ddMMyy");

            impresora.SendCmd("PG" + diaIncrementado);

            return diaIncrementado;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string archivo = e.Argument.ToString();
            string[] lineasArchivo = File.ReadAllLines(archivo);

            do
            {
                for (int i = 0; i < lineasArchivo.Length && !backgroundWorker1.CancellationPending; i++)
                {
                    LineaTextoEnviando = i;
                    string linea = lineasArchivo[i];
                    if (linea != "")
                    {

                        //
                        // estas comprovaciones de palabras especiales las voy a quitar cuando implemente el parser
                        if (linea.Contains("FACTURA()") || linea.Contains("factura()"))
                        {
                            Int16 ciclos;

                            string palabra = getBetween(linea, "(", ")");

                            if (palabra == "")
                                ciclos = 1;
                            else
                                ciclos = Convert.ToInt16(palabra);

                            while (ciclos-- > 0)
                            {
                                string line;
                                _assembly = Assembly.GetExecutingAssembly();
                                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.factura.txt"));

                                while ((line = _textStreamReader.ReadLine()) != null)
                                {
                                    impresora.SendCmd(line);
                                }

                                Thread.Sleep(2000);
                                impresora.GetPrinterStatus();
                                _textStreamReader.Close();
                            }


                        }
                        else
                        if ( linea.Contains("retardoms()") || linea.Contains("RETARDOMS()") || linea.Contains("retardominutos()") || linea.Contains("RETARDOMINUTOS()"))
                        { }
                        else
                        if (linea.Contains("NOTACREDITO()") || linea.Contains("notacredito()"))
                            {
                                Int16 ciclos;

                            string palabra = getBetween(linea, "(", ")");

                            if (palabra == "")
                                ciclos = 1;
                            else
                                ciclos = Convert.ToInt16(palabra);

                            while (ciclos-- > 0)
                            {
                                string line;
                                _assembly = Assembly.GetExecutingAssembly();
                                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.nc.txt"));

                                while ((line = _textStreamReader.ReadLine()) != null)
                                {
                                    impresora.SendCmd(line);
                                }
                                Thread.Sleep(2000);
                                impresora.GetPrinterStatus();                           
                                _textStreamReader.Close();
                            }
                        }
                        else
                        if (linea.Contains("NOTADEBITO()") || linea.Contains("notadebito()"))
                        {
                            Int16 ciclos;

                            string palabra = getBetween(linea, "(", ")");

                            if (palabra == "")
                                ciclos = 1;
                            else
                                ciclos = Convert.ToInt16(palabra);

                            while (ciclos-- > 0)
                            {
                                string line;
                                _assembly = Assembly.GetExecutingAssembly();
                                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.nd.txt"));

                                while ((line = _textStreamReader.ReadLine()) != null)
                                {
                                    impresora.SendCmd(line);
                                }
                                Thread.Sleep(2000);
                                impresora.GetPrinterStatus();
                                _textStreamReader.Close();
                            }
                        }
                        else
                        if (linea == "INCREMENTAR_DIA()")
                        {
                            incrementarDia();
                        }
                        else
                            try
                            {
                                if (linea == "S1")
                                {
                                    S1PrinterData reporteS1 = impresora.GetS1PrinterData();
                                }
                                else
                                if (linea == "S2" || linea == "s2") impresora.GetS2PrinterData();
                                else
                                if (linea == "S3" || linea == "s3") impresora.GetS3PrinterData();
                                else
                                if (linea == "S4" || linea == "s4") impresora.GetS4PrinterData();
                                else
                                if (linea == "S5" || linea == "s5") impresora.GetS5PrinterData();
                                else
                                if (linea == "S8E" || linea == "s8e") impresora.GetS8EPrinterData();
                                else
                                if (linea == "S8P" || linea == "s8p") impresora.GetS8PPrinterData();
                                else
                                if (linea == "5") impresora.CheckFPrinter();
                                else
                                if (linea == "05") impresora.CheckFPrinter();
                                else
                                if (linea == "U0X4") impresora.GetX4Report();
                                else
                                if (linea == "U0X5") impresora.GetX5Report();
                                else
                                if (linea == "U0X2") impresora.GetX2Report();
                                else
                                if (linea == "U0X7") impresora.GetX7Report();
                                else // si todas las demás comprobaciones fallan, envía lo que leiste
                                    impresora.SendCmd(linea);

                            }
                            catch (Exception)
                            {
                                MessageBox.Show("La impresora no respondió al comando " + linea);
                                //throw;
                            }


                        if (linea.Contains("RETARDOMS") || linea.Contains("retardoms"))
                        {
                            Int16 tiempoArgumento;

                            string palabra = getBetween(linea, "(", ")");

                            if (palabra == "")
                                tiempoArgumento = 1;
                            else
                                tiempoArgumento = Convert.ToInt16(palabra);

                            TimeSpan tiempo = new TimeSpan(0, 1, 0);
                            Thread.Sleep(1);
                            Thread.Sleep(tiempoArgumento);
                        }
                        else
                        if (linea.Contains("RETARDOMINUTOS") || linea.Contains("retardominutos"))
                        {
                            Int16 tiempoArgumento;

                            string palabra = getBetween(linea, "(", ")");

                            if (palabra == "")
                                tiempoArgumento = 1;
                            else
                                tiempoArgumento = Convert.ToInt16(palabra);

                            TimeSpan tiempo = new TimeSpan(0, tiempoArgumento, 0);
                            Thread.Sleep(tiempo);
                        }
                        else
                        { //meter acá la conversión de la hora para el monitor :( 
                            lineaEnviada = linea;
                            TiempoDeEnvio = "Env " + DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);

                            //Poner otra vez otro día
                            //LineaRecibida = BitConverter.ToString(impresora.SerialPortInputBuffer).Replace("-", " ");

                            TiempoDeRecepcion = "Rec " + DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
                            backgroundWorker1.ReportProgress(0);
                        }


                    }

                    else
                    {
                        #region verificacionDeSiPuedoSeguirEnviandoComandos
                        bool ciclo = true;
                        DateTime TiempoDeVerificacion = DateTime.Now;
                        int retardoRespuestaImpresoraMS = 5000;
                        int retardoEntreSolicitudesMS = 1000;
                        byte[] respuestaImpresora;
                        do
                        {
                            Thread.Sleep(retardoEntreSolicitudesMS); // :/
                            impresora.CheckFPrinter();
                            if ((respuestaImpresora = impresora.SerialPortInputBuffer) != null)
                            {
                                if ((LineaRecibida = BitConverter.ToString(respuestaImpresora)) != null)
                                {

                                    if (LineaRecibida == "02-40-40-03-03" || //Lista para imprimir
                                        LineaRecibida == "02-60-40-03-23" ||   //Lista para imprimir
                                        LineaRecibida == "02-40-41-03-02" ||//Papel bajo
                                        LineaRecibida == "02-60-41-03-22")//Papel bajo
                                    {
                                        ciclo = false;
                                    }

                                    //Imprimiendo
                                    if (LineaRecibida == "02-41-40-03-02" ||
                                        LineaRecibida == "02-62-40-03-21" ||
                                        LineaRecibida == "02-61-40-03-22" ||
                                        LineaRecibida == "06" ||
                                        LineaRecibida == "02-40-42-03-01" ||
                                        LineaRecibida == "02-60-42-03-21")
                                    {
                                        ciclo = true; // sigue preguntando, pero acá no deberia preguntar nada
                                    }

                                }
                            }
                            else
                            {
                                //Si la impresora pasó más de 5 segundos sin responder
                                if (((TimeSpan)(DateTime.Now - TiempoDeVerificacion)).TotalMilliseconds > retardoRespuestaImpresoraMS)
                                {
                                    DialogResult resultado = MessageBox.Show(null, "La impresora está ocupada imprimiendo, si escoge cancelar se anulará el documento en curso", "Hubo un problema", MessageBoxButtons.RetryCancel);
                                    if (ciclo = (resultado == System.Windows.Forms.DialogResult.Retry))
                                    {
                                        TiempoDeVerificacion = DateTime.Now;
                                    }
                                    else
                                    {
                                        impresora.SendCmd("7");
                                    }
                                }
                            }


                        } while (ciclo);
                        #endregion
                    }
                }
            } while (CicloEnviar); // Se queda repitiendo el envio permanentemente.

            e.Result = true;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 0)
            {
                TextBox2.Text += TiempoDeEnvio + " --> " + lineaEnviada + "\n";
                TextBox2.Text += TiempoDeRecepcion + " --> " + LineaRecibida + "\n";
                TextBox2.Focus();
                TextBox2.CaretIndex = TextBox2.Text.Length;
                TextBox2.ScrollToEnd();
            }

            ProgressBar1.Value = ((LineaTextoEnviando + 1) * 100 / TextBox1.LineCount);

            if (ProgressBar1.Value == 100)
                ProgressBar1.Value = 0;

        }

        #endregion

        // Si se quiere cancelar un script muy largo por alguna razón.
        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            backgroundWorker1.CancelAsync();
            impresora.SendCmd("7");
            CicloEnviar = false;
        }

        // Para guardar el texto editado
        private void Button_Guardar_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "nuevo documento"; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Archivos de Texto | *.*";

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                archivoActivo = dlg.FileName;
                string fileText = TextBox1.Text;
                File.WriteAllText(archivoActivo, fileText);
            }
        }

        private void Button_Repetir_Click(object sender, RoutedEventArgs e)
        {
            CicloEnviar = !CicloEnviar;
            if (CicloEnviar)
            {
                Button_Repetir.Content = "No Repetir";
            }
            else
            {
                Button_Repetir.Content = "Repetir";
            }

        }

        //TODO: Probar!!!
        private void Button_Pruebas_Montos_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Window_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox1.Focusable = true;
            Keyboard.Focus(TextBox1);
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void TextBox1_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
            }
        }

        private void TextBox1_SelectionChanged(object sender, RoutedEventArgs e)
        {
            int row = TextBox1.GetLineIndexFromCharacterIndex(TextBox1.CaretIndex);
            int col = TextBox1.CaretIndex - TextBox1.GetCharacterIndexFromLineIndex(row);

            if (TextBox1.SelectedText != "")
            {
                lblCursorPosition.Text = "Selección: " + TextBox1.SelectedText.Length + " Chars";
            }
            else
            {
                lblCursorPosition.Text = "Cursor: Línea " + (row + 1) + ", Columna " + (col + 1);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Assembly _assembly;
            _assembly = Assembly.GetExecutingAssembly();
            string[] names = _assembly.GetManifestResourceNames();

            StreamReader _textStreamReader;
            _assembly = Assembly.GetExecutingAssembly();

            _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.vacio.txt"));

            string FileContents;
            //            FileContents = _textStreamReader.ReadToEnd();

            RichTextBox1.Document.Blocks.Clear();

            string line;
            int i = 0;
            while ((line = _textStreamReader.ReadLine()) != null)
            {
                i++;
                if (i % 2 == 0)
                {
                    RichTextBox1.AppendText(line + "\n");
                    RichTextBox1.Background = System.Windows.Media.Brushes.Black;
                    RichTextBox1.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    RichTextBox1.AppendText(line + "\n");
                    RichTextBox1.Background = System.Windows.Media.Brushes.Red;
                    RichTextBox1.Foreground = System.Windows.Media.Brushes.Black;
                }
            }

            _textStreamReader.Close();
        }

        private void Montos_Click(object sender, RoutedEventArgs e)
        {

            _assembly = Assembly.GetExecutingAssembly();
            _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.vacio.txt"));

            string Evento = ((System.Windows.Controls.MenuItem)sender).Header.ToString();

            //Pruebas de acumuladores
            if (Evento == "Factura Iva Exc")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Acumuladores.montos_fa_iva-excluido.txt"));
            if (Evento == "Factura Iva Inc")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Acumuladores.montos_fa_iva-incluido.txt"));
            if (Evento == "NC Iva Exc")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Acumuladores.montos_nc_iva-excluido.txt"));
            if (Evento == "NC Iva Inc")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Acumuladores.montos_nc_iva-incluido.txt"));
            if (Evento == "ND Iva Exc")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Acumuladores.montos_nd_iva-excluido.txt"));
            if (Evento == "ND Iva Inc")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Acumuladores.montos_nd_iva-incluido.txt"));

            //Montos Máximos.
            if (Evento == "9.999.999,99")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.MontosMaximos.MontosMaximos9999999999.txt"));
            if (Evento == "99.999.999,99")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.MontosMaximos.MontosMaximos9999999999.txt"));
            if (Evento == "999.999.999,99")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.MontosMaximos.MontosMaximos99999999999.txt"));
            if (Evento == "9.999.999.999,99")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.MontosMaximos.MontosMaximos99999999999.txt"));

            //Palabras Prohibidas.
            if (Evento == "áéíóúÑ y esas cosas")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Caracteres.diferentesCaracteres.txt"));
            if (Evento == "Caracteres por Descriptor")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Caracteres.cantidadDeCaracteres.txt"));
            if (Evento == "Palabras Prohibidas")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Caracteres.palabrasProhibidas.txt"));


            //Descriptores Z
            if (Evento == "Descriptores Z")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Descriptores.DescriptoresZ.txt"));

            //Programación
            if (Evento == "Cajeros")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Programacion.Cajeros.txt"));
            if (Evento == "Encabezados")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Programacion.Encabezados.txt"));
            if (Evento == "Medios de Pago")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Programacion.MediosDePago.txt"));

            string prueba = Evento;

            //Corte de Energía
            if (Evento == "_Corte de Energía")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.CorteEnergia.Corte.txt"));

            //Cambios de Tasa
            if (Evento == "_Cambios de Tasa")
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.cambioDeTasa.txt"));



            TextBox1.Text = _textStreamReader.ReadToEnd();
            _textStreamReader.Close();
        }

        private void btnFacturaRecargo_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBlock _NombrePrueba = (System.Windows.Controls.TextBlock)(((System.Windows.Controls.Button)sender).Content);
            String NombrePrueba = _NombrePrueba.Text;
            Assembly _assembly;
            StreamReader _textStreamReader;
            _assembly = Assembly.GetExecutingAssembly();

            //excepcion aca?
            _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Pruebas.Acumuladores." + NombrePrueba + ".txt"));

            string fileText1 = _textStreamReader.ReadToEnd();
            EnviarArchivo(fileText1);

        }

        private static bool IsTextAllowed(string Text)
        {
            Regex regex = new Regex("[^0-9]"); //regex that matches disallowed text
            return !regex.IsMatch(Text);
        }

        // Use the DataObject.Pasting Handler 
        private void PastingHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void textBox_Facturas_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void Flag0_Click(object sender, RoutedEventArgs e)
        {
            string Evento = ((System.Windows.Controls.Button)sender).Name;
            Assembly _assembly;
            StreamReader _textStreamReader;
            _assembly = Assembly.GetExecutingAssembly();

            if (Evento == "Flag0_Button")
            {
                System.Windows.Forms.MessageBox.Show("Se imprimirán dos facturas. La segunda indica un error");
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("POHOS3.Resources.Flags.00.txt"));
                string fileText1 = _textStreamReader.ReadToEnd();
                EnviarArchivo(fileText1);
            }
        }


    }

    public class Attached : DependencyObject
    {
        public static DependencyProperty ArchivoProperty =
            DependencyProperty.RegisterAttached(
                "Archivo",
                typeof(string),
                typeof(Attached),
                new FrameworkPropertyMetadata(""));

        public static string GetArchivo(DependencyObject obj)
        {
            return (string)obj.GetValue(ArchivoProperty);
        }

        public static void SetArchivo(DependencyObject obj, string value)
        {
            obj.SetValue(ArchivoProperty, value);
        }
    }
}

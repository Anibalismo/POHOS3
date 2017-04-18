using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;


namespace POHOS3.Resources.Controles
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        // Miembros públicos:
        public int numeroFlag;

        // Miembros privados
        // http://stackoverflow.com/questions/1568091/why-use-getters-and-setters
        string descripcionPrueba { get; set; }

        private ObservableCollection<DescripcionFlags> descripcionFlags = new ObservableCollection<DescripcionFlags>();

        public UserControl1()
        {
            InitializeComponent();

            DescripcionFlags test = new DescripcionFlags();
            test.Descripcion = "Descripción prueba";

            descripcionFlags.Add(test);

            lbDescripcion.Content = descripcionFlags;
        }

        //Expone el evento click
        public event RoutedEventHandler Button1Click;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Button1Click != null) Button1Click(sender, e);
        }
    }

    
    public class DescripcionFlags : INotifyPropertyChanged
    {
        private string descripcion;
        public string Descripcion {
            get { return this.descripcion; }
            set {
                if (this.descripcion != value) {
                    this.descripcion = value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propDescripcion)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propDescripcion));
        }

    }
    
}

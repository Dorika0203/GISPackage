using System.Diagnostics;
using System.Windows.Controls;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;

namespace GISControlWPFGL2
{
    /// <summary>
    /// Interaction logic for GISControl.xaml
    /// </summary>
    public partial class GISControl : UserControl
    {
        GLProgram program = new GLProgram();

        public GISControl()
        {
            InitializeComponent();
            OpenTKControl.Start();
            program.Initialize();
        }

        private void OpenTKControl_Render(TimeSpan obj)
        {
            program.Render();
        }

        private void OpenTKControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
        }

        private void OpenTKControl_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
        }
    }

}

using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace EFA
{
    public partial class Form1 : Form
    {
        private Dictionary<Color, Color> mapeoColores = new Dictionary<Color, Color>();
        private Dictionary<Point, Color> coloresOriginales = new Dictionary<Point, Color>();
        private int tolerancia = 30; // Definir un rango de tolerancia para los colores
        private Bitmap imagenOriginal;
        private Bitmap imagenModificada;

        public Form1()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.MouseClick += PictureBox2_MouseClick;
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            ConectarBaseDatos();
            CargarColoresDesdeBD();
            CargarDatosEnDataGridView();
        }

        private void ConectarBaseDatos()
        {
            string cadenaConexion = "server=localhost;database=bdcolores;uid=root;pwd=;";
            MySqlConnection conexion = new MySqlConnection(cadenaConexion);
            try
            {
                conexion.Open();
                MessageBox.Show("¡Conexión Abierta!");
                conexion.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("¡No se puede abrir la conexión! " + ex.Message);
            }
        }

        private void CargarColoresDesdeBD()
        {
            string cadenaConexion = "server=localhost;database=bdcolores;uid=root;pwd=;";
            MySqlConnection conexion = new MySqlConnection(cadenaConexion);
            try
            {
                conexion.Open();
                string consulta = "SELECT ColorOriginal, ColorModificado FROM ModificaColor";
                MySqlCommand comando = new MySqlCommand(consulta, conexion);
                MySqlDataReader lector = comando.ExecuteReader();
                while (lector.Read())
                {
                    string colorOriginalString = lector["ColorOriginal"].ToString();
                    string colorCambioString = lector["ColorModificado"].ToString();
                    Color colorOriginal = ParsearColor(colorOriginalString);
                    Color colorCambio = ParsearColor(colorCambioString);
                    mapeoColores[colorOriginal] = colorCambio;
                }
                lector.Close();
                conexion.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("¡Error al cargar colores desde la base de datos! " + ex.Message);
            }
        }

        // Resto del código para cargar imágenes y aplicar cambios...
    

// Subir imagen y mostrar en PictureBox sin modificarla
private void button1_Click(object sender, EventArgs e)
            {
                OpenFileDialog abrirDialogo = new OpenFileDialog();
                abrirDialogo.Filter = "Archivos de Imagen(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
                if (abrirDialogo.ShowDialog() == DialogResult.OK)
                {
                    imagenOriginal = new Bitmap(abrirDialogo.FileName);
                    pictureBox1.Image = imagenOriginal;
                    coloresOriginales.Clear(); // Limpiar colores originales anteriores
                }
            }

            // Aplicar cambios a la imagen basada en la base de datos y mostrar en PictureBox2
            private void button2_Click(object sender, EventArgs e)
            {
                if (imagenOriginal == null)
                {
                    MessageBox.Show("Primero sube una imagen.");
                    return;
                }

                imagenModificada = CambiarColoresSegunBD(new Bitmap(imagenOriginal));
                pictureBox2.Image = imagenModificada;
            }

            // Función para cambiar píxeles basado en colores de la base de datos
            private Bitmap CambiarColoresSegunBD(Bitmap bitmap)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color colorPixel = bitmap.GetPixel(x, y);
                        foreach (var mapeo in mapeoColores)
                        {
                            if (EsColorEnRango(colorPixel, mapeo.Key))
                            {
                                coloresOriginales[new Point(x, y)] = colorPixel;
                                CambiarPixelesCircundantes(bitmap, x, y, mapeo.Value);
                                break;
                            }
                        }
                    }
                }
                return bitmap;
            }

            private bool EsColorEnRango(Color color1, Color color2)
            {
                return Math.Abs(color1.R - color2.R) <= tolerancia &&
                       Math.Abs(color1.G - color2.G) <= tolerancia &&
                       Math.Abs(color1.B - color2.B) <= tolerancia;
            }

            private Color ParsearColor(string colorString)
            {
                string[] partes = colorString.Split(',');
                if (partes.Length == 3)
                {
                    int r = int.Parse(partes[0]);
                    int g = int.Parse(partes[1]);
                    int b = int.Parse(partes[2]);
                    return Color.FromArgb(r, g, b);
                }
                return Color.White;
            }

            private void CambiarPixelesCircundantes(Bitmap bitmap, int x, int y, Color nuevoColor)
            {
                int rango = 20;

                for (int dy = -rango; dy <= rango; dy++)
                {
                    for (int dx = -rango; dx <= rango; dx++)
                    {
                        int nuevoX = x + dx;
                        int nuevoY = y + dy;

                        if (nuevoX >= 0 && nuevoX < bitmap.Width && nuevoY >= 0 && nuevoY < bitmap.Height)
                        {
                            Point point = new Point(nuevoX, nuevoY);
                            if (!coloresOriginales.ContainsKey(point))
                            {
                                coloresOriginales[point] = bitmap.GetPixel(nuevoX, nuevoY);
                            }
                            bitmap.SetPixel(nuevoX, nuevoY, nuevoColor);
                        }
                    }
                }
            }

        private void PictureBox2_MouseClick(object sender, MouseEventArgs e)
        {
            if (imagenModificada == null) return;

            // Obtener el punto de clic relativo al PictureBox2
            Point punto = ObtenerCoordenadasImagen(e.Location, pictureBox2, imagenModificada);
            if (punto.X < 0 || punto.Y < 0 || punto.X >= imagenModificada.Width || punto.Y >= imagenModificada.Height)  // Corregido: 'or' cambiado a '||'
                return;

            Color colorClickeado = imagenModificada.GetPixel(punto.X, punto.Y);
            if (coloresOriginales.ContainsKey(punto))
            {
                Color colorOriginal = coloresOriginales[punto];
                textBox1.Text = $"Original: {colorOriginal} (RGB: {colorOriginal.R}, {colorOriginal.G}, {colorOriginal.B})\n" +
                                $"Cambiado: {colorClickeado} (RGB: {colorClickeado.R}, {colorClickeado.G}, {colorClickeado.B})";
                panelColor.BackColor = colorClickeado;
            }
            else
            {
                textBox1.Text = $"Cambiado: {colorClickeado} (RGB: {colorClickeado.R}, {colorClickeado.G}, {colorClickeado.B})";
                panelColor.BackColor = colorClickeado;
            }
        }


        private Point ObtenerCoordenadasImagen(Point punto, PictureBox pictureBox, Bitmap imagen)
            {
                int x = punto.X * imagen.Width / pictureBox.Width;
                int y = punto.Y * imagen.Height / pictureBox.Height;
                return new Point(x, y);
            }

            private void pictureBox1_Click(object sender, EventArgs e)
            {
            }
        }
    }


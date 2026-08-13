
using Nimble.Drawing;
using System.Diagnostics;
using System.Drawing;

const int COLOR_RANGE = 1000;

var matrix = new Composite[COLOR_RANGE, COLOR_RANGE];

var colors = new Composite[COLOR_RANGE * COLOR_RANGE];

for (int i = 0; i < COLOR_RANGE * COLOR_RANGE; i++)
{
    colors[i] = Composite.FromRandom();
}

//colors.Sort(new Comparer());

// Draw the matrix on a bitmap and save it to disk.

for (int i = 0; i < COLOR_RANGE; i++)
{
    for (int j = 0; j < COLOR_RANGE; j++)
    {
        matrix[i, j] = colors[i * COLOR_RANGE + j];
    }
}

var bitmap = new Bitmap(COLOR_RANGE, COLOR_RANGE);

for (int i = 0; i < COLOR_RANGE; i++)
{
    for (int j = 0; j < COLOR_RANGE; j++)
    {
        bitmap.SetPixel(i, j, matrix[i, j].ToColor());
    }
}

bitmap.Save("matrix.png");

// Open the generated image using the default image viewer on the system.

Process.Start(new ProcessStartInfo("matrix.png") { UseShellExecute = true });
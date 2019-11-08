
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render
{
    public class Texture
    {
        private readonly int handle;

        public Texture(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            handle = GL.GenTexture();
            Draw();

            if (!File.Exists(path))
                path = "materials/error.png";

            using (var image = new Bitmap(path))
            {
                var data = image.LockBits(
                    new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    image.Width,
                    image.Height,
                    0,
                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                    PixelType.UnsignedByte,
                    data.Scan0);
            }

            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Draw(TextureUnit unit = TextureUnit.Texture0)
        {
            if (handle != 0)
            {
                GL.ActiveTexture(unit);
                GL.BindTexture(TextureTarget.Texture2D, handle);
            }
        }
    }
}

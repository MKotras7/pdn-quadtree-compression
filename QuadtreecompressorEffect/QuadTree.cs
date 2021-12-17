using PaintDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuadtreecompressorEffect
{
    public class QuadTree
    {
        private Surface surface;
        private (int x, int y) position;
        private int size;
        private QuadTree parent;
        private QuadTree[] children;

        private float variance;
        private ColorBgra color;
        float sensitivity;

        public QuadTree(QuadTree parent, Surface surface, (int x, int y) position, int size, float sensitivity)
        {
            this.parent = null;
            this.surface = surface;
            this.position = position;
            this.size = size;
            this.sensitivity = sensitivity;

            this.children = null;
        }

        public void doPerPixel(Action<ColorBgra> action)
        {
            for(int x = position.x; x < position.x + size; x++)
            {
                for (int y = position.y; y < position.y + size; y++)
                {
                    action(surface[x, y]);
                }
            }
        }

        public ColorBgra getAverage()
        {
            //Average = sum of all pixels squared, divide that by n, square root that
            long squareR = 0;
            long squareG = 0;
            long squareB = 0;
            long squareA = 0;
            doPerPixel((ColorBgra color) =>
            {
                squareR += color.R * color.R;
                squareG += color.G * color.G;
                squareB += color.B * color.B;
                squareA += color.A * color.A;
            });
            int numPixels = size * size;
            squareR /= numPixels;
            squareG /= numPixels;
            squareB /= numPixels;
            squareA /= numPixels;
            squareR = (long) Math.Sqrt(squareR);
            squareG = (long) Math.Sqrt(squareG);
            squareB = (long) Math.Sqrt(squareB);
            squareA = (long) Math.Sqrt(squareA);
            return ColorBgra.FromBgra((byte) squareB, (byte)squareG, (byte)squareR, (byte)squareA);
        }

        public float getVariance()
        {
            //Variance is the sum of all pixels' difference from mean, that squared, divide by n-1.
            ColorBgra average = getAverage();
            float varianceR = 0.0f;
            float varianceG = 0.0f;
            float varianceB = 0.0f;
            float varianceA = 0.0f;
            doPerPixel((ColorBgra color) =>
            {
                varianceR += MathF.Pow(color.R - average.R, 2);
                varianceG += MathF.Pow(color.G - average.G, 2);
                varianceB += MathF.Pow(color.B - average.B, 2);
                varianceA += MathF.Pow(color.A - average.A, 2);
            });
            int numPixels = size * size;
            int degreesOfFreedom = numPixels - 1;
            varianceR /= degreesOfFreedom;
            varianceG /= degreesOfFreedom;
            varianceB /= degreesOfFreedom;
            varianceA /= degreesOfFreedom;

            return varianceR + varianceG + varianceB + varianceA;
        }

        public void compress()
        {
            color = getAverage();
            variance = getVariance();
            //if(false)
            if(MathF.Sqrt(variance) >= sensitivity && size > 1)
            {
                children = new QuadTree[4];
                (int x, int y) midPoint = (position.x + (size / 2), position.y + (size / 2));

                children[0] = new QuadTree(this, surface, (position.x, position.y), size / 2, sensitivity);
                children[1] = new QuadTree(this, surface, (position.x, midPoint.y), size / 2, sensitivity);
                children[2] = new QuadTree(this, surface, (midPoint.x, position.y), size / 2, sensitivity);
                children[3] = new QuadTree(this, surface, (midPoint.x, midPoint.y), size / 2, sensitivity);
                children[0].compress();
                children[1].compress();
                children[2].compress();
                children[3].compress();
            }
        }

        public void render(Surface destinationSurface)
        {
            if(size == 1)
            {
                destinationSurface[position.x, position.y] = color;
            }
            else
            {
                if(children != null)
                {
                    children[0].render(destinationSurface);
                    children[1].render(destinationSurface);
                    children[2].render(destinationSurface);
                    children[3].render(destinationSurface);
                }
                else
                {
                    for (int x = position.x; x < position.x + size; x++)
                    {
                        for (int y = position.y; y < position.y + size; y++)
                        {
                            destinationSurface[x, y] = color;
                        }
                    }
                }
            }
        }
    }
}

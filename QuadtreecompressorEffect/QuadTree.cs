using PaintDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        private QuadTree[] children;

        //private (float b, float g, float r, float a) channelError;
        private (long b, long g, long r, long a) channelSums;
        private (long b, long g, long r, long a) channelSquareSums;
        private int pixelCount;
        private ColorBgra centerColor;
        float errorThreshold;

        public QuadTree(Surface surface, (int x, int y) position, int size, float errorThreshold)
        {
            this.surface = surface;
            this.position = position;
            this.size = size;
            this.errorThreshold = errorThreshold;

            this.children = null;
        }

        public void compress()
        {
            if (size == 1)
            {
                //If 1x1, base case, build color, pixelCount, and error
                if (surface.IsVisible(position.x, position.y))
                {
                    centerColor = surface[position.x, position.y];
                    channelSums = (centerColor.B, centerColor.G, centerColor.R, centerColor.A);
                    channelSquareSums = (centerColor.B * centerColor.B, centerColor.G * centerColor.G, centerColor.R * centerColor.R, centerColor.A * centerColor.A);
                    pixelCount = 1;
                }
                else
                {
                    pixelCount = 0;
                }
            }
            else
            {
                //Otherwise, build the 4 children, let them compute their values.
                (int x, int y) midPoint = (position.x + (size / 2), position.y + (size / 2));
                children = new QuadTree[4];
                children[0] = new QuadTree(surface, (position.x, position.y), size / 2, errorThreshold);
                children[1] = new QuadTree(surface, (position.x, midPoint.y), size / 2, errorThreshold);
                children[2] = new QuadTree(surface, (midPoint.x, position.y), size / 2, errorThreshold);
                children[3] = new QuadTree(surface, (midPoint.x, midPoint.y), size / 2, errorThreshold);
                children[0].compress();
                children[1].compress();
                children[2].compress();
                children[3].compress();

                bool anyChildHasChildren = false;
                foreach (QuadTree child in children)
                {
                    anyChildHasChildren |= (child.children != null);
                }
                if(!anyChildHasChildren)
                {
                    pixelCount = 0;
                    channelSums = (0, 0, 0, 0);
                    channelSquareSums = (0, 0, 0, 0);
                    foreach (QuadTree child in children)
                    {
                        pixelCount += child.pixelCount;

                        channelSums.b += child.channelSums.b;
                        channelSums.g += child.channelSums.g;
                        channelSums.r += child.channelSums.r;
                        channelSums.a += child.channelSums.a;

                        channelSquareSums.b += child.channelSquareSums.b;
                        channelSquareSums.g += child.channelSquareSums.g;
                        channelSquareSums.r += child.channelSquareSums.r;
                        channelSquareSums.a += child.channelSquareSums.a;
                    }
                    if(pixelCount == 0 && size > 1)
                    {
                        return;
                    }
                    (float b, float g, float r, float a) meanColor = (
                        MathF.Sqrt(channelSquareSums.b / pixelCount),
                        MathF.Sqrt(channelSquareSums.g / pixelCount),
                        MathF.Sqrt(channelSquareSums.r / pixelCount),
                        MathF.Sqrt(channelSquareSums.a / pixelCount)
                    );
                    centerColor = ColorBgra.FromBgra(
                        (byte)(meanColor.b),
                        (byte)(meanColor.g),
                        (byte)(meanColor.r),
                        (byte)(meanColor.a)
                    );

                    float varianceB = (channelSquareSums.b - (2 * centerColor.B * channelSums.b) + (pixelCount * (meanColor.b * meanColor.b))) / pixelCount;
                    float varianceG = (channelSquareSums.g - (2 * centerColor.G * channelSums.g) + (pixelCount * (meanColor.g * meanColor.g))) / pixelCount;
                    float varianceR = (channelSquareSums.r - (2 * centerColor.R * channelSums.r) + (pixelCount * (meanColor.r * meanColor.r))) / pixelCount;
                    float varianceA = (channelSquareSums.a - (2 * centerColor.A * channelSums.a) + (pixelCount * (meanColor.a * meanColor.a))) / pixelCount;
                    float stdev = MathF.Sqrt(varianceB + varianceG + varianceR + varianceA);

                    if (stdev <= errorThreshold)
                    {
                        children = null;
                    }
                }
            }
        }

        public void render(Surface destinationSurface, (int x, int y) offset)
        {
            if (children == null)
            {
                for (int x = position.x; x < position.x + size; x++)
                {
                    for (int y = position.y; y < position.y + size; y++)
                    {
                        if (destinationSurface.IsVisible(x + offset.x, y + offset.y))
                        {
                            destinationSurface[x + offset.x, y + offset.y] = centerColor;
                        }
                        else
                        {
                            throw new Exception("afi;");
                        }
                    }
                }
            }
            else
            {
                foreach (QuadTree child in children)
                {
                    child.render(destinationSurface, offset);
                }
            }
        }

        public void render(Surface destinationSurface, Rectangle rect)
        {
            if(children == null)
            {
                for (int x = position.x; x < position.x + size; x++)
                {
                    for (int y = position.y; y < position.y + size; y++)
                    {
                        if(destinationSurface.IsVisible(x, y))
                        {
                            if(rect.Contains(x, y))
                            {
                                destinationSurface[x, y] = centerColor;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (QuadTree child in children)
                {
                    child.render(destinationSurface, rect);
                }
            }
        }
    }
}

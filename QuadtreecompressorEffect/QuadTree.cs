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
        //Reference to the source image
        private Surface surface;

        private (int x, int y) position;
        private int size;
        private QuadTree[] children;

        private (long b, long g, long r, long a) channelSums; //The sum of all color in the node's bounds
        private (long b, long g, long r, long a) channelSquareSums; //The sum of all color squared in the node's bounds
        private int pixelCount; //Number of actual pixels that this bound contains (doesn't count positions outside the image)
        private ColorBgra centerColor; //Mean color
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
                //If 1x1, base case, build color, pixelCount, and sums
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
                //We are larger than a 1x1 build the 4 children, let them compute their values.
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

                //Then, if any child has a child, we know we can't compress, so we won't do any work on this node.
                bool anyChildHasChildren = false;
                foreach (QuadTree child in children)
                {
                    anyChildHasChildren |= (child.children != null);
                }
                if(!anyChildHasChildren)
                {
                    //Combines all the sums of the children.
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
                    //The mean color is the same as sqrt( [sum for all pixels p (p^2)] / n )
                    //Since we already have the sum of all squares, this is very easy to compute.
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

                    //The variance is equivalent to:
                    // [Sum of all pixels p (p - pAverage)^2] / n
                    // If you expand the inside (a-b)^2 and use rules of sums you get:
                    // [sum of all p^2] - [(2 * pAverage) * (sum of all p)] + [number of pixels * pAverage^2]
                    //So, we apply that formula for all BGRA channels, then, since variances can be added, we add them.
                    //Finally, we take the square root to get the standard deviation, which is what the user uses as the threshold.
                    float varianceB = (channelSquareSums.b - (2 * centerColor.B * channelSums.b) + (pixelCount * (meanColor.b * meanColor.b))) / pixelCount;
                    float varianceG = (channelSquareSums.g - (2 * centerColor.G * channelSums.g) + (pixelCount * (meanColor.g * meanColor.g))) / pixelCount;
                    float varianceR = (channelSquareSums.r - (2 * centerColor.R * channelSums.r) + (pixelCount * (meanColor.r * meanColor.r))) / pixelCount;
                    float varianceA = (channelSquareSums.a - (2 * centerColor.A * channelSums.a) + (pixelCount * (meanColor.a * meanColor.a))) / pixelCount;
                    float stdev = MathF.Sqrt(varianceB + varianceG + varianceR + varianceA);

                    if (stdev <= errorThreshold)
                    {
                        //If this node after accumulating its children is under the threshold, then it can
                        //become the representative node for its entire region, removing its children.
                        children = null;
                    }
                }
            }
        }

        public void render(Surface destinationSurface, Rectangle rect)
        {
            //If this node has no children, then it holds the color for the node's entire bounds.
            //So, for every valid position this node contains, color it in.
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
            //Otherwise, this node has children, and we simply recurse onto all child nodes.
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

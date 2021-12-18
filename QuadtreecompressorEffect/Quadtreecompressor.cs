using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Reflection;
using System.Drawing.Text;
using System.Windows.Forms;
using System.IO.Compression;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Registry = Microsoft.Win32.Registry;
using RegistryKey = Microsoft.Win32.RegistryKey;
using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Clipboard;
using PaintDotNet.IndirectUI;
using PaintDotNet.Collections;
using PaintDotNet.PropertySystem;
using PaintDotNet.Rendering;
using PaintDotNet.Effects;
using ColorWheelControl = PaintDotNet.ColorBgra;
using AngleControl = System.Double;
using PanSliderControl = PaintDotNet.Pair<double,double>;
using FilenameControl = System.String;
using ReseedButtonControl = System.Byte;
using RollControl = System.Tuple<double, double, double>;
using IntSliderControl = System.Int32;
using CheckboxControl = System.Boolean;
using TextboxControl = System.String;
using DoubleSliderControl = System.Double;
using ListBoxControl = System.Byte;
using RadioButtonControl = System.Byte;
using MultiLineTextboxControl = System.String;

[assembly: AssemblyTitle("Quadtree compressor plugin for Paint.NET")]
[assembly: AssemblyDescription("Quadtree compressor selected pixels")]
[assembly: AssemblyConfiguration("quadtree compressor")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Quadtree compressor")]
[assembly: AssemblyCopyright("Copyright ©2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyMetadata("BuiltByCodeLab", "Version=6.4.7995.19949")]
[assembly: SupportedOSPlatform("Windows")]

namespace QuadtreecompressorEffect
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author
        {
            get
            {
                return base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
            }
        }

        public string Copyright
        {
            get
            {
                return base.GetType().Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
            }
        }

        public string DisplayName
        {
            get
            {
                return base.GetType().Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
            }
        }

        public Version Version
        {
            get
            {
                return base.GetType().Assembly.GetName().Version;
            }
        }

        public Uri WebsiteUri
        {
            get
            {
                return new Uri("https://www.getpaint.net/redirect/plugins.html");
            }
        }
    }

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Quadtree compressor")]
    public class QuadtreecompressorEffectPlugin : PropertyBasedEffect
    {
        public static string StaticName
        {
            get
            {
                return "Quadtree compressor";
            }
        }

        public static Image StaticIcon
        {
            get
            {
                return null;
            }
        }

        public static string SubmenuName
        {
            get
            {
                return null;
            }
        }

        public QuadtreecompressorEffectPlugin()
            : base(StaticName, StaticIcon, SubmenuName, new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        public enum PropertyNames
        {
            Amount1
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new Int32Property(PropertyNames.Amount1, 0, 0, 500));

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.DisplayName, "Standard deviation threshold");

            return configUI;
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            // Add help button to effect UI
            props[ControlInfoPropertyNames.WindowHelpContentType].Value = WindowHelpContentType.PlainText;
            props[ControlInfoPropertyNames.WindowHelpContent].Value = "Quadtree compressor v1.1\nCopyright ©2021 by \nAll rights reserved.";
            base.OnCustomizeConfigUIWindowProperties(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken token, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            Amount1 = token.GetProperty<Int32Property>(PropertyNames.Amount1).Value;

            PreRender(dstArgs.Surface, srcArgs.Surface);

            base.OnSetRenderInfo(token, dstArgs, srcArgs);
        }

        protected override unsafe void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            if (length == 0) return;
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Render(DstArgs.Surface,SrcArgs.Surface,rois[i]);
            }
        }

        #region User Entered Code
        // Name: Quadtree compressor
        // Submenu:
        // Author:
        // Title:
        // Version:
        // Desc:
        // Keywords:
        // URL:
        // Help:
        #region UICode
        IntSliderControl Amount1 = 0;
        #endregion

        private Surface quadSurface;

        void PreRender(Surface dst, Surface src)
        {
            //This is the singlethreaded code that executes before render.
            //In our case, it is where the entire effect resides.
            Rectangle selection = EnvironmentParameters.SelectionBounds;

            int powerOfTwoWidth = 1;
            while (powerOfTwoWidth < selection.Width)
            {
                powerOfTwoWidth <<= 1;
            }

            int powerOfTwoHeight = 1;
            while (powerOfTwoHeight < selection.Height)
            {
                powerOfTwoHeight <<= 1;
            }
            int pow2Size = Math.Max(powerOfTwoWidth, powerOfTwoHeight);
            QuadTree quadTree = new QuadTree(src, (selection.X, selection.Y), pow2Size, Amount1);
            quadTree.compress();
            quadSurface = new Surface(new Size(pow2Size, pow2Size));
            quadTree.render(dst, selection);
        }

        void Render(Surface dst, Surface src, Rectangle rect)
        {
            //We don't need to do any work here because this is where all of the multithreaded logic goes,
            //and it seems slower if we do that.
        }
        
        #endregion
    }
}

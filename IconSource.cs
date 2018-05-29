using System;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Runtime.InteropServices;

namespace pinger
{

   


	/// <summary>
	/// Summary description for IconSource.
	/// </summary>
    /// 
	
    
    public class IconSource  //: System.ComponentModel.Component
	{	
		private float size;
		private Bitmap bitmap;
		private Color color;
		private Graphics g;
		public int radius;
		private int max_radius;
		private Pen pen;
		private SolidBrush sb, sback;
		public Color bcolor= Color.White;

		public IconSource(int Size)
		{
			//
			// TODO: Add constructor logic here
			//
			max_radius=Size/2;
			size=(float)Size;
			bitmap=new Bitmap(Size,Size);
			g=Graphics.FromImage(bitmap);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			radius=1;
			pen = new Pen(Color.Black);
			sb=new SolidBrush(Color.Black);
			sback=new SolidBrush(Color.Black);

		}
		public Icon next_icon()
		{
			if (radius > max_radius) radius=0;
			radius++;
			return draw_circle();
		}
		public Icon icon(int new_radius)
		{
			radius=new_radius;
			return draw_circle();
		}
		public Color mycolor
		{
			set
			{
				color=value;
			}
			get
			{
				return color;
			}
		}

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
extern static bool DestroyIcon(IntPtr handle);



		private Icon draw_circle()
		{
			float x,y,x0,y0,side;
			x0=size/2F;
			y0=size/2F;
			x=x0-((float)radius);
			y=y0-((float)radius);
			side=(float)radius*2F;
			pen.Color=color;
			sb.Color=color;
			sback.Color=bcolor;
			g.FillRectangle(sback,0f,0f,size,size);
			g.FillEllipse(sb,x,y,side,side);
			IntPtr handle=bitmap.GetHicon();
			Icon t =Icon.FromHandle(handle);
            Icon r = (Icon)t.Clone();
            DestroyIcon(t.Handle);
			return r;

		}
	}
	}


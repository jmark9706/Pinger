using System;
using System.Drawing;

namespace pinger
{
	/// <summary>
	/// Summary description for IconSequence.
	/// </summary>
	public class IconSequence
	{
		
		private Icon[] IconSeq;
		private int IconCount;
		private int next_one;
		public IconSequence(int ic)
		{
			//
			// TODO: Add constructor logic here
			//
			IconCount=ic;
			IconSeq = new Icon[ic];
			next_one=0;
		}
		public Icon this[int idx]
		{
			get
			{ return IconSeq[idx];}
			set
			{ IconSeq[idx]=value; }
		}
		public int count
		{
			get
			{ return IconCount; }
		}
		public Icon next_icon
		{
			get
			{
				if (next_one>IconCount-1)next_one=0;
				return IconSeq[next_one++];
				
			}
		}
	}
}

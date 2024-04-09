using Celeste.Mod;
using Celeste.Mod.SusanHelper;
using Microsoft.Xna.Framework;
using SusanHelper.Entities.Paint;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.SusanHelper
{
	public class SusanHelperSession : EverestModuleSession
	{
		public float gasTimer { get; set; } = 0f;
		public bool inInkWater { get; set; } = false;
		public float timeToInkDeath { get; set; } = 5f;
		public float inkDeathTimer { get; set; } = 0f;
		public float inkDashesNow { get; set; } = 1f;
		public int inkPaintLayer { get; set; } = 1;
		public bool hangingToRainbowInk { get; set; } = false;
		public PaintBall currentBall { get; set; } = null;
		public string goodColorsStr { get; set; } = "FF6B6B,FFAD60,FFD666,A8E6CF,A7C7E7,C3AED6";
        //Full Saturation: ff6b6b-ffad60-ffd666-a8e6cf-a7c7e7-c3aed6
        //Semi-Saturated: ffb5b5-ffd6b0-ffebb2-d3f2e7-d3e3f3-e1d6eb
        //Barely Saturated: ffcdcd-ffe3ca-fff1cb-e1f6ef-e1ecf7-ebe3f1
        //Desaturated: ffdddd-ffecdb-fff5dc-ebf9f4-ebf2f9-f1ecf5
        public string badColorsStr { get; set; } = "000000";
		public int inkEntryDashes { get; set; } = -1;
		public bool canTriggerFloatFallBlock { get; set; } = true;
		public bool inkPaintSafe { get; set; } = false;
    }
}
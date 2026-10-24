using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;

namespace Mod_PandaAllyFuseMod
{
    [BepInPlugin("panda.allyfuse.mod", "Panda's Ally Fuse Mod", "1.0.0.0")]

    public class Mod_PandaAllyFuseMod : BaseUnityPlugin
    {
        private void Start()
        {
            var harmony = new Harmony("Panda's Ally Fuse Mod");
            harmony.PatchAll();
        }
    }
}

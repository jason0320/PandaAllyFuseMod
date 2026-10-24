using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Mod_PandaAllyFuseMod
{
    [HarmonyPatch(typeof(Chara), nameof(Chara.IsFusable))]
    public static class FusePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Chara __instance, Chara c, ref bool __result)
        {
            __result = c != null
                    && c != __instance
                    && c.id == __instance.id
                    && c.c_bossType == 0
                    && __instance.c_bossType == 0
                    && (EClass.debug.enable
                        || (!c.affinity.CanSleepBeside() && !__instance.affinity.CanSleepBeside()));
            return false;
        }
    }

    public static class FuseState
    {
        public static Chara Fuser;
        public static readonly HashSet<string> AllyMatons = new HashSet<string>
        {
            "shojomaton", "imotomaton", "ojomaton"
        };
    }

    [HarmonyPatch(typeof(Chara), nameof(Chara.TryFuse))]
    public static class TryFuseScopePatch
    {
        [HarmonyPrefix] public static void Prefix(Chara __instance) => FuseState.Fuser = __instance;
        [HarmonyFinalizer] public static void Finalizer() => FuseState.Fuser = null;
    }

    [HarmonyPatch(typeof(Card), nameof(Card.Destroy))]
    public static class FuseDestroyPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Card __instance, out bool __state)
        {
            __state = false;
            var fuser = FuseState.Fuser;
            if (fuser == null) return;
            if (!(__instance is Chara c)) return;
            if (!c.IsPCFaction || c.isSummon || c.id != fuser.id) return;

            foreach (Thing t in c.things.ToList())
            {
                if (t.isEquipped) c.body.Unequip(t);
                EClass._zone.AddCard(t, c.pos);
            }
            c.isSummon = true;
            __state = true;
        }

        [HarmonyPostfix]
        public static void Postfix(Card __instance, bool __state)
        {
            if (__state && __instance is Chara c) c.isSummon = false;
        }
    }

    [HarmonyPatch(typeof(Zone), nameof(Zone.SpawnMob), new[] { typeof(string), typeof(Point) })]
    public static class FuseSpawnAllyPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Chara __result)
        {
            var fuser = FuseState.Fuser;
            if (fuser == null || __result == null) return;
            if (!fuser.IsPCFaction) return;
            if (!FuseState.AllyMatons.Contains(__result.id)) return;
            __result.MakeAlly(false);
        }
    }

    [HarmonyPatch]
    public static class AllyAutoFusePatch
    {
        [HarmonyTargetMethod]
        static MethodBase Target() => AccessTools.Method(typeof(Chara), "Tick");

        [HarmonyPostfix]
        static void Postfix(Card __instance)
        {
            try
            {
                if (!(__instance is Chara c)) return;
                if (c.IsPC || !c.IsPCFactionOrMinion) return;
                if (!c.HasElement(1432) || !c.IsAliveInCurrentZone) return;
                if (c.TryFuse(0.05f)) Debug.Log("[PandaFuse] fuse triggered");
            }
            catch (Exception e) { Debug.Log("[PandaFuse] " + e); }
        }
    }
}
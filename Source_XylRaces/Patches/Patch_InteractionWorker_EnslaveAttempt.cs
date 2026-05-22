using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(InteractionWorker_EnslaveAttempt))]
    public static class Patch_InteractionWorker_EnslaveAttempt
    {
        [Feature(nameof(DefOf.XylWillFallRate))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
        [InfixPatch(nameof(InteractionWorker_EnslaveAttempt.Interacted))]
        public static float GetStatValue_Wrapper(Thing thing, StatDef stat, Pawn recipient, bool applyPostProcess, int cacheStaleAfterTicks)
        {
            float value = thing.GetStatValue(stat, applyPostProcess, cacheStaleAfterTicks);
            if (stat == StatDefOf.NegotiationAbility)
                value *= recipient.GetStatValue(DefOf.XylWillFallRate);
            return value;
        }

        // TODO this does not do anything because it has no harmony attributes...
        //[Feature(typeof(GeneDefExtension_WildMan))]
        public static bool Interacted_Prefix(
            Pawn initiator,
            Pawn recipient,
            List<RulePackDef> extraSentencePacks,
            out string letterText,
            out string letterLabel,
            out LetterDef letterDef,
            out LookTargets lookTargets)
        {
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;

            if (recipient.IsWildMan() &&
                (recipient.Faction == null || !recipient.Faction.def.humanlikeFaction))
            {
                float tameChance;
                if (initiator.InspirationDef == InspirationDefOf.Inspired_Taming)
                {
                    tameChance = 1f;
                    initiator.mindState.inspirationHandler.EndInspiration(InspirationDefOf.Inspired_Taming);
                }
                else
                {
                    tameChance = initiator.GetStatValue(StatDefOf.TameAnimalChance);
                    float statValue = recipient.GetStatValue(StatDefOf.Wildness);
                    tameChance *= InteractionWorker_RecruitAttempt.TameChanceFactorCurve_Wildness.Evaluate(statValue);
                    if (recipient.IsPrisonerInPrisonCell())
                    {
                        tameChance *= 0.6f;
                    }

                    if (initiator.relations.DirectRelationExists(PawnRelationDefOf.Bond, recipient))
                    {
                        tameChance *= 4f;
                    }

                    if (initiator.Ideo != null && initiator.Ideo.IsVeneratedAnimal(recipient))
                    {
                        tameChance *= 2f;
                    }
                }

                if (Rand.Chance(tameChance))
                {
                    if (GenGuest.TryEnslavePrisoner(initiator, recipient))
                    {
                        if (!letterLabel.NullOrEmpty())
                        {
                            letterDef = LetterDefOf.PositiveEvent;
                        }

                        letterLabel = "LetterLabelEnslavementSuccess".Translate() + ": " + recipient.LabelCap;
                        letterText = "LetterEnslavementSuccess".Translate(initiator, recipient);
                        letterDef = LetterDefOf.PositiveEvent;
                        lookTargets = new LookTargets(recipient, initiator);
                        if (initiator.InspirationDef == InspirationDefOf.Inspired_Taming)
                        {
                            initiator.mindState.inspirationHandler.EndInspiration(InspirationDefOf.Inspired_Taming);
                        }

                        extraSentencePacks.Add(RulePackDefOf.Sentence_RecruitAttemptAccepted);

                        return true;
                    }
                }
                else
                {
                    TaggedString taggedString = "TextMote_TameFail".Translate(tameChance.ToStringPercent());
                    MoteMaker.ThrowText((initiator.DrawPos + recipient.DrawPos) / 2f, initiator.Map, taggedString, 8f);
                    recipient.mindState.CheckStartMentalStateBecauseRecruitAttempted(initiator);
                    extraSentencePacks.Add(RulePackDefOf.Sentence_RecruitAttemptRejected);

                    return true;
                }
            }

            return true;
        }
    }
}

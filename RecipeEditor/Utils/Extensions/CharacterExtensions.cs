using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using RoR2.Skills;

namespace RecipeEditor.Utils
{
    public static class CharacterExtensions
    {
        /// <summary>Checks if a CharacterBody has a specific SkillDef equipped</summary>
        /// <param name="skill">the SkillDef to check for</param>
        /// <returns>whether or not the SkillDef was equipped</returns>
        public static bool HasSkillEquipped(this CharacterBody body, SkillDef skill)
        {
            foreach (GenericSkill slot in body.GetComponents<GenericSkill>())
            {
                if (slot.skillDef == skill)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
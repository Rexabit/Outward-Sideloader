using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;

// this class just contains a few helper functions for setting up skills. Custom Skills are still mostly defined from the CustomItems class.

// most importantly at the moment, this class contains helpers for setting up Skill Trees.

namespace SideLoader
{
    public class CustomSkills : MonoBehaviour
    {
        public static CustomSkills Instance;

        internal void Awake()
        {
            Instance = this;
        }

        // set a skill tree icon for a skill
        public static void SetSkillSmallIcon(int id, string textureName)
        {
            Skill skill = ResourcesPrefabManager.Instance.GetItemPrefab(id) as Skill;
            var tex = SL.Instance.TextureData[textureName];
            tex.filterMode = FilterMode.Bilinear;
            skill.SkillTreeIcon = TexReplacer.CreateSprite(tex);
        }

        // create a skillschool template.
        public static SkillSchool CreateSkillSchool(string name)
        {
            if ((Resources.Load("_characters/CharacterProgression") as GameObject).transform.Find("Test") is Transform template)
            {
                // instantiate a copy of the dev template
                var customObj = Instantiate(template).gameObject;
                DontDestroyOnLoad(customObj);
                var CustomTree = customObj.GetComponent<SkillSchool>();

                // set the name to the gameobject and the skill tree name/uid
                customObj.name = name;
                At.SetValue(name, typeof(SkillSchool), CustomTree, "m_defaultName");
                At.SetValue("", typeof(SkillSchool), CustomTree, "m_nameLocKey");
                At.SetValue(new UID(name), typeof(SkillSchool), CustomTree, "m_uid");

                // add it to the game's skill tree holder.
                var list = (At.GetValue(typeof(SkillTreeHolder), SkillTreeHolder.Instance, "m_skillTrees") as SkillSchool[]).ToList();
                list.Add(CustomTree);
                At.SetValue(list.ToArray(), typeof(SkillTreeHolder), SkillTreeHolder.Instance, "m_skillTrees");

                return CustomTree;
            }

            return null;
        }

        // for creating a SkillSlot on a transform
        public static SkillSlot CreateSkillSlot(Transform row, string name, int refSkillID, int requiredMoney, BaseSkillSlot requiredSlot = null, bool isBreakthrough = false, int column = 1)
        {
            var slotObj = new GameObject(name);
            slotObj.transform.parent = row;

            var slot = slotObj.AddComponent<SkillSlot>();
            At.SetValue(ResourcesPrefabManager.Instance.GetItemPrefab(refSkillID) as Skill, typeof(SkillSlot), slot, "m_skill");
            At.SetValue(requiredMoney, typeof(SkillSlot), slot, "m_requiredMoney");
            At.SetValue(column, typeof(BaseSkillSlot), slot as BaseSkillSlot, "m_columnIndex");

            if (requiredSlot != null)
            {
                At.SetValue(requiredSlot, typeof(BaseSkillSlot), slot, "m_requiredSkillSlot");
            }

            if (isBreakthrough)
            {
                slot.IsBreakthrough = true;
            }

            return slot;
        }

        // for destroying all children of a transform immediately. kind of unsafe, use caution with this.
        public static void DestroyChildren(Transform t)
        {
            while (t.childCount > 0)
            {
                DestroyImmediate(t.GetChild(0).gameObject);
            }
        }
    }
}

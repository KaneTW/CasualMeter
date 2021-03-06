﻿// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tera.Game.Messages;

namespace Tera.Game
{
    // Contains information about skills
    // Currently this is limited to the name of the skill
    public class SkillDatabase
    {
        private readonly Dictionary<RaceGenderClass, List<UserSkill>> _userSkilldata = new Dictionary<RaceGenderClass, List<UserSkill>>();
        private readonly Dictionary<PlayerClass, List<Skill>> _damageSkillIdOverrides = new Dictionary<PlayerClass, List<Skill>>();
        private readonly Dictionary<PlayerClass, List<Skill>> _healSkillIdOverrides = new Dictionary<PlayerClass, List<Skill>>();

        public SkillDatabase(string directory)
        {
            InitializeSkillDatabase(Path.Combine(directory, "user_skills.txt"));
            InitializeSkillDatabaseOverrides(Path.Combine(directory, "skill-overrides"));
        }

        private void InitializeSkillDatabase(string filename)
        {
            var lines = File.ReadLines(filename);
            var listOfParts = lines.Select(s => s.Split(new[] { ' ' }, 5));
            foreach (var parts in listOfParts)
            {
                var skill = new UserSkill(int.Parse(parts[0]), new RaceGenderClass(parts[1], parts[2], parts[3]), parts[4]);
                if (!_userSkilldata.ContainsKey(skill.RaceGenderClass))
                    _userSkilldata[skill.RaceGenderClass] = new List<UserSkill>();
                _userSkilldata[skill.RaceGenderClass].Add(skill);
            }
        }

        private void InitializeSkillDatabaseOverrides(string directory)
        {
            InitializeSkillDatabaseOverrides(Path.Combine(directory, "damage"), _damageSkillIdOverrides);
            InitializeSkillDatabaseOverrides(Path.Combine(directory, "heal"), _healSkillIdOverrides);
        }

        private void InitializeSkillDatabaseOverrides(string directory, Dictionary<PlayerClass, List<Skill>> collection)
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                PlayerClass playerClass;
                if (Enum.TryParse(Path.GetFileNameWithoutExtension(file), true, out playerClass))
                {
                    collection[playerClass] = new List<Skill>();
                    var lines = File.ReadLines(file).Where(l => !l.StartsWith("#") && !string.IsNullOrWhiteSpace(l));
                    var listOfParts = lines.Select(s => s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                    foreach (var parts in listOfParts)
                    {
                        int skillId;
                        string skillName;
                        bool? isChained = null;

                        if (parts.Length >= 3)
                        {
                            isChained = parts[2].Equals("Chained", StringComparison.OrdinalIgnoreCase) ? (bool?)true :
                                        parts[2].Equals("Unchained", StringComparison.OrdinalIgnoreCase) ? (bool?)false :
                                        null;
                        }
                        if (parts.Length >= 2)
                        {
                            if (int.TryParse(parts[0], out skillId))
                            {
                                skillName = parts[1];
                                collection[playerClass].Add(new Skill(skillId, skillName, isChained));
                            }
                        }
                    }
                }
            }
        }

        // skillIds are reused across races and class, so we need a RaceGenderClass to disambiguate them
        public Skill Get(UserEntity user, EachSkillResultServerMessage message)
        {
            var skillId = message.SkillId;

            //check if we have an override first

            var overrideCollection = message.IsHeal ? _healSkillIdOverrides : _damageSkillIdOverrides;
            if (overrideCollection.ContainsKey(user.RaceGenderClass.Class))
            {   //check class specific overrides
                var skill = overrideCollection[user.RaceGenderClass.Class].FirstOrDefault(s => s.Id == skillId);
                //check common overrides
                if (skill == null && overrideCollection.ContainsKey(PlayerClass.Common))
                {
                    skill = overrideCollection[PlayerClass.Common].FirstOrDefault(s => s.Id == skillId);
                }
                
                if (skill != null)
                    return skill;
            } 

            var raceGenderClass = user.RaceGenderClass;
            var comparer = new Helpers.ProjectingEqualityComparer<Skill, int>(x => x.Id);
            foreach (var rgc2 in raceGenderClass.Fallbacks())
            {
                if (!_userSkilldata.ContainsKey(rgc2))
                    continue;

                var searchSkill = new UserSkill(skillId, raceGenderClass, null);

                var index = _userSkilldata[rgc2].BinarySearch(searchSkill, comparer);
                if (index < 0)
                    index = ~index - 1;
                if (index < 0)
                    continue;

                var item = _userSkilldata[rgc2][index];
                return item;
            }
            return null;
        }
    }
}

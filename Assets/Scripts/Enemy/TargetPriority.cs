using UnityEngine;

namespace Game.AI.Enemies
{
    [System.Serializable]
    public class TargetPriority
    {
        [Range(1, 5)] public int campfire = 1;
        [Range(1, 5)] public int player = 2;
        [Range(1, 5)] public int structure = 3;
        [Range(1, 5)] public int wall = 4;
        [Range(1, 5)] public int defense = 5;

        public int GetPriority(Targetable.TargetType type)
        {
            return type switch
            {
                Targetable.TargetType.Campfire => campfire,
                Targetable.TargetType.Player => player,
                Targetable.TargetType.Structure => structure,
                Targetable.TargetType.Wall => wall,
                Targetable.TargetType.Defense => defense,
                _ => int.MaxValue
            };
        }

        #if UNITY_EDITOR
        public void Validate()
        {
            int[] values =
            {
                Mathf.Clamp(campfire, 1, 5),
                Mathf.Clamp(player, 1, 5),
                Mathf.Clamp(structure, 1, 5),
                Mathf.Clamp(wall, 1, 5),
                Mathf.Clamp(defense, 1, 5)
            };

            bool[] used = new bool[6]; // indices 1..5

            for (int i = 0; i < values.Length; i++)
            {
                int value = values[i];

                // If this value is already taken, find the next unused slot.
                if (used[value])
                {
                    for (int replacement = 1; replacement <= 5; replacement++)
                    {
                        if (!used[replacement])
                        {
                            value = replacement;
                            break;
                        }
                    }
                }

                used[value] = true;
                values[i] = value;
            }

            campfire = values[0];
            player = values[1];
            structure = values[2];
            wall = values[3];
            defense = values[4];
        }
        #endif
    }
}

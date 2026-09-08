using System;
using System.Collections.Generic;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// The current run only. Cleared when a run ends. Ship stat mutations during a run live
    /// on ShipRuntime (Assembly-CSharp), never here and never on the ScriptableObject.
    /// </summary>
    [Serializable]
    public class RunState
    {
        public string selectedShipId = "";
        public int level = 1;
        public int subLevel = 1;
        public int respawnsRemaining = 1;
        public bool runInProgress;
        public int enemiesKilled;
        public float timeSinceGameStarted;

        public string abilityName = "";
        public int abilityLevel;

        public List<int> upgradeIndices = new List<int>();

        public void Clear()
        {
            selectedShipId = "";
            level = 1;
            subLevel = 1;
            respawnsRemaining = 1;
            runInProgress = false;
            enemiesKilled = 0;
            timeSinceGameStarted = 0f;
            abilityName = "";
            abilityLevel = 0;
            upgradeIndices.Clear();
        }
    }
}

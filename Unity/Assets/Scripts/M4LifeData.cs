using System;
using Reborn1985.M2;
using UnityEngine;

namespace Reborn1985.M4
{
    [Serializable]
    public sealed class M4LifeData
    {
        public int dayIndex = 1, purchasedMeals;
        public long mealExpenseCents;
        public bool helpOffered, helpCompleted, helpMealClaimed;
        public bool openingSeen, letterRead, yearRead, noticeRead, puddleObserved, kettleObserved, residentTalked;
        public M4LifeData Copy() => (M4LifeData)MemberwiseClone();
    }
    [Serializable]
    public sealed class M4SaveData
    {
        public int version = 1;
        public string scene = "M1Greybox", parameters;
        public Vector2 position, facing;
        public M2RunSnapshot run;
    }
    public enum LifeView { None, Breakfast, Help, Rest, Letter, Year, Notice, Puddle, Kettle, Resident, Journal, Load }
}

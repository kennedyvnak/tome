namespace Kennedy.UnityUtility.RangedValues
{
    [System.Serializable]
    public struct RangedInt
    {
        public int min;
        public int max;

        /// <returns>A random value between the min and max</returns>
        public int RandomRange() => UnityEngine.Random.Range(min, max + 1);

        public RangedInt(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        /// <returns>True if the value is between min and max</returns>
        public bool Contains(int time) => time >= min && time <= max;

        /// <returns>True if the value is between min and max</returns>
        public bool Contains(float time) => time >= min && time <= max;
    }
}

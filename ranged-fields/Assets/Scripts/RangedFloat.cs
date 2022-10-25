namespace Kennedy.UnityUtility.RangedValues
{
    [System.Serializable]
    public struct RangedFloat
    {
        public float min;
        public float max;

        /// <returns>A random value between the min and max</returns>
        public float RandomRange() => UnityEngine.Random.Range(min, max);

        public RangedFloat(float min, float max)
        {
            this.min = System.Math.Min(min, max);
            this.max = max;
        }

        /// <returns>True if the value is between min and max</returns>
        public bool Contains(float t) => t >= min && t <= max;
    }
}
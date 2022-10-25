using UnityEngine;

namespace Kennedy.UnityUtility.RangedValues.Tests
{
    public class RangeValueDrawerTest : MonoBehaviour
    {
        [SerializeField] private RangedInt m_RangedInt;
        [SerializeField] private RangedFloat m_RangedFloat;

        [SerializeField, RangedValue(-100, 100)] private RangedInt m_MinMaxRangedInt;
        [SerializeField, RangedValue(0f, 50f)] private RangedFloat m_MinMaxRangedFloat;
    }
}